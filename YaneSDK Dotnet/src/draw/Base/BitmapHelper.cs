using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

using Yanesdk.Ytl;
using Yanesdk.System;

namespace Yanesdk.Draw
{
	/// <summary>
	/// GDI+のBitmapに FileSysを経由して画像を読み込んだりする。
	/// こうすることにより、独自アーカイバからの画像の読み出しが
	/// 可能になる。
	/// </summary>
	public class BitmapHelper
	{
		/// <summary>
		/// 指定したファイルから画像を読み込み、その
		/// System.Drawing.Bitmap を返す。
		/// 
		/// 画像の読み込み時には、System.FileSysを
		/// 利用するので、このファイルシステムにadd onされた
		/// アーカイバ等のなかのファイルであっても読める。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		static public Bitmap LoadToBitmap(string filename)
		{
			try
			{
				// 実在するなら、そのまま読み込めば良い
				string realfile = FileSys.IsRealExist(filename);
				if (realfile!=null)
					return new Bitmap(realfile);
	
				// memory stream経由で読み込むように修正

				byte[] data = FileSys.Read(filename);
				if ( data == null )
					return null;

				using ( MemoryStream stream = new MemoryStream(data) )
				{
					return new Bitmap(stream);
				}
			}
			catch { }
			return null;

			/*
			Surface surface = new Surface();
			if (surface.Load(filename) != YanesdkResult.NoError)
				return null;

			// Bitmap.FromHbitmap(surface.HBitmap);のほうがいいか？

			int w = surface.Width;
			int h = surface.Height;

			Bitmap bitmap = new Bitmap(w,h);
			BitmapData bmpdata;
			if (surface.CheckRGB888())
			{
				bmpdata = bitmap.LockBits(new Rectangle(0, 0, w, h),
					ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

				// 画像コピー
				unsafe
				{
					// 最悪なのことにalignされている(´ω`)

					byte* dst = (byte*)bmpdata.Scan0;
					int dst_pitch = bmpdata.Stride;
					byte* src = (byte*)surface.Pixels;
					int src_pitch = surface.Pitch;

					for (int y = 0; y < h; ++y)
					{
						for (int i = 0; i < w * 3; i += 3)
						{
							// しかもR,G,B逆かよ！(｀ω´)
							dst[i + 0] = src[i + 2];
							dst[i + 1] = src[i + 1];
							dst[i + 2] = src[i + 0];
						}
						src += src_pitch;
						dst += dst_pitch;
					}
				}
			}
			else
			{
				bmpdata = bitmap.LockBits(new Rectangle(0, 0, w, h),
					ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				// 画像コピー
				unsafe
				{
					// 最悪なのことにalignされている(´ω`)

					byte* dst = (byte*)bmpdata.Scan0;
					int dst_pitch = bmpdata.Stride;
					byte* src = (byte*)surface.Pixels;
					int src_pitch = surface.Pitch;

					for (int y = 0; y < h; ++y)
					{
						for (int i = 0; i < w * 4; i += 4)
						{
							// しかもR,G,B逆かよ！(｀ω´)
							dst[i + 0] = src[i + 2];
							dst[i + 1] = src[i + 1];
							dst[i + 2] = src[i + 0];
							dst[i + 3] = src[i + 3];
						}
						src += src_pitch;
						dst += dst_pitch;
					}
				}
			}

			bitmap.UnlockBits(bmpdata);

			return bitmap;
			 */
		}

		/// <summary>
		/// Bitmap画像をSurfaceに変換(このあとTexture化したりするのに使える)
		/// </summary>
		/// <param name="bmp"></param>
		/// <param name="surface"></param>
		/// <returns></returns>
		public static YanesdkResult BitmapToSurface(Bitmap bmp,out Surface surface)
		{
			int w = bmp.Width;
			int h = bmp.Height;

			if (w == 0 || h == 0)
			{
				surface = null;
				return YanesdkResult.PreconditionError;
			}

			BitmapData bmpdata;

			bmpdata = bmp.LockBits(new Rectangle(0, 0, w, h),
					ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			if (bmpdata==null)
			{
				surface = null;
				return YanesdkResult.PreconditionError;
			}

			surface = new Surface();
			surface.CreateDIB(w, h, true); // 32bpp ARGBになってんでない？

			// 画像コピー
			unsafe
			{
				// 最悪なのことにalignされている(´ω`)

				byte* src = (byte*)bmpdata.Scan0;
				int src_pitch = bmpdata.Stride;
				byte* dst = (byte*)surface.Pixels;
				int dst_pitch = surface.Pitch;

				for (int y = 0; y < h; ++y)
				{
					for (int i = 0; i < w * 4; i += 4)
					{
						// しかもR,G,B逆かよ！(｀ω´)
						dst[i + 0] = src[i + 2];
						dst[i + 1] = src[i + 1];
						dst[i + 2] = src[i + 0];
						dst[i + 3] = src[i + 3];
					}
					src += src_pitch;
					dst += dst_pitch;
				}
			}
			bmp.UnlockBits(bmpdata);

			return YanesdkResult.NoError;
		}
	}
}
