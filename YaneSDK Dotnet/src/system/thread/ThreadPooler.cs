using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Yanesdk.Ytl;
using System.Diagnostics;

namespace Yanesdk.System
{
	/// <summary>
	/// Threadをpoolしておいて、そいつに対してコマンドを
	/// 送ってそれぞれのthreadに特定の処理を行なわせる仕組み
	/// </summary>
	public class ThreadPooler : IDisposable
	{
		/// <summary>
		/// 最大thread数はdefaultで10。
		/// 必要があればMaxThreadプロパティで変更すること。
		/// </summary>
		public ThreadPooler()
		{
			scheduler = new Thread(this.Scheduler);
			scheduler.Start();
		}

		/// <summary>
		/// taskを渡す。ここで渡したtaskはworker threadが
		/// ユーザーの書いたtaskを呼び出すときにパラメータとして
		/// 渡してくれる。
		/// </summary>
		/// <param name="task"></param>
		public void AddTask(object task)
		{
			taskQueue.Enqueue(task);
		}

		/// <summary>
		/// task queueをクリアする。すでに実行している分は
		/// 停止させることはできない。
		/// </summary>
		public void ClearTaskQueue()
		{
			taskQueue.Clear();
		}

		/// <summary>
		/// worker threadに渡すコマンド
		/// </summary>
		private SynchronizedQueue<object>
			taskQueue = new SynchronizedQueue<object>();

		/// <summary>
		/// worker threadから呼び出されるtask
		/// 呼び出して欲しいメソッドを登録しておくこと。
		/// </summary>
		public TaskDelegate Task
		{
			get { return task; }
			set { task = value; }
		}
		private volatile TaskDelegate task;

		/// <summary>
		/// Taskのdelegate
		/// </summary>
		/// <param name="param"></param>
		public delegate void TaskDelegate(object param);

		/// <summary>
		/// worker threadのentry point
		/// </summary>
		private void Worker(object o)
		{
			WorkerThreadParam param = o as WorkerThreadParam;

			while (param.IsValid || param.Command!=null)
			{
				if (param.Command == null)
				{	// 仕事がねーので休む
					Thread.Sleep(100);
					continue;
				}

				param.Working = true;
				// 細かいことだが、Workingはバリアの働きをするので
				// Commandのpopより先行して行なう必要がある。

				// commandのpop(長さ1のqueueだと考えよ)
				object Command = param.Command;
				param.Command = null;

				// taskが設定されていないと実行しようがない
				Debug.Assert(task != null, "呼び出すべきTaskが設定されていない");
				if (task!=null)
					Task(Command);

				param.Working = false;
			}
		}

		/// <summary>
		///	スケジュール用のthreadのentry point
		/// </summary>
		private void Scheduler()
		{
			while (true)
			{
				// 終了するんか？
				if (!isValid)
				{
					maxThread = 0;
					if (threads.Count == 0)
						break;
				}

				// スレッド数が異なるならば
				int maxthread = this.maxThread;
				if (maxthread != threads.Count)
				{
					// maxThreadの値はmain threadによって書き換えられる可能性があるが
					// threads.Countはこのスレッドからしか扱わないのでおかしい値になる
					// 可能性がない。

					if (maxthread > threads.Count)
					{
						while (maxthread > threads.Count)
						{
							// threadを生成する必要がある。
							WorkerThread thread = new WorkerThread();
							thread.Thread = new Thread(this.Worker);
							thread.Thread.Start(thread.Param);
							threads.AddLast(thread);
						}
					}
					else
					{
						// threadが多すぎる。
						// threadを停止させる(先頭から)
						threads.First.Value.Param.IsValid = false; // こうしとけばいずれ止まるやろ

						// 死んだthreadを回収する
						if (!threads.First.Value.Param.Working
							&& threads.First.Value.Param.Command == null
							)
							threads.RemoveFirst();
					}
				}

				// taskがあるなら仕事をしてないやつに渡してやる
				if (taskQueue.Count != 0)
				{
					// 暇を持て余しているスレッドを探し、そいつに依頼する
					foreach (WorkerThread worker in threads)
					{
						// IsValidのフラグ変更はこの上の箇所でしか行なわないので
						// 他のスレッドから値が変更されることは想定しなくて良い
						if (worker.Param.IsValid
							&& !worker.Param.Working
							&& worker.Param.Command == null
						)
						{
							// 見つけた。こいつに仕事を依頼する
							worker.Param.Command = taskQueue.Dequeue();

							// 次の仕事が待っているかも知れないのでSleepは省略する
							goto SkipSleep;
						}
					}
				}
				
				Thread.Sleep(100);

			SkipSleep: ;

				// 現在仕事をしているスレッド数をカウントする
				{
					int workingCount = 0;
					foreach (WorkerThread worker in threads)
						if (worker.Param.Working)
							++workingCount;
					currentWorkingThread = workingCount;
				}

				// 現在プールされているスレッド数をカウントする
				currentAliveThread = threads.Count;
			}

		}

		/// <summary>
		/// 破棄するときに必ず呼び出してくれい。
		/// </summary>
		public void Dispose()
		{
			isValid = false;
		}

		/// <summary>
		/// Schedulerスレッドが生きていて良いのか
		/// </summary>
		private volatile bool isValid = true;

		/// <summary>
		/// poolする最大thread数
		/// 
		/// 1threadあたり2MB程度必要になる。
		/// メモリ不足に注意すること。
		/// </summary>
		public int MaxThread
		{
			get
			{
				return this.maxThread;
			}

			set
			{
				Debug.Assert(value >= 0, "Threadの数としてマイナスが指定された");
				this.maxThread = value;
				// すでに生成済みであれば現在のthreadを停止させるわけにはいかない
				// よってthread数の増減はschedulerに一任する
			} 
		}
		private volatile int maxThread = 10;

		/// <summary>
		/// 現在の仕事をしているthread数
		/// </summary>
		public int CurrentWorkingThread
		{
			get { return currentWorkingThread; }
		}
		private volatile int currentWorkingThread = 0;

		/// <summary>
		/// 現在poolされているthread数
		/// </summary>
		public int CurrentAliveThread
		{
			get { return currentAliveThread; } 
		}
		private volatile int currentAliveThread = 0;		

		/// <summary>
		/// worker threadとやりとりするパラメータ
		/// </summary>
		internal class WorkerThreadParam
		{
			/// <summary>
			/// schedulerからworker threadへのコマンド
			/// </summary>
			public volatile object Command = null;

			/// <summary>
			/// コマンドを処理中である
			/// </summary>
			public volatile bool Working = false;

			/// <summary>
			/// 仕事をしていて良い
			/// </summary>
			public volatile bool IsValid = true;
		}

		/// <summary>
		/// worker thread本体
		/// </summary>
		internal class WorkerThread
		{
			/// <summary>
			/// worker threadの実体
			/// </summary>
			public Thread Thread;

			public WorkerThreadParam Param = new WorkerThreadParam();
		}

		/// <summary>
		/// poolされているthread
		/// </summary>
		private SynchronizedLinkedList<WorkerThread>
			threads = new SynchronizedLinkedList<WorkerThread>();

		/// <summary>
		/// スケジューラ用のthread
		/// </summary>
		private Thread scheduler;

		
	}
}
