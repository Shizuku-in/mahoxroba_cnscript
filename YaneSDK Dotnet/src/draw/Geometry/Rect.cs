using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 矩形を表しているクラス。
	/// </summary>
	/// <remarks>
	/// 矩形(Left,Top)-(Right,Bottom)であるが、(Right,Bottom)は含まない。
	/// 
	/// また、Rectは正則であること (Left >= Right ,  Bottom  >= Top であること) が前提条件。
	/// ただし特殊な用途で使う場合、↑の限りではない。
	/// 
	/// たとえばBlt系のメソッドで転送元矩形を表す場合は、
	/// LeftとRightを入れかえれば左右反転しての描画となる。
	/// TopとBottomとを入れかえれば上下反転しての描画となる。
	/// 
	/// 交差領域などは、GeometricToolsクラスのほうにまとめて用意してある。
	/// </remarks>
	public class Rect
	{
		#region members

		/// <summary>
		/// 左。
		/// </summary>
		public float Left;
		/// <summary>
		/// 上。
		/// </summary>
		public float Top;
		/// <summary>
		/// 右。
		/// </summary>
		public float Right;
		/// <summary>
		/// 下。
		/// </summary>
		public float Bottom;

		#endregion

		#region properties
		/// <summary>
		/// 幅を取得する
		/// </summary>
		/// <remarks>
		/// Right - Left をしているだけ。符号がマイナスになる場合もありうる
		/// </remarks>
		/// <returns></returns>
		public float Width
		{
			get { return Right - Left; }
		}

		/// <summary>
		/// 高さを取得する
		/// </summary>
		/// <remarks>
		/// Bottom - Top をしているだけ。符号がマイナスになる場合もありうる。
		/// </remarks>
		public float Height
		{
			get { return Bottom - Top; }
		}

		/// <summary>
		/// 左上(Top,Left)をPointとして取得する/設定する。
		/// Pointはstructであることに注意。
		/// </summary>
		public Point TopLeft
		{
			get { return new Point(Left, Top); }
			set { Left = value.X; Top = value.Y; }
		}

		/// <summary>
		/// 右下(Right,Bottom)をPointとして取得する/設定する。
		/// Pointはstructであることに注意。
		/// </summary>
		public Point BottomRight
		{
			get { return new Point(Right, Bottom); }
			set { Right = value.X; Bottom = value.Y; }
		}
		#endregion

		#region ctor
		public Rect()
		{
			Left = Right = Top = Bottom = 0;
		}

		/// <summary>
		/// Rectの4点を与えて初期化するコンストラクタ。
		/// </summary>
		/// <param name="left_"></param>
		/// <param name="top_"></param>
		/// <param name="right_"></param>
		/// <param name="bottom_"></param>
		public Rect(float left_, float top_, float right_, float bottom_)
		{
			Left = left_;
			Top = top_;
			Right = right_;
			Bottom = bottom_;
		}

		/// <summary>
		/// 左上と右下の2点を与えてRectを初期化するコンストラクタ。
		/// </summary>
		/// <param name="TopLeft"></param>
		/// <param name="BottomRight"></param>
		public Rect(Point TopLeft, Point BottomRight)
		{
			this.TopLeft = TopLeft;
			this.BottomRight = BottomRight;
		}

		/// <summary>
		/// rectからはdeep copyを行なう。
		/// </summary>
		/// <param name="r"></param>
		public Rect(Rect rect)
		{
			Left = rect.Left;
			Right = rect.Right;
			Top = rect.Top;
			Bottom = rect.Bottom;
		}

		/// <summary>
		/// deep copyして返す。
		/// </summary>
		/// <returns></returns>
		public Rect Clone()
		{
			return new Rect(this);
		}
		#endregion

		#region methods
		/// <summary>
		/// 各データを設定する。
		/// </summary>
		/// <param name="left_"></param>
		/// <param name="top_"></param>
		/// <param name="right_"></param>
		/// <param name="bottom_"></param>
		public void SetRect(float left_, float top_, float right_, float bottom_)
		{
			Left = left_;
			Top = top_;
			Right = right_;
			Bottom = bottom_;
		}
	
		/// <summary>
		/// 指定の座標が、この矩形に含まれるのかを判定する。
		/// Left >= Right ,  Bottom  >= Top であることが前提条件。
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public bool IsIn(float x, float y)
		{
			return (Left <= x && x < Right && Top <= y && y < Bottom);
		}

		#endregion


	}
}
