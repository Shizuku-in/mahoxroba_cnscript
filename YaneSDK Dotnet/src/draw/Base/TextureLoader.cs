using System;
using System.Collections.Generic;
using System.Text;

using Yanesdk.Ytl;
using Yanesdk.System;
using System.Diagnostics;

namespace Yanesdk.Draw
{
	/// <summary>
	/// ITexture派生クラスを必要に応じてfactoryで生成して、
	/// ファイルを読み込み、返す。
	/// 
	/// また、CacheSystemクラスを利用して、cacheを行なう。
	/// SoundLoaderのTexture版だと考えると良いだろう。
	/// 
	/// SoundLoaderの説明も読むこと。
	/// </summary>
	/// <remarks>
	/// SmartTextureLoaderを用いるときは、
	/// TextureLoaderが勝手にTextureを解放するとSmartTextureLoaderが困るので
	/// TextureLoader.Disposeは呼び出さないこと。
	/// </remarks>
	public class TextureLoader : CachedObjectLoader
	{
		#region ctor
		/// <summary>
		/// TextureFactoryを渡さないコンストラクタ。
		/// Factoryプロパティでfactoryのdelegateを渡して。
		/// 
		/// ※ defaultではGlTextureをnewするようにしてある
		/// </summary>
		public TextureLoader()
		{
			Factory = delegate { return new GlTexture(); };
		}
		
		/// <summary>
		/// コンストラクタで、ITextureCachedObject派生クラスを生成するfactoryを渡してやる。
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
		public TextureLoader(CachedObjectFactory factory)
		{
			Factory = factory;
		}
		#endregion

		#region methods
		/// <summary>
		///	指定された番号のオブジェクトを構築して画像を読み込み返す
		/// </summary>
		/// 暗黙のうちにオブジェクトは構築され、Loadされる。
		///	定義ファイル上に存在しない番号を指定した場合はnullが返る
		///	ファイルの読み込みに失敗した場合は、nullは返らない。
		///	定義ファイル上に存在しない番号のものを読み込むことは
		///	考慮しなくてもいいと思われる．．。
		/// 
		/// Loadするときに、Win32Windowなどでは、Screen.Selectされて
		/// いないとまずいので、
		/// 
		/// ITexture GetTexture(int no){
		///     window.Screen.Select();
		///     ITexture tex = loader.GetTexture(no);
		///     window.Screen.Unselect();
		///     return tex;
		/// }
		/// と書くべき。すなわち、GetTextureを呼び出すときは、
		/// Screen.Select～Unselect/Updateで囲まれた領域でなければならない。
		/// 
		/// <param name="no"></param>
		/// <returns></returns>
		public ITexture GetTexture(int no)
		{
			return GetCachedObjectHelper(no) as ITexture;
		}
		#endregion

		#region properties
		/// <summary>
		/// 抜き色(カラーキー)の指定。
		/// LoadDefFileで定義ファイルを読み込む前に設定しなければ効果が無いので注意。
		/// </summary>
		public ColorKey ColorKey
		{
			get { return colorKey; }
			set
			{
				Debug.Assert(colorKey != null, "ColorKeyとしてnullが設定された");
				colorKey = value;
			}
		}
		private ColorKey colorKey = new ColorKey();
		#endregion

		#region overridden from base class
		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		protected override ICachedObject OnDefFileLoaded(FilenamelistReader.Info info)
		{
			if (Factory == null)
				return null; //  YanesdkResult.PreconditionError; // factoryが設定されていない

			ICachedObject obj = Factory();

			// info.name, ColorKey を渡したい
			obj.Construct(new TextrueConstructAdaptor(info.name,ColorKey));

			return obj;
		}
		#endregion
	}

	/// <summary>
	/// TextureLoaderのSmartLoader版
	/// 
	/// defaultではGlTextureをnewする。
	/// </summary>
	/// <remarks>
	/// SmartTextureLoaderを用いるときは、
	/// TextureLoaderが勝手にTextureを解放するとSmartTextureLoaderが困るので
	/// TextureLoader.Disposeは呼び出さないこと。
	/// </remarks>
	public class SmartTextureLoader : CachedObjectSmartLoader<TextureLoader>
	{
		/// <summary>
		/// ディフォルトではGlTextureを生成するFactoryを設定してある。
		/// </summary>
		public SmartTextureLoader()
		{
			Factory = delegate { return new GlTexture(); };
		}
	}

	/// <summary>
	/// Texture系のclassがCachedObject.Reconstructを実装するときに使うadaptor
	/// </summary>
	internal class TextrueConstructAdaptor
	{
		public TextrueConstructAdaptor(string filename, ColorKey colorKey)
		{
			this.FileName = filename;
			this.ColorKey = colorKey;
		}
		public string FileName;
		public ColorKey ColorKey;
	}

}
