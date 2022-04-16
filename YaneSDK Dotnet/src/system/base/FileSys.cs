using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Reflection;

using Sdl;
using Yanesdk.Ytl;
using Yanesdk.Math; // Zip解凍に使うCRC32

namespace Yanesdk.System
{
	/// <summary>
	/// fileを扱うときに使うと良い。
	/// </summary>
	/// <remarks>
	///	ファイルシステムクラス。
	///
	///	path関連は、multi-threadを考慮していない。
	///	(つまりsetPathしているときに他のスレッドがgetPathするようなことは
	///	想定していない)
	///
	///	pathはたいてい起動時に一度設定するだけなので、これでいいと思われる。
	///
	///	使用例)
	///	FileSys.addPath("..");
	///	FileSys.addPath("sdl\src\");
	///	FileSys.addPath("yanesdk4d/");
	///	char[] name = FileSys.makeFullName("y4d.d");
	///
	///	./y4d.d(これはディフォルト) と ../y4d.d と sdl/src/y4d.d と
	///	yanesdk4d/y4d.d を検索して、存在するファイルの名前が返る。
	///
	///	また、アーカイバとして、 FileArchiverBase 派生クラスを
	///	addArchiver で指定できる。その Archiver も read/readRWのときに
	///	必要ならば自動的に呼び出される。
	/// </remarks>
	public class FileSys
	{

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <remarks>
		/// コンストラクタでは、addPath("./");で、カレントフォルダを
		/// 検索対象としている。
		/// </remarks>
		static FileSys()
		{
			AddPath("");
		}

		/// <summary>
		/// フォルダ名とファイル名をくっつける。
		/// </summary>
		/// <remarks>
		///	1. / は \ に置き換える
		///	2.左辺の終端の \ は自動補間する
		///	"a"と"b"をつっつければ、"a\b"が返ります。
		///	3.駆け上りpathをサポートする
		///	"dmd/bin/"と"../src/a.d"をつくっければ、"dmd\src\a.d"が返ります。
		///	4.カレントpathをサポートする
		///	"./bin/src"と"./a.d"をくっつければ、"bin\src\a.d"が返ります
		///	"../src/a.d"と"../src.d"をつっつければ"..\src\src\src.d"が返ります
		///	5.途中のかけあがりもサポート
		///	"./src/.././bin/"と"a.c"をつっつければ、"bin\a.c"が返ります。
		///	6.左辺が ".."で始まるならば、それは除去されない
		///	"../src/bin"と"../test.c"をつっつければ、"../src/test.c"が返ります。
		/// 7.network driveを考慮
		/// "\\my_network\test"と"../test.c"をつっつければ"\\my_network\test.c"が返る。
		/// "\\my_network\"と"../test.c"は"\\my_network\..\test.c"が返る
		/// (\\の直後の名前はnetwork名なのでvolume letter扱いをしなくてはならない)
		/// </remarks>
		///	<code>
		/// // 動作検証用コード
		/// string name;
		///	name = FileSys.concatPath("", "b");  // b
		///	name = FileSys.concatPath("a", "b");  // a\b
		///	name = FileSys.concatPath("dmd/bin", "../src/a.d"); // dmd\src\a.d
		///	name = FileSys.concatPath("dmd\\bin", "../src\\a.d"); // dmd\src\a.d
		///	name = FileSys.concatPath("../src/a.d", "..\\src\\src.d"); // ..\src\src\src.d
		///	name = FileSys.concatPath("../src/bin", "../test.c"); // ../src/test.c
		///	name = FileSys.concatPath("./src/.././bin", "a.c"); // bin\a.c
		///	name = FileSys.concatPath("../../test2/../test3", "test.d"); // ..\..\test3\test.d
		/// name = FileSys.concatPath("\\\\my_network\\test", "../test.c"); // bin\a.c
		///	name = FileSys.concatPath("\\\\my_network\\", "../test.d"); // ..\..\test3\test.d
		/// </code>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static string ConcatPath(string path1 , string path2)
		{
			//	bが絶対pathならば、そのまま返す。
			if ( IsAbsPath(path2) )
				return ConcatPathHelper("" , path2);
			string s = ConcatPathHelper("" , path1);
			//	終端が \ でないなら \ を入れておく。
			if ( s.Length != 0 && !s.EndsWith("\\") && !s.EndsWith("/") )
				s += Path.DirectorySeparatorChar;
			return ConcatPathHelper(s , path2);
		}

		/// <summary>
		/// 絶対pathかどうかを判定する
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static bool IsAbsPath(string path)
		{
			if ( path == null || path.Length == 0 )
				return false;
			if ( path[0] == '\\' || path[0] == '/' )
				return true;
			return path.Contains(Path.VolumeSeparatorChar.ToString());
			// volume separator(':')があるということは、
			//	絶対pathと考えて良いだろう
		}

		/// <summary>
		/// path名を連結するときのヘルパ。駆け上がりpath等の処理を行なう。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		private static string ConcatPathHelper(string path1 , string path2)
		{
			if ( path2 == null || path2.Length == 0 )
				return path1; // aが空なのでこのまま帰る

			StringBuilder res = new StringBuilder(path1);
			for ( int i = 0 ; i < path2.Length ; ++i )
			{
				char c = path2[i];
				bool bSep = false;
				if ( c == '/' || c == '\\' )
				{
					bSep = true;
				}
				else if ( c == '.' )
				{
					//	次の文字をスキャンする
					char n = ( i + 1 < path2.Length ) ? path2[i + 1] : '\0';
					//	./ か .\
					if ( n == '/' || n == '\\' ) { i++; continue; }
					//	../ か ..\
					if ( n == '.' )
					{
						char n2 = ( i + 2 < path2.Length ) ? path2[i + 2] : '\0';
						if ( n2 == '/' || n2 == '\\' )
						{
							string nn = GetDirName(res.ToString());
							if ( nn == res.ToString() )
							{
								//	".."分の除去失敗。
								//	rootフォルダかのチェックを
								//	したほうがいいのだろうか..
								res.Append("..");
								res.Append(Path.DirectorySeparatorChar);
							}
							else
							{
								res = new StringBuilder(nn);
							}
							i += 2;
							continue;
						}
					}
				}
				if ( bSep )
				{
					res.Append(Path.DirectorySeparatorChar);
				}
				else
				{
					res.Append(c);
				}
			}
			return res.ToString();
		}


		/// <summary>
		/// ファイル名に、setPathで指定されているpathを結合する(存在チェック付き)
		/// </summary>
		/// <remarks>
		///	ただし、ファイルが存在していなければ、
		///	pathは、設定されているものを先頭から順番に調べる。
		///	ファイルが見つからない場合は、元のファイル名をそのまま返す
		///	fullname := setPathされているpath + localpath + filename;
		/// </remarks>
		/// <see cref=""/>
		/// <param name="localpath"></param>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static string MakeFullName(string localpath , string filename)
		{
			return MakeFullName(ConcatPath(localpath , filename));
		}

		/// <summary>
		/// ファイル名に、setPathで指定されているpathを結合する(存在チェック付き)
		/// </summary>
		/// <remarks>
		/// 
		///	ただし、ファイルが存在していなければ、
		///	pathは、設定されているものを先頭から順番に調べる。
		///	ファイルが見つからない場合は、元のファイル名をそのまま返す
		///
		///	fullname := setPathされているpath + filename;
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static string MakeFullName(string filename)
		{
			foreach (string path in pathlist)
			{
				string fullname = ConcatPath(path, filename);
				//	くっつけて、これが実在するファイルか調べる
				if (IsExist(fullname))
					return fullname;
			}

			//	アーカイバを用いて調べてみる
			foreach (FileArchiverBase arc in archiver)
			{
				foreach (string path in pathlist)
				{
					string fullname = ConcatPath(path, filename);
					if (arc.IsExist(fullname))
					{
						return fullname;
					}
				}
			}
			return filename;	//	not found..
		}

		/// <summary>
		/// FileSysが読み込むときのディレクトリポリシー。
		/// 
		/// あるファイルを読み込むとき、
		///		・実行ファイル相対で読み込む(default)
		///		・起動時のworking directory相対で読み込む
		///		・現在のworking directory相対で読み込む
		/// の3種から選択できる。
		/// </summary>
		public static CurrentDirectoryEnum DirectoryPolicy = CurrentDirectoryEnum.ExecuteDirectory;

		/// <summary>
		///	ファイルの存在確認
		/// </summary>
		/// <remarks>
		///	指定したファイル名のファイルが実在するかどうかを調べる
		///	setPathで指定されているpathは考慮に入れない。
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static bool IsExist(string filename)
		{
			using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
				return File.Exists(filename);
		}

		/// <summary>
		/// ファイルの生存確認(pathも込み)
		/// </summary>
		/// <remarks>
		///	setPathで指定したpathも含めてファイルを探す。
		///	アーカイブ内のファイルは含まない。
		///	あくまでファイルが実在するときのみそのファイル名が返る。
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns>ファイルが見つからない場合はnull。</returns>
		public static string IsRealExist(string filename)
		{
			foreach ( string path in pathlist )
			{
				string fullname = ConcatPath(path , filename);
				//	くっつけて、これが実在するファイルか調べる
				if ( IsExist(fullname) )
					return fullname;
			}
			return null;
		}

		/// <summary>
		/// pathを取得。
		/// </summary>
		/// <returns></returns>
		public static List<string> PathList { get { return pathlist; } }


