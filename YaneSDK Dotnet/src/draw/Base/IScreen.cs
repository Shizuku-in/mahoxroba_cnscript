using System;
using System.Collections.Generic;
using System.Text;
using Yanesdk.Ytl;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 2D/3D共通interface
	/// </summary>
	public interface IScreen
	{
		/// <summary>
		/// このスクリーンに対して描画を開始するときに呼び出す
		/// </summary>
		/// <remarks>
		/// select～updateまでが一連の描画である。
		/// 
		/// また、HDCWindowを用いる場合、focusのあるopenglに
		/// テクスチャを読み込ませる必要があるため、bltしたいHDCWindowの
		/// openglをactiveにしておき読み込ませる必要がある。
		/// 
		/// hdcWindow.Screen.Select();
		/// texture.load("file.jpg");
		/// hdcWindow.Screen.Unselect(); // select～unselectでtextureを読み込む
		/// 
		/// ここでSelect～UnSelect/Updateまでの時間は、WM_PAINTのPaintハンドラ
		/// でBeginPaint～EndPaintしてるようなもので、他のウィンドウへの描画
		/// (Select自体が)出来ない。(するべきではない)
		/// </remarks>
		void Select();

		/// <summary>
		/// selectを解除する。描画は行なわない。
		/// </summary>
		void Unselect();

		///	<summary>実画面に描画を行なう(2D/3D)。</summary>
		///	<remarks>このメソッドを呼び出したときにselectを解除する。
		/// ビデオドライバの実装によっては、このメソッドによって、
		/// 垂直帰線待ちになることがある。よって、画面のリフレッシュレート以上は
		/// 出ないことがあると考えるべき。
		///	</remarks>
		YanesdkResult Update();

		///	<summary>画面のクリア(2D/3D)。</summary>
		/// <remarks>
		///	画面をクリアする色は、 setClearColor で設定された色
		/// </remarks>
		void Clear();

		///	<summary>clearするときの色を設定する(2D/3D)。</summary>
		/// <remarks> 
		///	rgbaは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		void SetClearColor(int r, int g, int b, int a);

		///	<summary>clearするときの色を設定する(2D/3D)。</summary>>
		/// <remarks>
		///	rgbは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		void SetClearColor(int r, int g, int b);

		///	<summary>色を設定する(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を設定する。
		///	rgbは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		void SetColor(int r, int g, int b);

		/// <summary>
		///	色を設定する(2D/3D)
		/// </summary>
		/// <remarks>
		/// ラインやポリゴンの描画色を設定する。
		/// </remarks>
		/// <param name="c"></param>
		void SetColor(Color4ub c);

		///	<summary>色を設定する(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を設定する。
		///	rgbは各 0～255。
		///	パラメータはbyteでいいのだが、intからbyteに暗黙で変換しないので
		///	かえって使いにくいので、intをとるようになっている。
		/// </remarks>
		void SetColor(int r, int g, int b, int a);

		///	<summary>色をリセットする(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を(255,255,255)に設定する。
		///	テクスチャー貼り付けのときなど、setColorの値で乗算されるので、
		///	そのままの画像を表示したいならば(255,255,255)に設定しておく
		///	必要があるため。
		/// </remarks>
		void ResetColor();

		///	<summary>色をセットする(2D/3D)。</summary>
		/// <remarks>
		///	ラインやポリゴンの描画色を(a,a,a)に設定する。
		///	aの範囲は0～255。
		///	テクスチャー貼り付けのときなど、setColorの値で乗算されるので、
		///	fade(薄く描画)したければ、(a,a,a)に設定する必要があるため。
		/// </remarks>
		void SetColor(int a);

		/// <summary>
		/// setColorで設定されている色の取得。
		/// </summary>
		/// <param name="r_"></param>
		/// <param name="g_"></param>
		/// <param name="b_"></param>
		void GetColor(out int r, out int g, out int b);

		/// <summary>
		/// setColorで設定されている色の取得。
		/// </summary>
		/// <param name="r_"></param>
		/// <param name="g_"></param>
		/// <param name="b_"></param>
		/// <param name="a_"></param>
		void GetColor(out int r, out int g, out int b, out int a);

		///	<summary>画面にオフセットを加える(2D)。</summary>
		/// <remarks>
		///	表示する視界に2次元オフセット(ox,oy)を加える。
		///	(100,200)を設定すると、画面下が(100,200),画面右上が(100+640,200+480)
		///	の意味になる。画面を揺らしたりするときに使うと良い。
		/// </remarks>
		void SetScreenOffset(int ox, int oy);

		///	<summary>視体積の設定(2D)。</summary>
		/// <remarks>
		///	ウィンドゥの左上が(x1,y1),右下が(x2,y2)になるように設定される。
		///	setScreenOffset,setVideoMode等で内部的に呼び出される
		/// </remarks>
		void SetView(int x1, int y1, int x2, int y2);


		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	テクスチャーを(0,0)に等倍で描画する。
		/// テクスチャーの色には、直前のsetColorの値が乗算される。
		///
		///	色の変更　→　setColor(r,g,b)/setColor(a)/resetColor()
		///	ブレンド方法変更　→　blendなんちゃら関数 
		///
		///	※　テクスチャーを描画するとき、
		///	glBindTextureを内部的に呼び出すので、これを設定している人は注意。
		/// 描画を転送先でclipしたいときは、 getDrawContext で
		/// 描画コンテクストを書き換えればok。
		/// 
		/// Q)このメソッドは描画すべき座標をなぜ指定できないのか
		/// A)このメソッドはSpriteクラスのように内部的に座標を保持できるクラスで
		/// 用いることを想定している。あるいは本当に(0,0)にBGを描画したいときに用いると良い。
		/// </remarks>
		void Blt(ITexture src);

		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	テクスチャーを(x,y)に等倍で描画する。
		///	テクスチャーの色には、直前のsetColorの値が乗算される。
		///
		///	色の変更　→　setColor(r,g,b)/setColor(a)/resetColor()
		///	ブレンド方法変更　→　blendなんちゃら関数 
		///
		///	※　テクスチャーを描画するとき、
		///	glBindTextureを内部的に呼び出すので、これを設定している人は注意。
		/// 描画を転送先でclipしたいときは、 getDrawContext で
		/// 描画コンテクストを書き換えればok。
		/// </remarks>
		void Blt(ITexture src, int x, int y);

		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	<para>bltの転送元矩形の指定できるバージョン。
		///	srcRectがnullのときはソース全域。
		/// </para>
		/// <para>
		///	高速化のため転送元矩形が転送元サーフェースからハミ出る場合の
		///	処理はしていないので、何とかしる。
		/// </para>
		/// <para>
		///	転送元矩形として、(Right,Top,Left,Bottom)を指定すれば
		///	左右反転して表示される。(Left,Bottom,Right,Top)を指定すれば
		///	上下反転して表示される。
		///	</para>
		/// </remarks>
		void Blt(ITexture src, int x, int y, Rect srcRect);

		///	<summary>画像の描画(2D)。</summary>
		/// <remarks>
		///	bltの転送元矩形と転送先サイズの指定できるバージョン。
		///	srcRectがnullのときはソース全域。
		///	dstSizeがnullのときは転送先全域(のサイズ)。
		///
		///	転送元矩形として、(Right,Top,Left,Bottom)を指定すれば
		///	左右反転して表示される。(Left,Bottom,Right,Top)を指定すれば
		///	上下反転して表示される。
		/// </remarks>
		void Blt(ITexture src, int x, int y, Rect srcRect, Size dstSize);

		///	<summary>画像の描画(2D)</summary>
		/// <remarks>
		///	bltの転送元矩形と転送先4点の指定できるバージョン。
		///	srcRectがnullのときはソース全域。
		///	転送元矩形が、転送先として指定された4点に移動。
		///
		///	転送先は、
		///	point[0]が左上、point[1]が右上、point[2]が右下、point[3]が左下
		///	の点を指定しているものとする。
		/// </remarks>
		/// <example>
		/// <code>
		///	Screen screen = new Screen;
		///	screen.setVideoMode(640,480,0);
		///	GlTexture tex = new GlTexture;
		///	tex.load("title.png");
		///
		///	while (GameFrame.pollEvent()==0){
		///
		///		screen.clear();
		///
		///		Point[4] points;
		///		points[0].setPoint(100,100);
		///		points[1].setPoint(400,100);
		///		points[2].setPoint(640,480);
		///		points[3].setPoint(0,480);
		///		screen.blt(tex2,0,0,null,points);	//	台形描画
		///
		///		screen.Update();
		///	}
		///	</code>
		///	</example>
		void Blt(ITexture src, Rect srcRect, Point[] point);

		/// <summary>
		/// 凸四角形→凸四角形への転送。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcPoint"></param>
		/// <param name="dstPoint"></param>
		void Blt(ITexture src, Point[] srcPoint, Point[] dstPoint);

		/// <summary>
		/// bltの回転機能つき。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="rad"></param>
		/// <param name="rate"></param>
		/// <param name="base_"></param>
		/// <remarks>
		/// <para>
		/// 指定のテクスチャを転送先の(x,y)にrad回して描画します。
		/// </para>
		/// <para>
		/// radの単位は、0～512で一周(2π)となる角度。
		/// 	回転の方向は、右まわり（時計まわり）。
		/// rateは、拡大率。1.0ならば等倍。	
		/// </para>
		/// <para>
		/// basePosは転送元画像どの点が(x,y)に来るかを指定します。
		/// 	0 : 左上。		1 : 上辺の中点　2:右上
		/// 	3 : 左辺の中点	4 : 画像中心	5:右辺の中点	
		/// 	6 : 左下。　	7 : 下辺の中点	8:右下
		/// </para>
		/// </remarks>
		void BltRotate(ITexture src, int x, int y, int rad, float rate, int basePos);

		/// <summary>
		/// bltの回転機能つき。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="rad"></param>
		/// <param name="rate"></param>
		/// <param name="bx"></param>
		/// <param name="by"></param>
		/// <remarks>
		/// <para>
		/// 指定のテクスチャを転送先の(x,y)にrad回して描画します。
		/// </para>
		/// <para>
		/// radの単位は、0～512で一周(2π)となる角度。
		/// 	回転の方向は、右まわり（時計まわり）。
		/// rateは、拡大率。1.0ならば等倍。
		/// (bx,by)は転送元の回転中心。(x,y)の地点が転送元の画像中心になるように
		/// したいならば、(x-bx*rate,y-by*rate)をこのメソッドのx,yに渡す必要がある。	
		/// </para>
		/// </remarks>
		void BltRotate(ITexture src, int x, int y, int rad, float rate, int bx, int by);

		/// <summary>
		/// bltの回転機能つき。
		/// </summary>
		/// <param name="src"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="srcRect"></param>
		/// <param name="rad"></param>
		/// <param name="rate"></param>
		/// <param name="bx"></param>
		/// <param name="by"></param>
		/// <remarks>
		/// <para>
		/// 指定のテクスチャを転送先の(x,y)に半径rで描画します。
		/// </para>
		/// <para>
		/// radの単位は、0～512で一周(2π)となる角度。
		/// 	回転の方向は、右まわり（時計まわり）。
		/// rateは、拡大率。1.0ならば等倍。
		/// (bx,by)は転送元の画像の、回転中心。
		/// srcRectは転送元矩形。nullならば転送元テクスチャ全体。
		/// </para>
		/// </remarks>
		void BltRotate(ITexture src, int x, int y, Rect srcRect, int rad, float rate, int bx, int by);

		///	<summary>
		/// ブレンドを有効化/無効化する。
		/// ブレンドの方式は、別途BlendSrcAlpha等から選択する必要がある。
		/// </summary>
		/// <remarks>
		/// Blend = falseとすると、
		///	blt メソッド等で描画するときのblendを無効化する。
		///	(ディフォルトでは無効)
		/// 
		/// 以下、OpenGLの説明から抜粋。
		/// 
		///	<code>glBlendFunc(srcFunc,dstFunc);</code>
		///	//	転送元に適用する関数 , 転送先に適応する関数
		/// 
		/// <example>
		///	例)
		///	<code>glBlendFunc(GL_DST_COLOR,GL_ZERO);</code>
		///	ならば、
		///		転送先のピクセルの値
		///			＝　転送元のピクセル×転送先のピクセル(GL_DST_COLOR)
		///				＋　転送先のピクセル×０(GL_ZERO)
		///			＝　転送元のピクセル×転送先のピクセル
		///	となり、転送元ピクセルで抜いたような形になる。
		///
		///	指定できる定数は、以下の通り。
		///		定数 (fR,fG,fB,fA) 
		///	<code>
		///		GL_ZERO (0,0,0,0) 
		///		GL_ONE (1,1,1,1) 
		///		GL_SRC_COLOR (Rs/kR ,Gs/kG,Bs/kB,As/kA) 
		///		GL_ONE_MINUS_SRC_COLOR (1,1,1,1)-(Rs/kR,Gs/kG,Bs/kB,As/kA) 
		///		GL_DST_COLOR (Rd/kR,Gd/kG,Bd/kB,Ad/kA) 
		///		GL_ONE_MINUS_DST_COLOR (1,1,1,1)-(Rd/kR,Gd/kG,Bd/kB,Ad/kA) 
		///		GL_SRC_ALPHA (As/kA,As/kA,As/kA,As/kA) 
		///		GL_ONE_MINUS_SRC_ALPHA (1,1,1,1)-(As/kA,As/kA,As/kA,As/kA) 
		///		GL_DST_ALPHA (Ad/kA,Ad/kA,Ad/kA,Ad/kA) 
		///		GL_ONE_MINUS_DST_ALPHA (1,1,1,1)-(Ad/kA,Ad/kA,Ad/kA,Ad/kA) 
		///		GL_SRC_ALPHA_SATURATE (i,i,i,1) 
		///		</code>
		///	s : source(転送元) , d : destination(転送先)
		///	kR,kG,kB,kA : 255(RGBA8888のとき)
		///	i : min(As, kA-Ad) / kA
		///
		///		<code>glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);</code>
		///	なら、
		///		転送先　＝
		///			転送元の値×転送元のα　＋　転送先の値×（１－転送元のα）
		///	となり、いわゆる普通のαブレンドになる
		/// </example>
		/// </remarks>
		bool Blend { get; set; }

		///	<summary>ブレンドとしてaddColorを指定する。</summary>
		/// <remarks>
		///	blt メソッド等で描画するときのblendを有効にする。
		///	無効にするには disableBlend を呼び出すこと。
		///
		///	ブレンド方法としては、addColor(加色合成)を指定する。
		///	すなわち、glBlendFunc(GL_ONE,GL_ONE);を行なっている。
		///
		///	α付きサーフェースでなくともaddColorは出来るので、
		///	これを使って転送すると高速化を図れることがある。
		///
		///	光の表現をするときなどに使うと良い。
		///
		///	http://www.platz.or.jp/~moal/ablend.html
		///	が詳しい。
		/// 
		/// alpha channelありのテクスチャに対して、このメソッドを呼び出して
		/// Bltした場合、転送元のalpha channelはすべて無視される。(255として扱われる)
		/// それで良ければ呼び出して構わない。(ダメならBlendAddColorAlphaを用いること)
		/// </remarks>
		void BlendAddColor();

		/// <summary>
		/// 転送元がα付きサーフェースのときのaddcolor。
		/// </summary>
		/// <remarks>
		/// <para>
		/// blendAddColor の α付きサーフェースのとき用。
		/// </para>
		/// <para>
		/// 内部的には
		/// <code>
		/// enableBlend();
		/// glBlendFunc(GL_SRC_ALPHA,GL_ONE);
		/// </code>
		/// を行なっている。
		/// </para>
		/// 転送元のテクスチャにalpha channelが無い場合は、
		/// そのテクスチャは、全域で alpha == 255 となみされる。
		/// よって、alpha channelなしのテクスチャに対して、このメソッドを呼び出した状態に
		/// してBltしてもかまわない。
		/// </remarks>
		void BlendAddColorAlpha();

		///	<summary>ブレンドとしてsubColorを指定する。</summary>
		/// <remarks>
		///	blt メソッド等で描画するときのblendを有効にする。
		///	これを使って転送すると高速化を図れることがある。
		///
		///	ブレンド方法としては、subColor(加色合成)を指定する。
		///	すなわち、glBlendFunc(GL_ZERO,GL_ONE_MINUS_SRC_COLOR);を行なっている。
		///
		///	α付きサーフェースでなくともsubColorは出来るので、
		///	これを使って転送すると高速化を図れることがある。
		///
		///	http://www.platz.or.jp/~moal/subtract.html
		///	が詳しい。
		///
		/// alpha channelありのテクスチャに対して、このメソッドを呼び出して
		/// Bltした場合、転送元のalpha channelはすべて無視される。(255として扱われる)
		/// それで良ければ呼び出して構わない。(ダメならBlendSubColorAlphaを用いること)
		/// </remarks>
		void BlendSubColor();

		/// <summary>
		/// 転送元がα付きサーフェースのときのsubcolor
		/// </summary>
		/// <remarks>
		/// <para>
		/// blendSubColor の α付きサーフェースのとき用。
		/// </para>
		/// <para>
		/// 内部的には
		/// <code>
		/// enableBlend();
		/// glBlendFunc(GL_ZERO,GL_ONE_MINUS_SRC_ALPHA);
		/// </code>
		/// を行なっている。
		/// </para>
		/// 転送元のテクスチャにalpha channelが無い場合は、
		/// そのテクスチャは、全域で alpha == 255 となみされる。
		/// よって、alpha channelなしのテクスチャに対して、このメソッドを呼び出した状態に
		/// してBltしてもかまわない。
		/// </remarks>
		void BlendSubColorAlpha();

		///	<summary>ブレンドとして、αブレンドを指定する。</summary>
		/// <remarks>
		///	blt メソッド等で描画するときのblendを有効にする。
		///	ブレンド方法として、
		///
		///	<code>glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);</code>
		///
		///	を指定している。
		///
		///	これにより、転送元のα値により、αブレンドが行なわれる。
		///	テクスチャーのカラーキー付き転送も、実際はカラーキーの部分のα値を
		///	0にすることにより実現しているので、この転送方式を用いないと、
		///	カラーキー付き転送は実現できない。
		/// 
		/// 転送元のテクスチャにalpha channelが無い場合は、
		/// そのテクスチャは、全域で alpha == 255 となみされる。
		/// よって、alpha channelなしのテクスチャに対して、このメソッドを呼び出した状態に
		/// してBltしてもかまわない。
		/// </remarks>
		void BlendSrcAlpha();

		/// <summary>
		/// Blendを無効にする。
		/// </summary>
		/// <remarks>
		/// Blend = falseとしているのと同じなのだけれども、
		/// BlendSrcAlpha等と対となるメソッドがないと気持ち悪いので。
		/// </remarks>
		void BlendDisable();

		///	<summary>三角形ポリゴンを描く(2D)。</summary>
		/// <remarks>
		///	3点を繋ぐようにした三角形を描く。中は、塗りつぶし。
		///	※描画は二次元的な描画なのであとから描画したものが前面に描画される。
		///	DrawContext に基づくclipping、座標系の変換を行なう。
		/// </remarks>
		void DrawPolygon(int x1, int y1, int x2, int y2, int x3, int y3);

		///	<summary>長方形ポリゴンを描く(2D)。</summary>
		/// <remarks>
		///	4点を繋ぐようにした四角形を描く。要するに凸4角形。中は、塗りつぶし。
		///	※描画は二次元的な描画なのであとから描画したものが前面に描画される。
		///	DrawContext に基づくclipping、座標系の変換を行なう。
		/// </remarks>
		void DrawPolygon(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);

		///	<summary>凸5角形を描く。(2D)。</summary>
		/// <remarks>
		///	5点を繋ぐようにした四角形を描く。要するに凸5角形。中は、塗りつぶし。
		///	※描画は二次元的な描画なのであとから描画したものが前面に描画される。
		///	DrawContext に基づくclipping、座標系の変換を行なう。
		/// </remarks>
		void DrawPolygon(int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4, int x5, int y5);


		/// <summary>
		/// 描画用のコンテクストの設定/取得。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// 値を変更すると、その変更した値に従って描画される。
		/// </remarks>
		DrawContext DrawContext { get; set; }

		/// <summary>
		/// このサーフェースのサイズを取得
		/// </summary>
		int Width { get; }

		/// <summary>
		/// このサーフェースのサイズを取得
		/// </summary>
		int Height { get; }

		/// <summary>
		/// 画面キャプチャをとる
		/// </summary>
		/// <returns></returns>
		Surface GetSurface();

		/// <summary>
		/// 破棄する
		/// </summary>
		void Dispose();
	}

	/// <summary>
	/// 2D専用interface
	/// </summary>
	public interface IScreen2D : IScreen
	{

	}
}
