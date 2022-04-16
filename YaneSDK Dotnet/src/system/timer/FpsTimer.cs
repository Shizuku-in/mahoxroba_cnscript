using System;
using System.Threading;

namespace Yanesdk.Timer
{
	/// <summary>
	/// 時間を効率的に待つためのタイマー。
	/// </summary>
	/// <remarks>
	/// フレームレート（一秒間の描画回数）を60FPS（Frames Par Second）に
	/// 調整する時などに使う。
	/// 
	/// while (true){
	///		fpstimer.waitFrame();
	///		if(!fpstimer.toBeSkip()) Draw();
	/// }
	/// と書けば、1秒間にsetFPSで設定された回数だけDraw関数が呼び出される
	/// </remarks>
	/**
	    // Game用のthreadを作らずにcallbackで書く場合
		// threadを作っても良いのだが、生成スレッドと異なると
		// FormのControlにアクセスできなくなってしまうので、あまり得策ではない。
        public Form1()
        {
            InitializeComponent();

			this.timer1.Interval = 3; // 3msごとにcallback
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            this.timer1.Start();
			timer.setFps(60);
		}

		System.Windows.Forms.Timer interval_timer = new System.Windows.Forms.Timer();
		FpsTimer timer = new FpsTimer();

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (!timer.toBeRendered())
				return;

			myRender();
		}
	 */
	/*
	 * 　以下、threadを作って描画する例

		void MyThread()
		{
			while ( isValid )
			{
				if ( fps.ToBeRendered )
				{
					if ( !fps.ToBeSkip )
					{
						window.Screen.Select();
						window.Screen.Clear();
						window.Screen.Blt(txt , x++ % 300 , 0);
						fpslayer.OnDraw(window.Screen , 100 , 40);
						window.Screen.Update();
					//	UserControl1 u = this.Controls[0] as UserControl1;
					//	u.textBox1.Text = "ABC"; // これダメ。threadが違うのでアクセスできない
					}
				}
				else
					Thread.Sleep(1);
		
		//　あるいは、
	 
				fps.WaitFrame();
				{
					if (! fps.ToBeSkip )
					{
						window.Screen.Select();
						window.Screen.Clear();
						window.Screen.Blt(txt , x++ % 300 , 0);
						fpslayer.OnDraw(window.Screen , 100 , 40);
						window.Screen.Update();
					}
				}
			}
	*/	 
	public class FpsTimer
	{
        /// <summary>
        /// FPS値の設定（イニシャライズを兼ねる）と取得。
        /// ディフォルトでは60fps。0にするとnon-wait mode(FPS = ∞)
        /// </summary>
        /// <param name="fps"></param>
		public float Fps
		{
			set
			{
				lastDrawTime = timer.Time; // 前回描画時間は、ここで設定
				bFrameSkip = false;
				frameSkipCount = 0;
				frameSkipCountNow = 0;
				drawCount = 0;

				this.fps = value;
				if (value == 0)
				{	// non-wait mode
					return;
				}
				// １フレームごとに何ms待つ必要があるのか？[ms]
				fpsWait = 1000 / fps;

			}
			get
			{
				return fps;
			}
		}

        /// <summary>
        /// FPSの取得（測定値）
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// 1秒間に何回WaitFrameを呼び出すかを、
        /// 前回32回の呼び出し時間の平均から算出する。
        /// </remarks>
		public float RealFps {
			get
			{
				if (drawCount < 16) return 0; // まだ16フレーム計測していない
				if (drawCount < 32)
				{
					float t = aDrawTime[(drawCount - 1)]	// 前回時間
						  - aDrawTime[(drawCount - 16)];	// 15回前の時間
					if (t == 0)
					{
						return 0;	//	測定不能
					}
					return (1000 * 15.0f) / t;
					// 平均から算出して値を返す（端数は四捨五入する）
				}
				else
				{
					float t = aDrawTime[(drawCount - 1) & 31]	 // 前回時間
						  - aDrawTime[(drawCount) & 31];	 // 31回前の時間
					if (t == 0)
					{
						return 0;	//	測定不能
					}
					return (1000 * 31.0f) / t;
				}
			}
		}

		/// <summary>
		/// FPSの取得(測定値)
		/// </summary>
		/// <remarks>
		/// getRealFpsの戻り値はfloatなので、こちらは、小数点以下を四捨五入して返すメソッド。
		/// </remarks>
		/// <returns></returns>
		public int RealFpsInt { get { return (int)(RealFps + 0.5); } }

