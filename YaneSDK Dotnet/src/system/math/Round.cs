using System;

namespace Yanesdk.Math
{
	/// <summary>
	/// Round の概要の説明です。
	/// </summary>
	public abstract class Round
	{
		/// <summary>
		/// 丸め(四捨五入)つきの右シフト。tをnRShiftCountだけ右シフトして返す。
		/// </summary>
		/// <param name="t"></param>
		/// <param name="nRShiftCount"></param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// 正と負の対称性を考えると、負数に関しては四捨五入の“入”
		/// の場合は 0から遠い方の整数に行って欲しい気がします。
		/// 負数の四捨五入について議論されるのをほとんど見たことが
		/// 無いのですが、そのほうが自然でしょう。ここで四捨五入
		/// と言うとき、この定義に基づくものとします。また、
		/// この定義に基づくと、-3.5は四捨五入されて-4になるべきで
		/// しょうし、-3.4ならば四捨五入の“捨”で、-3になるべきです。
		/// </para>
		/// <para>
		/// 逆にこうならなければ、sin,cosで円を描くときに非対称(3.5は
		/// 4になるのに-3.5は-4になる)ので歪んだものになる可能性があります。
		/// </para>
		/// </remarks>
		
		public static int RShift(int t,int nRShiftCount) {
			int r;
			r = t + (nRShiftCount<=0?0:((int)1) << (nRShiftCount-1))
				- (t >= 0 ? 0 : 1);
			r >>= nRShiftCount;

			return r;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		/// <param name="nRShiftCount"></param>
		/// <returns></returns>
		public static long RShift(long t,int nRShiftCount) {
			long r;
			r = t + (nRShiftCount<=0?0:((long)1) << (nRShiftCount-1)) - (t >= 0 ? 0 : 1);
			r >>= nRShiftCount;

			return r;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		/// <param name="nRShiftCount"></param>
		/// <returns></returns>
		public static short RShift(short t,int nRShiftCount) {
			short r;
			r = (short)(t + (nRShiftCount<=0?0:((short)1) << (nRShiftCount-1)) - (t >= 0 ? 0 : 1));
			r >>= nRShiftCount;

			return r;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		/// <param name="nRShiftCount"></param>
		/// <returns></returns>
		public static byte RShift(byte t,int nRShiftCount) {
			byte r;
			r = (byte)(t + (nRShiftCount<=0?0:((byte)1) << (nRShiftCount-1)) - (t >= 0 ? 0 : 1));
			r >>= nRShiftCount;

			return r;
		}

		/// <summary>
		/// 丸め(四捨五入)つきの割り算。
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		/// <remarks>
		/// r = x/yを返す。xが負数のときも正しく答えが返る。
		/// round.Div(4/3) = 1 , round.Div(-4,3) = -1
		/// round.Div(3/2) = 2 , round.Div(-3,2) = -2(-1ではないことに注意)
		/// </remarks>
		public static int Div(int x,int y) {
			int r;
			if (x>=0)
				r = ( x + (y>>1) ) / y;
			else
				r = ( x - (y>>1) ) / y;
			return r;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static long Div(long x,long y) {
			long r;
			if (x>=0)
				r = ( x + (y>>1) ) / y;
			else
				r = ( x - (y>>1) ) / y;
			return r;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static short Div(short x,short y) {
			short r;
			if (x>=0)
				r = (short)(( x + (y>>1) ) / y);
			else
				r = (short)(( x - (y>>1) ) / y);
			return r;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static byte Div(byte x,byte y) {
			byte r;
			if (x>=0)
				r = (byte)(( x + (y>>1) ) / y);
			else
				r = (byte)(( x - (y>>1) ) / y);
			return r;
		}
	}
}
