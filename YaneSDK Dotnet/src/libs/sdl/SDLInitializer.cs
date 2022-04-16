using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Sdl;
using Yanesdk.Ytl;
using Yanesdk.Sound;
using Yanesdk.System;

// MacのSDLはCocoaフレームワーク依存
using Yanesdk.Sdl.MacAPI;
using System.Reflection;
using System.Diagnostics;

// Yanesdk内のSDL名前空間を作るとまぎらわしくなる。
namespace Yanesdk.Sdl
{
	/// <summary>
	/// SDLの初期化用クラス。実際は、この派生クラス(これがRefSingletonへのadaptor
	/// になっている)をRefSingletonに食わせて使うと良い。
	/// 
	/// 使用例は、VisutalStudioの「すべての参照を検索」を用いて
	/// 調べてください。
	/// </summary>
	public class SDL_Initializer : IDisposable
	{
		#region SDLのファイル名
		// (配置場所はYanesdk.System.DllManager.DLL_CURRENT)
		public const string DLL_SDL = "SDL";

		// Sound関連のDLLのファイル名。
		public const string DLL_SDL_MIXER = "SDL_mixer";
		public const string DLL_OGG = "ogg";
//		public const string DLL_VORBIS = "vorbis";
		public const string DLL_VORBISFILE = "vorbisfile";
		public const string DLL_SMPEG = "smpeg";
		
		// 画像関係のDLLファイル名
		public const string DLL_SDL_IMAGE = "SDL_image";
		public const string DLL_ZIP = "zlib1";
		public const string DLL_PNG = "libpng12";
		public const string DLL_JPEG = "jpeg";
		public const string DLL_TIFF = "libtiff";

		// 文字フォント関連のDLLファイル名
		public const string DLL_SDL_TTF = "SDL_ttf";

#if TESTCODE
		// 上記のDLLがLinux,MacOS環境でどんな名前になるのか、そのmapping table
		public static string[][] DLL_MAPPER = 
		{
			//  Windows , Linux(除くMacOS) , MacOSの順番で記述する
			new string[]{ DLL_ZIP , "z" , "z" },				// Linux,MacOSではzlibは"libz.so"である
			new string[]{ OpenGl.Gl.DLL_GL , "GL" , "GL" },		// Linux,MacOSではOpenGL32.dllは"libGL.so"である
			new string[]{ OpenGl.Glu.DLL_GLU , "GLU" , "GLU" }	// Linux,MacOSではGlu32.dllは"libGLU.so"である
		};
		
		/// <summary>
		/// 上記のDLLが存在しないときはこのメソッドが返すものを用いる
		/// </summary>
		/// <param name="dllName"></param>
		/// <returns></returns>
		private string GetMappedDllName(string dllName)
		{
			string mappedName = null;
			if (!Yanesdk.System.Platform.IsWindows)
			{
				// Windows環境でないならば対応するやつを探してくる。
				foreach (string[] mapper in DLL_MAPPER)
				{
					if (dllName == mapper[0])
					{
						mappedName = mapper[Yanesdk.System.Platform.IsLinux ? 1 : 2];
						break;
					}
				}
			}

			//	mappedName = "lib/x86/SDL";

			return mappedName;
		}
#endif

		#endregion

