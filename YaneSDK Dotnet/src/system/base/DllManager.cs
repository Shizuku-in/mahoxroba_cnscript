using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

using Yanesdk.System;
using Yanesdk.Ytl;

namespace Yanesdk.System
{
	/// <summary>
	/// dllの読み込みwrapper
	/// </summary>
	public class DllManager
	{
		#region const(DLLの配置位置などはこれを変更してください)

		/// <summary>
		/// lib/配下に配置したいときは、"lib\\"と書くべし。
		/// 
		/// defaultでWindows環境では"lib\\x86\\"(32bit環境),"lib\\x64\\"(64bit環境)
		/// Linux/MacOS環境では""
		/// 
		/// Windows環境で動作させる場合に限り、起動直後に書き換えても良い。
		/// 
		/// 例)
		///  if(Platform.IsWindows)
		///    DllManager.DLL_CURRENT = ((IntPtr.Size == 4) ?
		///      "sdl_lib\\x86\\" : "sdl_lib\\x64\\"); // Windows(x86,x64)
		/// 
		/// Linux環境ではこれは書き変えずにYanesdk.dll.configのほうで変更すること。
		/// 
		/// </summary>
		public static string DLL_CURRENT =
			Platform.IsLinux?"":								// Linux
			((IntPtr.Size == 4) ? "lib\\x86\\" : "lib\\x64\\"); // Windows(x86,x64)
		// DLLは事前にメモリに読み込んでおけば、次に同名のdllを読み込もうとしても
		// 読み込まないようにOS側で処理してくれるようになっていることを前提とした実装である。

		#endregion

		/// <summary>
		/// dllファイルを読み込む。
		/// 
		/// dllファイルを事前に読み込むことで、dllからdllの間接的な読み込みを防ぐ。
		/// (SDLのsmpeg.dllなどはこれをしないと配置フォルダを変更できないので)
		/// </summary>
		/// <remarks>
		/// linux環境下ではこのメソッドは必ずエラーを返す。
		/// ここで指定するdllファイル名には末尾の".dll"は不要。
		/// </remarks>
		/// <param name="path"></param>
		/// <param name="dllFileName"></param>
		/// <returns></returns>
		public YanesdkResult LoadLibrary(string path, string dllFileName)
		{
			YanesdkResult result;

			// すでに読み込んでいるのか？
			if (filelists.ContainsKey(dllFileName))
				result = YanesdkResult.AlreadyDone;
			else
				result = LoadLibrarySafe(path, dllFileName+".dll");

			// 正常に読み込めたならば、ファイルリストに書き出しておく。
			if (result == YanesdkResult.NoError)
				filelists.Add(dllFileName, null);

			return result;
		}

		/// <summary>
		/// LoadLibrary(DLL_CURRENT, dllFileName);を行なう
		/// ここで指定するdllファイル名には末尾の".dll"は不要。
		/// </summary>
		/// <param name="dllFileName"></param>
		/// <returns></returns>
		public YanesdkResult LoadLibrary(string dllFileName)
		{
			return LoadLibrary(DLL_CURRENT, dllFileName);
		}

		/// <summary>
		/// このクラスのsingletonなインスタンス
		/// </summary>
		public static DllManager Instance
		{
			get { return Singleton<DllManager>.Instance; }
		}

		/// <summary>
		/// 一度読み込んだdllは二度読み込まないようにするために読み込んだdllのリストを
		/// 保持しておく。
		/// </summary>
		private Dictionary<string, object> filelists = new Dictionary<string,object>();

		// linux系の場合、どうしたらええのん？(´ω`)
		[DllImport("Kernel32.dll", EntryPoint="LoadLibrary",CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern IntPtr LoadLibrary_(string name);

		/// <summary>
		/// DllManagerがdllを読み込むときのディレクトリポリシー。
		/// 
		/// あるファイルを読み込むとき、
		///		・実行ファイル相対で読み込む(default)
		///		・起動時のworking directory相対で読み込む
		///		・現在のworking directory相対で読み込む
		/// の3種から選択できる。
		/// </summary>
		public static CurrentDirectoryEnum DirectoryPolicy = CurrentDirectoryEnum.ExecuteDirectory;

		/// <summary>
		/// dllファイルを事前に読み込むことで、dllからdllの間接的な読み込みを防ぐ。
		/// (SDLのsmpeg.dllなどはこれをしないと配置フォルダを変更できないので)
		/// </summary>
		private static YanesdkResult LoadLibrarySafe(string path,string filename)
		{
			YanesdkResult result;

			if (!System.Platform.IsWindows)
			{
				// linuxの場合どうやっていいのかは知らん(｀ω´)
				// 必要があるならなんとかしてくれ(´ω`)

				result = YanesdkResult.HappenSomeError;
			}
			else
			{
				string dllPathName = FileSys.ConcatPath(path, filename);

				// いったんCurrent Directoryを変更しないと
				// smpeg.dllのようにDllMainでおかしなことをしているdllは読み込めない。

				// dllは実行ファイル相対で配置されると仮定できる。

				// CurrentDirectoryを一時的に変更する
				{
					using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(DllManager.DirectoryPolicy))
					{
						// そのdllファイルが存在するのか事前に調べておく。
						if (FileSys.IsExist(dllPathName))
						{
							IntPtr handle = LoadLibrary_(dllPathName);
							if (handle == IntPtr.Zero)
							{
								// Console.WriteLine("dllの読み込みに失敗 : " + dllFileName );
								//	Debug.Fail(file + " の読み込みに失敗: " + 
								//		(new global::System.ComponentModel.Win32Exception()).Message);
								result = YanesdkResult.HappenSomeError; // なんか致命的なエラー(´ω`)
							}
							else
								result = YanesdkResult.NoError;
						}
						else
							result = YanesdkResult.FileNotFound;
					}
				}
			}
			return result;
		}

		/// <summary>
		/// yanesdk内で使用する
		/// dllのsearch pathを設定する。
		/// 
		/// 必要であればまず最初に呼び出すように。
		/// 
		/// [使わないで]
		/// 普通は、これでDllのPathを変更できるのだが、
		/// SDL.dllは内部的にSDL_image.dllを呼び出したりする。
		/// この場合、SDL.dllが同じpathにSDL_image.dllが存在すると
		/// 勘違いして読み込もうとするので、おかしなことになる。
		/// よって、SDL.dllを使うなら、このメソッドを呼び出すべきではない。
		/// 
		/// せいぜい、このメソッドをコピペして、Yanesdk.dllの存在する
		/// フォルダを指定するような使い方をするぐらいだろう。
		/// 
		/// </summary>
		/// <param name="lib"></param>
		public static void SetDllPath(string lib)
		{
			AppDomain.CurrentDomain.AssemblyResolve
			+= new ResolveEventHandler(
				delegate(object sender, ResolveEventArgs args)
				{
					string[] files = args.Name.Split(new char[] { ',' });
					// 無限再帰になってたりして？
					if (files[0].Length >= 256)
						throw new DllNotFoundException("DLL: " + lib + Path.PathSeparator + files[0] + ".dllが見当たらない");
					byte[] dll = File.ReadAllBytes(lib + Path.DirectorySeparatorChar + files[0] + ".dll");
					Assembly asm = Assembly.Load(dll);
					return asm;
				}
			);
		}
	
	}
}
