using System;
using OpenGl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 描画用のContext。描画する画面サイズ、比率、オフセット位置等が入っている。
	/// 描画はこれに基づいて行なわれるので、画面を揺らしたり、全体的に拡大縮小したい場合などには
	/// この構造体の値を書き換えれば良い。
	/// </summary>
	public class DrawContext
	{
		/// <summary>
		/// スクリーンのX方向のオフセット値を設定/取得する。
		/// </summary>
		/// <remarks>
		/// SDLWindow.setOffsetで設定される値は、視体積を変更するので
		/// クリップとは関係ないが、ここで変更されるoffsetは直接的に
		/// 描画に関係してくる。
		/// offsetX = 1.0 ならば、一画面分(rateX)描画時にオフセットが加わる。
		/// すなわち、(-640,-480)に描画したものが(0,0)に描画される
		/// </remarks>
		public float OffsetX
		{
			get { return offsetX;}
			set { offsetX = value; Update(); }
		}
		private float offsetX;

		/// <summary>
		/// スクリーンのY方向のオフセット値を設定/取得する。
		/// </summary>
		/// <remarks>
		/// SDLWindow.setOffsetで設定される値は、視体積を変更するので
		/// クリップとは関係ないが、ここで変更されるoffsetは直接的に
		/// 描画に関係してくる。
		/// offsetX = 1.0 ならば、一画面分(rateX)描画時にオフセットが加わる。
		/// すなわち、(-640,-480)に描画したものが(0,0)に描画される
		/// </remarks>
		public float OffsetY
		{
			get { return offsetY;}
			set { offsetY = value; Update(); }
		}
		private float offsetY;

		/// <summary>
		/// オフセットを設定する
		/// </summary>
		/// <param name="offsetX"></param>
		/// <param name="offsetY"></param>
		public void SetOffset(float offsetX, float offsetY)
		{
			this.offsetX = offsetX;
			this.offsetY = offsetY;
			Update();
		}

		/*
		/// <summary>
		/// クローンメソッド。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// データメンバのcopyは行なわない。
		/// あくまで同じ型のオブジェクトを生成するだけ。
		/// </remarks>
		public virtual DrawContext Clone() { return new DrawContext(); }
		*/

		/// <summary>
		/// 画面サイズの設定/取得
		/// </summary>
		public float ScreenSizeX
		{
			get { return screenSizeX; }
			set { screenSizeX = value; Update(); }
		}

		/// <summary>
		/// 画面サイズの設定/取得
		/// </summary>
		public float ScreenSizeY
		{
			get { return screenSizeY; }
			set { screenSizeY = value; Update(); }
		}
		private float screenSizeX;
		private float screenSizeY;

		/// <summary>
		/// 画面サイズの設定
		/// </summary>
		public void SetScreenSize(float x, float y)
		{
			screenSizeX = x;
			screenSizeY = y;
			Update();
		}

		/// <summary>
		/// クリップする矩形を指定する。
		/// </summary>
		/// <param name="rc"></param>
		/// <remarks>
		/// <para>
		/// ここで指定するRectの座標系は、
		/// (screenSizeX,screenSizeY)を基準とする。
		/// </para>
		/// <para>
		/// Rectの(Left,bottm)の点は含まない。
		/// </para>>
		/// <para>
		/// すなわち、 <c>screenSizeX = 640, screenSize Y = 480 </c>のときに
		/// (0,0,640,480)のRectをClip矩形として渡した場合、
		/// <c>Top = 0, Left = 0, Right = 1.0 , Bottom = 1.0</c>
		/// がclip位置として記録されることになる。
		/// </para>
		/// </remarks>
		public void SetClipRect(Rect rc)
		{
			Left   = rc.Left   / screenSizeX;
			Top    = rc.Top    / screenSizeY;
			Right  = rc.Right  / screenSizeX;
			Bottom = rc.Bottom / screenSizeY;
			Update();
		}

		/// <summary>
		/// SetClipRectで
		/// (0,0,1.0,1.0)のRECTを渡したのと同じ。
		/// </summary>
		public void SetClipRect() {
			Left = Top = 0.0f;
			Right = Bottom = 1.0f;
			Update();
		}

		/// <summary>
		/// クリップを有効/無効にする。
		/// </summary>
		public bool Clip
		{
			set { clip = value; Update(); }
			get { return clip; }
		}

		/// <summary>
		/// clipするときはこれがtrue。
		/// </summary>
		private bool clip;

		/// <summary>
		/// 描画時の倍率。
		/// </summary>
		private float rateX, rateY;

		/// <summary>
		/// 描画時の倍率の設定/取得
		/// </summary>
		/// <para>
		/// 640×480の画面を想定してコーディングしたものを
		/// 320×240の画面にうまく描画されるようにするためには
		/// <c>SetRate(0.5,0.5)</c>を行なえば良い。
		/// </para>
		/// <para>
		/// Update での計算式を見ればわかるように、
		/// offset , clipRect に関係してくる。
		/// </para>
		/// <para>
		/// defaultでは(1.0,1.0)
		/// </para>
		/// </remarks>
		public float RateX
		{
			get { return this.rateX;}
			set { this.rateX = value;}
		}

		/// <summary>
		/// 描画時の倍率の設定。
		/// </summary>
		/// <para>
		/// 640×480の画面を想定してコーディングしたものを
		/// 320×240の画面にうまく描画されるようにするためには
		/// <c>setRate(0.5,0.5)</c>を行なえば良い。
		/// </para>
		/// <para>
		/// Update での計算式を見ればわかるように、
		/// offset , clipRect に関係してくる。
		/// </para>
		/// <para>
		/// defaultでは(1.0,1.0)
		/// </para>
		/// </remarks>
		public float RateY
		{
			get { return rateY; }
			set { rateY = value; }
		}


		/// <summary>
		/// Update。
		/// </summary>
		/// <remarks>
		/// <para>
		/// 1.offsetの値を反映させる
		/// <code>
		/// offsetRX = offsetX * screenSizeX * rateX;
		/// offsetRY = offsetY * screenSizeY * rateY;
		/// </code>
		/// </para>
		/// 2.rectの値を反映させる
		/// <code>
		/// leftR = Left * screenSizeX * rateX;
		/// topR  = Top  * screenSizeY * rateY;
		/// rightR= Right * screenSizeX * rateX;
		/// bottomR= Bottom * screenSizeY * rateY;
		/// </code>
		/// <para>
		/// </para>
		/// </remarks>
		public virtual void Update() {
			myUpdate();
		}

		private void myUpdate()
		{
			float rx = screenSizeX * rateX;
			float ry = screenSizeY * rateY;

			OffsetRX = offsetX * rx;
			OffsetRY = offsetY * ry;

			LeftR = ( Left + offsetX ) * rx;
			TopR = ( Top + offsetY ) * ry;
			RightR = ( Right + offsetX ) * rx;
			BottomR = ( Bottom + offsetY ) * ry;
		}

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public DrawContext() {
			screenSizeX = screenSizeY = 1.0f;
			rateX = rateY = 1.0f;
			offsetX = offsetY = 0.0f;

			// Update();
			// ↑まだGLの初期化が終わっているとは限らないので、
			// ここではupdateは行なわないことにする。

			// MyClass.Update();
			// ↑VBなら出来るのに…(´ω`)
			myUpdate();
		}

		//---- 以下、メンバ。場合によっては直接アクセスしても構わない。

		/// <summary>
		/// 描画するときのclip情報 左。
		/// </summary>
		public float Left;
		/// <summary>
		/// 描画するときのclip情報 上。
		/// </summary>
		public float Top;
		/// <summary>
		/// 描画するときのclip情報 右。
		/// </summary>
		public float Right;
		/// <summary>
		/// 描画するときのclip情報 下。
		/// </summary>
		public float Bottom;

		/// <summary>
		/// offsetRX = offsetX * screenSizeX , offsetRY = offsetY * screenSizeY。
		/// </summary>
		public float OffsetRX,OffsetRY;
		/// <summary>
		/// rateとscreenSize,offsetを反映させたもの。実clip座標。
		/// </summary>
		public float LeftR,TopR,RightR,BottomR;
	}

	/// <summary>
	/// DrawContextのGl版
	/// </summary>
	/// <remarks>
	/// 矩形でのclippingの実現のために、openGLの
	/// 	glViewport(x1,screenSizeY-y2,x2-x1,y2-y1);
	/// 	glOrtho(x1,x2,y2,y1,0,256);
	/// を行なっている箇所があるので、これがまずければ、このDrawContext依存系の
	/// 描画は行なわないこと。
	/// </remarks>
	public class GlDrawContext : DrawContext
	{
		/*
		/// <summary>
		/// クローンメソッド。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// データメンバのcopyは行なわない。
		/// あくまで同じ型のオブジェクトを生成するだけ。
		/// </remarks>
		public override DrawContext Clone() { return new GlDrawContext(); }
		*/

		/// <summary>
		/// update処理を行なう
		/// </summary>
		public override void Update()
		{
			base.Update();

			if (Clip)
			{
				Gl.glLoadIdentity();
				int x1 = (int)LeftR, x2 = (int)RightR, y1 = (int)TopR, y2 = (int)BottomR;
				if (x1 > x2) x2 = x1;
				if (y1 > y2) y2 = y1;
				// orthoは整数なので、viewportに渡すのと丸めかたが違うと誤差が出る
				Gl.glViewport(x1, (int)(ScreenSizeY - y2), x2 - x1, y2 - y1);
				Gl.glOrtho(x1, x2, y2, y1, 0, 256);
			}
			else
			{
				Gl.glLoadIdentity();
				Gl.glViewport(0, 0, (int)ScreenSizeX, (int)ScreenSizeY);
				Gl.glOrtho(0, ScreenSizeX, ScreenSizeY, 0, 0, 256);
			}
		}

	}
}
