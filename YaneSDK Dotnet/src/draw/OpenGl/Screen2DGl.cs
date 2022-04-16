using System;
using System.Collections.Generic;
using System.Text;

using OpenGl;
using Yanesdk.Ytl;
using Yanesdk.Math;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Yanesdk.Draw
{
	/// <summary>
	/// レンダリングスクリーン
	/// </summary>
	/// <remarks>
	/// こいつに対してbltすれば画面に表示される。
	/// 
	/// このクラスが保持しているのは、opengl依存の部分。(Windows/Linux環境共通)
	/// 2Dの描画に必要なものだけ抽出してある。
	/// 
	/// このあと
	/// 
	///		IScreen (2D/3D共通interfaceクラス)
	/// 
	///		IScreen2D (2D用interfaceクラス)
	///		 Screen2DDX (DirectXを用いた2D描画)
	/// 
	///		IScreen3D (3D用interfaceクラス)
	///			Screen3DGl (OpenGLを用いた3D描画)
	///			Screen3DDX (DirectXを用いた3D描画)
	///	
	/// を用意する予定。
	/// 
	/// </remarks>
	public class Screen2DGl : IScreen2D , IDisposable
	{
		#region ctor & Dispose
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public Screen2DGl()
		{
			color.R = color.G = color.B = 255; color.A = 255;
		}
		/// <summary>
		/// SDL Videoの初期化用
		/// </summary>
		private RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>
			init = new RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>();

		public void Dispose()
		{
			// SelectしていればUnselectを呼び出すべき
			if (selected)
				Unselect();

			init.Dispose();
		}

		#endregion

		#region Select/Unselect/Update
		/// <summary>
		/// このスクリーンに対して描画を開始するときに呼び出す
		/// </summary>
		/// <remarks>
		/// select～updateまでが一連の描画である。
		/// 
		/// また、HDCWindowを用いる場合、focusのあるopenglに
		/// テクスチャを読み込ませる必要があるため、bltしたいHDCWindowの
		/// openglをactiveにしておき読み込ませる必要がある。
		/// 
		/// hdcWindow.screen.select();
		/// texture.load("file.jpg");
		/// hdcWindow.screen.unselect(); // select～unselectでtextureを読み込む
		/// </remarks>
		public void Select()
		{
		//	Debug.Assert(!selected, "Screen2DGlでSelectしてるのにSelectが呼び出された");
			// ↑このassertにひっかかっているときにメッセージループがまわって
			// また呼び出されるのってすごく嫌なんだけど(´ω`)
			// ↓こう書きなおす。

			if (selected)
			{
				selected = false;
				Debug.Assert(!selected, "Screen2DGlでSelectしてるのにSelectが呼び出された");
			}

			if (BeginDrawDelegate != null)
				BeginDrawDelegate();

			// ブレンド状態を保持しないだろうから、
			// ここで再度設定してやる必要がある。
			if (bBlend)
			{
				Gl.glEnable(Gl.GL_BLEND);
			}
			else
			{
				Gl.glDisable(Gl.GL_BLEND);
			}

			Gl.glEnable(Gl.GL_TEXTURE_2D);
			// ↑これ、wglMakeCurrentを呼び出すとリセットされる(´ω`)

			DrawContext.Update(); // 視点等を再度初期化しなければ。

			selected = true;
		}

		private bool selected = false;

		/// <summary>
		/// selectを解除する。描画は行なわない。
		/// </summary>
		public void Unselect()
		{
			if (QuitDrawDelegate != null)
				QuitDrawDelegate();

			Debug.Assert(selected, "Screen2DGlでSelectしていないのにUnselectが呼び出された");
			selected = false;
		}

		///	<summary>実画面に描画を行なう(2D/3D)。</summary>
		///	<remarks>このメソッドを呼び出したときにselectを解除する。
		/// ビデオドライバの実装によっては、このメソッドによって、
		/// 垂直帰線待ちになることがある。よって、画面のリフレッシュレート以上は
		/// 出ないことがあると考えるべき。
		///	</remarks>
		public YanesdkResult Update()
		{
		//	if (!bTestScreenSuccess)
		//		return YanesdkResult.PreconditionError; // サーフェスーが構築されてない

			//	openGLの場合は、SDL_UpdateRectではいけない
			Gl.glFlush();
			
			// SDL.SDL_GL_SwapBuffers();
			//  ↑これを呼び出してしまうとSdl依存になってしまう

			if (EndDrawDelegate!=null)
				EndDrawDelegate();

			Debug.Assert(selected, "Screen2DGlでSelectしていないのにUpdateが呼び出された");
			selected = false;

			return YanesdkResult.NoError;
		}

		public delegate void RenderingDelegate();

		/// <summary>
		/// select(描画開始)のときに呼び出されるdelegate
		/// </summary>
		/// <remarks>
		/// このdelegateはSDLWindow,HDCWindowを使うならばそれらが設定してくれる。
		/// </remarks>
		public RenderingDelegate BeginDrawDelegate;

		/// <summary>
		/// updateで呼び出されるdelegate
		/// </summary>
		/// <remarks>
		/// このdelegateはSDLWindow,HDCWindowを使うならばそれらが設定してくれる。
		/// </remarks>
		public RenderingDelegate EndDrawDelegate;

		/// <summary>
		/// unselectで呼び出されるdelegate
		/// </summary>
		/// <remarks>
		/// このdelegateはSDLWindow,HDCWindowを使うならばそれらが設定してくれる。
		/// </remarks>
		public RenderingDelegate QuitDrawDelegate;

		#endregion

		#region IScreen2Dの実装(違うのも混じってるけど)

		///	<summary>画面のクリア(2D/3D)。</summary>
		/// <remarks>
		///	画面をクリアする色は、 setClearColor で設定された色
		/// </remarks>
		public void Clear()
		{
			Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
			/*
				GL_COLOR_BUFFER_BIT 
				Indicates the buffers currently enabled for color writing. 
				GL_DEPTH_BUFFER_BIT 
				Indicates the depth buffer. 
				GL_ACCUM_BUFFER_BIT 
				Indicates the accumulation buffer. 
				GL_STENCIL_BUFFER_BIT 
				Indicates the stencil buffer. 
			*/
		}
		///	<summary>clearするときの色を設定する(2D/3D)。</summary>
		/// <remarks> 
		///	rgbaは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		public void SetClearColor(int r, int g, int b, int a)
		{
			Gl.glClearColor(((float)r) / 255, ((float)g) / 255, ((float)b) / 255, ((float)a) / 255);
		}

		///	<summary>clearするときの色を設定する(2D/3D)。</summary>>
		/// <remarks>
		///	rgbは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		public void SetClearColor(int r, int g, int b)
		{
			SetClearColor(r, g, b, 0);
		}

		///	<summary>直線を引く(2D)。</summary>
		/// <remarks>
		///	※描画は二次元的な描画なのであとから描画したものが前面に描画される。
		///	DrawContext に基づくclipping、座標系の変換を行なう。
		/// 線の太さはSetLineWidthで設定する。
		/// </remarks>
		public void DrawLine(int x1, int y1, int x2, int y2)
		{
			DrawContext dc = DrawContext;

			Gl.glBegin(Gl.GL_LINES);
			Gl.glVertex2f(x1 * dc.RateX + dc.OffsetRX + 0.5f , y1 * dc.RateY + dc.OffsetRY + 0.5f);
			Gl.glVertex2f(x2 * dc.RateX + dc.OffsetRX + 0.5f , y2 * dc.RateY + dc.OffsetRY + 0.5f);
			Gl.glEnd();
		}

		/// <summary>
		/// 線を引く。線の太さはSetLineWidthで設定する。
		/// </summary>
		/// <param name="lines"></param>
		/// <param name="count"></param>
		private void DrawLines(float[] lines, int count)
		{
			drawLines_(lines, count);
		}
		// こんなショーモナイ　メソッドいらんやろ(´ω`)
		
		unsafe private void drawLines_(float[] lines, int count)
		{
			//	現在のcontextに即して変換する必要がある。
			DrawContext dc = DrawContext;

			for (int i = 0; i < lines.Length; i += 2)
			{
				lines[i + 0] = lines[i + 0] * dc.RateX + dc.OffsetRX + 0.5f;
				lines[i + 1] = lines[i + 1] * dc.RateY + dc.OffsetRY + 0.5f;
			}

			byte[] indeces = new byte[count / 2];
			for (int i = 0; i < indeces.Length; ++i)
				indeces[i] = (byte)i;

			Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);
			fixed (float* v = &lines[0])
			fixed (byte* d = &indeces[0])
			{
				Gl.glVertexPointer(2, Gl.GL_FLOAT, 0, (IntPtr)v);
				Gl.glDrawElements(Gl.GL_LINES, indeces.Length, Gl.GL_UNSIGNED_BYTE, (IntPtr)d);
			}
		}

		///	<summary>三角形ポリゴンを描く(2D)。</summary>
		/// <remarks>
		///	3点を繋ぐようにした三角形を描く。中は、塗りつぶし。
		///	※描画は二次元的な描画なのであとから描画したものが前面に描画される。
		///	DrawContext に基づくclipping、座標系の変換を行なう。
		/// </remarks>
		public void DrawPolygon(int x1, int y1, int x2, int y2, int x3, int y3)
		{
			//	現在のcontextに即して変換する必要がある。
			DrawContext dc = DrawContext;
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2f(x1 * dc.RateX + dc.OffsetRX , y1 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x2 * dc.RateX + dc.OffsetRX, y2 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x3 * dc.RateX + dc.OffsetRX, y3 * dc.RateY + dc.OffsetRY);
			Gl.glEnd();
		}

		///	<summary>長方形ポリゴンを描く(2D)。</summary>
		/// <remarks>
		///	4点を繋ぐようにした四角形を描く。要するに凸4角形。中は、塗りつぶし。
		///	※描画は二次元的な描画なのであとから描画したものが前面に描画される。
		///	DrawContext に基づくclipping、座標系の変換を行なう。
		/// </remarks>
		public void DrawPolygon(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4)
		{
			//	現在のcontextに即して変換する必要がある。
			DrawContext dc = DrawContext;
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2f(x1 * dc.RateX + dc.OffsetRX , y1 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x2 * dc.RateX + dc.OffsetRX, y2 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x3 * dc.RateX + dc.OffsetRX, y3 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x4 * dc.RateX + dc.OffsetRX, y4 * dc.RateY + dc.OffsetRY);
			Gl.glEnd();
		}

		///	<summary>凸5角形を描く。(2D)。</summary>
		/// <remarks>
		///	5点を繋ぐようにした四角形を描く。要するに凸5角形。中は、塗りつぶし。
		///	※描画は二次元的な描画なのであとから描画したものが前面に描画される。
		///	DrawContext に基づくclipping、座標系の変換を行なう。
		/// </remarks>
		public void DrawPolygon(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, int x5, int y5)
		{
			//	現在のcontextに即して変換する必要がある。
			DrawContext dc = DrawContext;
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glVertex2f(x1 * dc.RateX + dc.OffsetRX, y1 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x2 * dc.RateX + dc.OffsetRX, y2 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x3 * dc.RateX + dc.OffsetRX, y3 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x4 * dc.RateX + dc.OffsetRX, y4 * dc.RateY + dc.OffsetRY);
			Gl.glVertex2f(x5 * dc.RateX + dc.OffsetRX, y5 * dc.RateY + dc.OffsetRY);
			Gl.glEnd();
		}

		///	<summary>色を設定する(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を設定する。
		///	rgbは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		public void SetColor(int r, int g, int b)
		{
			Gl.glColor4ub((byte)r, (byte)g, (byte)b, 255);
			color.R = (byte)r; color.G = (byte)g; color.B = (byte)b; color.A = 255;
		}

		/// <summary>
		///	色を設定する(2D/3D)
		/// </summary>
		/// <remarks>
		/// ラインやポリゴンの描画色を設定する。
		/// </remarks>
		/// <param name="c"></param>
		public void SetColor(Color4ub c)
		{
			Gl.glColor4ub(c.R, c.G, c.B, c.A);
			color = c;
		}

		///	<summary>色を設定する(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を設定する。
		///	rgbは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		public void SetColor(int r, int g, int b, int a)
		{
			Gl.glColor4ub((byte)r, (byte)g, (byte)b, (byte)a);
			color.R = (byte)r; color.G = (byte)g; color.B = (byte)b; color.A = (byte)a;
		}

		///	<summary>色をリセットする(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を(255,255,255)に設定する。
		///	テクスチャー貼り付けのときなど、setColorの値で乗算されるので、
		///	そのままの画像を表示したいならば(255,255,255)に設定しておく
		///	必要があるため。
		/// </remarks>
		public void ResetColor()
		{
			SetColor(255, 255, 255);
		}

		///	<summary>色をセットする(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を(a,a,a)に設定する。
		///	aの範囲は0～255。
		///	テクスチャー貼り付けのときなど、setColorの値で乗算されるので、
		///	fade(薄く描画)したければ、(a,a,a)に設定する必要があるため。
		/// </remarks>
		public void SetColor(int a)
		{
			SetColor(a, a, a);
		}

		/// <summary>
		/// setColorで設定されている色の取得。
		/// </summary>
		/// <param name="r_"></param>
		/// <param name="g_"></param>
		/// <param name="b_"></param>
		public void GetColor(out int r, out int g, out int b)
		{
			r = color.R; g = color.G; b = color.B;
		}

		/// <summary>
		/// setColorで設定されている色の取得。
		/// </summary>
		/// <param name="r_"></param>
		/// <param name="g_"></param>
		/// <param name="b_"></param>
		/// <param name="a_"></param>
		public void GetColor(out int r, out int g, out int b, out int a)
		{
			r = color.R; g = color.G; b = color.B; a = color.A;
		}
		///	<summary>画面にオフセットを加える(2D)。</summary>
		/// <remarks>
		///	表示する視界に2次元オフセット(ox,oy)を加える。
		///	(100,200)を設定すると、画面下が(100,200),画面右上が(100+640,200+480)
		///	の意味になる。画面を揺らしたりするときに使うと良い。
		/// </remarks>
		public void SetScreenOffset(int ox, int oy)
		{
			offsetX = ox;
			offsetY = oy;
			UpdateView();
		}

		///	<summary>視体積の設定(2D)。</summary>
		/// <remarks>
		///	ウィンドゥの左上が(x1,y1),右下が(x2,y2)になるように設定される。
		///	setScreenOffset,setVideoMode等で内部的に呼び出される
		/// </remarks>
		public void SetView(int x1, int y1, int x2, int y2)
		{
			Gl.glLoadIdentity();
			Gl.glOrtho(x1, x2, y2, y1, 0, 256);	//	0～256 = depth
		}

		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	テクスチャーを(0,0)に等倍で描画する。
		/// テクスチャーの色には、直前のsetColorの値が乗算される。
		///
		///	色の変更　→　setColor(r,g,b)/setColor(a)/resetColor()
		///	ブレンド方法変更　→　blendなんちゃら関数 
		///
		///	※　テクスチャーを描画するとき、
		///	glBindTextureを内部的に呼び出すので、これを設定している人は注意。
		/// 描画を転送先でclipしたいときは、 getDrawContext で
		/// 描画コンテクストを書き換えればok。
		/// 
		/// Q)このメソッドは描画すべき座標をなぜ指定できないのか
		/// A)このメソッドはSpriteクラスのように内部的に座標を保持できるクラスで
		/// 用いることを想定している。あるいは本当に(0,0)にBGを描画したいときに用いると良い。
		/// </remarks>
		public void Blt(ITexture src)
		{
			Blt(src, 0, 0);
		}

		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	テクスチャーを(x,y)に等倍で描画する。
		///	テクスチャーの色には、直前のsetColorの値が乗算される。
		///
		///	色の変更　→　setColor(r,g,b)/setColor(a)/resetColor()
		///	ブレンド方法変更　→　blendなんちゃら関数 
		///
		///	※　テクスチャーを描画するとき、
		///	glBindTextureを内部的に呼び出すので、これを設定している人は注意。
		/// 描画を転送先でclipしたいときは、 getDrawContext で
		/// 描画コンテクストを書き換えればok。
		/// </remarks>
		public void Blt(ITexture src, int x, int y)
		{
			if (src != null)
				src.Blt(DrawContext, x, y);
		}

		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	<para>bltの転送元矩形の指定できるバージョン。
		///	srcRectがnullのときはソース全域。
		/// </para>
		/// <para>
		///	高速化のため転送元矩形が転送元サーフェースからハミ出る場合の
		///	処理はしていないので、何とかしる。
		/// </para>
		/// <para>
		///	転送元矩形として、(Right,Top,Left,Bottom)を指定すれば
		///	左右反転して表示される。(Left,Bottom,Right,Top)を指定すれば
		///	上下反転して表示される。
		///	</para>
		/// </remarks>
		public void Blt(ITexture src, int x, int y, Rect srcRect)
		{
			if (src != null)
				src.Blt(DrawContext, x, y, srcRect);
		}

		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	bltの転送元矩形と転送先サイズの指定できるバージョン。
		///	srcRectがnullのときはソース全域。
		///	dstSizeがnullのときは転送先全域(のサイズ)。
		///
		///	転送元矩形として、(Right,Top,Left,Bottom)を指定すれば
		///	左右反転して表示される。(Left,Bottom,Right,Top)を指定すれば
		///	上下反転して表示される。
		/// </remarks>
		public void Blt(ITexture src, int x, int y, Rect srcRect, Size dstSize)
		{
			if (src != null)
				src.Blt(DrawContext, x, y, srcRect, dstSize);
		}

		///	<summary>画像の描画(2D)</summary>
		/// <remarks>
		///	bltの転送元矩形と転送先4点の指定できるバージョン。
		///	srcRectがnullのときはソース全域。
		///	転送元矩形が、転送先として指定された4点に移動。
		///
		///	転送先は、
		///	point[0]が左上、point[1]が右上、point[2]が右下、point[3]が左下
		///	の点を指定しているものとする。
		/// </remarks>
		/// <example>
		/// <code>
		///	Screen screen = new Screen;
		///	screen.setVideoMode(640,480,0);
		///	GlTexture tex = new GlTexture;
		///	tex.load("title.png");
		///
		///	while (GameFrame.pollEvent()==0){
		///
		///		screen.clear();
		///
		///		Point[4] points;
		///		points[0].setPoint(100,100);
		///		points[1].setPoint(400,100);
		///		points[2].setPoint(640,480);
		///		points[3].setPoint(0,480);
		///		screen.blt(tex2,0,0,null,points);	//	台形描画
		///
		///		screen.Update();
		///	}
		///	</code>
		///	</example>
		public void Blt(ITexture src, Rect srcRect, Point[] point)
		{
			if (src != null)
				src.Blt(DrawContext, srcRect, point);
		}

		/// <summary>
		/// 凸四角形→凸四角形への転送。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcPoint"></param>
		/// <param name="dstPoint"></param>
		public void Blt(ITexture src, Point[] srcPoint, Point[] dstPoint)
		{
			if (src != null)
				src.Blt(DrawContext, srcPoint, dstPoint);
		}


		/// <summary>
		/// bltの回転機能つき。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="rad"></param>
		/// <param name="rate"></param>
		/// <param name="base_"></param>
		/// <remarks>
		/// <para>
		/// 指定のテクスチャを転送先の(x,y)にrad回して描画します。
		/// </para>
		/// <para>
		/// radの単位は、0～512で一周(2π)となる角度。
		/// 	回転の方向は、右まわり（時計まわり）。
		/// rateは、拡大率。1.0ならば等倍。	
		/// </para>
		/// <para>
		/// basePosは転送元画像どの点が(x,y)に来るかを指定します。
		/// 	0 : 左上。		1 : 上辺の中点　2:右上
		/// 	3 : 左辺の中点	4 : 画像中心	5:右辺の中点	
		/// 	6 : 左下。　	7 : 下辺の中点	8:右下
		/// </para>
		/// </remarks>
		public void BltRotate(ITexture src, int x, int y, int rad, float rate, int basePos)
		{
			if (src == null) return;
			int w = (int)src.Width;
			int h = (int)src.Height;
			switch (basePos)
			{
				case 0: w = 0; h = 0; break;
				case 1: w /= 2; h = 0; break;
				case 2: h = 0; break;
				case 3: w = 0; h /= 2; break;
				case 4: w /= 2; h /= 2; break;
				case 5: h /= 2; break;
				case 6: w = 0; break;
				case 7: w /= 2; break;
				case 8: break;
				default:
					//		throw new YanesdkException(this, String.Format("unknown base_({0})", base_));
					return; // 例外きもちわるす
			}
			BltRotate(src, (int)(x - rate * w), (int)(y - rate * h), null, rad, rate, w, h);
		}

		/// <summary>
		/// bltの回転機能つき。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="rad"></param>
		/// <param name="rate"></param>
		/// <param name="bx"></param>
		/// <param name="by"></param>
		/// <remarks>
		/// <para>
		/// 指定のテクスチャを転送先の(x,y)にrad回して描画します。
		/// </para>
		/// <para>
		/// radの単位は、0～512で一周(2π)となる角度。
		/// 	回転の方向は、右まわり（時計まわり）。
		/// rateは、拡大率。1.0ならば等倍。
		/// (bx,by)は転送元の回転中心。(x,y)の地点が転送元の画像中心になるように
		/// したいならば、(x-bx*rate,y-by*rate)をこのメソッドのx,yに渡す必要がある。	
		/// </para>
		/// </remarks>
		public void BltRotate(ITexture src, int x, int y, int rad, float rate, int bx, int by)
		{
			BltRotate(src, x, y, null, rad, rate, bx, by);
		}

		/// <summary>
		/// bltの回転機能つき。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		/// <param name="rad"></param>
		/// <param name="rate"></param>
		/// <param name="bx"></param>
		/// <param name="by"></param>
		/// <remarks>
		/// <para>
		/// 指定のテクスチャを転送先の(x,y)に半径rで描画します。
		/// </para>
		/// <para>
		/// radの単位は、0～512で一周(2π)となる角度。
		/// 	回転の方向は、右まわり（時計まわり）。
		/// rateは、拡大率。1.0ならば等倍。
		/// (bx,by)は転送元の画像の、回転中心。
		/// srcRectは転送元矩形。nullならば転送元テクスチャ全体。
		/// </para>
		/// </remarks>
		public void BltRotate(ITexture src, int x, int y, Rect srcRect, int rad, float rate, int bx, int by)
		{
			if (src == null) return;

			int sx, sy;	//	転送元サイズ

			if (srcRect == null)
			{
				srcRect = new Rect(0, 0, src.Width, src.Height);
			}	

			{
				sx = (int)(srcRect.Right - srcRect.Left);
				sy = (int)(srcRect.Bottom - srcRect.Top);
			}

			if (sx == 0 || sy == 0) return;

			int dx, dy;	//	転送先サイズ
			dx = (int)(sx * rate);
			dy = (int)(sy * rate);
			if (dx == 0 || dy == 0) return;

			// 転送元の回転中心
			bx = (int)(bx * rate);
			by = (int)(by * rate);

			//	転送後の座標を計算する
			int nSin = SinTable.Instance.Sin(rad);
			int nCos = SinTable.Instance.Cos(rad);

			Point[] dstPoints = new Point[4];

			//	0.5での丸めのための修正項 →　0x8000
			int px = x + bx;
			int py = y + by;

			int ax0 = -bx;
			int ay0 = -by;
			dstPoints[0].X = Round.RShift((int)(ax0 * nCos - ay0 * nSin), 16) + px;
			dstPoints[0].Y = Round.RShift((int)(ax0 * nSin + ay0 * nCos), 16) + py;
			int ax1 = dx - bx;
			int ay1 = -by;
			dstPoints[1].X = Round.RShift((int)(ax1 * nCos - ay1 * nSin), 16) + px;
			dstPoints[1].Y = Round.RShift((int)(ax1 * nSin + ay1 * nCos), 16) + py;
			int ax2 = dx - bx;
			int ay2 = dy - by;
			dstPoints[2].X = Round.RShift((int)(ax2 * nCos - ay2 * nSin), 16) + px;
			dstPoints[2].Y = Round.RShift((int)(ax2 * nSin + ay2 * nCos), 16) + py;
			int ax3 = -bx;
			int ay3 = dy - by;
			dstPoints[3].X = Round.RShift((int)(ax3 * nCos - ay3 * nSin), 16) + px;
			dstPoints[3].Y = Round.RShift((int)(ax3 * nSin + ay3 * nCos), 16) + py;
			//	変数無駄な代入が多いが、最適化されるかなぁ(´Д｀)

			src.Blt(DrawContext, srcRect, dstPoints);
		}

		///	<summary>テクスチャーのバインドを解除する。</summary>
		/// <remarks>
		///	GlTexture.bind();
		///	でバインドしていたテクスチャーをunbindする。
		/// </remarks>
		public void Unbind()
		{
			Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
		}

		///	<summary>ブレンドを有効化/無効化する。</summary>
		/// <remarks>
		/// Blend = falseとすると、
		///	blt メソッド等で描画するときのblendを無効化する。
		///	(ディフォルトでは無効)
		/// 
		/// 以下、OpenGLの説明から抜粋。
		/// 
		///	<code>glBlendFunc(srcFunc,dstFunc);</code>
		///	//	転送元に適用する関数 , 転送先に適応する関数
		/// 
		/// <example>
		///	例)
		///	<code>glBlendFunc(GL_DST_COLOR,GL_ZERO);</code>
		///	ならば、
		///		転送先のピクセルの値
		///			＝　転送元のピクセル×転送先のピクセル(GL_DST_COLOR)
		///				＋　転送先のピクセル×０(GL_ZERO)
		///			＝　転送元のピクセル×転送先のピクセル
		///	となり、転送元ピクセルで抜いたような形になる。
		///
		///	指定できる定数は、以下の通り。
		///		定数 (fR,fG,fB,fA) 
		///	<code>
		///		GL_ZERO (0,0,0,0) 
		///		GL_ONE (1,1,1,1) 
		///		GL_SRC_COLOR (Rs/kR ,Gs/kG,Bs/kB,As/kA) 
		///		GL_ONE_MINUS_SRC_COLOR (1,1,1,1)-(Rs/kR,Gs/kG,Bs/kB,As/kA) 
		///		GL_DST_COLOR (Rd/kR,Gd/kG,Bd/kB,Ad/kA) 
		///		GL_ONE_MINUS_DST_COLOR (1,1,1,1)-(Rd/kR,Gd/kG,Bd/kB,Ad/kA) 
		///		GL_SRC_ALPHA (As/kA,As/kA,As/kA,As/kA) 
		///		GL_ONE_MINUS_SRC_ALPHA (1,1,1,1)-(As/kA,As/kA,As/kA,As/kA) 
		///		GL_DST_ALPHA (Ad/kA,Ad/kA,Ad/kA,Ad/kA) 
		///		GL_ONE_MINUS_DST_ALPHA (1,1,1,1)-(Ad/kA,Ad/kA,Ad/kA,Ad/kA) 
		///		GL_SRC_ALPHA_SATURATE (i,i,i,1) 
		///		</code>
		///	s : source(転送元) , d : destination(転送先)
		///	kR,kG,kB,kA : 255(RGBA8888のとき)
		///	i : min(As, kA-Ad) / kA
		///
		///		<code>glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);</code>
		///	なら、
		///		転送先　＝
		///			転送元の値×転送元のα　＋　転送先の値×（１－転送元のα）
		///	となり、いわゆる普通のαブレンドになる
		/// </example>
		/// </remarks>
		public bool Blend
		{
			get { return bBlend; }
			set {
				if (value)
				{
					if (!bBlend)
					{
						Gl.glEnable(Gl.GL_BLEND);
						bBlend = true;
					}
				}
				else
				{
					if (bBlend)
					{
						Gl.glDisable(Gl.GL_BLEND);
						bBlend = false;
					}
				}
			}
		}

		/// <summary>
		///  ブレンドモードであることを表す
		/// </summary>
		private bool bBlend;

		///	<summary>ブレンドとしてaddColorを指定する。</summary>
		/// <remarks>
		///	blt メソッド等で描画するときのblendを有効にする。
		///	無効にするには disableBlend を呼び出すこと。
		///
		///	ブレンド方法としては、addColor(加色合成)を指定する。
		///	すなわち、glBlendFunc(GL_ONE,GL_ONE);を行なっている。
		///
		///	α付きサーフェースでなくともaddColorは出来るので、
		///	これを使って転送すると高速化を図れることがある。
		///
		///	光の表現をするときなどに使うと良い。
		///
		///	http://www.platz.or.jp/~moal/ablend.html"
		///	が詳しい。
		/// 
		/// alpha channelありのテクスチャに対して、このメソッドを呼び出して
		/// Bltした場合、転送元のalpha channelはすべて無視される。(255として扱われる)
		/// それで良ければ呼び出して構わない。(ダメならBlendAddColorAlphaを用いること)
		/// </remarks>
		public void BlendAddColor()
		{
			Blend = true;
			Gl.glBlendFunc(Gl.GL_ONE, Gl.GL_ONE);
		}

		/// <summary>
		/// 転送元がα付きサーフェースのときのaddcolor。
		/// </summary>
		/// <remarks>
		/// <para>
		/// blendAddColor の α付きサーフェースのとき用。
		/// </para>
		/// <para>
		/// 内部的には
		/// <code>
		/// enableBlend();
		/// glBlendFunc(GL_SRC_ALPHA,GL_ONE);
		/// </code>
		/// を行なっている。
		/// </para>
		/// 転送元のテクスチャにalpha channelが無い場合は、
		/// そのテクスチャは、全域で alpha == 255 となみされる。
		/// よって、alpha channelなしのテクスチャに対して、このメソッドを呼び出した状態に
		/// してBltしてもかまわない。
		/// </remarks>
		public void BlendAddColorAlpha()
		{
			Blend = true;
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE);
		}

		///	<summary>ブレンドとしてsubColorを指定する。</summary>
		/// <remarks>
		///	blt メソッド等で描画するときのblendを有効にする。
		///	これを使って転送すると高速化を図れることがある。
		///
		///	ブレンド方法としては、subColor(加色合成)を指定する。
		///	すなわち、glBlendFunc(GL_ZERO,GL_ONE_MINUS_SRC_COLOR);を行なっている。
		///
		///	α付きサーフェースでなくともsubColorは出来るので、
		///	これを使って転送すると高速化を図れることがある。
		///
		///	http://www.platz.or.jp/~moal/subtract.html
		///	が詳しい。
		///
		/// alpha channelありのテクスチャに対して、このメソッドを呼び出して
		/// Bltした場合、転送元のalpha channelはすべて無視される。(255として扱われる)
		/// それで良ければ呼び出して構わない。(ダメならBlendSubColorAlphaを用いること)
		/// </remarks>
		public void BlendSubColor()
		{
			Blend = true;
			Gl.glBlendFunc(Gl.GL_ZERO, Gl.GL_ONE_MINUS_SRC_COLOR);
		}

		/// <summary>
		/// 転送元がα付きサーフェースのときのsubcolor
		/// </summary>
		/// <remarks>
		/// <para>
		/// blendSubColor の α付きサーフェースのとき用。
		/// </para>
		/// <para>
		/// 内部的には
		/// <code>
		/// enableBlend();
		/// glBlendFunc(GL_ZERO,GL_ONE_MINUS_SRC_ALPHA);
		/// </code>
		/// を行なっている。
		/// </para>
		/// 転送元のテクスチャにalpha channelが無い場合は、
		/// そのテクスチャは、全域で alpha == 255 となみされる。
		/// よって、alpha channelなしのテクスチャに対して、このメソッドを呼び出した状態に
		/// してBltしてもかまわない。
		/// </remarks>
		public void BlendSubColorAlpha()
		{
			Blend = true;
			Gl.glBlendFunc(Gl.GL_ZERO, Gl.GL_ONE_MINUS_SRC_ALPHA);
		}

		///	<summary>ブレンドとして、αブレンドを指定する。</summary>
		/// <remarks>
		///	blt メソッド等で描画するときのblendを有効にする。
		///	ブレンド方法として、
		///
		///	<code>glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);</code>
		///
		///	を指定している。
		///
		///	これにより、転送元のα値により、αブレンドが行なわれる。
		///	テクスチャーのカラーキー付き転送も、実際はカラーキーの部分のα値を
		///	0にすることにより実現しているので、この転送方式を用いないと、
		///	カラーキー付き転送は実現できない。
		/// 
		/// 転送元のテクスチャにalpha channelが無い場合は、
		/// そのテクスチャは、全域で alpha == 255 となみされる。
		/// よって、alpha channelなしのテクスチャに対して、このメソッドを呼び出した状態に
		/// してBltしてもかまわない。
		/// </remarks>
		public void BlendSrcAlpha()
		{
			Blend = true;
			Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
		}

		/// <summary>
		/// Blendを無効にする。
		/// </summary>
		/// <remarks>
		/// Blend = falseとしているのと同じなのだけれども、
		/// BlendSrcAlpha等と対となるメソッドがないと気持ち悪いので。
		/// </remarks>
		public void BlendDisable()
		{
			Blend = false;
		}

		#endregion

		#region デバッグ用
		/// <summary>openGLを用いた、簡単な文字列の描画。</summary>
		/// <remarks>
		///	対応しているのは、数字とアルファベットのみ。
		///	openGLは初期化済みと仮定している。
		///
		///	文字列を描画していく。指定された座標(x,y)から、指定されたsizeで
		///	右側に向かって描画していく。2D polygonを使用。
		///
		///	簡単なデバッグ用だと想定している。
		/// <code>
		///	screen.setLineWidth(2);
		///	screen.drawString("0123456789",0,0,10);
		/// </code>
		///	もし必要ならば、変換行列を設定して、回転・移動を行なえば良い。
		/// </remarks>
		public void DrawString(string txt, int x, int y, int size)
		{
			float[] vertex = new float[txt.Length * 4 * 11];
			int count = 0;

			string s = txt.ToUpper();
			for (int i = 0; i < s.Length; ++i)
			{
				char c = s[i];
				int f = moji.IndexOf(c);
				if (f == -1) goto next;

				short u = mojiData[f];
				if ((u & 1) != 0)
				{
					vertex[count + 0] = x;
					vertex[count + 1] = y;
					vertex[count + 2] = x + size;
					vertex[count + 3] = y;
					count += 4;
				}
				if ((u & 2) != 0)
				{
					vertex[count + 0] = x;
					vertex[count + 1] = y;
					vertex[count + 2] = x;
					vertex[count + 3] = y + size / 2;
					count += 4;
				}
				if ((u & 4) != 0)
				{
					vertex[count + 0] = x + size;
					vertex[count + 1] = y;
					vertex[count + 2] = x + size;
					vertex[count + 3] = y + size / 2;
					count += 4;
				}
				if ((u & 8) != 0)
				{
					vertex[count + 0] = x;
					vertex[count + 1] = y + size / 2;
					vertex[count + 2] = x + size;
					vertex[count + 3] = y + size / 2;
					count += 4;
				}
				u >>= 4;
				if ((u & 1) != 0)
				{
					vertex[count + 0] = x;
					vertex[count + 1] = y + size / 2;
					vertex[count + 2] = x;
					vertex[count + 3] = y + size;
					count += 4;
				}
				if ((u & 2) != 0)
				{
					vertex[count + 0] = x + size;
					vertex[count + 1] = y + size / 2;
					vertex[count + 2] = x + size;
					vertex[count + 3] = y + size;
					count += 4;
				}
				if ((u & 4) != 0)
				{
					vertex[count + 0] = x;
					vertex[count + 1] = y + size;
					vertex[count + 2] = x + size;
					vertex[count + 3] = y + size;
					count += 4;
				}
				u >>= 3;
				if ((u & 1) != 0)
				{
					vertex[count + 0] = x;
					vertex[count + 1] = y;
					vertex[count + 2] = x + size;
					vertex[count + 3] = y + size / 2;
					count += 4;
				}
				if ((u & 2) != 0)
				{
					vertex[count + 0] = x + size;
					vertex[count + 1] = y;
					vertex[count + 2] = x;
					vertex[count + 3] = y + size / 2;
					count += 4;
				}
				if ((u & 4) != 0)
				{
					vertex[count + 0] = x;
					vertex[count + 1] = y + size / 2;
					vertex[count + 2] = x + size;
					vertex[count + 3] = y + size;
					count += 4;
				}
				if ((u & 8) != 0)
				{
					vertex[count + 0] = x + size;
					vertex[count + 1] = y + size / 2;
					vertex[count + 2] = x;
					vertex[count + 3] = y + size;
					count += 4;
				}

			next:
				x += (int)(size * 1.3);
			}
			DrawLines(vertex, count);
			//		glLineWidth(lineWidth);
		}
		#endregion

		#region DrawContext～UpdateView
		/// <summary>
		/// 描画用のコンテクストの設定/取得。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// 値を変更すると、その変更した値に従って描画される。
		/// </remarks>
		public DrawContext DrawContext
		{
			get { return drawContext; }
			set { drawContext = value; }
		}

		///	<summary>視体積の設定(こちらは内部的に使用される)。</summary>
		///	<remarks>SDLWindowとfriend指定ができればpublicにしなくて済むのに(´ω`)</remarks>
		public void UpdateView()
		{
			if (drawContext != null)
			{
				drawContext.SetScreenSize(screenX, screenY);
			}
			SetView(offsetX, offsetY, offsetX + screenX, offsetY + screenY);
		}

		/// <summary>
		/// 視体積の設定(内部的に使用される)
		/// </summary>
		///	<remarks>SDLWindowとfriend指定ができればpublicにしなくて済むのに(´ω`)</remarks>
		/// <param name="screenX"></param>
		/// <param name="screenY"></param>
		/// <param name="offsetX"></param>
		/// <param name="offsetY"></param>
		public void UpdateView(int screenX, int screenY, int offsetX, int offsetY)
		{
			this.screenX = screenX;
			this.screenY = screenY;
			this.offsetX = offsetX;
			this.offsetY = offsetY;
			UpdateView();
		}

		/// <summary>
		/// 視体積の設定(内部的に使用される)
		/// </summary>
		///	<remarks>SDLWindowとfriend指定ができればpublicにしなくて済むのに(´ω`)</remarks>
		/// <param name="screenX"></param>
		/// <param name="screenY"></param>
		public void UpdateView(int screenX, int screenY)
		{
			this.screenX = screenX;
			this.screenY = screenY;
			UpdateView();
		}
		#endregion

		#region 画面キャプチャ用
		/// <summary>
		/// Screenの画像をサーフェスにする。
		/// ただし、画面の上に載せている.NET Frameworkのコントロールは
		/// キャプチャできないので注意。
		/// </summary>
		public Surface GetSurface()
		{
			Surface surface = new Surface();
			if (surface.CreateDIB(Width, Height, false) != YanesdkResult.NoError)
			{
				surface.Dispose();
				return null;
			}

			// 読み出し元を設定。
			// デフォルトでバックバッファだが念のため設定。
			//Gl.glReadBuffer(Gl.GL_FRONT);
			Gl.glReadBuffer(Gl.GL_BACK);

			//*

			Gl.glReadPixels(0, 0, surface.Width, surface.Height,
				surface.Alpha ? Gl.GL_RGBA : Gl.GL_RGB,
				Gl.GL_UNSIGNED_BYTE,
				surface.Pixels);

			// glReadPixels()で取得したピクセルが上下逆で、
			// 反転させる方法が分からなかったので、
			// 仕方なくこちら側で反転させるコードを書いてみた。(´ω`)
			unsafe
			{
				int height = surface.Height;
				int pitch = surface.Pitch;
				byte* pixels = (byte*)surface.Pixels;
				byte* buffer = (byte*)Marshal.AllocHGlobal(pitch);

				for (int y = 0; y < height / 2; y++)
				{
					byte* y1 = pixels + y * pitch;
					byte* y2 = pixels + (height - y - 1) * pitch;
					CopyMemory(buffer, y1, (uint)pitch);
					CopyMemory(y1, y2, (uint)pitch);
					CopyMemory(y2, buffer, (uint)pitch);
				}

				Marshal.FreeHGlobal((IntPtr)buffer);
			}

			/*/
			
			// ↓こっちの方が微妙に重い。
			// 排他処理かなにかでもしてるのかも？
			unsafe {
				byte* p = (byte*)surface.Pixels;
				uint format = surface.Alpha ? Gl.GL_RGBA : Gl.GL_RGB;
				for (int y = 0, h = surface.Height; y < h; y++) {
					// 1行ずつ読む。
					Gl.glReadPixels(0, h - y - 1, surface.Width, 1,
						format, Gl.GL_UNSIGNED_BYTE, (IntPtr)p);
					p += surface.Pitch;
				}
			}

			//*/

			return surface;
		}

