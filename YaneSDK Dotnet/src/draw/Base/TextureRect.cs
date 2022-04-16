using System;
using System.Collections.Generic;
using System.Text;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// テクスチャの内部の矩形を表現するテクスチャ。
	/// 
	/// 例)
	///    TextureRect tr = new TextureRect();
	///    tr.SetTexture(texture,new Rect(100,200,150,250));
	/// 
	///    screen.Blt(tr,20,30);
	///    // screen.Blt(texture,20,30,new Rect(100,200,150,250));と同義
	/// 
	/// スプライトを表示するのに使うと良い。
	/// </summary>
	public class TextureRect : ITexture
	{
		#region ctor
		/// <summary>
		/// のちほどSetTextureを行なうべし。
		/// </summary>
		public TextureRect() { }

		/// <summary>
		/// SetTextureを兼ねるコンストラクタ。
		/// </summary>
		public TextureRect(ITexture texture)
		{
			SetTexture(texture);
		}

		/// <summary>
		/// SetTextureを兼ねるコンストラクタ。
		/// </summary>
		public TextureRect(ITexture texture, Rect rect)
		{
			SetTexture(texture, rect);
		}

		/// <summary>
		/// SetTextureを兼ねるコンストラクタ。
		/// </summary>
		public TextureRect(ITexture texture, float offsetX, float offsetY)
		{
			SetTexture(texture, offsetX, offsetY);			
		}

		/// <summary>
		/// SetTextureを兼ねるコンストラクタ。
		/// </summary>
		public TextureRect(ITexture texture, Rect srcRect,float offsetX, float offsetY)
		{
			SetTexture(texture, rect,offsetX, offsetY);
		}

		#endregion

		#region ITextureの実装
		/*
		/// <summary>
		/// このテクスチャを指定のscreenにbltする。
		/// </summary>
		/// <param name="drawContext"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <remarks>
		/// visitorパターンのために実装してある。
		/// SDLWindow.bltからVisitorとして呼び出される。
		/// </remarks>
		public void Blt(DrawContext context, float x, float y)
		{
			texture.Blt(context, x +DstOffsetX, y + DstOffsetY, rect);
		}

		/// <summary>
		/// このテクスチャを指定のscreenにbltする。
		/// </summary>
		/// <param name="drawContext"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		/// <remarks>
		/// visitorパターンのために実装してある。
		/// SDLWindow.bltからVisitorとして呼び出される。
		/// </remarks>
		public void Blt(DrawContext src, float x, float y, Rect srcRect)
		{
			// このsrcRectはrectのなかでの座標を意味するので座標変換を施す必要がある。
			Rect r = Rect.CalcRectInRect(rect, srcRect);

			texture.Blt(src, x + DstOffsetX, y + DstOffsetY, r);
		}
		*/

		/// <summary>
		/// このテクスチャを指定のscreenにbltする。
		/// </summary>
		/// <param name="drawContext"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstSize"></param>
		/// <remarks>
		/// visitorパターンのために実装してある。
		/// SDLWindow.bltからVisitorとして呼び出される。
		/// </remarks>
		public void Blt(DrawContext src, float x, float y, Rect srcRect, Size dstSize)
		{
			// このsrcRectはrectのなかでの座標を意味するので座標変換を施す必要がある。
		//	Rect r = Rect.CalcRectInRect(rect, srcRect);
			Rect r = srcRect;

			texture.Blt(src, x + DstOffsetX, y + DstOffsetY, r, dstSize);
		}

		/// <summary>
		/// このテクスチャを指定のscreenにbltする。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstPoint"></param>
		/// <remarks>
		/// visitorパターンのために実装してある。
		/// SDLWindow.bltからVisitorとして呼び出される。
		/// </remarks>
		public void Blt(DrawContext src, Rect srcRect, Point[] dstPoint)
		{
			// このsrcRectはrectのなかでの座標を意味するので座標変換を施す必要がある。
		//	Rect r = Rect.CalcRectInRect(rect, srcRect);
			Rect r = srcRect;

			Point[] dstPoint_ = new Point[dstPoint.Length];
			for (int i = 0; i < dstPoint_.Length; ++i)
			{
				dstPoint_[i].X = dstPoint[i].X + DstOffsetX;
				dstPoint_[i].Y = dstPoint[i].Y + DstOffsetY;
			}

			texture.Blt(src, r, dstPoint_);
		}

		/// <summary>
		/// このテクスチャを指定のscreenにbltする。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcPoint"></param>
		/// <param name="dstPoint"></param>
		/// <remarks>
		/// visitorパターンのために実装してある。
		/// SDLWindow.bltからVisitorとして呼び出される。
		/// これ、 Textures では未実装。(面倒だから)
		/// </remarks>
		public void Blt(DrawContext src, Point[] srcPoint, Point[] dstPoint)
		{
			if (srcPoint == null)
				Blt(src, (Rect)null, dstPoint);

			// このsrcPointはrectのなかでの座標を意味するので座標変換を施す必要がある。

			// outRectのWidth,Heightがマイナスの値であることを考慮して…
			int sw = global::System.Math.Sign(rect.Right-rect.Left);
			int sh = global::System.Math.Sign(rect.Bottom-rect.Top);

			Point[] srcPoint_ = new Point[srcPoint.Length];

			for(int i=0;i<srcPoint.Length;++i)
			{
				srcPoint_[i].X = rect.Left + sw * srcPoint[i].X;
				srcPoint_[i].Y = rect.Top + sh * srcPoint[i].Y;
			}

			Point[] dstPoint_ = new Point[dstPoint.Length];
			for (int i = 0; i < dstPoint_.Length; ++i)
			{
				dstPoint_[i].X = dstPoint[i].X + DstOffsetX;
				dstPoint_[i].Y = dstPoint[i].Y + DstOffsetY;
			}

			texture.Blt(src, srcPoint_, dstPoint_);
		}

		/// <summary>
		/// Bitmapを設定できると便利。
		/// </summary>
		/// <param name="bmp"></param>
		/// <returns></returns>
		public YanesdkResult SetBitmap(global::System.Drawing.Bitmap bmp)
		{
			return YanesdkResult.NotImplemented;
		}

		/// <summary>
		/// Surfaceも設定できると便利。
		/// </summary>
		/// <param name="surface"></param>
		/// <returns></returns>
		public YanesdkResult SetSurface(Surface surface)
		{
			return YanesdkResult.NotImplemented;
		}

		/// <summary>
		/// 内部的に保持しているSurfaceを解放。
		/// 
		/// Disposeを呼び出した場合は、再度Loadすることは出来ない。
		/// これがReleaseとDisposeとの違い。
		/// </summary>
		public void Release()
		{
			texture.Release();
		}

		/// <summary>
		/// 解体する。通常呼び出す必要はない。
		/// </summary>
		public void Dispose()
		{
			texture.Dispose();
		}

		#endregion

		#region methods

		/// <summary>
		/// テクスチャと、そのどの部分をこのテクスチャとして扱うのかを設定する。
		/// 
		/// rect == nullであればtexture全体。
		/// OffsetX = OffsetY = 0にする。
		/// 
		/// ここで指定するoffsetX,offsetYはこのクラスのプロパティである
		/// OffsetX,OffsetY。
		/// </summary>
		/// <param name="texture"></param>
		/// <param name="rect"></param>
		public void SetTexture(ITexture texture, Rect rect)
		{
			this.texture = texture;
			this.rect = rect;
			this.offsetX = this.offsetY = 0;
		}

		/// <summary>
		/// SetTexture(texture,null)と同義。
		/// </summary>
		/// <param name="texture"></param>
		public void SetTexture(ITexture texture)
		{
			this.texture = texture;
			this.rect = null;
			this.offsetX = this.offsetY = 0;
		}

		/// <summary>
		/// テクスチャと、そのどの部分をこのテクスチャとして扱うのかを設定する。
		/// 
		/// ここで指定するoffsetX,offsetYはこのクラスのプロパティである
		/// OffsetX,OffsetY。
		/// </summary>
		public void SetTexture(ITexture texture, Rect rect, float offsetX, float offsetY)
		{
			this.texture = texture;
			this.rect = rect;
			this.offsetX = offsetX;
			this.offsetY = offsetY;
		}

		/// <summary>
		/// テクスチャと、そのどの部分をこのテクスチャとして扱うのかを設定する。
		/// 
		/// ここで指定するoffsetX,offsetYはこのクラスのプロパティである
		/// OffsetX,OffsetY。
		/// </summary>
		public void SetTexture(ITexture texture, float offsetX, float offsetY)
		{
			this.texture = texture;
			this.rect = null;
			this.offsetX = offsetX;
			this.offsetY = offsetY;
		}


		#endregion

		#region properties
		/// <summary>
		/// 保持しているテクスチャ
		/// </summary>
		public ITexture Texture
		{
			get { return texture; }
			set { texture = value; }
		}
		private ITexture texture;

		/// <summary>
		/// このテクスチャの幅
		/// 
		/// Abs(right-left)を行なっているのでマイナスは有りえない。
		/// </summary>
		public float Width
		{
			get
			{
				if (rect == null)
					return 0;
				return global::System.Math.Abs(rect.Right - rect.Left);
			//	return rect.Right - rect.Left;
			}
		}

		/// <summary>
		/// このテクスチャの高さ
		/// 
		/// Abs(bottom-top)を行なっているのでマイナスはあり得ない
		/// </summary>
		public float Height
		{
			get
			{
				if (rect == null)
					return 0;
				return global::System.Math.Abs(rect.Bottom - rect.Top);
			//	return rect.Bottom - rect.Top;
			}
		}

		/// <summary>
		/// textureのどの矩形をこのテクスチャは表現しているのかを取得する。
		/// これはSetTextureで設定した値。nullであればTexture全体。
		/// </summary>
		public Rect Rect
		{
			get { return rect; }
		}
		/// <summary>
		/// textureのどの矩形をこのテクスチャは表現しているのか。
		/// </summary>
		private Rect rect = null;

		/// <summary>
		/// 描画するときのオフセット量
		/// </summary>
		public float OffsetX
		{
			get { return offsetX; }
			set { offsetX = value; }
		}
		private float offsetX = 0;

		/// <summary>
		/// 描画するときのオフセット量
		/// </summary>
		public float OffsetY
		{
			get { return offsetY; }
			set { offsetY = value; }
		}
		private float offsetY = 0;

		/// <summary>
		/// 描画座標
		/// 
		/// OffsetX,Yと同じような意味だが、Offsetはスプライトに付随する
		/// 情報であり、通例、変更しない。こちらのX,Yは、スプライトの描画位置を
		/// 表すものであり、変更しながら使う。
		/// </summary>
		public float X
		{
			get { return x; }
			set { x = value; }
		}
		private float x=0;

		/// <summary>
		/// 描画座標
		/// 
		/// OffsetX,Yと同じような意味だが、Offsetはスプライトに付随する
		/// 情報であり、通例、変更しない。こちらのX,Yは、スプライトの描画位置を
		/// 表すものであり、変更しながら使う。
		/// </summary>
		public float Y
		{
			get { return y; }
			set { y = value; }
		}
		private float y=0;

		/// <summary>
		/// スプライトとしてマップ上などに描画するときの
		/// 足元のライン。描画座標Y + BaseLineを基準にソートして並び替えを
		/// 行なって描画する。
		/// </summary>
		public float BaseLine
		{
			get { return baseLine; }
			set { baseLine = value; }
		}
		private float baseLine = 0;

		#endregion

		#region private
		/// <summary>
		/// 描画先のオフセット値
		/// </summary>
		public float DstOffsetX { get { return x + OffsetX; } }

		/// <summary>
		/// 描画先のオフセット値
		/// </summary>
		public float DstOffsetY { get { return y + OffsetY; } }
		#endregion
	}
}
