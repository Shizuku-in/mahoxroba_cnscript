using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// フルスクリーン化モジュール。
	/// </summary>
	/// <remarks>
	/// ■原因不明■
	/// サスペンドからの復帰や、マルチディスプレイ時にうまく機能しないことがある。
	/// (フォーカスが移るまで何も描画されない)
	/// ビデオカードのドライバに依存するようだが…。
	/// ビデオカードのドライバ側のbugとも言えるかも知れない。
	/// 
	/// ひょっとすると同じアプリケーションドメインから同時に複数のディスプレイの解像度を
	/// 切り替えてはいけないのかも知れない。
	/// </remarks>
	public class Win32DisplaySetting : IDisposable
	{
		#region ctor & Dispose
		public Win32DisplaySetting()
		{
			// Windows専用である
			if (System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);
		}

		/// <summary>
		///  Disposeでは、元の解像度に戻すことを保証する
		/// </summary>
		public void Dispose()
		{
			if (form != null)
			{
				form.Activated -= this.OnActivated;
			}

			{	// 元の解像度に戻す
				WindowMode();
			}
		}

		#endregion

		#region フルスクリーン化するために必要なモジュール

		// フルスクリーン化するために必要
		[StructLayout(LayoutKind.Sequential , CharSet = CharSet.Auto)]
		private struct DEVMODE
		{
			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 32)]
			public string dmDeviceName;
			public short dmSpecVersion;
			public short dmDriverVersion;
			public short dmSize;
			public short dmDriverExtra;
			public int dmFields;

			public int dmPositionX;
			public int dmPositionY;
			public int dmDisplayOrientation;
			public int dmDisplayFixedOutput;
			/*  // ↑は↓と等価 
				short  dmOrientation;
				short  dmPaperSize;
				short  dmPaperLength;
				short  dmPaperWidth;
				short  dmScale;
				short  dmCopies;
				short  dmDefaultSource;
				short  dmPrintQuality;
			 */

			public short dmColor;
			public short dmDuplex;
			public short dmYResolution;
			public short dmTTOption;
			public short dmCollate;
			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 32)]
			public string dmFormName;
			public short dmLogPixels;
			public int dmBitsPerPel;
			public int dmPelsWidth;
			public int dmPelsHeight;
			public int dmDisplayFlags;
			public int dmDisplayFrequency;

			public int dmICMMethod;
			public int dmICMIntent;
			public int dmMediaType;
			public int dmDitherType;
			public int dmReserved1;
			public int dmReserved2;
			public int dmPanningWidth;
			public int dmPanningHeight;
		}
		
		/// <summary>
		/// このAPI NT4.0は未サポート
		/// </summary>
		/// <param name="Unused"></param>
		/// <param name="iDevNum"></param>
		/// <param name="lpDisplayDevice"></param>
		/// <param name="dwFlags"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		private static extern bool EnumDisplayDevices(string lpDevice , uint iDevNum ,
		   ref DISPLAY_DEVICE lpDisplayDevice , uint dwFlags);

		[StructLayout(LayoutKind.Sequential , CharSet = CharSet.Auto)]
		private struct DISPLAY_DEVICE
		{
			[MarshalAs(UnmanagedType.U4)]
			public int cb;

			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 32)]
			public string DeviceName;

			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 128)]
			public string DeviceString;

			[MarshalAs(UnmanagedType.U4)]
			public uint StateFlags;

			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 128)]
			public string DeviceID;

			[MarshalAs(UnmanagedType.ByValTStr , SizeConst = 128)]
			public string DeviceKey;
		}
		

		[DllImport("user32.dll")]
		private static extern bool EnumDisplaySettings(
		  string deviceName , int modeNum , ref DEVMODE devMode);
		[DllImport("user32.dll" , CharSet = CharSet.Auto , SetLastError = true)]
		static extern bool EnumDisplaySettingsEx(string lpszDeviceName , int iModeNum ,
		   ref DEVMODE lpDevMode , uint dwFlags);
		[DllImport("user32.dll" , CharSet = CharSet.Auto)]
		static extern int ChangeDisplaySettingsEx(string lpszDeviceName ,
		   ref DEVMODE lpDevMode , IntPtr hwnd , uint dwflags , IntPtr lParam);

		private const int ENUM_CURRENT_SETTINGS = -1;
		private const int ENUM_REGISTRY_SETTINGS = -2;
		private const int CDS_UPDATEREGISTRY = 0x01;
		private const int CDS_TEST = 0x02;
		private const int CDS_FULLSCREEN = 0x04;
		private const int DISP_CHANGE_SUCCESSFUL = 0;
		private const int DISP_CHANGE_RESTART = 1;
		private const int DISP_CHANGE_FAILED = -1;
		
		// dmFields
		private const int DM_BITSPERPEL = 0x40000;
		private const int DM_PELSWIDTH = 0x80000;
		private const int DM_PELSHEIGHT = 0x100000;
		private const int DM_DISPLAYFLAGS = 0x200000;
		private const int DM_DISPLAYFREQUENCY = 0x400000;

	//	[DllImport("user32.dll")]
	//	private static extern int MoveWindow(IntPtr hwnd , int x , int y ,
	//	int nWidth , int nHeight , int bRepaint);

		#endregion



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
		public void BeginScreenTest()
		{
			bTestScreen = true;
			bTestScreenSuccess = false;

			if ( devName != "" )
			{
				TestVideoMode(0 , 0 , 0); // まずは元に戻す
			}

		}

		///	<summary>スクリーン解像度のテスト終了(2D/3D)。</summary>
		/// <remarks>
		///	beginScreenTest も参照すること。
		///
		///	スクリーン変更に成功していれば0,失敗していれば非0が返る。
		/// </remarks>
		public YanesdkResult EndScreenTest()
		{
			bTestScreen = false;
			return bTestScreenSuccess ? YanesdkResult.NoError : YanesdkResult.HappenSomeError;
		}

		/// スクリーンテスト中か。
		/// </summary>
		protected bool bTestScreen;
		/// <summary>
		/// スクリーンテストに成功した。
		/// </summary>
		protected bool bTestScreenSuccess;

		///	<summary>スクリーン解像度のテスト(2D/3D)。</summary>
		/// <remarks>
		///	スクリーン解像度のテストを行なう。
		///	width,height はスクリーンサイズ。bpp はピクセル深度(bits per pixel)
		///	bpp として0を指定すれば、ウィンドゥモード。(元のモード)
		///	(現在のモードから解像度切り替えを行なわないの意味)
		/// bpp == 0を指定するときは、width,heightは無視される。
		/// 
		///	beginScreenTest～endScreenTestで用いる。
		///	beginScreenTest も参照すること。
		///
		///	成功すれば0が返るが、この返し値を使うのではなく、
		///	beginScreenTest～endScreenTestで用いること。
		/// 
		/// TopMostとか、Maximizeは指定していないので、場合によってはウィンドゥを移動させられてしまう。
		/// 自前でウィンドゥキャプションのdragを実装しているときは注意すること。
		/// </remarks>
		public YanesdkResult TestVideoMode(int width , int height , int bpp)
		{
			if ( bTestScreenSuccess )
				return YanesdkResult.AlreadyDone; // すでに成功している

			if ( !bFullScreen && bpp == 0 )
				return YanesdkResult.AlreadyDone; // Windowsモードなので変更なし。

			// dual displayで正しく動作させるためには、
			// 現在、Formの存在するほうのDisplayに対してアクションを行なう必要がある。

			DEVMODE dm = new DEVMODE();
			dm.dmDeviceName = new String(new char[32]);
			dm.dmFormName = new String(new char[32]);
			dm.dmSize = ( short ) Marshal.SizeOf(typeof(DEVMODE));
			dm.dmDriverExtra = 0;

			if ( devName == null )
			{
				global::System.Windows.Forms.Screen scr;
				// このフォームの存在するdisplayを探す必要があるのか？
				if ( form != null )
					scr	= global::System.Windows.Forms.Screen.FromHandle(form.Handle);
				else
					scr = global::System.Windows.Forms.Screen.PrimaryScreen;
				devName = scr.DeviceName;
			}

			#region テスト用のコード
				/*
				// ↑こいつから、何番目のdisplayであるのかを探し当てる
				int displayNo = -1;
				for ( int i = 0 ; i < global::System.Windows.Forms.Screen.AllScreens.Length ; ++i )
				{
					if ( global::System.Windows.Forms.Screen.AllScreens[i].Bounds == scr.Bounds )
					{
						displayNo = i;
						break;
					}
				}
				*/


				/*
				// このAPI NT4.0では未サポート..
				DISPLAY_DEVICE device = new DISPLAY_DEVICE();
				device.cb = Marshal.SizeOf(device);

				EnumDisplayDevices(null , (uint)displayNo ,ref device , 0);
				dm.dmDeviceName = device.DeviceName;
				*/

				// int result = EnumDisplaySettings(device.DeviceName ,
				//	  0 , ref dm);


				//if ( result == 0 )
				//	return YanesdkResult.Win32apiError; // お目当てのディスプレイが見当たらない

				//	if ( dm.dmDeviceName == devName )
				//		break; // 一致した
				// ↑この返ってくる文字列がどうもゴミ。

			/*
			try
			{
				if ( bpp == 0 )
				{
					if ( !EnumDisplaySettingsEx(devName ,
						   ENUM_REGISTRY_SETTINGS , ref dm , 0) )
					{
						return YanesdkResult.Win32apiError;
					}
				}
				else
				{
					if ( !EnumDisplaySettingsEx(devName ,
						   ENUM_CURRENT_SETTINGS , ref dm , 0) )
					{
						return YanesdkResult.Win32apiError;
					}
				}
			//	devName = dm.dmDeviceName;
				// ↑これ、嘘の情報が返ってくる可能性があるので使うべきではない
			}
			catch
			{	// 何を思ったか、detachしないビデオカードがあって、その場合、COM例外が飛んでくる。(?)
				return YanesdkResult.Win32apiError;
			}
			 */
			#endregion

			if ( bpp != 0 )
			{	//	フルスクリーン化 == 解像度を変更する必要あり
				dm.dmPelsWidth = width; // iWidth;
				dm.dmPelsHeight = height; // iHeight; 
				dm.dmBitsPerPel = (short)bpp;
				dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL;
			}
			else
			{	// ウィンドゥモード化 == 解像度を元に戻す
			//	dm.dmPelsWidth = originalWidth;
			//	dm.dmPelsHeight = originalHeight;
			//	dm.dmBitsPerPel = (short)originalBpp;

				// 元の解像度を知らねばならないので…
				try
				{
					// 元の解像度をレジストリから読み出す
					if ( !EnumDisplaySettingsEx(devName ,
						   ENUM_REGISTRY_SETTINGS , ref dm , 0) )
					{
						return YanesdkResult.Win32apiError;
					}
				} catch {
					return YanesdkResult.Win32apiError;
				}
				
				dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
			}

			try
			{
				// フルスクリーンモードにするのか？
				if ( bpp != 0 )
				{
					if ( ChangeDisplaySettingsEx(devName ,
						  ref dm , IntPtr.Zero , CDS_FULLSCREEN , IntPtr.Zero) != 0 )
					{
						return YanesdkResult.Win32apiError;
					}
				}
				else
				{
					// 元に戻した。
					if ( ChangeDisplaySettingsEx(devName ,
						  ref dm , IntPtr.Zero , 0 , IntPtr.Zero) != 0 )
					{
						return YanesdkResult.Win32apiError;
					}
					devName = null; // もうこのスクリーンのことは忘れ去って良い
				}
			}
			catch
			{	// 何を思ったか、detachしないビデオカードがあって、その場合、COM例外が飛んでくる。(?)
				return YanesdkResult.Win32apiError;
			}
			if ( bpp != 0 )
				bFullScreen = true;
			else
				bFullScreen = false;

			bTestScreenSuccess = true;	//	変更成功

			if ( form != null )
			{
				if ( bpp != 0 )
				{
					// ウィンドゥ解像度変更したので、タスクバーに隠れないように再度、最大化
					oldStyle = form.FormBorderStyle; // 前回の状態を記録しておく
					form.FormBorderStyle = FormBorderStyle.None;

					// form.Bounds = new global::System.Drawing.Rectangle(scr.Bounds.Left , scr.Bounds.Top ,
					// 	width , height);

					MoveWindowToMyDisplayDevice(this.devName , this.form);
					
					#region テスト用コード
					// form.Bounds = scr.Bounds;
				//	form.WindowState = FormWindowState.Maximized;

					// これ、いま解像度を変更したディスプレイの(0,0)でなければ意味がない。
					//form.SetDesktopLocation(0 , 0);
					
					// ここから、現在のposへ移動させてやる。

					// このフォームの存在するdisplayを探す必要がある。
					// global::System.Windows.Forms.Screen scr =
					//	global::System.Windows.Forms.Screen.FromHandle(form.Handle);
					///	new global::System.Drawing.Point(posX - scr.Bounds.Left , posY - scr.Bounds.Top);

					// Maximizedのアニメーション中だと、移動させる意味がないので、
					// 最大化アニメーションが終了したことを検知しなければならない。

					// form.Location = new global::System.Drawing.Point(scr.Bounds.Left , scr.Bounds.Top);
					// ↑maximizedされているlocationは変更できない(´ω`)

					//	MoveWindow(form.Handle , scr.Bounds.Left , scr.Bounds.Top , form.Width , form.Height , 1);
					#endregion
				}
				else
				{
					form.FormBorderStyle = oldStyle;
				//	form.WindowState = FormWindowState.Normal; // 通常モードに戻す必要あり
				}
			}

			return YanesdkResult.NoError;
		}
		private FormBorderStyle oldStyle = FormBorderStyle.None;

		/// <summary>
		/// フルスクリーン時に、他のディスプレイの状態が変化したがために
		/// 仮想スクリーンでのウィンドゥ座標が変更になっていた場合にウィンドゥを
		/// 移動させる処理をしないといけないので、そのためのチェックをここで行なう。
		/// </summary>
		private void OnActivated(object sender , global::System.EventArgs e)
		{
			// フルスクリーンモードのくせに、デスクトップのlocationがおかしければ移動
			if ( IsFullScreen )
			{

				#region テスト用コード
				// このディスプレイをfull screen化する
				// global::System.Windows.Forms.Screen scr;

				// このフォームの存在するdisplayを探す必要がある。
				// scr = global::System.Windows.Forms.Screen.FromHandle(form.Handle);

				/*
				if ( scr != null )
				{
					int x = Form.Location.X;
					int y = Form.Location.Y;
					if ( scr.Bounds.Left != x || scr.Bounds.Top != y )
					{	// ディスプレイ位置が変更になったのでウィンドゥ移動
						//	form.WindowState = FormWindowState.Normal;
						Form.Location = new global::System.Drawing.Point(scr.Bounds.Left , scr.Bounds.Top);
						//	form.WindowState = FormWindowState.Maximized;
					}
				}
				 */
				#endregion

				MoveWindowToMyDisplayDevice(this.devName , this.form);
			}
		}

		/// <summary>
		/// 自分の保持しているフォームを、フルスクリーン化されたときに、
		/// そのフォームが属するウィンドゥデバイスの(0,0)の位置に移動させるための処理
		/// </summary>
		private static void MoveWindowToMyDisplayDevice(string devName , Form form)
		{
			if ( form == null )
				return;

			DEVMODE dm = new DEVMODE();
			dm.dmDeviceName = new String(new char[32]);
			dm.dmFormName = new String(new char[32]);
			dm.dmSize = ( short ) Marshal.SizeOf(typeof(DEVMODE));
			dm.dmDriverExtra = 0;

			// このdevice nameを持つdisplayを列挙
			dm.dmDeviceName = devName;

			try
			{
				if ( !EnumDisplaySettingsEx(devName ,
					   ENUM_CURRENT_SETTINGS , ref dm , 0) )
					return;
			}
			catch
			{
				return;
			}

			// そのdisplayの(0,0)に移動
			int x = dm.dmPositionX;
			int y = dm.dmPositionY;

			form.Location = new global::System.Drawing.Point(x , y);

			#region メモ書き
			// 解像度切り替えの直後、何も描画されないことがある。
			// form.Invalidate();
			// これではないのか(´ω`)

			// これか？
			// form.FormBorderStyle = FormBorderStyle.Sizable;
			// form.FormBorderStyle = FormBorderStyle.None;

			// これか？
			//	form.Activate();
			
			// 違うようだ。

			// どうもmulti display時にopenGLのサーフェースをもつウィンドゥを
			// 複数出したときに、ビデオカードによっては、ChangeDisplayExの直後
			// 画面が出ないことがあるようだ。alt + tabで他のアプリがactiveになったときに
			// 出るのだけれども。
			// multi displayはどうも鬼門のようだ。
			#endregion
		}


		/// <summary>
		///  ウィンドゥモードにする。
		/// </summary>
		public void WindowMode()
		{
			if ( IsFullScreen )
			{
				BeginScreenTest();
				TestVideoMode(0 , 0 , 0);
				EndScreenTest();
			}
		}

		/// <summary>
		/// フルスクリーンモードにする
		/// 
		/// 切り替えに失敗すれば、32bpp → 24bpp → 16bppのようにbppを落として試して、
		/// それでもダメならWindowモードにする。
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="bpp"></param>
		public void FullScreenMode(int width , int height , int bpp)
		{
			BeginScreenTest();
			TestVideoMode(width , height , bpp);
			while ( bpp >= 16 )
			{
				bpp -= 8; // 32→24→16のようにbppを落としてみる
				TestVideoMode(width , height , bpp);
			}
			TestVideoMode(width , height , 0); // ダメならウィンドゥモード
			EndScreenTest();
		}

		/// <summary>
		/// 事前にFormを渡しておけば、フルスクリーン化するときに
		/// 自動的に
		///		this.WindowState = FormWindowState.Maximized
		///	と
		///		this.SetDesktopLocation(0 , 0);
		///	を呼び出す。
		/// </summary>
		public Form Form
		{
			get { return form; }
			set {
				form = value;
				// WM_ACTIVATEAPPをhookしとくか。
				form.Activated += OnActivated;
			}
		}
		private Form form;

		/// <summary>
		/// 解像度変更したデバイス
		/// </summary>
		private string devName = null;
		
		/// <summary>
		/// 現在の画面モード
		/// </summary>
		public bool IsFullScreen
		{
			get { return bFullScreen; }
		}
		private bool bFullScreen = false;
	}
}
