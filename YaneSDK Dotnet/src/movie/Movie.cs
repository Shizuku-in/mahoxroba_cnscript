
// Stop()の実装にRelease()を用いる。
// Stop()したあとPlay()すると、どうも環境によってはバグるようなので…。
#define RELEASE_ON_STOP

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Sdl;
using OpenGl;
using Yanesdk.Ytl;
using Yanesdk.System;
using Yanesdk.Draw;

namespace Yanesdk.Movie
{
	/// <summary>
	/// SMPEGによるmpgの再生を行うクラス。
	/// </summary>
	/// <remarks>
	/// どうやらSMPEGの制限上、SdlWindow使用時しか使えず、
	/// しかもSdlWindowのサイズより大きいムービーを読み込んだ場合、
	/// 右側や下側が切り取られてしまうっぽい。
	///		→ SDL.DLL側を修正した
	///
	///	↓こんな感じで使う。
	/// <code>
	///	// 初期化
	///	{
	///		Movie movie = new Movie();
	///		movie.Load("hoge.mpg");
	///
	///		GlTexture tx = new GlTexture();
	///		tx.SetSurface(movie.Surface);
	///		//tx.CreateSurface(640, 480, false);
	///		//tx.CreateSurface(movie.Width, movie.Height, false);
	///
	///		movie.IsLoop = true;		// デフォルトはfalse
	///		movie.Play();
	/// }
	///
	///	// 描画ループ
	/// {
	///		window.Screen.Select();
	///		if (movie.IsReadyToDraw) {
	///			movie.Draw(tx);
	///			window.Screen.Blt(tx, 0, 0);
	///			//window.Screen.Blt(tx, 0, 0, new Rect(0, 0, tx.Width, tx.Height), new Size(window.Width, window.Height));
	///		}
	///		window.Screen.Update();
	/// }
	/// </code>
	///
	/// あるいは、
	///
	/// <code>
	/// Movie movie = new Movie();
	/// movie.SetWindow(window);
	/// movie.Load("hoge.mpg");
	/// movie.IsLoop = true;		// デフォルトはfalse
	/// movie.Play();
	/// // 以降、勝手にwindowに描画される。
	/// // 色々制限などがあるので、SetWindow()のコメントを参照。
	/// </code>
	/// </remarks>
	public class Movie : ILoader , IDisposable
	{
		#region SDLWindowに描画するサンプル
		/*
			FpsTimer timer = new FpsTimer();
			timer.Fps = 30;

			KeyBoardInput key = new KeyBoardInput();

			SDLWindow2DGl window = new SDLWindow2DGl();
			window.SetVideoMode(800, 600, 0);

			GlTexture tx = new GlTexture();
			Movie movie = new Movie();
			movie.Load("test.mpg");
			tx.SetSurface(movie.Surface);
			movie.IsLoop = true;
			movie.Play();

			while (SDLFrame.PollEvent() == Yanesdk.Ytl.YanesdkResult.NoError) {
				key.Update();

				if (key.IsPush(KeyCode.b)) {
					break;
				} else if (key.IsPush(KeyCode.p)) {
					movie.Play();
				} else if (key.IsPush(KeyCode.s)) {
					movie.Stop();
				} else if (key.IsPush(KeyCode.q)) {
					movie.Pause();
				} else if (key.IsPush(KeyCode.r)) {
					movie.Resume();
				}

				window.Screen.Select();
				if (movie.IsReadyToDraw) {
					movie.Draw(tx);
					//window.Screen.Blt(tx, 0, 0);
					window.Screen.Blt(tx, 0, 0,
						new Yanesdk.Draw.Rect(0, 0, tx.Width, tx.Height),
						new Yanesdk.Draw.Size(window.Width, window.Height));
				}
				window.Screen.Update();

				timer.WaitFrame();
			}
		 */
		#endregion

