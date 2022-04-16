using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Text;

using Yanesdk.Ytl;

namespace Yanesdk.System
{
	/// <summary>
	/// Log出力ヘルパクラス
	/**
	1.
	logを1行ずつ書き出すと、disk上でfragmentationが生じる。これが気持ち悪い。
	そこで、1MBほど事前に確保('\0'を書き込んだファイルを作成)して、そこに
	書いていくことを考える。

	2.
	ファイルは日付別になっていて欲しい。
	20060401_1.logのように。
	↑日付

	ただし、1日のファイルサイズの上限は、1MBとして、それを超えると
	のように自動的に次のファイルが生成されるものとする。
	 例)
		20060401_2.log
		20060401_3.log
		20060401_4.log
			・
			・

	3.
	書き込みは1行ずつ。エラーログ等で用いるのでバッファリングは
	なしで、即座に書き込む。

	4.
	最初に書き込むときに、今日の日付が20060401だとすれば
		20060401_1.log
			・
			・
		20060401_6.log
	というように存在チェックを行ない、一番最後のファイルに追記していく。
	(このとき1MBを超えれば、また新しいファイルに書き出す)

	5.
	ファイルはぴったり1MBに収まるように設計して欲しい。1MBを超えてはならない。
	余ったぶんは、'\0'で埋まっている状態にする。

	6.
	↑で1MBと書いているのは、実際には、propertyで変更できるように
	してある。
	*/
	/// </summary>
	public class LogWriter : IDisposable
	{
		#region publicメソッド
		/// <summary>
		///		コンストラクタ
		/// </summary>
		public LogWriter()
		{
			// 書き込み用のスレッドを開始する。
			streamThread = new Thread(new ThreadStart(WriteThread));
			streamThread.Start();
		}

		/// <summary>書き出すフォルダ位置を指定する</summary>
		/// <example>
		///		SetPath("log");とやれば、log/20060401_1.log に書き出される
		///		(default : "")
		/// </example>
		/// <param name="path">書き出すフォルダ位置</param>
		public YanesdkResult SetPath(string path) 
		{
			try
			{
				if ( path != "" )	// 空文字の場合は特にディレクトリ作成の必要はない。
				{
					if ( !path.EndsWith(Path.DirectorySeparatorChar.ToString()) )
						path += Path.DirectorySeparatorChar; // ファイル名と連結させるので/を足しておく

					Directory.CreateDirectory(path); // ディレクトリの作成
				}
			}
			catch
			{
				path = ""; // ディレクトリが作成できない場合は、デフォルトに戻す。
				return YanesdkResult.HappenSomeError;
			}
			finally
			{
				outPath = path;
			}

			return YanesdkResult.NoError;
		}

		/// <summary>
		///	[async]	1行テキストを出力する
		/// </summary>
		/// <remarks>
		///		実際には即座に書き込みは行われない。
		///		いったんtextbufにためられ、別スレッドからファイルに書き込まれる。
		/// </remarks>
		/// <param name="logtext">出力するテキスト</param>
		public YanesdkResult Write(string logtext)
		{
			try
			{
				textbuf.Enqueue(logtext);	// バッファに投げる。
			}
			catch 
			{
				return YanesdkResult.HappenSomeError;
			}

			return YanesdkResult.NoError;
		}

		/// <summary>
		///		終了処理。
		/// </summary>
		public void Dispose()
		{
			// スレッドの終了
			threadExitRequest = true;
			streamThread.Join();
		}

		#endregion

		#region プロパティ
		/// <summary>
		/// 1ファイルの最大サイズ。default = 1MB = 1024*1024
		/// </summary>
		public long FileMaxSize
		{
			get { return fileMaxSize; }
			set { fileMaxSize = value; }
		}
		private long fileMaxSize = 1024 * 1024; // デフォルトは1MB

		/// <summary>
		///		ファイルのエンコードを指定します。
		/// </summary>
		/// <remarks>
		///		実際のファイルの書き込み(Write)を行う前に指定してください。
		/// </remarks>
		public global::System.Text.Encoding CodePage
		{
			get { return codePage; }
			set { codePage = value; }
		}

		// codepage。デフォルトはshift-jis
		private global::System.Text.Encoding codePage = Encoding.GetEncoding("Shift_JIS");

		#endregion

		#region privateメソッド

		/// <summary>
		///		同じ日付で最後に開いたファイルがあればそれを開く。
		/// </summary>
		/// <remarks>
		///		このメソッドが成功したときstreamは過去のファイルを開き
		///		書き込み位置がファイルの終端まで移動している。
		/// </remarks>
		/// <returns></returns>
		private bool OpenLastFile()
		{
			DateTime lastDate = DateTime.Now;

			// 日付内での番号表記。最後の番号を探す。20060101_1←この番号
			int count = 1;
			while ( File.Exists(GetFileName(lastDate , count)) )
				++count;

			if (count == 1) return false; // 過去に書き込まれたファイルはない。
			--count;

			try
			{
				// 最後に書き込んだファイルを開く。
				stream = new FileStream(GetFileName(lastDate , count), FileMode.Open, FileAccess.ReadWrite );
				stream.Seek(0, SeekOrigin.End); // ゼロはtruncateされているので、streamの最後の位置へ。
				long streamLastPos = stream.Position;

				byte[] temp = new byte[fileMaxSize - stream.Position];
				stream.Write(temp, 0, temp.Length); // 末尾までゼロを埋める。
				stream.Position = streamLastPos; // zerofill前の位置に戻す。

				this.lastDate = lastDate;

				return true;
			}
			catch
			{
				Close();
				return false;
			}
		}


