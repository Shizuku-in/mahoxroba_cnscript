using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// オブジェクトを事前に生成して貯めておく仕組み。
	/// </summary>
	/// <remarks>
	///	オブジェクトプールは、結局のところ有限スタックとして実装される。
	///	すなわち、サイズが事前に決まっているスタックであり、未使用オブジェクトを
	///	このスタック上にpushして(積んで)いき、未使用オブジェクトをもらうには、
	///	このスタックからpopして(降ろして)いくことにより、プールという仕組みを
	/// 実現することが出来る。
	/// <code>
	/// alias objectPool!(Star) StarPool;
	/// </code>
	/// のようにして、
	/// <code>
	///	//	☆オブジェクトを事前に生成しておく。
	///	StarPool pool = new StarPool;
	///	pool.setMax(500);
	///	foreach(inout Star name;pool) name = new Star();
	///	</code>
	/// プールからもらうときは、
	/// <code>
	/// //	プールからオブジェクトをひとつもらう
	///	Star name = pool.pop();
	///	if (name) {
	///		name.Reset(info,x,y);
	///		controller.AddTask(name,0);
	///	}
	///	</code>
	///	プールに返すときは、
	///	<code>
	///	if (time > 50){
	///		// 自殺する
	///		pool.push(this); // このオブジェクトをプールに戻す
	///		return 1;
	///	}
	///	</code>
	/// のようにする。
	/// </remarks>
	public class ObjectPool<T> : IDisposable
	{
		///	<summary>poolするオブジェクトの最大数を設定/取得する。</summary>
		/// <remarks>
		///	事前に設定すること。
		///	設定すると、以前保持していたオブジェクトは
		///	解放してしまうので注意。
		/// </remarks>
		public int Max
		{
			set {
				Release();
				stack = new T[value];
			}
			get {
				if (stack == null) return 0;
				return stack.Length;
			}
		}

		/// <summary>
		/// すべてを解放する。
		/// </summary>
		/// <remarks>
		/// 解放するときに、TのIDisposeを呼び出す。
		/// </remarks>
		protected void Release() {
			if (stack != null) {
				for (int i = 0; i < stack.Length; ++i) {
					IDisposable d = stack[i] as IDisposable;
					if (d != null)
						d.Dispose();
					stack[i] = default(T);
				}
				stack = null;
			}
			sp = 0;
		}

		public void Dispose()
		{
			Release();
		}

		///	<summary>オブジェクトをひとつもらう。</summary>
		/// <remarks>
		///	未使用のオブジェクトがなければ、nullが返る。
		/// オブジェクトは“貸し出し”であって、必ずpushで返す必要がある。
		/// </remarks>
		public T Pop(){
			if (stack == null) return default(T);
			if (sp == stack.Length) return default(T);
			return stack[sp++];
		}

		///	<summary>オブジェクトをひとつ返す。</summary>
		/// <remarks>
		///	最初setMaxで確保して、ここからpopで取得した分以外の
		///	オブジェクトを返してはならない。
		/// </remarks>
		public void Push(T t){
			if (stack == null) return ;
			if (sp==0) return ; // errorであるべきか..
			stack[--sp] = t;
		}

		/// <summary>未使用のオブジェクト数を返す。</summary>
		public int Rest { get { return stack.Length - sp; } }

		private T[] stack = null;
		private int sp = 0; // stack pointer

		///	<summary>オブジェクトをひとつ取得する。</summary>
		/// <remarks>
		///	オブジェクトに空きがない場合は、一番最後にpushForcedで返された
		///	オブジェクトを返す。(pushForcedと対にして使う)
		///
		///	(パーティクルを表現するときなどに)一番古い時間にpopで
		/// “貸し出し”されたオブジェクトを再利用したいときに
		///	これを使うと良い。
		/// </remarks>
		public T PopForced() {
			if (stack == null) return default(T); // これは仕方ない
			if (sp==stack.Length) {
				T t = stack[0];
				for(int i=0;i<sp-1;++i)
					stack[i] = stack[i+1];
				stack[sp-1] = t; // 一番最後に参照されたオブジェクトなのでここ。
				return t;
			}
			return stack[sp++];
		}

		///	<summary>オブジェクトをプールに戻す。</summary>
		/// <remarks>
		///	popForcedと対にして使えるように、戻すときに
		///	順序づけを行なって戻す。
		/// </remarks>
		public void PushForced(T t){
			/*
				Tが、stack[0]から現在のスタック位置までの間にあるはずなので
				それを削除して詰めていく。
			*/
			if (stack == null) return ; // うひゃー。どうなっとるんじゃ
			for(int i=0;i<sp;++i){
				if (stack[i].Equals(t)){ // みっけ！
					//	間を詰めて
					for(int j=i;j<sp-1;++j) {
						stack[j] = stack[j+1];
					}
					//	現在のスタックのところに戻す
					stack[--sp] = t; return;
				}
			}
			//	死にオブジェクト突っ込まれたと思われ。

			Debug.Assert(false);
		}

		/// <summary>
		/// 内部で使用しているspポインタそのまま返す/設定する。
		/// </summary>
		/// <remarks>
		/// このメソッド呼び出しは非推奨。
		/// </remarks>
		/// <returns></returns>
		public int Sp { get { return sp; } set { sp=value;} }

		/// <summary>
		/// 内部で使用しているスタックをそのまま返す。
		/// </summary>
		/// <returns></returns>
		public T[] Stack { get { return stack; } }

		/// <summary>
		/// IEnumerator interface
		/// </summary>
		/// <remarks>
		/// 現在までに popして“貸し出し中”である objectのみ列挙する。
		/// </remarks>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < sp;++i )
			{
				yield return stack[i];
			}
		}

	}
}