//	monoで動くようにCopyMemoryを自前で書いておく
#if true
		/// <summary>
		/// WinAPIのCopyMemory()。
		/// </summary>
		/// <remarks>
		/// 可搬性を考慮してC#で実装してみたところ、
		/// この実装でもAPIとほぼ同じ速度が出たので、常にこれを使う事にする。
		/// </remarks>
		static unsafe void CopyMemory(void* outDest, void* inSrc, uint inNumOfBytes)
		{
			// 転送先をuint幅にalignする
			const uint align = sizeof(uint) - 1;
			uint offset = (uint)outDest & align;
				// ↑ポインタは32bitとは限らないので本来このキャストはuintではダメだが、
				// 今は下位2bitだけあればいいのでこれでOK。
			if (offset != 0)
				offset = align - offset;
			offset = global::System.Math.Min(offset, inNumOfBytes);

			// 先頭の余り部分をbyteでちまちまコピー
			byte* srcBytes = (byte*)inSrc;
			byte* dstBytes = (byte*)outDest;
			for (uint i = 0; i < offset; i++)
				dstBytes[i] = srcBytes[i];

			// uintで一気に転送
			uint* dst = (uint*)((byte*)outDest + offset);
			uint* src = (uint*)((byte*)inSrc + offset);
			uint numOfUInt = (inNumOfBytes - offset) / sizeof(uint);
			for (uint i = 0; i < numOfUInt; i++)
				dst[i] = src[i];

			// 末尾の余り部分をbyteでちまちまコピー
			for (uint i = offset + numOfUInt * sizeof(uint); i < inNumOfBytes; i++)
				dstBytes[i] = srcBytes[i];
		}
