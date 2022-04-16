using System;
using Yanesdk.Ytl;
using Sdl;

namespace Yanesdk.Timer
{
	/// <summary>
	/// wrapされたtimer。Timerクラスからは、このgetTimeだけを呼び出す。
	/// </summary>
	internal class Timer_ : IDisposable
	{
		public uint getTime() { return SDL.SDL_GetTicks(); }

		private RefSingleton<Yanesdk.Sdl.SDL_TIMER_Initializer>
			init = new RefSingleton<Yanesdk.Sdl.SDL_TIMER_Initializer>();

		public void Dispose()
		{
			init.Dispose();
		}
	}

	/// <summary>
	/// 経過時間を取得するためのクラスです。
	/// </summary>
	/// <remarks></remarks>
	public class GameTimer : IDisposable
	{
        public GameTimer() { Reset(); }

		/// <summary>
		/// タイマーの値を設定/取得する。単位は [ms]。
		/// </summary>
		/// <returns>返し値はintなので2^31/(60*60*24*1000)≒24.8日で負数になる。
        /// そこから24.8日で0になる。
        /// </returns>
		public int Time {
			get
			{
				if (bPaused > 0)
				{
					return dwPauseTime - dwOffsetTime;
				}
				else
				{
					return (int)getTime() - dwOffsetTime;
				}
			}
			set
			{
				if (bPaused > 0)
				{
					dwOffsetTime = dwPauseTime - value;
				}
				else
				{
					dwOffsetTime = (int)getTime() - value;
				}
			}
		}

		/// <summary>
		/// タイマーをリセットする
        /// このメソッドを呼び出して、直後にtimeを取得した場合は(おそらくは)0が返る
        /// pause状態もリセットされる。
		/// </summary>
		public void Reset() {
            dwOffsetTime = (int)getTime();
			bPaused = 0;
		}

		/// <summary>
		/// リセットするが、Pauseされた状態になっていることを保証する
		/// </summary>
		public void PausedReset()
		{
			dwPauseTime = dwOffsetTime;
			bPaused = 1;
		}

		/// <summary>
		/// タイマーの経過を一時停止させる。
		/// </summary>
		public void Pause() {
			if (bPaused++ == 0) {
                dwPauseTime = (int)getTime();
			}
		}

		/// <summary>
		/// pauseを呼び出して一時停止させたタイマーを再開させる。
        /// pauseを複数回呼び出した場合、その回数と同じだけresumeを呼び出さないと
        /// pauseしていたものは再開されない。
		/// </summary>
		public void Resume() {
			if (--bPaused == 0) {
                dwOffsetTime += (int)getTime() - dwPauseTime;
			}
		}

		/// <summary>
		/// getTimeで取得している値と、このメソッドが返すべき値とのオフセット値。
		/// </summary>
		protected int dwOffsetTime;
		/// <summary>
		/// pause中であれば、pauseされた時刻。
		/// </summary>
		protected int dwPauseTime;

		/// <summary>
		/// pause中かどうかを判定する
		/// </summary>
		public bool IsPaused
		{
			get { return bPaused!=0; }
		}
		
		/// <summary>
		/// pause中であるかを表すフラグ。
		/// </summary>
		protected int bPaused;

		/// <summary>
		/// 強制的に
		/// ・pause状態にする(isPause == true時)
		/// ・pause状態を解除する(isPause == false時)
		/// 
		/// すでになっている場合は、それ以上何もしない。
		/// </summary>
		public void PauseForced(bool isPause)
		{
			if ( isPause )
			{
				// pauseされていない状態からならば
				// 一度Pauseを呼び出すだけでPauseされることは保証されている
				if ( bPaused == 0 )
					Pause();
			}
			else
			{
				// pauseされている状態からならば
				// bPause(pauseのための参照カウンタ)を1にして
				// Resumeを呼び出せば、Resumeされることが保証される
				if ( bPaused != 0 )
				{
					bPaused = 1;
					Resume();
				}
			}
		}

		/// <summary>
		/// 現在の時刻をTimer_クラスを呼び出して取得する。
		/// </summary>
		private uint getTime() { return timer_.getTime(); }

		/// <summary>
		/// Timerのdispose。呼び出さなくとも良いが、きちんと呼び出しておくと
		/// すべてのTimerのinstanceが無くなった時点でSDL_TIMERの終了処理を
		/// 行なうというご利益がある。
		/// </summary>
		public void Dispose()
		{
			timer_.Dispose();
		}

		private Timer_ timer_ = new Timer_();
	}
}
