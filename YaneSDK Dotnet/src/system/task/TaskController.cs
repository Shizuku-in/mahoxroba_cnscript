using System;
using System.Collections.Generic;

namespace Yanesdk.System {
	/// <summary>
	/// ゲーム用のタスクの基底クラス
	/// </summary>
	/**
		TaskControllerで用いるためのタスク基底クラス

		以下、D言語版のソース(備忘用)
	<PRE>
	int main(){
		TaskController controller = new TaskController();

		//	ここではタスクを直接ここに書いているが、実際は
		//	何らかのクラス、あるいは事前に生成してpoolしておいたものを用いる。
		controller.AddTask(
			new SimpleGameTask(
				delegate int (Object o){
					static int i =0;
					++i;
					printf("%d\n",i);
					if (i==5) {
						GameTaskController c = cast(GameTaskController)o;
						c.Terminate(); // コントローラーに終了通知
					}
				}
			 , 123 // Task Priority
			)
		);

		while (!controller.End()) {
			controller.DebugOutput();	//	debug用
			controller.CallTask(controller);
			//	ここではcontrollerを引数として渡しているが、
			//	実際は、画面まわりや入力まわりなどタスク側で必要となるものを
			//	集積させたクラスを渡すべき
		}
		return 0;
	}
	</PRE>
	*/
	public abstract class TaskBase {
		///	<summary>呼び出されるべきタスク。</summary>
		/// <remarks>
		///	派生クラス側で、これをオーバーライドして使う。
		///	この引数には、 TaskController.CallTask 呼び出し時に
		///	渡したパラメータが入る。
		///
		///	非0を返せば、このタスクは消される。
		/// </remarks>
		public abstract int Task(Object o);

		/* // これは、ToStringをオーバーライドすればいいので、消す
		///	<summary>デバッグ用にタスク名を返す関数。</summary>
		/// <remarks>
		///	必要ならばオーバーライドして使うと良い。
		/// </remarks>
		public virtual string getTaskName() { return "no name Task"; }
		*/

		/// <summary>
		/// タスク間で情報を渡したいときに用いる。
		/// </summary>
		/// <remarks>
		/// TaskControllerからは、taskのpriorityを指定すればそのTaskが取得できるので
		/// そのあと、このメソッドを使うと良い。
		///	</remarks>
		/// <returns></returns>
		public virtual Object TaskInfo
		{
			get
			{
				return null;
			}
		}

		///	デバッグ用にこのタスクの情報を返す
		/**
			必要なせば、オーバーライドして使うと良い。
		*/
		public virtual string TaskDebugInfo
		{
			get { return "no ExtraInfo"; }
		}

		/// <summary>
		/// 
		/// </summary>
		public TaskBase() {}
	}

	///	<summary>ゲーム用のタスクコントローラー。</summary>
	/// <remarks>
	///	タスクの管理を行ないます。
	///	タスク( TaskBase の派生クラス)を管理するために使います。
	///
	///	タスクには優先順位をつけおけば、指定した優先順位において
	///	呼び出されます。
	///
	///	サンプルについては、 SceneController の説明のものを参考にどうぞ。
	/// </remarks>
	public class TaskController : IDisposable
	{

		/// <summary>
		/// タスクひとつを表すTaskController内部で使う構造体
		/// </summary>
		public class Task
		{
			/// <summary>
			/// タスクのpriority
			/// </summary>
			/// <remarks>
			/// defaultではint.MaxValue。
			/// かならず使う前に変更して。
			/// </remarks>
			public int Priority = int.MaxValue;

			/// <summary>
			/// タスクが次に変更されるべきpriority
			/// </summary>
			/// <remarks>
			/// ここで設定した値に、次のiterationで変更される。
			/// ただし、変更には、TaskController.changeTaskPriorityを用いること。
			/// </remarks>
			internal int NextPriority;

