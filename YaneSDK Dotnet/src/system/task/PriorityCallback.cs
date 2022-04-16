using System.Collections.Generic;

namespace Yanesdk.Ytl
{
	// プライオリティ付きのcallback(描画等で用いる)のテスト用のコード
	/*
		class Task : IPriorityCallback
		{
			public void OnCallback()
			{
				Console.WriteLine("Priority {0}",Priority);
			}
			public int Priority {
				get { return priority_; }
				set { priority_ = value; }
			}
			private int priority_ = 0;
		}
		void game_main(){
				Task task1 = new Task();
				Task task2 = new Task();
				Task task3 = new Task();
				task1.Priority = 10;
				task2.Priority = 5;
				task3.Priority = 20;

				PriorityCallback p = new PriorityCallback();
				p.addCallback(task1);
				p.addCallback(task2);
				p.addCallback(task3);
				p.yieldCallback(); // 5,10,20のpriorityを持つtaskが順番に呼び出される
		}
	 * */

	/// <summary>
	/// 描画のコールバック用クラス。
	/// </summary>
	/// <remarks>
	/// PriorityCallbackで用いる。
	/// </remarks>
	public interface IPriorityCallback
	{
		/// <summary>
		/// こいつをコールバックする
		/// </summary>
		void OnCallback();

		/// <summary>
		/// プライオリティを持つ
		/// </summary>
		int Priority
		{
			get;
		}
	}

	/// <summary>
	/// 描画するときに、z-order(重ね合わせの優先順位)をつけたいことがある。
	/// よって、描画の前段階で、priorityを設定して、あとでpriority順に
	/// callbackしてもらうような仕組みが必要である。
	/// </summary>
	/// <remarks>
	///  1)clearCallbackを呼び出す
	///  2)addCallbackでcallback queueにどんどん積む
	///  3)yieldCallbackメソッドを呼び出す
	/// </remarks>
	public class PriorityCallback
	{
		/// <summary>
		/// コールバックqueueに積む
		/// </summary>
		/// <param name="o"></param>
		/// <param name="Priority"></param>
		public void AddCallback(IPriorityCallback o)
		{
			list.Add(o);
		}

		/// <summary>
		/// callback queueをクリア
		/// </summary>
		public void ClearCallback()
		{
			list.Clear();
		}

		/// <summary>
		/// addCallbackでqueueに積んだものをpriority順にcallback
		/// </summary>
		public void YieldCallback()
		{
			//	delegate、いまいち洗練されてないなぁ(´ω`)
			list.Sort(
				delegate(IPriorityCallback a, IPriorityCallback b)
				{
					return (a.Priority != b.Priority)?
						(a.Priority < b.Priority ? -1 : 1) : 0;
				}
			);
			foreach (IPriorityCallback m in list)
				m.OnCallback();
		}

		private List<IPriorityCallback> list = new List<IPriorityCallback>();
	}
}