		/// <summary>
		/// 初期化するコンホーネントを引数として渡す。
		/// 例) SDL.SDL_INIT_VIDEO
		/// 
		/// 引数は、CLS準拠にするためにuintではなくlongにしてある。
		/// </summary>
		/// <param name="initComponent"></param>
		public SDL_Initializer(long initComponent)
		{
			this.initComponent = (uint)initComponent;

			// 一度だけ初期化を行なう
			if (isFirst)
			{
				isFirst = false;

				// これ、ここまで書いておいて何だが、AssemblyResolveは
				// unamanged dllに対しては使えないようだ(´ω`)
				/*
				// DLLが見つからないときに別のDLLをサーチする
				AppDomain.CurrentDomain.AssemblyResolve
				+= new ResolveEventHandler(
					delegate(object sender, ResolveEventArgs args)
					{
						string[] files = args.Name.Split(new char[] { ',' });

						string unfoundDll = files[0];
						string mappedDll = GetMappedDllName(unfoundDll);
						if (mappedDll==null)
							throw new DllNotFoundException("DLL : " + Path.PathSeparator + unfoundDll + "が見当たらない。");

						using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(DllManager.DirectoryPolicy))
						{
							byte[] dll = File.ReadAllBytes(Path.DirectorySeparatorChar + mappedDll);
							Assembly asm = Assembly.Load(dll);
							return asm;
						}
					}
				);
				*/

				if (Yanesdk.System.Platform.IsMac)
				{
					Debug.Assert(macAf == null, "Mac用の初期化が二重に呼び出された。");
#pragma warning disable 618
					Assembly af = Assembly.LoadWithPartialName("cocoa-sharp");
					//「古い」と警告がでるけどしゃーない・・・
#pragma warning restore
					if (af != null)
					{
						macObj = af.GetType("Cocoa.Application").InvokeMember("Init",
							BindingFlags.Static | BindingFlags.InvokeMethod, null, macObj, null);
						IntPtr psn = IntPtr.Zero;
						MacAPI.MacAPI.GetCurrentProcess(ref psn);
						MacAPI.MacAPI.TransformProcessType(ref psn, 1);// これでコンソールから独立させることができる。
						MacAPI.MacAPI.SetFrontProcess(ref psn);

						MacAPI.MacAPI.NSApplicationLoad();
					}
					macAf = af;

				#region なぜこんなものが必要なの？
			/*
			Ozyさんいわく。

			cocoa-sharpは今のところ、GeoffNortonという人が実質一人でやってる状態だと
			思います。macapiはたしか今年の５月頃書いたはずですが、それからcocoa-sharpの
			開発って全然進んでないです(´ω`)

			OSXでSDLのアプリを作る場合はCocoaベースが主流で、最近のSDLにはObj-Cの
			スタートアップコードが含まれています。cocoa-sharpが進化して、osx+monoが
			もう少し一般的になってくれればSDLにもcocoa-sharp用のスタートアップコードが
			導入されるかもしれませんが、先の長い話になるかなーと思います。

			Assembly.Load云々の部分はmono側で対応してくれるでしょう、きっと。

			そんなわけで、私の見解としては、cocoa-sharpの開発が進んでSDLのCocoa用
			スタートアップコードが容易にYanesdkに移植できるようになるまでは現状維持。
			Assembly.Loadの警告はそれまでなんとか誤摩化し続ける^^;
			てなかんじです。
			*/
			#endregion

				}
			}

			{
				// SDL関連のDLLの読み込み
				Yanesdk.System.DllManager d = Yanesdk.System.DllManager.Instance;

				// 本体は必ず必要
				d.LoadLibrary(DLL_SDL);

				// サテライトdll
				if (initComponent == SDL.SDL_INIT_VIDEO)
				{
					// VIDEO関連
					d.LoadLibrary(DLL_SDL_IMAGE);
					d.LoadLibrary(DLL_ZIP);
					d.LoadLibrary(DLL_PNG);
					d.LoadLibrary(DLL_JPEG);
					d.LoadLibrary(DLL_TIFF);
				} else if (initComponent == SDL.SDL_INIT_AUDIO)
				{
					// SOUND関連
					d.LoadLibrary(DLL_SDL_MIXER);
					d.LoadLibrary(DLL_OGG);
					//	d.LoadLibrary(DLL_VORBIS);
					d.LoadLibrary(DLL_VORBISFILE);
					d.LoadLibrary(DLL_SMPEG);
				}
				else if (initComponent == SDL_INIT_TTF)
				{
					// TTF関連
					d.LoadLibrary(DLL_SDL_TTF);
				}
			}

			// SDLの読み込みに失敗していると、ここで失敗するんだな、これが。
			try
			{
				if (this.initComponent == SDL_INIT_TTF)
				{
					result = SDL.TTF_Init();
				}
				else
				{
					result = SDL.SDL_InitSubSystem(this.initComponent);
				}
			}
			catch
			{
				Debug.Assert(false, "SDLの読み込みに失敗している。");
			}
		}