			/// <summary>
			/// タスク
			/// </summary>
			public TaskBase TaskBase = null;

			/// <summary>
			/// このタスクは死亡しているのか。
			/// もし trueならば、次のiterationにおいて削除される
			/// </summary>
			public bool Dead = false;

			public Task() { }
			public Task(TaskBase task, int priority)
			{
				this.TaskBase = task;
				this.Priority = priority;
				this.NextPriority = priority;
			}
		}

		private LinkedList<Task> taskList = new LinkedList<Task>();	//	タスクリスト

		/// <summary>
		/// タスクリストの取得
		/// </summary>
		/// <remarks>
		/// タスクコントローラーのデバッガを作るときなど、何かの時に使うと良い。
		/// </remarks>
		/// <returns></returns>
		public LinkedList<Task> TaskList { get { return taskList; } }
		
		/// <summary>
		/// すべてのタスクを呼び出す。
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// タスクプライオリティ(実行優先順位)に従い、
		/// すべてのタスクを呼び出します。</para>
		/// <para>
		/// ここで引数として渡しているObject型は、
		/// TaskBase の Task メソッドの引数として渡されます。
		/// 
		/// 殺されたタスクは、死ぬ前にDispose(IDisposableのinterfaceを持つならば)が
		/// 呼び出されることが保証されます。
		/// </para>
		/// <para>
		/// terminateメンバが呼び出されると、この関数は非0を返すように
		/// なります。
		/// </para>
		/// </remarks>
		public int CallTask(object o)
		{
			{
				LinkedListNode<Task> node = taskList.First;

				// disableTaskList.ContainsKeyにあるpriorityのTaskは呼び出さない

				/*
				while ( node != null )
				{
					Task t = node.Value;
					if ( !t.Dead )
					{
						if (!disableTaskList.ContainsKey(t.Priority))
							if ( t.TaskBase.Task(o) != 0 )
								t.Dead = true; // こいつ殺す
					}
					node = node.Next;
				}
				 */

				// ただし、↑のように同じタスクpriorityなのに何度もContainsKeyを呼び出すのは
				// 忍びないので、その場合は、チェックをはしょる。

				// 1つの値だけcache出来ると考える
				bool bLast = true;
				int nLast = int.MaxValue;
				while ( node != null )
				{
					Task t = node.Value;
					if ( !t.Dead )
					{
						int nPriority = t.Priority;
						if ( nPriority != nLast )
						{
							nLast = nPriority;
							bLast = !disableTaskList.ContainsKey(t.Priority);
							// ここで 否定をとっておかないと 毎回 否定をとることになってしまって損
						}
						if ( bLast )
							if ( t.TaskBase.Task(o) != 0 )
								t.Dead = true; // こいつ殺す
					}
					node = node.Next;
				}

			}

			/*	// foreach中にcollection変更してはいけないので
			foreach ( Task t in taskList )
			{
				if (!t.Dead && t.TaskBase.Task(o) != 0 )
					t.Dead = true; // こいつ殺す
			}
			 */

			if ( bPriorityChanged )
			{
			// プライオリティの変更されたタスクを探して旅に出る
			Retry:
				Task t2 = null;
				foreach ( Task t in taskList )
				{
					if ( !t.Dead && t.Priority != t.NextPriority )
					{
						t.Dead = true;
						t2 = t;
						break;
					}
				}
				if ( t2 != null )
				{
					AddTask(t2.TaskBase , t2.NextPriority);
					goto Retry; // O(N^2)だが、そうそう変更するものでもないしまあいいかぁ(´ω`)
				}
				bPriorityChanged = false;
			}

			// 死にタスクを回収しておく。
			// taskのremoveはここでしか行なわないのでここで
			// taskを殺すときに Disposeを呼び出せば十分である。
			/*
			taskList.RemoveRemoveAll(delegate(Task t) {
				if (t.Dead)
				{
					IDisposable d = t.TaskBase as IDisposable;
					if (d != null) d.Dispose();
				}
				return t.Dead;
			});
			 */

			// LinkedListに対するRemoveAllなんであらへんねん(´ω`)
			{
				LinkedListNode<Task> node = taskList.First;
				while ( node != null )
				{
					LinkedListNode<Task> next = node.Next;
					if ( node.Value.Dead )
					{
						IDisposable d = node.Value.TaskBase as IDisposable;
						if ( d != null )
							d.Dispose();
						taskList.Remove(node);
					}
					node = next;
				}
			}

			return bEnd ? 1 : 0;
		}

