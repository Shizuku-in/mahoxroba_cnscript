using System;
using System.Text;

using Sdl;
using OpenGl;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// SDLWindowのtypedef
	/// </summary>
	public class SDLWindow : SDLWindow2DGl { }

	/// <summary>
	///  SDLの画面を表現しているサーフェース
	///  (2次元描画向け)
	/// </summary>
	/// <remarks>
	/// <para>
	///	描画にはopenGLを用いています。
	///	画面にbmp,png,etc..画像を描画するためには、 GlTexture クラスを用います。
	///	2D描画するときも、 GlTexture クラスを用います。
	/// </para>
	/// <para>
	///	座標系は、ディフォルトで、ウィンドゥの左上が(0,0)、右下が(640,480)となる
	///	※　右上の(640,480)というのは、ウィンドゥサイズにより異なります。
	/// </para>
	/// <para>
	///	視体積(見える部分)の深さ(z方向)は、0～256が初期値として設定してあります。
	///	つまりは、
	/// <c>
	/// glOrtho(0,640,480,0,0,256);	//	256 = depth
	/// </c>
	///	としてあります。これは、setVideoModeのときに設定されます。
	///	もし、これでまずければ再設定するようにしてください。
	/// </para>
	/// 
	/// 注意: SDLWindowを使うときは、メッセージポンプが必要。
	/// SDLFrame.PollEventを呼び出すようにしてください。
	/// 
	/// Ozyさんからの情報 :
	/// (Macで動かすときの話)
	///	今調べてみたんですが、SDL_SetVideoMode()の前に呼ぶのが
	/// OpenGLに限らずSDLの関数を呼んでも落ちました。
	/// たとえばにょきにょきゲーなら、SDLWindowをnewした直後に
	/// SDL_WM_SetIconを呼んでますが、これで落ちますし、
	/// 最初にTestVideoModeを呼ぶとSDL_SetVideoModeにたどり着く前に
	/// SDL_GL_SetAttributeがあるのでここで落ちます。
	/// ですので、TestVideoModeでSDL_GL_SetAttributeの前に
	/// SDL_SetVideoModeを呼んでおくと落ちません。
	/// そのかわりSDLWindowを使ったプログラムを書く場合は必ず
	/// SDLWindow window = new SDLWindow();
	/// window.TestVideoMode(...);
	/// と書かんと落ちることに(;´д｀) 
	/// 
	/// だそうです。よって、TestVideoMode呼び出て画面モードが確定したあとに、
	/// このクラスのメソッドを呼び出すようにしてください(´ω`)人 > all
	/// 
	///</remarks>
	public class SDLWindow2DGl : IDisposable {

		public SDLWindow2DGl()
		{
			screen_ = new Screen2DGl();

			// screen.beginDrawDelegate
			// SDLWindowを用いるときは、つねにウィンドゥはひとつ描画されているはずなので
			// ↑は何も設定しなくていい。
			
			Screen.EndDrawDelegate = delegate
			{
				SDL.SDL_GL_SwapBuffers();
			};
		}

		/// <summary>
		/// Disposeを呼び出したあとに再度描画することは出来ない。
		/// </summary>
		public void Dispose()
		{
			screen_.Dispose();
			init.Dispose();
		}

		/// <summary>
		/// SDL Video初期化用
		/// </summary>
		private RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>
			init = new RefSingleton<Yanesdk.Sdl.SDL_Video_Initializer>();


		///	<summary>スクリーン解像度をテストする(2D/3D)。</summary>
		/// <remarks>
		///	ビデオボードがこちらの希望のスクリーン解像度、
		///	bpp深度を持つとは限らない。
		///
		///	そこで、テストをする必要がある。
		/// <code>
		///	beginScreenTest();	//	いまからテストする
		///	testVideoMode(640,480,32);	// フルスクリーン 640×480×32をテスト
		///	testVideoMode(640,480,16);	// フルスクリーン 640×480×16をテスト
		///	testVideoMode(640,480,24);	// フルスクリーン 640×480×24をテスト
		///	testVideoMode(640,480,0);	// ウィンドゥモード 640×480をテスト
		///	endScreenTest();	//	テスト終了
		///	//	結果として、最初にスクリーン変更に成功した解像度になる。
		/// </code>
		/// フルスクリーンモードは、成功するとは限らない。
		/// ウィンドゥモードは、現在のディスプレイ解像度より小さなサイズならば
		/// メモリが足りている限りは成功する。
		/// </remarks>
		public void BeginScreenTest(){
			bTestScreen = true; bTestScreenSuccess=false;
		}

		///	<summary>スクリーン解像度のテスト終了(2D/3D)。</summary>
		/// <remarks>
		///	beginScreenTest も参照すること。
		///
		///	スクリーン変更に成功していれば0,失敗していれば非0が返る。
		/// </remarks>
		public YanesdkResult EndScreenTest() { 
			bTestScreen = false; 
			return bTestScreenSuccess?YanesdkResult.NoError:YanesdkResult.HappenSomeError; 
		}

		///	<summary>スクリーン解像度のテスト(2D/3D)。</summary>
		/// <remarks>
		///	スクリーン解像度のテストを行なう。
		///	x,y はスクリーンサイズ。bpp はピクセル深度(bits per pixel)
		///	bpp として0を指定すれば、ウィンドゥモード。
		///	(現在のモードから解像度切り替えを行なわないの意味)
		///
		///	beginScreenTest～endScreenTestで用いる。
		///	beginScreenTest も参照すること。
		///
		///	成功すれば0が返るが、この返し値を使うのではなく、
		///	beginScreenTest～endScreenTestで用いること。
		/// </remarks>
		public YanesdkResult TestVideoMode(int width,int height,int bpp){
			if (bTestScreenSuccess) return YanesdkResult.AlreadyDone; // すでに成功している
			uint option = 0;
			if (bpp == 0) {
				//	option |= SDL_RESIZABLE;
			} else {
				option |= SDL.SDL_FULLSCREEN;
			}
			option |= SDL.SDL_OPENGL;	//	openGL強制

			IntPtr VideoInfo = SDL.SDL_GetVideoInfo();
			// query SDL for information about our video hardware

			if (VideoInfo == IntPtr.Zero) {   // if this query fails
				//	これ失敗されてもなぁ．．(´Д`)
			} else {
				/*		//	これ書いたほうがいいかな？
							if(VideoInfo -> hw_available)
								VideoFlags |= SDL_HWSURFACE;
							else
								VideoFlags |= SDL_SWSURFACE;
				*/
				//			if(VideoInfo.blit_hw)
				//			if((VideoInfo.flags & 0x0200)!=0)
				// is hardware blitting available
				option |= SDL.SDL_HWACCEL;
			}

			//	画面がダブルバッファであることを要求する
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);

			//	3Dモードならばdepth bufferが無いことには...
			/*
			if (use3d_rendering){
				SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE,16);
				//	16ビットのdepth buffer
			} else {
			}
			 * */

			// 2D描画onlyなのでdepth buffer いらねーや(´ー｀)
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 0);

			//	ステンシルバッファとアキュームレーションバッファは指定なし
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 0);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCUM_RED_SIZE, 0);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCUM_GREEN_SIZE, 0);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCUM_BLUE_SIZE, 0);
			SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCUM_ALPHA_SIZE, 0);

			IntPtr surface = SDL.SDL_SetVideoMode(width,height,bpp,option);
			if (surface == IntPtr.Zero) return YanesdkResult.SdlError; // failed
			/*
				openGLの場合は、surfaceに対して書き込むのでは
				ないから、取得する必要がない。
			*/

			bTestScreenSuccess = true;	//	変更成功
			if (strCaption != null)
			{
				//	キャプション設定されている
				SetCaption(strCaption);

				#region 備考
				/*
			285 ナマエ：Ozy　2006/05/01(月) 09:28 [*.bbtec.net] [Netscape6/WindowsXP]

			Mono1.1.15で動かしてみましたが、Mac版はほとんど変わってないっす;-;
			>>277
			<重要>の部分はほぼ同じです。但しMacOSXの場合はWIndowCaptionに
			渡す文字列をUTF-8にしなければうまく動いてくれません。
			つまりGetEncoding(932)だけではなく、public void SetCaptionのところも
			byte[] bs = Encoding.GetEncoding("utf-8").GetBytes(strCaption);
			としなければなりません。。。
				
			294 ナマエ：lpszMarimo_11　2006/05/01(月) 18:46 [*.accsnet.ne.jp] [Netscape6/Linux]

			>>289
			ウチの環境(FC4,SuSE10.0)ではGetEncoding("utf-8")でも大丈夫ぽいです。
			そこまでLinuxに詳しくないんで、他のディストリビューションではちょっとわかりませんが…
			*/
				#endregion
			}

			screen_.UpdateView(width,height);

			/*

			//	描画用のコンテクストも書き換えておかねば
			DrawContext d = screen_.getDrawContext();
			if (d != null){
				d.SetScreenSize(x,y);
			}
			 * */

			//	テクスチャー描画が基本なのでテクスチャーを有効にしておかねば
			Gl.glEnable(Gl.GL_TEXTURE_2D);

			#region by aki.
			
			this.bpp = bpp;
			this.option = option;

			#endregion

			return YanesdkResult.NoError;
		}

		#region by aki.

		// Movieで使うためにとりあえず設定をメンバに持っておく。(´ω`)

		public int Bpp
		{
			get { return bpp; }
		}

		public int Option
		{
			get { return (int)option; }
		}

		private int bpp;
		private uint option;

		#endregion

		/// <summary>
		/// 2次元描画向けスクリーン構造体。
		/// 画面への描画は、このメンバを介して行なう。
		/// </summary>
		public Screen2DGl Screen {
			get { return screen_; }
		}
		private Screen2DGl screen_;

		///	<summary>画面解像度の変更(2D/3D)。</summary>
		/// <remarks>
		///	引数の意味はtestVideoModeと同じ。
		///	変更できなければ非0が返る。変更できるとは限らないので
		///	beginScreenTest～endScreenTestを用いるべき。
		/// </remarks>
		public YanesdkResult SetVideoMode(int width, int height, int bpp)
		{
			BeginScreenTest();
			TestVideoMode(width,height,bpp);
			return EndScreenTest();
		}


		///	<summary>ウィンドゥキャプションの設定(2D/3D)。</summary>
		/// <remarks>
		///	画面の解像度変更前/後、どちらに実行しても良い。
		/// </remarks>
		public void SetCaption(string name)
		{
			strCaption = name;
			//			byte[] bs = Encoding.Default.GetBytes(strCaption);
			//			SDL.SDL_WM_SetCaption(bs, null);
			/*

			// Linuxの場合
			//  SDL.SDL_WM_SetCaption(Encoding.GetEncoding("utf-8").GetBytes(strCaption),null);

			// Windowsの場合
			//  SDL.SDL_WM_SetCaption(Encoding.UTF8.GetBytes(strCaption), null);

			 */

			// 修正されたSDLならば、単にこれだけで良いはずなのだが。
			// (SDL本家にはbug報告済み)
			SDL.SDL_WM_SetCaption(Encoding.UTF8.GetBytes(strCaption), null);
		}

		/// <summary>
		/// アプリケーションのアイコンを設定する
		/// </summary>
		/// <remarks>
		/// setVideoModeを実行前に呼び出すこと
		/// </remarks>
		/// <param name="file"></param>
		public void SetIcon(string file)
		{
			if (!bTestScreenSuccess)
				SDL.SDL_WM_SetIcon(SDL.IMG_Load(file), IntPtr.Zero);
		}

		///	<summary>このサーフェースの幅を得る。</summary>
		/// <remarks>
		///	setVideoModeで指定したxがこれ。
		/// </remarks>
		public int Width { get { return screen_.Width;}  }

		///	<summary>このサーフェースの高さを得る。</summary>
		/// <remarks>
		///	setVideoModeで指定したyがこれ。
		/// </remarks>
		public int Height { get { return screen_.Height; } }


		/// スクリーンテスト中か。
		/// </summary>
		protected bool bTestScreen;
		/// <summary>
		/// スクリーンテストに成功した。
		/// </summary>
		protected bool bTestScreenSuccess;
		/// <summary>
		/// ウィンドゥキャプション。
		/// </summary>
		protected string strCaption;

	}

	/// <summary>
	/// SDLWindowを使うときには、メッセージをpumpしないと
	/// いけない。そのpumpを行なうためのframework
	/// </summary>
	/// <code>
	/// while (sdlFrame.PollEvent() == YanesdkResult.NoError
	///     && !info.taskController.End)
	/// {
	///     info.taskController.CallTask(info);
	/// }
	/// sdlFrame.Quit();
	/// </code>
	public class SDLFrame
	{
		/// <summary>
		/// SDLWindowを用いる場合は、必ず定期的にこれを呼び出すこと。
		/// </summary>
		/// <returns></returns>
		public static YanesdkResult PollEvent()
		{
			//	Application.DoEvents(); // これを入れておかないとWindow切り替えでもたるようだ(?)
			#region ↑原因判明
			/*

146 ナマエ：もっちり　2006/04/04(火) 12:17 [*.yournet.ne.jp] [Opera8.53/WindowsXP]

IMEバーをタスクバーから取り出すか、非表示にするか、
詳細なテキストサービスをオフにするかで解決する類かもです。 

150 ナマエ：avin　2006/04/04(火) 14:37 [*.ocn.ne.jp] [MSIE6.0/WindowsXP]

>>149
色々確認してみましたが正常に動作しているようです。
お騒がせしてすみませんでした（；´ω`）
この件は解決したということで、協力していただいた皆様有難うございましたm(_ _)m
*/
			#endregion

			unsafe
			{
				SDL.SDL_Event evt = new SDL.SDL_Event();
				while (SDL.SDL_PollEvent(ref evt) != 0)
					if (evt.type == SDL.SDL_QUIT)
						return YanesdkResult.ShouldBeQuit;
				return YanesdkResult.NoError;
			}
		}

		/// <summary>
		/// 終了するときに呼び出す。内部的にSDL.Quitを呼び出している。
		/// MacOSの場合は必ず呼び出す必要がある。
		/// </summary>
		/// <returns></returns>
		public static void Quit()
		{
			SDL.SDL_Quit();

			if (Yanesdk.System.Platform.IsMac)
				Yanesdk.Sdl.MacAPI.MacAPI.ExitToShell();
		}
	}

}
