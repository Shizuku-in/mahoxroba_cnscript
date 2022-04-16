using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// OpenGLを用いたコントロールクラス(のサンプル)
	/// Windows環境専用。
	/// </summary>
	/// <remarks>
	/// System.Windows.Forms.Controlから派生しているので
	/// Visual Studioのフォームデザイナからダイアログに貼り付けて使うことが出来る。
	/// 
	/// Visual Studioのフォームデザイナときどきバグっておかしくなるので、
	/// このコンポーネントが表示できないときは、このファイルだけプロジェクトから
	/// いったんはずして、そのあとにプロジェクトに追加すると良い。
	/// </remarks>
	public class Control2DGl : global::System.Windows.Forms.Control, IDisposable
	// This is the OpenGL Control.  It inherits from the System Control class and will
	// be added to the main form.
	{

		public Control2DGl()
		{
			// Windows専用の機能
			if (Yanesdk.System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkException(YanesdkResult.NotImplemented);

			window = new Win32Window2DGl(Handle);
		}

		public new void Dispose()
		{
			window.Dispose();
			base.Dispose();
		}

		// This method is called when the Size property of the control is changed
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (window != null)
			{
			//	window.Screen.UpdateView(Width, Height);
				window.Screen.UpdateView(ClientSize.Width, ClientSize.Height);
			}
		}

		/// <summary>
		/// このメソッドをoverrideして、backgroundへの描画を
		/// 潰しておかないと、画面がちらつく。
		/// 
		/// あるいは、
		///    SetStyle(ControlStyles.Opaque, true);
		/// とやっても良いが。
		/// </summary>
		/// <param name="pevent"></param>
		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			//            base.OnPaintBackground(pevent);
		}

		// This method is called when the control is redrawn
		protected override void OnPaint(PaintEventArgs eventArgs)
		{
			// Visual Studioのデザインモードのとき。
			if (DesignMode)
			{
				Screen2DGl scr = window.Screen;
				scr.Select();
				scr.SetClearColor(100, 200, 100);
				scr.Clear();
				scr.DrawString("Open GL", 5, 10, 10);
				scr.DrawString("Control", 5, 30, 10);
				scr.Update();
			}
			// 描画用のメソッドはここに書いて。
		}

		private Win32Window2DGl window;
	}
}