		#region Win32Windowに描画するサンプルソース
		/*
	public partial class Form7 : Form
	{
		public Form7()
		{
			InitializeComponent();

			init();
		}

		Movie movie = new Movie();

		// ボタンハンドラ×4↓ Formデザイナで適当にボタンを配置して使ってちょうだい。
		private void button1_Click(object sender, EventArgs e)
		{
			movie.Play();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			movie.Pause();
		}

		private void button3_Click(object sender, EventArgs e)
		{
			movie.Resume();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			movie.Stop();
		}

		public void init()
		{
			timer.Fps = 30;

			window = new Win32Window(this.Handle);
			window.Screen.Select();

			tx = new GlTexture();
			movie.Load("test.mpg");
			tx.SetSurface(movie.Surface);
			movie.IsLoop = true;

			window.Screen.Unselect();
		//	movie.Play();
		}

		FpsTimer timer = new FpsTimer();
		Win32Window window;
		GlTexture tx;

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (timer.ToBeRendered)
			{
				window.Screen.Select();
				if (movie.IsReadyToDraw)
				{
					movie.Draw(tx);
					if (rot == 0)
					{
						window.Screen.Blt(tx, 0, 0 , null,null);
					//	window.Screen.Blt(tx, 0, 0,
					//		new Yanesdk.Draw.Rect(0, 0, tx.Width, tx.Height),
					//		new Yanesdk.Draw.Size(this.Width, this.Height));
					}
					else
					{
						// 試しにムービーをまわしてみる。
						window.Screen.BltRotate(tx, 0, 0, rot, 1.0f, 200, 200);
					}
				}
				window.Screen.Update();

				rot += rotateStep;

				timer.WaitFrame();
			}
		}

		int rot = 0;
		int rotateStep = 0;
		private void button5_Click(object sender, EventArgs e)
		{
			rotateStep++;
		}
	}
		 */
		#endregion

		#region ctor and Dispose
		/// <summary>
		/// オーディオの再生周波数等は、Yanesdk.Sound.Sound.SoundConfigで設定された値で初期化される。
		/// </summary>
		public Movie()
		{
			callback = new SMPEG.SMPEG_DisplayCallback(smpeg_DisplayCallback);
			//mix_hook = new SDL.mix_func_t(SMPEG.SMPEG_playAudioSDL);
			mix_hook = new SDL.mix_func_t(mix_HookMusic);

			// 排他処理オブジェクトの作成
			mutex = SDL.SDL_CreateMutex();

			// MasterVolumeが変更されたときにそれに追随して再生Volumeが変更されるように。
			Yanesdk.Sound.Sound.ChunkManager.OnMasterVolumeChanged
				+= OnMasterVolumeChanged;
		}

		/// <summary>
		///  Disposeを呼び出したあとは再生できない。
		/// 単に解放したいだけならばReleaseで行なうべし。
		/// </summary>
		public void Dispose()
		{
			Release();

			// MasterVolumeが変更されたときにそれに追随して再生Volumeが変更されるようにしてあったのを戻す。
			Yanesdk.Sound.Sound.ChunkManager.OnMasterVolumeChanged
				-= OnMasterVolumeChanged;

			initTimer.Dispose();
			initAudio.Dispose();
			initVideo.Dispose();

			SDL.SDL_DestroyMutex(mutex);
		}

		/// <summary>
		/// SDL VideoとAudioの初期化用クラス。
		/// </summary>
		private RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>
			initVideo = new RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>();
		private RefSingleton<Yanesdk.Sdl.SDL_AUDIO_Initializer>
			initAudio = new RefSingleton<Yanesdk.Sdl.SDL_AUDIO_Initializer>();
		private RefSingleton<Yanesdk.Sdl.SDL_TIMER_Initializer>
			initTimer = new RefSingleton<Yanesdk.Sdl.SDL_TIMER_Initializer>();

		/// <summary>
		/// マスターvolumeが変更になったときのハンドラ
		/// </summary>
		private void OnMasterVolumeChanged()
		{
			// ボリュームの再設定を行なう。
			// setterではmaster volumeに対して掛け算されていることを利用する。
			Volume = Volume;
		}
		#endregion