		/// <summary>
		/// pathを設定。ここで設定したものは、makeFullPathのときに使われる
		/// </summary>
		/// <remarks>
		///	設定するpathの終端は、\ および / でなくとも構わない。
		///	(\ および / で あっても良い)
		/// 
		/// ディフォルトでは、""のみが設定されている。
		/// </remarks>
		/// <param name="pathlist_"></param>
		public static void SetPath(string[] pathlist_)
		{
			pathlist.Clear();
			pathlist.AddRange(pathlist_);
		}

		/// <summary>
		/// pathを追加。ここで設定したpathを追加する。
		/// </summary>
		/// <remarks>
		/// 
		///	設定するpathの終端は、\ および / でなくとも構わない。
		///	(\ および / で あっても良い)
		///	ただし、"."や".."などを指定するときは、"\" か "/"を
		///	付与してないといけない。(こんなのを指定しないで欲しいが)
		///
		///	ディフォルトでは""のみがaddPathで追加されている。
		///	(カレントフォルダを検索するため)
		/// </remarks>
		/// <param name="path"></param>
		public static void AddPath(string path) { pathlist.Add(path); }

		/// <summary>
		/// pathのリスト
		/// </summary>
		private static List<string> pathlist = new List<string>();

		/// <summary>
		/// 親フォルダ名を返す。
		/// </summary>
		/// <param name="fname"></param>
		/// <returns></returns>
		/// <remarks>
		/// ディレクトリ名の取得。
		/// 例えば、 "d:\path\foo.bat" なら "d:\path\" を返します。
		///		"d:\path"や"d:\path\"ならば、"d:\"を返します。
		///	※　windows環境においては、'/' は使えないことに注意。
		///		(Path.DirectorySeparatorChar=='\\'であるため)
		///	※　終端は必ず'\'のついている状態になる
		/// 　　ただし、返し値の文字列が空になる場合は除く。
		/// 　　つまり、getDirName("src\")==""である。
		///	※　終端が ".." , "..\" , "../"ならば、これは駆け上がり不可。
		///		(".."の場合も終端は必ず'\'のついている状態になる)
		/// ※  network driveを考慮して、"\\network computer名\"までは、
		/// 　ドライブレター扱い。
		/// </remarks>
		public static string GetDirName(string path)
		{
			//	コピーしておく
			string path2 = path;

			//	終端が'\'か'/'ならば1文字削る
			int l = path2 == null ? 0 : path2.Length;
			if ( l != 0 )
			{
				bool bNetwork = ( path2.Length >= 2 ) && ( path2.Substring(0 , 2) == "\\\\" );

				int vpos = -1;
				// volume separator(networkフォルダの場合、
				// computer名の直後の\マーク)のあるpos
				if ( bNetwork )
				{
					for ( int i = 2 ; i < path2.Length ; ++i )
					{
						char c = path2[i];
						if ( c == '\\' || c == '/' )
						{
							vpos = i;
							break;
						}
					}
				}

				if ( ( path2.EndsWith("\\") || path2.EndsWith("/") ) && vpos != path2.Length - 1 )
					path2 = path2.Remove(--l);

				//	もし終端が".."ならば、これは駆け上がり不可。
				if ( path2.EndsWith("..") )
				{
					//	かけ上がれねーよヽ(`Д´)ノ
				}
				else
				{
					// fullname = Path.GetDirectoryName(fullname);
					// 動作が期待するものではないので自前で書く。
					for ( int pos = path2.Length - 1 ; ; --pos )
					{
						char c = path2[pos];
						if ( c == Path.VolumeSeparatorChar || pos == vpos )
						{
							// c: なら c:
							// c:/abc なら c: に。
							// c:abc なら c:に。
							if ( pos != path2.Length - 1 )
								path2 = path2.Remove(pos + 1);
							break;
						}
						if ( c == '\\' || c == '/' )
						{
							// そこ以下を除去
							path2 = path2.Remove(pos);
							break;
						}
						if ( pos == 0 )
						{ // 単体フォルダ名なので空の文字を返すのが正しい
							path2 = "";
							break;
						}
					}
				}
				if ( !path2.EndsWith("\\") && !path2.EndsWith("/") && path2.Length != 0 )
				{ path2 += Path.DirectorySeparatorChar; }
			}
			return path2;
		}

		/// <summary>
		/// 正味のファイル名だけを返す(フォルダ名部分を除去する)
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static string GetPureFileName(string filename)
		{
			if ( filename == null )
				return "";
			return Path.GetFileName(filename);
		}

		/// <summary>
		/// readメソッド
		/// </summary>
		/// <remarks>
		/// fileを読み込む。ファイルが実在しないときは、
		///	setArchiveで設定されているアーカイバを利用して読み込めるか試す。
		///	読み込めないときはnullが返る。
		/// 
		/// 	FileSys.addPath("src");
		///		byte[] data = FileSys.read("cacheobject.cs");
		///			// ↑src/cacheobject.csが読み込まれる
		///
		/// 
		/// メモリ上のテキストをファイルと錯覚して読み込ませることも出来る。
		///		例)
		///		byte[] data = File.Sys("MEM:abc"); だと、dataには Unicodeで[BOM]+"abc" が入る
		/// 
		/// BOMが入るのは、CSVReaderクラスがBOMでunicodeかどうかを判定するためである。
		/// 
		/// </remarks>
		/// <param name="filename">filenameはstring(utf-16)である。</param>
		/// <returns></returns>
		public static byte[] Read(string filename)
		{

			if ( filename.Length >= 4 &&
				filename.Substring(0 , 4).Equals("MEM:" , StringComparison.CurrentCultureIgnoreCase) )
			{
				// メモリからの読み出し
				byte[] data = new byte[( filename.Length - 4 ) * 2 + 2];
				data[0] = 0xff;
				data[1] = 0xfe;
				for ( int i = 4 , j = 2 ; i < filename.Length ; ++i , j += 2 )
				{
					char c = filename[i];
					data[j + 0] = ( byte ) ( c & 0xff );
					data[j + 1] = ( byte ) ( ( c >> 8 ) & 0xff );
				}
				return data;
			}

			string f = IsRealExist(filename);
			if ( f != null ) { return ReadSimple(f); }

			//	ここで、アーカイバから読み込む必要がある
			foreach ( FileArchiverBase arc in archiver )
			{
				foreach ( string path in pathlist )
				{
					string file = ConcatPath(path , filename);
					byte[] p = arc.Read(file);
					if ( p != null )
						return p;
					//	アーカイバで、解凍が成功するまで
					//	次の候補(path,アーカイバ)を試していく。
				}
			}
			return null;
		}

		/// <summary>
		/// ファイルからデータを読み込む
		/// </summary>
		/// <returns></returns>
		public static Stream OpenRead(string filename)
		{
			Stream stream;
			try
			{
				using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
					stream = File.OpenRead(filename);
			}
			catch
			{
			//	try
			//	{
			//		stream = resourceManager.GetStream(filename);
			//		//	stream = null;
			//	}
			//	catch
			//	{
					stream = null;
			//	}
			}
			return stream;
		}

		/// <summary>
		/// ファイルを読み込む(例外は投げない)
		/// </summary>
		/// <remarks>
		/// 
		///	file.readが例外を投げてうっとおしいときに使います。
		///	読み込みに失敗するとnullが返ります。
		///	pathのサーチは行ないません。登録されているarchiverも無視です。
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns>読み込んだデータをbyte[]で返すが、
		/// 読み込みに失敗した場合にはnullを返す。</returns>
		public static byte[] ReadSimple(string filename)
		{
			byte[] rdata = null;
			try
			{
				using ( Stream fs = FileSys.OpenRead(filename) )
				{
					rdata = new byte[fs.Length];
					fs.Read(rdata , 0 , rdata.Length);
				}
			}
			catch
			{
				return null;
			}
			return rdata;
		}

		/// <summary>
		/// ファイルを書き込む(例外は投げない)
		/// </summary>
		/// <remarks>
		/// 
		///	file.readが例外を投げてうっとおしいときに使います。
		///	読み込みに失敗すると非0(YanesdkResult.no_error以外)が返ります。
		/// 
		/// </remarks>
		/// <param name="filename"></param>
		/// <param name="rdata"></param>
		/// <returns></returns>
		public static YanesdkResult WriteSimple(string filename , byte[] rdata)
		{
			try
			{
				using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
				using (FileStream fs = File.OpenWrite(filename))
				{
					fs.Write(rdata , 0 , rdata.Length);
				}
			}
			catch ( Exception )
			{
				return YanesdkResult.FileWriteError;
			}
			return YanesdkResult.NoError;
		}

		/// <summary>
		/// 例外を投げない書き込みメソッド
		/// </summary>
		/// <remarks>
		///	書き込みするときに、FileArchiverを指定できる
		///	arc == null なら、そのまま書き出す
		///
		///	成功すれば0。失敗すれば非0が返る。
		/// </remarks>
		/// <param name="filename"></param>
		/// <param name="data"></param>
		/// <param name="arc"></param>
		/// <returns></returns>
		public static YanesdkResult Write(string filename , byte[] data , FileArchiverBase arc)
		{
			if ( data == null )
				return YanesdkResult.InvalidParameter;	// no data
			if ( arc == null )
			{
				if ( FileSys.WriteSimple(filename , data) != YanesdkResult.NoError )
					return YanesdkResult.FileWriteError; // だめぽ
				return YanesdkResult.NoError;
			}
			else
			{
				return arc.Write(filename , data);
			}
		}

