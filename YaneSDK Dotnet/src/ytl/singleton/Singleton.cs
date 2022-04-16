using System;
using System.Collections;
using System.Collections.Specialized;

namespace Yanesdk.Ytl {
	/// <summary>
	/// Singletonオブジェクトを取得する
	/// </summary>
	/// <remarks>
	/// ここで言うsingletonオブジェクトとは、最初に要求されたときに生成される、
	/// オブジェクトである。二度目以降の要求に対しては、一度目に生成した
	/// オブジェクトを返す。
	/// </remarks>
	public sealed class Singleton<T> where T : class,new()
	{
		#region 古い実装
		/*
		public sealed class Singleton
		{
			private static HybridDictionary instance = new HybridDictionary();

			private Singleton() { }

			/// <summary>
			/// このメソッドでsingletonオブジェクトを取得できる
			/// </summary>
			/// <param name="t"></param>
			/// <returns></returns>
			public static object getInstance(Type t)
			{
				object o = instance[t];
				if (o == null)
				{
					lock (instance.SyncRoot)
					{
						o = instance[t];
						if (o == null)
						{
							o = Activator.CreateInstance(t);
							instance[t] = o;
						}
					}
				}
				return o;
			}
		}
		*/
		#endregion

		/// <summary>
		/// singletonオブジェクトを取得する。
		/// </summary>
		/// <remarks>
		/// 最初の取得時にオブジェクトは一度だけ生成される。
		/// cf.
		/// http://www.microsoft.com/japan/msdn/library/default.asp?url=/japan/msdn/library/ja/dnpatterns/htm/ImpSingletonInCsharp.asp
		/// </remarks>
		/// <returns></returns>
		public static T Instance 
		{
			get
			{
				// double checked locking
				if (instance_ == null)
				{
					lock (syncRoot)
					{
						if (instance_ == null)
							instance_ = new T();
					}
				}

				return instance_;
			}
		}

		private static volatile T instance_;
		private static object syncRoot = new Object();

		// newして使うんじゃないぞよ
		private Singleton() { }

	}

}
			