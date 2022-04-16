using System;
using Yanesdk.Timer;
using Yanesdk.System; // StringConv

namespace Yanesdk.Draw
{
	/// <summary>
	/// FPSを画面に表示するクラス。
	/// </summary>
	/// <remarks>
	/// FpsTimerをセットしておけば、それを画面に表示してくれる。
	/// あくまでデバッグ用。もしくは、サンプル目的。
	///
	/// 自分用は、これを改造して使ってもらえれば良いと思う。
	/// </remarks>
	public class FpsLayer : IDrawLayer
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fpstimer_"></param>
		public FpsLayer(FpsTimer fpstimer_)
		{ fpstimer = fpstimer_; size = 25; }

		/// <summary>
		/// 
		/// </summary>
		public FpsLayer() { size = 25; }

		///	<summary>FpsTimerのsetter。</summary>
		/// <remarks>
		///	コンストラクタで渡しそびれた時のために。
		/// setterとgetter
		/// </remarks>
		public FpsTimer FpsTimer{
			set { fpstimer = value; }
			get { return fpstimer ;}
		}

		///	<summary>文字のサイズの設定/取得。</summary>
		/// <remarks>
		///	defaultは25。
		///	※ 色は、Screenに対してセットすればok。
		/// </remarks>
		public int FontSize
		{
			set { size = value; }
			get { return size; }
		}

		///	<summary>
		///	描画メソッド。
		///	Screen2DGlの線の太さを変更しているので注意。
		///	</summary>
		public void OnDraw(Screen2DGl screen,int x,int y) {
			if (fpstimer == null) return ;
			int fps = (int)fpstimer.Fps;
			int realfps = fpstimer.RealFpsInt;

			screen.SetLineWidth((int)((float)size)/6 + 1);

			// string txt = realfps.ToString("D6");


			string txt = StringConv.ToDecZeroSuppress(realfps,6) + "FPS";
			int r,g,b;
			screen.GetColor(out r, out g, out b);	// 色を保存
			screen.SetColor(0,120,0);
			screen.DrawString(txt,x,y,size);
			screen.SetColor(r,g,b);	// 色を復帰
		}

		/// <summary>
		/// fps timer。
		/// </summary>
		private FpsTimer fpstimer;
		/// <summary>
		/// 文字サイズ。
		/// </summary>
		private int size;
	}
}
