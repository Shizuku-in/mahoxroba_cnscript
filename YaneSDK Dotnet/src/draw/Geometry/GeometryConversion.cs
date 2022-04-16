using System;
using System.Collections.Generic;
using System.Text;
using Yanesdk.Draw;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{

	/// <summary>
	/// ITextureが描画時に幾何学変換を行なうが、
	/// そのサポートをするためのクラス。
	/// 
	/// 描画変換は、以下の手順で行なわれる。
	/// 
	///		SrcRect:転送元矩形(nullは不可)の取得。
	/// 　　　　↓
	///		BltLayout:転送元矩形のどの地点をベースポイントとするかの取得。
	///			※ベースポイントは、転送先として指定されている(x,y)へ
	///			そのまま移動する。
	///			↓
	///		Affine変換 Trans0 を行なう。
	///			※これはスプライトクラスなどが内部的に使用するためのもの
	///			ユーザー側はこれを変更しないほうが無難。
	///			↓
	///		Affine変換 Trans を行なう。
	///			※これはユーザー側で変更できる。
	///			表示時のオフセットや回転拡大縮小などにはこれを指定すると良いだろう。
	///			↓
	///		DstSize:転送先サイズの取得。(指定がなければこの変換は行なわれない)
	///			↓
	///		DstPoint:転送先の取得。
	/// 
	/// よって、
	///			M0 : SrcRectのLayoutで指定されているポイントを原点(0,0)へ移動させる平行させる変換
	///			M1 : 原点をDstPointへ平行移動させる変換
	///			K0 : DstSizeに転送元矩形を変更する変換
	/// とすれば、
	///		合成変換 = M1・Trans・Trans0・K0・M0
	/// として求まる。
	/// 
	/// ここで求まったaffine変換をAとする。
	/// 
	/// 次にSrcRectのclippingを行なう。これは
	///		foreach point in SrcRect
	///			pointが転送元矩形からはみ出ていれば転送元矩形内に押し戻す
	///			(例 : if (x >= SrcWidth) x = SrcWidth-1;
	/// 
	/// これで転送元のclippingが完了する。次に、clipping処理後のrectをAで変換した
	/// ときのDstを求め、そこに転送する。
	/// 
	/// 要するに最終的な出力は
	///		Point[4] SrcPoints と Point[4] DstPointsである。
	/// よって、CalcではこのクラスのSrcPoints,DstPointsに計算された値を設定する。
	/// 合成変換Aもこの時得られるので、TransAllとして取得可能である。
	/// 
	/// また、さらにSrc側はテクスチャ座標に変換することを考慮して、
	/// テクスチャの(width,height)位置に相当する座標を
	///		Point TextureRate
	/// として設定できる。ここで設定した値に対して、最終的に
	///		foreach point in SrcPoint
	///			point = point * TextureRate
	/// という掛算が行なわれる。
	/// </summary>
	/// <remarks>
	/// SrcRectの(left,bottom)の点は含まないものとする。
	/// また、top > bottomや、right > leftの場合、上下反転、左右反転を
	/// 意味するものとする。
	/// 
	/// 例えば、左右反転時においては、(right,bottom)の点をこの矩形に含まない。
	/// このように定義することによって、左右反転して描画したいときは単に
	/// leftとrightを入れ替えるだけで良いからである。
	///	</remarks>
	public class GeometricConversion2D
	{
		#region properties
		/// <summary>
		/// 転送元矩形。nullならばSrcWidth,SrcHeightの意味。
		/// </summary>
		public Rect SrcRect;

		/// <summary>
		/// 転送元サーフェースの幅
		/// </summary>
		public float SrcWidth;

		/// <summary>
		/// 転送元サーフェースの高さ
		/// </summary>
		public float SrcHeight;

		/// <summary>
		/// 転送時に基準とする転送元の点
		/// </summary>
		public BltLayout Layout;

		/// <summary>
		/// スプライトなどが内部的に用いるaffine変換
		/// </summary>
		public Affine2D Trans0 = new Affine2D();

		/// <summary>
		/// 転送先へ転送する直前にかかるaffine変換
		/// </summary>
		public Affine2D Trans = new Affine2D();

		/// <summary>
		/// 転送先のサイズ。nullなら転送先サイズに指定なし。
		/// </summary>
		public Size DstSize;

		/// <summary>
		/// 転送先の移動させたい点。
		/// </summary>
		public Point DstPoint;

		/// <summary>
		/// テクスチャ座標への比率
		/// </summary>
		public Point TextureRate;

		/// <summary>
		/// Calcの結果として得られる転送元矩形を表す点列
		/// </summary>
		public Point[] SrcPoints;

		/// <summary>
		/// Calcの結果として得られる転送先矩形を表す点列
		/// </summary>
		public Point[] DstPoints;

		/// <summary>
		/// Calcの結果として得られた合成変換
		/// (YanesdkResult.NoErrorが戻ってきたときのみ有効)
		/// 
		/// SrcPointsは、この変換のあとSrc側のクリッピング処理と、
		/// TextureRateを掛算してテクスチャ座標系に戻す処理が入る。
		/// </summary>
		public Affine2D TransAll;

		#endregion

		#region Calc method(これがすべて)
		/// <summary>
		/// 設定された各変換に基づいて
		/// SrcRectが変換される先を求める。
		/// 詳しくは、このクラス自体の説明を読むべし。
		/// </summary>
		/// <returns>転送先矩形が空集合の場合は
		/// YanesdkResult.InvalidParameterが返る。
		/// </returns>
		public YanesdkResult Calc()
		{
			//　転送元矩形の確定
			if (SrcWidth <= 0 || SrcHeight <= 0)
				return YanesdkResult.PreconditionError;

			Rect r = SrcRect != null
				? SrcRect.Clone() : new Rect(0, 0, SrcWidth, SrcHeight);

			// 右下の点は含まないのでそのための補正も行なう。
			// とは言ってもあえてその点を除外する必要はない。
			//	(理由はsubpixelを考えるとわかる。詳しくは割愛。)

			BltLayoutHelper bltLayout = new BltLayoutHelper();
			bltLayout.Rect1 = r;
			bltLayout.Layout = Layout;
			bltLayout.Update();
			Point layoutPoint = bltLayout.LayoutPoint;


			//　合成変換を求める。
			Affine2D a;
			if ((Trans0 == Trans0_ && Trans == Trans_))
			{
				//　前回計算した結果と同じなので計算する必要なし
				a = A_;
			}
			else
			{
				//　計算すると同時に計算結果をcacheしておく。
				a = A_ = Trans * Trans0;
				Trans0_ = Trans0;
				Trans_ = Trans;
			}

			// 転送先でのサイズが指定されているのか？
			if (DstSize != null)
			{
				//	このサイズに合わせて拡大縮小する変換が必要である。
				a = new Affine2D(DstSize.Cx/SrcWidth,DstSize.Cy/SrcHeight)*a;
			}
			
			// 合成変換 = DstPoint平行移動変換×DstSize変換×Trans×Trans0× -LayoutPoint平行移動変換
			//	A = { DstSize変換×Trans×Trans0 }
			a = Affine2D.Compose(DstPoint,
					Affine2D.Compose(a,-layoutPoint));

			// 正常に計算できたのでこの変換を保存しておく。
			TransAll = a;

			/*
			// 転送元でのclipping処理
			SrcPoints = new Point[4];
			SrcPoints[0] = new Point(left, top);
			SrcPoints[1] = new Point(right, top);
			SrcPoints[2] = new Point(right, bottom);
			SrcPoints[3] = new Point(left, bottom);

			DstPoints = new Point[4];

			// これらの点がはみ出ていれば押し戻して転送先の点を求める
			for (int i = 0; i < 4; ++i)
			{
				// affine変換を用いているので転送先のclippingが容易である
				Point p = SrcPoints[i];
				if (p.X < 0)
					p.X = 0;

				if (SrcWidth < p.X)
					p.X = SrcWidth;

				if (p.Y < 0)
					p.Y = 0;

				if (SrcHeight < p.Y)
					p.Y = SrcHeight;

				// これだけで転送先が求まる！
				DstPoints[i] = a * p;

				// テクスチャ座標に変換しておく必要がある。
				SrcPoints[i] = p * TextureRate;
			}
			*/

			return YanesdkResult.NoError;
		}
		#endregion

		#region private
		/// <summary>
		/// 計算量を減らすために前回計算した合成変換の結果をcacheしておく。
		/// 以下、privateなfieldはすべてそのためのもの。
		/// </summary>
		private Affine2D A_;
		private Affine2D Trans0_;
		private Affine2D Trans_;
		#endregion

	}
}
