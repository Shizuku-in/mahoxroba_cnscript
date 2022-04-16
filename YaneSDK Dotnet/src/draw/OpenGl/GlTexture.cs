using System;

using Sdl;
using OpenGl;

using Yanesdk.Ytl;
using Yanesdk.System;
using System.Diagnostics;
using System.Text;

namespace Yanesdk.Draw
{
	#region Texture生成時のオプションに関するinterfaceおよびclass
	public interface IGlTextureOption
	{
		/// <summary>
		/// 拡大縮小のときに線形補間を用いるのか？
		/// </summary>
		/// <remarks>
		/// これを用いると古いビデオカードでは極端に遅くなる。
		///	</remarks>
		bool Smooth { get; set; }

		/// <summary>
		/// S3TCテクスチャ圧縮を用いるのか。
		/// ビデオメモリの少ないマシンでは用いたほうが良いが
		/// 万人向けというわけでもない。
		/// 
		/// ゲーム内のconfigで変更できるように作るのが好ましいだろう。
		/// ※　TxRcとの併用は出来ないのでTxRcが使える状況ならばTxRcを優先する。
		/// ただし、NPOTとは併用できる。
		/// 
		/// default = false
		/// </summary>
		bool S3TC { get; set; }

		/// <summary>
		/// FXT1テクスチャ圧縮を用いるのか。
		/// ビデオメモリの少ないマシンでは用いたほうが良いが
		/// 万人向けというわけでもない。
		/// 
		/// default = false
		/// </summary>
		bool FXT1 { get; set; }

		/// <summary>
		/// テクスチャ圧縮(S3TC,FXT1など)を用いるのか？
		/// 
		/// テクスチャ圧縮を用いると画質が落ちるが
		/// ビデオメモリ節約になる。
		/// </summary>
		bool Compress { get; set; }

		/// <summary>
		/// NPOT(2の累乗サイズ以外のテクスチャ)が使える状況なら
		/// ビデオメモリ節約のために使うかのフラグ。
		/// 
		/// ただし、Radeon系は遅くなるので、使える状況でも使わない。
		/// </summary>
		bool NPOT { get; set; }

		/// <summary>
		/// TxRc(自由なサイズのテクスチャ)が使えるなら使うか。
		/// ビデオメモリ節約のために使ったほうが良い。
		/// 
		/// TxRcのほうがNPOTより対応しているビデオカードが多く、
		/// また、NPOTと違い、速度的なペナルティも少ないのでお勧め。
		/// 
		/// GlTextureはNPOTとTxRcとどちらも使える状況ならば、TxRcを優先する。
		/// </summary>
		bool TxRc { get; set; }
	}

	/// <summary>
	/// GlTexture用のGlobalOption。
	/// </summary>
	public class GlTextureGlobalOption : IGlTextureOption
	{
		/// <summary>
		/// 拡大縮小時の補間をSmoothにするのか。
		/// (ハードで対応していないと遅くなる)
		/// 
		/// 拡大縮小をあまり使わないならfalseでいいと思う。
		/// 
		/// default == false
		/// </summary>
		public bool Smooth
		{
			get { return smooth; }
			set { smooth = value; }
		}
		private bool smooth = false;

		/// <summary>
		/// S3TCテクスチャ圧縮を用いるのか。
		/// ビデオメモリの少ないマシンでは用いたほうが良いが
		/// 万人向けというわけでもない。
		/// 
		/// ゲーム内のconfigで変更できるように作るのが好ましいだろう。
		/// ※　TxRcとの併用は出来ないのでTxRcが使える状況ならばTxRcを優先する。
		/// ただし、NPOTとは併用できる。
		/// 
		/// default = false
		/// </summary>
		public bool S3TC
		{
			get { return s3TC; }
			set { s3TC = value; }
		}
		private bool s3TC = false;

		/// <summary>
		/// FXT1テクスチャ圧縮を用いるのか。
		/// ビデオメモリの少ないマシンでは用いたほうが良いが
		/// 万人向けというわけでもない。
		/// 
		/// default = false
		/// </summary>
		public bool FXT1
		{
			get { return fXT1; }
			set { fXT1 = value; }
		}
		private bool fXT1 = false;

		/// <summary>
		/// テクスチャ圧縮(S3TC,FXT1など)を用いるのか？
		/// 
		/// テクスチャ圧縮を用いると画質が落ちるが
		/// ビデオメモリ節約になる。
		/// </summary>
		public bool Compress
		{
			get { return S3TC || FXT1; }
			set { S3TC = FXT1 = value; }
		}

		/// <summary>
		/// NPOT(2の累乗サイズ以外のテクスチャ)が使える状況なら
		/// ビデオメモリ節約のために使うかのフラグ。
		/// 
		/// ただし、Radeon系は遅くなるので、使える状況でも使わない。
		/// </summary>
		public bool NPOT
		{
			get { return nPOT; }
			set { nPOT = value; }
		}
		private bool nPOT = true;

		/// <summary>
		/// TxRc(自由なサイズのテクスチャ)が使えるなら使うか。
		/// ビデオメモリ節約のために使ったほうが良い。
		/// 
		/// TxRcのほうがNPOTより対応しているビデオカードが多く、
		/// また、NPOTと違い、速度的なペナルティも少ないのでお勧め。
		/// 
		/// GlTextureはNPOTとTxRcとどちらも使える状況ならば、TxRcを優先する。
		/// </summary>
		public bool TxRc
		{
			get { return txRc; }
			set { txRc = value; }
		}
		private bool txRc = true;
	}

