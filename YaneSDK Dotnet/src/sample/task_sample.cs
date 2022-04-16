	/**
		タスクシステムを使うサンプル
		タスクシステムは、Yanesdkを使う上で必須課題である。

		具体的には、TaskControllとSceneControllクラスである。
		これは、system/taskcontroller.csに収められている。

		たとえば、二人対戦ゲームならば、
		プレイヤーひとりぶんを SceneControllerのインスタンスひとつで管理し、
		TaskControllerを介して、SceneController配下のタスク同士が交信を
		行なうことによってゲームを進行させる、というようなことも出来る。

		以下にこの二つのクラスを利用するサンプルを示す。
	*/

	public class Test
	{
		public void GameMain()
		{
			GameInfo info = new GameInfo();
			info.taskController = new TaskController();
			info.sceneController = new SceneController<TaskName>();
			info.sceneController.TaskFactory = new MySceneFactory();

			//	シーンコントローラーもタスクの一部なのでタスクコントローラーに
			//	登録しておく。
			// (シーンコントローラーは task priority == 1と固定する)
			info.taskController.AddTask(info.sceneController , 1);

			//	最初に実行するタスクをシーンコントローラーに指定
			info.sceneController.JumpScene(TaskName.Task0);

			while ( !info.taskController.End )
			{
				info.taskController.CallTask(info);
			}

			//	タスクコントローラーのDisposeを呼び出す
			//	その他のtaskはすべてこいつが管理しているので解放するときに
			//	IDisposable interfaceを持っていれば自動的にDisposeが呼び出されることが保証される。
			info.taskController.Dispose();
		}


		//	タスク名
		enum TaskName { Task0 , Task1 , Task2 , Task3 , Task4 };

		//	そのゲームで使う構造体各種よせ集め
		class GameInfo
		{
			public TaskController taskController;
			public SceneController<TaskName> sceneController;
			//	他、singleton的なものはすべてここに持たせる
			//	入力系のクラス,描画系のクラス等々。
		}

		class Task0 : TaskBase , IDisposable
		{
			public override int Task(Object o)
			{
				c++;
				if ( c == 5 )
				{
					( ( GameInfo ) o ).sceneController.JumpScene(TaskName.Task1);
				}
				Console.WriteLine("Task0");
				return 0;
			}
			public void Dispose() // ちゃんと呼び出されるか。
			{
				Console.WriteLine("Dispose Task0");
			}
			private int c;
		}

		class Task1 : TaskBase , IDisposable
		{
			public override int Task(Object o)
			{
				c++;
				GameInfo info = o as GameInfo;
				if ( c == 5 )
				{
					info.sceneController.CallScene(TaskName.Task2);
				}
				else if ( c == 10 )
				{
					info.sceneController.ExitScene();
					//	この呼び出し以降、このTask1.taskは呼び出されない

					//	ゲーム自体を終了させるならば、タスクコントローラーに
					//	要求する必要がある。
					//	例)
					info.taskController.Terminate();
				}
				Console.WriteLine("Task1");
				return 0;
			}
			public void Dispose() // ちゃんと即座に呼び出されるか。
			{
				Console.WriteLine("Dispose Task1");
			}
			private int c;
		}

		class Task2 : TaskBase
		{
			public override int Task(Object o)
			{
				c++;
				if ( c == 5 )
				{
					TaskName[] a = { TaskName.Task4 , TaskName.Task3 };
					( ( GameInfo ) o ).sceneController.PushScene(a);
					//	pushしてもすぐに呼び出されるわけではない。
					//	ReturnSceneしてはじめて呼び出される
					( ( GameInfo ) o ).sceneController.ReturnScene();
				}
				Console.WriteLine("Task2");
				return 0;
			}
			private int c;
		}

		class Task3 : TaskBase
		{
			public override int Task(Object o)
			{
				c++;
				if ( c == 5 )
				{
					( ( GameInfo ) o ).sceneController.ReturnScene();
				}
				Console.WriteLine("Task3");
				return 0;
			}
			private int c;
		}

		class Task4 : TaskBase
		{
			public override int Task(Object o)
			{
				c++;
				if ( c == 5 )
				{
					( ( GameInfo ) o ).sceneController.ReturnScene();
				}
				Console.WriteLine("Task4");
				return 0;
			}
			private int c;
		}

		class MySceneFactory : TaskFactoryBase<TaskName>
		{
			public override TaskBase CreateTask(TaskName name)
			{
				switch ( name )
				{
					case TaskName.Task0:
						return new Task0();
					case TaskName.Task1:
						return new Task1();
					case TaskName.Task2:
						return new Task2();
					case TaskName.Task3:
						return new Task3();
					case TaskName.Task4:
						return new Task4();
				}
				throw null; // never reached
			}
		}
	}
