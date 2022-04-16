using System;
using System.Runtime.InteropServices;
using Sdl;
using Yanesdk.Draw;
using System.Windows.Forms;
using Yanesdk.Ytl;

namespace Yanesdk.Input
{

	/// <summary>
	///	マウス入力クラス。
	/// 	/**
	/// 		ウィンドゥが生きていて、かつ、
	/// 		メッセージポンプが動いている状態で呼び出されなくてはならない。
	/// 
	/// 	例)
	/// 	<PRE>
	/// 		SDLWindow window = new SDLWindow();
	/// 		window.setVideoMode(640,480,0);
	/// 
	/// 		MouseInput mouse = new MouseInput();
	/// 		while (!GameFrame.pollEvent()){
	/// 			window.screen.clear();
	/// 			window.screen.Update();
	/// 
	/// 			int x,y;
	/// 			mouse.getPos(x,y);
	/// 
	/// 			bool a,b,c;
	/// 			a = mouse.isPress(MouseInput.Button.Left);
	/// 			b = mouse.isPress(MouseInput.Button.middle);
	/// 			c = mouse.isPress(MouseInput.Button.Right);
	/// 
	/// 			printf("%d %d : %d %d %d\n",x,y,a?1:0,b?1:0,c?1:0);
	/// 
	/// 			if (x &lt;10) 
	/// 				mouse.setPos(600,300);
	/// 
	/// 		}
	/// </PRE>
	/// 
	/// </summary>
	/* // test code
			int x, y; bool b1,b2;
			mouse.GetPos(out x,out y);
			b1 = mouse.IsPress(MouseInput.Button.Left);
			b2 = mouse.IsPress(MouseInput.Button.Right);
			label1.Text = x.ToString() + ":" + y.ToString() + ":" + b1.ToString()
				+ b2.ToString();
	
	*/
	public class MouseInput : IDisposable {

		public MouseInput() {
			Reset();
		}

		/// <summary>
		/// Windowsプラットフォームの場合は、Controlを渡して、そのUserControlに
		/// 対するマウス位置を取得できる
		/// </summary>
		/// <param name="control"></param>
		public MouseInput(global::System.Windows.Forms.Control control)
		{
			// Windows専用の機能
			if (Yanesdk.System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);

			Init(control , false);
		}

		/// <summary>
		/// Windowsプラットフォームの場合は、UserControlを渡して、そのUserControlに
		/// 対するマウス位置を取得できる
		/// </summary>
		/// <remarks>
		/// onlyOnControl == trueであれば、このControl上でマウスが押されたときしか
		/// このクラスはマウスボタンが押されたと認識しない。
		/// </remarks>
		/// <param name="control"></param>
		public MouseInput(global::System.Windows.Forms.Control control,bool onlyOnControl)
		{
			// Windows専用の機能
			if (Yanesdk.System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);

			Init(control , onlyOnControl);
		}
		private global::System.Windows.Forms.Control userControl = null;


		/// <summary>
		/// Windowsプラットフォームの場合は、Formを渡して、そのフォームに
		/// 対するマウス位置を取得できる
		/// </summary>
		public MouseInput(global::System.Windows.Forms.Form form)
		{
			Init(form , false);
		}
		/// <summary>
		/// Windowsプラットフォームの場合は、Formを渡して、そのフォームに
		/// 対するマウス位置を取得できる
		/// </summary>
		/// <remarks>
		/// onlyOnForm == trueであれば、このForm上でマウスが押されたときしか
		/// このクラスはマウスボタンが押されたと認識しない。
		/// </remarks>
		public MouseInput(global::System.Windows.Forms.Form form , bool onlyOnForm)
		{
			Init(form , onlyOnForm);
		}

		/// <summary>
		/// あとからforcusのあるcontrolを変更したいときに用いる
		/// </summary>
		/// <param name="form"></param>
		/// <param name="onlyOnControl"></param>
		public void Init(global::System.Windows.Forms.Control control , bool onlyOnControl)
		{
			Dispose();
			Reset();
			userControl = control;

			this.isClickOnlyOnForm = onlyOnControl;

			if ( onlyOnControl )
			{
				//  マウスカーソルが入力フォーカスのあるControl,Formの外にあるのかを
				//  判定するために入力をhookする。
				control.MouseEnter += new global::System.EventHandler(this.Activated);
				control.MouseLeave += new global::System.EventHandler(this.Deactivated);

				// とりあえずはこいつを初期値としよう
				isActivated = !IsMouseOutOfForm;
			}
			else
			{
				isActivated = true;
			}
		}

