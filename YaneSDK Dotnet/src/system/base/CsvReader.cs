using System.Text;
using System.IO;
using System.Collections.Generic;

using Yanesdk.Ytl;

namespace Yanesdk.System
{
	/// <summary>
	/// CSVファイルを読み込むためのクラス
	/// </summary>
	/// <remarks>
	/// 読み込むファイルは0x0A,0x0Dを改行コードとする、ansi or BOM(0xff,0xfe)
	/// 付きのUnicode(UTF-16)のテキストを想定している。
	/// 
	/// \t(TAB)は無視する。
	/// スペースは、項目の先頭と末尾のスペースのみ無視。
	/// 
	/// 例)
	///   AB CD , 123 45 , XYZ   // こんにちは
	/// ならば "AB CD"と"123 45"と"XYZ"
	/// となる。
	/// 
	///   A,,C
	/// ならば "A","","C"になる。
	/// 
	///   ,C
	/// ならば"","C"になる。
	/// 
	///		,
	///
	/// は、"",""とみなす。
	/// 
	///		(空行)
	/// は、ノーカウント。
	/// 
	/// </remarks>
	public class CSVReader
	{
		#region テスト用のコード
			/*
				CSVReader csv = new CSVReader();
				csv.Read("test.csv");
				csv.DebugOutput();

		 [test.csv]
				AA , BB
			 AB  ,CD	   // ここからコメント なーんてな ,,
			AB,	CD	,,EF
			AB,		,,EF
			AB,		,,
		 */
		#endregion

		private bool BOMFound = false;

		/// <summary>
		/// CSVファイルからデータを読み込む
		/// </summary>
		///	<example>
		/// CSVReader reader = new CSVReader();
		///	reader.read("csv.txt");
		///	reader.DebugOutput();
		/// </example>
		/// <param name="filename"></param>
		/// <returns></returns>
		public YanesdkResult Read(string filename)
		{
			csvdata.Clear();

			byte[] data = FileSys.Read(filename);
			if (data == null)
				return YanesdkResult.FileReadError;

			BOMFound = (data.Length >= 2 &&
				data[0] == 0xff &&
				data[1] == 0xfe) ;

			// このdataを解析していく。
			StringBuilderEncoding line = new StringBuilderEncoding();

			if (BOMFound)
			{
				for (int i = 2; i < data.Length;)
				{
					byte b1 = data[i++];
					if (!(i < data.Length)) break;
					byte b2 = data[i++];
					if (b2 == 0 && (b1 == 0x0a || b1 == 0x0d))
					{
						// 一行読めたのでlineを解析
						Parse(line, BOMFound);
						line.Clear();
					}
					else
					{
						line.Add(b1);
						line.Add(b2);
					}
				}
			}
			else
			{
				foreach(byte c in data)
				{
					if (c == 0x0a || c == 0x0d)
					{
						// 一行読めたのでlineを解析
						Parse(line,BOMFound);
						line.Clear();
					}
					else
					{
						line.Add(c);
					}
				}
			}
			// 最後に改行なしのlineがある可能性アリ
			if (line.Count != 0) Parse(line, BOMFound);

			return YanesdkResult.NoError;
		}

		/// <summary>
		/// このクラスで使うコードページを指定する。
		/// 一度設定すると再度設定するまで有効。
		/// ディフォルトでは、Shift_JIS。
		/// BOM付きのutf-16でも読み込める。
		/// </summary>
		public global::System.Text.Encoding CodePage
		{
			get { return codePage; }
			set { codePage = value; }
		}
		private global::System.Text.Encoding codePage = Encoding.GetEncoding("Shift_JIS");

		/// <summary>
		/// 一行解析する
		/// </summary>
		/// <remarks>
		/// 行の解析のときに
		///		1,2,3 // コメント
		/// 　　　　　↑このような書き方をされる可能性がある。これを正しく解析するのは
		///		　　　結構面倒だけど対応させることにする。
		/// </remarks>
		/// <param name="line"></param>
		private void Parse(StringBuilderEncoding bytes, bool BOMFound)
		{
			string line;
			if (BOMFound)
			{
				line = bytes.Convert(Encoding.GetEncoding("utf-16"));
			}
			else
			{
				line = bytes.Convert(codePage);
			}

			List<string> list = new List<string>();
			StringBuilder atom = new StringBuilder();

			// end of line判定フラグ
			bool eol = false;

			// 空行ではないのか
			bool valid = false;

			char c = default(char);
					// ↑warningが出ると嫌なので(´ω`)
			for (int i = 0; i < line.Length+1; ++i)
			{
				if (i<line.Length)
					c = line[i];
				else
					eol = true;

				//  "//"で始まるのか調べるために、'/'であれば
				// 一文字だけ先読みさせてもらう。
				if (c == '/' && i < line.Length - 1 && line[i + 1] == '/')
				{
					// ここ以降はコメントなので、カンマに遭遇したとすれば良い。
					eol = true;
				}

				if (!eol)
				{
					// 項目先頭のspaceは無視
					if (atom.Length == 0 && c == ' ')
						continue;

					// 項目途中でも TAB は常にら無視
					if (c == '\t')
						continue;
				}

				bool comma = c == ',';

				// 一つでもカンマがあればそれは意味のある行だとみなす
				if (comma)
					valid = true;

				if (comma || eol)
				{
					//↓カンマ区切りで ""を設定することがあるのでこのチェック不要。
				//	if (atom.Length!=0)

					// 末尾のspaceは削除。
					while (0 < atom.Length
						&& atom[atom.Length - 1]==' ')
						-- atom.Length;

					list.Add(atom.ToString());

					// ひとつでも有効な要素があれば有効な行である
					if (atom.Length != 0)
						valid = true;

					atom.Length = 0;

					if (eol)
						break; // forから抜ける

					continue;
				}

				atom.Append(c);
			}

			if (valid)
				csvdata.Add(list);
		}

		/// <summary>
		/// 読み込んだデータを取得する。
		/// </summary>
		/// <remarks>
		/// 読み込みに失敗しても戻り値はnullではないことは保証される。
		/// </remarks>
		public List<List<string>> CsvData { get { return csvdata; } }

		/// <summary>
		/// Debug用に読み込んだデータをConsoleに表示
		/// </summary>
		public void DebugOutput()
		{
			foreach (List<string> line in csvdata)
			{
				bool first = true;
				foreach (string s in line)
				{
					// 先頭以外にはカンマを入れる
					if (!first)
						global::System.Console.Write(',');

					first = false;
					global::System.Console.Write(s);
				}
				global::System.Console.WriteLine();
			}

		}

		private List<List<string>> csvdata = new List<List<string>>();
	}


}

