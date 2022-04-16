using System;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 色を表す構造体
	/// このクラス名の末尾のubはunsigned byteの略。
	///	OpenGLのほうでこのように名前をつける習慣がある)
	/// </summary>
	public struct Color4ub {
		/// <summary>
		/// 色情報
		/// </summary>
		/// <remarks>
		/// r,g,bは赤,緑,青の光の三原色。0～255までの値で。
		/// aはαチャンネル。0ならば完全な透明(配置したときに背景が透けて見える)
		/// 255なら不透明。0～255までの値で。
		/// </remarks>
		public byte R,G,B,A; 

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="r_"></param>
		/// <param name="g_"></param>
		/// <param name="b_"></param>
		/// <param name="a_"></param>
		public Color4ub(byte r_, byte g_, byte b_, byte a_) {
			R = r_;
			G = g_;
			B = b_;
			A = a_;
		}

		/// <summary>
		/// 色のリセット。r=g=b=a=255に。
		/// </summary>
		public void ResetColor() { R=G=B=A=255;}

		/// <summary>
		/// 色のセット r,g,bは0～255。aは自動的に255になる。
		/// </summary>
		/// <param name="r_"></param>
		/// <param name="g_"></param>
		/// <param name="b_"></param>
		public void SetColor(int r_,int g_,int b_) {
			R = (byte)r_; G = (byte)g_; B = (byte)b_; A = 255;
		}

		/// <summary>
		/// 色のセット r,g,b,aは0～255
		/// </summary>
		/// <param name="r_"></param>
		/// <param name="g_"></param>
		/// <param name="b_"></param>
		/// <param name="a_"></param>
		public void SetColor(int r_,int g_,int b_,int a_) {
			R = (byte)r_; G = (byte)g_; B = (byte)b_; A = (byte)a_;
		}

		/// <summary>
		/// 色の掛け算
		/// </summary>
		/// <remarks>
		/// r,g,b,aに関して、
		///   r = r1 * r2 / 255
		///   g = g1 * g2 / 255
		///   b = b1 * b2 / 255
		///   a = a1 * a2 / 255
		/// を行なう。
		/// </remarks>
		/// <param name="lhs"></param>
		/// <param name="rhs"></param>
		/// <returns></returns>
		public static Color4ub operator*(Color4ub lhs, Color4ub rhs){
			Color4ub color_;
			//		color_.r = r*color.r/255;
			//	255で割るのは255足して256で割ればほぼ同じだ
			/*
				詳しいことは、やね本2を見ること！
			*/
			color_.R = (byte)((lhs.R+255)*rhs.R >> 8);
			color_.G = (byte)((lhs.G+255)*rhs.G >> 8);
			color_.B = (byte)((lhs.B+255)*rhs.B >> 8);
			color_.A = (byte)((lhs.A+255)*rhs.A >> 8);

			return color_;
		}
	}
}