		/// <summary>
		/// 例外を投げない書き込みメソッド
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="data"></param>
		/// <returns>成功すれば0(YanesdkResult.NoError) 失敗すれば非0が返る。</returns>
		public static YanesdkResult Write(string filename , byte[] data)
		{
			return Write(filename , data , null);
		}

		/// <summary>
		/// FileArchiverBaseの派生クラス(zip解凍クラスetc..)を設定する
		/// </summary>
		/// <remarks>
		/// ここで設定したものは、isExist,makeFullName,read/readRWで有効。
		/// </remarks>
		/// <param name="a"></param>
		public static void SetArchiver(FileArchiverBase[] a)
		{
			archiver.Clear();
			archiver.AddRange(a);
		}

		/// <summary>
		/// FileArchiverBaseの派生クラス(zip解凍クラスetc..)を追加する
		/// </summary>
		/// <remarks>
		/// ここで設定したものは、isExist,makeFullName,read/readRWで有効。
		/// </remarks>
		/// <param name="name"></param>
		public static void AddArchiver(FileArchiverBase s) { archiver.Add(s); }

		/// <summary>
		/// FileArchiverBaseの派生クラス(zip解凍クラスetc..)を取得する
		/// </summary>
		/// <remarks>
		/// ここで設定したものは、isExist,makeFullName,read/readRWで有効。
		/// </remarks>
		/// <returns></returns>
		public static List<FileArchiverBase> Archiver { get { return archiver; } }

		/// <summary>
		/// ファイルを読み込むときに、圧縮ファイル等に対して独自のアーカイバクラスを
		/// 用意してこのfilesysからシームレスに読み込ませることができる。
		/// </summary>
		private static List<FileArchiverBase> archiver = new List<FileArchiverBase>();

		/// <summary>
		/// テンポラリファイルを扱う。
		/// </summary>
		public class TmpFile : IDisposable
		{
			/// <summary>
			/// テンポラリファイルを作成して扱うためのクラス。
			/// 1.メモリ(byte[])を渡して、それをテンポラリファイルに書き出す(setMemory)
			/// 2.既存のファイルをあたかも 1.のように扱う(setFile)
			///
			/// 1. or 2.を行ない、そのあとgetFileNameすれば、1.ならば
			/// テンポラリファイル、2.ならば、setFileで渡したファイル名が返る。
			/// 
			/// </summary>
			/// <param name="file"></param>
			/// <remarks>
			/// 2.の場合、実在するファイルなので終了するときに削除はされない。
			/// 削除されるのは、setMemoryでファイルを生成したときのみ。
			/// </remarks>
			public void SetFile(string file)
			{
				filename = file;
				bMade = false;
			}

			/// <summary>
			/// byte[]を渡して、テンポラリファイルを生成する。
			/// </summary>
			/// <param name="mem"></param>
			/// <returns></returns>
			/// <remarks>
			/// 渡されるmemが、nullならば、テンポラリファイルは生成しない。
			/// 生成に失敗したら非YanesdkResult.no_error以外が返る。
			/// 
			/// @portnote テンポラリファイルの作成は環境依存。移植時に注意が必要。
			/// </remarks>
			public YanesdkResult SetMemory(byte[] mem)
			{
				Release();
				if ( mem == null )
					return YanesdkResult.InvalidParameter;	//	データ無いやん..

				// テンポラリファイル名を取得する(環境依存)
				filename = Path.GetTempFileName();
				//	ファイルを生成してみるテスト。
				if ( WriteSimple(filename , mem) != 0 )
				{
					Release();
					return YanesdkResult.FileWriteError; // 書き出しエラー
				}
				bMade = true;
				return YanesdkResult.NoError;
			}

			/// <summary>
			/// 確保していたテンポラリファイルを解放する(作成していたときのみ)。
			/// </summary>
			/// <remarks>
			/// setMemoryでファイルを生成していたときのみファイルを削除。
			/// setFileで設定していた場合は、ファイルは削除しない。
			/// </remarks>
			public void Release()
			{
				try
				{
					// このファイルはフルパスで指定されているはずなのでCurrentDirectoryの変更はしないで良い。
					if ( bMade )
						File.Delete(filename);
				}
				catch { } // 握り潰す
				filename = null;
				bMade = false;
			}

			/// <summary>
			/// ファイル名を返す。
			/// </summary>
			/// <returns></returns>
			/// <remarks>
			/// setFileで設定されたファイル名および、setMemoryで
			/// メモリを渡されたときに生成されたテンポラリファイル名を返します。
			/// </remarks>
			public string FileName { get { return filename; } }

			/// <summary>
			/// 事前に用意されたファイルをセットする。
			/// </summary>
			/// <param name="file"></param>
			public TmpFile(string file) { SetFile(file); }

			/// <summary>
			/// バッファを渡して、そのバッファの内容のファイルを生成。
			/// </summary>
			/// <param name="mem"></param>
			/// <remarks>
			/// 内部的にはmakeFileを用いてテンポラリファイルを生成している。
			/// </remarks>
			public TmpFile(byte[] mem) { SetMemory(mem); }

			/// <summary>
			/// テンポラリファイル名
			/// </summary>
			private string filename;
			/// <summary>
			/// filenameのファイルが、このクラスが生成したファイルなのかを示すフラグ
			/// </summary>
			private bool bMade;

			public void Dispose()
			{
				Release();
				// GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// 	ファイルの実在性をチェックして、無ければファイルを作成
		/// </summary>
		/// <remarks>
		/// アーカイバ内にファイルがあって、readでは読み込めるが、
		/// 実在はしないので他のplug in等に渡せなくて困る場合に、
		/// このメソッドを利用する。
		/// 
		/// つまり、このメソッドの動作としては、
		/// 
		///		1.isRealExist()で実存するかチェック。実在するなら、そのファイル名を返す
		///		2.実存しないならばread(filename)して、それをコピーして書き出した
		///			テンポラリファイルを作成して返す。
		/// 
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static TmpFile GetTmpFile(string filename)
		{
			string file = IsRealExist(filename);
			if ( file != null )
			{
				return new TmpFile(file); // 実在するらしい
			}
			else
			{
				return new TmpFile(Read(filename));
			}
		}

		/// <summary>
		/// fileから読み込む。SDL_RWops*を返す。
		/// </summary>
		/// <remarks>
		/// 
		///	読み込みに失敗すればこの関数は非0が返る
		///
		///			使用例)
		///			SDL_RWops* rwops = FileSys.readRW(name);
		///			if (rwops)
		///			{
		///				chunk = Mix_LoadWAV_RW(rwops,1);
		///				return 0;	// 正常終了
		///			} else {
		///				return 1;	// file not found
		///			}
		///	※ SDL_RWopsとは、SDLで、メモリ内のデータを扱うためのopearator。
		///		SDLでRWのついている関数は、これを引数にとる。
		/// 
		/// ここで得たハンドルはSDL.Close_RW(rwops)で解放すべきなのだが
		/// C#ではdllからのcallbackが実装できないので明示的にSDL.Close_RWを呼び出せないらしく、
		/// SDL.LoadXXX_RWの freesec 引数として 1(==trueの意味)を指定すると良いようだ。
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static SDL_RWopsH ReadRW(string filename)
		{
			byte[] data = Read(filename);
			if ( data == null )
				return new SDL_RWopsH(); // 読み込めてないやん..

			SDL_RWopsH rw = SDL.SDL_RWFromMem(data);
			data = null;
			GC.Collect(0); // でかいメモリなのですぐに片付けてくれんと困る。
			return rw;
		}
	
	}

	/// <summary>
	/// アーカイバ内のファイルをcacheしたりするときに使うと良いクラス。
	/// 
	/// FileArchiverZipでcacheのために用いている。
	/// ユーザーが直接用いることはない(だろう)
	/// </summary>
	public class ArchiveFileList
	{
		/// <summary>
		/// archiveファイルから、ファイルリストへのmapper
		/// </summary>
		public Dictionary<string , Dictionary<string , object>> ArchiveList
		{ get { return archiveList; } }

		private Dictionary<string , Dictionary<string , object>> archiveList
			= new Dictionary<string , Dictionary<string , object>>();

		/// <summary>
		/// 1つのarchive(例:Zipファイル)内に存在するファイルのリストと、
		/// そのファイルに紐付けされた情報
		/// </summary>
		public Dictionary<string , object> FileList
		{ get { return fileList; } }
		
		private Dictionary<string , object> fileList
			= new Dictionary<string , object>();


		/// <summary>
		/// FileListのなかに所定のファイルの情報があるのか
		/// </summary>
		public bool IsExistArchive(string file)
		{
			return archiveList.ContainsKey(file);
		}
	}



	/// <summary>
	/// Fileで用いる、圧縮ファイルを扱うためのクラス。
	/// </summary>
	/// <remarks>
	/// Fileにこのクラスの派生クラスをセットしてやれば、
	/// FileSys.readやisExistで、圧縮ファイルも検索するようになる
	/// 
	/// 実装例として FileArchiverZipFile も参考にすること。
	/// </remarks>
	public abstract class FileArchiverBase
	{
		///	存在するかを問い合わせる関数
		/**
			path等は無視。file名には '/'は用いておらず、'\'を用いてあり
			(linux環境では'/')、また、".."や"."は付帯していないと仮定して良い。
		*/
		public abstract bool IsExist(string filename);

		/// <summary>
		/// 存在するか調べて読み込む。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		/// <remarks>
		/// 存在しないときは、nullが戻る
		/// 
		/// path等は無視。file名には '/'は用いておらず、'\'を用いてあり
		/// (linux環境では'/')、また、".."や"."は付帯していないと仮定して良い。
		/// </remarks>
		public abstract byte[] Read(string filename);

