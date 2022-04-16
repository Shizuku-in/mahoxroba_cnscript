using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.System
{

	/// <summary>
	/// CurrentDirectoryHelperで使う定数
	/// </summary>
	public enum CurrentDirectoryEnum
	{
		StartupDirectory, // 起動時のworking directory
		WorkingDirectory, // 現在のcurrent directory
		ExecuteDirectory, // 実行ファイルの存在するdirectory相対
	}

	/// <summary>
	/// カレントフォルダを一時的に変更するためのヘルパクラス
	/// </summary>
	public class CurrentDirectoryHelper : IDisposable
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="e">カレントフォルダの設定</param>
		public CurrentDirectoryHelper(CurrentDirectoryEnum e)
		{
			switch (e)
			{
				case CurrentDirectoryEnum.ExecuteDirectory:
					SetCurrentDir(global::System.AppDomain.CurrentDomain.BaseDirectory);
					break;
				case CurrentDirectoryEnum.StartupDirectory:
					SetCurrentDir(startUpDir);
					break;
				case CurrentDirectoryEnum.WorkingDirectory:
					// do nothing
					break;

				default:
					throw null; // ありえない
			}
		}

		/// <summary>
		/// カレントフォルダの設定
		/// </summary>
		/// <param name="path"></param>
		private void SetCurrentDir(string path)
		{
			try
			{
				if (path != null)
				{
					currentDir = Environment.CurrentDirectory;
					Environment.CurrentDirectory = path;
				}
			}
			catch // CurrentDirectoryの変更に失敗しうる。
			{
				currentDir = null;
			}
		}

		/// <summary>
		/// 変更前のカレントフォルダ
		/// </summary>
		private string currentDir = null;

		/// <summary>
		/// 起動時のフォルダ
		/// </summary>
		private static string startUpDir = Environment.CurrentDirectory;

		/// <summary>
		/// 変更していたカレントフォルダを元に戻す
		/// </summary>
		public void Dispose()
		{
			if (currentDir != null)
			{
				try
				{
					Environment.CurrentDirectory = currentDir;
				}
				catch
				{ }
			}
		}

	}

}
