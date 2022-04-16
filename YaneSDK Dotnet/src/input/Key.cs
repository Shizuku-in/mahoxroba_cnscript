using System;
using Yanesdk.Input;

namespace Yanesdk.Input
{
	/// <summary>
	/// キーデバイス登録済み、仮想キー設定済みのクラスです。
	/// </summary>
	/// <remarks>
	/// <para>
	/// ＫｅｙＢｏａｒｄ　＋　ＪｏｙＳｔｉｃｋ判定。
	/// 上下左右＋２ボタンのゲーム用。
	/// </para>
	/// <para>
	/// ボタン番号
	/// ０：ＥＳＣキー
	/// １：テンキー８，↑キー，ジョイスティック↑
	/// ２：テンキー２，↓キー，ジョイスティック↓
	/// ３：テンキー４，←キー，ジョイスティック←
	/// ４：テンキー２，→キー，ジョイスティック→
	/// ５：スペースキー，ジョイスティック　ボタン１
	/// ６：テンキーEnter,リターンキー，左シフト，右シフト。ジョイスティック　ボタン２
	/// </para>
	/// </remarks>
	/// <example>
	/// 使用例)
	/// <code>
	///
	///		Key1 key = new Key1();
	///
	///		while(true){
	///
	///			key.Update();
	///
	///			string s = null;
	///			keyinput.Update();
	///			for (int i = 0; i < 7; ++i)
	///			{
	///				if (keyinput.IsPress(i))
	///					s+=i.ToString();
	///			}
	///			Console.WriteLine(s);
	///		}
	///	}
 	/// </code>
	/// </example>
	public class Key1 : VirtualKey {
		/// <summary>
		/// 
		/// </summary>
		public Key1() {
			keyboard = new KeyBoardInput();
			joystick = new JoyStick(0);

			AddDevice(keyboard);
			AddDevice(joystick);

			//	0	:	Escape
			AddKey(0,0,(int)KeyCode.ESCAPE);

			//	1	:	Up
			AddKey(1,0,(int)KeyCode.KP8);
			AddKey(1,0,(int)KeyCode.UP);
			AddKey(1,1,0);

			//	2	:	Down
			AddKey(2,0,(int)KeyCode.KP2);
			AddKey(2,0,(int)KeyCode.DOWN);
			AddKey(2,1,1);

			//	3	:	Left
			AddKey(3,0,(int)KeyCode.KP4);
			AddKey(3,0,(int)KeyCode.LEFT);
			AddKey(3,1,2);

			//	4	:	Right
			AddKey(4,0,(int)KeyCode.KP6);
			AddKey(4,0,(int)KeyCode.RIGHT);
			AddKey(4,1,3);

			//	5	:	Space
			AddKey(5,0,(int)KeyCode.SPACE);
			AddKey(5,1,4);

			//	6	:	Return
			AddKey(6,0,(int)KeyCode.RETURN);
			AddKey(6,0,(int)KeyCode.KP_ENTER);
			AddKey(6,0,(int)KeyCode.LSHIFT);
			AddKey(6,0,(int)KeyCode.RSHIFT);
			AddKey(6,1,5);
		}

		/// <summary>
		/// 
		/// </summary>
		private KeyBoardInput keyboard;
		/// <summary>
		/// 
		/// </summary>
		private JoyStick joystick;
	}

	/// <summary>
	/// ＫｅｙＢｏａｒｄ　＋　ＪｏｙＳｔｉｃｋ判定。
	/// →　Key1も参照のこと。
	/// </summary>
	/// <remarks>
	/// <para>こちらは、上下左右＋６ボタンのゲーム用。
	/// </para>
	/// <para>
	/// ボタン配置：
	/// ０：ＥＳＣキー,ジョイスティック　ボタン７，８，９
	/// １：テンキー８，↑キー，ジョイスティック↑
	/// ２：テンキー２，↓キー，ジョイスティック↓
	/// ３：テンキー４，←キー，ジョイスティック←
	/// ４：テンキー２，→キー，ジョイスティック→
	/// ５：スペースキー，Ｚキー，ジョイスティック　ボタン１
	/// ６：テンキーEnter,リターンキー，Ｘキー，ジョイスティック　ボタン２
	/// ７：Ｃキー，ジョイスティック　ボタン３
	/// ８：Ａキー，ジョイスティック　ボタン４
	/// ９：Ｓキー，ジョイスティック　ボタン５
	/// １０：Ｄキー，ジョイスティック　ボタン６
	/// </para>
	/// </remarks>
	public class Key2 : VirtualKey {
		/// <summary>
		/// 
		/// </summary>
		public Key2() {
			keyboard = new KeyBoardInput();
			joystick = new JoyStick(0);

			AddDevice(keyboard);
			AddDevice(joystick);

			//	0	:	Escape
			AddKey(0,0,(int)KeyCode.ESCAPE);
			AddKey(0,1,10);
			AddKey(0,1,11);
			AddKey(0,1,12);

			//	1	:	Up
			AddKey(1,0,(int)KeyCode.KP8);
			AddKey(1,0,(int)KeyCode.UP);
			AddKey(1,1,0);

			//	2	:	Down
			AddKey(2,0,(int)KeyCode.KP2);
			AddKey(2,0,(int)KeyCode.DOWN);
			AddKey(2,1,1);

			//	3	:	Left
			AddKey(3,0,(int)KeyCode.KP4);
			AddKey(3,0,(int)KeyCode.LEFT);
			AddKey(3,1,2);

			//	4	:	Right
			AddKey(4,0,(int)KeyCode.KP6);
			AddKey(4,0,(int)KeyCode.RIGHT);
			AddKey(4,1,3);

			//	5	:	Space
			AddKey(5,0,(int)KeyCode.SPACE);
			AddKey(5,0,(int)KeyCode.z);
			AddKey(5,1,4);

			//	6	:	Return
			AddKey(6,0,(int)KeyCode.RETURN);
			AddKey(6,0,(int)KeyCode.KP_ENTER);
			AddKey(6,0,(int)KeyCode.LSHIFT);
			AddKey(6,0,(int)KeyCode.RSHIFT);
			AddKey(6,0,(int)KeyCode.x);
			AddKey(6,1,5);

			//	7	:	Button C
			AddKey(7,0,(int)KeyCode.c);
			AddKey(7,1,6);

			//	8	:	Button A
			AddKey(8,0,(int)KeyCode.a);
			AddKey(8,1,7);

			//	9	:	Button S
			AddKey(9,0,(int)KeyCode.s);
			AddKey(9,1,8);

			//	10	:	Button D
			AddKey(10,0,(int)KeyCode.d);
			AddKey(10,1,9);

		}

