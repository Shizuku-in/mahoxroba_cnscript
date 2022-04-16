using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Yanesdk.System {
	/* test用コード
		DirEnumerator e = new DirEnumerator();
			e.setFileOnly(false);
			foreach (string name in e)
				MessageBox.Show(name);
	*/

	
	/// <summary>
	/// ディレクトリ内のファイルを列挙するためのクラス。
	/// 実行ファイル相対なのか、working directory相対なのかは
	/// FileSys.DirectoryPolicyに従う。
	/// </summary>
	/// <example>
	/// 使用例)
	/// <code>
	/// DirEnumerator d = new DirEnumerator;
	/// d.setDir("temp/");
	///	foreach(string filename in d)
	///		puts(filename);
	/// </code>
	/// ※　現在のところ圧縮ファイルの中まで見に行く機能は未実装。
	/// </example>
	public class DirEnumerator :  /*IEnumerator ,*/ IEnumerable {

		/// <summary>
		/// ディフォルトでは、setSubDir(true),setFileOnly(true)の状態。
		/// </summary>
		public DirEnumerator()
		{
			bSubDir = true;
			bFileOnly = true;
		}
		
		///	<summary>検索するディレクトリ名を設定。</summary>
		/// <remarks>
		///	最後は'\'でも'/'でも、そうでなくとも良い。
		/// </remarks>
		public void SetDir(string dirname_) {
			dirname = dirname_;
		}

		///	<summary>サブフォルダも検索対象にするかしないか(default:true)。</summary>
		public bool SubDir {
			set { bSubDir = value; }
			get { return bSubDir; }
		}

		///	<summary>ファイルのみを対象とするかしないか(default:true)。</summary>
		public bool FileOnly {
			set { bFileOnly = value; }
			get { return bFileOnly; }
		}

		///	<summary>nameはファイル名か？</summary>
		public static bool IsFile(string name)
		{
			using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
				return File.Exists(name);
		}

		/// <summary>nameはディレクトリ名か？</summary>
		public static bool IsDir(string name)
		{
			using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
				return Directory.Exists(name);
		}

		/// <summary>
		/// 該当するファイルを一気に取得する。
		/// (ファイル数が多いとメモリが足りなくなる可能性もあるが、
		/// そうでなければこちらのほうが使いやすいだろう)
		/// </summary>
		/// <returns></returns>
		public string[] GetEntries()
		{
			List<string> list = new List<string>();
			foreach (string s in this)
				list.Add(s);
			return list.ToArray();
		}
		
		/// <summary>
		/// ディレクトリ内のファイルを列挙するiterator。
		/// </summary>
		public virtual IEnumerator GetEnumerator()
		{
			using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
			{
				if (dirname == null || dirname.Length == 0) dirname = ".";
				string[] items;
				try { items = Directory.GetFileSystemEntries(dirname); }
				catch { yield break; }
				foreach (string item in items)
				{
					if (IsFile(item))
						yield return Strip(item);
					else
					{
						if (!FileOnly)
							yield return Strip(item);

						if (SubDir)
						{
							string[] items2;
							try { items2 = Directory.GetFileSystemEntries(item); }
							catch { yield break; }
							foreach (string item2 in items2)
								yield return Strip(item2);
						}
					}
				}
			}
		}

		/// <summary>
		/// "./test.txt"→"test.txt"のようにする。
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private string Strip(string path)
		{
			try
			{
				string t = path.Substring(0, 2);
				if (path.Substring(0, 2) == "." + Path.DirectorySeparatorChar)
					return path.Remove(0, 2);
				return path;
			}
			catch
			{
				return path;
			}
		}

		/// <summary>
		/// フォルダ名
		/// </summary>
		protected string dirname;
		/// <summary>
		/// 検索のときにサブフォルダを対象とするのか？
		/// </summary>
		protected bool bSubDir;
		/// <summary>
		/// ファイルのみを対象として、フォルダは列挙する対象としないのか？
		/// </summary>
		protected bool bFileOnly;

	}
}