	public class GlTextureLocalOption : IGlTextureOption
	{
		/// <summary>
		/// テクスチャを貼るときに線形補間する。
		/// </summary>
		/// <remarks>
		/// テクスチャを GL_LINEAR で貼る。
		/// 線形補間されるので拡大・縮小時に綺麗に貼れる。
		/// むかしのビデオカードだとハードで対応していないので遅くなる。
		/// 
		/// この関数を設定するなら、loadを行なう前に設定しておくこと。
		/// BG用の画像等は、(拡大縮小しないならば) Smooth = false;　しておけば
		/// 良いと思われる。
		/// 
		/// 設定されていなければglobal optionに従う。
		/// global optionのほうはディフォルトではtrue。
		/// </remarks>
		public bool Smooth
		{
			get { return smooth.HasValue ? smooth.Value : GlTexture.GlobalOption.Smooth; }
			set { smooth = value; }
		}
		private bool? smooth = null;		//	テクスチャ貼り付け方法(trueならばGL_LINEAR)

		/// <summary>
		/// テクスチャにS3TC圧縮を用いる。
		/// </summary>
		/// <remarks>
		/// この関数を設定するなら、loadを行なう前に設定しておくこと。
		/// この圧縮を用いるとビデオメモリが節約できる。画像はやや汚くなる。
		/// 
		/// 設定されていなければglobal optionに従う。
		/// global optionのほうはディフォルトではfalse。
		/// </remarks>
		public bool S3TC
		{
			get { return s3tc.HasValue ? s3tc.Value : GlTexture.GlobalOption.S3TC; }
			set { s3tc = value; }
		}
		private bool? s3tc = null;

		/// <summary>
		/// テクスチャにFXT1圧縮を用いる。
		/// </summary>
		/// <remarks>
		/// この関数を設定するなら、loadを行なう前に設定しておくこと。
		/// この圧縮を用いるとビデオメモリが節約できる。画像はやや汚くなる。
		/// 
		/// 設定されていなければglobal optionに従う。
		/// global optionのほうはディフォルトではfalse。
		/// </remarks>
		public bool FXT1
		{
			get { return fxt1.HasValue ? fxt1.Value : GlTexture.GlobalOption.FXT1; }
			set { fxt1 = value; }
		}
		private bool? fxt1 = null;

		/// <summary>
		/// テクスチャ圧縮(S3TC,FXT1など)を用いるのか？
		/// 
		/// テクスチャ圧縮を用いると画質が落ちるが
		/// ビデオメモリ節約になる。
		/// 
		/// 設定されていなければglobal optionに従う。
		/// global optionのほうはディフォルトではfalse。
		/// </summary>
		/// <remarks>
		/// getterは S3TC || FXT1をそのまま返しているだけなので注意。
		/// </remarks>
		public bool Compress
		{
			get { return S3TC || FXT1; }
			set { S3TC = value; FXT1 = value; }
		}

		/// <summary>
		/// NPOT(2の累乗サイズ以外のテクスチャ)が使える状況なら
		/// ビデオメモリ節約のために使うかのフラグ。
		/// 
		/// ただし、Radeon系は遅くなるので、使える状況でも使わない。
		/// </summary>
		public bool NPOT
		{
			get { return nPOT.HasValue ? nPOT.Value : GlTexture.GlobalOption.NPOT; }
			set { nPOT = value; }
		}
		private bool? nPOT = true;

		/// <summary>
		/// TxRc(自由なサイズのテクスチャ)が使えるなら使うか。
		/// ビデオメモリ節約のために使ったほうが良い。
		/// 
		/// TxRcのほうがNPOTより対応しているビデオカードが多く、
		/// また、NPOTと違い、速度的なペナルティも少ないのでお勧め。
		/// 
		/// GlTextureはNPOTとTxRcとどちらも使える状況ならば、TxRcを優先する。
		/// </summary>
		public bool TxRc
		{
			get { return txRc.HasValue ? txRc.Value : GlTexture.GlobalOption.TxRc; }
			set { txRc = value; }
		}
		private bool? txRc = true;
	}
	#endregion

	#region テクスチャを解放後に描画パフォーマンスが落ちないかのテスト用コード
	/*
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Drawing;
	using System.Text;
	using System.Windows.Forms;

	using Yanesdk.Ytl;
	using Yanesdk.Draw;
	using Yanesdk.Database;
	using Yanesdk.Network;
	using Yanesdk.Timer;


	namespace WindowsApplication1
	{
		public partial class Form4 : Form
		{
			public Form4()
			{
				InitializeComponent();

				Init();
			}

			GlTexture[] txt = new GlTexture[1000];

			/// <summary>
			/// 
			/// </summary>
			public void Init()
			{
				window = new Win32Window(this.Handle);
				window.Screen.Select();

				fpsTimer = new FpsTimer();
				fps = new FpsLayer(fpsTimer);

				window.Screen.Unselect();
			}


			Win32Window window;

			FpsTimer fpsTimer;
			FpsLayer fps;

			int r = 0;
			bool tobeLoad = false;
			bool tobeRelease = false;

			private void timer1_Tick(object sender, EventArgs e)
			{
				Yanesdk.Draw.IScreen scr = window.Screen;
				scr.Select();
				scr.SetClearColor(255, 0, 0);
				scr.Clear();

				if (tobeLoad)
				{
					tobeLoad = false;
					txt[r] = new GlTexture();
					YanesdkResult result = txt[r].Load("2.bmp");

					if (result == YanesdkResult.NoError)
					{

					}

					r++;

					label1.Text = r.ToString();
				}
				if (tobeRelease)
				{
					tobeRelease = false;
					r--;
					txt[r].Dispose();
					txt[r] = null;

					label1.Text = r.ToString();
				}

				//	scr.Blt(txt , 0 , 0);
				scr.Blend = true;
				scr.BlendSrcAlpha();

				for (int i = 0; i < r; ++i)
					scr.Blt(txt[i], 0, 0);

				fps.OnDraw(window.Screen, 400, 100);

				scr.Update();
				fpsTimer.WaitFrame();
			}

			private void button1_Click(object sender, EventArgs e)
			{
				tobeLoad = true;
			}

			private void button2_Click(object sender, EventArgs e)
			{
				tobeRelease = true;
			}

		}
	}
	*/
	#endregion

