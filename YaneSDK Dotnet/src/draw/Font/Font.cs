using System;
using System.Runtime.InteropServices;

using Sdl;
using Yanesdk.Ytl;
using Yanesdk.System;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 文字の描画用クラス
	/// </summary>
	/// <remarks>
	///	文字の描画にはttf(true type font)を使用します。
	///	文字の描画ごとに、ttfを展開して、テクスチャーorサーフェースを
	///	再構築していたのではすごく遅いので、何らかの方法でキャッシュして
	///	使うことをお勧めします。(→FontRepositoryを参考にすること)
	/// </remarks>
	/// <example>
	/// サンプルコード。
	/// <code>
	///		GlTexture fontTexture = new GlTexture();
	///		Yanesdk.Draw.Font font = new Yanesdk.Draw.Font();
	///		font.Open(0, 30);
	///		font.SetColor(200, 0, 50);
	///		font.Style = 1; 
	///		Surface surface = new Surface();
	///		font.DrawBlendedUnicode(surface,"こんにちは！abcde");
	///		fontTexture.SetSurface(surface);
	/// 
	///     // 上の最後の3行は、fontTexture.SetSurface(DrawBlended("こんにちは！abcde"));
	///     // と書くこともできる。
	/// 
	///		font.Close();
	/// </code>
	/// </example>
	public class Font : CachedObject , ILoader, IDisposable
	{
		#region フォントファイル名の定義

		const string gothic = "msgothic.ttc";				// Windowsのとき
		const string mincho = "msmincho.ttc";
		const string gothicLinux = "kochi-gothic-subst.ttf"; // Linuxのとき
		const string minchoLinux = "kochi-mincho-subst.ttf";
		const string gothicMacOS = "ヒラギノ角ゴ Pro W6.otf"; // MacOSのとき
        const string minchoMacOS = "ヒラギノ明朝 Pro W6.otf";

		#endregion

		#region ctor & Dispose
		/// <summary>
		/// 
		/// </summary>
		public Font()
		{
			This = this; // mix-in classにthisを渡す

			// defaultでUnmanagedResourceManagerのcache systemに接続
			CacheSystem = UnmanagedResourceManager.Instance.UnmanagedMemory;
			CacheSystem.Add(this);

			SetColor(255, 255, 255);
		}

		/// <summary>
		/// SDL TTF初期化用
		/// </summary>
		private RefSingleton<Yanesdk.Sdl.SDL_TTF_Initializer>
			init = new RefSingleton<Yanesdk.Sdl.SDL_TTF_Initializer>();

		/// <summary>
		/// Loadで読み込んだものの解放するにはReleaseを呼び出すべし。
		/// Disposeのほうは、Disposeしたあとは再度Load出来ない。
		/// </summary>
		public void Dispose()
		{
			Release();
			init.Dispose();

			CacheSystem.Remove(this);
		}

		#endregion

		#region ILoaderの実装

		/// <summary>フォントをオープンする。</summary>
		/// <remarks>
		/// <para>
		///	指定するのは、フォント名。
		///	indexは、ttc(true type collection＝ttfが複数入ったフォントファイル)の
		///	ときに、何番目のものかを指定する。0番から始まる番号。
		///	</para>
		/// <para>
		///	Windowsならフォルダは決まっているので、カレントとパスの通っている
		///	ところに無ければ、そこから取得する。
		///	</para>
		/// <para>
		///	Linuxのほうは、カレント、あとパスの通っているところから取得する。
		///	</para>
		/// </remarks>
		/// <example>
		///		例)
		///		Windowsでは、
		///		<list type="">
		///			msgothic.ttc : MSゴシック
		///			msmincho.ttc : MS明朝
		///		</list>
		///		<para>
		///		Linuxの場合、日本語フォントを用いる場合は、
		///		GT書体 (東京大学多国語処理研究会) 
		///		http://www.l.u-tokyo.ac.jp/GT/
		///		などから持ってきて、msgothic.ttcやmsmincho.ttcとリネームして
		///		カレントフォルダに配置しておくようにしてください。</para>
		///
		///		<para>
		///		フォントフォルダの検索に用いているAPIは、
		///		win95の場合はIE4.0以降をインストールしてないとダメ
		///		win98以降は使える。
		///		</para>
		/// </example>
		public YanesdkResult Load(string fontname,int fontsize, int index)
		{
			Release();

			YanesdkResult hr = Load(FileSys.ReadRW(fontname), fontsize, index);
			if (hr != YanesdkResult.NoError)
			{
				//	カレントにないので、SHGetSpecialFolderPath関数を使って
				//	フォントディレクトリを取得して頑張ってみる。

				string fontpath;
				switch (System.Platform.PlatformID)
				{
					case Yanesdk.System.PlatformID.Windows:
						fontpath = FileSys.ConcatPath(Environment.SystemDirectory, @"..\fonts");
						break;
					case Yanesdk.System.PlatformID.Linux:
						fontpath = "/usr/share/fonts/ja/TrueType/";
						break;
					case Yanesdk.System.PlatformID.MacOS:
						fontpath = "/System/Library/Fonts/";
						break;
					default:
						throw null;
				}
		
				hr = Load(FileSys.ReadRW(FileSys.MakeFullName(fontpath, fontname)), fontsize, index);
			}

			if (hr == YanesdkResult.NoError)
			{
				fileName = fontname;

				// もし、ローダー以外からファイルを単に読み込んだだけならば、Reconstructableにしておく。
				if (constructInfo == null)
				{
					constructInfo = new FontConstructAdaptor(fontname, fontsize, index);
				}
			}

			return hr;
		}

		/// <summary>フォントをオープンする。</summary>
		/// <remarks>openのindex=0決め打ちバージョン。</remarks>
		public YanesdkResult Load(string fontname,int fontsize)
		{
			return Load(fontname,fontsize,0);
		}

		/// <summary>
		/// Load(fontname,14)と等価。ILoadable interfaceを持たせるための
		/// ダミー的な実装。実際に使うことはない。
		/// </summary>
		/// <param name="fontname"></param>
		/// <returns></returns>
		public YanesdkResult Load(string fontname)
		{
			return Load(fontname , 14);
		}

		///	<summary>フォントをオープンする。</summary>
		/// <remarks>
		/// <para>
		///	MSゴシックとMS明朝をオープンするバージョン。
		/// Linux環境では「フォントファイル名の定義」のところを見ること。
		///	</para>
		/// <list type="">
		///	0 :	msgothic.ttc : MSゴシック
		///	1 :	msmincho.ttc : MS明朝
		///	2 :	msgothic.ttc : MSPゴシック
		///	3 :	msmincho.ttc : MSP明朝
		/// </list>
		///	をオープンします。
		/// </remarks>
		public YanesdkResult Load(int openNo,int fontsize)
		{
			string fontname;
			switch(openNo) {
				case 0:
				case 2:
					switch (Yanesdk.System.Platform.PlatformID)
					{
						case Yanesdk.System.PlatformID.Windows:
							fontname = gothic; break;
						case Yanesdk.System.PlatformID.Linux:
							fontname = gothicLinux; break;
						case Yanesdk.System.PlatformID.MacOS:
							fontname = gothicMacOS; break;
						default:
							throw null;
					}									
					break;

				case 1:
				case 3:

					switch (Yanesdk.System.Platform.PlatformID)
					{
						case Yanesdk.System.PlatformID.Windows:
							fontname = mincho; break;
						case Yanesdk.System.PlatformID.Linux:
							fontname = minchoLinux; break;
						case Yanesdk.System.PlatformID.MacOS:
							fontname = minchoMacOS; break;
						default:
							throw null;
					}
					break;
				default: return YanesdkResult.FileReadError;
			}
			return Load(fontname,fontsize,openNo/2);

			//	このttcの中身は、
			//	index : 0=普通の , 1=プロポーショナルフォント , 2 = UI用。
		}

		///	<summary>フォントをオープンする(RWopsから)。</summary>
		/// <remarks>
		///	indexは、ttc(true type collection＝ttfが複数入ったフォントファイル)の
		///	ときに、何番目のものかを指定する。0番から始まる番号。
		/// </remarks>
		public YanesdkResult Load(SDL_RWopsH rw,int fontsize,int index)
		{
			Release();
			if (fontsize <= 0 || rw.Handle == IntPtr.Zero) 
				return YanesdkResult.InvalidParameter;

			//	23pt未満であれば倍率掛けて拡大しておく
			rate = 1;
//			while (fontsize * rate < 23) ++rate;
			// ↑この処理、Soildで描画するときにおかしくなるので廃止にしよう。

			// フォントが23pt以下だと拡大する処理について。
			// SDL_ttf.dllが古いと↑これを入れないといけない。
			// 詳しくはYanesdk.NETのreadme.txtの更新履歴2006/10/28を見ること。

			font = SDL.TTF_OpenFontIndexRW(rw.Handle, 1, fontsize * rate, index);
			if (font != IntPtr.Zero) {
				//	読みこめたので、これをファイルサイズとする。
				bufferSize = rw.Length;
				bOpen = true;
				rwops = rw;
			}
	
			if (font == IntPtr.Zero)
				return YanesdkResult.FileReadError;

		//	fileName = ファイル名不明(´ω`)

			CacheSystem.OnResourceChanged(this);

			return YanesdkResult.NoError;
		}

		/// <summary>フォントをオープンする。</summary>
		/// <remarks>openのindex=0決め打ちバージョン。</remarks>
		public YanesdkResult Load(SDL_RWopsH rw,int fontsize)
		{
			return Load(rw,fontsize,0);
		}

		/// <summary>
		/// Loadで読み込んだものの解放
		/// Disposeのほうは、Disposeしたあとは再度Load出来ない。
		/// </summary>
		public void Release()
		{
			if (bOpen)
			{
				SDL.TTF_CloseFont(font);
				bOpen = false;
				font = IntPtr.Zero;
				Marshal.FreeHGlobal(rwops.hMem);
				bufferSize = 0;
				fileName = null;

				CacheSystem.OnResourceChanged(this);
				constructInfo = null;
			}
		}

		/// <summary>
		/// フォントを読み込み済みかどうかを表すフラグ
		/// </summary>
		public bool Loaded
		{
			get { return bOpen; }
		}

		/// <summary>
		/// 読み込んでいるフォントファイル名
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}
		string fileName;

		#endregion

		#region properties
		///	<summary>文字の色を設定する。</summary>
		/// <remarks>
		/// <para>
		///	各、RGB、0～255までで指定する。
		///	drawを呼び出すまでに設定すればok。
		///	</para>
		/// <para>
		///	設定しなければ、(255,255,255) すなわち、白が指定されている状態になる。
		///	設定された値は、再設定されるまで有効。
		///	</para>
		/// </remarks>
		public void SetColor(int r_,int g_,int b_)
		{
			color.r = (byte)r_; color.g = (byte)g_; color.b = (byte)b_;
		}

		/// <summary>
		/// フォントのスタイルを設定/取得する。
		/// TTF_STYLE_BOLD      : 1
		/// TTF_STYLE_ITALIC    : 2
		/// TTF_STYLE_UNDERLINE : 4
		/// 
		/// 例) boldかつunderlineを入れるには、1+4 = 5を指定する
		/// 
		/// ただし、SDLが独自にやっているため見づらくなる可能性あり。
		/// </summary>
		/// <param name="style"></param>
		public int Style
		{
			get { return style; }
			set { style = value; }
			// これを
			//		SDL.TTF_SetFontStyle(font, style);
			// で反映させるのは描画直前にする。
		}
		private int style = 0;

		#endregion

		#region 文字描画メソッド群
		///	<summary>文字を描画したサーフェースを返します。</summary>
		/// <remarks>
		/// <para>
		///	漢字は出ない。
		///	漢字を出したいときはUTF8かUnicodeバージョンを用いること。
		///	</para>
		/// <para>
		///	生成されるサーフェースは、αなし。
		///	</para>
		/// </remarks>
		public Surface DrawSolid(string str)
		{
			Surface surface = new Surface();
			DrawSolid(surface,str);
			return surface;
		}

		///	<summary>サーフェースに文字列を描画します。</summary>
		/// <remarks>
		///	サーフェースをnewしたくないときは、こちらを使うよろし。
		/// </remarks>
		public void DrawSolid(Surface surface, string str)
		{
			CacheSystem.OnAccess(this);

			if (font != IntPtr.Zero) {
				SDL.TTF_SetFontStyle(font, style);
				IntPtr image = SDL.TTF_RenderText_Solid(font, str, color);
				RateCheck(ref image);
				surface.SDL_Surface = image; //  setSurface(image);
			} else {
				surface.SDL_Surface = IntPtr.Zero; // setSurface(IntPtr.Zero);
			}
		}

		///	<summary>文字を描画したサーフェースを返します。</summary>
		/// <remarks>
		/// <para>
		///	画質はあらいです。そのかわり早いです。
		///	</para>
		/// <para>
		///	生成されるサーフェースは、αなし。
		///	</para>
		/// </remarks>
		public Surface DrawSolidUTF8(byte[] str)
		{
			Surface surface = new Surface();
			DrawSolidUTF8(surface, str);
			return surface;
		}

		///	<summary>サーフェースに文字列を描画します。</summary>
		/// <remarks>
		///	サーフェースをnewしたくないときは、こちらを使うよろし。
		/// </remarks>
		public void DrawSolidUTF8(Surface surface, byte[] str)
		{
			DrawSolidUTF8_(surface, str);
		}
		
		unsafe private void DrawSolidUTF8_(Surface surface, byte[] str)
		{
			CacheSystem.OnAccess(this);

			if (font != IntPtr.Zero && str.Length > 0)
			{
				SDL.TTF_SetFontStyle(font, style);
				Array.Resize(ref str, str.Length + 1); // C文字列用の終端の '\0' を追加。
				IntPtr image = SDL.TTF_RenderUTF8_Solid(font, str, color);
				RateCheck(ref image);
				surface.SDL_Surface = image; //  setSurface(image);
			}
			else {
				surface.SDL_Surface = IntPtr.Zero; // setSurface(IntPtr.Zero);
			}
		}

		///	<summary>文字を描画したサーフェースを返します。</summary>
		/// <remarks>
		///	画質はあらいです。そのかわり早いです。
		///	生成されるサーフェースは、αなしだが、抜き色の指定がある。
		/// 
		/// もし、このあとテクスチャ等に落とし込む気ならば、その抜き色指定のある部分を
		/// α = 0として扱うべき。
		/// </remarks>
		public Surface DrawSolidUnicode(string str)
		{
			Surface surface = new Surface();
			DrawSolidUnicode(surface,str);
			return surface;
		}

		///	<summary>サーフェースに文字列を描画します。</summary>
		/// <remarks>
		///	サーフェースをnewしたくないときは、こちらを使うよろし。
		/// </remarks>
		public void DrawSolidUnicode(Surface surface, string str)
		{
			CacheSystem.OnAccess(this);
			if (font != IntPtr.Zero)
			{
				SDL.TTF_SetFontStyle(font, style);
				IntPtr image = SDL.TTF_RenderUNICODE_Solid(font, str, color);
				RateCheck(ref image);

				/*
				// debug目的でsurfaceの種類を調べるためのコード
				unsafe
				{
					SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)((SDL.SDL_Surface*)image)->format;
					int alpha = format->alpha;
				}
				 * // 8bppのsurfaceが戻ってきているな…
				 */

				// ここで取得されるsurfaceは転送元color keyを設定されているので
				// これを設定するとalphaつきのsurfaceになってしまうのだが…(´ω`)
				// どうしたもんかの…。

				surface.SDL_Surface = image; // .setSurface(image);
			} else {
				surface.SDL_Surface = IntPtr.Zero; // setSurface(IntPtr.Zero);
			}
		}

		///	<summary>文字を描画したサーフェースを返します。</summary>
		/// <remarks>
		/// <para>
		///	drawSolid のα付きサーフェースを返すバージョン。
		///	綺麗なかわりに、遅い。
		///	</para>
		///	<para>
		///	漢字は出ない。漢字を出したいときはUTF8かUnicodeバージョンを用いること。
		///	</para>
		/// </remarks>
		public Surface	DrawBlended(string str)
		{
			Surface surface = new Surface();
			DrawBlended(surface,str);
			return surface;
		}

		///	<summary>サーフェースに文字列を描画します。</summary>
		/// <remarks>
		///	サーフェースをnewしたくないときは、こちらを使うよろし。
		/// </remarks>
		public void DrawBlended(Surface surface, string str)
		{
			CacheSystem.OnAccess(this);
			if (font != IntPtr.Zero)
			{
				SDL.TTF_SetFontStyle(font, style);
				IntPtr image = SDL.TTF_RenderText_Blended(font, str, color);
				RateCheck(ref image);
				surface.SDL_Surface = image; //  setSurface(image);
			} else {
				surface.SDL_Surface = IntPtr.Zero; //  setSurface(IntPtr.Zero);
			}
		}

		///	<summary>文字を描画したサーフェースを返します。</summary>
		/// <remarks>
		///	drawSolid のα付きサーフェースを返すバージョン。
		///	綺麗なかわりに、遅い。
		/// </remarks>
		public Surface DrawBlendedUTF8(byte[] str)
		{
			Surface surface = new Surface();
			DrawBlendedUTF8(surface, str);
			return surface;
		}

		///	<summary>サーフェースに文字列を描画します。</summary>
		/// <remarks>
		///	サーフェースをnewしたくないときは、こちらを使うよろし。
		/// </remarks>
		public void DrawBlendedUTF8(Surface surface, byte[] str) {
			DrawBlendedUTF8_(surface, str);
		}
		unsafe private void DrawBlendedUTF8_(Surface surface, byte[] str)
		{
			CacheSystem.OnAccess(this);
			if (font != IntPtr.Zero && str.Length > 0)
			{
				SDL.TTF_SetFontStyle(font, style);
				Array.Resize(ref str, str.Length + 1); // C文字列用の終端の '\0' を追加。
				fixed (byte* p = &str[0])
				{
					IntPtr image = SDL.TTF_RenderUTF8_Blended(font, (IntPtr)p, color);
					RateCheck(ref image);
					surface.SDL_Surface = image; //  setSurface(image);
				}
			}
			else {
				surface.SDL_Surface = IntPtr.Zero; //  setSurface(IntPtr.Zero);
			}
		}

		///	文字を描画したサーフェースを返します。
		/**
			drawSolid のα付きサーフェースを返すバージョン。
			綺麗なかわりに、遅い。
		*/
		public Surface DrawBlendedUnicode(string str)
		{
			Surface surface = new Surface();
			DrawBlendedUnicode(surface,str);
			return surface;
		}

		///	<summary>サーフェースに文字列を描画します。</summary>
		/// <remarks>
		///	サーフェースをnewしたくないときは、こちらを使うよろし。
		/// </remarks>
		public void	DrawBlendedUnicode(Surface surface, string str)
		{
			CacheSystem.OnAccess(this);
			if (font != IntPtr.Zero)
			{
				SDL.TTF_SetFontStyle(font, style);
				IntPtr image = SDL.TTF_RenderUNICODE_Blended(font, str, color);
				RateCheck(ref image);
				
				// debug目的でsurfaceの種類を調べるためのコード
				unsafe
				{
					SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)((SDL.SDL_Surface*)image)->format;
					int alpha = format->alpha;
				}

				surface.SDL_Surface = image; // setSurface(image);
			}
			else {
				surface.SDL_Surface = IntPtr.Zero; // setSurface(IntPtr.Zero);
			}
		}
		#endregion

		#region private
		/// <summary>
		/// フォント実体。
		/// </summary>
		protected IntPtr font;

		private SDL_RWopsH	rwops;
		private bool	bOpen;
		private SDL.SDL_Color color;

		/// <summary>
		///	フォント倍率
		///
		///	(22pt以下のフォントを作成すると、SDL_ttfでは1bppのものが返ってくる(?)ので
		///	22pt以下の場合は、n倍して、23を超えるようにする。
		///	これは、そのための倍率である)
		/// </summary>
		private int	rate;

		unsafe private void RateCheck(ref IntPtr image) {

			//	rateが1でなければ、サーフェースを縮小しなければならない。
			if (rate==1) { return ; } // ok
			if (image == IntPtr.Zero) { return ; } // surfaceの作成に失敗しちょる

			SDL.SDL_Surface* image_ = (SDL.SDL_Surface*)image;

			// 1/rateのサイズのサーフェースを作ることからはじめよう
			int ix,iy;
			ix = image_->w; iy = image_->h;
			ix /= rate; iy /= rate;

			//	これが rateで割り切れない時のことは知らネ
			if (ix==0 || iy==0) return ;

			SDL.SDL_PixelFormat* format = (SDL.SDL_PixelFormat*)image_->format;
			bool bAlpha = (format->BitsPerPixel == 32);
			/*
				SDL_ttfの返すサーフェースは、αつきなら32bpp,αなしなら8bppと
				決まっちょる
			*/

			//	SDLには縮小する関数が無いので、
			//	DIBを作って、それを縮小することにする
			//	SDL_gfxを持ってきてもいいのだが、そこまで大がかりでもないので..

			IntPtr image2;
			if (Surface.CreateDIBstatic(out image2,image_->w,image_->h,bAlpha)!=0)
				return ; //	作成失敗

			if (bAlpha)
			{
				//	αを無効化しておかないとARGB→ARGBのbltで
				//	αを考慮して転送しやがる
				SDL.SDL_SetAlpha(image,0,0);
			}

			if (SDL.SDL_BlitSurface(image,IntPtr.Zero,image2,IntPtr.Zero)!=0)
			{
				SDL.SDL_FreeSurface(image2);
				return ; // 転送失敗
			}

			IntPtr image2s;
			// ix,iyが2^nの場合、フォントが表示されないのでiy+1していた。
			// 原因が分かったら戻すこと→わかったので戻した。
			if (Surface.CreateDIBstatic(out image2s,ix,iy,bAlpha)!=0) {
				SDL.SDL_FreeSurface(image2);
				SDL.SDL_FreeSurface(image2s);
				return ; //	作成失敗
			}

			//	縮小するためにlockする
			if (SDL.SDL_LockSurface(image2)!=0) {
				SDL.SDL_FreeSurface(image2);
				SDL.SDL_FreeSurface(image2s);
				return ; // lock失敗
			}
			if (SDL.SDL_LockSurface(image2s)!=0) {
				SDL.SDL_UnlockSurface(image2s);
				SDL.SDL_FreeSurface(image2);
				SDL.SDL_FreeSurface(image2s);
				return ; // lock失敗
			}

			SDL.SDL_Surface* image2_, image2s_;
			image2_ = (SDL.SDL_Surface*)image2;
			image2s_ = (SDL.SDL_Surface*)image2s;

			//	縮小ルーチン
			if (bAlpha) {
				uint rt,gt,bt,at;
				uint rr = (uint)(rate*rate);
				uint* pixels1 = (uint*)image2_->pixels;
				uint* pixels2 = (uint*)image2s_->pixels;
				for(int y=0;y<iy/*image2s_->h*/;++y){
					uint xx = 0;
					for(int x=0;x<ix/*image2s_->w*/;++x){
						//	ピクセルの平均を求める
						uint r,g,b,a;
						uint* pixels1t = pixels1;
						r=g=b=a=0;
						for(int j=0;j<rate;++j){
							for(int i=0;i<rate;++i){
								uint p = pixels1t[xx+i];
								format = (SDL.SDL_PixelFormat*)image2_->format;
								rt = p & format->Rmask;
								rt >>= format->Rshift;
								r+= rt;
								gt = p & format->Gmask;
								gt >>= format->Gshift;
								g+= gt;
								bt = p & format->Bmask;
								bt >>= format->Bshift;
								b+= bt;
								at = p & format->Amask;
								at >>= format->Ashift;
								a+= at;
							}
							pixels1t = (uint*)(((byte*)pixels1) + image2_->pitch);
						}
						r /= rr; g /= rr; b /= rr; a /= rr;
						//	↑これだと切り捨てすぎかも..
						// format = (SDL.SDL_PixelFormat*)image2_->format;
						format = (SDL.SDL_PixelFormat*)image2s_->format;
						pixels2[x] = (r << format->Rshift)
							+		 (g << format->Gshift)
							+		 (b << format->Bshift)
							+		 (a << format->Ashift);
						xx+=(uint)rate;

					}
					pixels1 = (uint*)(((byte*)pixels1) + image2_->pitch*rate);
					pixels2 = (uint*)(((byte*)pixels2) + image2s_->pitch);
				}
			} else {
				//	24bppのはず
				uint rr = (uint)(rate*rate);
				byte* pixels1 = (byte*)image2_->pixels;
				byte* pixels2 = (byte*)image2s_->pixels;
				for(int y=0;y<image2s_->h;++y){
					uint xx,x2;
					xx=x2=0;
					for(int x=0;x<image2s_->w;++x){
						//	ピクセルの平均を求める
						uint r,g,b;
						byte* pixels1t = pixels1;
						r=g=b=0;
						for(int j=0;j<rate;++j){
							uint xxx = xx;
							for(int i=0;i<rate;++i){
								r += pixels1t[xxx+i+0];
								g += pixels1t[xxx+i+1];
								b += pixels1t[xxx+i+2];
								xxx += 3;
							}
							pixels1t = pixels1 + image2_->pitch;
						}
						r /= rr; g /= rr; b /= rr;

						pixels2[x2+0] = (byte)r;
						pixels2[x2+1] = (byte)g;
						pixels2[x2+2] = (byte)b;

						x2 += 3;
						xx += (uint)(3*rate);
					}
					pixels1 = pixels1 + image2_->pitch*rate;
					pixels2 = pixels2 + image2s_->pitch;
				}
			}

			//	さすがにunlockは失敗しないでそ．．
			SDL.SDL_UnlockSurface(image2);
			SDL.SDL_UnlockSurface(image2s);

			SDL.SDL_FreeSurface(image);
			SDL.SDL_FreeSurface(image2);
			image = image2s; // 書き換えて返す
		}
		#endregion

		#region overridden from base class(CachedObject)

		///	<summary>読み込んでいるフォントのファイルサイズを返す。</summary>
		public override long ResourceSize
		{
			get { return bufferSize; }
		}
		private long bufferSize;

		protected override YanesdkResult OnReconstruct(object param)
		{
			FontConstructAdaptor info = param as FontConstructAdaptor;
			return Load(info.FileName,info.FontSize,info.FontIndex);
		}

		#endregion

	}

}
