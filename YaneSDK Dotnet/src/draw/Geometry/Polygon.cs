using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// ëΩäpå`
	/// </summary>
	public class Polygon : List<Point>
	{
		#region ctor
		public Polygon() { }
		public Polygon(Point[] p) { AddRange(p); }
		#endregion
	}
}