	/// <summary>
	/// SDLWindow/Win32Window に画像を描画するためのサーフェース
	/// </summary>
	/// <remarks>
	///	openGLのテクスチャー。
	///	bmp,png,etc..を読み込んで、画面に描画するときに使います。
	///
	///	サイズは 幅、高さがそれぞれ、2のべき乗でなければ、2のべき乗に
	///	なるようにテクスチャーサイズを内部で拡大します。
	/// 
	/// Loadを行なうときにalpha channel付きの画像かどうか自動判定を行なう。
	/// alpha channel付きの画像であれば、自動的に、alpha channel付きのテクスチャを生成する。
	/// また、SetColorKey,SetColorKeyPosで抜き色が指定されているときも強制的にalpha channel付きの
	/// テクスチャを生成する。
	/// 
	/// 一度でもLoadしたGlTextureのインスタンスは、CreateDIBするべきではない。
	/// (cacheの関係上→気になる人はconstructInfoを追いかけてみよ。)
	/// このへん、mutableなクラスになるように設計したほうが良かったのではないかと思わないでもない。
	/// </remarks>
	public class GlTexture : CachedObject , ITexture, ILoader , IDisposable
	{
		#region コンストラクタとIDisposableの実装
		public GlTexture()
		{
			This = this; // CacheObjectにmix-inするときのthisを渡す必要がある。

			// defaultでUnmanagedResourceManagerのcache systemに接続
			CacheSystem = UnmanagedResourceManager.Instance.VideoMemory;
			CacheSystem.Add(this);
		}
		/// <summary>
		/// SDL Videoの初期化用
		/// </summary>
		private RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>
			init = new RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>();

		/// <summary>
		/// Disposeを呼び出したあと再度Loadは出来ない。
		/// 再度LoadしたいならばReleaseを呼び出して解放すること。
		/// </summary>
		public void Dispose()
		{
			Release();

			init.Dispose();
			CacheSystem.Remove(this);
		}
		#endregion

		#region Surface,SDL_Surface,.NET FrameworkのBitmapを渡してテクスチャ化する
		/// <summary>
		/// SDL_Surafceを渡し、それをテクスチャーとする
		/// このときこのクラスで設定されているColorKeyは考慮されない
		/// </summary>
		/// <remarks>
		/// ここで渡したsurface自体は解放されないし、破壊もされない。
		/// </remarks>
		/// <param name="surface"></param>
		/// <returns></returns>
 		public YanesdkResult SetSurface(IntPtr surface)
		{
			Release();

			YanesdkResult result;
			if (surface == IntPtr.Zero)
			{
				result = YanesdkResult.InvalidParameter;
			}
			else
			{
				using (Surface tmpSurface = new Surface())
				{
					tmpSurface.SDL_Surface = surface;
					result = InnerSetSurface(tmpSurface, true);	// surface,dup/init
					// 渡したSurfaceを破壊してはいけないので trueを指定する。

					tmpSurface.Unbind_SDL_Surface();
					// 戻しとかないとtmpSurfaceが解放されるときにここで設定したsurfaceが
					// 勝手に解放されてしまう。
				}
			}
			// リソースサイズが変更になったことをcache systemに通知する
			CacheSystem.OnResourceChanged(this);

			return result;
		}

		/// <summary>
		/// Surface(SDL_Surafceのwrapper)を渡し、それをテクスチャーとする。
		/// このときこのクラスで設定されているColorKeyは考慮されない
		/// </summary>
		/// <remarks>
		/// ここで渡したsurface自体は解放されない。
		/// </remarks>
		/// <param name="surface"></param>
		/// <returns></returns>
		public YanesdkResult SetSurface(Surface surface) 
		{
			Release();

			return InnerSetSurface(surface, true);	// surface,dup/init
		}

		/// <summary>
		/// .NET FrameworkのBitmapをベースにTexture化する。
		/// </summary>
		/// <remarks>これがあれば、.NETのGraphics等で描画したものを
		/// テクスチャー化して描画できる。ただし、内部的には何度もメモリ
		/// コピーを行なう必要があるためあまり速くはない。なるべくなら動的には
		/// 行なわないほうが良い。</remarks>
		/// <code>
		///		Bitmap bmp = new Bitmap(100, 100);
		///		Graphics g = Graphics.FromImage(bmp);
		///		g.DrawLine(new Pen(Color.Aqua, 10), 0, 0, 100, 100);
		///		GlTexture txt = new GlTexture();
		///		txt.SetBitmap(bmp);
		///		g.Dispose();
		///		scr.Blt(txt, 100, 100);
		///		txt.Dispose();
		/// </code>
		/// <param name="bmp"></param>
		/// <returns></returns>
		public YanesdkResult SetBitmap(global::System.Drawing.Bitmap bmp)
		{
			//	転送元と転送先のサイズが同じで転送先がα付きのときは
			//	glTextureSubImageで転送したほうが良いのだが
			//	そんなに再々呼び出すことはないので、このままにしておこう…。

			Surface surface;
			YanesdkResult result = BitmapHelper.BitmapToSurface(bmp, out surface);
			if (result != YanesdkResult.NoError)
				return result;
			this.SetSurface(surface);
			surface.Dispose();

			return YanesdkResult.NoError;
		}
		#endregion

