using System;
using System.IO;

namespace Yanesdk.Math {

	/// <summary>
	/// いわゆるCRC32を求める
	/// </summary>
	[CLSCompliant(false)]
	public sealed class CRC32
	{
		/// <summary>
		/// 32bit mask
		/// </summary>
		private static UInt32 HighBitMask = 0xFFFFFFFF;
		
		/// <summary>
		/// ファイル読み込みのときのバッファサイズ
		/// </summary>
		private const int BUFFER_SIZE = 1024;
		
		/// <summary>
		/// 32ビットのCRC生存時間。
		/// </summary>
		private UInt32 LifetimeCRC = 0;

		/// <summary>
		/// CRCテーブル
		/// </summary>
		private static UInt32[] CRCTable;

		static CRC32()
		{
			unchecked
			{
				// この多項式はPKZIP, WINZIPやTCP packetsのなかで使われるCRC32
				UInt32 CRC32_POLYNOMIAL = 0xEDB88320;

				CRCTable = new UInt32[256];

				// temp crc
				UInt32 crc;

				for (UInt32 i = 0; i < 256; ++i)
				{
					crc = i;

					for (int j = 0; j < 8; ++j)
					{
						if ((crc & 1) == 1)
							crc = (crc >> 1) ^ CRC32_POLYNOMIAL;
						else
							crc >>= 1;
					}

					CRCTable[i] = crc;
				}
			}
		}

		/// <summary>
		/// CRCの最終的な値
		/// </summary>
		public UInt32 FinalValue 
		{
			get 
			{
				return LifetimeCRC;
			}
		}
		
		/// <summary>
		/// CRC32のリセット
		/// </summary>
		public void Reset() 
		{ 
			LifetimeCRC = 0; 
		}

		//	バッファからCRCのupdate
		public UInt32 Update(byte[] buffer)
		{
			UInt32 crc = HighBitMask;

			int count = buffer.Length;

			for (int i = 0; i < count; i++)
				crc = ((crc) >> 8) ^ CRCTable[(buffer[i]) ^ ((crc) & 0x000000FF)];
						
			crc = ~crc;
			LifetimeCRC ^= crc;
			return crc;
		}

		public UInt32 Update(byte b, UInt32 crc)
		{
			crc = ( ( crc ) >> 8 ) ^ CRCTable[( b ) ^ ( ( crc ) & 0x000000FF )];
			return crc;
		}

		/// <summary>
		/// ファイルからCRC32を計算。返し値がCRC32。
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public UInt32 Update( global::System.IO.FileStream file )
		{
			unchecked
			{	
				byte[] buffer = new byte[BUFFER_SIZE];
				int readSize = BUFFER_SIZE;

				UInt32 crc = HighBitMask;

				try 
				{
					file.Lock( 0, file.Length );

					int count = file.Read(buffer, 0, readSize);
					while (count > 0)
					{
						
						for (int i = 0; i < count; i++)
							crc = ((crc) >> 8) ^ CRCTable[(buffer[i]) ^ ((crc) & 0x000000FF)];
						//	↑は crc = update_crc32(buffer[i], crc); と等価。
						
						count = file.Read(buffer, 0, readSize);
					}

					file.Unlock( 0, file.Length );

				}
				catch ( global::System.Exception e )
				{
					throw e;
				}
				
				crc = ~crc;
				LifetimeCRC ^= crc;
				return crc;
			}
		}

		private UInt32 Update( string FullPathToFile )
		{
			unchecked
			{	
				global::System.Text.UnicodeEncoding myEncoder = new global::System.Text.UnicodeEncoding();

				// yanesdkのFileSys.GetPureFileNameを呼び出したほうがいいんだけれど…。

				int index = FullPathToFile.LastIndexOf( Path.DirectorySeparatorChar );
				string strFileName = FullPathToFile.Substring( index + 1 );

				int count = myEncoder.GetByteCount( strFileName );
				byte[] buffer = new byte[count];
				buffer = myEncoder.GetBytes(strFileName);

				UInt32 crc = HighBitMask;

				for (int i = 0; i < count; i++)
					crc = ((crc) >> 8) ^ CRCTable[(buffer[i]) ^ ((crc) & 0x000000FF)];
				
				crc = ~crc;
				LifetimeCRC ^= crc;
				return crc;
			}
		}

		private unsafe UInt32 ProcessBuffer( ref byte[] buffer, ref UInt32 crc )
		{
			for (int i = 0; i < buffer.Length; i++)
				crc = ((crc) >> 8) ^ CRCTable[(buffer[i]) ^ ((crc) & 0x000000FF)];

			return crc;
		}
	}
}