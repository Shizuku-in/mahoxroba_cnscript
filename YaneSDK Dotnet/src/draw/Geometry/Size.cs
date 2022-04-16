using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Draw
{
	/// <summary>
	/// サイズを表している構造体。
	/// </summary>
	public class Size
	{
		/// <summary>
		/// 幅。
		/// </summary>
		public float Cx;

		/// <summary>
		/// 高さ。
		/// </summary>
		public float Cy;

		/// <summary>
		/// コンストラクタ。
		/// </summary>
		/// <param name="cx_"></param>
		/// <param name="cy_"></param>
		public Size(float cx_, float cy_)
		{
			Cx = cx_;
			Cy = cy_;
		}

		/// <summary>
		/// 各データを設定する。
		/// </summary>
		/// <param name="Cx"></param>
		/// <param name="Cy"></param>
		public void SetSize(float cx, float cy)
		{
			this.Cx = cx; this.Cy = cy;
		}
	}
}
