using System;

using Yanesdk.Ytl;
using Yanesdk.System;
using System.Collections.Generic;

namespace Yanesdk.Sound
{
	/// <summary>
	/// Sound の一括読み込みクラス。
	/// </summary>
	/// <remarks>
	///	Sound を cache したり、自動解放したりいろいろします。
	///	詳しいことは、親クラス CacheLoader を見てください。
	///
	///	int	loadDefFile(char[] filename);
	///	で読み込む定義ファイルは、
	///			a.wav , -1 , 2
	///			b.wav , -1
	///			c.wav ,    , 5
	///	のように書き、ひとつ目は、ファイル名、二つ目は、option、3つ目は読み込む番号である。
	///	optionとして再生するチャンクナンバーを指定できる。
	///	これは指定しない場合 0 を意味し、その場合、その時点で再生していないチャンクを
	///	用いて再生する。
	///	</remarks>
	/// <remarks>
	/// cacheするサウンドファイルのリソース上限を設定。
	/// (defaultでは64MB
	/// oggファイルであれ、内部では非圧縮の状態にして再生するので、
	/// そこでのサイズがリソースの値となる。3分程度のoggであれ、
	/// 44KHzで再生するならば40MB程度食うことは覚悟したほうがいいだろう。
	/// 
	/// そのへんを考慮に入れて適切な値に設定すべきである。
	/// </remarks>
	/// <example>
	/// クロスフェードのサンプル：
	/// <code>
	///	int main(){
	///		SoundLoader ca = new SoundLoader();
	///		ca.loadDefFile("filelist.txt");
	///		//	この↑ファイルの中身は、
	///		//		1.ogg
	///		//		2.ogg
	///		//	と、再生するファイル名が書いてある
	///
	///		ca.playBGMFade(0,3000);
	///		//	0番目のファイルを読み込み、そいつをフェードインさせながら再生
	///		// ca.get(0).playFade(3000);でも可
	///
	///		GameTimer t = new GameTimer();
	///
	///		while(t.get()<5000) {}
	///
	///		// ca.playBGMFade(1,3000);
	///		//	1番目のファイルをフェードイン再生
	///		// ca.stopBGMFade(0,3000);
	///		//	0番目のファイルをフェードアウト再生
	///
	///		ca.playBGMCrossFade(1,3000); // こんな関数もある。
	///
	///		t.Reset();
	///		while(t.get()<5000) {}
	///
	///		return 0;
	///	}
	/// </code>
	///	※　サウンドにおけるクロスフェードとは、片方のBGMがフェードしながら、もう片方がフェードインしてくることを言います。
	///
	///
	///	このoptionは、 Sound クラスで load のときに指定する、
	///	chunk番号として渡される。指定しなければ0である。
	///	</example>
	public class SoundLoader : CachedObjectLoader
	{
		#region ctor
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public SoundLoader()
		{
		}
		#endregion

		#region methods

		#region Soundオブジェクト取得
		/// <summary>
		///	指定された番号のオブジェクトを構築して返す
		/// </summary>
		/// 暗黙のうちにオブジェクトは構築され、loadされる。
		///	定義ファイル上に存在しない番号を指定した場合はnullが返る
		///	ファイルの読み込みに失敗した場合は、nullは返らない。
		///	定義ファイル上に存在しない番号のものを再生することは
		///	考慮しなくてもいいと思われる．．。
		/// <param name="no"></param>
		/// <returns></returns>
		public Sound GetSound(int no)
		{
			return GetCachedObjectHelper(no) as Sound;
		}

		/// <summary>
		/// SoundHelperメソッドに渡す用のdelegate
		/// </summary>
		/// <param name="sound"></param>
		/// <returns></returns>
		public delegate YanesdkResult SoundCallbackDelegate(Sound sound);
		
		/// <summary>
		/// サウンド再生で用いると便利かも知れないヘルパメソッド
		/// 
		/// 詳しいことは、この実装と、このメソッドの呼び出し元を見て。
		/// </summary>
		/// <param name="no"></param>
		/// <param name="dg"></param>
		/// <returns></returns>
		public YanesdkResult SoundHelper(int no,SoundCallbackDelegate dg)
		{
			if (!EnableSound)
				return YanesdkResult.NoError; // そのまま帰ればok

			Sound s = GetSound(no);
			if (s == null)
				return YanesdkResult.PreconditionError; // 存在しない
			else
				return dg(s);
		}
		#endregion