		/// <summary>
		/// 特定のpriorityのtaskをCallTaskからは一時的に呼び出さないようにする。
		/// (殺すわけではない)
		/// </summary>
		/// <param name="priority"></param>
		public void DisableTask(int priority)
		{
			if (! disableTaskList.ContainsKey(priority) )
			{
				disableTaskList.Add(priority,null);
			}
		}

		/// <summary>
		/// DisableTaskでCallTaskからは一時的に呼び出さないようにしていたものを、
		/// また呼び出すようにする。
		/// </summary>
		/// <param name="priority"></param>
		public void EnableTask(int priority)
		{
			if ( disableTaskList.ContainsKey(priority) )
			{
				disableTaskList.Remove(priority);
			}
		}

		/// <summary>
		/// DisableTaskで一時的に呼び出さないようにしていたものを
		/// すべて呼び出すように変更する。
		/// </summary>
		public void EnableAllTask()
		{
			disableTaskList.Clear();
		}
		private Dictionary<int , object> disableTaskList = new Dictionary<int , object>();


		/// <summary>
		/// 生成したタスクをタスクリストに登録する。
		/// </summary>
		/// <param name="Task"></param>
		/// <param name="Priority"></param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// new したCTaskControlBase派生クラスを渡してチョ
		/// プライオリティは、タスクの優先度。0だと一番優先順位が高い。
		/// 万が一に備えてマイナスの値を渡しても良いようにはなっている。
		/// </para>
		/// </remarks>
		public void AddTask(TaskBase task, int priority) {
			// 正しい位置にinsertしなくてはならない
			AddTask(new Task(task, priority));
		}

		/// <summary>
		/// タスクをタスクリストに加える
		/// </summary>
		/// <remarks>
		/// priorityを考慮して正しい位置にinsertする。
		/// (taskListはつねにpriorityで整順されている。)
		/// </remarks>
		/// <param name="t"></param>
		public void AddTask(Task t_)
		{
			// 正しい位置にinsertしなくてはならない

			LinkedListNode<Task> node = taskList.First;
			while(node!=null){
				if (t_.Priority < node.Value.Priority)
				{
					taskList.AddBefore(node, t_);
					return;
				}
				node = node.Next;
			}
			taskList.AddLast(t_);
		}

		///	タスクの削除。
		/**
			優先度を指定して、そのタスクを消滅させる。
			自分で自分のタスクを削除することも出来る。
			回収は、次のcallTaskのときに行なわれる
		*/
		public void KillTask(int priority)
		{
			foreach(Task t in taskList)
				if (t.Priority == priority)
					t.Dead = true;
		}

		/// <summary>
		/// タスクの一括削除。
		/// </summary>
		/// <param name="nStartPriority"></param>
		/// <param name="nEndPriority"></param>
		/// <remarks>
		/// <para>
		/// 優先度を指定してのタスクの一括削除。
		/// 自分のタスクが含まれても構わない。
		/// </para>
		/// <para>
		/// nStartPriority～nEndPriorityまでを削除する。
		/// (nEndPriorityも削除対象に含む。)
		/// </para>
		/// </remarks>
		public void KillTask(int nStartPriority,int nEndPriority){
			foreach (Task t in taskList)
			{
				int p = t.Priority;
				if (nEndPriority < p) break;
				if (nStartPriority <= p)
					t.Dead = true;
			}
		}

