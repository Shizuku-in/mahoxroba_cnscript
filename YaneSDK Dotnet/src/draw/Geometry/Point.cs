using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 点を表している構造体
	/// </summary>
	public struct Point
	{
		/// <summary>
		/// x座標。
		/// </summary>
		public float X;
		/// <summary>
		/// y座標。
		/// </summary>
		public float Y;

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="x_"></param>
		/// <param name="y_"></param>
		public Point(float x_, float y_)
		{
			X = x_;
			Y = y_;
		}

		/// <summary>
		/// 各データを設定する。
		/// </summary>
		/// <param name="x_"></param>
		/// <param name="y_"></param>
		public void SetPoint(float x_, float y_)
		{
			X = x_; Y = y_;
		}

		/// <summary>
		/// 加算
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Point operator +(Point a, Point b)
		{
			return new Point(a.X + b.X, a.Y + b.Y);
		}

		/// <summary>
		/// 減算
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Point operator -(Point a, Point b)
		{
			return new Point(a.X - b.X, a.Y - b.Y);
		}

		/// <summary>
		/// 定数倍
		/// </summary>
		/// <param name="k"></param>
		/// <param name="a"></param>
		/// <returns></returns>
		public static Point operator *(float k, Point a)
		{
			return new Point(k * a.X, k * a.Y);
		}

		/// <summary>
		/// 定数倍
		/// </summary>
		/// <param name="a"></param>
		/// <param name="k"></param>
		/// <returns></returns>
		public static Point operator *(Point a, float k)
		{
			return k * a;
		}

		/// <summary>
		/// 掛算(内積とは異なる)
		/// Point(a.X * b.X, a.Y * b.Y)を返す。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Point operator *(Point a, Point b)
		{
			return new Point(a.X * b.X, a.Y * b.Y);
		}

		/// <summary>
		/// 比較オペレータ
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(Point a, Point b)
		{
			return a.X == b.X && a.Y == b.Y;
		}

		/// <summary>
		/// 比較オペレータ
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(Point a, Point b)
		{
			return !(a == b);
		}

		/// <summary>
		/// ==と!=を定義したときには用意したほうが無難なので用意。
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return this == (Point)obj;
		//	return base.Equals(obj);
		}

		/// <summary>
		/// ==と!=を定義したときには用意したほうが無難なので用意。
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			// こんなもんでいいか..
			return (int)(X + Y);
		//	return base.GetHashCode();
		}

		/// <summary>
		/// 内積
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public float Dot(Point a)
		{
			return this.X * a.X + this.Y * a.Y;
		}

		/// <summary>
		/// 外積
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public float Cross(Point a)
		{
			return this.X * a.Y - this.Y * a.X;
		}

		/// <summary>
		/// 単項マイナス
		/// </summary>
		/// <returns></returns>
		public static Point operator -(Point a)
		{
			return new Point(-a.X, -a.Y);
		}

		/// <summary>
		/// ノルム(大きさの2乗)
		/// </summary>
		/// <returns></returns>
		public float Norm()
		{
			return X * X + Y * Y;
		}

		/// <summary>
		/// 原点からの距離を求める。
		/// ベクトルとみなしたときの大きさであり、ノルムのsqrtである。
		/// </summary>
		/// <returns></returns>
		public float Abs()
		{
			return (float)global::System.Math.Sqrt(Norm());
		}

		/// <summary>
		/// Debug用に文字列化する
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("({0},{1})", X, Y);
		}
	}

}