		#region ILoaderの実装
		/// <summary>
		/// mpegファイルを読み込む
		/// </summary>
		/// <param name="filename">ファイル名</param>
		public YanesdkResult Load(string filename) {
			lock (lockObject)
			{
				Release();
				tmpFile = FileSys.GetTmpFile(filename);
				if (tmpFile == null || tmpFile.FileName == null)
				{
					return YanesdkResult.FileNotFound;
				}
				// 読み込み。
				//mpeg = SMPEG.SMPEG_new(tmpFile.FileName, ref info, SDL.SDL_WasInit(SDL.SDL_INIT_AUDIO) == 0 ? 1 : 0);
				mpeg = SMPEG.SMPEG_new(tmpFile.FileName, ref info, 0);
				if (SMPEG.SMPEG_error(mpeg) != null)
				{
					Debug.Fail(SMPEG.SMPEG_error(mpeg));
					return YanesdkResult.SdlError; // …SDL？
				}

				// 初期設定
			//	loop = false;
				// ループ設定はここでは変更しないほうが良い。

				paused = false;
				
				SMPEG.SMPEG_enablevideo(mpeg, 1);
				SMPEG.SMPEG_enableaudio(mpeg, 0);
				if (info.has_audio != 0)
				{
					SDL.SDL_AudioSpec audiofmt = new SDL.SDL_AudioSpec();
					audiofmt.format = (ushort)Sound.Sound.SoundConfig.AudioFormat;
					audiofmt.freq = (ushort)Sound.Sound.SoundConfig.AudioRate;
					audiofmt.channels = (byte)Sound.Sound.SoundConfig.AudioChannels;
					audiofmt.samples = (ushort)Sound.Sound.SoundConfig.AudioBuffers;
					SMPEG.SMPEG_actualSpec(mpeg, ref audiofmt);
					SDL.Mix_HookMusic(mix_hook, mpeg);
					SMPEG.SMPEG_enableaudio(mpeg, 1);
				}
				SMPEG.SMPEG_enableaudio(mpeg, 1);
				Volume = volume;

				// 描画先サーフェスのsmpegへの登録など。
				if (window != null)
				{
					// SDLWindowに直に描画する

					if (surface != null)
					{
						surface.Dispose();
						surface = null;
					}

					uint flags = (uint)window.Option;
					flags &= ~SDL.SDL_OPENGL; // OPENGLはダメ。
					IntPtr s = SDL.SDL_SetVideoMode(window.Width, window.Height, window.Bpp, flags);
					if (s == IntPtr.Zero)
						return YanesdkResult.SdlError;

					SMPEG.SMPEG_setdisplay(mpeg, s, IntPtr.Zero, null);
					SMPEG.SMPEG_move(mpeg, (int)windowRect.Left, (int)windowRect.Top);
					SMPEG.SMPEG_scaleXY(mpeg, (int)windowRect.Width, (int)windowRect.Height);
				}
				else if (
					Yanesdk.System.Platform.PlatformID == Yanesdk.System.PlatformID.Windows
					&& hwnd != IntPtr.Zero)
				{
					// SDLWindowを貼り付けてそれに描画する

					if (surface != null)
					{
						surface.Dispose();
						surface = null;
					}

					// 親ウィンドウの位置・サイズを取得
					POINT parentPos = new POINT();
					ClientToScreen(hwnd, ref parentPos);
					RECT parentRect = new RECT();
					GetClientRect(hwnd, out parentRect);
					int w = parentRect.right - parentRect.left, h = parentRect.bottom - parentRect.top;

					// SDLウィンドウの位置を合わせる
					IntPtr sdlHwnd = GetSDLWindowHandle();
					//SetParent(sdlHwnd, hwnd); // これするとどうもバグる…
					SetWindowPos(sdlHwnd, IntPtr.Zero, parentPos.x, parentPos.y, w, h, SWP_NOZORDER);

					// SDLウィンドウの作成
					uint flags = SDL.SDL_HWSURFACE | SDL.SDL_NOFRAME;
					IntPtr s = SDL.SDL_SetVideoMode(w, h, 0, flags);
					if (s == IntPtr.Zero)
						return YanesdkResult.SdlError;

					sdlWindowCreated = true;

					sdlHwnd = GetSDLWindowHandle(); // 変わらないと思うが、一応再取得
					if (sdlHwnd == IntPtr.Zero)
						return YanesdkResult.SdlError;
					SetParent(sdlHwnd, hwnd);
					SetWindowPos(sdlHwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOSIZE);

					SMPEG.SMPEG_setdisplay(mpeg, s, IntPtr.Zero, null);
					SMPEG.SMPEG_move(mpeg, 0, 0);
					SMPEG.SMPEG_scaleXY(mpeg, w, h);
				}
				else
				{
					// surfaceに描画する

					if (surface != null && (!surface.CheckRGB888() ||
						surface.Width != Width || surface.Height != Height))
					{
						surface.Dispose();
						surface = null;
					}
					if (surface == null)
					{
						surface = new Surface();
						// 拡大・縮小はSMPEG側にやらせると重いので、原寸大で。
						YanesdkResult result = surface.CreateDIB(Width, Height, false);
						if (result != YanesdkResult.NoError)
						{
							return result;
						}
					}

					SMPEG.SMPEG_setdisplay(mpeg, surface.SDL_Surface, mutex, callback);
					SMPEG.SMPEG_move(mpeg, 0, 0);
					SMPEG.SMPEG_scaleXY(mpeg, surface.Width, surface.Height);
					//SetFilter(SMPEG.SMPEGfilter_null());
				}

				this.fileName = filename;

				// おしまい。
				return YanesdkResult.NoError;
			}
		}