		/// <summary>
		///  CPU稼動率の取得（測定値）
		/// </summary>
		/// <remarks>
		///	ＣＰＵの稼働率に合わせて、0～100の間の値が返る。
		///	ただしこれは、WaitFrameでSleepした時間から算出されたものであって、
		/// あくまで参考値である。
		/// 最新の16フレーム間で余ったCPU時間から計測する。16フレーム経過していない場合は100が返る。
		/// ただし、ここで言うフレームとは、waitFrameの呼び出しごとに１フレームと計算。
		/// </remarks>
		/// <returns></returns>
		public float CpuPower
		{
			get
			{
				if (drawCount < 16) return 100; // まだ16フレーム計測していない

				float t = 0;
				for (int i = 0; i < 16; i++)
					t += aElapseTime[i]; // ここ16フレーム内でFPSした時間
				// return 1-t/(1000*16/m_dwFPS)[%] ; // FPSノルマから算出して値を返す

				float w = 100 - (t * fps / 160);
				if (w < 0) w = 0;
				return w;
			}
		}

        /// <summary>
        /// スキップされたフレーム数を取得
        /// </summary>
        /// <remarks>
		///		setFpsされた値までの描画に、ToBeSkipがtrueになっていた
		///		フレーム数を返す。ただし、ここで言うフレーム数とは、
		///		waitFrameの呼び出しごとに１フレームと計算。
        /// </remarks>
        /// <returns></returns>
		public float SkipFrame
		{
			get { return frameSkipCount; }

		}


		/// <summary>
		/// resetする。
		/// </summary>
		/// <remarks>
		/// FPSTimerのインスタンス生成後、リソースを読み込み、そのあとゲームを
		/// 開始した場合、本来描画すべき時間から経過しているため、FpsTimerは
		/// 処理落ちしていると判断して、描画をskipしてしまう。
		/// 
		/// そこで、ゲームがはじまる直前にResetを行ない、FpsTimerが処理落ちしている
		/// と判定しないようにする必要がある。
		/// </remarks>
		public void Reset()
		{
			lastDrawTime = timer.Time;
		}
	

		/// <summary>
		/// １フレーム分の時間が来るまで待つ
		/// </summary>
		/// <remarks>
		///	メインループのなかでは、描画処理を行なったあと、
		///	このwaitFrameを呼び出せば、setFPSで設定した
		///	フレームレートに自動的に調整される。
		/// 
		/// toBeSkipメンバも参照すること。
		/// </remarks>
		public void WaitFrame()
		{
			int t = timer.Time; // 現在時刻

			//	スキップレートカウンタ
			if ( fps != 0 && ( ( drawCount % ( int ) fps ) == 0 ) )
			{
				frameSkipCount = frameSkipCountNow;
				frameSkipCountNow = 0;
			}

			// かなり厳粛かつ正確かつ効率良く時間待ちをするはず。
			if ( fps == 0 )
			{
				aElapseTime[drawCount & 31] = 0;
				lastDrawTime = t;
				aDrawTime[drawCount & 31] = lastDrawTime;  // Drawした時間を記録することでFPSを算出する手助けにする
				bFrameSkip = false;
				return; // Non-wait mode
			}

			// justで描画したとして計算する
			lastDrawTime += fpsWait;

			float delay = t - lastDrawTime;
			int et = 0;
			if ( delay < 0 )
			{
				bFrameSkip = false;
				// 時間が有り余っている
				et = ( int ) -delay;
				Thread.Sleep(et);	// SDL_Delayの置き換え
			}
			else if ( delay <= fpsWait*2 )
			{
				// 遅れは1～2フレーム以内である
				bFrameSkip = false;
			}
			else /* if ( delay < fpsWait * 2 ) */
			{
				// 1フレーム分は間違いなく時間が
				// 足りてないのでフレームスキップしたほうがいいのだが
				bFrameSkip = true;

				//	まったく描画無しだと暴走しているのかと思われると癪なので、
				//  4フレに1回は強制的に描画する。
				if ( ++continualFrameSkipCount == 4 )
				{
					bFrameSkip = false;
					continualFrameSkipCount = 0;
					lastDrawTime = t; // 今回描画したのでタイマをもとに。
				}
			}

			if ( bFrameSkip )
			{
				frameSkipCountNow++;
				lastDrawTime += fpsWait; // 描画時間を進める
			}
			else
			{
				aDrawTime[drawCount & 31] = lastDrawTime;  // Drawした時間を記録することでFPSを算出する手助けにする
				if ( ++drawCount == 64 )
					drawCount = 32;
				// 32に戻すことによって、0～31なら、まだ32フレームの描画が終わっていないため、
				// FPSの算出が出来ないことを知ることが出来る。

				aElapseTime[drawCount & 31] = et;
			}
		}

