using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 2次元行列クラス
	/// </summary>
	public class Matrix2D
	{
		/// <summary>
		/// 行列要素
		/// (a b)
		/// (c d)
		/// </summary>
		public float A, B, C, D;

		#region ctor
		/// <summary>
		/// 単位行列で初期化
		/// </summary>
		public Matrix2D() { A = D = 1; B = C = 0; }

		/// <summary>
		/// 単位行列のk倍で初期化
		/// </summary>
		/// <param name="k"></param>
		public Matrix2D(float k) { A = D = k; B = C = 0; }

		/// <summary>
		/// 行列の各要素を指定しての初期化
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <param name="d"></param>
		public Matrix2D(float a, float b, float c, float d)
		{
			A = a; B = b; C = c; D = d;
		}

		/// <summary>
		/// 回転行列を設定する。
		/// rad = 0～512を一周とする角度。
		/// 回転方向は、x軸を右、y軸を下方向にとるなら時計まわり。
		/// 
		/// 型の同じコンストラクタを持てないのでマーカーつきのコンストラクタを
		/// 用いて対処する。→MatrixRotateは単なるenum
		/// </summary>
		/// <returns></returns>
		public Matrix2D(MatrixRotate r, float rad)
		{
			A = D = (float)global::System.Math.Cos
				(global::System.Math.PI * rad / 256);
			C = (float)global::System.Math.Sin
				(global::System.Math.PI * rad / 256);
			B = -C;
		}

		/// <summary>
		/// 回転拡大行列を設定する。
		/// rad = 0～512を一周とする角度。
		/// rate = 拡大比率
		/// 回転方向は、x軸を右、y軸を下方向にとるなら時計まわり。
		/// 
		/// 型の同じコンストラクタを持てないのでマーカーつきのコンストラクタを
		/// 用いて対処する。→MatrixRotateは単なるenum
		/// </summary>
		/// <param name="r"></param>
		/// <param name="rad"></param>
		/// <param name="rate"></param>
		public Matrix2D(MatrixRotate dummyMarker, float rad, float rate)
		{
			A = D = (float)(global::System.Math.Cos
				(global::System.Math.PI * rad / 256) * rate);
			C = (float)(global::System.Math.Sin
				(global::System.Math.PI * rad / 256) * rate);
			B = -C;
		}

		public class MatrixRotate { }

		#endregion

		#region 行列演算関係のオペレータ
		/// <summary>
		/// 行列の加算
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Matrix2D operator +(Matrix2D a, Matrix2D b)
		{
			return new Matrix2D(a.A + b.A, a.B + b.B, a.C + b.C, a.D + b.D);
		}

		/// <summary>
		/// 行列の減算
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Matrix2D operator -(Matrix2D a, Matrix2D b)
		{
			return new Matrix2D(a.A - b.A, a.B - b.B, a.C - b.C, a.D - b.D);
		}

		/// <summary>
		/// 行列の掛算
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Matrix2D operator *(Matrix2D a, Matrix2D b)
		{
			return new Matrix2D(
				a.A * b.A + a.B * b.C,
				a.A * b.B + a.B * b.D,
				a.C * b.A + a.D * b.C,
				a.C * b.B + a.D * b.D);
		}

		/// <summary>
		/// 定数倍
		/// </summary>
		/// <param name="k"></param>
		/// <param name="a"></param>
		/// <returns></returns>
		public static Matrix2D operator *(float k, Matrix2D a)
		{
			return new Matrix2D(k * a.A, k * a.B, k * a.C, k * a.D);
		}

		/// <summary>
		/// 行列×ベクトル
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Point operator *(Matrix2D a, Point p)
		{
			return new Point
				(
					a.A * p.X + a.B * p.Y,
					a.C * p.X + a.D * p.Y
				);
		}
		#endregion

		#region methods
		/// <summary>
		/// 逆行列を求める。逆行列が存在しないときは、nullを返す。
		/// </summary>
		/// <returns></returns>
		public Matrix2D Inverse()
		{
			float det = A * D - B * C;
			if (det == 0)
				return null;

			return new Matrix2D(D / det, -B / det, -C / det, A / det);
		}

		#endregion

		/// <summary>
		/// debug用に文字列化
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("{({0},{1}),({2},{3})}", A, B, C, D);
		}
	}
}