		/// <summary>
		/// ファイルを読み込んでいる場合、読み込んでいるファイル名を返す
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}
		private string fileName = null;

		/// <summary>
		/// SDL.Mix_HookMusic()に登録する関数
		/// </summary>
		private void mix_HookMusic(/* void * */IntPtr udata, /* Uint8 * */IntPtr stream, int len)
		{
			// lock (lockObject) // これはデッドロックするかも。。

			if (mpeg != IntPtr.Zero && SMPEG.SMPEG_status(mpeg) == SMPEG.SMPEGstatus.SMPEG_PLAYING)
			{
				SMPEG.SMPEG_playAudio(mpeg, stream, len);
			}
		}

		/// <summary>
		/// 読み込んでいた動画の解放
		/// </summary>
		public void Release()
		{
			lock (lockObject)
			{
				fileName = null;

				if (Loaded)
				{
					SDL.Mix_HookMusic(null, IntPtr.Zero);

					Stop();
					SMPEG.SMPEG_delete(mpeg);
					mpeg = IntPtr.Zero;

					if (Yanesdk.System.Platform.PlatformID == Yanesdk.System.PlatformID.Windows
						&& sdlWindowCreated)
					{
						IntPtr sdlHwnd = GetSDLWindowHandle();
						SetParent(sdlHwnd, IntPtr.Zero);
						ShowWindow(sdlHwnd, SW_HIDE);
					}

				}
				if (tmpFile != null)
				{
					tmpFile.Dispose();
					tmpFile = null;
				}
				if (surface != null)
				{
					surface.Dispose();
					surface = null;
				}
			}
		}

		/// <summary>
		/// ファイルの読み込みが完了しているかを返す
		/// </summary>
		public bool Loaded
		{
			get { return mpeg != IntPtr.Zero; }
		}
		
		#endregion

		#region methods

		/// <summary>
		/// 描画先ウィンドウ
		/// </summary>
		private SDLWindow2DGl window = null;
		/// <summary>
		/// ウィンドウの描画先座標
		/// </summary>
		private Rect windowRect;

		/// <summary>
		/// 描画先ウィンドウをセット。Load()する前に呼び出すこと。
		/// </summary>
		/// <remarks>
		/// 拡大や縮小は遅くかつギザギザになってしまうので、
		/// これを使うなら等倍のみとした方がよい。
		/// また、終わったら、SDLWindow2DGl.TestVideoMode()をやり直して、
		/// 作成済みのテクスチャがあれば全て作り直す必要がある。
		/// </remarks>
		public void SetWindow(SDLWindow2DGl window)
		{
			SetWindow(window, null);
		}

		/// <summary>
		/// 描画先ウィンドウをセット。Load()する前に呼び出すこと。
		/// </summary>
		/// <remarks>
		/// <remarks>
		/// DirectXのoverlayで描画されるため、GlTextureSubImageの
		/// 遅いマシンではかなり効果が期待できる。
		/// 
		/// 速いマシンでは関係ない(´ω`)
		/// 
		/// ただし他のウィンドウを重ねることになるので
		/// もとあるウィンドウに載せてあるコントロールの
		/// 類はoverlayしているときには表示されなくなるので注意が必要。
		/// 
		/// 拡大や縮小は遅くかつギザギザになってしまうので、
		/// これを使うなら等倍のみとした方がよい。
		/// また、終わったら、SDLWindow2DGl.TestVideoMode()をやり直して、
		/// 作成済みのテクスチャがあれば全て作り直す必要がある。
		/// </remarks>
		public void SetWindow(SDLWindow2DGl w, Rect rc)
		{
			lock (lockObject)
			{
				if (Loaded)
				{
					Debug.Fail("Load()の後にMovie.SetWindow()が呼び出された");
					return;
				}
				window = w;
				windowRect = rc != null ? rc : new Rect(0, 0, w.Width, w.Height);
			}
		}

