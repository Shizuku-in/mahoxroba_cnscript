using System;
using System.Runtime.InteropServices;
using Sdl;

namespace Yanesdk.Input
{
	/// <summary>
	/// キー入力クラス。
	/// </summary>
	/// <remarks>
	/// <para>
	/// キーのスキャンコードはSDLのものと同等
	/// →SDL/SDL_keysym.dを参照のこと。
	/// →SDLのimportをしたくない場合、
	/// 	このヘッダのなかにあるKeyCodeというenumを用いても良い。
	/// </para>
	/// <para>
	/// 関数仕様についてはJoyStickと同じなのでそちらを参照のこと。
	/// </para>
	/// </remarks>
	public class KeyBoardInput : IKeyInput {

		/// <summary>
		/// 該当ボタン(キー)が前回押されていなくて今回押されているか。
		/// (updateを呼び出すごとに値が更新される)
		/// </summary>
		/// <param name="nButtonNo">KeyCodeを渡してちょ</param>
		/// <returns></returns>
		public bool IsPush(int nButtonNo) {
			if (nButtonNo > ButtonNum) return false;
			return (buttonState[1-flip][nButtonNo]==false)
				&& (buttonState[flip][nButtonNo]!=false);
		}

		/// <summary>
		/// IsPush(int)のほうはcastするのが面倒なので
		/// こちらも用意。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsPush(KeyCode key)
		{
			return IsPush((int)key);
		}

		/// <summary>
		/// どれかひとつでも押されていれば trueが返る
		/// </summary>
		/// <returns></returns>
		public bool IsPush() {
			for (int i = 0; i < ButtonNum; ++i) {
				if (IsPush(i))
					return true;
			}
			return false;
		}

		/// <summary>
		/// ボタンが現在おされているか。
		/// KeyCodeを渡してちょ。
		/// </summary>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsPress(int nButtonNo) {
			if (nButtonNo >= ButtonNum) return false;
			return (buttonState[flip][nButtonNo]!=false);
		}

