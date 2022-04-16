using System;
using System.Text;

namespace Yanesdk.System
{
	/// <summary>
	/// 文字列の変換補助用関数群
	/// </summary>
	public abstract class StringConv
	{
		/// <summary>
		///	m桁の16進数文字列に変換する
		///	文字列の前方に桁が余った場合は、0で埋める。
		/// </summary>
		/// <param name="u"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static string ToHex(int u,int m) { return HelperUnsigned(u,16,m,'0'); }

		/// <summary>
		/// 16進数文字列に変換する
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		public static string ToHex(int u) { return HelperUnsigned(u,16,-1,'0'); }

		/// <summary>
		/// m桁の10進数文字列に変換する
		/// 文字列の前方に桁が余った場合は、0で埋める。
		/// </summary>
		/// <param name="u"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static string ToDec(int u,int m) { return HelperSigned(u,10,m,'0'); }

		/// <summary>
		/// m桁の10進数文字列に変換する
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		public static string ToDec(int u) { return HelperSigned(u,10,-1,'0'); }

		/// <summary>
		/// m桁の10進数文字列に変換する
		/// 文字列の前方に桁が余った場合は、' 'で埋める。
		/// </summary>
		/// <param name="u"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static string ToDecZeroSuppress(int u, int m) { return HelperSigned(u, 10, m, ' '); }

		/// <summary>
		///	m桁の8進数文字列に変換する
		/// 文字列の前方に桁が余った場合は、0で埋める。
		/// </summary>
		/// <param name="u"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static string ToOct(int u,int m) { return HelperSigned(u,8,m,'0'); }

		/// <summary>
		///	m桁の8進数文字列に変換する
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		public static string ToOct(int u) { return HelperSigned(u,8,-1,'0'); }

		/// <summary>
		/// m桁の8進数文字列に変換する
		/// 文字列の前方に桁が余った場合は、' 'で埋める。
		/// </summary>
		/// <param name="u"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static string ToOctZeroSuppress(int u, int m) { return HelperSigned(u, 8, m, ' '); }

		/// <summary>
		///	m桁の2進数文字列に変換する
		///	文字列の前方に桁が余った場合は、0で埋める。
		/// </summary>
		/// <param name="u"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static string ToBin(int u,int m) { return HelperSigned(u,2,m,'0'); }

		///	m桁の2進数文字列に変換する
		public static string ToBin(int u) { return HelperSigned(u,2,-1,'0'); }

		/// <summary>
		/// m桁の2進数文字列に変換する
		/// 文字列の前方に桁が余った場合は、' 'で埋める。
		/// </summary>
		/// <param name="u"></param>
		/// <param name="m"></param>
		/// <returns></returns>
		public static string ToBinZeroSuppress(int u, int m) { return HelperSigned(u, 2, m, ' '); }

		/// <summary>
		/// 無符号の数字をm桁n進数文字列に変換する
		/// </summary>
		/// <remarks>
		///		m == -1の場合は、前方のゼロサプレス(0で埋める)をしない。
		///		n の最大は36まで。
		///		桁が余った場合は前方を char cで埋める。
		///		<PRE>
		///		例)
		///		string s = ToConvHelper(123,5); // "00123"が戻る
		///		string s = ToConvHelper(12345,3); // "345"が戻る
		///		</PRE>
		/// </remarks>
		/// <param name="u"></param>
		/// <param name="n"></param>
		/// <param name="m"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static string HelperUnsigned(int u,int n,int m, char c) {
			if (n > 36 || n <= 1) 
				return null; // だめぽ
			const string d = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			char[] s= new char[m > 0 ? m : 16];
			int sp = 0;

			while (m>0 || m ==-1) {
				int v = (int)(u % n);
				s[sp++] = d[v];
				if (m>0) --m;
				u /= n;
				if (u == 0) break; // 桁がなくなった
			}
			if (sp > 0) {
				while (--m>=0)
					s[sp++] = c;
			}
			else
				s[sp++] = '0';
			Array.Reverse(s, 0, sp);
			return new String(s, 0, sp);
		}

		/// <summary>
		/// 符号つきの数字をm桁n進数文字列に変換する
		/// </summary>
		/// <remarks>
		/// m == -1の場合は、前方のゼロサプレス(0で埋める)をしない。
		///	マイナス符号は桁数に含めない。
		/// </remarks>
		/// <param name="u"></param>
		/// <param name="n"></param>
		/// <param name="m"></param>
		/// <param name="c"></param>
		/// <returns></returns>
		public static string HelperSigned(int u,int n,int m, char c) {
			if (u<0) {
				return "-" + HelperUnsigned(-u,n,m,c);
			}
			return HelperUnsigned(u,n,m,c);
		}
	}
}
