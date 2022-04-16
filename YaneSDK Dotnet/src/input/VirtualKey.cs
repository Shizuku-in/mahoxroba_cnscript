using System;
using System.Collections.Generic;

namespace Yanesdk.Input
{
	/// <summary>
	/// VirtualKey
	/// </summary>
	/// <remarks>
	/// <para>
	/// 統合キー入力クラス。CKeyBaseの派生クラスを利用して、
	/// それらをひとまとめにして管理できる。
	/// </para>
	/// <para>
	/// たとえば、キーボードの↑キーと、テンキーの８キー、
	/// ジョイスティックの↑入力を、一つの仮想キーとして登録することによって、
	/// それらのどれか一つが入力されているかを、関数をひとつ呼び出すだけで
	/// 判定できるようになる。
	/// </para>
	/// <para>
	/// 実際にKey1,Key2,Key3,Key4は、
	/// このクラスの応用事例なので、そちらも参照すること。
	/// </para>
	/// <para>
	/// 全体的な流れは、キーデバイスの登録→仮想キーの設定としておいて、
	/// InputしたのちisPress/isPushで判定します。
	/// </para>
	/// </remarks>
	public class VirtualKey : IKeyInput {
		/// <summary>
		/// 仮想キーの最大個数。88鍵（＾＾；
		/// </summary>
		public const int VIRTUAL_KEY_MAX = 88;

		/// <summary>
		/// デバイスのクリア。
		/// </summary>
		public void	ClearDevice()
		{
			aDevice.Clear(); 
		}

		/// <summary>
		/// キーデバイスの登録。
		/// </summary>
		/// <param name="device"></param>
		/// <remarks>
		/// キーデバイスとは、KeyInputBaseの派生クラスのインスタンス。
		/// 具体的にはKeyInput,JoyStick,MidiInputのインスタンスが
		/// 挙げられる。入力したいキーデバイスをまず登録する。
		/// そしてInputを呼び出すことによって、それらのGetStateが呼び出される。
		/// </remarks>
		public void	AddDevice(IKeyInput device)
		{	///	デバイスの登録
	 		aDevice.Add(device);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		public void RemoveDevice(IKeyInput device)
		{	///	デバイスの削除
			aDevice.RemoveAll(delegate(IKeyInput b) { return b == device; });
		}

		/// <summary>
		/// ｎ番目に登録したデバイスの取得。
		/// </summary>
		/// <param name="n"></param>
		/// <returns></returns>
		/// <remarks>
		/// この関数を使えばｎ番目（０から始まる）のaddDeviceしたデバイスを
		/// 取得できる。）
		/// </remarks>
		public IKeyInput GetDevice(int n)
		{
		 	return aDevice[n] as IKeyInput;
		}

		//	仮想キーの追加・削除
		/// <summary>
		/// 仮想キーのリセット。
		/// </summary>
		public void ClearKeyList()
		{
			for (int i = 0; i < keylist.Length; ++i)
				keylist[i] = new List<KeyInfo>();
		}

		/// <summary>
		/// 仮想キーの追加。
		/// </summary>
		/// <param name="vkey"></param>
		/// <param name="nDeviceNo"></param>
		/// <param name="key"></param>
		public void AddKey(int vkey,int nDeviceNo,int key) {	///	
			KeyInfo k = new KeyInfo(nDeviceNo,key);
			keylist[vkey].Add(k);
		}

		/// <summary>
		/// 仮想キーの削除。
		/// </summary>
		/// <param name="vkey"></param>
		/// <param name="nDeviceNo"></param>
		/// <param name="key"></param>
		/// <remarks>
		/// vkeyは、仮想キー番号。これは0～VIRTUAL_KEY_MAX-1
		/// (現在88とdefineされている)番まで登録可能。
		/// キーデバイスnDeviceNoは、GetDeviceで指定するナンバーと同じもの。
		/// keyは、そのキーデバイスのkeyナンバー。
		/// </remarks>
		public void RemoveKey(int vkey,int nDeviceNo,int key) {
			keylist[vkey].RemoveAll(delegate(KeyInfo k){
				return  (k.deviceNo_ == nDeviceNo && k.keyNo_ == key);
			});
		}

		//	----	overriden from KeyInputBase	 ------

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nKey"></param>
		/// <returns></returns>
		public bool IsPush(int nKey)
		{
			foreach(KeyInfo k in keylist[nKey]){
				IKeyInput kb = aDevice[k.deviceNo_];
				if (kb.IsPush(k.keyNo_)) return true;
			}
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="nKey"></param>
		/// <returns></returns>
		public bool IsPress(int nKey)
		{
			foreach(KeyInfo k in keylist[nKey]){
				IKeyInput kb = aDevice[k.deviceNo_];
				if (kb.IsPress(k.keyNo_)) return true;
			}
			return false;
		}

		/// <summary>
		/// 前回のupdateのときに押されていて、今回のupdateで押されていない。
		/// </summary>
		/// <remarks>
		/// ひとつのdeviceでもPullであればtrue
		/// </remarks>
		/// <returns></returns>
		public bool IsPull(int nKey)
		{
			foreach (KeyInfo k in keylist[nKey])
			{
				IKeyInput kb = aDevice[k.deviceNo_];
				if (kb.IsPull(k.keyNo_)) return true;
			}
			return false;
		}

		/// <summary>
		/// 前回のupdateのときに押されていなくて、今回のupdateでも押されていない。
		/// </summary>
		/// <remarks>
		/// ひとつのdeviceでもFreeであればtrue
		/// </remarks>
		/// <returns></returns>
		public bool IsFree(int nKey)
		{
			foreach (KeyInfo k in keylist[nKey])
			{
				IKeyInput kb = aDevice[k.deviceNo_];
				if (kb.IsFree(k.keyNo_)) return true;
			}
			return false;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string DeviceName { get { return "VirtualKeyInput"; } }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public int ButtonNum { get { return keylist.Length; } }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IntPtr Info { get { return IntPtr.Zero; } }

		/// <summary>
		/// 登録しておいたすべてのデバイスのupdateを呼び出す。
		/// </summary>
		public void Update() { foreach(IKeyInput e in aDevice) e.Update(); }

		/// <summary>
		/// 
		/// </summary>
		public VirtualKey() { ClearDevice(); ClearKeyList(); }

		/// <summary>
		/// このDisposeは実際は何もしない。
		/// </summary>
		public void Dispose() { }

		/// <summary>
		/// 
		/// </summary>
		protected class KeyInfo {
			/// <summary>
			/// 
			/// </summary>
			public int		deviceNo_;
			/// <summary>
			/// 
			/// </summary>
			public int		keyNo_;
			/// <summary>
			/// 
			/// </summary>
			/// <param name="deviceNo"></param>
			/// <param name="keyNo"></param>
			public KeyInfo(int deviceNo,int keyNo) { deviceNo_ = deviceNo; keyNo_=keyNo; }
		}
		/// <summary>
		/// 入力リスト。
		/// </summary>
		protected List<KeyInfo>[] keylist = new List<KeyInfo>[VIRTUAL_KEY_MAX];
		/// <summary>
		/// 入力キーデバイスリスト。
		/// </summary>
		protected List<IKeyInput> aDevice = new List<IKeyInput>();
	}
}