		/// <summary>
		/// あとからforcusのあるformを変更したいときに用いる
		/// </summary>
		/// <param name="form"></param>
		/// <param name="onlyOnForm"></param>
		public void Init(global::System.Windows.Forms.Form form , bool onlyOnForm)
		{
			Dispose();
			Reset();
			userForm = form;

			this.isClickOnlyOnForm = onlyOnForm;

			if ( onlyOnForm )
			{
				//  マウスカーソルが入力フォーカスのあるControl,Formの外にあるのかを
				//  判定するために入力をhookする。
				form.Activated += new global::System.EventHandler(this.Activated);
				form.Deactivate += new global::System.EventHandler(this.Deactivated);

				// とりあえずはこいつを初期値としよう
				isActivated = !IsMouseOutOfForm;
			}
			else
			{
				isActivated = true;
			}
		}

		// 現在、マウスがこのformの内側にあるのかないのかを判定するヘルパproperty
		private bool IsMouseOutOfForm
		{
			get
			{
				if ( isClickOnlyOnForm )
				{
					int x , y;
					GetPos(out x , out y);
					if ( userForm != null )
						return !(
							( 0 <= x ) && ( 0 <= y ) &&
							( x < userForm.Size.Width ) && ( y < userForm.Size.Height )
						);
					if ( userControl != null )
						return !(
							( 0 <= x ) && ( 0 <= y ) &&
							( x < userControl.Size.Width ) && ( y < userControl.Size.Height )
						);
				}

				return false;
			}
		}


		private global::System.Windows.Forms.Form userForm = null;

		private void Activated(object sender , EventArgs e)
		{
			isActivated = true;
		//	Console.WriteLine("Enter");
		}

		private void Deactivated(object sender , EventArgs e)
		{
			isActivated = false;
		//	Console.WriteLine("Leave");
		}
		private bool isActivated;

		/// <summary>
		/// Form/Control上でマウスが押されたときしか
		/// このクラスはマウスボタンが押されたと認識しないのか。
		/// </summary>
		private bool isClickOnlyOnForm = false;

		/*
		/// <summary>
		/// マウスカーソルが入力フォーカスのあるControl,Formの外にあるのか？
		/// </summary>
		/// <remarks>
		/// 外にある場合は、ボタン入力を無効にする
		/// </remarks>
		private bool isMouseOutOfForm = false;
		*/

		/// <summary>
		/// マウスカーソルを表示する
		/// </summary>
		public void Show() {

			if (Yanesdk.System.Platform.IsLinux)
			{
				SDL.SDL_ShowCursor((int)SDL.SDL_ENABLE);
			}
			else
			{
				if (show) return;
				if (userForm != null || userControl != null)
				{
					global::System.Windows.Forms.Cursor.Show();
				}
				else
				{
					SDL.SDL_ShowCursor((int)SDL.SDL_ENABLE);
				}
				show = true;
			}
		}

		private bool show=true; // マウスカーソルが表示なのか非表示なのか

		/// <summary>
		/// マウスカーソルを非表示にする
		/// </summary>
		public void Hide() {

			if (Yanesdk.System.Platform.IsLinux)
			{
				SDL.SDL_ShowCursor((int)SDL.SDL_DISABLE);
			}
			else
			{
				if (!show) return;
				if (userForm != null || userControl != null)
				{
					global::System.Windows.Forms.Cursor.Hide();
				}
				else
				{
					SDL.SDL_ShowCursor((int)SDL.SDL_DISABLE);
				}
				show = false;
			}
		}

		/// <summary>
		/// この範囲外にマウスカーソルがあるときはボタン入力を受け付けない。
		/// 
		/// コントロールをガードするのに使える。nullならばclipしない。
		/// </summary>
		public Rect ClipRect
		{
			get { return clipRect; }
			set { clipRect = value; }
		}
		private Rect clipRect;

		/// <summary>
		/// マウスの位置取得
		/// </summary>
		/// <remarks>
		///	マウスが画面外にあるときは最後に取得した値が返ります。
		///	すなわち、画面サイズが640×480ならば、xは0～639,yは0～479までの
		///	値しか返らないことが保証されます。
		/// 
		/// Updateが呼び出されるまで値は更新されません。
		/// </remarks>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void GetPos(out int x, out int y)
		{
			x = x_;
			y = y_;
		}

		/// <summary>
		/// マウス座標。Updateを呼び出すごとに更新される。
		/// </summary>
		private int x_, y_;

