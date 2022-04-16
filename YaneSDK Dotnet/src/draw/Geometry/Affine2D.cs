using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 2次元affine行列
	/// </summary>
	public class Affine2D
	{
		#region ctor
		/// <summary>
		/// 単位行列で初期化
		/// </summary>
		public Affine2D()
		{
			Matrix = new Matrix2D();
		}

		/// <summary>
		/// 単位行列のk倍で初期化
		/// </summary>
		/// <param name="a"></param>
		public Affine2D(float k)
		{
			Matrix = new Matrix2D(k);
		}

		/// <summary>
		/// 単位行列 + pで初期化
		/// </summary>
		/// <param name="p"></param>
		public Affine2D(Point p)
		{
			Matrix = new Matrix2D();
			Point = p;
		}

		/// <summary>
		/// 単位行列のk倍 + pで初期化
		/// </summary>
		/// <param name="k"></param>
		/// <param name="p"></param>
		public Affine2D(float k, Point p)
		{
			Matrix = new Matrix2D(k);
			Point = p;
		}

		/// <summary>
		/// 指定された2次元行列で初期化
		/// </summary>
		/// <param name="m"></param>
		public Affine2D(Matrix2D m)
		{
			Matrix = m;
		}

		/// <summary>
		/// 指定された2次元行列と平行移動とで初期化
		/// </summary>
		/// <param name="m"></param>
		/// <param name="p"></param>
		public Affine2D(Matrix2D m, Point p)
		{
			Matrix = m;
			Point = p;
		}

		/// <summary>
		/// affine行列の各要素を指定しての初期化
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <param name="d"></param>
		/// <param name="e"></param>
		/// <param name="f"></param>
		public Affine2D(float a, float b, float c, float d, float e, float f)
		{
			Matrix = new Matrix2D(a, b, c, d);
			Point = new Point(e, f);
		}

		/// <summary>
		/// 行列を
		/// (k1 0)
		/// (0 k2)
		/// で初期化する。
		/// </summary>
		/// <param name="k1"></param>
		/// <param name="k2"></param>
		public Affine2D(float k1, float k2)
		{
			Matrix = new Matrix2D(k1, 0, 0, k2);
		}

		/// <summary>
		/// src Rectをdst Rectへ変換するAffine行列を求める。
		/// src,dst Rectは正則であること。
		/// 正則の説明についてはRectの説明を見よ。
		/// 
		/// また求めるべきAffine行列が存在しないときはnullが返る。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dst"></param>
		public static Affine2D RectToRect(Rect src,Rect dst)
		{
			float srcWidth = src.Width;
			if (srcWidth == 0)
				return null;
			float widthRate = dst.Width / srcWidth;

			float srcHeight = src.Height;
			if (srcHeight == 0)
				return null;
			float heightRate = dst.Height / srcHeight;

			// 平行移動量
			Point point = dst.TopLeft - src.TopLeft;

			return new Affine2D(widthRate, 0, 0, heightRate, point.X,point.Y);

		}

		#endregion

		#region properties
		/// <summary>
		/// 2次元行列
		/// </summary>
		public Matrix2D Matrix;

		/// <summary>
		/// affine行列の平行移動成分
		/// </summary>
		public Point Point;
		#endregion

		#region affine変換の演算用operator

		/// <summary>
		/// affine行列の掛算。
		/// </summary>
		/// <remarks>
		/// affine行列
		///		M1 + P1
		/// と
		///		M2 + P2
		/// があったとする。この行列による合成変換
		///		(M1 + P1)(M2 + P2)によって
		/// 定点pがどこに移動するか考える。まずM2+P2によって
		///		(M2・p + P2) に移動したあと
		/// M1+P1でM1(M2・p + P2) + P1に移動する。
		/// M1M2・p + M1P2 + P1
		/// よって、行列部 M1M2 , 平行要素M1P2+P1
		/// が求めるべきaffine行列である。
		/// </remarks>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Affine2D operator *(Affine2D a, Affine2D b)
		{
			return new Affine2D(
				a.Matrix * b.Matrix,
				a.Matrix * b.Point + a.Point);
		}

		/// <summary>
		/// 定数倍
		/// </summary>
		/// <param name="k"></param>
		/// <param name="a"></param>
		/// <returns></returns>
		public static Affine2D operator *(float k, Affine2D a)
		{
			return new Affine2D(k * a.Matrix, k * a.Point);
		}

		public static Affine2D operator *(Affine2D a, float k)
		{
			return k * a;
		}

		/// <summary>
		/// 2次元affine行列に2次元ベクトルを掛ける。
		/// （点pがaffine変換aによって変換する先の点を求める。）
		/// Pointはstructであることに注意しながら使うこと。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="p"></param>
		/// <returns></returns>
		public static Point operator *(Affine2D a, Point p)
		{
			return a.Matrix * p + a.Point;
		}

		/// <summary>
		/// 平行移動変換pとaffine変換aの合成変換を求める。
		/// すなわち、a.Matrix + (A.Matrix * p + A.Point)
		/// Compose(Point p,Affine2D a)とは可換ではないことに注意！
		/// </summary>
		/// <param name="a"></param>
		/// <param name="p"></param>
		/// <returns></returns>
		public static Affine2D Compose(Affine2D a, Point p)
		{
			return new Affine2D(a.Matrix, a.Matrix * a.Point + p);
		}

		/// <summary>
		/// affine行列に平行移動変換を合成。
		/// すなわち、a.Matrix + a.Point + pである。
		/// Compose(Affine2D a, Point p)とは可換ではないことに注意！
		/// </summary>
		/// <param name="a"></param>
		/// <param name="p"></param>
		/// <returns></returns>
		public static Affine2D Compose(Point p, Affine2D a)
		{
			return new Affine2D(a.Matrix, a.Point + p);
		}

		/// <summary>
		/// 逆変換を求める。逆変換が存在しないときはnullが返る。
		/// 
		/// Affine2D: p' = MP + T
		/// よって、P = M^-1 P' - M^-1 T
		/// Affine2D inverse : M^-1 - M^-1 Tである。
		/// </summary>
		/// <returns></returns>
		public Affine2D Inverse()
		{
			Matrix2D inverseMatrix = this.Matrix.Inverse();
			if (inverseMatrix == null)
				return null;

			return new Affine2D(inverseMatrix, inverseMatrix * -this.Point);
		}

		#endregion

		/// <summary>
		/// デバッグ用に文字列化する機能
		/// </summary>
		public override string ToString()
		{
			return string.Format("[{0},{1}]"
				, Matrix.ToString(), Point.ToString());
		}
	}
}
