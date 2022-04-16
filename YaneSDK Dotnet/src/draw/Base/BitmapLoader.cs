using System;
using System.Drawing;

using Yanesdk.Ytl;
using Yanesdk.System;

namespace Yanesdk.Draw
{
	/// <summary>
	/// Bitmapオブジェクトは、リサイズ等を行なうならオブジェクト自体を
	/// 作り直す必要があるが、オブジェクトへの参照が変更されると
	/// 大変使いにくい。そこで、オブジェクトへの参照が変更されない
	/// BitmapのWrapperが必要になる。
	/// 
	/// ここで得たBitmapのサイズを変更したりしないこと。
	/// このBitmapのメモリ自体は、VideoMemoryから減算する
	/// (ほんとは.NET Framework2.0の実装ではVideoMemoryではないんだけども…)
	/// </summary>
	internal class BitmapWrapper : CachedObject , ILoader, IDisposable
	{
		public Bitmap Bitmap
		{
			get { return bitmap; }
		}
		private Bitmap bitmap;

		public BitmapWrapper()
		{
			loaded = false;
			UnmanagedResourceManager.Instance.VideoMemory.Add(this);
		}

		public void Release()
		{
			if (Bitmap != null)
			{
				Bitmap.Dispose();
				bitmap = null;
				loaded = false;
				fileName = null;

				CacheSystem.OnResourceChanged(this);
			}
		}

		public void Dispose()
		{
			Release();
			UnmanagedResourceManager.Instance.VideoMemory.Remove(this);
		}

		public YanesdkResult Load(string filename)
		{
			bitmap = BitmapHelper.LoadToBitmap(filename);
			if (bitmap == null) return YanesdkResult.FileReadError;
			// 一回読み込みに失敗したら、アクセス毎に読み込みに行くのを
			// 防止するために、もう読み込まないほうがいいと思うのだが

			loaded = true;
			fileName = filename;

			CacheSystem.OnResourceChanged(this);

			return YanesdkResult.NoError;
		}

		public bool Loaded
		{
			get { return loaded; }
		}
		private bool loaded;

		public string FileName
		{
			get { return fileName; }
		}
		private string fileName;

		public override long ResourceSize
		{
			get
			{
				if (!Loaded) return 0;
				int bpp = Bitmap.GetPixelFormatSize(bitmap.PixelFormat);
				return bitmap.Height * bitmap.Width * bpp;
			}
		}
	}

	/// <summary>
	/// .NETのBitmapをCacheSystemを使って、Cacheするためのもの。
	/// SoundLoader,TextureLoaderみたいなもの。
	/// </summary>
	/// <remarks>
	/// defaultでは64MBのbitmap cache。
	/// </remarks>
	public class BitmapLoader : CachedObjectLoader
	{
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
		public Bitmap GetBitmap(int no)
		{
			BitmapWrapper t =  GetCachedObjectHelper(no) as BitmapWrapper;
			if (t != null)
			{
				// アクセスされたことを示すマーカーをつける
				t.CacheSystem.OnAccess(t);
				return t.Bitmap;
			}
			else
				return null;
		}

	}

	/// <summary>
	/// BitmapLoaderのSmartLoader版
	/// </summary>
	/// <remarks>
	/// SmartBitmapLoaderを用いるときは、
	/// BitmapLoaderが勝手にTextureを解放するとSmartBitmapLoaderが困るので
	/// BitmapLoader.Disposeは呼び出さないこと。
	/// </remarks>
	public class SmartBitmapLoader : CachedObjectSmartLoader<BitmapLoader>
	{ }

}