		private void innerGetPos(out int x,out int y) {

			if (Yanesdk.System.Platform.IsLinux)
			{
				SDL.GetMouseState(out x, out y);
			}
			else
			{
				try
				{
					if (userForm != null)
					{
						global::System.Drawing.Point point;
						try
						{
							point =
								userForm.PointToClient(
									global::System.Windows.Forms.Cursor.Position
							);
						}
						catch // これ、解放されたあとでメッセージ飛んでくること、ありうるのよね…(´ω`)
						{
							x = y = 0;
							return;
						}
						x = point.X;
						y = point.Y;

						if (x < 0)
							x = 0;
						if (y < 0)
							y = 0;
						if (x >= userForm.ClientSize.Width)
							x = userForm.ClientSize.Width - 1;
						if (y >= userForm.ClientSize.Height)
							y = userForm.ClientSize.Height - 1;
					}
					else if (userControl != null)
					{
						global::System.Drawing.Point point =
							userControl.PointToClient(
								global::System.Windows.Forms.Cursor.Position
						);
						x = point.X;
						y = point.Y;

						if (x < 0)
							x = 0;
						if (y < 0)
							y = 0;
						if (x >= userControl.ClientSize.Width)
							x = userControl.Size.Width - 1;
						if (y >= userControl.ClientSize.Height)
							y = userControl.Size.Height - 1;
					}
					else
					{
						SDL.GetMouseState(out x, out y);
					}
				}
				catch
				{	// 破棄されたコントロールにアクセスしたのかも知れない。
					x = 0;
					y = 0;
				}
			}
		}

		/// <summary>
		/// マウスの位置を設定
		/// </summary>
		/// <remarks>
		/// SDLの実装を用いる場合は、SDLのマウスの位置設定イベントが発生する。
		/// 
		/// ここで設定した値は、GetPosで取得される値として(Updateを呼び出さずとも)
		/// 即座に反映する。
		/// </remarks>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void SetPos(int x,int y) {

			if (Yanesdk.System.Platform.IsLinux)
			{
				SDL.SDL_WarpMouse((ushort)x, (ushort)y);
			}
			else
			{
				if (userForm != null)
				{
					global::System.Windows.Forms.Cursor.Position
						= userForm.PointToScreen(new global::System.Drawing.Point(x, y));
				}
				else if (userControl != null)
				{
					global::System.Windows.Forms.Cursor.Position
						= userControl.PointToScreen(new global::System.Drawing.Point(x, y));
				}
				else
				{
					SDL.SDL_WarpMouse((ushort)x, (ushort)y);
				}
			}
			x_ = x;
			y_ = y;
		}

		/// <summary>
		/// ボタンの押し下げ判定のための構造体
		/// </summary>
		public enum Button {
			Left   = 1,	//	左ボタン
			Middle = 2,	//	中ボタン
			Right  = 3,	//	右ボタン

			/// Windows2000以降でサポートされたボタンLinux環境では無効。
			XButton1 = 4, // XButton1
			XButton2 = 5, // XButton2
		}

		/// <summary>
		/// ボタンが押し下げられているかを判定する関数
		/// </summary>
		/// <remarks>
		/// Updateが呼び出されるまでここで取得される状態は更新されない。
		/// </remarks>
		/// <param name="e"></param>
		/// <returns></returns>
		public bool IsPress(Button e)
		{
			bool b;
			switch (e)
			{
				case Button.Left  : b = left;   break;
				case Button.Middle: b = middle; break;
				case Button.Right : b = right;  break;
				case Button.XButton1: b = xbutton1; break;
				case Button.XButton2: b = xbutton2; break;

				default:	// こんなマウス状態ありえないはずなんだが
					throw null;
			}
			return b;
		}

		/// <summary>
		/// ボタンが押し下げられているかを判定する関数
		/// </summary>
		/// <remarks>
		/// リアルタイム情報を取得する
		/// </remarks>
		/// <param name="e"></param>
		/// <returns></returns>
		private bool innerIsPress(Button e) {

			if ( clipRect != null )
			{
				int x , y;
				GetPos(out x , out y);
				if (! clipRect.IsIn(x , y) )
					return false;
			}

			if (Yanesdk.System.Platform.IsLinux)
			{
				int x, y;
				return (SDL.SDL_GetMouseState(out x, out y) & SDL.SDL_BUTTON((uint)e)) != 0;
			}
			else
			{
				if (userForm != null || userControl != null)
				{

					// マウスがフォームから離れている
					if (IsMouseOutOfForm || !isActivated)
						return false;

					switch (e)
					{
						case Button.Left:
							return (global::System.Windows.Forms.Control.MouseButtons
									& global::System.Windows.Forms.MouseButtons.Left) != 0;
						case Button.Middle:
							return (global::System.Windows.Forms.Control.MouseButtons
									& global::System.Windows.Forms.MouseButtons.Middle) != 0;
						case Button.Right:
							return (global::System.Windows.Forms.Control.MouseButtons
									& global::System.Windows.Forms.MouseButtons.Right) != 0;
						case Button.XButton1:
							return (global::System.Windows.Forms.Control.MouseButtons
									& global::System.Windows.Forms.MouseButtons.XButton1) != 0;
						case Button.XButton2:
							return (global::System.Windows.Forms.Control.MouseButtons
									& global::System.Windows.Forms.MouseButtons.XButton2) != 0;

					}
					return false;
				}
				else
				{
					int x, y;
					return (SDL.SDL_GetMouseState(out x, out y) & SDL.SDL_BUTTON((uint)e)) != 0;
				}
			}
		}