		/// <summary>
		/// SDLウィンドウを重ねるウィンドウ
		/// </summary>
		private IntPtr hwnd = IntPtr.Zero;

		/// <summary>
		/// SDLウィンドウをこのクラスが作成したのかどうか。
		/// </summary>
		private bool sdlWindowCreated = false;

		/// <summary>
		/// 内部でSDLウィンドウを作成し、指定されたウィンドウに重ねる事で、
		/// SetWindow()と同様の挙動を、WindowsのFormなどに対して実現出来る。
		/// Load()する前に呼び出すこと。
		/// </summary>
		/// <remarks>
		/// DirectXのoverlayで描画されるため、GlTextureSubImageの
		/// 遅いマシンではかなり効果が期待できる。
		/// 
		/// 速いマシンでは関係ない(´ω`)
		/// 
		/// ただし他のウィンドウを重ねることになるので
		/// もとあるウィンドウに載せてあるコントロールの
		/// 類はoverlayしているときには表示されなくなるので注意が必要。
		/// 
		/// Windows専用の機能
		/// </remarks>
		/// <param name="hwnd">ウィンドウハンドル</param>
		/// <seealso cref="SetWindow()"/>
		public void Set_SDL_Overlay(IntPtr hwnd)
		{
			// Windows専用の機能
			if (Yanesdk.System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);
			
			this.hwnd = hwnd;
		}

		#region hwnd操作用のWinAPIなど

		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int left, top, right, bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct POINT
		{
			public int x, y;
		}

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

		readonly IntPtr HWND_TOP = new IntPtr(0);
		readonly IntPtr HWND_BOTTOM = new IntPtr(1);
		readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
		const uint SWP_NOSIZE = 0x0001;
		const uint SWP_NOMOVE = 0x0002;
		const uint SWP_NOZORDER = 0x0004;
		const uint SWP_NOREDRAW = 0x0008;
		const uint SWP_NOACTIVATE = 0x0010;
		const uint SWP_SHOWWINDOW = 0x0040;
		const uint SWP_HIDEWINDOW = 0x0080;
		const uint SWP_NOSENDCHANGING = 0x0400;

		[DllImport("user32.dll")]
		static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		const int SW_HIDE = 0;
		const int SW_SHOWNORMAL = 1;
		const int SW_NORMAL = 1;
		const int SW_SHOWMINIMIZED = 2;
		const int SW_SHOWMAXIMIZED = 3;
		const int SW_MAXIMIZE = 3;
		const int SW_SHOWNOACTIVATE = 4;
		const int SW_SHOW = 5;
		const int SW_MINIMIZE = 6;
		const int SW_SHOWMINNOACTIVE = 7;
		const int SW_SHOWNA = 8;
		const int SW_RESTORE = 9;
		const int SW_SHOWDEFAULT = 10;
		const int SW_FORCEMINIMIZE = 11;
		const int SW_MAX = 11;

		[DllImport("user32.dll")]
		static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

		const int WM_CLOSE = 0x0010;
		const int WM_QUIT = 0x0012;

		[DllImport("user32.dll")]
		static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		[StructLayout(LayoutKind.Sequential)]
		struct MSG
		{
			public IntPtr hwnd;
			public int message;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public POINT pt;
		}

		const uint PM_NOREMOVE = 0;
		const uint PM_REMOVE = 1;

		[DllImport("user32.dll")]
		static extern bool PeekMessage(ref MSG lpMsg,
			IntPtr hwnd, int wMsgFilterMin, int wMsgFilterMax, uint wRemoveMsg);

		[DllImport("user32.dll")]
		static extern bool TranslateMessage(ref MSG lpMsg);

		[DllImport("user32.dll")]
		static extern int DispatchMessage(ref MSG lpMsg);

		#endregion