		#region 通常の再生関連
		/// <summary>
		///	直接指定された番号のサウンドを再生させる
		/// </summary>
		/// <remarks>
		/// 失敗時はYanesdkResult.no_error以外が返る。
		///	(定義ファイル上に存在しない番号を指定した場合や
		///	ファイルの読み込みに失敗した場合もYanesdkResult.no_error以外が返る)
		/// </remarks>
		/// <param name="no"></param>
		/// <returns></returns>
		public YanesdkResult Play(int no) 
		{
			return SoundHelper(no, delegate(Sound s) { return s.Play(); });
		}

		/// <summary>
		/// FadeInつきのplay。FadeInのスピードは[ms]単位。
		/// </summary>
		/// <remarks>
		/// 失敗時はYanesdkResult.no_error以外が返る。
		///	(定義ファイル上に存在しない番号を指定した場合や
		///	ファイルの読み込みに失敗した場合もYanesdkResult.no_error以外が返る)
		/// </remarks>
		/// <param name="no"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public YanesdkResult PlayFade(int no,int speed){
			return SoundHelper(no, delegate(Sound s) { return s.PlayFade(speed); });
		}

		/// <summary>
		/// 直接指定された番号のサウンドを停止させる
		/// </summary>
		/// <remarks>
		/// 失敗時はYanesdkResult.no_error以外が返る。
		///	(定義ファイル上に存在しない番号を指定した場合や
		///	ファイルの読み込みに失敗した場合もYanesdkResult.no_error以外が返る)
		/// </remarks>
		/// <param name="no"></param>
		/// <returns></returns>
		public YanesdkResult Stop(int no)
		{
			return SoundHelper(no, delegate(Sound s) { return s.Stop(); });
		}

		/// <summary>
		///	FadeOutつきのstop。FadeOutのスピードは[ms]単位。
		/// </summary>
		/// <remarks>
		/// 失敗時はYanesdkResult.no_error以外が返る。
		///	(定義ファイル上に存在しない番号を指定した場合や
		///	ファイルの読み込みに失敗した場合もYanesdkResult.no_error以外が返る)
		/// </remarks>
		public YanesdkResult StopFade(int no, int speed)
		{
			return SoundHelper(no, delegate(Sound s) { return s.StopFade(speed); });
		}
		#endregion

		#region SE再生関連

		/// <summary>
		///	SE再生のguard時間
		/// </summary>
		/// <remarks>
		///	SE再生後、一定のフレーム数が経過するまでは、
		///	そのSEの再生はplaySEを呼び出しても行なわない。
		///	(ディフォルトでは5フレーム。)
		///	UpdateSE()を呼び出したときに1フレーム経過したものとする。
		/// </remarks>
		public int GuardTime
		{
			get { return guardTime; }
			set { guardTime = value; }
		}
		private int guardTime = 5;

		/// <summary>
		/// SE再生のためのもの。毎フレーム1回呼び出すこと！
		/// </summary>
		/// <remarks>
		///	SE再生後、一定のフレーム数が経過するまでは、
		///	そのSEの再生はplaySEを呼び出しても行なわない。
		///	これをguard timeと呼ぶ。これは、 SetGuardTime で設定する。
		///	また、毎フレーム updateSE を呼び出す。
		///	(これを呼び出さないとフレームが経過したとみなされない)
		/// </remarks>
		public void UpdateSE()
		{
			int[] keys = new int[guardTimes.Keys.Count];
			guardTimes.Keys.CopyTo(keys, 0);
			foreach (int key in keys)
			{
				int rest = guardTimes[key];
				if (0 < rest)
					guardTimes[key] = rest - 1;
			}
		}

		/// <summary>
		/// SE再生用のplay関数。
		/// </summary>
		/// <param name="no"></param>
		/// <returns></returns>
		/// <remarks>
		/// <para>
		/// SEの再生は例えばシューティングで発弾時に行なわれたりして、
		/// 1フレーム間に何十回と同じSE再生を呼び出してしまうことがある。
		/// そのときに、毎回SEの再生をサウンドミキサに要求していたのでは
		/// 確実にコマ落ちしてしまう。そこで、一定時間は同じ番号のSE再生要求は
		/// 無視する必要がある。それが、このplaySEである。
		/// </para>
		/// <para>
		/// playSEで再生させたものをstopさせるときは
		/// stop/stopFade関数を用いれば良い。
		/// </para>
		/// <para>
		/// この関数を用いる場合は、 updateSE を毎フレーム呼び出さないと
		/// いけないことに注意。
		/// </para>
		/// </remarks>
		public YanesdkResult PlaySE(int no)
		{
			return SoundHelper(no, delegate(Sound s)
			{
				if (IsInGuardTime(no))
					return YanesdkResult.NoError;

				return s.Play();
			});
		}

