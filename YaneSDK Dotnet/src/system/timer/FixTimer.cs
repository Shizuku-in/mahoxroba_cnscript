using System;
using Yanesdk.Timer;

namespace Yanesdk.Timer
{
	/// <summary>
	/// GameTimerクラスをゲームで使う場合、同じ描画フレーム内では同一の値が返ってきて
    /// 欲しいことがある。このFixTimerのほうは、updateが呼び出されるまではtimeを取得しても
    /// 同じ値しか返ってこない。updateが呼び出されるごとにgetで返る値が更新される。
    /// その他の使い方は、GameTimerクラスと同じである。
	/// </summary>
	public class FixTimer
	{
        /// <summary>
        /// タイマーのリセット。
        /// </summary>
		public void Reset() { timer.Reset(); dwTimeGetTime = 0; }
		/// <summary>
		/// GameTimerクラスのほうのtimeと同じだが、updateを呼び出すまでは値は更新されない。
		/// 設定もできる。
		/// </summary>
		/// <returns></returns>
		public int Time {
			get { return dwTimeGetTime; }
			set { timer.Time = value; dwTimeGetTime = value; }
		}
		/// <summary>
		/// タイマーの停止。
		/// </summary>
		public void Pause() { timer.Pause(); }
		/// <summary>
		/// タイマーの再開。
		/// </summary>
		public void Resume() { timer.Resume(); }

		/// <summary>
		/// 時刻のupdate。このメソッドを呼び出さない限りtimeで得られる値は更新されない。
		/// </summary>
		public void Update() {
			dwTimeGetTime = timer.Time;
		}

		protected GameTimer timer = new GameTimer();
		protected int dwTimeGetTime = 0;
	}
}
