using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// ü•ª
	/// </summary>
	public class Segment : List<Point>
	{
		public Segment(Point a, Point b)
		{
			Add(a); Add(b);
		}
	}
}
