using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// Listクラスの足りない部分を補ってくれるヘルパクラス
	/// </summary>
	public class ListHelper<T>
	{
		/// <summary>
		/// T[] への変換子は必須だろう
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public static T[] ConvertToArray(List<T> arg)
		{
			if (arg == null) return null;
			T[] a = new T[arg.Count];
			for (int i = 0; i < arg.Count; ++i)
				a[i] = arg[i];

			return a;
		}
	}
}