		/// <summary>
		/// IsPress(int)のほうはcastするのが面倒なので
		/// こちらを使っても良い。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsPress(KeyCode key){
			return IsPress((int)key);
		}

		/// <summary>
		/// どれかひとつでも押されていれば trueが返る
		/// </summary>
		public bool IsPress()
		{
			for (int i = 0; i < ButtonNum; ++i) {
				if (IsPress(i))
					return true;
			}
			return false;
		}

		/// <summary>
		/// 前回のupdateのときに押されていて、今回のupdateで押されていない。
		/// </summary>
		/// <returns></returns>
		public bool IsPull(int nButtonNo)
		{
			if (nButtonNo >= ButtonNum) return false;
			return (buttonState[flip ^ 1][nButtonNo]) && (!buttonState[flip][nButtonNo]);
		}

		/// <summary>
		/// IsPull(int)のほうはcastするのが面倒なので
		/// こちらを使っても良い。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsPull(KeyCode key)
		{
			return IsPull((int)key);
		}

		/// <summary>
		/// 前回のupdateのときに押されていなくて、今回のupdateでも押されていない。
		/// </summary>
		/// <returns></returns>
		public bool IsFree(int nButtonNo)
		{
			if (nButtonNo >= ButtonNum) return false;
			return (!buttonState[flip ^ 1][nButtonNo]) && (!buttonState[flip][nButtonNo]);
		}

		/// <summary>
		/// IsFree(int)のほうはcastするのが面倒なので
		/// こちらを使っても良い。
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsFree(KeyCode key)
		{
			return IsFree((int)key);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int ButtonNum { get { return buttonState[0].Length; } }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string DeviceName { get { return "KeyBoard"; } }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IntPtr Info { get { return IntPtr.Zero; } }

		/// <summary>
		/// デバイスをopenしているわけではないのでcloseする必要がない。
		/// </summary>
		public void Dispose()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// <para>
		/// SDLのSDL_PumpEventsを定期的に呼び出していることを前提とする。
		/// ここで得られるのは前回のSDL_PumpEventsを呼び出してからの
		/// 入力情報である。
		/// </para>
		/// <para>
		/// また、キー入力には、
		/// 	SDL_SetVideoMode(640,480,16,SDL_SWSURFACE);
		/// に相当する命令で、SDLのウィンドゥを初期化してある必要がある。
		/// (キー入力は、ウィンドゥに付帯するものなので)	
		/// 
		/// Windows上で動作させるときは、
		/// GetAsyncKeyStateを用いるのでフォーカスがどこにあろうとキー入力が出来る。
		/// もしこの仕様がまずければこのクラスは使うべきではない。
		/// 
		/// また、EnterキーはNum Padのほうと区別がつかない。
		/// Num PadのほうのEnterが押されてもKeyCode.RETURNが返ってくるので注意。
		/// あと、すべてのキーの情報は取得していない。(普段使わないキーをチェックしても
		/// 処理時間がもったいないため) サポートしているキーについては
		/// CheckKeyInWindowsメソッドの実装を見ること。これでまずければ自前で書いて。
		/// </para>
		/// </remarks>
		public void Update() {
			flip = 1 - flip;

			if (Yanesdk.System.Platform.IsLinux)
			{
				IntPtr pkeys = SDL.SDL_GetKeyState(IntPtr.Zero);
				byte[] keys = new byte[ButtonNum];
				Marshal.Copy(pkeys, keys, 0, keys.Length);
				for (int i = 0; i < keys.Length; i++)
				{
					buttonState[flip][i] = keys[i] != 0;
				}
			}
			else
			{
				/*
				if (userControl != null || userForm != null)
				{
					// キー入力のイベントハンドラをhookしているので
					// 何もしなくとも入力されると思われ。
				}
				else
				{
					IntPtr pkeys = SDL.SDL_GetKeyState(IntPtr.Zero);
					byte[] keys = new byte[ButtonNum];
					Marshal.Copy(pkeys, keys, 0, keys.Length);
					for (int i = 0; i < keys.Length; i++)
					{
						buttonState[flip][i] = keys[i] != 0;
					}
				}
				 */
				// あかんこの方法ではうまいこといかへん。
				CheckKeyInWindows();
			}
		}

		private void Init()
		{
			buttonState = new bool[2][];
			for (int i = 0; i < 2; ++i)
			{
				buttonState[i] = new bool[(int)KeyCode.LAST];
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public KeyBoardInput()
		{
			Init();
		}

	/*
		/// <summary>
		/// Windowsプラットフォームの場合は、UserControlを渡して、そのUserControlに
		/// 対するキー入力を取得できる
		/// </summary>
		/// <param name="control"></param>
		public KeyBoardInput(global::System.Windows.Forms.UserControl control)
		{
			Init();
			userControl = control;
			userControl.KeyDown+=
				new global::System.Windows.Forms.KeyEventHandler(KeyDown);
			userControl.KeyUp+=
				new global::System.Windows.Forms.KeyEventHandler(KeyUp);
		}
		private global::System.Windows.Forms.UserControl userControl = null;

		/// <summary>
		/// Windowsプラットフォームの場合は、Formを渡して、そのフォームに
		/// 対するキー入力を取得できる
		/// cf.FormUpdate
		/// </summary>
		public KeyBoardInput(global::System.Windows.Forms.Form form)
		{
			Init();
			userForm = form;

			// このformに付随するコントロールすべてのkey eventをhookする必要あり？
			userForm.KeyDown+=
				new global::System.Windows.Forms.KeyEventHandler(KeyDown);
			userForm.KeyUp +=
				new global::System.Windows.Forms.KeyEventHandler(KeyUp);
		}

		/// <summary>
		/// フォームからキーを入力しようとした場合、その下にControlが乗っていると
		/// そのControlに食われてしまう。そこで、そこに乗っているControlすべての
		/// KeyDown,KeyUpをhookしなければならない。
		/// 
		/// また、フォームにControlを加えたときも同様で、
		/// そこに乗っているControlの内容の
		/// </summary>
		public void FormUpdate()
		{

		}

		private global::System.Windows.Forms.Form userForm = null;

		// keyの押し下げ/押し上げをhookする。
		private void KeyDown(object sender, global::System.Windows.Forms.KeyEventArgs e){
			OnKey(e, true);
		}
		private void KeyUp(object sender, global::System.Windows.Forms.KeyEventArgs e)
		{
			OnKey(e, false);
		}

		private void OnKey(global::System.Windows.Forms.KeyEventArgs e,bool press)
		{
			int f = 1-flip;

			// すべてのキーに対するmappingを行なわなくてはならない。(´ω`)
			switch (e.KeyCode)
			{
				case Keys.A: buttonState[f][(int)KeyCode.a] = press; break;
				case Keys.B: buttonState[f][(int)KeyCode.b] = press; break;
				case Keys.C: buttonState[f][(int)KeyCode.c] = press; break;
			}
		}
	*/
		// あかん、この方法ではうまいこといかへん。

		// 仕方ない。全部のキーコードスキャンしよか..(´ω`)

		[DllImport("user32")]
		public static extern short GetAsyncKeyState(int nVirtKey);

		public void CheckKeyInWindows()
		{
			for (int i = 0; i < 10; ++i)
			{
				//--  0x30～0x39  メインキーボード 0～9  
				buttonState[flip][(int)KeyCode.KEY0 + i]
					= (GetAsyncKeyState(0x30 + i) & 0x8000) != 0;

				// -- NumPadの0～9
				// VK_NUMPAD0==0x60
				buttonState[flip][(int)KeyCode.KP0 + i]
					= (GetAsyncKeyState(0x60 + i) & 0x8000) != 0;
			}
			for (int i = 0; i < 26; ++i)
			{
				//--  0x41～0x5A  文字キー A から Z  
				buttonState[flip][(int)KeyCode.a + i]
					= (GetAsyncKeyState(0x41 + i) & 0x8000) != 0;
			}
			// 全部のキーをサポートしても仕方がないのであとは主要キーのみにする

			// カーソル関連とNumPadの+,-,*,/
			buttonState[flip][(int)KeyCode.UP] = (GetAsyncKeyState((int)VirtualKeyStates.VK_UP) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.DOWN] = (GetAsyncKeyState((int)VirtualKeyStates.VK_DOWN) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.RIGHT] = (GetAsyncKeyState((int)VirtualKeyStates.VK_RIGHT) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.LEFT] = (GetAsyncKeyState((int)VirtualKeyStates.VK_LEFT) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.KP_PLUS] = (GetAsyncKeyState((int)VirtualKeyStates.VK_ADD) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.KP_MINUS] = (GetAsyncKeyState((int)VirtualKeyStates.VK_SUBTRACT) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.KP_MULTIPLY] = (GetAsyncKeyState((int)VirtualKeyStates.VK_MULTIPLY) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.KP_DIVIDE] = (GetAsyncKeyState((int)VirtualKeyStates.VK_DIVIDE) & 0x8000) != 0;

			// あとはspace,enter,tab,alt,shift,ctrl,escぐらいでいいか。
			buttonState[flip][(int)KeyCode.SPACE] = (GetAsyncKeyState((int)VirtualKeyStates.VK_SPACE) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.RETURN] = (GetAsyncKeyState((int)VirtualKeyStates.VK_RETURN) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.TAB] = (GetAsyncKeyState((int)VirtualKeyStates.VK_TAB) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.LALT] = (GetAsyncKeyState((int)VirtualKeyStates.VK_LWIN) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.RALT] = (GetAsyncKeyState((int)VirtualKeyStates.VK_RWIN) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.LSHIFT] = (GetAsyncKeyState((int)VirtualKeyStates.VK_LSHIFT) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.RSHIFT] = (GetAsyncKeyState((int)VirtualKeyStates.VK_RSHIFT) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.LCTRL] = (GetAsyncKeyState((int)VirtualKeyStates.VK_LCONTROL) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.RCTRL] = (GetAsyncKeyState((int)VirtualKeyStates.VK_RCONTROL) & 0x8000) != 0;
			buttonState[flip][(int)KeyCode.ESCAPE] = (GetAsyncKeyState((int)VirtualKeyStates.VK_ESCAPE) & 0x8000) != 0;

		}

		#region VirtualKeyStates -- Windowsの仮想キーコード表
		private enum VirtualKeyStates : int
		{
			VK_LBUTTON = 0x01,
			VK_RBUTTON = 0x02,
			VK_CANCEL = 0x03,
			VK_MBUTTON = 0x04,
			//
			VK_XBUTTON1 = 0x05,
			VK_XBUTTON2 = 0x06,
			//
			VK_BACK = 0x08,
			VK_TAB = 0x09,
			//
			VK_CLEAR = 0x0C,
			VK_RETURN = 0x0D,
			//
			VK_SHIFT = 0x10,
			VK_CONTROL = 0x11,
			VK_MENU = 0x12,
			VK_PAUSE = 0x13,
			VK_CAPITAL = 0x14,
			//
			VK_KANA = 0x15,
			VK_HANGEUL = 0x15,  /* old name - should be here for compatibility */
			VK_HANGUL = 0x15,
			VK_JUNJA = 0x17,
			VK_FINAL = 0x18,
			VK_HANJA = 0x19,
			VK_KANJI = 0x19,
			//
			VK_ESCAPE = 0x1B,
			//
			VK_CONVERT = 0x1C,
			VK_NONCONVERT = 0x1D,
			VK_ACCEPT = 0x1E,
			VK_MODECHANGE = 0x1F,
			//
			VK_SPACE = 0x20,
			VK_PRIOR = 0x21,
			VK_NEXT = 0x22,
			VK_END = 0x23,
			VK_HOME = 0x24,
			VK_LEFT = 0x25,
			VK_UP = 0x26,
			VK_RIGHT = 0x27,
			VK_DOWN = 0x28,
			VK_SELECT = 0x29,
			VK_PRINT = 0x2A,
			VK_EXECUTE = 0x2B,
			VK_SNAPSHOT = 0x2C,
			VK_INSERT = 0x2D,
			VK_DELETE = 0x2E,
			VK_HELP = 0x2F,
			//
			VK_LWIN = 0x5B,
			VK_RWIN = 0x5C,
			VK_APPS = 0x5D,
			//
			VK_SLEEP = 0x5F,
			//
			VK_NUMPAD0 = 0x60,
			VK_NUMPAD1 = 0x61,
			VK_NUMPAD2 = 0x62,
			VK_NUMPAD3 = 0x63,
			VK_NUMPAD4 = 0x64,
			VK_NUMPAD5 = 0x65,
			VK_NUMPAD6 = 0x66,
			VK_NUMPAD7 = 0x67,
			VK_NUMPAD8 = 0x68,
			VK_NUMPAD9 = 0x69,
			VK_MULTIPLY = 0x6A,
			VK_ADD = 0x6B,
			VK_SEPARATOR = 0x6C,
			VK_SUBTRACT = 0x6D,
			VK_DECIMAL = 0x6E,
			VK_DIVIDE = 0x6F,
			VK_F1 = 0x70,
			VK_F2 = 0x71,
			VK_F3 = 0x72,
			VK_F4 = 0x73,
			VK_F5 = 0x74,
			VK_F6 = 0x75,
			VK_F7 = 0x76,
			VK_F8 = 0x77,
			VK_F9 = 0x78,
			VK_F10 = 0x79,
			VK_F11 = 0x7A,
			VK_F12 = 0x7B,
			VK_F13 = 0x7C,
			VK_F14 = 0x7D,
			VK_F15 = 0x7E,
			VK_F16 = 0x7F,
			VK_F17 = 0x80,
			VK_F18 = 0x81,
			VK_F19 = 0x82,
			VK_F20 = 0x83,
			VK_F21 = 0x84,
			VK_F22 = 0x85,
			VK_F23 = 0x86,
			VK_F24 = 0x87,
			//
			VK_NUMLOCK = 0x90,
			VK_SCROLL = 0x91,
			//
			VK_OEM_NEC_EQUAL = 0x92,   // '=' key on numpad
			//
			VK_OEM_FJ_JISHO = 0x92,   // 'Dictionary' key
			VK_OEM_FJ_MASSHOU = 0x93,   // 'Unregister word' key
			VK_OEM_FJ_TOUROKU = 0x94,   // 'Register word' key
			VK_OEM_FJ_LOYA = 0x95,   // 'Left OYAYUBI' key
			VK_OEM_FJ_ROYA = 0x96,   // 'Right OYAYUBI' key
			//
			VK_LSHIFT = 0xA0,
			VK_RSHIFT = 0xA1,
			VK_LCONTROL = 0xA2,
			VK_RCONTROL = 0xA3,
			VK_LMENU = 0xA4,
			VK_RMENU = 0xA5,
			//
			VK_BROWSER_BACK = 0xA6,
			VK_BROWSER_FORWARD = 0xA7,
			VK_BROWSER_REFRESH = 0xA8,
			VK_BROWSER_STOP = 0xA9,
			VK_BROWSER_SEARCH = 0xAA,
			VK_BROWSER_FAVORITES = 0xAB,
			VK_BROWSER_HOME = 0xAC,
			//
			VK_VOLUME_MUTE = 0xAD,
			VK_VOLUME_DOWN = 0xAE,
			VK_VOLUME_UP = 0xAF,
			VK_MEDIA_NEXT_TRACK = 0xB0,
			VK_MEDIA_PREV_TRACK = 0xB1,
			VK_MEDIA_STOP = 0xB2,
			VK_MEDIA_PLAY_PAUSE = 0xB3,
			VK_LAUNCH_MAIL = 0xB4,
			VK_LAUNCH_MEDIA_SELECT = 0xB5,
			VK_LAUNCH_APP1 = 0xB6,
			VK_LAUNCH_APP2 = 0xB7,
			//
			VK_OEM_1 = 0xBA,   // ';:' for US
			VK_OEM_PLUS = 0xBB,   // '+' any country
			VK_OEM_COMMA = 0xBC,   // ',' any country
			VK_OEM_MINUS = 0xBD,   // '-' any country
			VK_OEM_PERIOD = 0xBE,   // '.' any country
			VK_OEM_2 = 0xBF,   // '/?' for US
			VK_OEM_3 = 0xC0,   // '`~' for US
			//
			VK_OEM_4 = 0xDB,  //  '[{' for US
			VK_OEM_5 = 0xDC,  //  '\|' for US
			VK_OEM_6 = 0xDD,  //  ']}' for US
			VK_OEM_7 = 0xDE,  //  ''"' for US
			VK_OEM_8 = 0xDF,
			//
			VK_OEM_AX = 0xE1,  //  'AX' key on Japanese AX kbd
			VK_OEM_102 = 0xE2,  //  "<>" or "\|" on RT 102-key kbd.
			VK_ICO_HELP = 0xE3,  //  Help key on ICO
			VK_ICO_00 = 0xE4,  //  00 key on ICO
			//
			VK_PROCESSKEY = 0xE5,
			//
			VK_ICO_CLEAR = 0xE6,
			//
			VK_PACKET = 0xE7,
			//
			VK_OEM_RESET = 0xE9,
			VK_OEM_JUMP = 0xEA,
			VK_OEM_PA1 = 0xEB,
			VK_OEM_PA2 = 0xEC,
			VK_OEM_PA3 = 0xED,
			VK_OEM_WSCTRL = 0xEE,
			VK_OEM_CUSEL = 0xEF,
			VK_OEM_ATTN = 0xF0,
			VK_OEM_FINISH = 0xF1,
			VK_OEM_COPY = 0xF2,
			VK_OEM_AUTO = 0xF3,
			VK_OEM_ENLW = 0xF4,
			VK_OEM_BACKTAB = 0xF5,
			//
			VK_ATTN = 0xF6,
			VK_CRSEL = 0xF7,
			VK_EXSEL = 0xF8,
			VK_EREOF = 0xF9,
			VK_PLAY = 0xFA,
			VK_ZOOM = 0xFB,
			VK_NONAME = 0xFC,
			VK_PA1 = 0xFD,
			VK_OEM_CLEAR = 0xFE
		}
		#endregion


		/// <summary>
		/// 前回の入力状態と今回の入力状態。
		/// </summary>
		private bool[][]	buttonState;
		/// <summary>
		/// 前回の入力状態と今回の入力状態をflipさせて使う。flip={0,1}
		/// </summary>
		private int flip;
	}

	/// <summary>
	/// KeyBoardInputで用いるキースキャンコード
	/// </summary>
	public enum KeyCode : int {
		/* The keyboard syms have been cleverly chosen to map to ASCII */
		/// <summary>
		/// 
		/// </summary>
		UNKNOWN		= 0,
		/// <summary>
		/// 
		/// </summary>
		FIRST		= 0,
		/// <summary>
		/// 
		/// </summary>
		BACKSPACE		= 8,
		/// <summary>
		/// 
		/// </summary>
		TAB		= 9,
		/// <summary>
		/// 
		/// </summary>
		CLEAR		= 12,
		/// <summary>
		/// 
		/// </summary>
		RETURN		= 13,
		/// <summary>
		/// 
		/// </summary>
		PAUSE		= 19,
		/// <summary>
		/// 
		/// </summary>
		ESCAPE		= 27,
		/// <summary>
		/// 
		/// </summary>
		SPACE		= 32,
		/// <summary>
		/// 
		/// </summary>
		EXCLAIM		= 33,
		/// <summary>
		/// 
		/// </summary>
		QUOTEDBL		= 34,
		/// <summary>
		/// 
		/// </summary>
		HASH		= 35,
		/// <summary>
		/// 
		/// </summary>
		DOLLAR		= 36,
		/// <summary>
		/// 
		/// </summary>
		AMPERSAND		= 38,
		/// <summary>
		/// 
		/// </summary>
		QUOTE		= 39,
		/// <summary>
		/// 
		/// </summary>
		LEFTPAREN		= 40,
		/// <summary>
		/// 
		/// </summary>
		RIGHTPAREN		= 41,
		/// <summary>
		/// 
		/// </summary>
		ASTERISK		= 42,
		/// <summary>
		/// 
		/// </summary>
		PLUS		= 43,
		/// <summary>
		/// 
		/// </summary>
		COMMA		= 44,
		/// <summary>
		/// 
		/// </summary>
		MINUS		= 45,
		/// <summary>
		/// 
		/// </summary>
		PERIOD		= 46,
		/// <summary>
		/// 
		/// </summary>
		SLASH		= 47,
		/// <summary>
		/// 
		/// </summary>
		KEY0			= 48,
		/// <summary>
		/// 
		/// </summary>
		KEY1			= 49,
		/// <summary>
		/// 
		/// </summary>
		KEY2			= 50,
		/// <summary>
		/// 
		/// </summary>
		KEY3			= 51,
		/// <summary>
		/// 
		/// </summary>
		KEY4			= 52,
		/// <summary>
		/// 
		/// </summary>
		KEY5			= 53,
		/// <summary>
		/// 
		/// </summary>
		KEY6			= 54,
		/// <summary>
		/// 
		/// </summary>
		KEY7			= 55,
		/// <summary>
		/// 
		/// </summary>
		KEY8			= 56,
		/// <summary>
		/// 
		/// </summary>
		KEY9			= 57,
		/// <summary>
		/// 
		/// </summary>
		COLON		= 58,
		/// <summary>
		/// 
		/// </summary>
		SEMICOLON		= 59,
		/// <summary>
		/// 
		/// </summary>
		LESS		= 60,
		/// <summary>
		/// 
		/// </summary>
		EQUAL		= 61,
		/// <summary>
		/// 
		/// </summary>
		GREATER		= 62,
		/// <summary>
		/// 
		/// </summary>
		QUESTION		= 63,
		/// <summary>
		/// 
		/// </summary>
		AT			= 64,
		/* 
		   Skip uppercase letters
		 */
		/// <summary>
		/// 
		/// </summary>
		LEFTBRACKET	= 91,
		/// <summary>
		/// 
		/// </summary>
		BACKSLASH		= 92,
		/// <summary>
		/// 
		/// </summary>
		RIGHTBRACKET	= 93,
		/// <summary>
		/// 
		/// </summary>
		CARET		= 94,
		/// <summary>
		/// 
		/// </summary>
		UNDERSCORE		= 95,
		/// <summary>
		/// 
		/// </summary>
		BACKQUOTE		= 96,
		/// <summary>
		/// 
		/// </summary>
		a			= 97,
		/// <summary>
		/// 
		/// </summary>
		b			= 98,
		/// <summary>
		/// 
		/// </summary>
		c			= 99,
		/// <summary>
		/// 
		/// </summary>
		d			= 100,
		/// <summary>
		/// 
		/// </summary>
		e			= 101,
		/// <summary>
		/// 
		/// </summary>
		f			= 102,
		/// <summary>
		/// 
		/// </summary>
		g			= 103,
		/// <summary>
		/// 
		/// </summary>
		h			= 104,
		/// <summary>
		/// 
		/// </summary>
		i			= 105,
		/// <summary>
		/// 
		/// </summary>
		j			= 106,
		/// <summary>
		/// 
		/// </summary>
		k			= 107,
		/// <summary>
		/// 
		/// </summary>
		l			= 108,
		/// <summary>
		/// 
		/// </summary>
		m			= 109,
		/// <summary>
		/// 
		/// </summary>
		n			= 110,
		/// <summary>
		/// 
		/// </summary>
		o			= 111,
		/// <summary>
		/// 
		/// </summary>
		p			= 112,
		/// <summary>
		/// 
		/// </summary>
		q			= 113,
		/// <summary>
		/// 
		/// </summary>
		r			= 114,
		/// <summary>
		/// 
		/// </summary>
		s			= 115,
		/// <summary>
		/// 
		/// </summary>
		t			= 116,
		/// <summary>
		/// 
		/// </summary>
		u			= 117,
		/// <summary>
		/// 
		/// </summary>
		v			= 118,
		/// <summary>
		/// 
		/// </summary>
		w			= 119,
		/// <summary>
		/// 
		/// </summary>
		x			= 120,
		/// <summary>
		/// 
		/// </summary>
		y			= 121,
		/// <summary>
		/// 
		/// </summary>
		z			= 122,
		/// <summary>
		/// 
		/// </summary>
		DELETE		= 127,
		/* End of ASCII mapped keysyms */

		/* International keyboard syms */
		/// <summary>
		/// 
		/// </summary>
		WORLD_0		= 160,		/* 0xA0 */
		/// <summary>
		/// 
		/// </summary>
		WORLD_1		= 161,
		/// <summary>
		/// 
		/// </summary>
		WORLD_2		= 162,
		/// <summary>
		/// 
		/// </summary>
		WORLD_3		= 163,
		/// <summary>
		/// 
		/// </summary>
		WORLD_4		= 164,
		/// <summary>
		/// 
		/// </summary>
		WORLD_5		= 165,
		/// <summary>
		/// 
		/// </summary>
		WORLD_6		= 166,
		/// <summary>
		/// 
		/// </summary>
		WORLD_7		= 167,
		/// <summary>
		/// 
		/// </summary>
		WORLD_8		= 168,
		/// <summary>
		/// 
		/// </summary>
		WORLD_9		= 169,
		/// <summary>
		/// 
		/// </summary>
		WORLD_10		= 170,
		/// <summary>
		/// 
		/// </summary>
		WORLD_11		= 171,
		/// <summary>
		/// 
		/// </summary>
		WORLD_12		= 172,
		/// <summary>
		/// 
		/// </summary>
		WORLD_13		= 173,
		/// <summary>
		/// 
		/// </summary>
		WORLD_14		= 174,
		/// <summary>
		/// 
		/// </summary>
		WORLD_15		= 175,
		/// <summary>
		/// 
		/// </summary>
		WORLD_16		= 176,
		/// <summary>
		/// 
		/// </summary>
		WORLD_17		= 177,
		/// <summary>
		/// 
		/// </summary>
		WORLD_18		= 178,
		/// <summary>
		/// 
		/// </summary>
		WORLD_19		= 179,
		/// <summary>
		/// 
		/// </summary>
		WORLD_20		= 180,
		/// <summary>
		/// 
		/// </summary>
		WORLD_21		= 181,
		/// <summary>
		/// 
		/// </summary>
		WORLD_22		= 182,
		/// <summary>
		/// 
		/// </summary>
		WORLD_23		= 183,
		/// <summary>
		/// 
		/// </summary>
		WORLD_24		= 184,
		/// <summary>
		/// 
		/// </summary>
		WORLD_25		= 185,
		/// <summary>
		/// 
		/// </summary>
		WORLD_26		= 186,
		/// <summary>
		/// 
		/// </summary>
		WORLD_27		= 187,
		/// <summary>
		/// 
		/// </summary>
		WORLD_28		= 188,
		/// <summary>
		/// 
		/// </summary>
		WORLD_29		= 189,
		/// <summary>
		/// 
		/// </summary>
		WORLD_30		= 190,
		/// <summary>
		/// 
		/// </summary>
		WORLD_31		= 191,
		/// <summary>
		/// 
		/// </summary>
		WORLD_32		= 192,
		/// <summary>
		/// 
		/// </summary>
		WORLD_33		= 193,
		/// <summary>
		/// 
		/// </summary>
		WORLD_34		= 194,
		/// <summary>
		/// 
		/// </summary>
		WORLD_35		= 195,
		/// <summary>
		/// 
		/// </summary>
		WORLD_36		= 196,
		/// <summary>
		/// 
		/// </summary>
		WORLD_37		= 197,
		/// <summary>
		/// 
		/// </summary>
		WORLD_38		= 198,
		/// <summary>
		/// 
		/// </summary>
		WORLD_39		= 199,
		/// <summary>
		/// 
		/// </summary>
		WORLD_40		= 200,
		/// <summary>
		/// 
		/// </summary>
		WORLD_41		= 201,
		/// <summary>
		/// 
		/// </summary>
		WORLD_42		= 202,
		/// <summary>
		/// 
		/// </summary>
		WORLD_43		= 203,
		/// <summary>
		/// 
		/// </summary>
		WORLD_44		= 204,
		/// <summary>
		/// 
		/// </summary>
		WORLD_45		= 205,
		/// <summary>
		/// 
		/// </summary>
		WORLD_46		= 206,
		/// <summary>
		/// 
		/// </summary>
		WORLD_47		= 207,
		/// <summary>
		/// 
		/// </summary>
		WORLD_48		= 208,
		/// <summary>
		/// 
		/// </summary>
		WORLD_49		= 209,
		/// <summary>
		/// 
		/// </summary>
		WORLD_50		= 210,
		/// <summary>
		/// 
		/// </summary>
		WORLD_51		= 211,
		/// <summary>
		/// 
		/// </summary>
		WORLD_52		= 212,
		/// <summary>
		/// 
		/// </summary>
		WORLD_53		= 213,
		/// <summary>
		/// 
		/// </summary>
		WORLD_54		= 214,
		/// <summary>
		/// 
		/// </summary>
		WORLD_55		= 215,
		/// <summary>
		/// 
		/// </summary>
		WORLD_56		= 216,
		/// <summary>
		/// 
		/// </summary>
		WORLD_57		= 217,
		/// <summary>
		/// 
		/// </summary>
		WORLD_58		= 218,
		/// <summary>
		/// 
		/// </summary>
		WORLD_59		= 219,
		/// <summary>
		/// 
		/// </summary>
		WORLD_60		= 220,
		/// <summary>
		/// 
		/// </summary>
		WORLD_61		= 221,
		/// <summary>
		/// 
		/// </summary>
		WORLD_62		= 222,
		/// <summary>
		/// 
		/// </summary>
		WORLD_63		= 223,
		/// <summary>
		/// 
		/// </summary>
		WORLD_64		= 224,
		/// <summary>
		/// 
		/// </summary>
		WORLD_65		= 225,
		/// <summary>
		/// 
		/// </summary>
		WORLD_66		= 226,
		/// <summary>
		/// 
		/// </summary>
		WORLD_67		= 227,
		/// <summary>
		/// 
		/// </summary>
		WORLD_68		= 228,
		/// <summary>
		/// 
		/// </summary>
		WORLD_69		= 229,
		/// <summary>
		/// 
		/// </summary>
		WORLD_70		= 230,
		/// <summary>
		/// 
		/// </summary>
		WORLD_71		= 231,
		/// <summary>
		/// 
		/// </summary>
		WORLD_72		= 232,
		/// <summary>
		/// 
		/// </summary>
		WORLD_73		= 233,
		/// <summary>
		/// 
		/// </summary>
		WORLD_74		= 234,
		/// <summary>
		/// 
		/// </summary>
		WORLD_75		= 235,
		/// <summary>
		/// 
		/// </summary>
		WORLD_76		= 236,
		/// <summary>
		/// 
		/// </summary>
		WORLD_77		= 237,
		/// <summary>
		/// 
		/// </summary>
		WORLD_78		= 238,
		/// <summary>
		/// 
		/// </summary>
		WORLD_79		= 239,
		/// <summary>
		/// 
		/// </summary>
		WORLD_80		= 240,
		/// <summary>
		/// 
		/// </summary>
		WORLD_81		= 241,
		/// <summary>
		/// 
		/// </summary>
		WORLD_82		= 242,
		/// <summary>
		/// 
		/// </summary>
		WORLD_83		= 243,
		/// <summary>
		/// 
		/// </summary>
		WORLD_84		= 244,
		/// <summary>
		/// 
		/// </summary>
		WORLD_85		= 245,
		/// <summary>
		/// 
		/// </summary>
		WORLD_86		= 246,
		/// <summary>
		/// 
		/// </summary>
		WORLD_87		= 247,
		/// <summary>
		/// 
		/// </summary>
		WORLD_88		= 248,
		/// <summary>
		/// 
		/// </summary>
		WORLD_89		= 249,
		/// <summary>
		/// 
		/// </summary>
		WORLD_90		= 250,
		/// <summary>
		/// 
		/// </summary>
		WORLD_91		= 251,
		/// <summary>
		/// 
		/// </summary>
		WORLD_92		= 252,
		/// <summary>
		/// 
		/// </summary>
		WORLD_93		= 253,
		/// <summary>
		/// 
		/// </summary>
		WORLD_94		= 254,
		/// <summary>
		/// 
		/// </summary>
		WORLD_95		= 255,		/* 0xFF */

		/* Numeric keypad */
		/// <summary>
		/// 
		/// </summary>
		KP0		= 256,
		/// <summary>
		/// 
		/// </summary>
		KP1		= 257,
		/// <summary>
		/// 
		/// </summary>
		KP2		= 258,
		/// <summary>
		/// 
		/// </summary>
		KP3		= 259,
		/// <summary>
		/// 
		/// </summary>
		KP4		= 260,
		/// <summary>
		/// 
		/// </summary>
		KP5		= 261,
		/// <summary>
		/// 
		/// </summary>
		KP6		= 262,
		/// <summary>
		/// 
		/// </summary>
		KP7		= 263,
		/// <summary>
		/// 
		/// </summary>
		KP8		= 264,
		/// <summary>
		/// 
		/// </summary>
		KP9		= 265,
		/// <summary>
		/// 
		/// </summary>
		KP_PERIOD		= 266,
		/// <summary>
		/// 
		/// </summary>
		KP_DIVIDE		= 267,
		/// <summary>
		/// 
		/// </summary>
		KP_MULTIPLY	= 268,
		/// <summary>
		/// 
		/// </summary>
		KP_MINUS		= 269,
		/// <summary>
		/// 
		/// </summary>
		KP_PLUS		= 270,
		/// <summary>
		/// 
		/// </summary>
		KP_ENTER		= 271,
		/// <summary>
		/// 
		/// </summary>
		KP_EQUALS		= 272,

		/* Arrows + Home/End pad */
		/// <summary>
		/// 
		/// </summary>
		UP			= 273,
		/// <summary>
		/// 
		/// </summary>
		DOWN		= 274,
		/// <summary>
		/// 
		/// </summary>
		RIGHT		= 275,
		/// <summary>
		/// 
		/// </summary>
		LEFT		= 276,
		/// <summary>
		/// 
		/// </summary>
		INSERT		= 277,
		/// <summary>
		/// 
		/// </summary>
		HOME		= 278,
		/// <summary>
		/// 
		/// </summary>
		END		= 279,
		/// <summary>
		/// 
		/// </summary>
		PAGEUP		= 280,
		/// <summary>
		/// 
		/// </summary>
		PAGEDOWN		= 281,

		/* Function keys */
		/// <summary>
		/// 
		/// </summary>
		F1			= 282,
		/// <summary>
		/// 
		/// </summary>
		F2			= 283,
		/// <summary>
		/// 
		/// </summary>
		F3			= 284,
		/// <summary>
		/// 
		/// </summary>
		F4			= 285,
		/// <summary>
		/// 
		/// </summary>
		F5			= 286,
		/// <summary>
		/// 
		/// </summary>
		F6			= 287,
		/// <summary>
		/// 
		/// </summary>
		F7			= 288,
		/// <summary>
		/// 
		/// </summary>
		F8			= 289,
		/// <summary>
		/// 
		/// </summary>
		F9			= 290,
		/// <summary>
		/// 
		/// </summary>
		F10		= 291,
		/// <summary>
		/// 
		/// </summary>
		F11		= 292,
		/// <summary>
		/// 
		/// </summary>
		F12		= 293,
		/// <summary>
		/// 
		/// </summary>
		F13		= 294,
		/// <summary>
		/// 
		/// </summary>
		F14		= 295,
		/// <summary>
		/// 
		/// </summary>
		F15		= 296,

		/* Key state modifier keys */
		/// <summary>
		/// 
		/// </summary>
		NUMLOCK		= 300,
		/// <summary>
		/// 
		/// </summary>
		CAPSLOCK		= 301,
		/// <summary>
		/// 
		/// </summary>
		SCROLLOCK		= 302,
		/// <summary>
		/// 
		/// </summary>
		RSHIFT		= 303,
		/// <summary>
		/// 
		/// </summary>
		LSHIFT		= 304,
		/// <summary>
		/// 
		/// </summary>
		RCTRL		= 305,
		/// <summary>
		/// 
		/// </summary>
		LCTRL		= 306,
		/// <summary>
		/// 
		/// </summary>
		RALT		= 307,
		/// <summary>
		/// 
		/// </summary>
		LALT		= 308,
		/// <summary>
		/// 
		/// </summary>
		RMETA		= 309,
		/// <summary>
		/// 
		/// </summary>
		LMETA		= 310,
		/// <summary>
		/// Left "Windows" key
		/// </summary>
		LSUPER		= 311,
		/// <summary>
		/// Right "Windows" key
		/// </summary>
		RSUPER		= 312,
		/// <summary>
		/// "Alt Gr" key
		/// </summary>
		MODE		= 313,
		/// <summary>
		/// Multi-key compose key
		/// </summary>
		COMPOSE		= 314,

		/* Miscellaneous function keys */
		/// <summary>
		/// 
		/// </summary>
		HELP		= 315,
		/// <summary>
		/// 
		/// </summary>
		PRINT		= 316,
		/// <summary>
		/// 
		/// </summary>
		SYSREQ		= 317,
		/// <summary>
		/// 
		/// </summary>
		BREAK		= 318,
		/// <summary>
		/// 
		/// </summary>
		MENU		= 319,
		/// <summary>
		/// Power Macintosh power key
		/// </summary>
		POWER		= 320,
		/// <summary>
		/// Some european keyboards
		/// </summary>
		EURO		= 321,
		/// <summary>
		/// Atari keyboard has Undo
		/// </summary>
		UNDO		= 322,

		/* Add any other keys here */
		/// <summary>
		/// 
		/// </summary>
		LAST
	}
}
