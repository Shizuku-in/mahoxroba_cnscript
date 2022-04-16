using System;

namespace Yanesdk.Math
{
	/// <summary>
	/// 高速な乱数の発生。
	/// </summary>
	/// <remarks>
	/// Mersenne Twister法による高精度の乱数発生ルーチンです。
	/// 2^19937-1という天文学的な大きさの周期性を持った乱数を高速に生成します。
	/// </remarks>
	public class Rand {

		/// <summary>
		/// 乱数の取得。0～n-1までの乱数を取得。
		/// n==0の場合は0が返る(このほうが使いやすいので)
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		public int GetRand(int n) {			//	0～n-1の乱数の取得
			if (n==0) { 
				return 0;
			}
			return (int)(GetRand() % n);
		}

		/// <summary>
		/// 乱数の取得。32bitの乱数を取得。0～(2^32-1)までの値が返る。
		/// intで受けると負数も返ることになるので注意
		/// </summary>
		public long GetRand()
		{
			long y;
			// static uint mag01[2] = [ 0x0, MATRIX_A ];
			/* mag01[x] = x * MATRIX_A	for x=0,1 */

			if (m_nMti >= N)
			{ /* generate N words at one time */
				int kk;
				if (m_nMti == N + 1)	 /* if sgenrand() has not been called, */
					SetSeed(4357);	 /* a default initial seed is used	 */

				for (kk = 0; kk < N - M; kk++)
				{
					y = (m_dwMt[kk] & UPPER_MASK) | (m_dwMt[kk + 1] & LOWER_MASK);
					m_dwMt[kk] = m_dwMt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1];
				}
				for (; kk < N - 1; kk++)
				{
					y = (m_dwMt[kk] & UPPER_MASK) | (m_dwMt[kk + 1] & LOWER_MASK);
					m_dwMt[kk] = m_dwMt[kk + M - N] ^ (y >> 1) ^ mag01[y & 0x1];
				}
				y = (m_dwMt[N - 1] & UPPER_MASK) | (m_dwMt[0] & LOWER_MASK);
				m_dwMt[N - 1] = m_dwMt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1];
				m_nMti = 0;
			}
			y = m_dwMt[m_nMti++];

			y ^= (y >> 11);
			y ^= (y << 7) & TEMPERING_MASK_B;
			y ^= (y << 15) & TEMPERING_MASK_C;
			y ^= (y >> 18);
			return y; 
		}
	
		/// <summary>
		/// [0,1] 範囲で乱数生成 ←0,1を含むの意味
		/// </summary>
		/// <returns></returns>
		public double GetRand0_1A() {
			return (double)GetRand() * ((double)1.0/4294967295.0);	
			/* divided by 2^32-1 */ 
		}

		/// <summary>
		/// [0,1) 範囲で乱数生成 ←0は含む,1は含まないの意味
		/// </summary>
		/// <returns></returns>
		public double GetRand0_1B()
		{
			return (double)GetRand() * ((double)1.0/4294967296.0); 
			/* divided by 2^32 */
		}

		/// <summary>
		/// (0,1] 範囲で乱数生成 ←0は含まない,1は含むの意味
		/// </summary>
		/// <returns></returns>
		public double GetRand0_1C()
		{
			return ((double)GetRand()+1) * ((double)1.0/4294967296.0); 
			/* divided by 2^32 */
		}

		/// <summary>
		/// (0,1) 範囲で乱数生成 ←0,1は含まないの意味
		/// </summary>
		/// <returns></returns>
		public double GetRand0_1D()
		{
			return ((double)GetRand() + 0.5) * ((double)1.0/4294967296.0);	
			/* divided by 2^32 */
		}

		/// <summary>
		/// 乱数の種の設定。必ず一度呼び出す必要がある。
		/// </summary>
		/// <param name="dwSeed"></param>
		/// <remarks>
		/// 乱数の種を設定します。設定された種に従って、乱数は生成されていきます。
		/// 必ず一度は呼び出す必要があります。
		/// 呼び出さないときは、
		/// <c>SetSeed(4357);</c>
		/// が、一番最初の乱数取得のときに実行されます。
		/// </remarks>
		public void SetSeed(int dwSeed) {
			const int N = 624;
			for (int i=0;i<N;i++) {
				m_dwMt[i] = (int)(dwSeed & 0xffff0000);
				dwSeed = 69069 * dwSeed + 1;
				m_dwMt[i] |= (dwSeed & 0xffff0000) >> 16;
				dwSeed = 69069 * dwSeed + 1;
			}
			m_nMti = N;
		}

		/// <summary>
		/// 乱数の種として、現在時刻を与えます。
		/// </summary>
		/// <remarks>
		/// 要するに、再現性の無い乱数が出来ます。
		/// setSeed(タイマーの値)とやっているので、この関数を呼び出した場合、
		/// setSeedを呼び出す必要はありません。
		/// </remarks>
		public void Randomize() {
			SetSeed((int)DateTime.UtcNow.Ticks);
		}

		/// <summary>
		/// コンストラクタは、２種類あり、パラメータ無しのほうは、
		/// 乱数の初期化を行ないません。
		/// </summary>
		public Rand() { m_nMti = 624+1; } /* means m_dwMt is not initialized */

		/// <summary>
		/// コンストラクタは、２種類あり、パラメータ無しのほうは、
		/// 乱数の初期化を行ないません。パラメータ有りのほうは、
		/// 乱数の種を引数として取り、それに基づいた乱数を生成します。
		/// （内部的にSetSeedを行なうということです。
		/// よって、SetSeedをこのあと行なう必要はありません）
		/// 前回と同じ乱数系列を再現したい場合などにこれを使います。
		/// </summary>
		public Rand(int dwSeed) { SetSeed(dwSeed); }

		/// <summary>
		/// 
		/// </summary>
		private static uint[] mag01 = new uint[] { 0x0, MATRIX_A };

		/// <summary>
		/// 
		/// </summary>
		const int N = 624;

		/// <summary>
		/// 
		/// </summary>
		const int M = 397;

		/// <summary>
		/// the array for the state vector.
		/// </summary>
		long[]	m_dwMt = new long[N];
		/// <summary>
		/// initialization counter.
		/// </summary>
		int		m_nMti;

		/// <summary>
		/// constant vector a.
		/// </summary>
		private const uint MATRIX_A = 0x9908b0df;
		/// <summary>
		/// most significant w-r bits.
		/// </summary>
		private const uint UPPER_MASK = 0x80000000;
		/// <summary>
		/// least significant r bits.
		/// </summary>
		private const uint LOWER_MASK = 0x7fffffff;

		/// <summary>
		/// Tempering parameters.
		/// </summary>
		private const uint TEMPERING_MASK_B = 0x9d2c5680;
		/// <summary>
		/// Tempering parameters.
		/// </summary>
		private const uint TEMPERING_MASK_C = 0xefc60000;
	}
}
