using System;
using System.Collections.Generic;
using System.Text;

using Yanesdk.Ytl;

namespace Yanesdk.System
{
	/// <summary>
	/// CachedObjectを生成するためのFactoryのためのdelegate
	/// </summary>
	/// <returns></returns>
	public delegate ICachedObject CachedObjectFactory();

	/// <summary>
	/// ほげほげLoaderの基底クラス。具体的な使い方については、
	/// TextureLoaderやSoundLoaderを見てください。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CachedObjectLoader : IDisposable
		/*
		where
			T : ICachedObject

		 */
	{
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
		/// 定義ファイルを読み込む
		/// 
		/// 定義ファイルに書いてあるファイル名が実行ファイルからの相対pathなのか、それとも
		/// 定義ファイルの存在するフォルダからの相対pathなのかどうかは、
		/// IsDefRelativePathオプションに従う。
		/// </summary>
		public YanesdkResult LoadDefFile(string filename)
		{
			Release();

			basePath = FileSys.GetDirName(filename);
			YanesdkResult result = reader.LoadDefFile(filename, OptNum);
			if (result == YanesdkResult.NoError)
			{
				Dictionary<int,FilenamelistReader.Info>.KeyCollection
					keys = reader.Data.Keys;
				foreach (int key in keys)
				{
					// ファイル名は定義ファイル相対pathならそのようにする
					if (IsDefRelativePath)
					{
						reader.Data[key].name =
							FileSys.MakeFullName(basePath , reader.Data[key].name);
					}

					ICachedObject obj = OnDefFileLoaded(reader.Data[key]);
					dict.Add(key, obj);
				}
			}
			return result;
		}

		/// <summary>
		/// IsDefRelativePath = trueをしたのちに
		/// 設定ファイルを読み込む。
		/// </summary>
		/// <remarks>
		/// 最後のRはRelativeのR
		/// </remarks>
		/// <param name="filename"></param>
		/// <returns></returns>
		public YanesdkResult LoadDefFileR(string filename)
		{
			IsDefRelativePath = true;
			return LoadDefFile(filename);
		}

		/// <summary>
		/// 定義ファイルが読み込まれたときに実行する。
		/// 派生クラス側でoverrideして使うと良い。
		/// </summary>
		/// <returns></returns>
		protected virtual ICachedObject OnDefFileLoaded(FilenamelistReader.Info info)
		{
			return null;
		}

		/// <summary>
		/// 定義ファイルからの相対pathなのか
		/// </summary>
		public bool IsDefRelativePath
		{
			get { return isDefRelativePath; }
			set { isDefRelativePath = value; }
		}
		private bool isDefRelativePath = false;

		/// <summary>
		/// reader.LoadDefFileで指定するOptNum。
		/// 
		/// ※ これは継承必須
		/// </summary>
		protected virtual int OptNum
		{
			get { return 0; }
		}

		/// IsDefRelativePathがtrueの場合は、最後に定義ファイルを
		/// LoadDefFile/LoadDefFileRで読み込んだフォルダになる。
		/// 
		/// Loadするときに何も考えずに BasePath + filename　すれば良い。
		/// </summary>
		public string BasePath
		{
			get
			{
				if (IsDefRelativePath)
					return basePath;
				else
					return "";
			}
		}
		private string basePath;

		/// <summary>
		/// 定義ファイルの読み込みは、FilenamelistReaderに任せておけば良い。
		/// </summary>
		protected FilenamelistReader reader = new FilenamelistReader();

		/// <summary>
		/// このクラスで使うコードページを指定する。
		/// 一度設定すると再度設定するまで有効。
		/// ディフォルトでは、Shift_JIS。
		/// BOM付きのutf-16でも読み込める。
		/// </summary>
		public global::System.Text.Encoding CodePage
		{
			get { return reader.CodePage; }
			set { reader.CodePage = value; }
		}

		/*
		protected void LoadHelper(S s, int no)
		{
			if (!s.Loaded)
			{
				// 読み込まれてないのん？なんで？解放してしもたん？
				FilenamelistReader.Info info = reader.GetInfo(no);
				if (info != null)
				{
					s.Load(basePath + info.name);
					cache.ChangeCachedObjSize(no, s.BufferSize);
				}
			}
		}
		 */

		/// <summary>
		/// 格納しているオブジェクト
		/// </summary>
		protected Dictionary<int, ICachedObject> dict = new Dictionary<int, ICachedObject>();

		/// <summary>
		/// Cacheしているオブジェクトの取得を手伝う。
		/// 
		/// 指定された番号のものがなければnullが戻る。
		/// オブジェクトを取得したときには、Reconstructが自動的に呼び出される。
		/// </summary>
		/// <param name="no"></param>
		/// <returns></returns>
		protected ICachedObject GetCachedObjectHelper(int no)
		{
			if (!dict.ContainsKey(no))
				return null;
			ICachedObject t = dict[no]; // 読み込み時にオブジェクト自体は生成されていると仮定できる
			if (t.IsLost)
				t.Reconstruct(); // 念のためオブジェクトの再構築を行なっておく。

			// アクセスされたものとしてマーカーをつける？
			// ここでつけても各methodがつけないと意味がないか…。
			// t.CacheSystem.OnAccess(t);

			return t;
		}

		/// <summary>
		/// 確保しているオブジェクト(not resource)をすべて破棄
		/// </summary>
		public void Release()
		{
			Dictionary<int, ICachedObject>.ValueCollection
				values = dict.Values;
			foreach (ICachedObject obj in values)
				obj.Destruct();

			dict.Clear();
		}

		/// <summary>
		/// 確保しているオブジェクトをすべて破棄して終了する。
		/// </summary>
		public void Dispose()
		{
			Release();
		}
	}

}
