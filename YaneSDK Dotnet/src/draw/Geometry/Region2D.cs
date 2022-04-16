using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 矩形or凸多角形を表現する。
	/// 
	/// 転送のときにClipper2Dと絡めて使うと便利。
	/// 
	/// Rect,Polygonのどちらかはnullである。両方が非nullであることはない。
	/// </summary>
	public class Region2D
	{
		#region ctor
		public Region2D(Rect rect)
		{
			this.Rect = rect;
		}

		public Region2D(Point[] polygon)
		{
			this.Polygon = polygon;
		}
		#endregion

		#region properties
		/// <summary>
		/// 矩形(正則でなくてはならない)
		/// </summary>
		public Rect Rect;

		/// <summary>
		/// 凸多角形
		/// </summary>
		public Point[] Polygon;
		#endregion
	}
}