		/// <summary>
		/// 	FadeInつきのplaySE。FadeInのスピードは[ms]単位。
		/// 
		/// 　　これもguard time中だと利かないので注意すること。
		/// </summary>
		/// <param name="no"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public YanesdkResult PlaySEFade(int no,int speed)
		{
			return SoundHelper(no, delegate(Sound s)
			{
				if (IsInGuardTime(no))
					return YanesdkResult.NoError;

				return s.PlayFade(speed);
			});
		}

		/// <summary>
		///	guardタイムのリセット
		/// </summary>
		/// <remarks>
		/// 連続してクリックしたときにクリック音を鳴らさないと
		///	いけないことがある。SE連続再生を禁止したくないシーンにおいて、
		///	このメソッドを毎フレームごとor1回呼び出すことにより、
		///	すべてのサウンドのguardtimeをリセットすることができる
		/// </remarks>
		public void ResetGuardTime()
		{
			foreach (int key in guardTimes.Keys)
				guardTimes[key] = 0;
		}

		/// <summary>
		/// ガードタイムのチェック用
		/// </summary>
		/// <param name="no"></param>
		/// <returns></returns>
		private bool IsInGuardTime(int no)
		{
			if (!guardTimes.ContainsKey(no))
				guardTimes.Add(no, 0);

			int guardTime = guardTimes[no];

			// ガード時間中なので再生できナーだった(エラー扱いではない)
			if (0 < guardTime) return true;

			//　ガード時間の更新
			guardTimes[no] = this.guardTime;
			return false;
		}

		/// <summary>
		/// それぞれのSoundのinstanceに対するガードタイム
		/// </summary>
		private Dictionary<int, int> guardTimes = new Dictionary<int, int>();

		#endregion

		#region BGM再生関連

		/// <summary>
		///	BGMの再生用play関数
		/// </summary>
		/// <remarks>
		///	メニュー等で、サウンド再生とSEの再生のon/offを切り替えたとする。
		///	この切り替え自体はenableSound関数を使えば簡単に実現できるが、
		///	そのときに、(SEはともかく)BGMは鳴らないとおかしい、と言われることが
		///	ある。そのときにBGMを鳴らすためには、再生要求のあったBGMに関しては
		///	保持しておき、それを再生してやる必要がある(と考えられる)
		///
		///	よって、BGM再生に特化した関数が必要なのである。
		/// </remarks>
		/// <param name="no"></param>
		/// <returns></returns>
		public YanesdkResult PlayBGM(int no)
		{
			bgmNo = no;
			return Play(no);
		}

		/// <summary>
		/// FadeInつきの playBGM 。FadeInのスピードは[ms]単位。
		/// </summary>
		/// <param name="no"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public YanesdkResult PlayBGMFade(int no,int speed)
		{
			bgmNo = no;
			return PlayFade(no,speed);
		}

		/// <summary>
		/// BGMの再生用のstop関数
		/// </summary>
		/// <returns></returns>
		public YanesdkResult StopBGM()
		{
			if (bgmNo==Int32.MinValue)
			{
				return YanesdkResult.NoError; // 再生中じゃなさげ
			} else {
				int no = bgmNo;
				bgmNo=Int32.MinValue;
				return Stop(no);
			}
		}

		/// <summary>
		/// FadeOutつきの stopBGM 。FadeInのスピードは[ms]単位。
		/// </summary>
		/// <param name="speed"></param>
		/// <returns></returns>
		public YanesdkResult StopBGMFade(int speed)
		{
			if (bgmNo==Int32.MinValue)
			{
				return YanesdkResult.NoError; // 再生中じゃなさげ
			} else {
				int no = bgmNo;
				bgmNo=Int32.MinValue;
				return StopFade(no,speed);
			}
		}

		/// <summary>
		///  BGMのクロスフェード用関数
		/// </summary>
		///	<remarks>
		/// 
		/// 現在再生中のBGMをフェードアウトさせつつ、新しいBGMをフェードインして
		///	再生するための関数。fade in/outは同じスピードで行なわれる。
		///	スピードは[ms]単位。fade inするBGMとfade outするBGMの再生chunkが
		///	異なることが前提。
		/// </remarks>
		/// <param name="no"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public YanesdkResult PlayBGMCrossFade(int no,int speed)
		{
			StopBGMFade(speed);
			return PlayBGMFade(no,speed);
		}

