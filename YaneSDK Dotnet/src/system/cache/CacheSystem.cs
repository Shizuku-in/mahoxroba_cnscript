using System;
using System.Collections.Generic;
using Yanesdk.Ytl;
using System.Text;
using System.Diagnostics;

namespace Yanesdk.System
{
	/// <summary>
	/// CacheSystemでcacheすべきオブジェクト
	/// </summary>
	/// <remarks>
	/// このCacheSystemではICacheObjectに対して、自律的にリソースを
	/// 再構築する手段の実装を要求する。
	/// </remarks>
	public interface ICachedObject
	{
		/// <summary>
		/// オブジェクトを構築するための情報を前もって渡しておく。
		/// 実際に構築するわけではない。
		/// 
		/// 読み込むリソースファイル名など、必要なものを事前に渡しておくと良いだろう。
		/// </summary>
		/// <param name="info"></param>
		void Construct(object info);

		/// <summary>
		/// オブジェクト自体の再構築。
		/// 必要とあらばこのときファイルからリソースを読み込む
		/// </summary>
		void Reconstruct();

		/// <summary>
		/// リソースの解放(Reconstructable == trueのときしか呼び出した場合、Constructでの再構築はできない)
		/// </summary>
		void Destruct();

		/// <summary>
		/// Reonstructを呼び出して再構築することができるオブジェクトなのかどうかを取得する
		/// </summary>
		bool Reconstructable { get; }

		/// <summary>
		/// 使用しているリソースのサイズを取得するメソッド
		/// Destructを呼び出したあとは必ず 0 を返さなければならない。
		/// こいつが0か非0かを、リソースを読み込んでいるのかいないのかのフラグに使う。
		/// </summary>
		long ResourceSize { get; }

		/// <summary>
		/// Destructで解放されたかの判定フラグ。
		/// IsLost == true の場合、Reconstructを呼び出して再確保することができる。
		/// </summary>
		bool IsLost { get; }

		/// <summary>
		/// リソースサイズに変更(更新)があったときには
		///		CacheSystem.OnResourceChanged
		/// を呼び出す。
		/// 
		/// リソースにアクセスした際にはマーカーをつける意味で
		///		CacheSystem.OnAccess
		/// を呼び出す。
		/// 
		/// ICacheObject継承クラスは、この2つを必ず守らなければならない。
		/// 
		/// そのためのCacheSystemをここで設定する。
		/// </summary>
		CacheSystem<ICachedObject> CacheSystem
		{
			get;
			set;
			//	get { return cacheSystem; }
			//	set { cacheSystem = value; }
		}
		// CacheSystem<ICachedObject> cacheSystem;
		// 実装するときには、cacheSystem自体は、deafultでUnmanagedResourceManagerと
		// bindしてあって良い。
	}

	/// <summary>
	/// ファイルからリソースを読み込むやつらが実装しているべきinterface
	/// </summary>
	public interface ILoader
	{
		/// <summary>
		/// ファイルからリソースを読み込む
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		YanesdkResult Load(string filename);

		/// <summary>
		/// ファイルを読み込んでいる場合は、読み込んでいるファイル名を返す
		/// </summary>
		string FileName { get; }

		/// <summary>
		/// リソースを読み込んでいるかを判定する
		/// </summary>
		bool Loaded { get; }

		/// <summary>
		/// 読み込んでいるリソースを解放する
		/// </summary>
		/// <returns></returns>
		void Release();
	}

	/// <summary>
	/// ICacheObjectの代表的な実装。
	/// 多重継承を避けるため、この部分は、mix-inで書きたいのだが。
	/// </summary>
	public abstract class CachedObject : ICachedObject
	{
		#region ICachedObjectの実装
		/// <summary>
		/// mix-inで書きたいのだが(´ω`)
		/// </summary>
		/// <param name="loader">thisを渡してくれい</param>
		protected ILoader This
		{
			set { mix_in_this = value; }
		}
		// mix-inされるときのthis
		private ILoader mix_in_this;

		public void Construct(object obj)
		{
			constructInfo = obj; //  as TextrueConstructAdaptor;
			isLoadFailed = false;
		}

