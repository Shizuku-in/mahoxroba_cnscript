
using System;
using System.Collections.Generic;
using System.Text;
using OpenGl;
using Sdl;
using Yanesdk.Ytl;
using System.Runtime.InteropServices;

using System.Drawing;

namespace Yanesdk.Draw
{
	/// <summary>
	/// Win32Window2DGlのtypedef
	/// </summary>
	/// <remarks>後方互換のために用意されている</remarks>
	public class Win32Window : Win32Window2DGl {
		/// <summary>
		/// あとから初期化する場合は、InitByHdcかInitByHWndメソッドを呼び出すこと。
		/// </summary>
		public Win32Window() : base()
		{
			// Windows専用の機能
			if (Yanesdk.System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);
		}

		/// <summary>
		/// handleとして、コントロールのhandle(HWND型:C#ではIntPtrを渡して。
		/// </summary>
		/// <param name="handle"></param>
		public Win32Window(IntPtr hWnd) : base(hWnd)
		{
			// Windows専用の機能
			if (Yanesdk.System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);
		}
	}

	/// <summary>
	/// HDC経由で描画するWindowsプラットフォーム専用のクラス
	/// </summary>
	/// <remarks>
	/// .NETのpicture boxなど、HWnd(Handle)を取得できるものにならばどこにでも描画できる。
	/// ただし、ひとつのフォームに貼り付けられたpictrue boxに描画するために
	/// それぞれがこのクラスのinstanceを生成していたのでは、管理が複雑になる。
	/// (他のinstanceに関連付けられているTextureにアクセスできないため)
	/// よって、そういうつくりにはしないほうが賢明だと思われる。
	/// 
	/// 使い方としてはOpenGLControlクラスを参照のこと。
	/// 
	/// openglをWindowsで使う場合、bufferのDCが取れない。(DirectXならとれるのに..)
	/// このことが.NETのgraphicクラスと恐ろしく親和性が悪い。どうしたものか…。
	/// </remarks>
	public class Win32Window2DGl : IDisposable
	{
		#region opengl32を呼び出すための前準備

		// External Win32 libraries
		public const string GDI_DLL = "gdi32.dll";		// Import library for GDI on Win32
		public const string USR_DLL = "user32.dll";		// Import library for User on Win32
		public const string KER_DLL = "kernel32.dll";	// Import library for Kernel on Win32

		// user32.dll unmanaged Win32 DLL
		[DllImport(USR_DLL)]
		private static extern IntPtr GetDC(IntPtr hWnd);
		[DllImport(USR_DLL)]
		private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		// gdi32.dll unmanaged Win32 DLL
		[DllImport(GDI_DLL)]
		[CLSCompliant(false)]
		public unsafe static extern int ChoosePixelFormat(IntPtr hdc,
PIXELFORMATDESCRIPTOR* ppfd);
		[DllImport(GDI_DLL)]
		[CLSCompliant(false)]
		public unsafe static extern int SetPixelFormat(IntPtr hdc, int iPixelFormat,
PIXELFORMATDESCRIPTOR* ppfd);
		[DllImport(GDI_DLL)]
		[CLSCompliant(false)]
		public static extern int SwapBuffers(IntPtr hdc);

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT32
		{
			public int right;
			public int top;
			public int left;
			public int bottom;
		}

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hWnd, out RECT32 lpRect);

		[DllImport("user32.dll")]
		static extern bool GetClientRect(IntPtr hWnd, out RECT32 lpRect);

		// Structure of PIXELFORMATDESCRIPTOR used by ChoosePixelFormat()
		[StructLayout(LayoutKind.Sequential)]
		[CLSCompliant(false)]
		public struct PIXELFORMATDESCRIPTOR
		{
			public ushort nSize;
			public ushort nVersion;
			public uint dwFlags;
			public byte iPixelType;
			public byte cColorBits;
			public byte cRedBits;
			public byte cRedShift;
			public byte cGreenBits;
			public byte cGreenShift;
			public byte cBlueBits;
			public byte cBlueShift;
			public byte cAlphaBits;
			public byte cAlphaShift;
			public byte cAccumBits;
			public byte cAccumRedBits;
			public byte cAccumGreenBits;
			public byte cAccumBlueBits;
			public byte cAccumAlphaBits;
			public byte cDepthBits;
			public byte cStencilBits;
			public byte cAuxBuffers;
			public sbyte iLayerType;
			public byte bReserved;
			public uint dwLayerMask;
			public uint dwVisibleMask;
			public uint dwDamageMask;
		}

