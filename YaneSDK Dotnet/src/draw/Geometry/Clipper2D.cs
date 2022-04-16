using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 2次元の転送時クリッパ。
	/// 転送行列Affine2Dを保持する。
	/// </summary>
	/// <remarks>
	/// ■　使い方
	/// 1)SrcClipRegion,DstClipRegion,SrcRegion,Transを設定。
	///	2)Updateを呼び出すとSrcRegionがTrans行列によって写像されるDstRegionが求まる。
	/// 3)そのときにSrcClipRegion,DstClipRegionが設定されていれば、それらで
	/// Clipしたのちの写像であるDstRegionClippedが求まる。
	/// 
	/// ■　Clipperの合成
	/// このClipperによる変換は拡張されたAffine変換とみなすことができる。
	/// よって、Clipper同士を合成することが出来る。
	/// 
	/// 1)property設定済みのClipperとしてc1,c2があるとして
	///  c0 = c2 * c1;
	/// とすれば、まずc1のClipperで変換して、そののちにc2で変換したときの
	/// Clipper c0が得られる。
	/// 
	/// 2)この変換のときにSrcRegion,DstRegionが設定されていれば適切にClipされる。
	/// ただし、合成後のClipperでは、途中経路におけるClipは無視される。
	/// よって、SrcRegionは事前にc1かc2に設定されていなければならない。
	/// 
	/// ■　Clipper合成時
	/// 
	///  c0 = c3 * c2 * c1;
	/// と合成して、c1にSrcRegionが設定されておらず、c2にのみsrcRegionが
	/// 設定されているとしよう。ところがc1にsrcClipRegionが設定されていれば、
	/// c2のsrcRegionをまずc1の逆変換によってc1の座標系に変換し、そこで
	/// c1のscrClipRegionによってClipされなければならない。この変換を
	/// 合成の時に自動的に行なう。
	/// 
	/// </remarks>
	public class Clipper2D
	{

		#region properties
		/// <summary>
		/// [in] 転送元クリップリージョン
		/// </summary>
		public Region2D SrcClipRegion;

		/// <summary>
		/// [in] 転送先クリップリージョン
		/// </summary>
		public Region2D DstClipRegion;

		/// <summary>
		/// [in] 転送元のリージョン(nullであっても構わない)
		/// </summary>
		public Region2D SrcRegion;

		/// <summary>
		/// [in] 転送affine行列
		/// </summary>
		public Affine2D Trans;

		/// <summary>
		/// [out] クリップ後の転送元リージョン
		/// </summary>
		public Region2D SrcRegionClipped;

		/// <summary>
		/// [out] クリップ後の転送先リージョン
		/// </summary>
		public Region2D DstRegionClipped;

		#endregion

		#region members
		/// <summary>
		/// このクラスのpropertiesのうち in と書かれているものを入力に受け取り、
		/// outと書かれているものを出力する。
		/// 
		/// Clipper2Dの合成(operator *)を行なうときには自動的にこのUpdate関数が
		/// 呼び出されるので明示的に呼び出す必要はない。
		/// </summary>
		public void Update()
		{
			if (SrcClipRegion != null && SrcRegion != null)
			{
				// 転送元クリップリージョン
				SrcRegionClipped = GeometricTools.Intersect(SrcClipRegion, SrcRegion);
			}
			else
			{
				SrcRegionClipped = SrcRegion;
			}

			DstRegionClipped = GeometricTools.TransRegion(Trnas, SrcRegionClipped);

			// 転送先にクリッパが設定されていれば…
			if (DstClipRegion != null)
			{
				DstRegionClipped = GeometricTools.Intersect(DstClipRegion, DstRegionClipped);
				// ↑を転送元へ戻す
				SrcRegionClipped = GeometricTools.TransRegion(Trans.Inverse(), DstRegionClipped);
			}
		}

		/// <summary>
		/// クリッパの合成
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Clipper2D operator *(Clipper2D a , Clipper2D b)
		{
			Clipper2D clipper = new Clipper2D();
			
			// Affine変換はそのまま合成してしまって良い。
			clipper.Trans = a.Trans * b.Trans;


			return clipper;
		}
		#endregion
	}
}