		/// <summary>
		/// 再構築するときに必要となるパラメータ。
		/// </summary>
		public /*TextrueConstructAdaptor*/ object constructInfo = null;

		public void Reconstruct()
		{
			if (IsLost)
			{
				// 派生クラス側で再実装してくれい。
				YanesdkResult result = OnReconstruct(constructInfo);
				isLoadFailed = result != YanesdkResult.NoError;
			}
		}

		public void Destruct()
		{
			if (mix_in_this.Loaded)
			{
				// Releaseを呼び出したときに開放されかねないので退避させておく
				object info = constructInfo;
				mix_in_this.Release();
				constructInfo = info;
			}
		}

		public virtual bool Reconstructable { get { return constructInfo != null; } }

		public bool IsLost
		{
			get
			{
				return !mix_in_this.Loaded && constructInfo != null && !isLoadFailed;
				// これ、一度失敗したオブジェクトがlostしているからと言って
				// 読み込みを繰り返すとえらいことになる。
				// 一度でも読み込みに失敗したらIsLost == falseになるようにしておく。
			}
		}
		protected bool isLoadFailed = false;

		/// <summary>
		/// pressureをかけるCacheSystemを指定する。
		/// </summary>
		public CacheSystem<ICachedObject> CacheSystem
		{
			get { return cacheSystem; }
			set { cacheSystem = value; }
		}
		private CacheSystem<ICachedObject> cacheSystem;
		// 大文字、小文字のみの違いはCLS準拠にならないので
		// アンダーバーを入れるのも癪だし、protectedではなくprivateにしてしまう。

		/// <summary>
		/// 派生クラス側に任せた
		/// </summary>
		public abstract long ResourceSize { get; }

		#endregion

		#region protected
		/// <summary>
		/// 派生クラス側で再実装してくれい
		/// 
		/// ディフォルトでは渡されたものをstringとみなして
		/// それをリソースから読み込む。
		/// </summary>
		/// <param name="param">Constructで渡されたパラメータ</param>
		/// <returns></returns>
		protected virtual YanesdkResult OnReconstruct(object param)
		{
			return mix_in_this.Load(param as string);
		}
		#endregion
	}

