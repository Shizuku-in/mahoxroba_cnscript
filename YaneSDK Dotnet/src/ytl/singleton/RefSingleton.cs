using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// 参照カウントが0から1になった瞬間new()して、
	/// 1から0になった瞬間Disposeするような参照管理クラス。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class RefCountObject<T> where T : class, IDisposable, new()
	{
		/// <summary>
		/// 参照カウントを1だけ加算。
		/// 参照カウントが0から1になるときにTがnewされる。
		/// </summary>
		public void IncRef()
		{
			if (count++ == 0)
				t = new T();
		}

		/// <summary>
		/// 参照カウントの1だけ減算。
		/// 参照カウントが1から0になるときにTのDisposeが呼び出される。
		/// </summary>
		public void DecRef()
		{
			if (--count == 0)
			{
				t.Dispose();
				t = null;
			}
			Debug.Assert(count >= 0);
		}

		/// <summary>
		/// 参照カウンタ
		/// </summary>
		private int count;

		/// <summary>
		/// 参照カウントが0から1になったときに生成されたインスタンス
		/// </summary>
		public T Instance
		{
			get { return t; }
		}
		private T t;
	}

	/// <summary>
	/// 参照カウント型のsingletonクラス
	/// </summary>
	/// <remarks>
	/// コンストラクタで参照カウントを +1 する。
	/// Disposeで参照カウントを -1 する。
	/// 多重にDisposeを呼び出しても参照カウントは -1 しかされない。
	/// 
	/// 参照カウントが0から1になるときにT型のsingletonをnewする。
	/// 参照カウントが1から0になるときそのsingletonをDisposeする。
	/// 
	/// 参照カウントが 1以上であれば Instanceプロパティで T型のインスタンスを取得できる。
	/// 
	/// ※　Yanesdk全体でSDLの初期化に幾度となく使ってあるので、
	/// このクラスの「すべての参照の検索」で、使用例を見てください。
	/// </remarks>
	public class RefSingleton<T> : IDisposable
		where T : class,IDisposable,new()
	{
		/// <summary>
		/// 参照カウントを +1 する。
		/// </summary>
		public RefSingleton()
		{
			Inst.IncRef();
		}

		/// <summary>
		/// 参照カウントを-1する。
		/// 多重にDisposeを呼び出しても参照カウントは -1 しかされない。
		/// </summary>
		public void Dispose()
		{
			if (once)
			{
				once = false;
				Inst.DecRef();
			}
		}
		private bool once = true;

		/// <summary>
		/// 参照カウント型singletonオブジェクト
		/// </summary>
		public T Instance
		{
			get { return Inst.Instance; }
		}

		private RefCountObject<T> Inst
		{
			get { return Singleton<RefCountObject<T>>.Instance; }
		}
	}
}