		/// <summary>
		/// ファイルを書き込む。
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		/// <remarks>
		/// このクラスのreadで読み出せるように書き込みを実装する
		/// (実装しなくとも良い)
		/// 
		/// 書き出しに成功すれば0,失敗すれば非0が返るようにする。
		/// </remarks>
		public abstract YanesdkResult Write(string filename , byte[] data);

		/// <summary>
		/// ファイルのenumeratorを返す。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// 自分のサポートする書庫ファイルの場合は、そのenumeratorを返す。
		/// </remarks>
		public abstract DirEnumerator GetEnumerator(string filename);
	}

	/// <summary>
	/// 独自形式の圧縮ファイルをシームレスに扱うためのクラス(サンプル)。
	/// </summary>
	/// <remarks>
	/// FileSys.addArchiverでセットすれば、
	/// FileSys.read / readRW / isExistで読み込るようになる。
	/// </remarks>
	public class FileArchiverSample : FileArchiverBase
	{

		/// <summary>
		/// 存在するかどうかのチェック関数。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		/// <remarks>
		/// ここでは、与えられたファイル名に".bin"を付与したファイルが存在すれば
		/// trueを返します。
		/// </remarks>
		public override bool IsExist(string filename)
		{
			return FileSys.IsExist(filename + ".bin");
		}

		/// <summary>
		/// 読み込む関数。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		/// <remarks>
		/// ここでは与えられたファイル名に".bin"を付与したファイルがあれば
		/// 読み込みます。(独自の圧縮形式等ならば、そのあと、decode処理を
		/// 行なえば良い)。
		/// </remarks>
		public override byte[] Read(string filename)
		{
			return FileSys.ReadSimple(filename + ".bin");
		}

		/// <summary>
		/// 書き込む関数。
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="rdata"></param>
		/// <returns></returns>
		/// <remarks>
		/// ここでは与えられたファイル名に".bin"を付与したファイルに書き出します。
		/// (独自の圧縮形式等ならば、そのあと、encode処理を行なってから
		/// 書き出すと良い)
		/// 
		/// この関数は実装していなくてもok
		/// </remarks>
		public override YanesdkResult Write(string filename , byte[] rdata)
		{
			return FileSys.WriteSimple(filename + ".bin" , rdata);
		}

		/// <summary>
		/// enumeratorもわからなければ実装しなくてもok。
		/// </summary>
		/// <returns></returns>
		public override DirEnumerator GetEnumerator(string filename) { return null; }
	}

	/// <summary>
	/// Streamをランダムアクセスするためのwrapper
	/// </summary>
	/// <remarks>
	/// Zipファイルを扱うときに用いる。
	/// little endianだと仮定している。(zip headerがそうなので)
	/// </remarks>
	class StreamRandAccessor
	{

		/// <summary>
		/// openしたストリームを渡すナリよ！
		/// </summary>
		/// <param name="f_"></param>
		public void SetStream(Stream f_)
		{
			stream_length = f_.Length;
			f = f_;
			// 読み込んでいないので。
			pos = 0;
			readsize = 0;
			bWrite = false;
		}

		/// <summary>
		/// ストリームの先頭からiのoffsetの位置のデータbyteを読み込む
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public byte GetByte(long i)
		{
			check(ref i , 1);
			return data[i];
		}

		/// <summary>
		/// ストリームの先頭からiのoffsetの位置に、データbyteを書き込み
		/// </summary>
		/// <param name="i"></param>
		/// <param name="b"></param>
		public void PushByte(long i , byte b)
		{
			check(ref i , 1);
			data[i] = b;
			bWrite = true;
		}


		/// <summary>
		///	ストリームの先頭からiのoffsetの位置のデータushortを読み込む
		/// </summary>
		/// <remarks>
		/// little endian固定。
		/// </remarks>
		/// <param name="i"></param>
		/// <returns></returns>
		public ushort GetUshort(long i)
		{
			check(ref i , 2);

/*
#if BigEndian
			// BigEndianのコードは、すべて未検証
			// Zipファイルからの読み込みにしか使わない。
			// Zipファイル内の値はlittle endianなので
			// この #if BigEndianのコードが必要になることはない。

			byte b0 = data[i];
			byte b1 = data[i + 1];
			return (b0 << 8) | b1;
#else
 */
			//	return *(ushort*)&data[i];
			// return BitConverter.ToUInt16(data , ( int ) i);

			// ↑BitConverterはLE/BEが環境依存。

			byte b0 = data[i];
			byte b1 = data[i + 1];
			return (ushort)((b1 << 8) | b0);
			
//#endif
		}

		/// <summary>
		///	ストリームの先頭からiのoffsetの位置のデータuintを読み込む
		/// </summary>
		/// <remarks>
		/// little endian固定。
		/// </remarks>
		/// <param name="i"></param>
		/// <returns></returns>
		public uint GetUint(long i)
		{
			check(ref i , 4);

//#if BigEndian 
//			return bswap(*(uint *)&data[i]);
//#else
//			return BitConverter.ToUInt32(data , ( int ) i);
			//			return *(uint*)&data[i];

			// ↑BitConverterはLE/BEが環境依存。

			byte b0 = data[i];
			byte b1 = data[i + 1];
			byte b2 = data[i + 2];
			byte b3 = data[i + 3];
			return (uint)((b3 << 24)|(b2 << 16)|(b1 << 8) | b0);
			
//#endif
		}

		/// <summary>
		///	ストリームの先頭からiのoffsetの位置のデータushortを書き込む
		/// </summary>
		/// <remarks>
		/// little endian固定。
		/// </remarks>
		/// <param name="i"></param>
		/// <returns></returns>
		public void PutUshort(long i , ushort us)
		{
			check(ref i , 2);

//#if BigEndian 
//			data[0] = (byte)us;
//			data[1] = (byte)(us >> 8);
//#else
			//			*(ushort*)&data[i] = us;
			data[i + 0] = ( byte ) ( ( us ) & 0xff );
			data[i + 1] = ( byte ) ( ( us >> 8 ) );
//#endif
			bWrite = true;
		}

		/// <summary>
		///	ストリームの先頭からiのoffsetの位置のデータuintを書き込む
		/// </summary>
		/// <remarks>
		/// little endian固定。
		/// </remarks>
		/// <param name="i"></param>
		/// <returns></returns>
		public void PutUint(long i , uint ui)
		{
			check(ref i , 4);

//#if BigEndian 
//			ui = bswap(ui);
//#else
			//			*(uint *)&data[i] = ui;
			data[i + 0] = ( byte ) ( ( ui ) & 0xff );
			data[i + 1] = ( byte ) ( ( ui >> 8 ) & 0xff );
			data[i + 2] = ( byte ) ( ( ui >> 16 ) & 0xff );
			data[i + 3] = ( byte ) ( ( ui >> 24 ) );
//#endif
			bWrite = true;
		}

		/// <summary>
		/// 書き込みを行なう
		/// </summary>
		/// <remarks>
		///	pushUint等でデータに対して書き込みを行なったものを
		///	ストリームに戻す。このメソッドを明示的に呼び出さなくとも
		///	256バイトのストリーム読み込み用バッファが内部的に用意されており、
		///	現在読み込んでいるストリームバッファの外にアクセスしたときには
		///	自動的に書き込まれる。
		/// </remarks>
		public void Flush()
		{
			if ( bWrite )
			{
				//	writebackしなくては
				f.Write(data , 0 , readsize);
				bWrite = false;
			}
		}

		/// <summary>
		///	posからsize分読み込む(バッファリング等はしない)
		/// </summary>
		/// <returns>読み込まれたサイズを返す</returns>
		public long Read(byte[] data , long pos , uint size)
		{
			Flush();
			//	ファイルのシークを行なう前にはflushさせておかないと、
			//	あとで戻ってきて書き込むとシーク時間がもったいない

			f.Seek(pos , SeekOrigin.Begin);
			int s;
			try { s = f.Read(data , 0 , ( int ) size); }
			catch { s = 0; }
			// これでは2GBまでしか扱われへんやん..しょぼん..
			return ( long ) s;
		}

		/// <summary>
		/// setStreamも兼ねるコンストラクタ
		/// </summary>
		/// <param name="f"></param>

		public StreamRandAccessor(Stream f) { SetStream(f); }
		public StreamRandAccessor() { }

		public void Dispose() { Flush(); }

		private Stream f;

		private long stream_length;
		private byte[] data = new byte[256]; // 読み込みバッファ
		private int readsize;	 // バッファに読み込めたサイズ。
		private long pos;		 // 現在読み込んでいるバッファのストリーム上の位置
		private bool bWrite;	 // このバッファに対して書き込みを行なったか？
		//	書き込みを行なったら、他のバッファを読み込んだときに
		//	この分をwritebackする必要がある

		/// <summary>
		///	このストリームのiの位置にsizeバイトのアクセスをしたいのだが、
		///	バッファに読まれているかチェックして読み込まれていなければ読み込む。
		///	渡されたiは、data[i]で目的のところにアクセスできるように調整される。
		/// </summary>
		/// <param name="i"></param>
		/// <param name="size"></param>
		private void check(ref long i , uint size)
		{
			if ( i < pos || pos + readsize < i + size )
			{
				//	バッファ外でんがな
				Flush();
				// size<128と仮定して良い
				//	アクセスする場所がbufferの中央に来るように調整。
				int offset = ( int ) ( ( data.Length - size ) / 2 );
				if ( i < offset )
				{
					pos = 0;
				}
				else
				{
					pos = i - offset;
				}
				f.Seek(pos , SeekOrigin.Begin);
				readsize = f.Read(data , 0 , data.Length);
			}
			i -= pos;
		}