		/// <summary>
		/// 指定したプライオリティのタスクを得る
		/// </summary>
		/// <remarks>
		///	もし指定したプライオリティのタスクがなければnullが戻る。
		///	プライオリティに対応するタスクが唯一であるような設計をしている
		///	場合に使うと便利。
		/// ただし、死にタスクは取得できない。
		/// </remarks>
		/// <param name="Priority"></param>
		/// <returns></returns>
		public TaskBase GetTask(int priority)
		{
			foreach (Task t in taskList)
			{
				if (!t.Dead && t.Priority == priority)
					return t.TaskBase;
				if (t.Priority > priority)
					break; // 整順されているのでこの後ろに来ることは無い
			}
			return null;
		}

		/// <summary>
		///	プライオリティを変更
		/// </summary>
		/// <remarks>
		/// プライオリティを変更する。
		///	callTaskで呼び出されているタスクが自分自身のプライオリティを
		///	変更しても構わない。（ように設計されている）
		/// 
		/// 死にタスクは変更できない。
		/// </remarks>
		/// <param name="beforePriority"></param>
		/// <param name="afterPriority"></param>
		public void ChangePriority(int beforePriority,int afterPriority){
			// List<T>のiteration中に insertしたらあかんみたいやで..
			// 変更しといて、次のiterationで反映させるか..
			foreach (Task t in taskList)
			{
				if (!t.Dead && t.Priority == beforePriority)
				{
					t.NextPriority = afterPriority;
				}
				if (beforePriority < t.Priority) break;
			}
			bPriorityChanged = true;
		}

		/// <summary>
		/// 指定したプライオリティのタスクがあるかどうか調べる
		/// </summary>
		/// <remarks>
		///	タスクコントローラーみたいなものを用意して、そのタスクが
		///	存在しなければ次のタスクを生成する、というような使いかたをすれば
		///	便利だろう。
		/// 
		/// 死にタスクは対象に含まれない。
		/// </remarks>
		/// <param name="Priority"></param>
		/// <returns></returns>
		public bool IsExistTask(int priority) {
			foreach (Task t in taskList)
			{
				if (!t.Dead && t.Priority == priority)
					return true;
				if (t.Priority > priority)
					break; // 整順されているのでこの後ろに来ることは無い
			}
			return false;
		}

		/// <summary>
		/// 終了する場合に呼び出す
		/// </summary>
		/// <remarks>
		/// これを呼び出すと、callTaskの戻り値が非0になります
		/// </remarks>
		public void Terminate() { bEnd = true; }

		/// <summary>
		/// 終了するのか？
		/// </summary>
		/// <remarks>
		/// terminateを呼び出されたあとかを判定する
		/// </remarks>
		/// <returns></returns>
		public bool End { get { return bEnd; } }

		/// <summary>
		///	すべてのタスクを表示する
		/// </summary>
		/// <remarks>
		/// これはデバッグ用。
		///	グラフィカルなデバッグ環境が欲しいなぁ．．(´Д｀)
		/// </remarks>
		public void DebugOutput(){
			bool bFound = false;
			foreach(Task t in taskList){
				bFound = true;
				Console.WriteLine("Task Priority : {0} : {1} : {2}",
					t.Priority,
					t.TaskBase.ToString(),
					t.TaskBase.TaskDebugInfo
				);
			}
			if (!bFound) { Console.WriteLine("タスク無し"); }
		}

		/// <summary>
		/// 終了するならば非0
		/// </summary>
		private bool bEnd = false;

		/// <summary>
		/// プライオリティを変更したタスクがあるか
		/// </summary>
		/// <remarks>
		/// プライオリティを変更したタスクがあれば、callTaskでのiterationの
		/// 終わりに、そのタスクのプライオリティを変更しなければならない
		/// </remarks>
		private bool bPriorityChanged = false;