		// Layer types
		[CLSCompliant(false)]
		public enum LayerTypes : sbyte
		{
			PFD_MAIN_PLANE = 0,
			PFD_OVERLAY_PLANE = 1,
			PFD_UNDERLAY_PLANE = -1,
		}

		// Pixel types
		public enum PixelTypes : byte
		{
			PFD_TYPE_RGBA = 0,
			PFD_TYPE_COLORINDEX = 1,
		}
		// PIXELFORMATDESCRIPTOR flags

		[Flags]
		[CLSCompliant(false)]
		public enum PFD_Flags : uint
		{
			PFD_DOUBLEBUFFER = 0x00000001,
			PFD_STEREO = 0x00000002,
			PFD_DRAW_TO_WINDOW = 0x00000004,
			PFD_DRAW_TO_BITMAP = 0x00000008,
			PFD_SUPPORT_GDI = 0x00000010,
			PFD_SUPPORT_OPENGL = 0x00000020,
			PFD_GENERIC_FORMAT = 0x00000040,
			PFD_NEED_PALETTE = 0x00000080,
			PFD_NEED_SYSTEM_PALETTE = 0x00000100,
			PFD_SWAP_EXCHANGE = 0x00000200,
			PFD_SWAP_COPY = 0x00000400,
			PFD_SWAP_LAYER_BUFFERS = 0x00000800,
			PFD_GENERIC_ACCELERATED = 0x00001000,
			PFD_SUPPORT_DIRECTDRAW = 0x00002000,
		}

		// AttribMask
		[Flags]
		[CLSCompliant(false)]
		public enum AttribMask : uint
		{
			GL_CURRENT_BIT = 0x00000001,
			GL_POINT_BIT = 0x00000002,
			GL_LINE_BIT = 0x00000004,
			GL_POLYGON_BIT = 0x00000008,
			GL_POLYGON_STIPPLE_BIT = 0x00000010,
			GL_PIXEL_MODE_BIT = 0x00000020,
			GL_LIGHTING_BIT = 0x00000040,
			GL_FOG_BIT = 0x00000080,
			GL_DEPTH_BUFFER_BIT = 0x00000100,
			GL_ACCUM_BUFFER_BIT = 0x00000200,
			GL_STENCIL_BUFFER_BIT = 0x00000400,
			GL_VIEWPORT_BIT = 0x00000800,
			GL_TRANSFORM_BIT = 0x00001000,
			GL_ENABLE_BIT = 0x00002000,
			GL_COLOR_BUFFER_BIT = 0x00004000,
			GL_HINT_BIT = 0x00008000,
			GL_EVAL_BIT = 0x00010000,
			GL_LIST_BIT = 0x00020000,
			GL_TEXTURE_BIT = 0x00040000,
			GL_SCISSOR_BIT = 0x00080000,
			GL_ALL_ATTRIB_BITS = 0x000fffff,
		}

		// MatrixMode
		[CLSCompliant(false)]
		public enum MatrixMode : uint
		{
			GL_MODELVIEW = 0x1700,
			GL_PROJECTION = 0x1701,
			GL_TEXTURE = 0x1702,
		}

		// BeginMode
		[CLSCompliant(false)]
		public enum BeginMode : uint
		{
			GL_POINTS = 0x0000,
			GL_LINES = 0x0001,
			GL_LINE_LOOP = 0x0002,
			GL_LINE_STRIP = 0x0003,
			GL_TRIANGLES = 0x0004,
			GL_TRIANGLE_STRIP = 0x0005,
			GL_TRIANGLE_FAN = 0x0006,
			GL_QUADS = 0x0007,
			GL_QUAD_STRIP = 0x0008,
			GL_POLYGON = 0x0009,
		}

		/*
		//
		// 実は、明示的にopengl32.dllをloadlibraryで読み込んでおく必要がある。
		// (同じモジュール空間に事前に存在しないとダメのようだ)
		//

		private static uint m_hModuleOGL = 0;	// Handle to OPENGL32.DLL
		// kernel32.dll unmanaged Win32 DLL
		[DllImport(KER_DLL)]
//		private static extern uint LoadLibrary(string lpFileName);
		public const string OGL_DLL = "opengl32.dll";	// Import library for OpenGL on Win32

		static Win32Window()
		{
			// Explicitly load the OPENGL32.DLL library
			m_hModuleOGL = LoadLibrary(OGL_DLL);
		}
		 * */

