using System;

using Sdl;

using Yanesdk.Ytl;
using Yanesdk.System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 描画のためのサーフェース
	/// </summary>
	/// <remarks> 
	///	2D描画、3D描画のときに読み込み等に用いられるDIB的なサーフェース。
	///	bmp,gif,png,jpeg等の読み込みとbmpでの書き出しをサポート。
	/// <code>
	///		Surface s = new Surface();
	///		s.load("1.jpg");
	///		s.saveBMP("1.bmp");
	///		Console.WriteLine(s.Width.ToString() + "×"+ s.Height.ToString());
	/// </code>
	/// 
	/// CachedObject派生クラスではあるが、書き換えられることがありうるし、
	/// いつ使われたのかアクセスマーカーをつけるタイミングが難しいので
	/// Yanesdk.NETで扱うunmanaged resourceとしては例外的に自動解放能力は持たせない。
	/// よって自前で解放する必要があるので要注意。
	/// 
	/// </remarks>
	public class Surface : CachedObject , ILoader , IDisposable
	{

		#region ctor & Dispose
	/* // SDLInitializerで一括して行なうことにした。
		/// <summary>
		/// static class constructorはlazy instantiationが保証されているので
		/// ここで SDL_imageが間接的に読み込むdllを事前に読み込む。
		/// </summary>
		static Surface()
		{
			// image関連のDLLを必要ならば読み込んでおくべ。

			DllManager d = DllManager.Instance;
			string current = DllManager.DLL_CURRENT;
			d.LoadLibrary(current, DLL_SDL_IMAGE);
			d.LoadLibrary(current, DLL_ZIP);
			d.LoadLibrary(current, DLL_PNG);
			d.LoadLibrary(current, DLL_JPEG);
			d.LoadLibrary(current, DLL_TIFF);
		}
	*/

		/// <summary>
		/// 
		/// </summary>
		public Surface()
		{
			This = this;
			
			CacheSystem = UnmanagedResourceManager.Instance.VideoMemory;
			CacheSystem.Add(this);
		}

		/// <summary>
		/// Disposeを呼び出してしまうと二度とLoadできない。
		/// 単にサーフェースを解放するだけならReleaseを呼び出すこと。
		/// </summary>
		public void Dispose()
		{
			Release();

			CacheSystem.Remove(this);
		}
		#endregion

		#region ILoaderの実装

		/// <summary>
		/// サーフェースへの画像読み込みメソッド。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns>読み込み失敗のときは非0が返る。</returns>
		/// <remarks>
		/// bmp画像,png画像を読み込みサポート。
		/// </remarks>
		public YanesdkResult Load(string filename)
		{
			RWopsH = FileSys.ReadRW(filename);
			if (RWopsH.Handle != IntPtr.Zero) {
				surface = SDL.IMG_Load_RW(RWopsH.Handle, 0);
				if (surface != IntPtr.Zero) {

				//	// この時点でsurfaceのαチャンネルの有無を判定して、メンバalphaに反映させる必要あり。
				//	alpha = getAlpha(surface);
					// →α channelの判定は動的に行なうように変更する

					fileName = filename;

					CacheSystem.OnResourceChanged(this);
					return YanesdkResult.NoError;
				}
				return YanesdkResult.FileReadError;
			}
			else {
				return YanesdkResult.FileNotFound;
			}
		}

		/// <summary>
		/// サーフェースの解放
		/// </summary>
		public void Release()
		{
			if (surface != IntPtr.Zero)
			{
				SDL.SDL_FreeSurface(surface);
				// フォントを使ったときに↑ここでアクセス違反が出るなら
				// SDL_ttf.dllのバージョンが古い。
				// 詳しくはYanesdk.NETのreadme.txtの更新履歴2006/10/28を見ること。

				surface = IntPtr.Zero;
			}
			if (RWopsH.hMem != IntPtr.Zero)
			{
				global::System.Runtime.InteropServices.Marshal.FreeHGlobal(RWopsH.hMem);
				RWopsH.hMem = IntPtr.Zero;
				RWopsH.Handle = IntPtr.Zero;
			}
			fileName = null;

			CacheSystem.OnResourceChanged(this);
			// this.alpha = false; // defaultでfalse
		}

		/// <summary>
		/// ファイルから読み込みしている場合
		/// そのファイル名を取得する
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}
		private string fileName = null;

		/// <summary>
		/// ファイルから画像が読み込まれているのかを返す
		/// </summary>
		public bool Loaded
		{
			get { return fileName != null; }
		}

		#endregion

		#region CachedObjectの実装

		/// <summary>
		/// このオブジェクトが利用しているリソースのサイズを得る
		/// </summary>
		public override long ResourceSize
		{
			get { return Pitch * Height; } // まあ下が余ってることは考えんでええやろ…
		}

		public override bool Reconstructable
		{
			get
			{
				// 自動解放能力を持たない
				return false;
			}
		}

		#endregion

		#region methods

		#region クリア
		///	サーフェースをクリア
		public YanesdkResult Clear()
		{
			if (surface != IntPtr.Zero)
			{
				return SDL.SDL_FillRect(surface, IntPtr.Zero, 0) == 0 ?
					YanesdkResult.NoError : YanesdkResult.SdlError;
			}
			return YanesdkResult.PreconditionError;
		}
		#endregion

		#region 生成系

		/// <summary>
		/// RGB888 or RGBA8888のサーフェースを作成する。
		/// </summary>
		/// <param name="x">サーフェースサイズ(x)</param>
		/// <param name="y">サーフェースサイズ(y)</param>
		/// <param name="bAlpha">
		/// bAlphaがtrueのときはRGBA8888(1pixelがR,G,B,A各8bitα付き32bit)
		/// bAlphaがfalseのときはRGB888(1pixelがRGB各8bit,24bit)
		/// のサーフェースを作成する。
		/// </param>
		/// <returns>失敗時には非0が返る。</returns>
		public YanesdkResult CreateDIB(int x, int y, bool bAlpha)
		{
			Release();

			YanesdkResult result = CreateDIBstatic(out surface, x, y, bAlpha);

			// 作成が成功したときにのみ更新
			//	if ( result == YanesdkResult.NoError )
			//		this.alpha = bAlpha;

			return result;
		}

		/// <summary>
		/// createDIBのstaticバージョン
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="bAlpha"></param>
		/// <returns></returns>
		public static YanesdkResult CreateDIBstatic(out IntPtr surface, int x, int y, bool bAlpha)
		{
			// ベタ書きでいいでしょ
			if (SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN))
			{
				if (bAlpha)
				{
					surface = SDL.SDL_CreateRGBSurface(
						SDL.SDL_SWSURFACE,
						x, y, 32,
						0xff000000,		// Rmask
						0x00ff0000, 	// Gmask
						0x0000ff00,		// Bmask
						0x000000ff		// Amask
						);
				}
				else
				{
					surface = SDL.SDL_CreateRGBSurface(
						SDL.SDL_SWSURFACE,
						x, y, 24,
						0xff000000,		// Rmask
						0x00ff0000, 	// Gmask
						0x0000ff00,		// Bmask
						0x00000000		// Amask
						);
				}
			}
			else
			{
				if (bAlpha)
				{
					surface = SDL.SDL_CreateRGBSurface(
						SDL.SDL_SWSURFACE,
						x, y, 32,
						0x000000ff,		// Rmask
						0x0000ff00, 	// Gmask
						0x00ff0000,		// Bmask
						0xff000000		// Amask
						);
				}
				else
				{
					surface = SDL.SDL_CreateRGBSurface(
						SDL.SDL_SWSURFACE,
						x, y, 24,
						0x000000ff,		// Rmask
						0x0000ff00, 	// Gmask
						0x00ff0000,		// Bmask
						0x00000000		// Amask
						);
				}
			}
			if (surface == IntPtr.Zero) return YanesdkResult.SdlError; // 作成失敗
			return YanesdkResult.NoError;
		}

		/// <summary>
		/// ウィンドウのクライアント領域をキャプチャする。
		/// 失敗時はnullが返る。
		///	Form.Handleを渡すべし。
		/// 
		/// Windows専用。
		/// </summary>
	    public static Surface FromHandle(IntPtr hwnd)
		{
			// Windows専用である
			if (System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);

			RECT rect;
			GetClientRect(hwnd, out rect);

			using (Bitmap bmp = new Bitmap(rect.right - rect.left, rect.bottom - rect.top))
			{
				// HDCを取得してキャプチャ
				using (Graphics g = Graphics.FromImage(bmp))
				{
					IntPtr hdc = GetDC(hwnd);
					int rop = SRCCOPY;
					if (4 < Environment.OSVersion.Version.Major)
					{
						// layerd windowもキャプチャするにはこれも指定する必要がある。
						rop |= CAPTUREBLT;
					}
					BitBlt(g.GetHdc(), 0, 0, bmp.Width, bmp.Height, hdc, 0, 0, rop);
					ReleaseDC(hwnd, hdc);
				}
				// BitmapからSurfaceを作成
				Surface surface;
				if (BitmapHelper.BitmapToSurface(bmp, out surface) != YanesdkResult.NoError)
				{
					return null;
				}
				return surface;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int left, top, right, bottom;
		}

		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		static extern IntPtr GetDC(IntPtr hwnd);

		[DllImport("user32.dll")]
		static extern IntPtr ReleaseDC(IntPtr hwnd, IntPtr hdc);

		[DllImport("gdi32.dll")]
		static extern int BitBlt(IntPtr hDestDC,
			int x, int y, int nWidth, int nHeight,
			IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

		const int SRCCOPY = 0x00CC0020;
		const int CAPTUREBLT = 0x40000000; // 半透明ウィンドウを含めるオプション

		#endregion

		#region 転送系
		/// <summary>
		/// 画像の転送。
		/// </summary>
		/// <param name="src">転送元画像。</param>
		/// <param name="x">転送先x座標。</param>
		/// <param name="y">転送先y座標。</param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// これ以上の転送を期待しているなら、SDL gfxをportingしてくるべき。
		/// </para>
		/// </remarks>
		public YanesdkResult Blt(Surface src, int x, int y)
		{
			//	αを無効化しておかないとARGB→ARGBのbltで
			//	αを考慮して転送しやがる
			SDL.SDL_SetAlpha(src.SDL_Surface, 0, 0);
			return Blt_(src, x, y);
		}

		public YanesdkResult Blt(Surface src, int x, int y, byte alpha)
		{
			SDL.SDL_SetAlpha(src.SDL_Surface, SDL.SDL_SRCALPHA, alpha);
			return Blt_(src, x, y);
		}

		/// <summary>
		/// キーカラー付き転送。アルファも有効。(255でアルファ無し、128で半透明)
		/// </summary>
		/// <param name="src"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="keyColor"></param>
		/// <returns></returns>
		public YanesdkResult Blt(Surface src, int x, int y, Color4ub keyColor)
		{
			SDL.SDL_SetAlpha(src.SDL_Surface, SDL.SDL_SRCALPHA, keyColor.A);
			uint key = (uint)(keyColor.R << 16 | keyColor.G << 8 | keyColor.B);
			SDL.SDL_SetColorKey(src.SDL_Surface, SDL.SDL_SRCCOLORKEY, key);
			return Blt_(src, x, y);
		}

		unsafe private YanesdkResult Blt_(Surface src, int x, int y)
		{
			if (surface == IntPtr.Zero) return YanesdkResult.PreconditionError;	// 転送先が構築されていない
			if (src.surface == IntPtr.Zero) return YanesdkResult.InvalidParameter; // 転送元が構築されていない

			SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)src.surface;
			SDL.SDL_Rect dest = new SDL.SDL_Rect();
			dest.x = (short)x;
			dest.y = (short)y;
			dest.w = (ushort)surface_->w;
			dest.h = (ushort)surface_->h;

			return SDL.SDL_BlitSurface(src.surface, IntPtr.Zero, surface, (IntPtr)(&dest)) == 0 ?
				YanesdkResult.NoError : YanesdkResult.SdlError;
		}
		#endregion

		#region 保存系
		///	サーフェースの内容をBMP形式で保存する
		public YanesdkResult SaveBMP(string filename)
		{
			if (surface != IntPtr.Zero)
			{
				return SDL.SDL_SaveBMP(surface, filename) == 0 ?
					YanesdkResult.NoError : YanesdkResult.SdlError;
			}
			else
			{
				return YanesdkResult.PreconditionError; // no surface
			}
		}
		#endregion

		#region リージョンを作成するメソッド群

		/// <summary>
		/// SDLColorKeyのところをくり抜いたリージョン、
		/// あるいはα値が255のところのリージョンを作成する。
		/// αが無くSDLColorKeyもセットされていない場合、nullが返される。
		/// </summary>
		public Region MakeRegion()
		{
			return InnerMakeRegion(false, 0, 0, 0);
		}

		/// <summary>
		/// 指定された色をくり抜いたリージョンを作成する
		/// </summary>
		public Region MakeRegion(int r, int g, int b)
		{
			return InnerMakeRegion(true, (uint)r, (uint)g, (uint)b);
		}

		/// <summary>
		/// 指定された位置の色をくり抜いたリージョンを作成する
		/// </summary>
		public Region MakeRegion(int cx, int cy)
		{
			Color4ub c = GetPixel(cx, cy);
			return InnerMakeRegion(true, c.R, c.G, c.B);
		}

		/// <summary>
		/// リージョンを作成する
		/// </summary>
		private unsafe Region InnerMakeRegion(bool bColorKey, uint r, uint g, uint b)
		{
			if (!Alpha && !SDLColorKey.HasValue && !bColorKey)
			{
				// αとかカラーキーが無いならどうにも出来ないので。。
				return null;
			}

			Region region = new Region(new Rectangle(0, 0, 0, 0));
			// ↑ region = new Region(); では上手くいかない。何でだろ？(´ω`)

			using (Surface dupSurface = new Surface())
			{
				Surface src;

				// RGB888とか以外の形式なら変換する
				if (Alpha && !CheckARGB8888())
				{
					dupSurface.CreateDIB(Width, Height, true);
					dupSurface.Blt(this, 0, 0);
					src = dupSurface;
				}
				else if (!Alpha && !CheckRGB888())
				{
					dupSurface.CreateDIB(Width, Height, false);
					dupSurface.Blt(this, 0, 0);
					src = dupSurface;
				}
				else
				{
					src = this;
				}

				if (bColorKey || SDLColorKey != null)
				{
					uint colorKeyMask = SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN) ?
						0xffffff00 : 0x00ffffff;

					// カラーキーの取得
					uint colorKey;
					if (src.SDLColorKey.HasValue)
					{
						colorKey = (uint)src.SDLColorKey.Value & colorKeyMask;
					}
					else // if (bColorKey)
					{
						colorKey = SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN) ?
							b + (g << 8) + (r << 16) :
							(b << 16) + (g << 8) + r;
					}

					byte* pixels = (byte*)Pixels;
					int bytePerPixel = Alpha ? 4 : 3; // こうやっちゃうと各ピクセル毎にかけ算になっちゃうが。。

					for (int y = 0, h = Height, w = Width; y < h; y++)
					{
						int startX = -1;
						for (int x = 0; x < w; x++)
						{
							uint pixel = *(uint*)(pixels + x * bytePerPixel);
							if ((pixel & colorKeyMask) == colorKey)
							{
								// カラーキーなら透過させる
								if (0 <= startX)
								{
									region.Union(new Rectangle(startX, y, x - startX, 1));
									startX = -1;
								}
							}
							else
							{
								if (startX < 0)
								{
									startX = x;
								}
							}
						}
						if (0 <= startX)
						{
							region.Union(new Rectangle(startX, y, w - startX, 1));
							//startX = -1;
						}
						pixels += Pitch;
					}
				}
				else // if (Alpha)
				{
					// α値による処理

					uint alphaMask = SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN) ?
						0x000000ff : 0xff000000;

					uint* pixels = (uint*)Pixels;
					for (int y = 0, h = Height, w = Width; y < h; y++)
					{
						int startX = -1;
						for (int x = 0; x < w; x++)
						{
							if ((pixels[x] & alphaMask) != alphaMask)
							{
								// α != 255なら透過させる
								if (0 <= startX)
								{
									region.Union(new Rectangle(startX, y, x - startX, 1));
									startX = -1;
								}
							}
							else
							{
								if (startX < 0)
								{
									startX = x;
								}
							}
						}
						if (0 <= startX)
						{
							region.Union(new Rectangle(startX, y, w - startX, 1));
							//startX = -1;
						}
						pixels += Pitch / sizeof(uint);
					}
				}
			}

			return region;
		}

		#endregion

		#endregion

		#region properties

		/// <summary>
		/// サーフェースの幅を取得
		/// </summary>
		public int Width {
			get { return getWidth_(); }
		}

		unsafe private int getWidth_() {
			if (surface != IntPtr.Zero) {
				SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
				return surface_->w;
			}
			else {
				return 0;
			}
		}

		///	サーフェースのサイズを取得
		public void GetSize(ref int x, ref int y)
		{
			getSize_(ref x, ref y);
		}

		unsafe private void getSize_(ref int x, ref int y)
		{
			if (surface != IntPtr.Zero)
			{
				SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
				x = surface_->h; y = surface_->h;
			}
			else
			{
				x = y = 0;
			}
		}

		/// <summary>
		/// サーフェースの1ラインのpitch [byte]
		/// </summary>
		public int Pitch
		{
			get { return getPitch(); }
		}

		unsafe private int getPitch()
		{
			if (surface != IntPtr.Zero)
			{
				SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
				return surface_->pitch;
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// サーフェースの高さを取得
		/// </summary>
		/// <returns></returns>
		public int Height {
			get { return getHeight_(); }
		}
		
		unsafe private int getHeight_() {
			if (surface != IntPtr.Zero) {
				SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
				return surface_->h;
			}
			else {
				return 0;
			}
		}

		/// <summary>
		/// Alphaチャンネルを持つかどうかを返す
		/// </summary>
		public bool Alpha
		{
			get { return getAlpha(); }
		}

		unsafe private bool getAlpha()
		{
			if ( surface == IntPtr.Zero )
				return false;

			SDL.SDL_Surface* surface_ = ( SDL.SDL_Surface* ) surface;
			SDL.SDL_PixelFormat* format = ( SDL.SDL_PixelFormat* ) surface_->format;

			return ( format != null && format->Amask != 0 );
			// alpha maskを持つならば、alpha channelがあるのだろう。
		}

		/// <summary>
		///	SDL_Surfaceを取得する
		/// </summary>
		/// <remarks>
		/// 内部で使用しているサーフェースを取得する。
		///	(通常使うことはない)
		/// setすると内部で使用しているサーフェースとして外部のSDL_Surfaceを設定する。
		///	(通常使うことはない)
		/// 
		/// setterで設定するときに前のSurfaceは解放されてしまう。この動作がまずいのであれば、
		/// Unbind_SDL_Surfaceを用いるべし。
		/// </remarks>
		/// <returns></returns>
		public IntPtr SDL_Surface
		{
			get { return surface; }
			set
			{
				Release();
				surface = value;
			}
		}

		/// <summary>
		/// SDL_SurfaceにIntPtr.Zeroを代入する。
		/// (このとき前のものをReleaseしない)
		/// </summary>
		public void Unbind_SDL_Surface()
		{
			surface = IntPtr.Zero;
		}

		/// <summary>
		///	内部的に作成されたサーフェースがRGB888なのか判定する
		/// </summary>
		/// <returns>RGB888ならばtrueが返る。BGR888やARGB8888ならfalse。</returns>
		public bool CheckRGB888()
		{
			return CheckRGB888_();
		}

		unsafe private bool CheckRGB888_()
		{
			if (surface == IntPtr.Zero) return false;
			SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
			SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)surface_->format;
			if (format == null) return false;

			if (SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN))
			{
				return format->BitsPerPixel == 24
					&& format->Rmask == 0xff000000
					&& format->Gmask == 0x00ff0000
					&& format->Bmask == 0x0000ff00
					//	Amaskに関しては条件なし
					;
			}
			else
			{
				return format->BitsPerPixel == 24
					&& format->Rmask == 0x000000ff
					&& format->Gmask == 0x0000ff00
					&& format->Bmask == 0x00ff0000
					//	Amaskに関しては条件なし
					;
			}
		}


		/// <summary>
		/// 内部的に作成されたサーフェースがARGB8888かどうかを判定する
		/// </summary>
		/// <returns>ARGB8888ならばtrue。</returns>
		public bool CheckARGB8888()
		{
			return CheckARGB8888_();
		}

		unsafe private bool CheckARGB8888_()
		{
			if (surface == IntPtr.Zero) return false;
			SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
			SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)surface_->format;
			if (format == null) return false;

			if (SDL.SDL_BYTEORDER.Equals(SDL.SDL_BIG_ENDIAN))
			{
				return format->BitsPerPixel == 32
				&& format->Rmask == 0xff000000
				&& format->Gmask == 0x00ff0000
				&& format->Bmask == 0x0000ff00
				&& format->Amask == 0x000000ff
				;
			}
			else
			{
				return format->BitsPerPixel == 32
				&& format->Rmask == 0x000000ff
				&& format->Gmask == 0x0000ff00
				&& format->Bmask == 0x00ff0000
				&& format->Amask == 0xff000000
				;
			}
		}

		/// <summary>
		///	画像データへのポインタを返す
		///	画像が読み込まれていないときはIntPtr.Zeroが返る。
		/// </summary>
		/// <returns></returns>
		public IntPtr Pixels
		{
			get { return GetPixels_(); }
		}

		unsafe private IntPtr GetPixels_()
		{
			if (surface == IntPtr.Zero) return IntPtr.Zero;
			SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
			return surface_->pixels;
		}

		public Color4ub GetPixel(int x, int y)
		{
			return GetPixel_(x, y);
		}

		unsafe private Color4ub GetPixel_(int x, int y)
		{
			SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
			SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)surface_->format;
			uint u = 0;
			int i;

			switch (format->BitsPerPixel)
			{
				/*
				case 1:
					i = *((byte*)GetPixels_() + y * surface_->pitch + x / 8);
					return GetPaletteColor((i >> (x % 8)) & 0x01);
				case 2:
					i = *((byte*)GetPixels_() + y * surface_->pitch + x / 4);
					return GetPaletteColor((i >> (x % 4) * 2) & 0x03);
				case 4:
					i = *((byte*)GetPixels_() + y * surface_->pitch + x / 2);
					return GetPaletteColor((i >> (x % 2) * 4) & 0x0f);
				*/
				// ↑SDLがそもそもサポートしてないようなので要らない。
				case 8:
					i = *((byte*)GetPixels_() + y * surface_->pitch + x);
					return GetPaletteColor(i);
				case 16:
					u = *(ushort*)((byte*)GetPixels_() + y * surface_->pitch + x * 2);
					break;
				case 24:
					byte* p = ((byte*)GetPixels_()) + (y * surface_->pitch + x * 3);
					u = (uint)(p[2] << 16 | p[1] << 8 | p[0]);
					break;
				case 32:
					u = *(uint*)((byte*)GetPixels_() + y * surface_->pitch + x * 4);
					break;
			}

			return new Color4ub((byte)((u & format->Rmask) >> format->Rshift),
								(byte)((u & format->Gmask) >> format->Gshift),
								(byte)((u & format->Bmask) >> format->Bshift),
								(byte)((u & format->Amask) >> format->Ashift));
		}

		/// <summary>
		/// パレットから色を取得
		/// </summary>
		/// <param name="i">パレットのインデックス</param>
		private unsafe Color4ub GetPaletteColor(int i)
		{
			SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
			SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)surface_->format;
			SDL.SDL_Palette* palette = (SDL.SDL_Palette*)format->palette;
			SDL.SDL_Color* colors = (SDL.SDL_Color*)palette->colors;
			global::System.Diagnostics.Debug.Assert(i < palette->ncolors);

			return new Color4ub(colors[i].r, colors[i].g, colors[i].b, 0xff);
		}

		/// <summary>
		/// Loadで読み込んだときに透過色指定付きのpngなどであればそれをcolorKeyとして保持する
		/// </summary>
		/// <remarks>
		/// このcolorkeyの値は、SDL_SDL_Surface.format.colorkeyの値そのまま。
		/// </remarks>
		public long? SDLColorKey
		{
			get { return getColorKey(); }
		}

		/// <summary>
		/// SDL_Surfaceに設定されているカラーキーを返すためのヘルパ
		/// </summary>
		/// <returns></returns>
		unsafe private long? getColorKey()
		{
			SDL.SDL_Surface* surface_ = (SDL.SDL_Surface*)surface;
			SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)surface_->format;

			// 転送元カラーキーが設定されていないのでnullを返す
			if ((surface_->flags & SDL.SDL_SRCCOLORKEY) == 0)
				return null;

			uint ckey = format->colorkey;
			return ckey;
		}

		#endregion

		#region private
		/// <summary>
		/// サーフェースの実体。
		/// </summary>
		protected IntPtr surface;

		/// <summary>
		/// 
		/// </summary>
		private SDL_RWopsH RWopsH;

		#endregion

	}
}