		/// <summary>
		/// 
		/// </summary>
		private KeyBoardInput keyboard;
		/// <summary>
		/// 
		/// </summary>
		private JoyStick joystick;
	}

	/// <summary>
	/// Key1 から、ジョイスティックサポートを取り除いたもの。
	/// </summary>
	public class Key3 : VirtualKey {
		/// <summary>
		/// 
		/// </summary>
		public Key3() {
			keyboard = new KeyBoardInput();

			AddDevice(keyboard);

			//	0	:	Escape
			AddKey(0,0,(int)KeyCode.ESCAPE);

			//	1	:	Up
			AddKey(1,0,(int)KeyCode.KP8);
			AddKey(1,0,(int)KeyCode.UP);

			//	2	:	Down
			AddKey(2,0,(int)KeyCode.KP2);
			AddKey(2,0,(int)KeyCode.DOWN);

			//	3	:	Left
			AddKey(3,0,(int)KeyCode.KP4);
			AddKey(3,0,(int)KeyCode.LEFT);

			//	4	:	Right
			AddKey(4,0,(int)KeyCode.KP6);
			AddKey(4,0,(int)KeyCode.RIGHT);

			//	5	:	Space
			AddKey(5,0,(int)KeyCode.SPACE);

			//	6	:	Return
			AddKey(6,0,(int)KeyCode.RETURN);
			AddKey(6,0,(int)KeyCode.KP_ENTER);
			AddKey(6,0,(int)KeyCode.LSHIFT);
			AddKey(6,0,(int)KeyCode.RSHIFT);
		}

		/// <summary>
		/// 
		/// </summary>
		public KeyBoardInput keyboard;
	}

	/// <summary>
	/// Key2 から、ジョイスティックサポートを取り除いたもの。
	/// </summary>
	public class Key4 : VirtualKey {
		/// <summary>
		/// 
		/// </summary>
		public Key4() {
			keyboard = new KeyBoardInput();

			AddDevice(keyboard);

			//	0	:	Escape
			AddKey(0,0,(int)KeyCode.ESCAPE);

			//	1	:	Up
			AddKey(1,0,(int)KeyCode.KP8);
			AddKey(1,0,(int)KeyCode.UP);

			//	2	:	Down
			AddKey(2,0,(int)KeyCode.KP2);
			AddKey(2,0,(int)KeyCode.DOWN);

			//	3	:	Left
			AddKey(3,0,(int)KeyCode.KP4);
			AddKey(3,0,(int)KeyCode.LEFT);

			//	4	:	Right
			AddKey(4,0,(int)KeyCode.KP6);
			AddKey(4,0,(int)KeyCode.RIGHT);

			//	5	:	Space
			AddKey(5,0,(int)KeyCode.SPACE);
			AddKey(5,0,(int)KeyCode.z);

			//	6	:	Return
			AddKey(6,0,(int)KeyCode.RETURN);
			AddKey(6,0,(int)KeyCode.KP_ENTER);
			AddKey(6,0,(int)KeyCode.LSHIFT);
			AddKey(6,0,(int)KeyCode.RSHIFT);
			AddKey(6,0,(int)KeyCode.x);

			//	7	:	Button C
			AddKey(7,0,(int)KeyCode.c);

			//	8	:	Button A
			AddKey(8,0,(int)KeyCode.a);

			//	9	:	Button S
			AddKey(9,0,(int)KeyCode.s);

			//	10	:	Button D
			AddKey(10,0,(int)KeyCode.d);
		}

		/// <summary>
		/// 
		/// </summary>
		private KeyBoardInput keyboard;
	}
}
