using System;
using Yanesdk.Ytl;
using Yanesdk.System;

namespace Yanesdk.Draw
{
	/// <summary>
	/// 文字フォントのcacheシステム。
	/// </summary>
	/// <remarks>
	/// <para>
	/// Font を CacheLoader で cache する仕組みです。
	/// </para>
	/// <para>
	/// 文字フォントはリソースが大きいので、ゲーム中に読み込みや解放を
	/// 繰り返すべきではないと考えます
	/// </para>
	/// <para>
	/// そこで、一度読み込んだフォントを(読み込みフォントの総容量が一定に
	/// 達するまでは)解放しない仕組みが必要となります。
	/// </para>
	/// <para>
	/// このクラスは、その機能を実現します。
	/// <code>
	/// YanesdkResult LoadDefFile(char[] filename);
	/// </code>
	/// で読み込む定義ファイルは、
	/// <list type="bullet">
	///	<item><description>msgothic.ttc , 16 , 0</description></item>
	///	<item><description>msmincho.ttc , 24 , 1</description></item>
	///	<item><description>msmincho.ttc , 40 , 0</description></item>
	///	<item><description>msmincho.ttc , 40 , 2 , 5</description></item>
	///	</list>
	///	のように書き、ひとつ目は、ファイル名、2,3つ目は
	///	option1,2である。option1はフォントサイズ,option2はフォントのindex
	///	(Font.Loadの時に指定するindex)を指定する。これらは指定しない場合0を
	///	意味するのでoption1は必ず指定する必要がある。
	/// 
	/// 4つ目のoptionは、読み込み番号である。上記の例であれば
	///		GetFont(5)
	/// とした場合、「msmincho.ttc , 40 , 2」のフォントが読み込まれる。
	///	</para>
	/// <para>
	/// また、フォントは文字サイズごとに生成する必要がある。そこで頻繁に
	/// 文字サイズを変更するとリソースの再読み込みで結構時間がかかることになる。
	/// これを回避するためには、 TextureFontRepository を用いると良い。
	/// </para>
	/// <para>
	///	このクラスでキャッシュするフォントのサイズは、ディフォルトでは32MB。
	/// MS明朝,MSゴシックは10M程度あるので、32MBではひょっとすると少ないかも
	/// 知れないが。
	/// </para>
	/// </remarks>
	public class FontLoader : CachedObjectLoader
	{
		#region ctor
		/// <summary>
		/// 
		/// </summary>
		public FontLoader()
		{
		}
		#endregion

		#region methods
		/// <summary>
		///	指定された番号のオブジェクトを構築して返す
		/// </summary>
		/// <remarks>
		/// 暗黙のうちにオブジェクトは構築され、Loadされる。
		///	定義ファイル上に存在しない番号を指定した場合はnullが返る
		///	ファイルの読み込みに失敗した場合は、nullは返らない。
		///	定義ファイル上に存在しない番号のものを読み込むことは
		///	考慮しなくてもいいと思われる．．。
		/// </remarks>
		/// <param name="no"></param>
		/// <returns></returns>
		public Font GetFont(int no)
		{
			return GetCachedObjectHelper(no) as Font;
		}
		#endregion

		#region overridden from base class(CachedObjectLoader)

		/// <summary>
		/// Fontのdefファイルはオプションパラメータを2つとる。
		/// </summary>
		protected override int OptNum
		{
			get { return 2; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		protected override ICachedObject OnDefFileLoaded(FilenamelistReader.Info info)
		{
			ICachedObject obj = new Font(); // Factory();

			// info.name, info.opt1 を渡したい

			// fontsizeのdefault値は14扱い。
			if (info.opt1 == int.MinValue)
				info.opt1 = 14;

			// fontindexのdefault値は0扱い。
			if (info.opt2 == int.MinValue)
				info.opt2 = 0;

			obj.Construct(new FontConstructAdaptor(info.name, info.opt1,info.opt2));
			return obj;
		}
		#endregion
	}

	/// <summary>
	/// FontLoaderのSmartLoader版
	/// </summary>
	/// <remarks>
	/// SmartFontLoaderを用いるときは、
	/// FontLoaderが勝手にFontを解放するとSmartFontLoaderが困るので
	/// FontLoader.Disposeは呼び出さないこと。
	/// </remarks>
	public class SmartFontLoader : CachedObjectSmartLoader<FontLoader> {}

	/// <summary>
	/// Font系のclassがCachedObject.Reonstructを実装するときに使うadaptor
	/// </summary>
	public class FontConstructAdaptor
	{
		public FontConstructAdaptor(string filename, int fontsize, int fontindex)
		{
			FileName = filename;
			FontSize = fontsize;
			FontIndex = fontindex;
		}

		public string FileName;
		public int FontSize;
		public int FontIndex;
	}

}