	/// <summary>
	///	CacheSystem は、それぞれのオブジェクトのリソースサイズを管理しており、
	/// 指定されているキャッシュ容量をこえれば最後のアクセスしたものから
	/// 自動的に解放するようにディフォルト動作はなっている。
	/// </summary>
	/// <remarks>
	/// limitsizeの設定必須。
	/// 
	/// GlTexture/TextureLoader
	/// Sound/SoundLoader
	/// などで利用しているので、そちらも参考にすると良いだろう。
	/// </remarks>
	public class CacheSystem<T> : IDisposable
		where
			T : ICachedObject
	{
		#region ctor & Dispose
		/// <summary>
		/// 保持しているすべてのオブジェクトを解放する
		/// </summary>
		private void Release()
		{
			foreach (CacheInfo info in list.List)
			{
				if (info.CachedObject!=null)
					info.CachedObject.Destruct(); // 解放のために全部呼び出したる
			}
			list.Clear();
		}

		public void Dispose()
		{
			Release();
		}
		#endregion

		#region properties

		/* // こんな機能いらねーや(´ω`)
		/// <summary>
		/// Cacheのpressureを共有するために、親のCacheSystemを設定する。
		/// こうしておけば、親のCacheSystemのCacheLimitを見ながらpressureを
		/// かけてゆく。
		/// 
		/// Getほにゃららを行なうよりさきに設定する必要がある。
		/// </summary>
		public CacheSystem<T> Parent
		{
			get { return parent; }
			set { Release();  parent = value; }
		}
		private CacheSystem<T> parent;
		*/

		/// <summary>
		/// アクセス記録をつけるときに用いる時間
		/// </summary>
		/// <remarks>
		/// オブジェクトをcacheのなかに新規に追加するときは、一番古くにアクセスされた
		/// オブジェクトを自動的に追い出す必要があるので。
		/// </remarks>
		/// <returns></returns>
		protected long Now
		{
			get
			{
				// 親が居るなら、親とcache共有を行なう可能性があるので
				// 親の持つアクセス時間を利用する。
			//	if ( parent != null )
			//		return parent.Now;
			//	else
					return now++;
				// propertyのなかで値を変更するのはあまり上品な設計ではないが
				// まあこれが増えたところでuniqueな値だから問題ないやろ…(´ω`)
			}
		}
		/// <remarks>
		/// 初期値は0で、アクセスごとにインクリメントされる。
		/// long型なので、一秒間に100000回アクセスがあったとしても、
		/// 2^64 / 100000 / 60 / 60 / 24 / 365.24 = 5845420461年
		///	よって実用上、十分であると言える。
		/// </remarks>
		private long now = 0; // これはアクセスするごとに ++ して使う。

		/// <summary>
		/// このクラスがcacheする上限サイズ
		/// オブジェクトをaddするときに渡されるsizeの総計がこれを超えた時点で
		/// 一番古くにアクセスしたオブジェクトから自動的に解放する
		/// 
		/// これを設定していないと、初期状態では上限なし。
		/// </summary>
		public long LimitSize
		{
			get
			{
			//	if (parent != null)
			//		return parent.limitSize;
			//	else
					return limitSize;
			}
			set
			{
			//	if (parent != null)
			//		parent.limitSize = value;
			//	else
					limitSize = value;

				// 値が変更されたことによって、オブジェクトを追い出さないといけないかも。
				PurgeCheck();
			}
		}
		// 初期状態ではunlimited
		private long limitSize = long.MaxValue;

		/// <summary>
		/// 読み込みしているトータルサイズ
		/// </summary>
		/// <remarks>
		/// setterは外部からは使用しないでください。
		/// </remarks>
		public long TotalSize
		{
			get
			{
			//	if (parent != null)
			//		return parent.totalSize;
			//	else
					return totalSize;
			}
			set
			{
			//	if (parent != null)
			//		parent.totalSize = value;
			//	else
					totalSize = value;
			}
		}
		private long totalSize = 0;	

		#endregion

		#region ICachedObjectが内部的に利用すればよさげなメソッド群

		/// <summary>
		/// このcache system管理下のオブジェクトにする
		/// </summary>
		/// <param name="t"></param>
		public void Add(T t)
		{
			Debug.Assert(t != null,
				"CacheObjectとしてnullなものを突っ込もうとした。");

			CacheInfo info = new CacheInfo();
			info.CachedObject = t;
			info.LastAccessTime = Now;
			info.ResourceSize = t.ResourceSize;

			TotalSize += info.ResourceSize;
			list.Add(info, t);
		}

		/// <summary>
		/// このcache system管理下のオブジェクトを取り除く
		/// </summary>
		/// <param name="t"></param>
		public void Remove(T t)
		{
			Debug.Assert(t != null,
				"CacheObjectとしてnullなものを取り除こうとした。");

			list.Remove(t);
		}

		/// <summary>
		/// Tにアクセスされたときに、アクセスされたことを示すマーカーをつける。
		/// cacheがいっぱいになった場合には、このマーカーがもっとも古い時刻についているものから解放する。
		/// </summary>
		/// <param name="t"></param>
		public void OnAccess(T t)
		{
			CacheInfo info;
			try
			{
				info = list[t];
			}
			catch
			{
				Debug.Assert(false, "Disposeしたあとのオブジェクトにアクセスした？");
				return;
			}
			info.LastAccessTime = Now;
			list[t] = info; // CacheInfoはstructなのでwrite backしないといけない
		}

		/// <summary>
		/// cacheしているリソースのサイズに変更があった場合に
		/// 呼び出してください。
		/// 
		/// (Tから内部的にこれを呼び出すように設定しておけば良いだろう)
		/// </summary>
		public void OnResourceChanged(T t)
		{
			CacheInfo info;
			try
			{
				info = list[t];
			}
			catch
			{
				Debug.Assert(false, "Disposeしたあとのオブジェクトにアクセスした？");
				return;
			}
			long oldSize = info.ResourceSize;
			info.ResourceSize = t.ResourceSize;
			list[t] = info; // CacheInfoはstructなのでwrite backしないといけない

			// これによりtotalはいくら変化するかと言うと…
			TotalSize = TotalSize - oldSize + t.ResourceSize;

			PurgeCheck();
		}

		#endregion

		#region protected
		/// <summary>
		/// CacheSystemが内部的に保持しているオブジェクト
		/// </summary>
		/// <remarks>
		/// 内部的に保持しているので通常書き換えなくて良いのだが、
		/// あとになって、sizeやdelegateを書き換えたくなったときのために
		/// publicにしておく。
		/// </remarks>
		protected struct CacheInfo
		{
			/// <summary>
			/// cacheの対象となるオブジェクト
			/// </summary>
			public T CachedObject;

			/// <summary>
			///  オブジェクトのサイズ(読み込んでいるリソース量等)
			/// </summary>
			public long ResourceSize;

			/// <summary>
			/// 最後にこのオブジェクトへアクセスされた時刻
			/// </summary>
			public long LastAccessTime;
		}

		/// <summary>
		/// cacheしているオブジェクトのlist。
		/// 通常、これを直接いじる必要はないのだが…。
		/// 
		/// CacheInfoはstructなので要注意！
		/// </summary>
		/// <remarks>
		/// LinkedList内はhandleで整順されていると仮定できる
		/// </remarks>
		protected IndexedList<CacheInfo, T> list = new IndexedList<CacheInfo, T>();
		#endregion

		#region private
		/// <summary>
		/// Cacheしているオブジェクトのサイズが変更になったなどの理由により
		/// 古くなったものを追い出す必要があるかをチェックする。
		/// </summary>
		private void PurgeCheck()
		{
			if (TotalSize > LimitSize)
			{
				T t = default(T); // 最も昔にアクセスされたオブジェクトでかつ再構築可能なものをポイントする
				long time = long.MaxValue;

				// ファイルが阿呆ほどあるとこのiterationに時間がかかる可能性があるので
				// 極力高速化する。もちろん、CacheInfoはstructにしてある。

				List<CacheInfo> L = list.List;
				int count = L.Count; // local copyのほうが速いだろう
				for (int i = 0; i < count ; ++i )
				{
					if (L[i].CachedObject == null)
						continue;

					if (L[i].LastAccessTime < time
						&& L[i].CachedObject.Reconstructable
						&& L[i].CachedObject.ResourceSize != 0)
					{
						// めっけたので更新。
						time = L[i].LastAccessTime;
						t = L[i].CachedObject;
					}
				}

				if (time == long.MaxValue)
				{
					Debug.Assert(false, "解放できるCacheObjectが無い。");
					return ; // 合致するオブジェクトがないので解放できない
				}

				t.Destruct();
				// これを呼び出した瞬間に再帰がかかるのでここではこれ以上purgeのcheckを行なう必要がない
			}
		}
		#endregion

		#region Debug用
		/// <summary>
		/// デバッグ用に内部状態をConsoleに表示します。
		/// </summary>
		public void DebugOut()
		{
			Console.WriteLine(DebugOutString());
		}

		/// <summary>
		/// デバッグ用に内部状態をあらわす文字列を生成します。
		/// </summary>
		/// <returns></returns>
		public string DebugOutString()
		{
			StringBuilder s = new StringBuilder();

			s.Append(string.Format("CacheObject count = {0} , TotalResourceSize = {1} \n",
				list.List.Count.ToString(),totalSize.ToString()));
			foreach (CacheInfo info in list.List)
			{
				if (info.CachedObject == null)
				{
					s.Append("削除済みオブジェクト\n");
					continue;
				}

				ILoader loader = info.CachedObject as ILoader;
				string filename = loader != null ? loader.FileName : ""; 

				s.Append(
					string.Format("FileName : {0} ,  AccessTime : {1} , ResourceSize : {2} , IsLost : {3} , Reconstructable : {4}\n",
						filename,
						info.LastAccessTime.ToString(),
						info.ResourceSize.ToString(),
						info.CachedObject.IsLost.ToString(),
						info.CachedObject.Reconstructable.ToString()
					)
				);
			}

			return s.ToString();
		}
		#endregion
	}
}