		#region 転送系のmethod実装
		/// <summary>
		/// テクスチャーを貼り付けるときに使う
		/// </summary>
		/// <remarks>
		/// この関数を呼び出したあと、
		/// <code>
		///		glBegin(GL_POLYGON);
		///			glTexCoord2f(0 , 0); glVertex2f(-0.9 , -0.9);
		///			glTexCoord2f(0 , 1); glVertex2f(-0.9 , 0.9);
		///			glTexCoord2f(1 , 1); glVertex2f(0.9 , 0.9);
		///			glTexCoord2f(1 , 0); glVertex2f(0.9 , -0.9);
		///		glEnd();
		///	</code>
		///	のようにすれば、テクスチャーをポリゴンにはりつけることが出来ます。
		/// 
		/// ※ OpenGLのコードが混在するのが嫌なら使わないで。
		///	</remarks>
		public void Bind()
		{
			// bindのタイミングでcache systemにこのテクスチャを使用したことを通知する
			CacheSystem.OnAccess(this);

			if (loaded)
			{
				if (textureType == Gl.GL_TEXTURE_RECTANGLE_ARB)
					Gl.glEnable(Gl.GL_TEXTURE_RECTANGLE_ARB);

				Gl.glBindTexture(textureType, textureName);
			}
		}

		/// <summary>
		/// テクスチャーのバインドを解除する。
		/// </summary>
		/// <remarks>
		/// <c>bind();</c>でバインドしていたテクスチャーをunbindする。
		/// ※ OpenGLのコードが混在するのが嫌なら使わないで。
		/// </remarks>
		public void Unbind()
		{
			// UnbindするまでにReleaseされることがあるかも知れないが、
			// べつにそれはされても構わない(と思う)
			if (loaded)
			{
				Gl.glBindTexture(textureType, 0);

				// 念のためにこれも対になるように処理を入れておく。
				if (textureType == Gl.GL_TEXTURE_RECTANGLE_ARB)
					Gl.glDisable(Gl.GL_TEXTURE_RECTANGLE_ARB);
			}
		}

		/// <summary>
		/// このテクスチャを対象(DrawContext)の(x,y)に転送する。
		/// </summary>
		/// <param name="context"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Blt(DrawContext context,float x,float y) {
			if (!Loaded) return ;

			float w = Width;
			float h = Height;

			if (w==0 || h==0) return ;

			float wr = WidthRate;
			float hr = HeightRate;

			//	描画は、Drawcontext.RateX,rateYの値を反映しなければならない


			float rateX = context.RateX;
			float rateY = context.RateY;

			x = x * rateX + context.OffsetRX;
			y = y * rateY + context.OffsetRY;

			w *= rateX;
			h *= rateY;

			//	クリップ処理は、openglに任せたので、もはや不要なのだ
			//	クリップ無し
			Bind();
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glTexCoord2f(0,0); Gl.glVertex2f(x  ,y  );
			Gl.glTexCoord2f(wr,0); Gl.glVertex2f(x+w,y  );
			Gl.glTexCoord2f(wr,hr); Gl.glVertex2f(x+w,y+h);
			Gl.glTexCoord2f(0,hr); Gl.glVertex2f(x  ,y+h);
			Gl.glEnd();
			Unbind();
		}

		/// <summary>
		/// このテクスチャを対象(DrawContext)の(x,y)に転送する。
		///	転送元(このテクスチャ)の転送元矩形を指定できる。
		/// </summary>
		/// <param name="context"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		public void Blt(DrawContext context,float x,float y,Rect srcRect) {
			// ref Rectにreadonly制約はつけられへんのかいな(´ω`)

			if (!Loaded) return;

			float w = Width;
			float h = Height;

			if (w==0 || h==0) return ;

			if (srcRect == null){
				srcRect = new Rect(0, 0, w, h);
			}

			float wr = WidthRate;
			float hr = HeightRate;

			float left,top,right,bottom;
			left	= (wr * srcRect.Left) / w;
			top		= (hr * srcRect.Top) / h;
			right	= (wr * srcRect.Right) / w;
			bottom	= (hr * srcRect.Bottom) / h;

			float rateX = context.RateX;
			float rateY = context.RateY;

			w = srcRect.Right - srcRect.Left;
			h = srcRect.Bottom - srcRect.Top;

			if (w==0 || h==0) return ;

			//	左右、上下反転描画のためにabsをとる。
			if (w<0) w=-w;
			if (h<0) h=-h;

			x = x * rateX + context.OffsetRX;
			y = y * rateY + context.OffsetRY;

			w *= rateX;
			h *= rateY;

			Bind();
			Gl.glBegin(Gl.GL_POLYGON);

