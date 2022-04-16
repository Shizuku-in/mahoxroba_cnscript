using System;
using System.Runtime.InteropServices;

using Sdl;
using Yanesdk.Ytl;

namespace Yanesdk.Input
{
	/// <summary>
	/// ジョイスティック入力クラス。
	/// </summary>
	/// <remarks>
	/// Joystick joy = new Joystick(0);
	/// とすれば、0番に接続されているジョイスティックにattachします。
	/// 番号は0番から始まります。ジョイスティックが接続されていない場合、
	/// このjoy.isPush() や joy.isPress 等の入力判定関数はすべてfalseを
	/// 返すようになります。(NullDeviceとattachされる)
	/// 
	/// すなわちユーザーはjoystickの有無について考える必要は、(普通は)
	/// ありません。
	/// </remarks>
	public class JoyStick : IKeyInput {

		/// <summary>
		/// ボタンの入力判定。
		/// </summary>
		/// <param name="nButtonNo"></param>
		/// <returns>isPushは前回押されていなくて今回押されていればtrue。
		/// isPressは前回の状態は関係なく、今回押されていればtrue。
		/// </returns>
		/// <remarks>
		/// 軸入力もボタンとして扱う。
		/// ↑:0　↓:1　←:2　→:3
		/// 1つ目のボタン:4  2つ目のボタン:5 ...(以下ボタンのついている限り)
		/// これ以上の情報が欲しいならSDL_joystickを利用すべき
		/// また、updateを呼び出さないと状態は更新されないので注意。
		/// 
		/// <code>
		///		int n = JoyStick.countJoyStick();
		///		↑これで接続されているjoystickの本数を取得できる
		///
		///		JoyStick[] joy = { new JoyStick(0),new JoyStick(1)};
		///		while (true) {
		///			joy[0].Update(); // このメンバを呼び出さないと状態が更新されない
		///			if (joy[0].isPress(i))
		/// </code>
		/// </remarks>
		public bool IsPush(int nButtonNo) {
			// 現在押されているか(状態はupdate関数を呼び出さないと更新されない)
			return input.IsPush(nButtonNo);
		}

		/// <summary>
		/// 前回のupdateのときに押されていなくて、今回のupdateで押されたか。
		/// </summary>
		/// <remarks>
		/// updateを呼び出さないと状態は更新されないので注意。
		/// </remarks>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsPress(int nButtonNo) {
			return input.IsPress(nButtonNo);
		}

		/// <summary>
		/// 前回のupdateのときに押されていて、今回のupdateで押されていない場合にtrueを返す。
		/// </summary>
		/// <remarks>
		/// updateを呼び出さないと状態は更新されないので注意。
		/// </remarks>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsPull(int nButtonNo)
		{
			return input.IsPull(nButtonNo);
		}
		
		/// <summary>
		/// 前回のupdateのときに押されていなくて、今回のupdateでも押されていないならtrueを返す。
		/// </summary>
		/// <remarks>
		/// updateを呼び出さないと状態は更新されないので注意。
		/// </remarks>
		/// <param name="nButtonNo"></param>
		/// <returns></returns>
		public bool IsFree(int nButtonNo)
		{
			return input.IsFree(nButtonNo);
		}

		/// <summary>
		/// 接続されているJoyStickのdevice nameを返す。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// 未接続のJoyStickの場合、getDeviceName()で
		/// 	"KeyInputNullDevice"
		/// が返ります。	
		/// </remarks>
		public string DeviceName { get { return input.DeviceName; } }

		/// <summary>
		/// ボタンの数を返します。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// 軸入力も4つのボタンと扱われているので、
		/// 戻り値は ボタンの数+4になります。
		/// </remarks>
		public int ButtonNum { get { return input.ButtonNum; } }

		/// <summary>
		/// ジョイスティックの状態の更新
		/// </summary>
		/// <remarks>
		///  このメソッドを呼び出さないとisPush/isPressは更新されないので注意。
		/// </remarks>
		public void Update() { input.Update(); }

		/// <summary>
		/// 固有情報の取得
		/// </summary>
		/// <returns></returns>
		public IntPtr Info { get { return input.Info; }  }

		/// <summary>
		/// attachしたいJoyStickのナンバーを指定してやる。
		/// </summary>
		/// <param name="nDeviceNo">0～countJoyStick-1までの数</param>
		/// <remarks>
		/// 不正な値を渡した場合は、NullDeviceが生成される 
		/// (エラーにはならない)
		/// </remarks>
		public JoyStick(int nDeviceNo)
		{
			input = control.Instance.getDevice(nDeviceNo);
		}

		public void Dispose()
		{
			input.Dispose();
		}

		/// <summary>
		/// 接続されているjoyStickの数を返す。
		/// </summary>
		/// <returns>接続されているjoyStickの数。0 = 接続なし。</returns>
		public static int countJoyStick() {
			JoystickControl jc = Singleton<JoystickControl>.Instance;
			return jc.countJoyStick();
		}

		/// <summary>
		/// 
		/// </summary>
		private IKeyInput input;