		#endregion

		/// <summary>
		/// handleとして、コントロールのhandle(HWND型:C#ではIntPtrを渡して。
		/// </summary>
		/// <param name="handle"></param>
		public Win32Window2DGl(IntPtr hWnd)
		{
			InitByHWnd(hWnd);
		}

		/*
		   // これをやるとthreadが終了しなくなる(´ω`)
		sealed class GlInitializer {

			public GlInitializer()
			{
				// BGコンパチなsurface生成
				IntPtr hNullDC = GetDC(IntPtr.Zero);
				CreatePixelFormat(hNullDC);
				// ↑これに失敗してたらどうしようもないんだけども(´ω`)

				// Create a new OpenGL rendering contex
				hNullRC = opengl32.wglCreateContext(hNullDC);
				
				ReleaseDC(IntPtr.Zero,hNullDC);
			}

			~GlInitializer()
			{
			//	opengl32.wglDeleteContext(hNullRC);
			}

			public IntPtr hNullRC;
		}
		*/


		/// <summary>
		/// あとから初期化する場合は、InitByHdcかInitByHWndメソッドを呼び出すこと。
		/// </summary>
		public Win32Window2DGl()
		{
		}

		private static YanesdkResult CreatePixelFormat(IntPtr hDC)
		{
			// Win32系では、先行してGLの何らかのメソッドを呼び出して、
			// 同じプロセスメモリ空間にGLのdllを読み込んでおかないと、
			// ChoosePixelFormatが間接的に読み込むGLのmoduleが同定(?)されない。

			// よって、ここでダミーのGLのメソッド呼び出しを入れる次第である。
			Gl.glGetError();

			// Create a pixel format
			PIXELFORMATDESCRIPTOR pfd;
			unsafe
			{
				pfd.nSize = ( ushort ) sizeof(PIXELFORMATDESCRIPTOR);
				pfd.nVersion = 1;
				pfd.dwFlags = ( uint ) ( PFD_Flags.PFD_DRAW_TO_WINDOW | PFD_Flags.PFD_SUPPORT_OPENGL |
	 PFD_Flags.PFD_DOUBLEBUFFER );
				pfd.iPixelType = ( byte ) PixelTypes.PFD_TYPE_RGBA;
				pfd.cColorBits = 24;
				pfd.cRedBits = 0;
				pfd.cRedShift = 0;
				pfd.cGreenBits = 0;
				pfd.cGreenShift = 0;
				pfd.cBlueBits = 0;
				pfd.cBlueShift = 0;
				pfd.cAlphaBits = 0;
				pfd.cAlphaShift = 0;
				pfd.cAccumBits = 0;
				pfd.cAccumRedBits = 0;
				pfd.cAccumGreenBits = 0;
				pfd.cAccumBlueBits = 0;
				pfd.cAccumAlphaBits = 0;
				pfd.cDepthBits = 0;
				// ↑depthこれでええんか？2次元描画なら0でええんちゃう？
				//  ふつうは 16 or 32

				pfd.cStencilBits = 0;
				pfd.cAuxBuffers = 0;
				pfd.iLayerType = ( sbyte ) LayerTypes.PFD_MAIN_PLANE;
				pfd.bReserved = 0;
				pfd.dwLayerMask = 0;
				pfd.dwVisibleMask = 0;
				pfd.dwDamageMask = 0;

				// Match an appropriate pixel format 
				int iPixelformat;
				if ( ( iPixelformat = ChoosePixelFormat(hDC , &pfd) ) == 0 )
					return YanesdkResult.HappenSomeError;

				// Sets the pixel format
				if ( SetPixelFormat(hDC , iPixelformat , &pfd) == 0 )
					return YanesdkResult.HappenSomeError;
			}
			return YanesdkResult.NoError;
		}

		static IntPtr theFirstHRc;

		// IntPtr oldContext;
		//	↑
		// Win32WindowのSelectのネストが出来るようにしようと考えたのだが、
		// 前のコンテキストを保存しておいても、
		//	Select
		//	 Select
		//   Update ←このあと、1つ目のSelectのViewに設定しなおす必要があるのだが、
		//			このための復帰処理が難しい
		//  Update
		//		ので、こういう実装は現実的ではないと判断する。

