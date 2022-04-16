using System;
using System.Collections.Generic;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// テクスチャーのList。
	/// </summary>
	/// <remarks>
	///	ひとつの文字がひとつのテクスチャーであるとき、
	///	文字列は、textureのListとなる。これを管理するのがこのクラスである。
	/// </remarks>
	public class Textures : ITexture
	{
		#region テスト用のコード
		/* // テスト用のコード
	namespace WindowsApplication1
	{
		public partial class Form5 : Form
		{
			public Form5()
			{
				InitializeComponent();

				Init();
			}

			GlTexture txt = new GlTexture();
			Textures txts = new Textures();

			/// <summary>
			/// 
			/// </summary>
			public void Init()
			{
				window = new Win32Window(this.Handle);
				window.Screen.Select();

				fpsTimer = new FpsTimer();
				fps = new FpsLayer(fpsTimer);
				YanesdkResult result = txt.Load("omu.bmp");

				Console.WriteLine(result.ToString());

				Textures txts2 = new Textures();
				txts2.Add(txt, 0, 0);
				txts2.Add(txt, 100, 20);
				txts2.Add(txt, 200, 10);
				txts2.Update();
				txts2.Rate = 2;

				txts.Add(txts2, 0, 0);
				//	txts.Add(txt, 0, 0);
				//	txts.Add(txt, 100, 20);
				//	txts.Add(txt, 200, 10);
				txts.Add(txt, 300, 5);
				txts.Add(txt, 400, 0);
				txts.Update();

				window.Screen.Unselect();
			}


			Win32Window window;

			FpsTimer fpsTimer;
			FpsLayer fps;


			private void timer1_Tick_1(object sender, EventArgs e)
			{
				Yanesdk.Draw.IScreen scr = window.Screen;
				scr.Select();
				scr.SetClearColor(255, 0, 0);
				scr.Clear();

				//	scr.Blt(txt , 0 , 0);
				scr.Blend = true;
				scr.BlendSrcAlpha();

				//	scr.Blt(txt, 0, 40);
				scr.Blt(txts, 0, 40);
				txts.Rate += 0.01f;

				fps.OnDraw(window.Screen, 400, 100);

				scr.Update();
				fpsTimer.WaitFrame();
			}

		}
	}
	*/
		#endregion

		#region ctor
		/// <summary>
		/// コンストラクタ。
		/// </summary>
		public Textures()
		{
			Reset();
		}
		#endregion

		#region method
		/// <summary>
		/// テクスチャを連結する
		/// </summary>
		/// <param name="texture"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <remarks>
		/// 他のテクスチャを (x,y) の位置に連結する
		/// 連結後の Textures のサイズは、被覆部分を考慮しての最大矩形。
		/// 
		/// rateをかける前のwidth,heightから計算するので
		/// rate == 1と考えて座標を指定してください。
		/// 
		/// Addしたあとは再計算のために必ずUpdate()を呼び出すこと。
		/// </remarks>
		/// <example>
		/// 例)
		/// 	横50×縦30のTexturesに、横100×縦200のTexturesを
		/// 	(0,20)の位置の連結した場合、連結後のTexturesは、
		/// 	横100×縦220のサイズ。
		/// ITexture は、Texture , Textures の元となるクラスなので、
		/// 通常のTexture でも Textures でも連結できることを意味する。
		/// 
		/// x,yは負を指定しても良いが、その場合はwidth,heightには反映されない。
		/// </example>
		public void Add(ITexture texture, int x, int y)
		{
			TextureInfo info = new TextureInfo();

			info.Texture = texture;
			info.PosX = x;
			info.PosY = y;
			info.Width = texture.Width;
			info.Height = texture.Height;

			infos.Add(info);

			/* 連結後のtextureサイズを計算しなくては．． */
			float w = x + texture.Width;
			float h = y + texture.Height;

			//		width  = max(width,w);
			//		height = max(height,h);
			if (width < w) width = w;
			if (height < h) height = h;
		}

		/// <summary>
		/// テクスチャーの構造体(TextureInfo)を更新する。
		/// </summary>
		/// <remarks>
		/// このメソッドを呼び出さない限り、情報は更新されないので
		/// 正しく表示されない。Texture情報を書き換えた場合、必ず最後に
		/// 呼び出すようにしてください。
		/// 
		/// (Rateの変更に対してはUpdateを呼び出す必要はありません。)
		/// </remarks>
		public void Update() {
			//	ローカル変数に代入したほうが速い(だろう)
			float width_  = width;
			float height_ = height;
			foreach(TextureInfo info in infos){
				info.Left  = info.PosX / width_;
				info.LeftR = 1 - info.Left;
				info.Top   = info.PosY / height_;
				info.TopR  = 1 - info.Top;
				info.Right = (info.PosX+info.Width) / width_;
				info.RightR= 1 - info.Right;
				info.Bottom  = (info.PosY+info.Height) / height_;
				info.BottomR = 1 - info.Bottom;
			}
			//	update完了
		}

		/// <summary>
		/// リセット。
		/// </summary>
		/// <remarks>
		/// <para>
		/// 内部的に保持しているテクスチャをリセットする。
		/// </para>
		/// <para>
		/// <code>
		/// width = 0 , height = 0;
		/// rate = 1.0;
		/// infos = null;
		/// </code>
		/// </para>
		/// </remarks>
		public void Reset() { width = height = 0; rate = 1.0f; infos.Clear(); }
		
		#endregion

		#region ITextureの実装
		/// <summary>
		/// テクスチャの解放。
		/// 
		/// このクラス自体は、参照しているだけで、
		/// 何のテクスチャも持っていないと考えられるので
		/// リソース自体は解放しない。
		/// 
		/// 結局、Resetと同義。
		/// </summary>
		public void Release()
		{
			Reset();
		}

		/// <summary>
		/// このクラス自体は、参照しているだけで、
		/// 何のテクスチャも持っていないと考えられるので
		/// リソース自体は解放しない。
		/// 
		/// Releaseと同義。
		/// </summary>
		public void Dispose()
		{
			Release();
		}

		/// <summary>
		/// このメソッドはこのクラスではサポートされていない
		/// </summary>
		/// <param name="bmp"></param>
		/// <returns></returns>
		public YanesdkResult SetBitmap(global::System.Drawing.Bitmap bmp)
		{
			return YanesdkResult.NotImplemented;
		}

		/// <summary>
		/// このクラスではサーフェースの設定機能は実装されていない。
		/// </summary>
		/// <param name="surface"></param>
		/// <returns></returns>
		public YanesdkResult SetSurface(Surface surface)
		{
			return YanesdkResult.NotImplemented;
		}

		/// <summary>
		/// ITextureのBltのoverride
		/// </summary>
		/// <remarks>
		/// rateを考慮してのblt
		/// </remarks>
		/// <param name="drawContext"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Blt(DrawContext drawContext,float x,float y) {
			float w = Width;
			float h = Height;
			foreach(TextureInfo info in infos){
				info.Texture.Blt(drawContext, x+info.Left*w, y+info.Top*h
					,null
					,new Size(info.Width*rate,info.Height*rate)
				);
			}
		}

		/// <summary>
		/// ITextureのBltのoverride
		/// </summary>
		/// <remarks>
		/// rateを考慮してのblt
		/// </remarks>
		/// <param name="drawContext"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		public void Blt(DrawContext drawContext,float x,float y,Rect srcRect) {

			Point[] points = new Point[4];
			float w,h;

			if (srcRect == null)
			{
				srcRect = new Rect(0, 0, Width, Height);
			}

			w = srcRect.Right - srcRect.Left;
			if (w<0) w = -w;

			h = srcRect.Bottom - srcRect.Top;
			if (h<0) h = -h;

			points[0].SetPoint(x,y);
			points[1].SetPoint(x+w*rate,y);
			points[2].SetPoint(x+w*rate,y+h*rate);
			points[3].SetPoint(x,y+h*rate);

			Blt(drawContext,srcRect,points);
		}

		/// <summary>
		/// ITextureのBltのoverride
		/// </summary>
		/// <remarks>
		/// rateを考慮してのblt
		/// </remarks>
		/// <param name="drawContext"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstSize"></param>
		public void Blt(DrawContext drawContext,float x,float y, Rect srcRect,Size dstSize) {

			Point[] points = new Point[4];

			if (srcRect == null)
			{
				srcRect = new Rect(0, 0, Width, Height);
			}
			if (dstSize == null)
			{
				dstSize = new Size(drawContext.ScreenSizeX, drawContext.ScreenSizeY);
			}

			points[0].SetPoint(x,y);
			points[1].SetPoint(x+dstSize.Cx-1,y);
			points[2].SetPoint(x+dstSize.Cx-1,y+dstSize.Cy-1);
			points[3].SetPoint(x,y+dstSize.Cy-1);

			Blt(drawContext,srcRect,points);
		}


		/// <summary>
		/// 左上がd[0],右上がd[1],右下がd[2],左下がd[3]のとき、
		/// u,vをpに返す。
		/// </summary>
		/// <param name="d"></param>
		/// <param name="u"></param>
		/// <param name="v"></param>
		/// <param name="p"></param>
		protected void calcUV(Point[] d,float u,float v,ref Point p){
			//	(x1,y1) = d0 + (d0_1)u 
			float x1 = d[0].X * (1-u) + d[1].X*(u);
			float y1 = d[0].Y * (1-u) + d[1].Y*(u);

			//	(x2,y2) = d3 + (d3_2)u 
			float x2 = d[3].X * (1-u) + d[2].X*(u);
			float y2 = d[3].Y * (1-u) + d[2].Y*(u);

			//	(x1,y1)-(x2,y2)をvで内分
			p.X = x1 * (1-v) + x2 * v;
			p.Y = y1 * (1-v) + y2 * v;
		}

		/// <summary>
		/// ITextureのBltのoverride
		/// </summary>
		/// <param name="drawContext"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstPoint"></param>
		public void Blt(DrawContext drawContext,Rect srcRect,Point[] dstPoint) {
			float w = Width;
			float h = Height;

			if (srcRect == null)
			{
				srcRect = new Rect(0, 0, Width, Height);
			}

			Rect sr = new Rect(srcRect.Left / w, srcRect.Top / h, srcRect.Right / w, srcRect.Bottom / h);
			{
				//	転送元は0.0～1.0にclipping
				if (sr.Left<0.0) sr.Left = 0.0f;
				if (sr.Right<0.0) sr.Right = 0.0f;
				if (sr.Top<0.0) sr.Top = 0.0f;
				if (sr.Bottom<0.0) sr.Bottom = 0.0f;
				if (sr.Left>1.0) sr.Left = 1.0f;
				if (sr.Right>1.0) sr.Right = 1.0f;
				if (sr.Top>1.0) sr.Top = 1.0f;
				if (sr.Bottom>1.0) sr.Bottom = 1.0f;
			}

			bool bW=false,bH=false; // 水平方向の反転フラグ

			float www = sr.Right - sr.Left;
			float hhh = sr.Bottom - sr.Top;

			if (www == 0 || hhh == 0) return ;
			if (www<0) { www = -www; bW = true; }
			if (hhh<0) { hhh = -hhh; bH = true; }

			foreach(TextureInfo info in infos){

				Point[] dp = new Point[4];	//	転送先
				Rect r = new Rect();			//	転送元矩形

				//	転送元矩形をテクスチャ座標で表したもの
				Rect rr = new Rect(info.Left, info.Top, info.Right, info.Bottom);

				// sr のなかに含まれる矩形か?
				if (sr.Left <= info.Left && info.Right	<= sr.Right
					&&	sr.Top	<= info.Top  && info.Bottom <= sr.Bottom){
					//	含まれるのでそのまま描画
					r.SetRect(0,0,info.Width,info.Height);
				} else {
					//	完全には含まれていないので、削り取る作業が必要
					r.SetRect(0,0,info.Width,info.Height);

					float ww = info.Width  / (info.Right-info.Left);
					float hh = info.Height / (info.Bottom-info.Top);

					float t;

					if (!bW){
						t = sr.Left - info.Left;
						if (t>0) {
							r.Left += t * ww;
							rr.Left = sr.Left;
						}
						t = info.Right - sr.Right;
						if (t>0) {
							r.Right -= t * ww;
							rr.Right = sr.Right;
						}

						if (r.Left > r.Right) continue; // 表示エリアなし
					} else {
						t = sr.Right - info.Left;
						if (t>0) {
							r.Left += t * ww;
							rr.Left = sr.Right;
						}

						t = info.Right - sr.Left;
						if (t>0) {
							r.Right -= t * ww;
							rr.Right = sr.Left;
						}
						if (r.Left > r.Right) continue; // 表示エリアなし
					}
					if (!bH){
						t = sr.Top - info.Top;
						if (t>0) {
							r.Top += t * hh;
							rr.Top = sr.Top;
						}
						t = info.Bottom - sr.Bottom;
						if (t>0) {
							r.Bottom -= t * hh;
							rr.Bottom = sr.Bottom;
						} else t = 0;
						if (r.Top > r.Bottom) continue; // 表示エリアなし
					} else {
						t = sr.Bottom - info.Top;
						if (t>0) {
							r.Top += t * hh;
							rr.Top = sr.Bottom;
						} else t = 0;
						t = info.Bottom - sr.Top;
						if (t>0) {
							r.Bottom -= t * hh;
							rr.Bottom = sr.Top;
						}
						if (r.Top > r.Bottom) continue; // 表示エリアなし
					}
				}

				float leftRM;
				float rightRM;
				if (!bW){
					leftRM	= (rr.Left - sr.Left) / www;
					rightRM = (rr.Right - sr.Left) / www;
				} else {
					leftRM	= (1 - (rr.Left - sr.Right)) / www;
					rightRM = (1 - (rr.Right - sr.Right)) / www;
				}
				float topRM;
				float bottomRM;
				if (!bH){
					topRM = (rr.Top - sr.Top) / hhh;
					bottomRM = (rr.Bottom - sr.Top) / hhh;
				} else {
					topRM = (1-(rr.Top - sr.Bottom)) / hhh;
					bottomRM = (1-(rr.Bottom - sr.Bottom)) / hhh;
				}

				calcUV(dstPoint,leftRM,topRM,ref dp[0]);
				calcUV(dstPoint,rightRM,topRM,ref dp[1]);
				calcUV(dstPoint,rightRM,bottomRM,ref dp[2]);
				calcUV(dstPoint,leftRM,bottomRM,ref dp[3]);

				info.Texture.Blt(drawContext,r,dp);
			}
		}

		/// <summary>
		/// これ、めちゃくちゃ面倒くさいですなぁ．．（´Д｀）
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstPoint"></param>
		/// <remarks>
		/// 未実装だ！　実装する予定は当分無い！
		/// </remarks>
		public void Blt(DrawContext src, Point[] srcRect, Point[] dstPoint){}

		#endregion

		#region properties

		/// <summary>
		/// テクスチャの幅を得る。
		/// Width = 実幅×Rate
		/// </summary>
		/// <returns></returns>
		public float Width { get { return width * rate; } }

		/// <summary>
		/// テスクチャの高さを得る。
		/// </summary>
		/// <returns></returns>
		public float Height { get { return height * rate; } }

		/// <summary>
		/// 倍率を設定/取得する(default:1.0)。
		/// </summary>
		/// <param name="rate_"></param>
		/// <remarks>
		/// Width / Height で返される値は、
		/// Width / Heihgt や 連結等によって得られたテクスチャサイズ
		/// から、ここで設定した倍率の掛かった値になる。
		/// 
		/// 結果として描画するときにこの倍率の掛かったサイズで描画される。
		/// 連結時には、ここで設定された倍率は考慮されない。
		/// 
		/// ※　よって連結後、Update前までにRateを設定して使うと良いだろう。
		/// <returns></returns>
		public float Rate
		{
			get { return rate; }
			set { rate = value; }
		}

		/// <summary>
		/// 内部的に保持しているテクスチャを返す。
		/// </summary>
		/// <returns></returns>
		public List<TextureInfo> Infos
		{
			get { return infos; }
		}

		#endregion

		///	<summary>それぞれのテクスチャーの存在する位置を表す構造体。</summary>
		/// <remarks>
		/// <para>
		///	(left,top),(right,bottom)は、あるひとつのテクスチャの
		///	存在する位置(矩形)を(sizeX,sizeY)との比率で表したもの。
		///	</para>
		///
		///	<para>なお、leftR = 1-leftである。</para>
		/// <example>
		///	例) 矩形(x1,y1)-(x2,y2)に、Bltしたい場合、
		///		矩形(left*x1 + leftR*x2 , top*y1 + topR*y2)
		///			- (right*x1 + rightR*x2 , bottom*y1 + bottomR*y2)
		///	で求まる。
		///	</example>
		/// </remarks>
		public class TextureInfo
		{
			/// <summary>
			/// このテクスチャの幅。
			/// </summary>
			public float Width, Height;

			/// <summary>
			/// 描画位置。
			/// </summary>
			/// <remarks>
			/// Textures.updateでは、このwidth,height,posX,posYと
			/// Textures.widthとheightから
			/// left,leftR,top,topR,right,rightR,bottom,bottomRを算出します。
			/// 
			/// rateをかける前のwidth,heightから計算するので
			/// rate == 1と考えて座標を指定してください。
			/// </remarks>
			public float PosX, PosY;

			/// <summary>
			/// 描画すべきテクスチャー。
			/// </summary>
			public ITexture Texture;

			/// <summary>
			/// 
			/// </summary>
			public float	Left,LeftR;
			/// <summary>
			/// 
			/// </summary>
			public float	Top,TopR;
			/// <summary>
			/// 
			/// </summary>
			public float	Right,RightR;
			/// <summary>
			/// 
			/// </summary>
			public float	Bottom,BottomR;
		}

		#region private

		/// <summary>
		/// このテクスチャベクター全体でのサイズ。
		/// </summary>
		private float width,height;
		/// <summary>
		/// このテクスチャの倍率を設定する。
		/// </summary>
		private float rate;
		/// <summary>
		/// テクスチャーの情報(updateを呼び出した時に更新される)。
		/// </summary>
		private List<TextureInfo> infos = new List<TextureInfo>();

		#endregion

		#region Debug用
		//	debug用の表示関数
		private void DebugOutput(Point[] dp)
		{
			for(int i=0;i<dp.Length;++i){
				Console.WriteLine("i={0} : ({1},{2})", i,dp[i].X,dp[i].Y);
			}
		}
		#endregion
	}
}