		private JoystickControl Control { get { return control.Instance; } }
		private RefSingleton<JoystickControl> control = new RefSingleton<JoystickControl>();

	}

	/// <summary>
	/// Joystickのcontroller
	/// このクラスはsingleton的に用いると良いだろう。
	/// </summary>	
	internal class JoystickControl : IDisposable
	{

		public JoystickControl()
		{
			if (init.Instance.Result < 0)
				devices = null;
			else
				devices = new IKeyInput[SDL.SDL_NumJoysticks()];
		}

		public void Dispose()
		{
			init.Dispose();
		}

		private RefSingleton<Yanesdk.Sdl.SDL_JOYSTICK_Initializer>
			init = new RefSingleton<Yanesdk.Sdl.SDL_JOYSTICK_Initializer>();

		/// <summary>
		/// 接続されているjoystickの数を返す。
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// 一度目の呼び出し以降に接続されたものは認識できない。
		/// </remarks>
		public int countJoyStick()
		{
			return devices == null ? 0 : devices.Length;
		}

		/// <summary>
		/// 対応するjoystick deviceを生成して返す。
		/// </summary>
		/// <param name="device_index"></param>
		/// <returns></returns>
		/// <remarks>
		/// 最初のgetDeviceの呼び出し時点で接続されていない
		/// deviceについては認識できﾅｰ
		/// </remarks>
		public IKeyInput getDevice(int device_index) {
			if (device_index<0 || device_index >= countJoyStick()){
				return new KeyInputNullDevice();
				//	繋がってないのでnull deviceを代入
			}
			if (devices[device_index] == null){
				//	未生成っぽいので生成する
				IntPtr j = SDL.SDL_JoystickOpen(device_index);
				if (j == IntPtr.Zero) // open失敗しちょる
					devices[device_index] = new KeyInputNullDevice();
				else
					devices[device_index] = new JoyStickImp(j);
			} else {
				//	生成済みならそれを返せば?
			}
			return devices[device_index];
		}

		private IKeyInput[] devices;

	}

	/// <summary>
	/// JoyStickの実装
	/// </summary>
	internal class JoyStickImp : IKeyInput
	{
		public void Dispose()
		{
			SDL.SDL_JoystickClose(joystick);
		}

		public bool IsPush(int nButtonNo) {
			if (nButtonNo > ButtonNum) return false;
			return !buttonState[1-flip][nButtonNo]
				&& buttonState[flip][nButtonNo];
		}
		public bool IsPress(int nButtonNo) {
			if (nButtonNo > ButtonNum) return false;
			return buttonState[flip][nButtonNo];
		}

		public bool IsPull(int nButtonNo)
		{
			if (nButtonNo > ButtonNum) return false;
			return (buttonState[flip ^ 1][nButtonNo]) && (!buttonState[flip][nButtonNo]);
		}
		public bool IsFree(int nButtonNo)
		{
			if (nButtonNo > ButtonNum) return false;
			return (!buttonState[flip ^ 1][nButtonNo]) && (!buttonState[flip][nButtonNo]);
		}


		public int ButtonNum { get { return buttonState[0].Length; } }

		public void	Update(){
			// ジョイスティックの状態の更新
			SDL.SDL_JoystickUpdate();
			//	↑これ全部のjoystickをpollするので、イベントループのなかで
			//	行なわれていることに期待すべきだが..忘れそうなのでここに入れる

			flip = 1-flip; // flipping

			//	軸情報の更新
			for(int i=0;i<axes && i<2;++i){
				short u = SDL.SDL_JoystickGetAxis(joystick, i);
				//	 * The state is a value ranging from -32768 to 32767.

				//	2/3以上倒れてたら、入力ありと見なす
				buttonState[flip][3-((i<<1)+0)] = (u >=  32767*2/3);
				buttonState[flip][3-((i<<1)+1)] = (u <= -32768*2/3);

				//	一般的には、軸0が左右、軸1が上下
				//	たいていは左上が(-32768,-32768)のようだ
			}

			//	buttonの更新
			for(int i=4;i<ButtonNum;++i){
				buttonState[flip][i] = SDL.SDL_JoystickGetButton(joystick,i-4)!=0;
			}

		}

		public string DeviceName {
			get
			{
				int nDeviceIndex = SDL.SDL_JoystickIndex(joystick);
				return SDL.SDL_JoystickName(nDeviceIndex);
			}
		}

		/// <summary>
		/// 獲得したSDL_Joystick* を返す(SDLのJoyStickのAPIを直接呼びたいとき用)
		/// </summary>
		/// <returns></returns>
		public IntPtr Info { get { return joystick; } }

		/// <summary>
		/// 獲得したSDL_Joystick* を渡す(SDLのJoyStickのAPIを直接呼びたいとき用)
		/// </summary>
		/// <param name="j"></param>
		public JoyStickImp(IntPtr j) {
			joystick = j;
			int nNumButtons = SDL.SDL_JoystickNumButtons(joystick);
			for(int i=0;i<2;++i){
				buttonState[i] = new bool[nNumButtons+4]; // 4というのは軸の分
			}
			axes = SDL.SDL_JoystickNumAxes(joystick);
		}

		// ~JoyStickImp() { if (joystick == IntPtr.Zero) SDL.SDL_JoystickClose(joystick); }

		private IntPtr joystick;	//	SDLのjoystick識別子
		private bool[][] buttonState = new bool[2][];	//	前回の入力状態と今回の入力状態
		private int flip;	//	前回の入力状態と今回の入力状態をflipさせて使う。flip={0,1}
		private int axes;	//	軸の数
	}
}