		/// <summary>
		/// 前々回のupdateの呼び出しのときにボタンが押されていて、
		/// 前回のupdate呼び出しのときにボタンが押されていれば
		/// このメソッドはtrueを返す。
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public bool IsPush(Button e)
		{
			switch (e)
			{
				case Button.Left:	return !last_left && left;
				case Button.Middle: return !last_middle && middle;
				case Button.Right:  return !last_right && right;
				case Button.XButton1: return !last_xbutton1 && xbutton1;
				case Button.XButton2: return !last_xbutton2 && xbutton2;
			}
			throw null; // never reached
		}

		/// <summary>
		/// 前々回のupdateの呼び出しのときにボタンが押されていて
		/// 前回のupdate呼び出しのときにボタンが押されていなければ
		/// このメソッドはtrueを返す。
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public bool IsLeave(Button e)
		{
			switch (e)
			{
				case Button.Left: return last_left && !left;
				case Button.Middle: return last_middle && !middle;
				case Button.Right: return last_right && !right;
				case Button.XButton1: return last_xbutton1 && !xbutton1;
				case Button.XButton2: return last_xbutton2 && !xbutton2;
			}
			throw null; // never reached
		}

		/// <summary>
		/// ボタン状態とマウス座標を更新する。
		/// isPush,isPressで返るボタンの状態やGetPosの値を取得するには
		/// 必ずフレーム毎にこのメンバを呼び出すこと。
		/// </summary>
		public void Update()
		{
			last_left	= left;
			last_middle	= middle;
			last_right	= right;

			left	= innerIsPress(Button.Left);
			middle	= innerIsPress(Button.Middle);
			right	= innerIsPress(Button.Right);

			// Linux環境では無効だが。
			last_xbutton1 = xbutton1;
			last_xbutton2 = xbutton2;

			xbutton1 = innerIsPress(Button.XButton1);
			xbutton2 = innerIsPress(Button.XButton2);

			innerGetPos(out x_, out y_);
		}

		/// <summary>
		/// 内部的に保持しているボタン状態をリセットする
		/// clip rectはクリアされない。
		/// 
		/// SetLogicalPos(0,0);も内部的に行なう。
		/// </summary>
		public void Reset()
		{
			left = false;
			right = false;
			middle = false;
			last_left = false;
			last_right = false;
			last_middle = false;

			// Linux環境では無効だが。
			xbutton1 = false;
			last_xbutton1 = false;
			xbutton2 = false;
			last_xbutton2 = false;

			SetLogicalPos(0, 0);
		}

		/// <summary>
		/// マウスカーソルの座標も次のUpdateを呼び出すまでは(x,y)がGetPosで戻るようになる。
		/// (カーソルは実際はそこには無いが)
		/// 
		/// 画面上にhoverさせると反応するボタン素材などを配置しているときに、
		/// 手前に他のアプリのウィンドウが来たときに他のアプリのウィンドゥごしに
		/// そのボタンが反応してしまうのを避けるため、論理座標をどこかに追いやりたいときに
		/// 使うと良い。
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void SetLogicalPos(int x, int y)
		{
			x_ = x;
			y_ = y;
		}

		/// <summary>
		/// 前回の状態を保持しておく。
		/// </summary>
		private bool left,last_left;
		private bool middle, last_middle;
		private bool right, last_right;
		private bool xbutton1, last_xbutton1;
		private bool xbutton2, last_xbutton2;

		public void Dispose()
		{
			if (Yanesdk.System.Platform.IsWindows)
			{

				if (isClickOnlyOnForm)
				{
					// Disposeを二重に呼び出されたときに2重解放になるのを防止する
					isClickOnlyOnForm = false;

					if (userForm != null)
					{
						userForm.Activated -= new global::System.EventHandler(this.Activated);
						userForm.Deactivate -= new global::System.EventHandler(this.Deactivated);
						userForm = null;
					}
					if (userControl != null)
					{
						userControl.MouseEnter -= new global::System.EventHandler(this.Activated);
						userControl.MouseLeave -= new global::System.EventHandler(this.Deactivated);
						userControl = null;
					}
				}
				else
				{
					userForm = null;
					userControl = null;
				}
			}
		}
	
	}
}
