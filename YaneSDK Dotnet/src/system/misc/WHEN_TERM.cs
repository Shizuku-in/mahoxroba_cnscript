using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.System
{
	/// <summary>
	/// usingのなかで使うと便利クラス
	/// 
	/// using (WHEN_EXIT t = new WHEN_EXIT(delegate { XXXX; })
	/// 
	/// と書いておけば、このスコープが終わるときにXXXXが実行される。
	/// 
	/// </summary>
	public class WHEN_EXIT : IDisposable
	{
		/// <summary>
		/// Disposeが呼び出されるときに呼び出されるDelegateクラス
		/// </summary>
		public delegate void OnExitDelegate();

		/// <summary>
		/// Disposeが呼び出されるときに呼び出されるdelegate
		/// </summary>
		private OnExitDelegate onExit = null;

		/// <summary>
		/// Disposeが呼び出されるときに呼び出されるdelegateを渡して使う。
		/// </summary>
		/// <param name="exitDelegate"></param>
		public WHEN_EXIT(OnExitDelegate exitDelegate)
		{
			onExit = exitDelegate;
		}

		/// <summary>
		/// このメソッドが呼び出されるときにコンストラクタで設定した
		/// delegateが呼び出される。
		/// </summary>
		public void Dispose()
		{
			if (onExit != null)
			{
				onExit();
			}
		}
	}
}