		/// <summary>
		/// MacOSのときに読み込むcocoa-sharpのアセンブリ
		/// </summary>
		private static Assembly macAf = null;
		private static object macObj = null;

		/// <summary>
		/// 初回のみ初期化したいのでそのためのフラグ
		/// </summary>
		private static bool isFirst = true;

		/// <summary>
		/// 初期化したいコンポーネント
		/// </summary>
		private uint initComponent = 0;

		/// <summary>
		/// SDL初期化を行なったときのエラーリザルト。
		/// 0以外ならば失敗。
		/// </summary>
		public int Result
		{
			get { return result; }
		}
		private int result = 0;

		/// <summary>
		/// 解体を行なう。Disposeの多重呼び出しは禁止。
		/// </summary>
		public virtual void Dispose()
		{
			if (result == 0)
			{
				if (initComponent == SDL_INIT_TTF)
				{
					SDL.TTF_Quit();
				}
				else
				{
					SDL.SDL_QuitSubSystem(initComponent);
				}
			}
		}

		/// <summary>
		/// TTFの初期化用の命令を用意しとこっと。
		/// </summary>
		public const long SDL_INIT_TTF = 0x00000002;

	}

	/// <summary>
	/// SDL_Video初期化用クラス
	/// RefSingletonに食わせて使ってちょうだい。
	/// </summary>
	public class SDL_Video_Initializer : SDL_Initializer
	{
		public SDL_Video_Initializer() : base(SDL.SDL_INIT_VIDEO){}
	}

	/// <summary>
	/// SDL_ttf初期化用クラス
	/// RefSingletonに食わせて使ってちょうだい。
	/// </summary>
	public class SDL_TTF_Initializer : SDL_Initializer
	{
		public SDL_TTF_Initializer() : base(SDL_INIT_TTF){}
	}

	/// <summary>
	/// SDL_TIMER 初期化用クラス
	/// RefSingletonに食わせて使ってちょうだい。
	/// </summary>
	/// <remarks>
	///		タイマーの初期化を呼び出しておかないと、
	///		JoyStickの初期化等のついでにこれが呼び出されて
	///		タイマーがリセットされてしまう。
	///	</remarks>
	public class SDL_TIMER_Initializer : SDL_Initializer
	{
		public SDL_TIMER_Initializer() : base(SDL.SDL_INIT_TIMER) { }
	}

	/// <summary>
	/// SDL_CDROM 初期化用クラス
	/// RefSingletonに食わせて使ってちょうだい。
	/// </summary>
	public class SDL_CDROM_Initializer : SDL_Initializer
	{
		public SDL_CDROM_Initializer() : base(SDL.SDL_INIT_CDROM) { }
	}

	/// <summary>
	/// SDL_INIT_JOYSTICK 初期化用クラス
	/// RefSingletonに食わせて使ってちょうだい。
	/// </summary>
	public class SDL_JOYSTICK_Initializer : SDL_Initializer
	{
		public SDL_JOYSTICK_Initializer() : base(SDL.SDL_INIT_JOYSTICK) { }
	}
	
	/// <summary>
	/// SDL_AUDIO 初期化用クラス
	/// RefSingletonに食わせて使ってちょうだい。
	/// </summary>
	public class SDL_AUDIO_Initializer : SDL_Initializer
	{
		public SDL_AUDIO_Initializer() : base(SDL.SDL_INIT_AUDIO)
		{
			Sound.Sound.SoundConfig.Update(); // AudioのOpen
		}

		public override void Dispose()
		{
			if (Result == 0)
				Sound.Sound.SoundConfig.Close(); // AudioのClose
			base.Dispose();
		}
	}
}