		/// <summary>
		/// ストリーム長を返す
		/// </summary>
		public long Length
		{
			get
			{
				if ( f == null )
					return 0;
				return stream_length;
			}
		}
	}


	/// <summary>
	/// zip ファイル(アーカイブ)をシームレスに扱うためのクラス
	/// </summary>
	///	
	///	src/some.zip のアーカイブのなかに、a.txt b.txtが存在するとき、
	///		src/some/a.txt
	///		src/some/b.txt
	///	とアクセスすることが出来る。
	///
	///	このアーカイバーを単独で使うこともできる。
	///	FileArchiverZip a = new FileArchiverZip();
	///	byte[] v = a.read(r"test\test\ToDo.txt");
	///	printf("%.*name",v);
	///	//	test.zipのなかにあるtest/ToDo.txtを読み込まれる
	///
	///	password付きzipファイルにも対応。
	///	FileArchiverZip.s_setPass("testpass");
	///	//	↑事前に全zip共通passを設定する場合。
	///
	///	FileArchiverZip a = new FileArchiverZip();
	///	a.setPass("testpass");
	///	//	↑事後に読み込むファイルごとにpasswordを設定する場合。
	///
	///	byte[] v = a.read(r"test\ToDo.txt");
	///	printf("%.*name",v);
	///
	///	１．passwordは、passwordつき書庫に対してのみ適用される。
	///	２．static void s_setPass(char[])メソッドによって、すべてのzipファイル
	///	共通のパスワードを設定することも出来る。
	///	３．s_setPassでパスワードが設定されていれば、このクラスのコンストラクタで
	///	そのパスワードをsetPass(char[])メソッドで取り込む。
	///	４．s_setPassは、このクラスのコンストラクタが起動したあとで
	///		設定しても無駄。その場合は、このクラスのsetPassを用いる必要がある。
	///
	public class FileArchiverZip : FileArchiverBase
	{
		#region const(DLLの配置位置などはこれを変更してください)

		// Linuxの場合
		// const string DLL_ZIP =
		//	DllManager.DLL_CURRENT + "z";
		//	//(ライブラリのファイル名が「libz.so」のため。なんと言うか妙だ) 
		// → どうせSDL_imageが呼び出すlibpngがzlib12を必要とするので
		// Windowsのときと同じく zlib12を探しに行く。

		const string DLL_ZIP = Sdl.SDL_Initializer.DLL_ZIP;
		#endregion

		public FileArchiverZip()
		{
			// DLLを用いるときは必ずこれを呼び出す
			Yanesdk.System.DllManager.Instance.LoadLibrary(DLL_ZIP);

			zippass = s_zippass;
			this.reader = new FileReaderFile();
		}

		/// <summary>
		/// FileReaderBaseを指定できるほう。
		/// これを指定しておけばファイル読み込み時に、そのreaderが用いられる。
		///		arc = new FileArchiverZip(new FileReaderResource(rm,new FileReaderFile());
		/// などとやれば、リソース内のzipファイルも解凍できるようになる
		/// </summary>
		/// <param name="reader"></param>
		public FileArchiverZip(IFileReader reader)
		{
			zippass = s_zippass;
			this.reader = reader;
		}

		/// <summary>
		/// zlib1.dllを用いる場合、true(defaultではこの設定)
		/// .NET2.0のDeflateStreamを用いる場合は、falseを設定します。
		/// 
		/// ただし、後者の場合、前者の倍ぐらい展開に時間がかかります(´ω`)
		/// </summary>
		public bool UseZlib
		{
			get { return useZlib; }
			set { useZlib = value; }
		}
		private bool useZlib = true;

		private IFileReader reader;

		private ArchiveFileList arcFileList = new ArchiveFileList();

		/// <summary>
		/// zip archiveのなかにあるファイルリスト等をcacheするのか
		/// (default = true)
		/// </summary>
		public bool ReadCache
		{
			set { readCache = value; }
			get { return readCache; }
		}
		private bool readCache = true;

		/// <summary>
		/// ファイルから読み込む
		/// </summary>
		/// <remarks>
		///	1.FileSys.setPathで指定されているpathにあるファイルを優先
		///	2.無い場合、親フォルダにあるzipファイルを探していく
		///	というアルゴリズムになっています。
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns></returns>
		public override byte[] Read(string filename)
		{
			// 見つかればzipファイルを解凍して返す
			//	(保留中)
			byte[] data;
			if ( InnerRead(filename , true , out data) )
			{
				return data;
			}
			return null;
		}

		/// <summary>
		/// ファイルの存在チェック
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public override bool IsExist(string filename)
		{
			byte[] data;
			return InnerRead(filename , false , out data);
		}

		/// <summary>
		/// zlibのwrapper
		/// </summary>
		internal sealed class ZLibWrapper
		{
			public const string ZLIB_VERSION = "1.2.2.0";

			public const int Z_OK = 0;
			public const int Z_STREAM_END = 1;
			public const int Z_NEED_DICT = 2;
			public const int Z_ERRNO = -1;
			public const int Z_STREAM_ERROR = -2;
			public const int Z_DATA_ERROR = -3;
			public const int Z_MEM_ERROR = -4;
			public const int Z_BUF_ERROR = -5;
			public const int Z_VERSION_ERROR = -6;

			public const int Z_NO_FLUSH = 0;
			public const int Z_SYNC_FLUSH = 2;
			public const int Z_FULL_FLUSH = 3;
			public const int Z_FINISH = 4;
			public const int Z_BLOCK = 5;

			[StructLayout(LayoutKind.Sequential , CharSet = CharSet.Ansi)]
			public struct ZStream
			{
				/// <summary>
				/// next input byte
				/// </summary>
				public IntPtr next_in;
				/// <summary>
				/// number of bytes available at next_in
				/// </summary>
				public UInt32 avail_in;
				/// <summary>
				/// total nb of input bytes read so far
				/// </summary>
				public UInt32 total_in;

				/// <summary>
				/// next output byte should be put there
				/// </summary>
				public IntPtr next_out;
				/// <summary>
				/// remaining free space at next_out
				/// </summary>
				public UInt32 avail_out;
				/// <summary>
				/// total nb of bytes output so far
				/// </summary>
				public UInt32 total_out;

				/// <summary>
				/// last error message, NULL if no error
				/// </summary>
				public IntPtr msg;

				private IntPtr state; /* not visible by applications */

				private IntPtr zalloc;  /* used to allocate the internal state */
				private IntPtr zfree;   /* used to free the internal state */
				private IntPtr opaque;  /* private data object passed to zalloc and zfree */

				private int data_type;  /* best guess about the data type: binary or text */
				private UInt32 adler;      /* adler32 value of the uncompressed data */
				private UInt32 reserved;   /* reserved for future use */
			}

			public static int inflateInit2(ref ZStream stream , int windowBits)
			{
				return inflateInit2_(ref stream , windowBits , ZLIB_VERSION , Marshal.SizeOf(stream));
			}

			[DllImport(DLL_ZIP , ExactSpelling = true , CharSet = CharSet.Ansi)]
			private static extern int inflateInit2_(ref ZStream stream , int windowBits , string version , int streamSize);

			[DllImport(DLL_ZIP)]
			public static extern int inflate(ref ZStream stream , int flush);

			[DllImport(DLL_ZIP)]
			public static extern int inflateEnd(ref ZStream stream);
		}

		private class ZipPassUpdate
		{
			private uint[] key = new uint[3];
			CRC32 crc32 = new CRC32();
			public void initKey()
			{
				key[0] = 305419896;
				key[1] = 591751049;
				key[2] = 878082192;
			}
			public void updateKeys(byte b)
			{
				key[0] = crc32.Update(b , key[0]);
				key[1] = key[1] + ( key[0] & 0x000000ff );
				key[1] = key[1] * 134775813 + 1;
				key[2] = crc32.Update(( byte ) ( key[1] >> 24 ) , key[2]);
			}
			public byte decrypt_byte()
			{
				ushort temp = ( ushort ) ( key[2] | 2 );
				return ( byte ) ( ( temp * ( temp ^ 1 ) ) >> 8 );
			}
		}

		/// <summary>
		/// Zipファイル内のファイルリストをcacheするときに必要となる構造体
		/// </summary>
		private class InnerZipFileInfo
		{
			public InnerZipFileInfo(
				uint localHeaderPos ,
				uint compressed_size ,
				uint uncompressed_size
				)
			{
				this.localHeaderPos = localHeaderPos;
				this.compressed_size = compressed_size;
				this.uncompressed_size = uncompressed_size;
			}

			public uint localHeaderPos;
			public uint compressed_size;
			public uint uncompressed_size;
		}

