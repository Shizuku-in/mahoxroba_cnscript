
using System.Collections.Generic;

using Yanesdk.Ytl;

namespace Yanesdk.System
{

	/// <summary>
	/// ファイル名の一覧が書いてある定義ファイルを
	/// CSVRoaderを使って読み込む。
	/// </summary>
	public class FilenamelistReader
	{
		/// <summary>
		/// 定義ファイルの読み込み
		/// </summary>
		/// <para>
		///	・定義ファイル仕様
		/// </para>
		/// <para>
		///		file名 , 読み込み番号(load,getで指定するときの番号) , option1, option2
		///			の繰り返し。読み込み番号,option1,2は省略可能。
		///     省略した場合、int.MinValue扱いになる。
		/// </para>
		/// <para>
		///		以下はコメント
		///
		///		読み込み番号が指定された場合、そのあとは連番になる。
		///		例)
		///		<list type="bullet">
		///			<item><description>a.wav,2</description></item>
		///			<item><description>b.wav	 ← このファイルの番号は3になる</description></item>
		///			<item><description>c.wav	 ← このファイルの番号は4になる</description></item>
		///		</list>
		///
		///		ここで指定されているoptionが、どのような意味を持つかは、
		///		このCacheLoaderクラスを使用するクラス(派生クラス？)に依存する。
		/// </para>
		/// <para>
		///		その他、細かい解析ルールは、 CSVReader クラスの内容に依存する。
		///	</para>
		/// 
		/// optNum : optionの書いてある限度位置(列)を指定する。
		/// 上の例では、「a.wav , 2」なので、optNum = 0(なし)と考えることが出来る。
		/// 「a,wav , opt1, opt2 , 2」のようにopt1,opt2を指定するならば、
		/// optNum = 2と考えることが出来る。
		/// 「a,wav , opt1, 2 , opt2」のようにopt1,opt2を指定するならば、
		/// optNum = 1と考えることが出来る。
		/// 
		/// </remarks>
		/// <param name="filename"></param>
		public YanesdkResult LoadDefFile(string filename,int optNum)
		{
			Release();

			YanesdkResult result = reader.Read(filename);
			if (result != YanesdkResult.NoError)
				return result;

			// 読み込んだCSVから、ファイル名と番号とを対応させ、
			// LinkedListに格納していく。

			int line = 0;
			List<List<string>> list = reader.CsvData;
			foreach (List<string> a in list)
			{
				int opt1 = int.MinValue , opt2 = int.MinValue;
				for(int i=0,j=0;i<3;++i){
					int opt=int.MinValue;

					if ( a.Count < i + 2 )
						break;

					int n;
					if ( int.TryParse(a[i + 1] , out n) )
					{
						// 成功した
						if ( i == optNum )
						{
							line = n;
							continue;
						}
						else
						{
							opt = n;
						}
					}
					// opt[j] = n;
					switch ( j )
					{
						case 0:
							opt1 = opt;
							break;
						case 1:
							opt2 = opt;
							break;
					}
					++j;
				}

				try
				{
					dic.Add(line, new Info(a[0], opt1, opt2));
				}
				catch
				{
					// おそらくは、同一の値で定義されている行によるエラーなのだが..
					// この行スキップしたろか
#if DEBUG
					global::System.Console.WriteLine("FilenamelistReader.loadDefFileでエラー行検出。");
#endif
				}

				++line;
			}
			
			return YanesdkResult.NoError;
		}

		/// <summary>
		/// 解放処理。読み込んでいた定義ファイルの内容をリセットする。
		/// </summary>
		public void Release()
		{
			dic.Clear();
		}

		/// <summary>
		/// 定義ファイルから読み込んだデータのなかから、
		/// この番号に対応するファイル名を返す
		/// 
		/// 例)定義ファイルが
		///		a.wav
		///		b.ogg
		///		c.bmp
		/// と書いてあって、これをreadDefFileで読み込んだあとならば、
		/// get(0)とすれば"a.wav"が返る。
		/// </summary>
		/// <remarks>
		/// 定義ファイル上に該当行が見つからない場合は、getはnullを返す。
		/// </remarks>
		/// <param name="key"></param>
		/// <returns></returns>
		public string GetName(int key)
		{
			if (!dic.ContainsKey(key))
				return null;
			return dic[key].name;
		}

		/// <summary>
		/// getNameではstringしか戻ってこない。
		/// nameだけでなく、opt1,opt2も取得したいときには
		/// こちらのメソッドを利用する。
		/// </summary>
		/// <remarks>
		/// opt1,opt2が設定ファイル上で省略されていた場合には、
		/// int.MinValueが返る。
		/// </remarks>
		/// <param name="key"></param>
		/// <returns></returns>
		public Info GetInfo(int key)
		{
			if (!dic.ContainsKey(key))
				return null;
			return dic[key];
		}

		/// <summary>
		/// 定義ファイルに書いてあった1行ぶんのデータ
		/// </summary>
		public class Info
		{
			public string name;
			public int opt1, opt2;
			public Info(string name, int opt1, int opt2)
			{
				this.name = name;
				this.opt1 = opt1;
				this.opt2 = opt2;
			}
		}

		/// <summary>
		/// 読み込んだデータ
		/// </summary>
		/// <remarks>
		/// int  : key
		/// opt1,opt2
		/// </remarks>
		public Dictionary<int, Info> Data
		{
			get { return dic; }
		}
		private Dictionary<int,Info> dic = new Dictionary<int,Info>();

		/// <summary>
		/// このクラスで使うコードページを指定する。
		/// 一度設定すると再度設定するまで有効。
		/// ディフォルトでは、Shift_JIS。
		/// BOM付きのutf-16でも読み込める。
		/// </summary>
		public global::System.Text.Encoding CodePage
		{
			get { return reader.CodePage; }
			set { reader.CodePage = value; }
		}
		private CSVReader reader = new CSVReader();
	}

}