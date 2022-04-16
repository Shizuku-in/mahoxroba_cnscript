using System;
using Yanesdk.Sdl.MacAPI;

namespace Yanesdk.System
{
	/// <summary>
	/// PlatformID。Platformクラス内で用いる。
	/// このLinuxにMacOSは含まない。
	/// </summary>
	public enum PlatformID
	{
		Windows,
		Linux,		//	MacOSは含まない
		MacOS,
	}

	/// <summary>
	/// プラットフォームを判定するメソッド群
	/// </summary>
	public class Platform
	{
		/// <summary>
		/// Macかどうか判定する
		/// </summary>
		public static bool IsMac
		{
			get
			{
				if (isMac == null)
				{
					// Linux環境ではないのならば試すまでもなくMacではない
					if (!IsLinux)
					{
						isMac = false;
					}
					else
					{
						isMac = true;
						try
						{
							// 試しにAPIをひとつ呼び出してみる。
							IntPtr ptr = IntPtr.Zero;
							MacAPI.GetCurrentProcess(ref ptr);
						}
						catch
						{
							// 例外が出たということはMacではないと判断
							isMac = false;
						}
					}
				}
				return isMac.Value;
			}
		}
		/// <summary>
		/// Macかどうかをテストした結果。nullであれば未テスト。
		/// </summary>
		private static bool? isMac = null;

		/// <summary>
		/// Linux環境(MacOSを含む)なのか
		/// 
		/// PlatformIDのほうのLinuxはMacOSを含まないので注意すること。
		/// </summary>
		public static bool IsLinux
		{
			get
			{
				return global::System.Environment.OSVersion.Platform
					== global::System.PlatformID.Unix;
			}
		}

		/// <summary>
		/// Windows環境なのか
		/// </summary>
		public static bool IsWindows
		{
			get { return !IsLinux; }
		}

		/// <summary>
		/// 現在実行されているプラットフォームを返す
		/// </summary>
		public static PlatformID PlatformID
		{
			get
			{
				if (platformID == null)
				{
					if (IsLinux)
					{
						if (IsMac)
							platformID = PlatformID.MacOS;
						else
							platformID = PlatformID.Linux;
					}
					else
						platformID = PlatformID.Windows;
				}
				return platformID.Value;
			}
		}
		/// <summary>
		/// プラットフォームをテストした結果。nullであれば未テスト。
		/// </summary>
		private static PlatformID? platformID = null;
	}
}
