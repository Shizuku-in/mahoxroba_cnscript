using System;
using System.Collections.Generic;
using System.Text;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 転送時に基準とする転送元の点を指定するための列挙体
	/// </summary>
	public enum BltLayout
	{
		TopLeft = 0,		// 左上
		TopMiddle = 1,		// 中央上
		TopRight = 2,		// 右上
		MiddleLeft = 3,		// 左中央
		Center = 4,			// 中央
		MiddleRight = 5,	// 右中央
		BottomLeft = 6,		// 左下
		BottomMiddle = 7,	// 中央下
		BottomRight = 8		// 右下
	}

	/// <summary>
	/// Rectを与えて、そこからBltLayoutに従ったポジションを算出する。
	/// Updateが呼び出されたときにこのRect1のBltLayoutで指定されるPointを返す。
	/// またRect1を正則(Bottom >= Top , Right >= Left)にしたものをRect2に返す。
	/// </summary>
	/// </summary>
	public class BltLayoutHelper
	{
		/// <summary>
		/// 入力Rect
		/// </summary>
		public Rect Rect1;

		/// <summary>
		/// 入力Layout
		/// </summary>
		public BltLayout Layout; 

		/// <summary>
		/// 出力Point
		/// </summary>
		public Point LayoutPoint;
	
		/// <summary>
		/// 出力Rect(Rect1を正則化したもの)
		/// </summary>
		public Rect Rect2;

		public YanesdkResult Update()
		{
			// またここで、Layoutの指定を行なう必要があるので
			// Top,Left,Bottom,Rightを確定させておく。


			float top, left, bottom, right;
			if (Rect1.Left < Rect1.Right)
			{
				left = Rect1.Left;
				right = Rect1.Right;
			}
			else
			{
				left = Rect1.Right;
				right = Rect1.Left;
			}
			if (Rect1.Top < Rect1.Bottom)
			{
				top = Rect1.Top;
				bottom = Rect1.Bottom;
			}
			else
			{
				top = Rect1.Bottom;
				bottom = Rect1.Top;
			}

			float srcRectWidth = right - left;
			float srcRectHeight = bottom - top;
			if (srcRectWidth <= 0 || srcRectHeight <= 0)
				return YanesdkResult.InvalidParameter;

			switch (Layout)
			{
				case BltLayout.TopLeft:
					LayoutPoint = new Point(left, top); break;
				case BltLayout.TopMiddle:
					LayoutPoint = new Point(left + srcRectWidth / 2, top); break;
				//	↑転送元矩形の座標が端数になるとアンチエイリアスが
				// かかって気持ち悪い気も少しするが、これは仕様ということで。
				case BltLayout.TopRight:
					LayoutPoint = new Point(right, top); break;

				case BltLayout.MiddleLeft:
					LayoutPoint = new Point(left, top + srcRectHeight / 2); break;
				case BltLayout.Center:
					LayoutPoint = new Point(left + srcRectWidth / 2, top + srcRectHeight / 2); break;
				case BltLayout.MiddleRight:
					LayoutPoint = new Point(right, top + srcRectHeight / 2); break;

				case BltLayout.BottomLeft:
					LayoutPoint = new Point(left, bottom); break;
				case BltLayout.BottomMiddle:
					LayoutPoint = new Point(left + srcRectWidth / 2, bottom); break;
				case BltLayout.BottomRight:
					LayoutPoint = new Point(right, bottom); break;

				default:
					return YanesdkResult.InvalidParameter; // あってはならないのだが。
			}

			Rect2 = new Rect(left, top, right, bottom);

			return YanesdkResult.NoError;
		}


	}
}
