using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 幾何的な関係演算を行なう一連のツール
	/// </summary>
	/// <remarks>
	///	この幾何演算クラスについては
	///	http://www.prefield.com/algorithm/
	///	を参考にさせていただいた。
	///	洗練された極めて優れたソースコードである。
	/// </remarks>
	public class GeometricTools
	{
		/// <summary>
		/// CCW(CounterClockwise:反時計回り)であるかの判定を行なう。
		/// 与えられた三点 a, b, c を a → b → c と進むときの位置関係を調べる。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="c"></param>
		/// <returns>
		/// 以下の時計回り,反時計まわりというのは、x軸の正方向を右、y軸の正方向を
		/// 上方向にとるときの話である。
		/// 
		/// +1 : 反時計まわり
		/// -1 : 時計まわり
		///  0 : a , c , bの順番で同一直線上にある。(もしくはbとcが同じ点である)
		/// </returns>
		public static int CCW(Point a, Point b, Point c)
		{
			b -= a; c -= a;
			float bc = b.Cross(c);

			return global::System.Math.Sign(bc);
			/*
			if (bc > 0) return +1;				// counter clockwise
			if (bc < 0) return -1;				// clockwise

			/// +2 : c , a , bの順番で同一直線上にある。
			/// -2 : a , b , cの順番で同一直線上にある。
			// if (b.Dot(c) < 0) return +2;		// c--a--b on line
			// if (b.Norm() < c.Norm()) return -2; // a--b--c on line
			return 0;							// a--c--b on line (or b == c)
			 */
		}

		/// <summary>
		/// 直線と直線の交点を返す。
		/// 
		/// ただし交点が存在しないときは
		/// Point(float.Nan,float.Nan)が返る。
		/// </summary>
		/// <param name="line1"></param>
		/// <param name="line2"></param>
		/// <returns></returns>
		public static Point CrossPoint(Line line1,Line line2)
		{
			//　直線 L : a + s b
			//　直線 M : c + t d
			// があったとして、交点は a + sb = c + td
			// sb - td = c - a なので右辺をΔとおく。
			// s(b×d) = (sb)×d  = (td + Δ)×d = t d×d + Δ×d
			// 同一ベクトルの外積は0なので
			// = t 0 + Δ×d = Δ×d
			// よって s (b×d) = Δ×d
			//   s = Δ×d / (b×d) ただし (b×d)!=0

			Point u = line1[1] - line1[0];
			float v = u.Cross(line2[1] - line2[0]);
			if (v == 0)
				return new Point(float.NaN, float.NaN);
			float s = u.Cross(line1[0] - line2[0]) / v;
			return line2[0] + s * (line2[1] - line2[0]);
		}

		/// <summary>
		/// 直線A,Bの交点を求める。
		/// 直線A上の点、a0,a1
		/// 直線B上の点、b0,b1
		/// </summary>
		/// <param name="a0"></param>
		/// <param name="a1"></param>
		/// <param name="b0"></param>
		/// <param name="b1"></param>
		/// <returns></returns>
		public static Point CrossPoint(Point a0,Point a1,Point b0,Point b1)
		{
			Point u = a1 - a0;
			float s = u.Cross(a0 - b0) / u.Cross(b1- b0);
			return b0 + s * (b1 - b0);
		}

		/// <summary>
		/// 与えられた点列が右回りなのか左回りなのかを判定する。
		/// 
		/// 左回り(反時計まわり) : 1
		/// 回転なし(面積0) : 0
		/// 右回り(時計まわり) : -1
		/// 
		/// ※　x軸の正方向を右、y軸の正方向を上にとったとき。
		/// 
		/// </summary>
		/// <remarks>
		/// これは面積を外積を使って求めればその正負により右回りか左回りかを決定できる。
		/// 
		///	直線と多角形への距離とか交差領域などについて延々と書いてある本『Geometric Tools for Computer Graphics』に面白い記述があったので紹介。
		///	Area(P) = 1/2 ∑ XiYi+1 - Xi+1Yi (i=0,…,n-1) と導き出したあと、この∑の部分を ∑ Xi(Yi+1 - Yi-1) + ∑ (XiYi-1 - Xi+1Yi)と分離して、
		///	後者は X0Y-1 - XnYn-1 = 0となる。よって面積公式は以下のように単純化できる。
		///	Area(P) = 1/2 ∑Xi(Yi+1 - Yi-1) { i = 0,…,n-1 }
		///	このFAQはUsenetのcomp.graphics.algorithmsでDan Sunday(2001)が投稿したが、以前 sci.mathにDave Rusin(1995)が投稿している。
		/// 探せばもっと古いのがあるかも知れないとのこと。
		/// </remarks>
		/// <param name="point"></param>
		/// <returns></returns>
		public static int IsClockwise(Point[] points)
		{
			int n = points.Length;
			float sum2 = 0; // 求めるべき面積の2倍。
			for (int i = 0; i < n; ++i)
				sum2 += points[i].X * (points[(i + 1) % n].Y - points[(i + n - 1) % n].Y);

			return global::System.Math.Sign(sum2);
		}

		/// <summary>
		/// 多角形の面積を求める。
		/// </summary>
		/// <param name="points"></param>
		/// <returns></returns>
		public static float Area(Point[] points)
		{
			int n = points.Length;
			float sum2 = 0; // 求めるべき面積の2倍。
			for (int i = 0; i < n; ++i)
				sum2 += points[i].X * (points[(i + 1) % n].Y - points[(i + n - 1) % n].Y);

			return global::System.Math.Abs(sum2/2);
		}

		/// <summary>
		/// 多角形aと多角形bとの交差領域を求める。
		/// ただし、aは凸包でかつ左回り(反時計まわり)でなければならない。
		/// aは空集合ではならない。
		/// 
		/// ここで言う左回りは x軸の正方向を右、y軸の正方向を上にとったとき。
		/// </summary>
		/// <remarks>
		/// 転送先clippingなどに用いると良い。
		/// </remarks>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Point[] Intersect(Point[] a, Point[] b)
		{
			// このアルゴリズムはやねうらおの手による。
			int N = a.Length;
			for(int i=0;i<N;++i)
			{
				int M = b.Length;
				if (M == 0) continue;

				// 直線 (a[i],a[i-1]) でclipping
				Point a0 = a[i];
				Point a1 = a[(i + 1) % N];

				Point Now = b[M - 1];

				Polygon c = new Polygon();
				for (int j = 0; j < M; ++j)
				{
					Point Next = b[j];

					// 外側の領域以外(内側か直線上)ならばその点を格納。
					if (CCW(a0, a1, Now) != -1) c.Add(Now);
					
					// 内側から外側、あるいは外側から内側にきたならば、その交点を保存。
					if (CCW(a0, a1, Now)*CCW(a0, a1, Next) < 0)
						c.Add(CrossPoint(Now,Next,a0,a1));

					Now = Next;
				}
				b = c.ToArray();
		  }
		  return b;
		}

		/// <summary>
		/// RectとRectの交差範囲を求める。
		/// 交差領域がなければnull。
		/// 
		/// rect1,rect2ともに正則でなければならない。
		/// </summary>
		/// <param name="rect1"></param>
		/// <param name="rect2"></param>
		/// <returns></returns>
		public static Rect Intersect(Rect rect1, Rect rect2)
		{
			if (rect1.Right <= rect2.Left ||
				rect2.Right <= rect1.Left ||
				rect1.Bottom <= rect2.Top ||
				rect2.Bottom <= rect1.Top )
				return null;

			// 交差領域は空集合ではない。

			return new Rect
			(
				rect1.Left < rect2.Left ? rect2.Left : rect1.Left ,			// max(left)
				rect1.Top < rect2.Top ? rect2.Top : rect1.Top ,				// max(top)
				rect1.Right < rect2.Right ? rect1.Right : rect2.Right,		// min(right)
				rect1.Bottom < rect2.Bottom ? rect1.Bottom : rect2.Bottom 	// min(bottom)
			);

		}

		/// <summary>
		/// 凸多角形と矩形との交差領域を求める。矩形は正則であること。
		/// 交差領域が存在しなければnull。
		/// 
		/// 高速化を意識してコーディングしてある。
		/// 凸多角形がまるごとRect aに収まるならば、凸多角形であるbの参照がそのまま返ることもある。(高速化のため)
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Point[] Intersect(Rect a, Point[] b)
		{
			int N = b.Length;
			
			// 現在のカレントの点はレンジ外であるか
			bool isLastPointOut = false, isCurrentPointOut = false;
			// ↑ここで宣言するのは行儀が悪いが、これは本来初期化しなくていい変数なので
			// unrollしているところで何度も初期化されたくない。

			// y = topで切断
			{
				bool inRect = true;  // すべてが矩形内
				bool outRect = true; // すべてが矩形外
				float top = a.Top;

				for (int i = 0; i < N; ++i)
				{
					outRect = outRect && (b[i].Y <= top);
					inRect = inRect && (b[i].Y >= top);
				}
				if (outRect)
					return null;
				if (!inRect)
				{
					// 凸多角形c を y = topで切断したものをbとする必要がある。
					Polygon c = new Polygon();

					// ループを 0と[1,N]にすれば、[1,N]の区間で(i-1)が非負であることを保証できる
					for (int i = 0; i < N + 1; ++i)
					{
						isCurrentPointOut = b[i % N].Y < top;
						if (i != 0)
						{
							if (isLastPointOut ^ isCurrentPointOut)
							{
								// 辺の両端が領域をまたぐので直線とy=topとの交点を出力
								Point p1 = b[(i - 1) % N];
								Point p2 = b[i % N];

								// t・P1 + (1-t)・P2 = { x , top }
								// t(P1 - P2) + P2 = { x, top }
								// ∴ t = (top - P2.Y)/(P1.Y - P2.Y)

								float t = (top - p2.Y) / (p1.Y - p2.Y);
								float x = t * p1.X + (1 - t) * p2.X;

								c.Add(new Point(x, top));
							}
							if (!isCurrentPointOut)
							{
								// 辺の今回の頂点が内部領域なのでb[i]を出力
								c.Add(b[i % N]);
							}
						}
						isLastPointOut = isCurrentPointOut;
					}
					b = c.ToArray();
					N = b.Length;
				}
			}

			// y = bottomで切断
			{
				bool inRect = true;  // すべてが矩形内
				bool outRect = true; // すべてが矩形外
				float bottom = a.Bottom;

				for (int i = 0; i < N; ++i)
				{
					outRect = outRect && (b[i].Y >= bottom);
					inRect = inRect && (b[i].Y <= bottom);
				}
				if (outRect)
					return null;
				if (!inRect)
				{
					// 凸多角形c を y = bottomで切断したものをbとする必要がある。
					Polygon c = new Polygon();

					// ループを 0と[1,N]にすれば、[1,N]の区間で(i-1)が非負であることを保証できる
					for (int i = 0; i < N + 1; ++i)
					{
						isCurrentPointOut = b[i % N].Y > bottom;
						if (i != 0)
						{
							if (isLastPointOut ^ isCurrentPointOut)
							{
								// 辺の両端が領域をまたぐので直線とy=bottomとの交点を出力
								Point p1 = b[(i - 1) % N];
								Point p2 = b[i % N];

								// t・P1 + (1-t)・P2 = { x , bottom }
								// t(P1 - P2) + P2 = { x, bottom }
								// ∴ t = (bottom - P2.Y)/(P1.Y - P2.Y)

								float t = (bottom - p2.Y) / (p1.Y - p2.Y);
								float x = t * p1.X + (1 - t) * p2.X;

								c.Add(new Point(x, bottom));
							}
							if (!isCurrentPointOut)
							{
								// 辺の今回の頂点が内部領域なのでb[i]を出力
								c.Add(b[i % N]);
							}
						}
						isLastPointOut = isCurrentPointOut;
					}
					b = c.ToArray();
					N = b.Length;
				}
			}

			// x = leftで切断
			{
				bool inRect = true;  // すべてが矩形内
				bool outRect = true; // すべてが矩形外
				float left = a.Left;

				for (int i = 0; i < N; ++i)
				{
					outRect = outRect && (b[i].X <= left);
					inRect = inRect && (b[i].X >= left);
				}
				if (outRect)
					return null;
				if (!inRect)
				{
					// 凸多角形c を y = leftで切断したものをbとする必要がある。
					Polygon c = new Polygon();

					// ループを 0と[1,N]にすれば、[1,N]の区間で(i-1)が非負であることを保証できる
					for (int i = 0; i < N + 1; ++i)
					{
						isCurrentPointOut = b[i % N].X < left;
						if (i != 0)
						{
							if (isLastPointOut ^ isCurrentPointOut)
							{
								// 辺の両端が領域をまたぐので直線とx=leftとの交点を出力
								Point p1 = b[(i - 1) % N];
								Point p2 = b[i % N];

								// t・P1 + (1-t)・P2 = { left , y }
								// t(P1 - P2) + P2 = { left , y }
								// ∴ t = (left - P2.X)/(P1.X - P2.X)

								float t = (left - p2.X) / (p1.X - p2.X);
								float y = t * p1.Y + (1 - t) * p2.Y;

								c.Add(new Point(left, y));
							}
							if (!isCurrentPointOut)
							{
								// 辺の今回の頂点が内部領域なのでb[i]を出力
								c.Add(b[i % N]);
							}
						}
						isLastPointOut = isCurrentPointOut;
					}
					b = c.ToArray();
					N = b.Length;
				}
			}

			// x = rightで切断
			{
				bool inRect = true;  // すべてが矩形内
				bool outRect = true; // すべてが矩形外
				float right = a.Right;

				for (int i = 0; i < N; ++i)
				{
					outRect = outRect && (b[i].X >= right);
					inRect = inRect && (b[i].X <= right);
				}
				if (outRect)
					return null;
				if (!inRect)
				{
					// 凸多角形c を y = rightで切断したものをbとする必要がある。
					Polygon c = new Polygon();

					// ループを 0と[1,N]にすれば、[1,N]の区間で(i-1)が非負であることを保証できる
					for (int i = 0; i < N + 1; ++i)
					{
						isCurrentPointOut = b[i % N].X > right;
						if (i != 0)
						{
							if (isLastPointOut ^ isCurrentPointOut)
							{
								// 辺の両端が領域をまたぐので直線とx=rightとの交点を出力
								Point p1 = b[(i - 1) % N];
								Point p2 = b[i % N];

								// t・P1 + (1-t)・P2 = { left , y }
								// t(P1 - P2) + P2 = { left , y }
								// ∴ t = (left - P2.X)/(P1.X - P2.X)

								float t = (right - p2.X) / (p1.X - p2.X);
								float y = t * p1.Y + (1 - t) * p2.Y;

								c.Add(new Point(right, y));
							}
							if (!isCurrentPointOut)
							{
								// 辺の今回の頂点が内部領域なのでb[i]を出力
								c.Add(b[i % N]);
							}
						}
					}
					b = c.ToArray();
					N = b.Length;
				}
			}

			return b;
		}


		/// <summary>
		/// 多角形a,bのすべてが同じ点であるかをチェックする。
		/// UnitTestなどで用いると良いだろう。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>等価であればtrue。</returns>
		public static bool PolygonEqual(Point[] a, Point[] b)
		{
			if (a.Length != b.Length)
				return false;

			for (int i = 0; i < a.Length; ++i)
				if (a[i] != b[i])
					return false;

			return true;
		}

		/// <summary>
		/// 多角形の頂点列を与えて、それが凸多角形かどうかを判定する。
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public static bool IsConvex(Point[] a)
		{
			int N = a.Length;
			bool[] b = new bool[3];
			for (int i = 0; i < N; ++i)
			{
				// CCWは-1,0,1しか返さない。
				// ループの最中にCCWが-1と1を返した場合にのみfalse
				b[CCW(a[i], a[(i + 1) % N], a[(i + 2) % N]) + 1] = true;

				if (b[0] && b[2])
					return false;
			}

		//	return !(b[0] && b[2]);
			return true;
		}

		/// <summary>
		/// UnitTest用のメソッド
		/// </summary>
		public static void UnitTest()
		{
			Debug.Assert(
				CCW(new Point(0, 0),
					new Point(1, 0),
					new Point(1, 1)) == 1
			);

			Debug.Assert(
				CrossPoint(
					new Line(new Point(0,1),new Point(2,1)),
					new Line(new Point(1,0),new Point(1,2))
					) == new Point(1,1)
			);

			Debug.Assert(
				CrossPoint(
					new Point(0, 1), new Point(2, 1),
					new Point(1, 0), new Point(1, 2)
					) == new Point(1, 1)
			);

			Debug.Assert(
				IsClockwise(new Point[3] { new Point(0, 0), new Point(2, 0), new Point(1, 1) }) == 1
			);

			Debug.Assert(
				PolygonEqual
					(Intersect(
						new Point[4] { new Point(0, 0), new Point(10, 0), new Point(10, 5), new Point(0, 5) },
						new Point[4] { new Point(3, -5), new Point(6, -5), new Point(6, 20), new Point(3, 20) }
					),
					new Point[4] { new Point(3, 0), new Point(6, 0), new Point(6, 5), new Point(3, 5) }
				)
			);

			Debug.Assert(
				PolygonEqual
					(Intersect(
						new Point[4] { new Point(0, 0), new Point(10, 0), new Point(10, 5), new Point(0, 5) },
						new Point[4] { new Point(6, -5), new Point(3, -5), new Point(3, 20), new Point(6, 20) }
					),
					new Point[4] { new Point(6, 0), new Point(3, 0), new Point(3, 5), new Point(6, 5) }
				)
			);

			Debug.Assert(
				PolygonEqual
					(Intersect(
						new Rect(0,0,10,5),
						new Point[4] { new Point(6, -5), new Point(3, -5), new Point(3, 20), new Point(6, 20) }
					),
					new Point[4] { new Point(3, 5), new Point(6, 5), new Point(6, 0), new Point(3, 0) }
				)
			);

		}

	}
}