		/// <summary>
		///	zipファイル内のファイル名を指定しての読み込み。
		/// </summary>
		/// <param name="filename">zipファイル名</param>
		/// <param name="innerFileName">zip内のファイル名</param>
		/// <param name="bRead">読み込むのか？読み込むときはbuffにその内容が返る</param>
		/// <param name="buff"></param>
		/// <returns>
		///		bRead=falseのとき、ヘッダ内に該当ファイルが存在すればtrue
		///		bRead=trueのとき、読み込みが成功すればtrue
		/// </returns>
		/// <summary>
		///	zipファイル内のファイル名を指定しての読み込み。
		/// 
		/// 大文字小文字の違いは無視する。
		/// </summary>
		/// <param name="filename">zipファイル名</param>
		/// <param name="innerFileName">zip内のファイル名</param>
		/// <param name="bRead">読み込むのか？読み込むときはbuffにその内容が返る</param>
		/// <param name="buff"></param>
		/// <returns>
		///		bRead=falseのとき、ヘッダ内に該当ファイルが存在すればtrue
		///		bRead=trueのとき、読み込みが成功すればtrue
		/// </returns>
		public bool Read(string filename , string innerFileName , bool bRead , out byte[] buff)
		{
			// .NET Framework2.0には System.Io.CompressionにGZipStreamというクラスがある。
			// これはZipStreamを扱うクラスなのでファイルを扱う部分は自前で用意してやる必要がある
			// cf.
			// http://msdn2.microsoft.com/en-us/library/zs4f0x23.aspx

			buff = null;

			if ( ! reader.IsExist(filename) )
				goto Exit;

			Stream file = reader.Read(filename);
			if ( file == null )
				goto Exit; // おそらくは存在しないか二重openか。

			// 二重openできないので、使いおわったら必ずCloseしなくてはならない。
			// そのためにtry～finallyで括ることにする。
			try
			{
				StreamRandAccessor acc = new StreamRandAccessor(file);

				Dictionary<string,object> cacheList = null;
				
				// 読み込みcacheが有効らしい
				if ( readCache )
				{
					filename = filename.ToLower();
					if ( arcFileList.IsExistArchive(filename) )
					{
						// このarcFileListのなかから探し出す

						// 格納されているファイル名をToLower等で正規化しておく必要あり
						object obj;
						try
						{
							innerFileName = innerFileName.ToLower();
							obj = arcFileList.ArchiveList[filename][innerFileName];
						}
						catch
						{
							return false; // 見つからない
						}
						// ここで取得したobjを元にファイルから読み込む

						InnerZipFileInfo info = obj as InnerZipFileInfo;

						uint localHeaderPos = info.localHeaderPos;
						uint compressed_size = info.compressed_size;
						uint uncompressed_size = info.uncompressed_size;

						return innerExtract(acc , localHeaderPos , bRead , compressed_size , uncompressed_size ,
							 out buff);
					}
					// このファイルにファイルリスト読み込むのが先決では…
					cacheList=  new Dictionary<string,object>();
					arcFileList.ArchiveList.Add(filename,cacheList);
				}

				// Find 'end record index' by searching backwards for signature
				long stoppos;
				//	ulongって引き算とか比較が面倒くさいですなぁ．．（´Д｀）
				if ( file.Length < 66000 )
				{
					stoppos = 0;
				}
				else
				{
					stoppos = acc.Length - 66000;
				}
				long endrecOffset = 0;
				for ( long i = acc.Length - 22 ; i >= stoppos ; --i )
				{
					if ( acc.GetUint(i) == 0x06054b50 )
					{ // "PK\0x05\0x06"
						ushort endcommentlength = acc.GetUshort(i + 20);
						if ( i + 22 + endcommentlength != acc.Length )
							continue;
						endrecOffset = i;
					}
					goto endrecFound;
				}
				// ダメジャン
				goto Exit;

			endrecFound:
				;
				//	---- endrecOffsetが求まったナリよ！

				ushort filenum = acc.GetUshort(endrecOffset + 10);
				//	zipに格納されているファイルの数(分割zipは非対応)

				long c_pos = acc.GetUint(endrecOffset + 16);
				//	central directoryの位置

				//	printf("filenum %d",filenum);

				//	---- central directoryが求まったなりよ！
				while ( filenum-- > 0 )
				{
					if ( acc.GetUint(c_pos) != 0x02014b50 )
					{ // シグネチャー確認!
						goto Exit; //  return false; // おかしいで、このファイル
					}

					uint compressed_size = acc.GetUint(c_pos + 20);
					uint uncompressed_size = acc.GetUint(c_pos + 24);
					ushort filename_length = acc.GetUshort(c_pos + 28);
					ushort extra_field_length = acc.GetUshort(c_pos + 30);
					ushort file_comment_length = acc.GetUshort(c_pos + 32);

					//printf("filenamelength : %d",filename_length);
					// local_header_pos
					uint lh_pos = acc.GetUint(c_pos + 42);
					//	ファイル名の取得
					StringBuilderEncoding fname = new StringBuilderEncoding(filename_length);
					for ( int i = 0 ; i < filename_length ; ++i )
					{
						fname.Add(acc.GetByte(i + c_pos + 46));
					}
					//			printf("%.*name\n",fname);
					//	ファイル名が得られた。

					// string fullfilename = dirname + fname;
					// yield return fullfilename;

					if ( cacheList!=null )
					{
						// readCacheが有効なら、まずは格納していく。
						cacheList.Add(fname.ToString().ToLower() ,
							new InnerZipFileInfo(lh_pos , compressed_size , uncompressed_size));
					}
					else
					{
						// ファイル名の一致比較は、大文字小文字の違いは無視する。
						//
						// Windows環境ではファイルシステムは、ファイルの大文字小文字の違いは無視するが、
						// いざリリースのときにzipフォルダにまとめたせいでいままで動いていたものが
						// 動かなくなると嫌なので。
						if ( fname.Convert(codePage).Equals(innerFileName , StringComparison.CurrentCultureIgnoreCase) )
						{

							//	一致したでー！これ読み込もうぜー!
							return innerExtract(acc , lh_pos , bRead , compressed_size , uncompressed_size , out buff);

						}
					}

					//	さーて、来週のサザエさんは..
					c_pos += 46 + filename_length
						+ extra_field_length + file_comment_length;
				}
			}
			finally
			{
				if ( file != null )
					file.Dispose();
			}

			// readCacheが有効なのに、ここまで到達するというのは、
			// archive内のfile listをcacheしていなかったので調べていたに違いない。
			// よって、再帰的に呼び出すことによって解決できる。
			if ( readCache )
				return Read(filename , innerFileName , bRead , out buff);

		Exit:
			;

			buff = null;
			return false;
		}

