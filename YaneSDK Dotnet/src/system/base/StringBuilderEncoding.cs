using System;
using System.Collections.Generic;
using System.Text;

using Yanesdk.Ytl;

namespace Yanesdk.System
{
	/// <summary>
	/// 特定の文字スケールにencodeされた文字列をbyteで渡していき、
	/// 最終的にstringが欲しいときに使う。
	/// </summary>
	public class StringBuilderEncoding
	{
		public StringBuilderEncoding() {
			data = new List<byte>();
		}

		/// <summary>
		/// 事前にAddする回数(変換元の文字列長)がわかっている場合は、
		/// こちらのコンストラクタで生成したほうがそのあとAddする
		/// オーバーヘッドが少ない。
		/// </summary>
		/// <param name="length"></param>
		public StringBuilderEncoding(int length)
		{
			data = new List<byte>(length);
		}

		/// <summary>
		/// 結合したデータを変換して文字列として返す
		/// </summary>
		/// <example>
		/// string s = builder.Convert(Encoding.GetEncoding("Shift_JIS"));
		/// </example>
		/// <param name="CodePage"></param>
		/// <returns></returns>
		public string Convert(global::System.Text.Encoding codePage)
		{
			return codePage.GetString(ListHelper<byte>.ConvertToArray(data));
		}

		/// <summary>
		/// 1バイト追加する
		/// </summary>
		/// <param name="c"></param>
		public void Add(byte c)
		{
			data.Add(c);
		}

		/// <summary>
		/// Addで追加していったバッファをクリアする
		/// </summary>
		public void Clear()
		{
			data.Clear();
		}

		public int Count
		{
			get { return data.Count; }
		}

		/// <summary>
		/// 内部的に保持しているバッファをディフォルトのコードスケールとみなして
		/// 変換したものを返す。
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Convert(Encoding.Default);
		}

		/// <summary>
		/// 読み込んだデータ
		/// </summary>
		private List<byte> data;

	}
}
