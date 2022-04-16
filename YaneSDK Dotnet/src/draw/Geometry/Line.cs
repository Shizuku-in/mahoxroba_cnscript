using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// ’¼ü
	/// </summary>
	public class Line : List<Point>
	{
		public Line(Point a,Point b)
		{
			Add(a); Add(b);
		}
	}
}