		/// <summary>
		/// Zipファイル解凍のための内部メソッド
		/// </summary>
		/// <param name="acc"></param>
		/// <param name="lh_pos">lh_pos == local header position</param>
		/// <param name="bRead"></param>
		/// <param name="buff"></param>
		/// <returns></returns>
		private bool innerExtract(StreamRandAccessor acc , uint lh_pos,bool bRead,
			uint compressed_size,
			uint uncompressed_size,
			out byte[] buff)
		{
			buff = null;

			//	読み込み指定されてなければ
			//	ファイルが存在することを意味するtrueを返す
			if ( !bRead )
				return true;

			if ( acc.GetUint(lh_pos) != 0x04034b50 )
			{
				//	なんで？ヘッダのシグネチャー違うやん．．
				return false;
			}

			ushort lh_flag = acc.GetUshort(lh_pos + 6);
			ushort lh_compression_method = acc.GetUshort(lh_pos + 8);
			ushort lh_filename_length = acc.GetUshort(lh_pos + 26);
			ushort lh_extra_field = acc.GetUshort(lh_pos + 28);
			long startpos = lh_pos + 30
				+ lh_filename_length + lh_extra_field;
			//	データの読み込み
			//	データの読み込み

			bool encry = ( lh_flag & 1 ) != 0;
			byte[] crypt_header = null;
			if ( encry )
			{
				crypt_header = new byte[12];
				if ( acc.Read(crypt_header , startpos , 12) != 12 )
					return false;
				compressed_size -= 12;
				startpos += 12;
			}
			byte[] read_buffer = new byte[compressed_size];
			long readsize =
				acc.Read(read_buffer , startpos , compressed_size);
			//	これ0ってことはあるのか..? まあ空のファイルかも知れないんで考慮する必要はないか。
			if ( readsize != compressed_size )
				return false;
			// 読み込みエラー

			//printf("lh_compression_method : %d",lh_compression_method);

			if ( encry )
			{
				//	暗号化ファイル
				/*
					暗号の解読
				*/
				ZipPassUpdate zipupdate = new ZipPassUpdate();
				zipupdate.initKey();

				foreach ( char c in zippass )
					zipupdate.updateKeys(( byte ) c);

				//	Read the 12-byte encryption header into Buffer
				for ( int i = 0 ; i < 12 ; ++i )
				{
					byte c = ( byte ) ( crypt_header[i] ^ zipupdate.decrypt_byte() );
					zipupdate.updateKeys(c);
					crypt_header[i] = c;
				}

				for ( int i = 0 ; i < compressed_size ; ++i )
				{
					byte c = ( byte ) ( read_buffer[i] ^ zipupdate.decrypt_byte() );
					zipupdate.updateKeys(c);
					read_buffer[i] = c;
				}
			}

			switch ( lh_compression_method )
			{
				case 0:
					//	無圧縮のようなので、これをそのまま返す
					buff = read_buffer;
					return true;
				case 8:
					byte[] decompressedBuffer;

					// zlibを用いて解凍

					// -15 is a magic value used to decompress zip files.
					// It has the effect of not requiring the 2 byte header
					// and 4 byte trailer.
					//	data = (cast(void[])read_buffer,uncompressed_size,-15);
					#region やねうコメント
					/*
↓以下、D版のyanesdkを作っていたときの掲示板でのやりとりから。
706 ナマエ：やねうらお◆Ze9R3gKs　2004/01/20(火) 07:57 [192.168.1.*] [MSIE6.0/WindowsXP]

zipの暗号化書庫に対応させるの実装してみますた。
でも、うまく解凍されナーです。

くぅ．．。

phobosのzlib.dを見ると
// -15 is a magic value used to decompress zip files.
// It has the effect of not requiring the 2 byte header
// and 4 byte trailer.
とか書いてあって、uncompressの第3パラメータに-15を渡してる
みたいで．．この-15ってのが何なんやという．．。winbits??なんやこれ．．



--------------------------------------------------------------------------------

707 ナマエ：やねうらお◆Ze9R3gKs　2004/01/20(火) 08:06 [192.168.1.*] [Netscape7.02/WindowsXP]

>>706
このパラメータはそのままzlibのinflateInit2に渡してる。このinflateInit2の仕様はここに書いてある。

http://www.sra.co.jp/people/m-kasahr/zlib/zlib-ja.h

引数 windowBits は、ウィンドウの大きさ (履歴バッファの大きさ) を 2
を底とする対数で指定する。このバージョンの zlib ライブラリでは、8
～ 15 にすべきである。

だそうな。対数で指定するってどういうこっちゃ。2^nのnを指定するってことかいな？-15っちゅーと、ほぼ0ってことかな？



--------------------------------------------------------------------------------

708 ナマエ：やねうらお◆Ze9R3gKs　2004/01/20(火) 08:19 [192.168.1.*] [Netscape7.02/WindowsXP]

>>707
ああ。意味わかった。暗号化zipの場合は12バイトの暗号化ヘッダがくっついとるから12足したアドレスをzlib.compressに渡せばいいんだな。12バイト足したアドレスって言ってもvoid[]を取るからコピーして渡すか何かしなきゃいけないんだけど、さすがにそんなでかいデータをコピーするわけにはいかないから、事前にうまいことやって読み込んでおく必要がある、と。



--------------------------------------------------------------------------------

709 ナマエ：k.inaba　2004/01/20(火) 08:34 [*.home.ne.jp] [MSIE6.0/WindowsXP]

>>707
暗号化とは直接関係ないですがちなみに、deflateInit2 / inflateInit2 の windowBits は

int　バッファサイズ = 2^abs(windowBits);
bool 圧縮後データブロックの先頭などにCheckSumが入るフラグ = (windowBits >= 0);

てな感じに使われるので、-15だと「32768byteのバッファ + ヘッダ無し」になりまする。


--------------------------------------------------------------------------------

710 ナマエ：やねうらお◆Ze9R3gKs　2004/01/20(火) 08:49 [192.168.1.*] [MSIE6.0/WindowsXP]

>>709
そ、、そうなんですか。納得！

そんなわけで皆様のおかげでパスワード付きのzipファイルを
シームレスに読み込めるようになりました。

この場をお借りしてお礼申し上げます。
ありがとうございますm(_ _)m
						 */
					#endregion

					/*
						decompressedBuffer = new byte[uncompressed_size];
						MemoryStream ms = new MemoryStream();
						ms.Write(read_buffer, 0, (int)compressed_size);
						ms.Position = 12;
						GZipStream zipStream = new GZipStream(ms, CompressionMode.Decompress);
						int readcount = zipStream.Read(decompressedBuffer, 0, (int)uncompressed_size);

						if (readcount != uncompressed_size)
						{
							buff = null; // あかん。なんやこれ。解凍失敗や
							return false;
						}
						*/
					// どうも、.NET2.0の GZipStreamは辞書サイズを指定できないので
					// zipファイルと互換にならないようだ..


					if (useZlib)
					{

						// zlibを使う実装
						// cf.
						//http://www.codeproject.com/managedcpp/mcppzlibwrapper.asp
						bool bError = false;
						decompressedBuffer = new byte[uncompressed_size];
						ZLibWrapper.ZStream stream = new ZLibWrapper.ZStream();
						try
						{
							int resultInflateInit = ZLibWrapper.inflateInit2(ref stream, -15);
							if (resultInflateInit == ZLibWrapper.Z_OK)
							{
								unsafe
								{
									fixed (byte* outBuffer = decompressedBuffer)
									fixed (byte* inBuffer = read_buffer)
									{
										stream.next_in = (IntPtr)inBuffer;
										stream.avail_in = compressed_size;
										stream.next_out = (IntPtr)outBuffer;
										stream.avail_out = uncompressed_size;
										int resultInflate = ZLibWrapper.inflate(ref stream, ZLibWrapper.Z_NO_FLUSH);
										if (resultInflate < 0)
										{
											bError = true;
										}
									}
								}
							}
							else
							{
								bError = true;
							}
						}
						catch
						{
							bError = true;
						}
						finally
						{
							//	後始末する
							int resultInflateEnd = ZLibWrapper.inflateEnd(ref stream);
							if (resultInflateEnd != ZLibWrapper.Z_OK)
							{
								bError = true;
							}
						}
						if ( bError )
						{
							//	展開エラー
							buff = null;
							return false;
						}
						//	解凍いけたいけた
						buff = decompressedBuffer;
						return true;

					}
					else
					{
						// zlibを用いずに自前で展開する。
						decompressedBuffer = new byte[uncompressed_size];
						MemoryStream ms = new MemoryStream();
						ms.Write(read_buffer, 0, (int)compressed_size);
						ms.Position = 0;
						DeflateStream zipStream = new DeflateStream(ms, CompressionMode.Decompress);
						int readcount = zipStream.Read(decompressedBuffer, 0, (int)uncompressed_size);
						if (readcount != uncompressed_size)
						{
							buff = null; // あかん。なんやこれ。解凍失敗や
							return false;
						}
						//	解凍いけたいけた
						buff = decompressedBuffer;
						return true;
					}

				default:
					//	対応してないzip圧縮
					buff = null;
					return false;
			}
		}

		/// <summary>
		/// zip書き込みは未実装。
		/// System.IO.Compress.GZipStreamを使うといい(かな？)
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public override YanesdkResult Write(string filename , byte[] data)
		{
			return YanesdkResult.NotImplemented;
		}

		/// <summary>
		/// enumerator
		/// </summary>
		/// <remarks>
		/// このメソッドが呼び出されたときに設定されていた
		/// CodePageが、そのままDirEnumeratorに反映されるので注意すること。
		/// (これによって、zipファイルの中の文字列のcodesetが決定する)
		/// defaultではshift-jis。
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns></returns>
		public override DirEnumerator GetEnumerator(string filename)
		{
			ZipDirEnumerator ze = new ZipDirEnumerator(null , filename);
			ze.CodePage = this.codePage;
			return ze;
		}

		/// <summary>
		/// Zipファイル内に格納されているファイルを列挙するためのもの。
		/// </summary>
		public class ZipDirEnumerator : DirEnumerator
		{
			public ZipDirEnumerator(string dirname_ , string filename_)
			{
				SetDir(dirname_);
				filename = filename_;
				this.reader = new FileReaderFile();
			}
			public ZipDirEnumerator(string dirname_ , string filename_ , IFileReader reader)
			{
				SetDir(dirname_);
				filename = filename_;
				this.reader = reader;
			}
			private IFileReader reader;

			public override IEnumerator GetEnumerator()
			{
				//	ZipStream zip = new GZipStream(filename);
				//	foreach (ZipEntry e in zip) {
				//		yield return e.Name;
				//	}

				Stream file = null;

				if ( !reader.IsExist(filename) )
					goto Exit;

				file = reader.Read(filename);
				if ( file == null )
					goto Exit; // おそらくは存在しないか二重openか。
				
				StreamRandAccessor acc = new StreamRandAccessor(file);

				// Find 'end record index' by searching backwards for signature
				long stoppos;
				//	ulongって引き算とか比較が面倒くさいですなぁ．．（´Д｀）
				if ( file.Length < 66000 )
				{
					stoppos = 0;
				}
				else
				{
					stoppos = acc.Length - 66000;
				}
				long endrecOffset = 0;
				for ( long i = acc.Length - 22 ; i >= stoppos ; --i )
				{
					if ( acc.GetUint(i) == 0x06054b50 )
					{ // "PK\0x05\0x06"
						ushort endcommentlength = acc.GetUshort(i + 20);
						if ( i + 22 + endcommentlength != acc.Length )
							continue;
						endrecOffset = i;
					}
					goto endrecFound;
				}
				// ダメジャン
				goto Exit;

			endrecFound:
				;
				//	---- endrecOffsetが求まったナリよ！

				ushort filenum = acc.GetUshort(endrecOffset + 10);
				//	zipに格納されているファイルの数(分割zipは非対応)

				long c_pos = acc.GetUint(endrecOffset + 16);
				//	central directoryの位置

				//	printf("filenum %d",filenum);

				//	---- central directoryが求まったなりよ！
				while ( filenum-- > 0 )
				{
					if ( acc.GetUint(c_pos) != 0x02014b50 )
					{ // シグネチャー確認!
						goto Exit; //  return false; // おかしいで、このファイル
					}
					uint compressed_size = acc.GetUint(c_pos + 20);
					uint uncompressed_size = acc.GetUint(c_pos + 24);
					ushort filename_length = acc.GetUshort(c_pos + 28);
					ushort extra_field_length = acc.GetUshort(c_pos + 30);
					ushort file_comment_length = acc.GetUshort(c_pos + 32);

					//printf("filenamelength : %d",filename_length);
					// local_header_pos
					uint lh_pos = acc.GetUint(c_pos + 42);
					//	ファイル名の取得

					StringBuilderEncoding fname = new StringBuilderEncoding(filename_length);
					for ( int i = 0 ; i < filename_length ; ++i )
					{
						fname.Add(acc.GetByte(i + c_pos + 46));
					}

					//			printf("%.*name\n",fname);
					//	ファイル名が得られた。

					string fullfilename = dirname + fname.Convert(codePage);
					yield return fullfilename;

					//	さーて、来週のサザエさんは..
					c_pos += 46 + filename_length
						+ extra_field_length + file_comment_length;
				}

			Exit:
				;

				if ( file != null )
					file.Dispose();
			}

