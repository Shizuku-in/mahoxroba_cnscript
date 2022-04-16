using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Timer
{
	/// <summary>
	/// ゲームで使うと便利なカウンタ
	/// </summary>
	public class Counter
	{
		public Counter()
		{
			Init(0,0);
		}

		/// <summary>
		/// カウンタの初期化
		/// start ＜ endならば +1ずつupcount 
		/// start ＞ endならば -1ずつdowncount
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public Counter(int start , int end)
		{
			Init(start , end);
		}


		/// <summary>
		/// 保持しているカウンタ
		/// 
		/// 値を更新するためにはUpdateを呼び出す必要がある。
		/// </summary>
		public int Count
		{
			get { return count; }
			set { count = value; }
		}
		private int count;

		/// <summary>
		/// カウンタのupdate。
		/// これを呼び出さないと値は更新されない。
		/// </summary>
		public void Update()
		{
			if ( IsEnd )
				return;

			count += step;

			// 飛び越えるかどうかをチェックする必要がある
			if ( start < end && end< count )
			{
				count = end;
			}
			else if ( start > end && count < start )
			{
				count = start;
			}
		}

		/// <summary>
		/// カウンタの初期化
		/// Count = start になる
		/// </summary>
		public void Init(int start,int end,int step)
		{
			this.start = start;
			this.end = end;
			this.count = start;
			this.step = step;
		}

		/// <summary>
		/// 開始値に戻る
		/// </summary>
		public void GoToStart()
		{
			count = start;
		}

		/// <summary>
		/// カウンタの初期化。
		/// start ＜ endならば +1ずつupcount 
		/// start ＞ endならば -1ずつdowncount
		/// </summary>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public void Init(int start , int end)
		{
			if ( start < end )
				Init(start , end , 1);
			else
				Init(start , end , -1);
		}

		private int start;
		private int end;
		private int step;

		/// <summary>
		/// カウントが終端に達していれば終了
		/// </summary>
		public bool IsEnd
		{
			get { return count==end; }
		}
	
	
	}
}
