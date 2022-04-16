using System;

using Yanesdk.System;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// Texture転送の基底クラス。
	/// </summary>
	/// <remarks>
	///
	///	<para>描画の基本的な考えかた。
	///
	///	ITextureは、転送元の描画矩形を保持しています。
	///	これは転送元矩形という言葉で表されます。
	///
	///	ただし、実際は矩形である必要はなく、 Textures のように、
	///	矩形の集合や、何らかの集合であっても構いません。概念的な矩形だと
	///	考えてください。
	///
	///	この転送元矩形を SDLWindow.blt メソッドによって、転送先に転送すると
	///	いう風に捉えます。転送先は、矩形、凸四角形、clipされた矩形etc..です。
	///
	///	文字列集合は、それぞれの文字 GlTexture の集合だと考えることが
	///	出来るでしょう。これが TexturVector の実体です。
	///
	///	よって、 Textures は、単なる矩形と考えて、 SDLWindow.blt を用いて
	///	GlTexture 同様に描画することができます。
	/// </para>
	/// </remarks>
	public interface ITexture : IDisposable {
		/// <summary>
		/// テクスチャの幅を得る。
		/// </summary>
		/// <remarks>
		/// floatにしているのは、テクスチャを拡大縮小して連結したりしたときに
		/// 端数が出うるから。
		/// </remarks>
		/// <returns></returns>
		float Width { get; }

		/// <summary>
		/// テスクチャの高さを得る。
		/// </summary>
		/// <remarks>
		/// floatにしているのは、テクスチャを拡大縮小して連結したりしたときに
		/// 端数が出うるから。
		/// </remarks>
		/// <returns></returns>
		float Height { get; }

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
		void Blt(DrawContext src, float x, float y);

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
		void Blt(DrawContext src, float x, float y, Rect srcRect);

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
		void Blt(DrawContext src, float x, float y, Rect srcRect, Size dstSize);

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
		void Blt(DrawContext src,Rect srcRect,Point[] dstPoint);

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
		void Blt(DrawContext src,Point[] srcPoint,Point[] dstPoint);

		/// <summary>
		/// Bitmapを設定できると便利。
		/// </summary>
		/// <param name="bmp"></param>
		/// <returns></returns>
		YanesdkResult SetBitmap(global::System.Drawing.Bitmap bmp);

		/// <summary>
		/// Surfaceも設定できると便利。
		/// </summary>
		/// <param name="surface"></param>
		/// <returns></returns>
		YanesdkResult SetSurface(Surface surface);

		/// <summary>
		/// 内部的に保持しているSurfaceを解放。
		/// 
		/// Disposeを呼び出した場合は、再度Loadすることは出来ない。
		/// これがReleaseとDisposeとの違い。
		/// </summary>
		void Release();

	}
}