		/// <summary>
		/// テクスチャに描画。
		/// 更新されていない場合処理しないので、専用のテクスチャを用意しておいて、
		/// 毎回それを指定すること。
		/// </summary>
		public void Draw(GlTexture tx)
		{
			lock (lockObject)
			{
				Debug.Assert(IsReadyToDraw);
				// ↑これは呼び出し元でチェックしといて、
				// falseなうちは描画しない方がいい。(´ω`)

				if (window != null)
				{
					Debug.Fail("Movie.SetWindow()されたのにMovie.Draw()が呼ばれた");
					return;
				}

				// lock
				int p = SDL.SDL_mutexP(mutex); Debug.Assert(p == 0);

				// フラグ操作
				//*
				if (!updateRect.HasValue)
				{
					// unlock
					int vv = SDL.SDL_mutexV(mutex); Debug.Assert(vv == 0);
					return;
				}
				//Rectangle clientRect = updateRect.Value; // 高速化に使えそうな予感もするものの…。
				updateRect = null;
				/*/
				if (!updated) {
					// unlock
					int vv = SDL.SDL_mutexV(mutex); Debug.Assert(vv == 0);
					return;
				}
				updated = false;
				//*/

				// テクスチャに描画
				tx.Bind();
				Gl.glTexSubImage2D((uint)tx.TextureType, 0,
					0, 0, surface.Width, surface.Height,
					surface.Alpha ? Gl.GL_RGBA : Gl.GL_RGB,
					Gl.GL_UNSIGNED_BYTE, surface.Pixels);
				tx.Unbind();

				// unlock
				int v = SDL.SDL_mutexV(mutex); Debug.Assert(v == 0);
			}
		}
		//private volatile bool updated = false;
		//private volatile Rectangle? updateRect;
		private Rectangle? updateRect;

		/// <summary>
		/// 再生開始
		/// </summary>
		public YanesdkResult Play()
		{
			lock (lockObject)
			{
#if RELEASE_ON_STOP
				if (IsPlaying)
				{
					if (!paused)
					{
						InnerStop();
					}
					paused = false;
					SMPEG.SMPEG_rewind(mpeg); // 巻き戻す
				} 
				else
				{
					if (!Loaded && fileName != null)
					{
						YanesdkResult result = Load(fileName);
						if (result != YanesdkResult.NoError)
						{
							return result;
						}
					}
				}
#else
				Stop();
#endif
				readyToDraw = false; // 描画しなおされるまでは描画準備状態とする
				InnerPlay();

				return YanesdkResult.NoError;
			}
		}

		/*

		// Windowに直接描画するとき
		// SetFilter(SMPEG.SMPEGfilter_bilinear());
		// とかすればスムーズに拡大・縮小されるのかと思いきや、そうでもない。
		// 微妙な違いはあるのだが、拡大とかする前の段階での処理のようで、
		// 見比べてみないと分からない程度。
		// よって、とりあえずデフォルトのままにしておく。

		private void SetFilter(IntPtr filter)
		{
			IntPtr f = SMPEG.SMPEG_filter(mpeg, filter);
			// 古いフィルタの後始末
			SMPEG.SMPEG_Filter fs = (SMPEG.SMPEG_Filter)Marshal.PtrToStructure(f, typeof(SMPEG.SMPEG_Filter));
			fs.destroy(f);
		}

		//*/

		/// <summary>
		///	play中のムービーを停止させる
		/// </summary>
		public void Stop()
		{
			lock (lockObject)
			{
#if RELEASE_ON_STOP
				if (fileName != null) {
					// Release()で一端filenameはnullになるので、終わってから再設定する。(´ω`)
					string tmp = fileName;
					Release();
					fileName = tmp;
				}
#else
				Debug.Assert(Loaded);
				if (IsPlaying)
				{
					if (!paused)
					{
						InnerStop();
					}
					paused = false;
					SMPEG.SMPEG_rewind(mpeg); // 巻き戻す
				}
#endif
			}
		}

		/// <summary>
		/// 一時停止
		/// </summary>
		public void Pause()
		{
			lock (lockObject)
			{
				if (IsPlaying && !paused)
				{
					// SMPEG_pause()はstop時にバグりがちなので使うのやめとく。
					InnerStop();
					paused = true;
				}
			}
		}

		/// <summary>
		/// pause中かを表すメソッド
		/// </summary>
		private bool paused = false;

		/// <summary>
		/// 再開
		/// </summary>
		public void Resume()
		{
			lock (lockObject)
			{
				if (IsPlaying && paused)
				{
					//SMPEG.SMPEG_pause(mpeg); // 呼び出す毎に切り替わるらしい
					InnerPlay();
					paused = false;
				}
			}
		}