		/// <summary>
		/// C#でフォームを書いていると、数msごとにevent callbackをかけて、
		/// そのcallback先のハンドラで、一定のFPSで描画を行ないたいことがある。
		/// このメソッドは、それを実現する。
		/// </summary>
		/// <remarks>
		/// 
		/// 必ず以下のように書くべし！
		///		timer = new System.Windows.Forms.Timer();
		///		timer.Interval = 1;
		///		timer.Tick += delegate { OnCallback(); };
		///		timer.Start();
		///
		///
		///		// OnDrawのCallback用タイマ
		///		private global::System.Windows.Forms.Timer timer;
		///
		///		public void OnCallback()
		///		{
		///			if ( !gameContext.FPSTimer.ToBeRendered )
		///				return;
		///
		///			// フレームスキップ処理
		///			OnMove(); // 論理的な移動 
		///			if ( gameContext.FPSTimer.ToBeSkip ){
		///				return; 
		///			}
		///			OnDraw(); // 画面描画
		///		}
		/// </remarks>
		/// <returns></returns>
		public bool ToBeRendered
		{
			get
			{
				// 以下のソースはwaitFrameからの改変

				// これある日突然マイナスになるので本当はまずいんだが、
				// そのときは一瞬コマ落ちするだけなのでいいか…。
				int t = timer.Time; // 現在時刻
				if (t - lastDrawTime < fpsWait)
				{
					// 時間あまっちょるのでまだ描画しない。
					return false;
				}

				try
				{
					// justで描画したとして計算する
					lastDrawTime += fpsWait;

					// かなり厳粛かつ正確かつ効率良く時間待ちをするはず。
					if ( fps == 0 )
					{
						aElapseTime[drawCount & 31] = 0;
						bFrameSkip = false;
						return true; // Non-wait mode
					}

					float delay = t - lastDrawTime;
					if ( delay <= fpsWait )
					{
						bFrameSkip = false;
						aElapseTime[drawCount & 31] = fpsWait-delay;
					}
					else if ( delay < fpsWait * 2 )
					{	// 時間足りてないので、時間消費する必要なし
						bFrameSkip = false;
						aElapseTime[drawCount & 31] = 0;
					}
					else
					{	// 1フレーム分、時間足りてないのでフレームスキップしたほうがいいのだが
						bFrameSkip = true;
						aElapseTime[drawCount & 31] = 0;

						//	まったく描画無しだと暴走しているのかと思われると癪なので、
						//  4フレに1回は強制的に描画する。
						if ( ++continualFrameSkipCount == 4 )
						{
							bFrameSkip = false;
							continualFrameSkipCount = 0;

							lastDrawTime = t; // どうしようもないので時間を現在に
						}
					}

					if ( bFrameSkip )
					{
						frameSkipCountNow++;
					}
					return true;
				}
				finally
				{
					if ( !bFrameSkip )
					{
						//	スキップレートカウンタ
						if ( fps != 0 && ( ( drawCount % ( int ) fps ) == 0 ) )
						{
							frameSkipCount = frameSkipCountNow;
							frameSkipCountNow = 0;
						}

						// fps == 0は描画扱いなので描画時刻を記録する必要がある。
						aDrawTime[drawCount & 31] = lastDrawTime;  // Drawした時間を記録することでFPSを算出する手助けにする

						if ( ++drawCount == 64 )
							drawCount = 32;
						// 32に戻すことによって、0～31なら、まだ32フレームの描画が終わっていないため、
						// FPSの算出が出来ないことを知ることが出来る。
					}
				}
			}
		}

		/// <summary>
		/// スキップすべきかを示すフラグを返す。
		/// </summary>
		/// <remarks>
		/// while (true){
		///		fpstimer.waitFrame();
		///		if(!fpstimer.toBeSkip) Draw();
		/// }
		/// と書けば、1秒間にsetFPSで設定された回数だけDraw関数が呼び出される
		/// </remarks>
		/// <returns></returns>
		public bool ToBeSkip
		{
			get { return bFrameSkip; }
		}

		public FpsTimer()
		{
			timer = new GameTimer();
			Fps = 60;
			continualFrameSkipCount = 0;
			Reset();
		}

		/// <summary>
        /// FPS(ディフォルトで60)
		/// </summary>
		private float	fps;
		/// <summary>
        /// 1000/FPS; // 60FPSに基づくウェイト時間 [ms]単位
		/// </summary>
		private float	fpsWait;					
		/// <summary>
        /// 前回の描画時刻
		/// </summary>
		private float lastDrawTime;

		/// <summary>
        /// FPS測定用の描画時間計算用
		/// </summary>
		private float[]	aDrawTime = new float[32];
		/// <summary>
        /// CPU Power測定用
		/// </summary>
		private float[]	aElapseTime = new float[32];

		/// <summary>
        /// WaitFrameを呼び出された回数
		/// </summary>
		private int drawCount;
		/// <summary>
        /// 次のフレームはスキップするのか？
		/// </summary>
		private bool	bFrameSkip;
		/// <summary>
        /// フレームスキップカウンタ
		/// </summary>
		private int	frameSkipCount;
		/// <summary>
        /// 計測中のフレームスキップカウンタ
		/// </summary>
		private int	frameSkipCountNow;

		/// <summary>
		/// フレームスキップ10回に1回は強制描画する。そのためのカウンタ。
		/// </summary>
		private int continualFrameSkipCount;

        private GameTimer	timer;
	}
}