			private string filename;

			/// <summary>
			/// このクラスで使うコードページを指定する。
			/// (これによって、zipファイルの中の文字列のcodesetが決定する)
			/// 一度設定すると再度設定するまで有効。
			/// ディフォルトでは、Shift_JIS。
			/// </summary>
			public global::System.Text.Encoding CodePage
			{
				get { return codePage; }
				set { codePage = value; }
			}
			private global::System.Text.Encoding codePage = Encoding.GetEncoding("Shift_JIS");
		}

		/// <summary>
		/// このクラスで使うコードページを指定する。
		/// 一度設定すると再度設定するまで有効。
		/// ディフォルトでは、Shift_JIS。
		/// </summary>
		public global::System.Text.Encoding CodePage
		{
			get { return codePage; }
			set { codePage = value; }
		}
		private global::System.Text.Encoding codePage = Encoding.GetEncoding("Shift_JIS");

		/// <summary>
		/// zip書庫の共通パスワードを設定/取得する(FileSysをnewする前に設定しておくこと)
		/// staticなプロパティ。
		/// </summary>
		/// <param name="pass"></param>
		public static string PassStatic
		{
			set { s_zippass = value; }
			get { return s_zippass; }
		}

		/// <summary>
		/// zip書庫のパスワードを設定/取得する
		/// </summary>
		/// <param name="pass"></param>
		public string Pass
		{
			set { zippass = value; }
			get { return zippass; }
		}

		/// <summary>
		/// 全Zip共通のdefault pass
		/// </summary>
		private static string s_zippass;

		/// <summary>
		/// 今回のarchiverだけのpass
		/// </summary>
		private string zippass;

		/// <summary>
		/// ファイルが存在しないときルートまで駆け上がって調べていく必要があるので
		/// 駆け上がるための処理。
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="bRead"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		private bool InnerRead(string filename , bool bRead , out byte[] data)
		{
			string dirname = String.Empty;
			string oldDirname = String.Empty;
			string innerFilename = String.Empty;

			data = null;
			while ( true )
			{
				dirname = FileSys.GetDirName(filename);
				if ( dirname.Length == 0 || dirname.Length >= filename.Length )
					return false;
				// ↑これ以上、駆け上がれないのか

				dirname = dirname.Substring(0 , dirname.Length - 1);
				oldDirname = dirname;

				string purename = FileSys.GetPureFileName(filename);
				if ( purename == null )
					return false; // これ以上かけのぼれない
				string inFile = purename + innerFilename;
				bool b = Read(dirname + zipExtName , inFile , bRead , out data);
				if ( b )
					return true;
				innerFilename = "/" + inFile;
				//	zipのファイル内で使用されているフォルダのセパレータは'/'
				filename = oldDirname;
			}
		}

		/// <summary>
		/// zipファイルの拡張子を指定する。
		/// .datという拡張子のファイル(中身はzip)をzipファイルとして
		/// 扱いたいときは、ここで変更すると良い。
		/// 
		/// defaultでは ".zip"
		/// 
		/// ZipExtName = ""として、拡張子なしのzipファイルをフォルダに
		/// 見せかけることも可能。
		/// </summary>
		public string ZipExtName
		{
			get { return zipExtName; }
			set { zipExtName = value; }
		}
		private string zipExtName = ".zip";
	}

	/// <summary>
	/// 読み込みの基底クラス
	/// </summary>
	public interface IFileReader
	{
		/// <summary>
		/// ファイルの存在チェックメソッド
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		bool IsExist(string filename);

		/// <summary>
		///  ファイルの読み込みメソッド
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		Stream Read(string filename);
	}

	/// <summary>
	/// ファイルからファイルを読み込むクラス
	/// </summary>
	public class FileReaderFile : IFileReader
	{
		/// <summary>
		/// readerは、このクラスのReadメソッドで読み込めなかったときの救済用のFileReader
		/// 
		/// (俗に言うdecoratorパターンになっている)
		/// </summary>
		/// <param name="reader"></param>
		public FileReaderFile(IFileReader reader)
		{
			this.reader = reader;
		}

		public FileReaderFile()
		{
			this.reader = null;
		}

		public bool IsExist(string filename)
		{
			if ( FileSys.IsRealExist(filename) != null )
				return true;

			if ( reader != null )
				return reader.IsExist(filename);

			return false;
		}

		public Stream Read(string filename)
		{
			Stream stream;
			try
			{
				using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
					stream = File.OpenRead(filename);
			}
			catch
			{
				if ( reader != null )
				{
					stream = reader.Read(filename);
					if ( stream != null )
						return stream;
				}
				return null;
			}
			return stream;
		}
		private IFileReader reader;
	}

	/// <summary>
	/// Resourceから暗黙的にファイルを読み込むクラス
	/// </summary>
	public class FileReaderResource : IFileReader {

		/// <summary>
		/// Resourceから暗黙的にファイルを読み込む
		/// 
		/// readerは、このクラスのReadメソッドで読み込めなかったときの救済用のFileReader
		/// (俗に言うdecoratorパターンになっている)
		/// 
		/// </summary>
		/// <remarks>
		/// readerは、このクラスのReadメソッドで読み込めなかったときの救済用のFileReader
		///
		/// リソースを追加するときは、以下の手順で行なうこと。
		/// 
		/// ソリューションエクスプローラで右ポップアップだして
		/// 既存の項目の追加そこから すべてのファイルを表示に切り替え、
		/// ファイル選択してビルドアクションを変更。
		/// </remarks>
		/// <param name="?"></param>
		/**<example>
		// リソースからzipファイル内のファイルの読み込みをするコード
		{
				global::System.Reflection.Assembly asm;
				asm = global::System.Reflection.Assembly.GetExecutingAssembly();

				FileArchiverZip zipArc = new FileArchiverZip(new FileReaderResource
					(asm , "Application1." , ( new FileReaderFile() )));
				zipArc.ZipExtName = "";
				zipArc.Pass = "secret";
				FileSys.AddArchiver(zipArc);
		}
		 * </example>
		*/
		/// <param name="reader"></param>
		public FileReaderResource(global::System.Reflection.Assembly asm , string myNamespace , IFileReader reader)
		{
			this.asm = asm;
			this.myNamespace = myNamespace;
			this.reader = reader;
		}

		public FileReaderResource(global::System.Reflection.Assembly asm ,string myNamespace)
		{
			this.asm = asm;
			this.myNamespace = myNamespace;
			this.reader = null;
		}

		public bool IsExist(string filename)
		{
			Stream stream;
			try
			{
				stream = asm.GetManifestResourceStream(myNamespace + filename);
				// 丸読みしたくないのだが、GetStreamでは、リソースに埋め込まれたファイルを
				// 取得できない。

			}
			catch
			{
				stream = null;
			}
			if ( stream != null )
			{
				stream.Dispose();
				return true;
			}

			if ( reader != null )
				return reader.IsExist(filename);

			return false;
		}
	
		public Stream Read(string filename)
		{
			Stream stream;
			try
			{
				stream = asm.GetManifestResourceStream(myNamespace + filename);
				// これcacheする必要がある
			}
			catch
			{
				if ( reader != null )
				{
					stream = reader.Read(filename);
					if ( stream != null )
						return stream;
				}
				return null;
			}
			return stream;
		}

		// リソースからも暗黙的に読み込む。		
		private global::System.Reflection.Assembly asm;
		private string myNamespace;
		private IFileReader reader;
	}

	/// <summary>
	/// FileReaderBaseをコンストラクタを食わせて、
	/// FileArchiverのように振舞うクラス
	/// </summary>
	public class FileArchiverDefault : FileArchiverBase
	{
		public FileArchiverDefault(IFileReader reader)
		{
			this.reader = reader;
		}

		private IFileReader reader;

		/// <summary>
		///	存在するかを問い合わせる関数
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public override bool IsExist(string filename)
		{
			return reader.IsExist(filename);
		}

		/// <summary>
		/// 存在するか調べて読み込む。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		/// <remarks>
		/// 存在しないときは、nullが戻る
		/// 
		/// path等は無視。file名には '/'は用いておらず、'\'を用いてあり
		/// (linux環境では'/')、また、".."や"."は付帯していないと仮定して良い。
		/// </remarks>
		public override byte[] Read(string filename)
		{
			byte[] rdata = null;
			try
			{
				using ( Stream fs = reader.Read(filename) )
				{
					rdata = new byte[fs.Length];
					fs.Read(rdata , 0 , rdata.Length);
				}
			}
			catch
			{
				return null;
			}
			return rdata;
		}

		/// <summary>
		/// ファイルを書き込む。
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		/// <remarks>
		/// このクラスのreadで読み出せるように書き込みを実装する
		/// (実装しなくとも良い)
		/// 
		/// 書き出しに成功すれば0,失敗すれば非0が返るようにする。
		/// </remarks>
		public override YanesdkResult Write(string filename , byte[] data)
		{ return YanesdkResult.NotImplemented; }

		/// <summary>
		/// ファイルのenumeratorを返す。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// 自分のサポートする書庫ファイルの場合は、そのenumeratorを返す。
		/// </remarks>
		public override DirEnumerator GetEnumerator(string filename) { return null; }
	}

}