			//	クリップ処理は、openglに任せたので、もはや不要なのだ
			//	クリップ無し
			Gl.glTexCoord2f(left,top); Gl.glVertex2f(x  ,y  );
			Gl.glTexCoord2f(right,top); Gl.glVertex2f(x+w,y  );
			Gl.glTexCoord2f(right,bottom); Gl.glVertex2f(x+w,y+h);
			Gl.glTexCoord2f(left,bottom); Gl.glVertex2f(x  ,y+h);
			Gl.glEnd();
			Unbind();
		}

		/// <summary>
		/// このテクスチャを対象(DrawContext)の(x,y)に転送する。
		///	転送元(このテクスチャ)の転送元矩形を指定できる。
		/// また、転送先でのサイズを指定できる。
		/// </summary>
		/// <param name="context"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstSize"></param>
		public void Blt(DrawContext context,float x,float y,Rect srcRect,Size dstSize) {
			if (!Loaded) return;

			float w = Width;
			float h = Height;

			if (w==0 || h==0) return ;

			if (srcRect == null)
			{
				srcRect = new Rect(0, 0, w, h);
			}
			
			float wr = WidthRate;
			float hr = HeightRate;

			float left,top,right,bottom;
			left	= (wr * srcRect.Left) / w;
			top		= (hr * srcRect.Top) / h;
			right	= (wr * srcRect.Right) / w;
			bottom	= (hr * srcRect.Bottom) / h;

			float rateX = context.RateX;
			float rateY = context.RateY;

			w = srcRect.Right - srcRect.Left;
			h = srcRect.Bottom - srcRect.Top;

			if (w==0 || h==0) return ;

			//	転送先サイズは指定されているが..
			if (dstSize == null)
			{
			//	dstSize = new Size(context.ScreenSizeX, context.ScreenSizeY);
				w = context.ScreenSizeX;
				h = context.ScreenSizeY;
			} else {
				w = dstSize.Cx;
				h = dstSize.Cy;
			}

			x = x /* * rateX */ + context.OffsetRX;
			y = y /* * rateY */ + context.OffsetRY;

		//	w *= rateX;
		//	h *= rateY;

			Bind();
			Gl.glBegin(Gl.GL_POLYGON);

			//	クリップ処理は、openglに任せたので、もはや不要なのだ
			//	クリップ無し
			Gl.glTexCoord2f(left,top); Gl.glVertex2f(x  ,y  );
			Gl.glTexCoord2f(right,top); Gl.glVertex2f(x+w,y  );
			Gl.glTexCoord2f(right,bottom); Gl.glVertex2f(x+w,y+h);
			Gl.glTexCoord2f(left,bottom); Gl.glVertex2f(x  ,y+h);
			Gl.glEnd();
			Unbind();
		}

		/// <summary>
		/// このテクスチャを対象(DrawContext)の(x,y)に転送する。
		///	転送元(このテクスチャ)の転送元矩形を指定できる。
		/// また、転送先での四角形を指定できる。
		/// </summary>
		/// <param name="context"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstPoint">dstPoint Point[4]は、転送元矩形の左上,右上,右下,左下が、
		/// 転送先において対応する4点。</param>
		public void Blt(DrawContext context,Rect srcRect,Point[] dstPoint) {
			if (!Loaded) return;

			float w = Width;
			float h = Height;

			if (w==0 || h==0) return ;

			if (srcRect == null)
			{
				srcRect = new Rect(0, 0, w, h);
			}

			float wr = WidthRate;
			float hr = HeightRate;

			float left,top,right,bottom;
			left	= (wr*srcRect.Left) / w;
			top		= (hr*srcRect.Top) / h;
			right	= (wr*srcRect.Right) / w;
			bottom	= (hr*srcRect.Bottom) / h;

			if (dstPoint == null)
				return;

			Point[] dp = new Point[4];
			for(int i=0;i<4;++i){
				dp[i].X = dstPoint[i].X*context.RateX + context.OffsetRX;
				dp[i].Y = dstPoint[i].Y*context.RateY + context.OffsetRY;
			}

			Bind();
			Gl.glBegin(Gl.GL_POLYGON);
			Gl.glTexCoord2f(left,top);
			Gl.glVertex2f(dp[0].X,dp[0].Y);

			Gl.glTexCoord2f(right,top);
			Gl.glVertex2f(dp[1].X,dp[1].Y);

			Gl.glTexCoord2f(right,bottom);
			Gl.glVertex2f(dp[2].X,dp[2].Y);

			Gl.glTexCoord2f(left,bottom);
			Gl.glVertex2f(dp[3].X,dp[3].Y);
			Gl.glEnd();
			Unbind();
		}

		/// <summary>
		/// このテクスチャを対象(DrawContext)の(x,y)に転送する。
		///	転送元(このテクスチャ)の転送元四角形を指定できる。
		/// また、転送先での四角形を指定できる。
		/// 
		/// 転送元の4点(srcPoint[4])が、転送先の4点(dstPoint[4])に
		/// 対応するように転送される。
		/// </summary>
		/// <param name="context"></param>
		/// <param name="srcPoint"></param>
		/// <param name="dstPoint"></param>
		public void Blt(DrawContext context,Point[] srcPoint,Point[] dstPoint) {
			if (!Loaded) return;

			//	クリップ処理は、openglに任せたので、もはや不要なのだ

			float w = Width;
			float h = Height;

			if (w==0 || h==0) return ;

			float wr = WidthRate;
			float hr = HeightRate;

			if (dstPoint == null)
				return ;

			Bind();
			Gl.glBegin(Gl.GL_POLYGON);
			for(int i=0;i<4;++i){
				float xx	= (wr*srcPoint[i].X) / w;
				float yy	= (hr*srcPoint[i].Y) / h;
				Gl.glTexCoord2f(xx,yy);
				float dx	= dstPoint[i].X * context.RateX + context.OffsetRX;
				float dy	= dstPoint[i].Y * context.RateY + context.OffsetRY;
				Gl.glVertex2f(dx,dy);
			}
			Gl.glEnd();
			Unbind();
		}
		#endregion

		#region properties
		/// <summary>
		/// テクスチャ生成のグローバルオプションの設定、取得。
		/// </summary>
		public static IGlTextureOption GlobalOption
		{
			get { return Singleton<GlTextureGlobalOption>.Instance; }
		}

		/// <summary>
		/// このテクスチャの生成時のオプション
		/// </summary>
		public IGlTextureOption LocalOption
		{
			get { return localOption; }
		}
		private GlTextureLocalOption localOption = new GlTextureLocalOption();

		/// <summary>
		/// α channelを持つか持たないかを取得する
		/// </summary>
		/// <remarks>
		/// <para>
		/// 元情報にαが含まれていない場合で、α付きの転送を行なわないと
		/// わかっている場合(BGなど)は、無効化しておくことで、
		/// 転送時の高速化をはかれる。
		/// </para>
		/// <para>
		/// 抜き色の指定をする場合は、αサーフェースでなければならない。
		/// (抜き色をαを書き換えて実現しているため)
		/// </para>
		/// </remarks>
		public bool Alpha
		{
			get { return alpha; }
			//	set { alpha = value; }
		}
		private bool alpha = false;

		/// <summary>
		/// 抜き色を指定するためのColorKey構造体。
		/// 
		/// このメソッドに対してアクセスしても良いが、
		/// このColorKeyに他のColorKeyを代入はしないように。
		/// 
		/// また、Loadの前に設定しなければ意味がない。
		/// </summary>
		public ColorKey ColorKey
		{
			get { return colorKey; }
			set
			{
				Debug.Assert(colorKey != null, "ColorKeyとしてnullが設定された");
				colorKey = value;
			}
		}
		private ColorKey colorKey = new ColorKey();

		/// <summary>
		///	テクスチャーの幅を取得
		/// </summary>
		/// <remarks>
		/// テクスチャーを読み込んでいないときは0を返す。
		/// </remarks>
		public float Width { get { return sizeX; } }

		/// <summary>
		///	テクスチャーの高さを取得
		/// </summary>
		/// <remarks>
		///	テクスチャーを読み込んでいないときは0を返す。
		/// </remarks>
		public float Height { get { return sizeY; } }

		/// <summary>
		///	テクスチャーの確保された幅を取得
		/// </summary>
		/// <remarks>
		/// openglにおいて、テクスチャーは、実際には2のべき乗に切り上げて確保される。
		/// (opengl extensionを用いればぴったりのサイズで確保できるのだが…)
		///	テクスチャーを読み込んでいないときは0を返す。
		/// </remarks>
		/// <returns></returns>
		public int RealWidth { get { return sizeRX; } }

		/// <summary>
		///	テクスチャーの確保された高さを取得
		/// </summary>
		/// <remarks>
		/// openglにおいて、テクスチャーは、実際には2のべき乗に切り上げて確保される。
		/// (opengl extensionを用いればぴったりのサイズで確保できるのだが…)
		///	テクスチャーを読み込んでいないときは0を返す。
		/// </remarks>
		/// <returns></returns>
		public int RealHeight { get { return sizeRY; } }

		/// <summary>
		/// Width/RealWidth を返す
		/// ただし拡張テクスチャを用いている場合はテクスチャの実サイズが入る。
		/// </summary>
		/// <returns></returns>
		public float WidthRate { get { return widthRate; } }

		/// <summary>
		/// Height/RealHeight を返す
		/// ただし拡張テクスチャを用いている場合はテクスチャの実サイズが入る。
		/// </summary>
		/// <returns></returns>
		public float HeightRate { get { return heightRate; } }

		/// <summary>
		/// テクスチャのタイプ。
		/// 本当はuintなのだけどCLS準拠にならないのでintにしてある。castして使って。
		/// 
		/// RcTx拡張を用いる場合は、
		/// Gl.GL_TEXTURE_RECTANGLE_ARBをbindするときに指定しなければならない。
		/// (普通は、Gl.GL_TEXTURE_2Dなのだが。)
		/// </summary>
		public int TextureType
		{
			get { return (int)textureType; }
			set { textureType = (uint)value; }
		}
		private uint textureType;
		#endregion

		#region テクスチャ生成のためのprivate methods
		/// <summary>
		/// 2^nに繰り上げる
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		private int	Floor(int n)
		{
			/*
				2^nに繰り上げる by yaneurao 2003 Dec.
			*/
			int r=1,bits=0;
			while (n != 0) {
				bits += n & 1;
				n >>= 1; r <<= 1;
			}
			return (bits==1) ? (r>>1) : r;
		}

		/// <summary>
		/// サーフェースをセットする。
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="bKeepSurface">trueであれば元のSurfaceを破壊しないことを保証する。</param>
		/// <returns></returns>
		unsafe private YanesdkResult InnerSetSurface(Surface surface, bool bKeepSurface) 
		{
			if (surface == null)
			{
				return YanesdkResult.InvalidParameter;
			}

			sizeX = surface.Width;
			sizeY = surface.Height;

			if (sizeX == 0 || sizeY == 0)
			{
				sizeX = sizeY = 0;
				return YanesdkResult.PreconditionError; // おかしい..
			}

			// NPOTを使ったのか？
		//	bool npot = false;

			// TxRcを使ったのか？
			bool txrc = false;

			//  GL_TEXTURE_RECTANGLE_ARBが使える状況なら積極的に使う。
			//　GL_TEXTURE_RECTANGLE_ARBが使えずにNPOT拡張が使えるというのは
			//  あまり考えにくいのだが(´ω`)
			if (localOption.TxRc && GlExtensions.Instance.IsAvailableRectTexture(sizeX, sizeY))
			{
				textureType = Gl.GL_TEXTURE_RECTANGLE_ARB;

				sizeRX = sizeX;
				sizeRY = sizeY;

				// 非正規化テクスチャなのでテクスチャ座標は
				// (0,0)×(1,1)の座標系ではなく、
				// (0,0)×(sizeRX,sizeRY)の座標系になる。
				widthRate = (float)sizeX;
				heightRate = (float)sizeY;

				// ↓これ本来どこで呼び出すのが正しいんだろ。(´ω`)
				// Gl.glEnable(Gl.GL_TEXTURE_RECTANGLE_ARB);

				// ↑draw contextを切り替えたときにうまく動く必要がある
				// bindするときに指定すべき。

				txrc = true;
			}
			else
			{
				if (localOption.NPOT && GlExtensions.Instance.NPOT)
				{
					// NPOT拡張が使えるなら使う。

					// 2の累乗サイズのテクスチャでなくとも構わないならそうしたほうが
					// ビデオメモリの節約になって大変良い(´ー｀)ｖ

					// 2の累乗じゃなくてもいいならぴったりサイズで作成。
					sizeRX = sizeX;
					sizeRY = sizeY;

			//		npot = true;
				}
				else
				{
					sizeRX = Floor(sizeX);
					sizeRY = Floor(sizeY);
				}
				
				textureType = Gl.GL_TEXTURE_2D;

				widthRate = ((float)sizeX) / sizeRX;
				heightRate = ((float)sizeY) / sizeRY;
			}

			using (Surface dupSurface = new Surface())
			{
				// 複製を作成するかのフラグ
				bool bDup = false;

				// カラーキーが指定されているのか
				bool bColorKey = colorKey.ColorKeyType != ColorKeyType.None
					|| surface.SDLColorKey != null;

				// α付きのサーフェースを作成する必要があるのか？
				bool bAlpha = surface.Alpha || bColorKey;

				// colorkeyの実現のために元のサーフェースを書き換えるので
				// 複製をとる必要がある。
				if (bColorKey && bKeepSurface)
					bDup = true;

				if (!bAlpha)
				{
					if (!surface.CheckRGB888() || sizeX != sizeRX || sizeY != sizeRY)
						bDup = true;
				}
				else
				{
					if (!surface.CheckARGB8888() || sizeX != sizeRX || sizeY != sizeRY)
						bDup = true;
				}

				//　複製を作成するのか
				if (bDup)
				{	// bDupフラグが立っていれば無条件に複製する
					dupSurface.CreateDIB(sizeRX, sizeRY, bAlpha /* surface.Alpha*/ );
					dupSurface.Blt(surface, 0, 0);
				}

				/*	//	書き換える例)
					SDL_Surface* s = dupSurface.getSurface();
					SDL_LockSurface(s);

					for(int i=0;i < s.w * s.h ; ++i){
						((ubyte*)s.pixels)[i*4 + 3] = 255;
					}

					SDL_UnlockSurface(s);
				*/

				if (bColorKey)
				{
						//	カラーキーが有効なので、カラーキーの該当部分のαを0に
						//	書き換えていく作業が必要となる。(気が遠くなりそう…)
						uint colorKeyMask = SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN) ? 0xffffff00 : 0x00ffffff;
						uint* p0 = (uint*)
							(bDup ? dupSurface.Pixels :
							surface.Pixels);

						if (colorKey.ColorKeyType != ColorKeyType.None)
						{
							uint* p = p0;
							uint colorKeyValue;
							if (colorKey.ColorKeyType == ColorKeyType.ColorKeyRGB)
							{
								int r = colorKey.R;
								int g = colorKey.G;
								int b = colorKey.B;
								colorKeyValue =
									(uint)
										(SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN)
											? b + (g << 8) + (r << 16) : (b << 16) + (g << 8) + r);
							}
							else
							{
								int cx = colorKey.CX;
								int cy = colorKey.CY;
								if (cx < sizeX && cy < sizeY)
								{
									colorKeyValue = p[cx + cy * sizeRX] & colorKeyMask;
								}
								else
								{
									//	範囲外だとダメじゃん..
									goto exitColorKey;
								}
							}
							for (int y = 0; y < sizeY; ++y)
							{
								for (int x = 0; x < sizeX; ++x)
								{
									uint data = p[x];
									if ((data & colorKeyMask) == colorKeyValue)
									{
										p[x] = colorKeyValue; // α=0なので抜きを意味する
									}
								}
								p += sizeRX;	//	1ラスタ送る
							}
						}

						// SDLのBltは、転送先がα付きで転送元がColorKeyが指定されていれば
						// ColorKeyが指定されている部分は転送されない == その部分のαは0となる。
						/*
						if (surface.ColorKey != null)
						{
							uint colorKey = (uint)surface.ColorKey;

							uint* p = p0;
							for (int y = 0; y < sizeY; ++y)
							{
								for (int x = 0; x < sizeX; ++x)
								{
									uint data = p[x];
									if ((data & colorKeyMask) == colorKey)
									{
										p[x] = colorKey; // α=0なので抜きを意味する
									}
								}
								p += sizeRX;	//	1ラスタ送る
							}
						}
						 */
					}
			exitColorKey:

				//	textureの生成
				fixed (uint* p = &textureName)
				{
					Gl.glGenTextures(1, (IntPtr)p);
				}
				// textureNameは1から割り当てられると考えられる。
				// 0が返ってきたら、割り当てに失敗していると思うのだが。

				if (textureName == 0)
				{
					// これ 割り当て失敗でね？
					this.Release();
					return YanesdkResult.HappenSomeError;
				}

				//	bind
				loaded = true;	//	これをtrueにしておかないとbindできない
				Bind();

				uint internalformat = bAlpha ? Gl.GL_RGBA : Gl.GL_RGB;
				{
					// テクスチャ圧縮を用いるのか？

					bool s3tc = localOption.S3TC;
					bool fxt1 = localOption.FXT1;

					GlExtensions exts = GlExtensions.Instance;

					// テクスチャ圧縮を行ないたい場合
					if ((s3tc || fxt1)
					//	&& !npot // s3tcはnpotとは併用できる
						&& !txrc // s3tcはTxRcとは併用できない
						&& exts.IsAvailable("GL_ARB_texture_compression"))
					{
						// S3TCが使えるのか？
						if (s3tc
							&& exts.IsAvailable("GL_EXT_texture_compression_s3tc"))
						{
							internalformat = bAlpha ?
								Gl.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT :
								Gl.GL_COMPRESSED_RGB_S3TC_DXT1_EXT;
							//
							// RGBAの方は、DXT1、DXT3、DXT5の3種類があり、
							// DXT1はαが1ビットらしいので除外するとしても、
							// DXT3はα値が急激に変化するフォントなどに向いていて、
							// DXT5はグラデーションなどの、α値が緩やかに変化する場合に向いている…らしい。
							//
							// まぁ、あんまり深く考えずにどちらか決めうちでいい気も。。(´ω`)
							//
						}
						// FXT1が使えるのか？
						else if (fxt1
							&& exts.IsAvailable("GL_3DFX_texture_compression_FXT1"))
						{
							internalformat = bAlpha ?
								Gl.GL_COMPRESSED_RGBA_FXT1_3DFX :
								Gl.GL_COMPRESSED_RGB_FXT1_3DFX;
						}
					}
				}

				IntPtr v = bDup ? dupSurface.Pixels : surface.Pixels;

				Gl.glTexImage2D(textureType,
					0,	//	texture level
					(int)internalformat,
					sizeRX, sizeRY,
					0,	//	texture境界は存在しない
					bAlpha ? Gl.GL_RGBA : Gl.GL_RGB,	// α付き画像ならば GL_RGBAを指定する
					Gl.GL_UNSIGNED_BYTE, v
					);

				uint error = Gl.glGetError();
				global::System.Diagnostics.Debug.Assert(error == Gl.GL_NO_ERROR
					,Glu.gluErrorString(error)  );
				if (error != 0)
				{
					Unbind();
					this.Release();
					return YanesdkResult.HappenSomeError;
				}

				int option = (int)(localOption.Smooth ? Gl.GL_LINEAR : Gl.GL_NEAREST);

				Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, option);
				Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, option);

				//	surface.Dispose();	//	画像もったいないから解放しておく
				// ここではなくて、どこか適切な場所でするべき…か。

				this.alpha = bAlpha;

				Unbind();

			}	// dupSurface.Dispose();	//	no more needed

			return YanesdkResult.NoError;
		}
		#endregion

		#region このテクスチャの内部的に保持している情報
		private uint textureName;	//	openGLではテクスチャー名(handle)は整数
		private int sizeX, sizeY;	//	テクスチャーサイズ
		private int sizeRX, sizeRY;	//	テクスチャーの実サイズ

		/// <summary>
		/// 指定されたテクスチャーサイズと
		///	実際に確保されたサイズとの比
		/// ただし、拡張テクスチャを用いている場合は、テクスチャの実サイズ。
		/// </summary>
		private float widthRate, heightRate;
		#endregion

		#region ILoaderの実装
		///	<summary>指定されたファイル名の画像を読み込む。</summary>
		///	<remarks>
		/// 
		/// ここで読み込んだリソースは、VideoCache(→UnmanagedResourceManager)が
		/// いっぱいになると自動的に解放する。
		/// 
		/// Bindするときにそのチェックを行ない、解放されていれば自動的に再構築するので
		/// 通常、意識する必要は無いが、解放～再構築されることがあるので
		///	ここで読み込んだテクスチャのピクセルを直接いじってはいけない。
		/// 
		/// 直接いじりたいなら、まずSurfaceに読み込ませて、それをSetSurfaceで設定してから
		/// 使うこと。あるいは、解放されないようにVideoCacheのLimitを調整するか、
		/// 毎フレーム使用するなどして、解放されないようにすること。
		///	</remarks>
		public YanesdkResult Load(string filename)
		{
			Release();

			using (Surface tmpSurface = new Surface())
			{
				YanesdkResult result = tmpSurface.Load(filename);
				if (result == YanesdkResult.NoError)
					result = InnerSetSurface(tmpSurface, false);	// surface,dup/init

				if (result == YanesdkResult.NoError)
				{
					this.fileName = filename;

					// もし、ローダー以外からファイルを単に読み込んだだけならば、Reconstructableにしておく。
					if (constructInfo == null)
					{
						constructInfo = new TextrueConstructAdaptor(filename, this.ColorKey);
					}

					// リソースサイズが変更になったことをcache systemに通知する
					// Releaseのときに0なのは通知しているので通知するのは正常終了時のみでok.
					CacheSystem.OnResourceChanged(this);
				}

				return result;
			}
		}

		/// <summary>
		/// ファイルを読み込んでいる場合、読み込んでいるファイル名を返す
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}
		private string fileName;

		/// <summary>
		///	画像を読み込んでいるか
		/// </summary>
		/// <returns></returns>
		public bool Loaded
		{
			get { return loaded; }
		}
		private bool loaded;			//	テクスチャーを読み込んでいるか

		/// <summary>
		/// テクスチャーの解放
		/// loadで読み込んだ画像を解放する。
		/// </summary>
		public void Release()
		{
			sizeX = sizeY = sizeRX = sizeRY = 0;
			widthRate = heightRate = 0;
			alpha = false;
			if (loaded)
				unsafe
				{
					uint n = textureName;
					if (n != 0) // 読み込み途中で失敗した場合0ということもありえる。
					{
						Gl.glDeleteTextures(1, (IntPtr)(&n));
					}
					loaded = false;
					fileName = null;
				}

			// リソースサイズが変更になったことをcache systemに通知する
			CacheSystem.OnResourceChanged(this);

			constructInfo = null;
		}

		#endregion

		#region overridden from base class(CachedObject)

		/// <summary>
		///	確保しているテクスチャで使用しているメモリサイズを取得する
		/// </summary>
		/// <remarks>
		/// 単位は[byte]。cacheシステムでテクスチャとして使用している
		///	サイズを計測するのに使用すると良い。
		/// 
		/// sizeX * sizeY * (alpha?4:3)が返る。
		/// </remarks>
		/// <returns></returns>
		public override long ResourceSize
		{
			get { return sizeRX * sizeRY * (alpha ? 4 : 3); }
		}

		protected override YanesdkResult OnReconstruct(object param)
		{
			TextrueConstructAdaptor info = param as TextrueConstructAdaptor;

			ColorKey = info.ColorKey;
			return Load(info.FileName);
		}

		#endregion

		#region Debug用
		/// <summary>
		/// debug用に情報を文字列化する
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return string.Format("FileName : {0} , ResourceSize : {1} , IsLost : {2}",
				FileName,
				ResourceSize,
				IsLost.ToString()
			);
		}
		#endregion
	}
}

