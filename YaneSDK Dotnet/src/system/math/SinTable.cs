using Yanesdk.Ytl;

namespace Yanesdk.Math
{
	/// <summary>
	/// SinTable
	/// </summary>
	public class SinTable {
		/// <summary>
		/// cosを求める。n:0-511で一周。結果は 16回左シフトされたものが返る。
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public int	Cos(int n) { return m_alCosTable[n & 511]; }

		/// <summary>
		/// sinを求める。n:0-511で一周。結果は&lt;&lt;16されて返る。
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public int	Sin(int n) { return m_alCosTable[(n+384) & 511]; }

		/// <summary>
		/// cos(n)*r の整数部を返す。n:0-511で一周。(小数部丸めあり)。
		/// </summary>
		/// <param name="n"></param>
		/// <param name="r"></param>
		/// <returns></returns>
		public int	Cos(int n,int r) { 
			int	l = (int)(Round.RShift((long)Cos(n) * r,16));
			return l;
		}

		/// <summary>
		/// sin(n)*r の整数部を返す。n:0-511で一周。(小数部丸めあり)。
		/// </summary>
		/// <param name="n"></param>
		/// <param name="r"></param>
		/// <returns></returns>
		public int	Sin(int n,int r) { 
			int	l = (int)(Round.RShift((long)Sin(n) * r,16));
			return l;
		}

		/// <summary>
		/// cos(n)を浮動小数で返す。n:0-511で一周。
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public double CosDouble(int n) { return Cos(n)/(double)65536.0; }

		/// <summary>
		/// sin(n)を浮動小数で返す。n:0-511で一周。
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public double SinDouble(int n) { return Sin(n)/(double)65536.0; }

		/// <summary>
		/// 0.5+cos(n)*0.5を浮動小数で返す。n:0-511で一周。
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		/// <remarks>
		/// 1からはじまり0との間をいったりきたりするのに便利。
		/// </remarks>
		public double Cos1_0(int n) { return Cos(n)/(double)65536.0/2 + 0.5; }

		/// <summary>
		/// 1.0-cos1_0を返す関数。
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		/// <remarks>
		/// 0からはじまり1との間をいったりきたりするのに使うと便利。
		/// </remarks>
		public double Cos0_1(int n) { return 1.0-Cos1_0(n); }

		/// <summary>
		/// arctanを求める。C言語のatan2と同じ。結果は0-65535(1周が65536)。
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		/// <remarks>
		/// 高速なArcTanを提供。C言語のatan2と同じ。
		/// 結果は0-65535(1周が65536)
		///
		/// (x,y)が(0,0)のときは YanesdkException が throwされる
		///	throwされて嫌なら、事前にチェックしる！
		/// </remarks>
		public int Atan(int x,int y) {
			if (x==0 && y==0)
				throw new YanesdkException(this, "getで引数が(0,0)");
			if (y < 0) return (ushort)(atan0(-x,-y) + 0x8000);
			return atan0(x,y);
		}

		/// <summary>
		/// </summary>
		public SinTable(){
			//		const double PI = 3.1415926535897932384626433832795;
			for(int i=0;i<512;++i){
				m_alCosTable[i] = (int)((global::System.Math.Cos(i * global::System.Math.PI / 256) * 65536));
			}
			for(int i=0;i<256;++i){
				m_alAtanTable[i] = (ushort)(global::System.Math.Atan(((double)i / 256))
					* (65536 / 2) / global::System.Math.PI);
			}
		}

		/// <summary>
		/// singleton object
		/// SinTable.Instance.sin()などとして使うと良い。
		/// </summary>
		/// <returns></returns>
		public static SinTable Instance{
			get { return Singleton<SinTable>.Instance; }
		}

		/// <summary>
		/// cos table.
		/// </summary>
		private int[] m_alCosTable = new int[512];

		/// <summary>
		/// atan table.
		/// </summary>
		private ushort[] m_alAtanTable = new ushort[256];

		/// <summary>
		/// y>=0専用のAtan。
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private ushort	atan0(int x,int y) {
			if (x<0) return (ushort)(atan1(y,-x)+0x4000);
			return atan1(x,y);
		}

		/// <summary>
		/// x>=0,y>=0専用のAtan。
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private ushort	atan1(int x,int y) {
			if (x==y) return 0x2000;
			if (y>x) return (ushort)(0x4000-atan2(y,x));
			return atan2(x,y);
		}

		/// <summary>
		/// x>y , x>=0 , y>=0専用のAtan。
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		private ushort	atan2(int x,int y) {
			if (x==0) return 0;
			ushort r = (ushort)((((long)(y))<<8)/x);
				return m_alAtanTable[r];
			//	x>yより両辺をxで割って0<y/x<1。よって0<=(y<<8)/x<256
		}
	}
}
