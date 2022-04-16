using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// 要素のAdd,RemoveがO(1)で出来る
	/// (database用語で言う)indexつきのListコンテナ。
	/// 
	/// collecton要素はuniqueであること(重複しないこと)が前提。
	/// 
	/// </summary>
	/// <remarks>
	/// 削除した要素は、default(T)になるので、このコレクションをforeachで回すときは注意が必要。
	///	</remarks>
	public class IndexedList<T> : IndexedList<T,T>
	{
		/// <summary>
		/// 新しい要素を追加する。
		/// Tは重複していないことが前提。
		/// 重複している場合は、YanesdkResult.InvalidParameterがエラーとして返る
		/// </summary>
		/// <param name="t"></param>
		public YanesdkResult Add(T t) { return Add(t,t); }

		/// <summary>
		/// こっちのメンバは隠匿しておく必要がある。
		/// </summary>
		/// <param name="t"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		private new YanesdkResult Add(T t, T s) { return Add(t, s); }
	}

	/// <summary>
	/// 要素のAdd,RemoveがO(1)で出来る
	/// (database用語で言う)indexつきのListコンテナ。
	/// 
	/// collecton要素はuniqueであること(重複しないこと)が前提。
	/// </summary>
	/// <remarks>
	/// Tは格納する要素。
	/// Sは、Tのfieldにあるクラス。
	/// SをkeyとしてTを引き出すのとに使う。
	/// 
	/// Sはuniqueであることが条件。
	/// indexはSに対して張る。
	/// 
	/// 削除した要素は、default(T)になるので、このコレクションをforeachで回すときは注意が必要。
	/// </remarks>
	public class IndexedList<T,S>
	//	where T : class
	{
		/// <summary>
		/// すべての要素をクリアする。
		/// </summary>
		public void Clear()
		{
			list.Clear();
			indexOfList.Clear();
			reuseList.Clear();
		}

		/// <summary>
		/// 新しい要素を追加する。
		/// keyは重複していないことが前提。
		/// 重複している場合は、YanesdkResult.InvalidParameterがエラーとして返る
		/// </summary>
		/// <param name="t"></param>
		public YanesdkResult Add(T t,S key)
		{
			YanesdkResult result;

		//	Debug.Assert(t != null, "tの値としてnullが渡ってきている。");
			
			// すでに含まれているならばエラー
			if (indexOfList.ContainsKey(key))
				result = YanesdkResult.InvalidParameter;
			else
			{
				// 要素をコンテナに追加して、そのindexを張る

				if (reuseList.Count == 0)
				{
					indexOfList.Add(key, list.Count);
					list.Add(t);
				}
				else
				{
					// 再利用できるらしいので、再利用する。
					int lastIndex = reuseList.Count - 1;
					int index = reuseList[lastIndex];
					reuseList.RemoveAt(lastIndex);

					indexOfList.Add(key, index);
					list[index] = t;
				}

				result = YanesdkResult.NoError;
			}

			return result;
		}

		/// <summary>
		/// コンテナからある要素を削除する
		/// 存在しない場合は、YanesdkResult.InvalidParameterがエラーとして返る
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public YanesdkResult Remove(S key)
		{
			YanesdkResult result;

		//	Debug.Assert(key != default(S),
		//		"keyの値としてnullが渡ってきている。");

			if (indexOfList.ContainsKey(key))
			{
				int index = indexOfList[key];
				indexOfList.Remove(key);

				// このあいた空間(list[index]) に、末尾の要素を移動させる必要がある)
				// のだが、T!=SだとTからSを取得する手段がないのでkeyのindex張り直しができない
				// そこで再利用リストに格納してお茶を濁すことにする。

				// nullで埋めて参照を切っておく必要がある。
				list[index] = default(T);

				// 再利用してねん。
				reuseList.Add(index);

				/*
				int last = list.Count - 1;
				if (last == 0)
				{
					// 移動させたいが移動させる要素がもう無い。
					list.Clear();
				}
				else 
				{
					T lastObj = list[index] = list[last]; // 末尾要素
					list.RemoveAt(last); // 末尾要素の削除はそのあとのpackingが不要なので早いはずだ

					// こいつにindexを張り替える
					// これがSに対してはできないのか(´ω`)
					// TからSを得る手段がないと駄目なのよ…
					indexOfList.Add(lastObj,index);
				}
				 */

				result = YanesdkResult.NoError;
			}
			else
			{
				result = YanesdkResult.InvalidParameter;
			}

			return result;
		}

		/// <summary>
		/// keyに対してTを引き出す。
		/// 存在しないものにアクセスしようとすると例外が出る。(するな)
		/// </summary>
		/// <remarks>
		/// Tがstructだと、値を変更したいならgetしたあと最終的にsetする必要がある。
		/// </remarks>
		/// <param name="key"></param>
		/// <returns></returns>
		public T this[S key]
		{
			get { return list[indexOfList[key]]; }

			// setしてkeyが変わってしもたら嫌なのでこれやめとくか？
			set { list[indexOfList[key]] = value; }
		}

		/// <summary>
		/// T本体を自体を格納するコレクション
		/// foreachなどですべての要素に対して何らかの検査をする必要が
		/// ある場合はこれを用いること。
		/// 
		/// (注意)indexを内部的に形成してあるのでこの要素を
		/// コレクション内で移動ないし削除してはならない。
		/// また、コレクション内でRemoveした値はdefault(T)になっていることにも注意せよ。
		/// </summary>
		public List<T> List
		{
			get { return list; }
		}
		private List<T> list = new List<T>();

		/// <summary>
		/// Sからintへmapする。このintの値は、this.listのindexを意味している。
		/// </summary>
		private Dictionary<S, int> indexOfList = new Dictionary<S, int>();

		private List<int> reuseList = new List<int>();
	}
}