		/// <summary>
		/// Disposeにおいて、このタスクコントローラーが保持しているすべてのタスクに対して、
		/// IDispose interafaceを持つものは、Disposeを呼び出す。
		/// </summary>
		public void Dispose()
		{
			foreach (Task t in taskList)
			{
				IDisposable d = t.TaskBase as IDisposable;
				if (d != null) d.Dispose();
			}
		}

	}

	/// <summary>
	/// taskを生成するためのfactory
	/// </summary>
	/// <typeparam name="TaskName">
	/// Task名を表現している列挙体
	/// </typeparam>
	public abstract class TaskFactoryBase<TaskName> {

		/// <summary>
		/// 指定されたTaskNameのタスクを生成して返すfactoryを
		/// オーバーライドして書く。
		/// newで生成しなくとも、事前に生成しておいたpoolから
		/// 割り当てても良い。
		/// </summary>
		/// <param name="name">
		/// 生成すべきtaskのTaskName。
		/// TaskNameに対して、一意に生成すべきタスククラス(TaskBase派生クラス)が
		/// 決定できるときにこのメソッドを用いる。
		/// </param>
		/// <returns></returns>
		public abstract TaskBase CreateTask(TaskName name);
	}

	/// <summary>
	/// taskを用いたシーンコントローラー。
	/// </summary>
	/// <remarks>
	/// いわゆる、シーンコントローラー。
	/// 逐次的なタスク実行を実現する。
	/// 
	/// シーンを管理するタスク(TaskBase派生クラス)をシーンタスクと呼ぶ。
	/// シーンタスクは、priorityに対して一意に定まる。
	/// このクラスのsetTaskFactoryメソッドでシーンタスクのfactoryを設定する。
	/// 
	/// あとは、遷移すべきシーンタスクのpriorityをこのクラスのjumpScene等で
	/// 渡してやれば、自動的にそのシーンタスクが生成される。
	/// 
	/// シーンタスク間の移動は、このシーンタスク自体をtask Priority==1などに
	/// 固定しておき、
	///		SceneController scene = taskController.GetTask(1) as SceneController;
	///		scene.JumpScene(Taskname.Task1);
	/// のようにして遷移する。
	/// 
	///　注意事項
	///    taskFactoryの設定必須。
	/// 
	/// </remarks>
	public class SceneController<TaskName> : TaskBase,IDisposable {

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// TaskBaseからのオーバーライド
		/// </remarks>
		/// <param name="o"></param>
		/// <returns></returns>
		public override int Task(object o){
			if (End) return 1;

			TaskBase task = NowTask;
			if (task == null) {
				//	タスクがすべて終了しているので
				//	次に予約されているタスクを生成して実行しなければならない。
				TaskName name = PopTaskName();
				// isEndをfalse通過しているわけでstacklist.length!=0

				task = TaskFactory.CreateTask(name);
				taskList.Add(task);
			}
			task.Task(o);
			return 0;
		}

		/// <summary>
		/// 次のシーンに飛ぶ
		/// </summary>
		/// <remarks>
		/// このメソッドを呼び出した瞬間に、そのタスクがIDisposable interfaceを持つなら
		/// 即座にDisposeメソッドが呼び出される。よって、そのあとメンバ等にはアクセスしてはならない。
		/// </remarks>
		/// <param name="nPriority"></param>
		public void JumpScene(TaskName name)
		{
			//	現在実行中のシーンを削除
			if (NowTask != null) { PopTask(); }
			PushTaskName(name);
			//	次にtaskが呼び出されたときにnewされるでしょー
		}

		/// <summary>
		///	呼び出し元のシーンに戻る
		/// </summary>
		///	このときに、pushSceneでスタック上にシーンが積まれていれば、
		///	そちらを実体化する。
		public void ReturnScene(){
			PopTask();
			if (taskNameList.Count!=0){
				TaskName name = PopTaskName();
				TaskBase task = TaskFactory.CreateTask(name);
				taskList.Add(task);
			}
		}