#else
		[DllImport("kernel32.dll")]
		static unsafe extern void CopyMemory(void* outDest, void* inSrc, uint inNumOfBytes);
#endif
		#endregion // GetSurface

		#region protected
		/// <summary>
		/// スクリーンのサイズ。
		/// </summary>
		protected int screenX, screenY;
		/// <summary>
		/// スクリーンのオフセット値。
		/// </summary>
		protected int offsetX, offsetY;
		/// <summary>
		/// setColorで設定されている色。
		/// </summary>
		protected Color4ub color;
		#endregion

		#region properties

		/// <summary>
		/// このサーフェースのサイズを取得
		/// </summary>
		public int Width { get { return screenX; } }

		/// <summary>
		/// このサーフェースのサイズを取得
		/// </summary>
		public int Height { get { return screenY; } }

		private DrawContext drawContext = new GlDrawContext();

		///	<summary>drawLine,drawStringの線の太さをピクセル単位で指定する。</summary>
		///	<remarks>ここで設定した値は、容易に変更されうるので使う直前に設定すること。</remarks>
		public void SetLineWidth(float u)
		{
			//	現在のcontextに即して変換する必要がある
			DrawContext dc = DrawContext;

			Gl.glLineWidth(u * dc.RateX);
		}

		#endregion

		#region Debug用
		/// <summary>
		/// インチキ文字表示
		/// </summary>
		protected readonly string moji =
			//	表示に対応している文字
			"0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		protected readonly short[] mojiData =
			new short[] {
				/*
							---	 1
							|	|  2   3		8(＼) ・ 9(／)
							---	 4
							|	|  5   6		10(＼)・11(／)
							---	 7
						*/
				0x77, 0x24, 0x5D, 0x6D, // 0,1,2,3
				0x2E, 0x6B, 0x7B, 0x25, // 4,5,6,7
				0x7F, 0x2F, 0x7D, 0x7A, // 8,9,a,b
				0x53, 0x7C, 0x5B, 0x1B, // c,d,e,f
				0x27B, 0x3E, 0x24, 0x64, // g,h,i,j
				0x312, 0x52, 0x1B7, 0x38, // k,l,m,n
				0x0F, 0x1F, 0x127, 0x313, // o,p,q,r
				0x6B, 0x5A, 0x70, 0x220, // s,t,u,v
				0x630, 0x600, 0x6E, 0x448, // w,x,y,z
			};
		#endregion
	}
}