		//　初期化用のhelper関数
		private YanesdkResult InitByHdc_(IntPtr hDc)
		{
			this.hDC = hDc;

			screen_ = new Screen2DGl();
			screen_.BeginDrawDelegate = delegate
			{
				hDC = GetDC(this.hWnd);
			//	oldContext = opengl32.wglGetCurrentContext();
				Opengl32.wglMakeCurrent(this.hDC , this.hRC);
				//	テクスチャー描画が基本なのでテクスチャーを有効にしておかねば
			};
			screen_.EndDrawDelegate = delegate
			{
				SwapBuffers(this.hDC);
				Opengl32.wglMakeCurrent(this.hDC,IntPtr.Zero);
				ReleaseDC(this.hWnd, this.hDC);
			};

			// 描画を行なわないdelegateを渡す。
			// なぜなら、テクスチャの読み込みは、
			// wglMakeCurrentが実行されていないと
			// 正しく読み込まれないので、beginDrawDelegateを行なってから
			// 読み込み、それが終わり次第この
			// delegateを呼び出して、openglのassignを解放してやる必要がある。
			screen_.QuitDrawDelegate = delegate
			{
				Opengl32.wglMakeCurrent(this.hDC , IntPtr.Zero);
		//		opengl32.wglMakeCurrent(this.hDC , oldContext);
				ReleaseDC(this.hWnd, this.hDC);
			};
			
			YanesdkResult result = CreatePixelFormat(this.hDC);

			if ( result != YanesdkResult.NoError )
				return result; 

			// Create a new OpenGL rendering contex
			this.hRC = Opengl32.wglCreateContext(this.hDC);

			// context共有を利用する。
			// 0番は常に生きていると仮定。
			// opengl32.wglShareLists(Singleton<GlInitializer>.Instance.hNullRC,this.hRC);
			//	↑これをやるとthreadが終了しなくなるので
			//    一つ目のRendering Contextだけ解放しないことにする。

			if ( theFirstHRc != IntPtr.Zero )
				Opengl32.wglShareLists(theFirstHRc , this.hRC);
			else
				theFirstHRc = this.hRC;

			ReleaseDC(this.hWnd, this.hDC);

			RECT32 rect = new RECT32();
		//	GetWindowRect((IntPtr)this.hWnd, out rect);

			GetClientRect((IntPtr)this.hWnd, out rect);
			Screen.UpdateView(rect.left - rect.right, rect.bottom - rect.top);

			return YanesdkResult.NoError;
		}

	/*
		/// <summary>
		/// コンストラクタで初期化せずに
		/// あとから初期化する場合は、このメソッドか、InitByHWndかを呼び出して。
		/// </summary>
		/// <param name="hWnd"></param>
		public YanesdkResult InitByHdc(IntPtr hDc)
		{
			Release();
			this.hWnd = (IntPtr)null;
			return InitByHdc_(hDc);
		}
	*/

		/// <summary>
		/// コンストラクタで初期化せずに
		/// あとから初期化する場合は、このメソッドを呼び出して。
		/// </summary>
		/// <param name="hWnd"></param>
		public YanesdkResult InitByHWnd(IntPtr hWnd)
		{
			Release();

			this.hWnd = hWnd;
			return InitByHdc_(GetDC(this.hWnd));
		}

		/// <summary>
		/// 終了処理。(Disposeから呼び出されるので通例、呼び出す必要はない)
		/// </summary>
		private void Release()
		{
			if (hRC != IntPtr.Zero)
			{
				if (theFirstHRc != this.hRC) // ひとつ目のだけ保持しておく必要あり。
					Opengl32.wglDeleteContext(hRC);
				hRC = IntPtr.Zero;
			}
		}

		public void Dispose()
		{
			Release();
		}

		/// <summary>
		/// 描画対象となるDeviceContext
		/// </summary>
		private IntPtr hWnd =IntPtr.Zero;		// Window Handle
		private IntPtr hDC = IntPtr.Zero;		// Window Device Context
		private IntPtr hRC = IntPtr.Zero;		// OpenGL rendering context

		public Screen2DGl Screen
		{
			get { return screen_; }
		}
		Screen2DGl screen_;
	}

}