		/// <summary>
		///	他のシーンを呼び出す(現在実行中のタスク自体は破棄しない)
		/// </summary>
		/// <remarks>
		///	現在実行中のシーン破棄はされません。
		///	ただし、task関数のなかでは(現在実行中のシーンへreturnSceneなどで
		///	制御が戻ってくるまでは)呼び出されなくなります。
		///	</remarks>
		/// <param name="nPriority"></param>
		public void CallScene(TaskName name)
		{
			TaskBase task = TaskFactory.CreateTask(name);
			taskList.Add(task);
		}

		/// <summary>
		/// 指定したタスクを指定した順番で呼び出す
		/// </summary>
		/// <remarks>
		///	指定したタスクをシーンスタックに積みます。
		///	(returnSceneされたときに逆順で呼び出します)
		///
		///	A,B,Cと呼び出したいならば、
		///		static TaskName [] a = { CのTaskName,BのTaskName,AのTaskName };
		///		controller.PushScene(a);
		///	こんな感じ。
		/// </remarks>
		/// <param name="a"></param>
		public void PushScene(TaskName[] a)
		{
			taskNameList.AddRange(a);
		}

		/// <summary>
		///	シーンをすべて破棄
		/// </summary>
		/// <remarks>
		///	これを呼び出すと End が true を返すようになります。
		/// </remarks>
		public void ExitScene(){
			taskNameList.Clear();
			while (taskList.Count!=0)
				PopTask();
		}

		/// <summary>
		/// 現在実行しているシーンを取得
		/// </summary>
		/// <returns></returns>
		public TaskBase NowTask{
			get
			{
				if (taskList.Count == 0) return null;
				return taskList[taskList.Count - 1];
			}
		}

		/// <summary>
		/// 呼び出すべきシーンがもうないのか？
		/// </summary>
		/// <returns></returns>
		public bool End {
			get
			{
				return taskNameList.Count == 0 && taskList.Count == 0;
			}
		}

		/// <summary>
		///	TaskFactoryBaseを設定する(必須)
		/// </summary>
		/// <remarks>
		/// このシーンタスクコントローラーから呼び出しうるシーン(Task)の
		///	createのみをサポートしてあれば良い。
		/// </remarks>
		/// <param name="c"></param>
		public TaskFactoryBase<TaskName> TaskFactory;

		/*
			逐次実行の実現のためには、スタックさえあればそれで良い。
		*/
		private List<TaskName> taskNameList = new List<TaskName>();	//	生成するtaskのpriorityを予約しておくキュー
		private List<TaskBase> taskList = new List<TaskBase>();		//	タスクのcallstack

		public void Dispose()
		{
			Reset();
		}

		/// <summary>
		/// 全taskのreleaseおよび積まれているTaskNameのreset
		/// </summary>
		public void Reset()
		{
			foreach (TaskBase t in taskList)
			{
				IDisposable d = t as IDisposable;
				if (d != null) d.Dispose();
			}
			taskList.Clear();
			taskNameList.Clear();
		}

		private TaskName PopTaskName()
		{
			TaskName name = taskNameList[taskNameList.Count - 1];
			taskNameList.RemoveAt(taskNameList.Count - 1);
			return name;
		}
		private void PushTaskName(TaskName name) { taskNameList.Add(name); }

		/// <summary>
		/// popTaskするときにシーンタスクがIDisposableメンバであれば、
		/// Disposeを呼び出すことをここで保証する。
		/// </summary>
		private void PopTask() {
			if (taskList.Count == 0) return ;
			TaskBase t = taskList[taskList.Count - 1];
			IDisposable d = t as IDisposable;
			if (d != null)
				d.Dispose();
			taskList.RemoveAt(taskList.Count-1);
		}
	}

}

