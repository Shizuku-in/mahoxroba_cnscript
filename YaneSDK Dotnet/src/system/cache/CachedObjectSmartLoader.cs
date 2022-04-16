using System;
using System.Collections.Generic;
using System.Text;
using Yanesdk.Ytl;

namespace Yanesdk.System
{
	/// <summary>
	/// CachedObjectLoaderの親玉。
	/// 
	/// CachedObjectLoaderのfactoryである(｀ω´)
	/// 
	/// T : CachedObjectLoader派生クラスを渡す
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="S"></typeparam>
	public class CachedObjectSmartLoader<T> :
		IDisposable
			where
		T : CachedObjectLoader , new()
	{
		/// <summary>
		/// 定義ファイルの読み込み。
		/// 
		/// ここで生成したCachedObjectLoaderのDisposeを呼び出さないこと。
		/// (同時に同じ定義ファイルをLoadDefFileしているCachedObjectLoaderのインスタンスと
		/// 同一インスタンスなので、そちら側で困ったことになる)
		/// </summary>
		/// <param name="defFile"></param>
		/// <returns></returns>
		public T LoadDefFile(string defFile)
		{
			// 念のため、一意な名前にしておく。
			// ex. "./def.txt"と"def.txt"が別のファイル名扱いされると
			// Loaderが別になってしまうので。
			string defFileName = FileSys.MakeFullName("", defFile);

			if ( maps.ContainsKey(defFileName) )
				return maps[defFileName];

			T loader = new T();
			loader.Factory = this.Factory;

			OnLoadDefFile(loader);

			loader.IsDefRelativePath = this.isDefRelativePath;
			loader.LoadDefFile(defFile); // このresultがどうなってようがそれは知らん

			maps.Add(defFileName , loader);
			return loader;
		}

		/// <summary>
		/// LoadDefFileのIsDefRelativePathの設定をかねる版。
		/// IsDefRelativePathにはこの値は反映されない。
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="isDefRelativePath"></param>
		/// <returns></returns>
		public T LoadDefFile(string filename , bool isDefRelativePath)
		{
			bool b = this.isDefRelativePath;
			
			this.isDefRelativePath = isDefRelativePath;
			T loader = LoadDefFile(filename);

			this.isDefRelativePath = b;

			return loader;
		}

		/// <summary>
		/// LoadDefFileでT型をnewした直後に何らかの処理を入れたいときは、
		/// これをoverrideして。
		/// </summary>
		/// <param name="loader"></param>
		protected virtual void OnLoadDefFile(T loader) {}

		/// <summary>
		/// CachedObjectを生成するためのFactoryのためのdelegate
		/// を設定する機能
		/// </summary>
		/// <returns></returns>
		public CachedObjectFactory Factory
		{
			get { return factory_; }
			set { factory_ = value; }
		}
		protected CachedObjectFactory factory_;
		// 大文字小文字違いだけのものはCLSに準拠しない(´ω`)

		/// <summary>
		/// LoadDefFileで定義ファイルの置いてあったフォルダからの相対pathで
		/// ファイルを読み込むのか
		/// 
		/// これがfalseの場合、実行ファイルからの相対path。
		/// (default = false)
		/// 
		/// これを設定しておけば、LoadDefFileでは T(ほにゃららLoader)の
		/// IsDefRelativePathを、ここで設定した値にしたものが返る。
		/// </summary>
		public bool IsDefRelativePath
		{
			get { return isDefRelativePath; }
			set { isDefRelativePath = value; }
		}
		private bool isDefRelativePath;

		/// <summary>
		/// 定義file名から実体ファイルへのmap
		/// </summary>
		protected Dictionary<string,T> maps = new Dictionary<string,T>();

		/// <summary>
		/// こいつから生成したものはすべて破棄
		/// こいつが生成したloaderすべてのDispose()も呼び出す。
		/// 
		/// ゲームの規模が大きい場合、シーン間の移動などに際してすべての画像を解放して良いとされる
		/// 状況ならば、そのタイミングでこのDisposeを呼び出したほうが良いだろう。
		/// </summary>
		public void Dispose()
		{
			foreach ( CachedObjectLoader loader in maps.Values )
				loader.Dispose();
		}
	}
}