		/// <summary>
		/// 再生開始とループ設定の反映
		/// </summary>
		private void InnerPlay()
		{
			SMPEG.SMPEG_play(mpeg);
			// ループ設定を反映させる
			if (loop) SMPEG.SMPEG_loop(mpeg, loop ? 1 : 0);
		}

		/// <summary>
		/// 再生停止
		/// </summary>
		private void InnerStop()
		{
			SMPEG.SMPEG_loop(mpeg, 0); // ループしてると止まらないらしい
			SMPEG.SMPEG_stop(mpeg);
		}

		/// <summary>
		/// 描画時に呼び出されるコールバック関数。
		/// 別スレッドらしいので、GL周りの処理とかは止めとくのが無難っぽい。
		/// </summary>
		private void smpeg_DisplayCallback(/*SDL.SDL_Surface*/IntPtr dst, int x, int y, uint w, uint h)
		{
			Debug.Assert(dst == surface.SDL_Surface);

			//lock (lockObject) // これはデッドロックするかも。。

			//lock (callback) // ← SMPEG側でmutexでlockされてるのでこれは要らない

			//*
			Rectangle rc = new Rectangle(x, y, (int)w, (int)h);
			updateRect = updateRect.HasValue ? Rectangle.Union(updateRect.Value, rc) : rc;
			/*/
			updated = true;
			//*/

			// readyToDraw = true; // 一度でも描画されたら描画準備完了と見なす。
			if (!readyToDraw)
			{
				// 全面が更新されたら描画準備完了
				Rectangle r = updateRect.Value;
				if (//r.Left <= 0 && r.Right <= 0 &&
					Width <= r.Width && Height <= r.Height)
				{
					readyToDraw = true;
				}
			}
		}

		/// <summary>
		/// SDLなウィンドウのHWNDを取得する。
		/// </summary>
		private IntPtr GetSDLWindowHandle()
		{
			unsafe
			{
				SDL.SDL_SysWMinfo* wminfo = stackalloc SDL.SDL_SysWMinfo[1];
				SDL.SDL_VERSION(ref wminfo->version);
				wminfo->window = IntPtr.Zero;
				SDL.SDL_GetWMInfo(new IntPtr(wminfo));
				return wminfo->window;
			}
		}

		#region ボツメソッド

		/*
			これはどうも遅いっぽい。(´ω`)

		/// <summary>
		/// ウィンドウへ描画
		/// </summary>
		public void Draw(SDLWindow2DGl window, int x, int y) {
			lock (lockObject)
			{
				SDL.SDL_Rect src = new SDL.SDL_Rect(), dst = new SDL.SDL_Rect();
				src.x = 0;
				src.y = 0;
				src.w = (ushort)surface.Width;
				src.h = (ushort)surface.Height;
				dst.x = (short)x;
				dst.y = (short)y;
				dst.w = (ushort)surface.Width;
				dst.h = (ushort)surface.Width;
				SDL.SDL_BlitSurface(surface.SDL_Surface, ref src, window.SDL_Surface, ref dst);
				SDL.SDL_UpdateRect(window.SDL_Surface, 0, 0, 0, 0);
			}
		}

		//*/

		#endregion

		#endregion

		#region prpoerties
		/// <summary>
		/// ループするのかどうか、取得・設定。
		/// 
		/// 一度変更すると、Loadしても変更はされない。
		/// 再度設定されるまで有効。初期状態ではfalse。
		/// </summary>
		public bool IsLoop
		{
			get
			{
				return loop;
			}
			set
			{
				lock (lockObject)
				{
					loop = value;
					if (IsPlaying) // 再生時じゃないとうまく反映されないらしい
					{
						// 現在読み込み中の動画があればループモードを設定
						SMPEG.SMPEG_loop(mpeg, loop ? 1 : 0);
					}
				}
			}
		}

		/// <summary>
		/// ループモードなのかどうか。
		/// </summary>
		private bool loop = false;

		/// <summary>
		/// 読み込んだリソースのサイズを取得するメソッド
		/// </summary>
		public long ResourceSize
		{
			get
			{
				if (!Loaded) return 0;
				return info.total_size;
			}
		}