		#endregion

		#endregion

		#region properties

		/// <summary>
		///	サウンドが再生中かを調べる
		/// </summary>
		///	ただし、再生中か調べるために、ファイルを読み込まれていなければ
		///	ファイルを読み込むので注意。
		/// <param name="no"></param>
		/// <returns></returns>
		public bool IsPlaying(int no)
		{
			if (!EnableSound) return false; // そのまま帰ればok
			Sound s = GetSound(no);
			if (s == null)
				return false;
			return s.IsPlaying();
		}

		/// <summary>
		/// サウンドの有効/無効の切り替え。
		/// </summary>
		/// <param name="bEnable"></param>
		/// <remarks>
		/// <para>
		/// 初期状態ではenable(有効)状態。
		/// これを無効にして、そのあと有効に戻したとき、playBGMで再生指定が
		/// されているBGMがあれば、有効に戻した瞬間にその番号のBGMを
		/// 再生する。
		/// </para>
		/// <para>
		/// 無効状態においては、play/playFade/playSE/playBGMの呼び出しすべてに
		/// おいて何も再生されないことが保証される。(ただし、このクラスを通じて
		/// 再生するときのみの話で、Soundクラスを直接getで取得して再生するならば
		/// この限りではない)
		/// </para>
		/// <para>
		/// また、有効状態から無効状態へ切り替えた瞬間、musicとすべてのchunkでの
		/// 再生を停止させる。
		/// </para>
		/// </remarks>
		public bool EnableSound
		{
			set
			{
				if (value == isSoundEnable) return; // 同じならば変更する必要なし
				if (value)
				{
					isSoundEnable = true;
					// 状態を先に変更しておかないと、playBGMが無効になったままだ

					//	無効状態から有効化されたならば、
					//	BGM再生要求がヒストリーにあればそれを再生する
					if (bgmNo != Int32.MinValue)
					{
						PlayBGM(bgmNo);
					}
				}
				else
				{
					//	無効化するならサウンドすべて停止させなくては..
					if (bgmNo != Int32.MinValue)
					{
						//	再生中のものを停止させていることを確認して
						//	再生中のBGMでなければ、BGMは再生されてないと把握し
						//	次にenableSound(true)とされたときも再開させる必要はない
						if (!IsPlaying(bgmNo)) bgmNo = Int32.MinValue;
					}
					Sound.StopAll();
					isSoundEnable = false;
					//	このあとで無効化しなくては、bSoundEnableがfalseの状態だと
					//	stop等が利かない可能性がある
				}
			}
			get { return isSoundEnable; }
		}

		#endregion

		#region private
		/// <summary>
		/// 現在再生しているBGMのナンバー(Int32.MinValueならば再生してない)
		/// </summary>
		protected int bgmNo = Int32.MinValue;

		/// <summary>
		/// サウンド再生は有効か？(default:true)
		/// </summary>
		protected bool isSoundEnable = true;
		#endregion

		#region overridden from base class(CachedObjectLoader)
		/// <summary>
		/// 
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		protected override ICachedObject OnDefFileLoaded(FilenamelistReader.Info info)
		{
		//	if (Factory == null)
		//		return null; //  YanesdkResult.PreconditionError; // factoryが設定されていない

			ICachedObject obj = new Sound(); // Factory();

			// info.name, info.opt1 を渡したい

			// default値は0扱い。
			if (info.opt1 == int.MinValue)
				info.opt1 = 0;

			obj.Construct(new SoundConstructAdaptor(info.name,info.opt1));
			return obj;
		}

		/// <summary>
		/// オプションとしてchunk noを指定するのでこの値は1になる。
		/// </summary>
		protected override int OptNum
		{
			get { return 1; }
		}
		#endregion
	}

	/// <summary>
	/// SoundLoaderのSmartLoader版
	/// </summary>
	/// <remarks>
	/// SmartSoundLoaderを用いるときは、
	/// SoundLoaderが勝手にSoundを解放するとSmartSoundLoaderが困るので
	/// SoundLoader.Disposeは呼び出さないこと。
	/// </remarks>
	public class SmartSoundLoader : CachedObjectSmartLoader<SoundLoader>{}

	/// <summary>
	/// Sound系のclassがICachedObject.Constructを実装するときに使うadaptor
	/// </summary>
	internal class SoundConstructAdaptor
	{
		public SoundConstructAdaptor(string filename, int chunkNo)
		{
			this.FileName = filename;
			this.ChunkNo = chunkNo;
		}
		public string FileName;
		public int ChunkNo;
	}

}
