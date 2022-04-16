using System;

namespace Yanesdk.Input
{
	/// <summary>
	/// 入力用の基底クラス。
	/// </summary>
	/// <remarks>
	/// 軸入力もボタンとして扱う。
	/// 	↑:0　↓:1　←:2　→:3
	/// 	1つ目のボタン:4  2つ目のボタン:5  3つ目のボタン:6..
	/// </remarks>
	public interface IKeyInput : IDisposable {
		
		/// <summary>
		/// 現在押されているか(状態はupdate関数を呼び出さないと更新されない)
		/// </summary>
		/// <param name="nButtonNo">
		/// 軸入力もボタンとして扱う。
		/// 	↑:0　↓:1　←:2　→:3
		/// 	1つ目のボタン:4  2つ目のボタン:5  3つ目のボタン:6..
		/// </param>
		/// <returns></returns>
		bool IsPress(int nButtonNo);

		/// <summary>
		/// 前回のupdateのときに押されていなくて、今回のupdateで押されたか。
		/// </summary>
		/// <param name="nButtonNo">
		/// 軸入力もボタンとして扱う。
		/// 	↑:0　↓:1　←:2　→:3
		/// 	1つ目のボタン:4  2つ目のボタン:5  3つ目のボタン:6..
		/// </param>
		/// <returns></returns>
		bool IsPush(int nButtonNo);

		/// <summary>
		/// 前回のupdateのときに押されていて、今回のupdateで押されていない。
		/// </summary>
		/// <param name="nButtonNo">
		/// 軸入力もボタンとして扱う。
		/// 	↑:0　↓:1　←:2　→:3
		/// 	1つ目のボタン:4  2つ目のボタン:5  3つ目のボタン:6..
		/// </param>
		/// <returns></returns>
		bool IsPull(int nButtonNo);

		/// <summary>
		/// 前回のupdateのときに押されていなくて、今回のupdateでも押されていない。
		/// </summary>
		/// <param name="nButtonNo">
		/// 軸入力もボタンとして扱う。
		/// 	↑:0　↓:1　←:2　→:3
		/// 	1つ目のボタン:4  2つ目のボタン:5  3つ目のボタン:6..
		/// </param>
		/// <returns></returns>
		bool IsFree(int nButtonNo);

		/// <summary>
		/// 状態を更新する。このメソッドを呼び出さないとisPress,isPushで
		/// 返ってくる値は更新されないので注意。
		/// </summary>
		void Update();

		/// <summary>
		/// デバイス名の取得。
		/// </summary>
		/// <returns></returns>
		string DeviceName { get; } 

		/// <summary>
		/// デバイスのボタンの数。
		/// </summary>
		/// <returns></returns>
		int ButtonNum { get; }

		/// <summary>
		/// デバイスの固有情報を返す。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// JoyStickならば、SDL_Joystick*
		/// (SDLのJoyStickのAPIを直接呼びたいときに)
		/// KeyInputNullDeviceならばならば、null
		/// </remarks>
		IntPtr Info { get; }

		/// <summary>
		/// このデバイスが不要になったときに呼び出す
		/// </summary>
		new void Dispose();
	}

	/// <summary>
	/// 入力系のnull device。
	/// すべてのメソッドは、false/null/0などを返す
	/// </summary>
	public class KeyInputNullDevice : IKeyInput {
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsPush(int nButtonNo) { return false; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsPress(int nButtonNo) { return false; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsPull(int nButtonNo) { return false; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsFree(int nButtonNo) { return false; }

		/// <summary>
		/// 
		/// </summary>
		public void Update() { }
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string DeviceName { get { return "KeyInputNullDevice"; }  }
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int ButtonNum { get {return 0;} }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IntPtr Info { get { return IntPtr.Zero;} }

		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
		}
	}
}