		/// <summary>
		///		書き込みスレッド
		/// </summary>
		private void WriteThread()
		{
			while ( !( threadExitRequest && textbuf.Count == 0) )
			{
				while ( textbuf.Count != 0 )
				{
					string text = textbuf.Dequeue();
					byte[] buf = null;

					ToByte(out buf, text);	// string->byte[]
					if (buf == null)		// 変換失敗
						continue;

					if (isFirst) // 起動時の1回だけ、最後に開いたファイルを読みにいってみる。
					{
						isFirst = false;
						if (!OpenLastFile()) // 現在の日付で、最後に開いたファイルがあればそれを開く
						{
							if (!CreateNewFile())// 新しいファイルを作成する						
							{
								// うーん…新しいファイルが作れないとなると、どうしようもねーんじゃね？
								// throw null;
							}
						}
					}

					// 日付が変更された、次の書き込みでオーバーする場合
					if (stream == null || IsNewDate || OverFileMax(buf.Length))
					{
						if (!CreateNewFile()) // 新しいファイルを作成する
						{
							// うーん…新しいファイルが作れないとなると、どうしようもねーんじゃね？
							// throw null;
						}
					}

					try
					{
						if (stream != null)
						{
							stream.Write(buf, 0, buf.Length);
							stream.Flush();
						}
					}
					catch
					{
						Close();
					}
				}

				Thread.Sleep(100);
			}

			// ストリームを閉じる
			Close();
		}

		/// <summary>
		/// ファイルをcloseする。closeするときにTruncateも行なう。
		/// </summary>
		private void Close()
		{
			if ( stream != null )
			{
				/// <summary>
				///		streamのseek位置からfileMaxSizeまで埋められているゼロを削除する。
				/// </summary>
				/// <remarks>このメソッドに失敗してもstreamはdisposeされたりnullにはならない。</remarks>
				stream.SetLength(stream.Position);	// ストリームのサイズを変更して、末尾を削除してしまう。

				//	↑TruncateFilledZero();
				
				stream.Dispose();
				stream = null;
			}
		}

		private bool isFirst = true; // 初回起動時フラグ

		/// <summary>
		///		char -> byte列変換を行う。
		/// </summary>
		/// <param name="strbyte">出力結果のバイト列</param>
		/// <param name="str">文字列</param>
		private void ToByte( out byte[] strbyte, string str )
		{
			try
			{
				// string -> byte[]へ変換。
				strbyte = codePage.GetBytes(str + "\r\n");
			}
			catch
			{
				// 失敗した場合はnullを設定
				strbyte = null;
			}
		}

		/// <summary>
		///		前回の呼び出しから、日付が変更になっているかどうかチェックする
		/// </summary>
		/// <returns>true:日付が変更されている/false:前回と同じ日付</returns>
		private bool IsNewDate
		{
			get { return DateTime.Now.Date != lastDate.Date; }
		}

		/// <summary>
		///		次のテキストを書き込むとファイルが規定サイズをオーバーするかどうかチェックする。
		/// </summary>
		/// <param name="logText">書き込むテキスト</param>
		/// <returns>true:オーバーする/false:オーバーしない</returns>
		private bool OverFileMax( int length )
		{
			return !( stream.Position + length < fileMaxSize );
		}

		/// <summary>
		///		新しい保存ファイル名を作成する
		/// </summary>
		/// <param name="date">新しいファイル名を作成するときの日付</param>
		/// <returns>YYYYMMDD_Xという形式で新しいファイル名を返す。</returns>
		private string GetNewFileName( DateTime date )
		{
			int count = 1;
			// 日付内での番号表記。次の番号を探す。
			// 20060101_1←この番号
			while ( File.Exists(GetFileName(date, count)) )
				++count;

			return GetFileName(date, count);
		}

		/// <summary>
		/// ログファイル名を生成する。
		/// </summary>
		/// <param name="fileHead"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		private string GetFileName(DateTime date , int count)
		{
			return 
				string.Format("{0}{1}_{2}.txt" ,
					outPath ,
					date.ToString("yyyyMMdd") ,	// 20060101形式で出力
					count.ToString()
				);
		}

		/// <summary>
		///		新しいファイルを作成し、fileMaxSizeの空データを書き込む
		/// </summary>
		/// <remarks>
		///		現在開いているファイルがある場合はcloseされる。
		///		成功するとstreamメンバは新しいファイルに書き込み可能な状態になる。
		///		失敗すると stream は null になる。
		/// </remarks>
		/// <returns>true:成功 , false:失敗</returns>
		private bool CreateNewFile()
		{
			Close();

			try
			{
				DateTime writeFileDate = DateTime.Now;
				string fileName = GetNewFileName(writeFileDate);
				stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

				const int tmpSize = 1024;
				byte[] temp = new byte[tmpSize];

				for ( long i = 0 ; i < fileMaxSize / tmpSize ; ++i )
					stream.Write(temp , 0 , tmpSize);
				int modSize = ( int ) (fileMaxSize % tmpSize);
				if (modSize !=0)
					stream.Write(temp , 0 , modSize);

				Close();

				// 書き込めるようにファイルを開く
				stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
				lastDate = writeFileDate; // 日付を更新しておく。
			}
			catch
			{
				Close();
				return false;
			}

			return true;
		}

		// 書き込みテキストのリスト
		private Queue<string> textbuf = new SynchronizedQueue<string>();

		// 書き込み用のスレッド
		private Thread streamThread = null;

		// 書き込み用スレッドの終了フラグ
		private volatile bool threadExitRequest = false;

		// 出力先のディレクトリ名
		private string outPath;

		//	ファイルを生成するときに使った日付
		private DateTime lastDate;

		//	生成しているログファイル
		private FileStream stream = null;

		#endregion
	}
}