		/// <summary>
		/// 横幅の取得
		/// </summary>
		public int Width
		{
			get { return info.width; }
		}

		/// <summary>
		/// 縦幅の取得
		/// </summary>
		public int Height
		{
			get { return info.height; }
		}

		/// <summary>
		/// 現在の動画サーフェスの取得
		/// </summary>
		public Surface Surface
		{
			get { return surface; }
		}
		private Surface surface = null;

		/// <summary>
		/// 再生中なのかどうか。
		/// Pause中もtrueを返す。
		/// ループしない場合、これがfalseになったら終わったと思えばOK。
		/// </summary>
		/// <seealso cref="IsReadyToDraw"/>
		public bool IsPlaying
		{
			get
			{
				lock (lockObject)
				{
					return Loaded && (paused || SMPEG.SMPEG_status(mpeg) == SMPEG.SMPEGstatus.SMPEG_PLAYING);
				}
			}
		}

		/// <summary>
		/// 描画可能な状態ならtrueを返す。
		/// 通常、これがtrueだった場合のみテクスチャにDraw()して、それをScreenにBltすればよい。
		/// </summary>
		/// <remarks>
		/// 基本的には IsPlaying と同じなのだが、
		/// SMPEG側に、Stop()してからPlay()すると、一瞬Stop()したときの画像が
		/// 表示されてしまうバグがあるので、
		/// Play()が呼ばれてからsurfaceにちゃんと描画されるまではfalseを返すようになっている。
		/// </remarks>
		public bool IsReadyToDraw
		{
			get
			{
				return IsPlaying && readyToDraw;
			}
		}

		private volatile bool readyToDraw = true;

		/// <summary>
		/// マスターvolumeの設定
		/// すべてのSoundクラスに影響する。
		///		出力volume = (master volumeの値)×(volumeの値)
		/// である。
		/// </summary>
		/// <param name="volume"></param>
		public static float MasterVolume
		{
			get { return Yanesdk.Sound.Sound.ChunkManager.MasterVolume; }
			set { Yanesdk.Sound.Sound.ChunkManager.MasterVolume = value; }
		}

		/// <summary>
		/// volumeの設定。0.0～1.0までの間で。
		/// 1.0なら100%の意味。
		///
		/// master volumeのほうも変更できる。
		///		出力volume = (master volumeの値)×(ここで設定されたvolumeの値)
		/// である。
		///
		/// ここで設定された値はLoad/Play等によっては変化しない。
		/// 再設定されるまで有効である。
		/// </summary>
		/// <param name="volume"></param>
		/// <returns></returns>
		public float Volume
		{
			get { return volume; }
			set
			{
				lock (lockObject)
				{
					volume = value;
					if (Loaded)
					{
						SMPEG.SMPEG_setvolume(mpeg, (int)(100 * MasterVolume * volume));
					}
				}
			}
		}
		private float volume = 1.0f;

		#endregion

		#region private

		/// <summary>
		/// movieのためのテンポラリファイル
		/// </summary>
		private FileSys.TmpFile tmpFile = null;

		/// <summary>
		/// movieファイルへのhandle(smpeg内部で使われている)
		/// </summary>
		private IntPtr mpeg = IntPtr.Zero;

		/// <summary>
		/// 読み込まれたmovie情報
		/// </summary>
		private SMPEG.SMPEG_Info info = new SMPEG.SMPEG_Info();

		/// <summary>
		/// surfaceに書き込みを行うmpegデコードスレッドと、
		/// Draw()との排他処理を行うSDL_mutex。
		/// </summary>
		private IntPtr mutex;

		/// <summary>
		/// SMPEGから描画時に呼び出されるコールバック。
		/// GCされないようにメンバに持ってしまう。
		/// </summary>
		private SMPEG.SMPEG_DisplayCallback callback;

		/// <summary>
		/// SDL.Mix_HookMusic()に登録するdelegate(中身はSMPEG.SMPEG_playAudioSDL)
		/// callbackと同様、GCされないようにメンバに。
		/// </summary>
		private SDL.mix_func_t mix_hook;

		/// <summary>
		/// lock用ダミーオブジェクト。
		/// SMPEGは複数のスレッドから使うようには作られていないと思われるので、
		/// 呼び出し側で片っ端からlockしておく。
		/// </summary>
		private object lockObject = new object();

		#endregion
	}
}
