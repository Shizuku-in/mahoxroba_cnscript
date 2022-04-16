using System;
using System.Collections.Generic;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 文字テクスチャーのキャッシュ用クラス。
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <example>
	/// <code>
	/// FontLoader loader = new FontLoader();
	/// loader.loadDefRW("msmincho.ttc , 0 , 50 , 0\n");
	///
	///	FontRepository fr = new FontRepository;
	///	fr.SetLoader(loader,0);
	///	fr.Max = 100;
	///
	///	//	メインループにて
	///
	///	Textures fonttv = fr.getTexture("この文字列描画どや！");
	///	//	↑font repositoryから取り出すので、超ハヤー（ﾟДﾟ）
	///
	///	//		fonttv.setSurface(
	///	//			loader.get(0).drawBlendedUnicode("この文字列描画どや！"));
	///	//	↑毎フレーム文字列フォントを生成するとすごく負荷がかかる
	///
	///	screen.SetColor(255,0,0);
	///
	///	screen.Blt(fonttv,30,100);
	///	screen.resetColor();
	/// 
	/// </code>
	/// </example>
	public class FontRepository : IDisposable
	{

		#region Font系のテスト用のコードその1
		/*
				FontRepository fr;
				FontLoader fl;
				Textures txts;

				public void init()
				{
					window = new Win32Window(this.pictureBox1.Handle);
					window.Screen.Select();

					fl =new FontLoader();
					fl.LoadDefFile("mem:msmincho.ttc , 16 , 0 , 0\n");
					fr = new FontRepository(delegate { return new GlTexture(); });
					fr.SetLoader(fl, 0);

					txts = fr.GetTexture("これ描画できるんか？",0);

					window.Screen.Unselect();
				}

				Win32Window window;

				private void timer1_Tick(object sender , EventArgs e)
				{
					Yanesdk.Draw.IScreen scr = window.Screen;
					scr.Select();
					scr.SetClearColor(255, 0, 0);
					scr.Clear();

					scr.Blend = true;
					scr.BlendSrcAlpha();
					scr.Blt(txts , 0 , 0);

					scr.Update();
				}
		*/
		#endregion

		#region Font系のテスト用のコードその2
		/*
		Win32Window window;

		private void OnInit(){
			window = new Win32Window(this.pictureBox1.Handle);
			window.Screen.Select();

			FontRepository fr;
			FontRepository fr2;
			FontLoader fl;
			Textures[] txts = new Textures[12];
			fl =new FontLoader();
			fl.LoadDefFile("mem:msmincho.ttc , 32 , 0 \nmsmincho.ttc , 16 , 0 \n");
			fr = new FontRepository(delegate { return new GlTexture(); });
			fr.SetLoader(fl, 0);
			fr2 = new FontRepository(delegate { return new GlTexture(); });
			fr2.SetLoader(fl, 1);

			for (int i = 0; i < 3; ++i)
			{
				int j = i * 4;
				txts[0+j] = fr.GetTexture("これ描画できるんか？", i);
				txts[1+j] = fr.GetTextureSolid("これ描画できるんか？", i);
				txts[2+j] = fr2.GetTexture("これ描画できるんか？", i);
				txts[3+j] = fr2.GetTextureSolid("これ描画できるんか？", i);
			}
		}

		private void timer1_Tick(object sender , EventArgs e)
		{
			Yanesdk.Draw.IScreen scr = window.Screen;
			scr.Select();
			scr.SetClearColor(255, 0, 0);
			scr.Clear();

			scr.Blend = true;
			scr.BlendSrcAlpha();
			
			for(int i=0;i<12;++i)
				scr.Blt(txts[i] , 0 , i*20);

			scr.Update();
		}	
	*/
		#endregion

		/// <summary>
		/// キャッシュする文字の数。
		/// </summary>
		/// <param name="max"></param>
		/// <remarks>
		/// 一画面に描画しうる最大文字数より大きな値にしておかないと
		/// 描画している最中に解放されて、大変なことになります。
		/// 
		/// 300ぐらいが適当だと思いますが、文字をたくさん描画する場合は
		/// その数に合わせて調整されることをお勧めします。
		/// 
		/// default値 : 300
		/// </remarks>
		public int Max
		{
			set
			{
				if (max != value)
				{
					ResizeFontCache(value);
					max = value;
				}
			}
		}
		private int max = 300;

		/// <summary>
		/// フォントのcacheサイズの変更を行なう。
		/// </summary>
		/// <param name="size"></param>
		private void ResizeFontCache(int size)
		{
			Release();

			fonts.Clear();
			fonts.Capacity = size;
		}

		public delegate ITexture TextureFactory();

		/// <summary>
		/// コンストラクタで、ITexture派生クラスを生成するfactoryを渡してやる。
		/// 
		/// 例)
		///		TextureLoader loader = new TextureLoader(delegate {
		///           return new GlTexture(); });
		/// </summary>
		/// <remarks>
		/// ディフォルトではcache size = 64MB
		/// OpenGLを描画に使っている場合、テクスチャサイズは2のべき乗でalignされる。
		/// たとえば、640×480の画像ならば1024×512(32bpp)になる。
		/// よって、1024×512×4byte = 2097152 ≒ 2MB消費する。
		/// 50MBだと640×480の画像をおおよそ25枚読み込めると考えて良いだろう。
		/// </remarks>
		/// <param name="factory"></param>
		public FontRepository(TextureFactory factory)
		{
			this.factory = factory;

			ResizeFontCache(max);
		}

		private TextureFactory factory;

		/// <summary>
		/// ひとつの文字に対する情報。
		/// </summary>
		internal class Info
		{
			/// <summary>
			/// キャッシュしている文字。
			/// </summary>
			public char Letter;
			/// <summary>
			/// キャッシュしている文字のスタイル
			/// </summary>
			public int Style;
			/// <summary>
			/// 使用しているフォントローダー。
			/// </summary>
			public FontLoader FontLoader;
			/// <summary>
			/// フォントナンバー(FontLoaderにおけるナンバー)。
			/// </summary>
			public int   FontNo;
			/// <summary>
			/// そのテクスチャ(null = 未生成)。
			/// </summary>
			public ITexture Texture;
			/// <summary>
			/// このTextureを参照された最後の時間。
			/// </summary>
			public long LastAccessTime;
		}

		/// <summary>
		/// FontLoaderとその使用するナンバーを指定設定する。
		/// 
		/// ナンバーは、FontLoaderで定義ファイルを読み込んだときに、
		/// 定義ファイルに書かれている先頭から0,1,2,…とナンバリングされている。
		/// </summary>
		/// <param name="loader_"></param>
		/// <param name="no_"></param>
		/// <remarks>
		/// 以降、GetTextureでは、このフォントが読み込まれる。
		/// </remarks>
		public void SetLoader(FontLoader loader,int fontNo){
			this.loader = loader;
			this.fontNo	= fontNo;
		}

		/// <summary>
		/// フォントローダーは変更せずに、使用するフォントナンバーを設定/取得
		/// </summary>
		/// <param name="no_"></param>
		public int FontNo
		{
			set
			{
				fontNo = value;
			}
			get
			{
				return fontNo;
			}
		}


		/// <summary>
		/// 文字に対応するフォントを取得。
		/// </summary>
		/// <param name="letter"></param>
		/// <returns></returns>
		/// <remarks>
		/// キャッシュから文字をかき集めるので、ここでかき集めた
		/// 文字列(テクスチャ)はいずれ解放されるので、使用するのは
		/// その描画フレームで限りにすること。
		/// 
		/// styleで指定する値の意味はFont.Sytleと同じ。
		/// 
		/// ※ 文字はDrawBlendedUnicodeで描画している。
		/// 
		/// </remarks>
		public ITexture GetTexture(char letter, int style)
		{
			return GetTextureHelper(letter, style, true);
		}

		/// <summary>
		/// 文字に対応するフォントを取得。
		/// </summary>
		/// <param name="letter"></param>
		/// <returns></returns>
		/// <remarks>
		/// キャッシュから文字をかき集めるので、ここでかき集めた
		/// 文字列(テクスチャ)はいずれ解放されるので、使用するのは
		/// その描画フレームで限りにすること。
		/// 
		/// styleで指定する値の意味はFont.Sytleと同じ。
		/// 
		/// ※ 文字はDrawSolidUnicodeで描画している。
		/// GetTextureのDrawSolidUnicodeで描画するバージョン
		/// 
		/// </remarks>
		public ITexture GetTextureSolid(char letter, int style)
		{
			return GetTextureHelper(letter, style, false);
		}

		/// <summary>
		/// GetTexture/GetTextureSolid用のhelper
		/// </summary>
		/// <remarks>
		/// isBlendedの値に応じて、DrawBlendedUnicodeとDrawTextureSolidとを切り替えて描画する。
		/// </remarks>
		/// <param name="letter"></param>
		/// <param name="style"></param>
		/// <param name="isBlended"></param>
		/// <returns></returns>
		private ITexture GetTextureHelper(char letter,int style,bool isBlended)
		{
			if (loader == null) return null; // うんこーヽ(`Д´)ノ

			int index = int.MaxValue; // fail safe

			//	探してみる
			for (int i = 0; i < fonts.Count; ++i)
			{
				if ((fonts[i].Letter == letter) &&
					(fonts[i].Style == style) &&
					(fonts[i].FontLoader == loader) &&
					(fonts[i].FontNo == fontNo))
				{
					//	みっけ！
					index = i;
					goto Exit;
				}
			}

			//　見つからなかったので生成してみる

			ITexture texture = factory();
			if (texture == null)
				return null; // factory設定されとらん(´ω`)

			Font font = loader.GetFont(fontNo);
			if (font != null)
			{
				Surface surface =
					isBlended
					? font.DrawBlendedUnicode(letter.ToString())
					: font.DrawSolidUnicode(letter.ToString());
				texture.SetSurface(surface);
				surface.Dispose(); // 解放しとかなきゃ
			}
			else
			{
				// なんやー。読み込まれへんかったわ
				return null;// 生成失敗
			}

			// 生成できた。次に空き場所を確定させる

			if (fonts.Count == max){
				//	場所が空いてないので、一番最後に参照された
				//	オブジェクトを解放する
				long t = long.MaxValue;
				for(int i=0;i<fonts.Count;++i){
					if (fonts[i].LastAccessTime < t)
					{
						t = fonts[i].LastAccessTime;
						index = i;
					}
				}
				ITexture tt = fonts[index].Texture;
				if (tt != null)
					tt.Dispose(); // 一応解放しておく
			} else {
				index = fonts.Count;
				fonts.Add(new Info()); // ひとつ確保
			}

			// このlocal copy、最適化で消えるやろか？
			Info info = new Info();
			info.Letter = letter;
			info.FontLoader = loader;
			info.FontNo = fontNo;
			info.Style = style;
			info.Texture = texture;

			fonts[index] = info;

		Exit:
				//	これ参照したので、時刻をずらしておく。
			fonts[index].LastAccessTime = ++time;
			return fonts[index].Texture;
		}

		/// <summary>
		/// 文字列に対する文字ベクタを取得。
		/// styleで指定する値の意味はFont.Sytleと同じ。
		/// </summary>
		/// <remarks>
		/// ※ 文字はDrawBlendedUnicodeで描画している。
		/// </remarks>
		/// <param name="str"></param>
		/// <returns></returns>
		public Textures GetTexture(string str, int style)
		{
			return GetTextureHelper(str, style, true);
		}

		/// <summary>
		/// 文字列に対する文字ベクタを取得。
		/// styleで指定する値の意味はFont.Sytleと同じ。
		/// </summary>
		/// <remarks>
		/// ※ 文字はDrawSolidUnicodeで描画している。
		/// </remarks>
		/// <param name="str"></param>
		/// <returns></returns>
		public Textures GetTextureSolid(string str, int style)
		{
			return GetTextureHelper(str, style, false);
		}

		/// <summary>
		/// GetTexture/GetTextureSolid用のhelper
		/// </summary>
		/// <remarks>
		/// isBlendedの値に応じて、DrawBlendedUnicodeとDrawTextureSolidとを切り替えて描画する。
		/// </remarks>
		/// <param name="str"></param>
		/// <returns></returns>
		private Textures GetTextureHelper(string str, int style,bool isBlended) {
			if (loader == null) return null; // うんこーヽ(`Д´)ノ

			Textures tv = new Textures();
			for (int i = 0; i < str.Length; ++i) {
				ITexture texture = isBlended ? GetTexture(str[i], style) : GetTextureSolid(str[i], style);

				if (texture == null)
					continue; // renderingに失敗したfontは飛ばす

				tv.Add(texture,(int)tv.Width,0);
				//	右に連結していく
			}
			tv.Update();	// これ呼び出しておかないと
			//	情報が更新されないんだな(´Д`)
			return tv;
		}

		/// <summary>
		/// 保持しているフォントを解放する。
		/// </summary>
		public void Release() {
			if (fonts != null) {
				//	明示的に解放しておくか..
				for (int i = 0; i < fonts.Count; ++i)
				{
					ITexture t = fonts[i].Texture;
					if (t != null) t.Dispose();
				}
				fonts.Clear();
			}
		}

		/// <summary>
		/// 現在選択されているフォントを取得
		/// </summary>
		/// <returns></returns>
		public Font CurrentFont
		{
			get { return loader.GetFont(fontNo);  }
		}

		/// <summary>
		/// Dispose後の再利用はできない。
		/// 単に解放するだけならばReleaseを呼び出して。
		/// </summary>
		public void Dispose()
		{
			Release();
		}

		private List<Info>	fonts = new List<Info>();	//	使用しているフォントキャッシュ
		private FontLoader loader;		//	読み込み
		private int		fontNo;			//	選択されているフォントナンバー
		private int		time;			//	時刻

	}
}

