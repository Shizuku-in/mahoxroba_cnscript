using System;
using System.Diagnostics;
using Sdl;
using Yanesdk.Ytl;
using Yanesdk.System;

namespace Yanesdk.Sound {

	/// <summary>
	/// Sound クラスのための再生品質設定クラス。
	/// </summary>
	/// <remarks>
	/// <code>
	/// int audio_rate; // = 44100;
	/// ushort audio_format; // = AUDIO_S16;
	/// int audio_channels; //	= 2;
	/// int audio_buffers; // = 4096;
	/// </code>
	/// 初期状態では上記のようになっている。
	/// 
	/// あえて変更したければsingletonオブジェクト通じて変更すること。
	/// </remarks>
	public class SoundConfig
	{

		#region ctor
		/// <summary>
		/// 
		/// </summary>
		public SoundConfig()
		{
			audioRate = 44100;
			audioFormat = (int)SDL.AUDIO_S16;
			audioChannels = 2;
			audioBuffers = 4096;

			Update();
		}
		#endregion

		#region properties
		/// <summary>
		/// 再生周波数(default:44100)。
		/// 値を変更したなら、Updateメソッドを呼び出すべし。
		/// </summary>
		public int AudioRate
		{
			get { return audioRate; }
			set { audioRate = value; dirty = true; }
		}
		private int audioRate;

		/// <summary>
		/// 再生ビット数(default:AUDIO_S16)。
		/// </summary>
		/// <remarks>
		/// このAUDIO_S16というのは、SDL_audio で定義されている。
		/// 値を変更したなら、Updateメソッドを呼び出すべし。
		/// </remarks>
		public int AudioFormat
		{
			get { return audioFormat; }
			set { audioFormat = value; dirty = true; }
		}
		private int audioFormat;

		/// <summary>
		/// 再生チャンネル数(default:2)。
		/// 値を変更したなら、Updateメソッドを呼び出すべし。
		/// </summary>
		public int AudioChannels
		{
			get { return audioChannels; }
			set { audioChannels = value; dirty = true; }
		}
		private int audioChannels;

		/// <summary>
		/// 再生バッファ長[bytes](default:4096)。
		/// 値を変更したなら、Updateメソッドを呼び出すべし。
		/// </summary>
		public int AudioBuffers
		{
			get { return audioBuffers; }
			set { audioBuffers = value; dirty = true; }
		}
		private int audioBuffers;

		/// <summary>
		/// 汚れフラグ。このフラグが立っていれば
		/// オーディオデバイスを再初期化する必要がある。
		/// </summary>
		public bool Dirty
		{
			get { return dirty; }
			set { dirty = value; }
		}
		private bool dirty = true;
		#endregion

		#region methods
		/// <summary>
		/// Audioデバイスを初期化する(明示的に呼び出す必要はないが、
		/// 他にAudioデバイスを必要とするモジュールがあるならばそこから呼び出すべし)
		/// 
		/// また、このクラスのフィールドを変更したときも呼び出すべし。
		/// </summary>
		/// <returns></returns>
		public YanesdkResult Update()
		{
			if (dirty)
			{
				// 処理をするのでフラグをおろす
				dirty = false;

				// 前回Openしていればいったん閉じる。
				if (isOpened)
				{
					isOpened = false;
					SDL.Mix_CloseAudio();
				}

				if (SDL.Mix_OpenAudio(audioRate, (ushort)audioFormat,
					audioChannels, audioBuffers) < 0)
				{
					// sound deviceが無いのかbufferがあふれたのかは不明なので
					//	サウンドを使えなくすることはない
					return YanesdkResult.PreconditionError;

				}

				// Openに成功したのでOpenフラグを立てておく。
				isOpened = true;

				//	どうせ高目で設定しても、その通りの能力を
				//	デバイスに要求するわけではないので．．
				//	  Mix_QuerySpec(&audio_rate, &audio_format, &audio_channels);
				//	↑最終的な結果は、これで取得できる
			}
			return YanesdkResult.NoError;
		}

		/// <summary>
		/// ミキサーの後始末
		/// </summary>
		public void Close()
		{
			if (isOpened)
			{
				isOpened = false;
				SDL.Mix_CloseAudio();
				// 次のUpdate()で再度初期化する
				dirty = true;
			}
		}

		#endregion

		#region private
		/// <summary>
		/// サウンドデバイスをopenしたのか。
		/// </summary>
		private bool isOpened = false;
		#endregion
	}

	/// <summary>
	/// サウンド再生用クラス。
	/// </summary>
	/// <remarks>
	/// サウンド再生の考えかた
	///	１．music(bgm) + chuck(se)×8　の9個を同時にミキシングして出力出来る
	///	２．次のmusicが再生されると前のmusicは自動的に停止する
	///	３．seを再生するときには1～8のchuck(チャンク)ナンバーを指定できる
	///	同じchuckナンバーのseを再生すると、前回再生していたものは
	///	自動的に停止する
	///	４．musicもchunkも、どちらもwav,riff,ogg...etcを再生する能力を持つ
	///	５．midi再生に関しては、musicチャンネルのみ。
	///	つまり、musicとchunkに関しては、５．６．の違いを除けば同等の機能を持つ。
	///	６．チャンクは、0を指定しておけば、1～8の再生していないチャンクを
	///	自動的に探す。
	///	７．bgmとして、musicチャンネルを用いない場合(midiか、途中から再生
	///	させるわけでもない限りは用いる必要はないと思われる)、
	///	bgmのクロスフェードなども出来る。
	/// 
	/// また、UnmanagedResourceManager.Instance.SoundCache.LimitSizeで示される値まで
	/// 自動的にcacheする仕組みもそなわっている。そのため明示的にDisposeを呼び出す必要はない。
	/// </remarks>
	/// <example>
	/// 使用例)
	/// <code>
	/// Sound name = new Sound();
	/// name.load("1.ogg",-1);
	/// name.setLoop(-1); // endless
	/// name.play();
	/// 
	/// Sound s2 = new Sound();
	/// s2.load("extend.wav",1);
	/// s2.play();
	/// </code>
	/// </example>
	public class Sound : CachedObject , ILoader , IDisposable
	{
		#region ctor & Dispose
		/// <summary>
		/// static class constructorはlazy instantiationが保証されているので
		/// ここで SDL_mixerが間接的に読み込むdllを事前に読み込む。
		/// </summary>
		
		/* SDL_Initializerで行なうように変更
		static Sound()
		{
			// Sound関連のDLLを必要ならば読み込んでおくべ。

			DllManager d = DllManager.Instance;
			string current = DllManager.DLL_CURRENT;
			d.LoadLibrary(current, DLL_OGG);
			d.LoadLibrary(current, DLL_VORBIS);
			d.LoadLibrary(current, DLL_VORBISFILE);
			d.LoadLibrary(current, DLL_SMPEG);
		}
		*/

		/// <summary>
		/// コンストラクタでは、Audioデバイスの初期化も行なう。
		/// </summary>
		public Sound()
		{
			This = this; // mix-in用

			// Audioデバイスの初期化。
			// SoundConfig.Update();
			// ↑これは、initがコンストラクタで行なう

			CacheSystem = UnmanagedResourceManager.Instance.SoundMemory;
			CacheSystem.Add(this);
		}

		/// <summary>
		/// このメソッドを呼び出したあとは再度Loadできない。
		/// Loadしたデータを解放したいだけならばReleaseを呼び出すこと。
		/// </summary>
		public void Dispose()
		{
			Release();

			init.Dispose();
			CacheSystem.Remove(this);
		}
		#endregion

		#region ILoaderの実装

		/// <summary>
		/// Load(filename,0)と等価。
		/// ILoader interfaceが要求するので辻褄合わせに用意してある。
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public YanesdkResult Load(string filename)
		{
			return Load(filename,0);
		}

		/// <summary>
		/// サウンドファイルを読み込む
		/// </summary>
		/// <param name="filename">ファイル名</param>
		/// <param name="ch">読み込むチャンネル
		/// -1	 : musicチャンネル
		/// 0	 : 1～8のchunkのうち再生時(play)に再生していないチャンネルをおまかせで
		/// 1～8 : chunkに読み込む
		/// </param>
		/// <returns>読み込みエラーならばYanesdkResult.no_error以外</returns>
		public YanesdkResult Load(string filename,int ch)
		{
			Release();

			YanesdkResult result;
			if (ch==-1) result = LoadMusic(filename);
			else if (ch==0) result = LoadChunk(filename);
			else if (1<=ch && ch<=8) result = LoadChunk(filename,ch);
			else { result = YanesdkResult.InvalidParameter; }	//	errorですよ、と。

			if (result == YanesdkResult.NoError)
			{
				loaded = true;
				this.fileName = filename;

				// もし、ローダー以外からファイルを単に読み込んだだけならば、Reconstructableにしておく。
				if (constructInfo == null)
				{
					constructInfo = new SoundConstructAdaptor(filename, ch);
				}

				// リソースサイズが変更になったことをcache systemに通知する
				// Releaseのときに0なのは通知しているので通知するのは正常終了時のみでok.
				CacheSystem.OnResourceChanged(this);
			}

			return result;
		}
		
		/// <summary>
		/// ファイルの読み込みが完了しているかを返す
		/// </summary>
		/// <returns></returns>
		public bool Loaded { get { return loaded; }  }
		private bool loaded = false;

		/// <summary>
		/// loadで読み込んだサウンドを解放する
		/// </summary>
		public void Release() {
			if (music != IntPtr.Zero) {
				Stop();
				SDL.Mix_FreeMusic(music);
				music = IntPtr.Zero;
			}
			if (chunk != IntPtr.Zero) {
				Stop();
				SDL.Mix_FreeChunk(chunk);
				chunk = IntPtr.Zero;
			}
			tmpFile = null;
			loaded = false;
			fileName = null;

			// リソースサイズが変更になったことをcache systemに通知する
			CacheSystem.OnResourceChanged(this);

			constructInfo = null;
		}
		
		/// <summary>
		/// ファイルを読み込んでいる場合、読み込んでいるファイル名を返す
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}
		private string fileName;
		
		#endregion

		#region methods
		/// <summary>
		/// loadで読み込んだサウンドを再生する
		/// </summary>
		/// <returns>
		/// 再生エラーならばYanesdkResult.no_error以外が返る
		/// </returns>
		public YanesdkResult Play()
		{
			// cache systemにこのSoundを使用したことを通知する
			CacheSystem.OnAccess(this);
			isPlayingLast = true;

			if (NoSound) return YanesdkResult.NoError;
			Stop(); // 停止させて、sound managerの再生チャンネルをクリアしなければ
			if (music != IntPtr.Zero) return PlayMusic();
			if (chunk != IntPtr.Zero) return PlayChunk();
			return YanesdkResult.PreconditionError; // Sound読み込んでないぽ
		}

		/// <summary>
		/// フェードイン付きのplay。speedはfade inに必要な時間[ms]
		/// </summary>
		/// <param name="speed"></param>
		/// <returns></returns>
		public YanesdkResult PlayFade(int speed)
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = true;

			if (NoSound) return YanesdkResult.NoError;
			Stop(); // 停止させて、sound managerの再生チャンネルをクリアしなければ
			if (music != IntPtr.Zero) return PlayMusicFade(speed);
			if (chunk != IntPtr.Zero) return PlayChunkFade(speed);
			return YanesdkResult.PreconditionError; // Sound読み込んでないぽ
		}

		/// <summary>
		///	play中のサウンドを停止させる
		/// </summary>
		///	読み込んでいるサウンドデータ自体を解放するには release を
		///	呼び出すこと。こちらは、あくまで停止させるだけ。次にplayが
		///	呼び出されれば、再度、先頭から再生させることが出来る。
		/// <returns></returns>
		public YanesdkResult Stop()
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = false;

			//	stopは、channelごとに管理されているので、
			//	自分が再生しているchannelなのかどうかを
			//	このクラスが把握している必要がある
			if (NoSound) return YanesdkResult.NoError;
			return ChunkManager.Stop(this);
		}

		/// <summary>
		/// 一時停止を行なう。
		/// 再生中にこのメソッドを呼び出した場合、IsPlayingはtrueを返す。
		/// その後、バッファを破棄するならばStopを必ず呼び出すこと。
		/// そうしないと、いつまでもこのchunkはIsPlayingがtrueを返すので
		/// Playでchunkをおまかせにしている場合、空きchunkがないと判断されることになる。
		/// </summary>
		/// <returns></returns>
		public YanesdkResult Pause()
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = false;

			if (NoSound) return YanesdkResult.NoError;
			return ChunkManager.Pause(this);
		}

		/// <summary>
		/// Pauseで停止させていたならば、
		/// それを前回停止させていた再生ポジションから再開する。
		/// </summary>
		/// <returns></returns>
		public YanesdkResult Resume()
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = true;

			if (NoSound) return YanesdkResult.NoError;
			return ChunkManager.Resume(this);
		}

		/// <summary>
		///	musicチャンネルを(徐々に)フェードアウトさせる
		/// </summary>
		/// <remarks>
		/// speedはfadeoutスピード[ms]
		/// </remarks>
		/// <param name="speed"></param>
		/// <returns></returns>
		public static YanesdkResult FadeMusic(int speed) {
			if (NoSound) return YanesdkResult.PreconditionError;
			ChunkManager.music = null;
			return SDL.Mix_FadeOutMusic(speed) == 0 ?
				YanesdkResult.NoError : YanesdkResult.SdlError;
		}

		/// <summary>
		/// 0～7のチャンネルを(徐々に)フェードアウトさせる
		/// </summary>
		/// <remarks>
		/// speedはfadeoutスピード[ms]
		/// </remarks>
		/// <param name="speed"></param>
		/// <returns></returns>
		public static YanesdkResult FadeChunk(int speed)
		{
			if (NoSound) return YanesdkResult.PreconditionError;
			for(int i=0;i<8;++i)
			{
				ChunkManager.chunk[i] = null;
				SDL.Mix_FadeOutChannel(i,speed);
			}
			return YanesdkResult.NoError;
		}

		/// <summary>
		///	すべてのchunk(除くmusicチャンネル)の再生を停止させる
		/// </summary>
		/// <remarks>
		///	このメソッド呼び出し中に他のスレッドから
		///	サウンド関係をいじることは考慮に入れていない
		/// </remarks>
		public static void StopAllChunk()
		{
			ChunkManager.StopAllChunk();
		}

		/// <summary>
		///	musicチャンネルの再生を停止させる
		/// </summary>
		/// <remarks>
		///	このメソッド呼び出し中に他のスレッドから
		///	サウンド関係をいじることは考慮に入れていない
		/// </remarks>
		/// <returns></returns>
		public static YanesdkResult StopMusic()
		{
			return ChunkManager.StopMusic();
		}

		/// <summary>
		///	musicチャンネルと、すべてのchunkの再生を停止させる
		/// </summary>
		/// <remarks>
		/// このメソッド呼び出し中に他のスレッドから
		///	サウンド関係をいじることは考慮に入れていない
		/// </remarks>
		/// <returns></returns>
		public static YanesdkResult StopAll()
		{
			StopAllChunk();
			return StopMusic();
		}

		/// <summary>
		/// ファイル(wav,ogg,mid)の読み込み。
		/// ここで読み込んだものは、bgmとして再生される
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		private YanesdkResult LoadMusic(string filename)
		{
			if (NoSound) return YanesdkResult.PreconditionError;

			tmpFile = FileSys.GetTmpFile(filename);
			string f = tmpFile.FileName;

			YanesdkResult result;
			if (f != null) {
				music = SDL.Mix_LoadMUS(f);

				// Debug.Fail(SDL.Mix_GetError());
				// ここでmusic == 0が返ってくるのは明らかにおかしい。
				// smpeg.dllがないのに mp3を再生しようとしただとか？

				if (music == IntPtr.Zero)
					result = YanesdkResult.HappenSomeError;
				else
					result = YanesdkResult.NoError;
			} else {
				result = YanesdkResult.FileNotFound; // file not found
			}

			//		music = Mix_LoadMUS_RW(rwops,1);
			//	この関数なんでないかなーヽ(´Д`)ノ

			//	↑については↓と教えていただきました。
			
			//	この関数は SDL_mixer をコンパイルするときに
			//	"USE_RWOPS" プリプロセッサ定義を追加すると使えるようです。
			//	http://ilaliart.sourceforge.jp/tips/mix_rwops.html
			
			// 最新のSDL_mixerならばあるらしい。
			
			//  ここで、注意して欲しいのは、Mix_LoadMUS_RW関数でMix_Musicオブジェクトを生成しても
			//  すぐにSDL_RWclose関数でSDL_RWopsオブジェクトを開放していないことである。
			//  Mix_Musicオブジェクトはストリーミング再生されるため、常にファイルを開いた状態でなければならない。
			//  そのため、SDL_RWcloseでロード後にすぐにファイルを閉じてしまった場合、Mix_PlayMusic関数で再生に失敗してしまう。
			//  そのため、再生に用いるSDL_RWopsオブジェクトは再生停止後に破棄する必要がある。
		/*
			SDL_RWopsH rwops = FileSys.ReadRW(filename);
			if (rwops.Handle != IntPtr.Zero)
			{
				music = SDL.Mix_LoadMUS_RW(rwops.Handle);
				// ↑このときrwopsをどこかで解放する必要あり

			}
			else
			{
				return YanesdkResult.FileNotFound;	// file not found
			}

			if (music == IntPtr.Zero) {
				return YanesdkResult.SdlError;
			}
		 */
			// MacOSやmp3再生だと対応してないっぽいので、やっぱりこれ使うのやめ

			if (result == YanesdkResult.NoError)
			{
				this.fileName = filename;
				CacheSystem.OnResourceChanged(this);
			}

			return result;
		}

		/// <summary>
		/// ファイル(ogg,wav)の読み込み。
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <remarks>
		/// 空きチャンネルを自動的に使用する。
		/// 使用するチャンクナンバーはお任せバージョン。
		/// </remarks>
		private YanesdkResult LoadChunk(string name)
		{
			return LoadChunk(name,0);
		}

		/// <summary>
		/// ファイル(ogg,wav)の読み込み。
		/// </summary>
		/// <param name="name"></param>
		/// <param name="ch">
		/// 読み込むチャンクを指定。ch=0なら自動で空きを探す 1～8</param>
		/// <returns></returns>
		private YanesdkResult LoadChunk(string name, int ch)
		{
			if (NoSound) return YanesdkResult.PreconditionError;
			SDL_RWopsH rwops = FileSys.ReadRW(name); 
			if (rwops.Handle != IntPtr.Zero) {
				chunk = SDL.Mix_LoadWAV_RW(rwops.Handle, 1);
			} else {
				return YanesdkResult.FileNotFound;	// file not found
			}
			if (chunk == IntPtr.Zero) {
				return YanesdkResult.FileReadError;	// 読み込みに失敗してやんの
			}
			chunkChannel = ch;

			CacheSystem.OnResourceChanged(this);

			return YanesdkResult.NoError;
		}

		/// <summary>
		/// loadMusicで読み込んだBGMを再生させる
		/// </summary>
		/// <returns></returns>
		private YanesdkResult PlayMusic()
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = true;

			if (NoSound) return YanesdkResult.PreconditionError;
			ChunkManager.music = this; // チャンネルの占拠を明示

			// volumeの設定(これは再生時に設定する)
			ChunkManager.SetVolume(0, Volume);
			
			return SDL.Mix_PlayMusic(music, loopflag) == 0 ?
				YanesdkResult.NoError : YanesdkResult.SdlError;
		}

		/// <summary>
		/// loadMusicで読み込んだBGMをfadeさせる
		/// </summary>
		private YanesdkResult PlayMusicFade(int speed)
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = true;

			if (NoSound) return YanesdkResult.PreconditionError;
			ChunkManager.music = this; // チャンネルの占拠を明示

			// volumeの設定(これは再生時に設定する)
			ChunkManager.SetVolume(0, Volume);
			
			return SDL.Mix_FadeInMusic(music, loopflag , speed) == 0 ?
				YanesdkResult.NoError : YanesdkResult.SdlError;
		}

		/// <summary>
		/// loadChunkで読み込んだサウンドを再生させる
		/// </summary>
		private YanesdkResult PlayChunk()
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = true;

			if (NoSound) return YanesdkResult.PreconditionError;

			int ch = 		
				(chunkChannel == 0) ?
					// おまかせchunkでの再生ならば空きチャンクを探す
					ch = ChunkManager.GetEmptyChunk(this)
				:
					ch = chunkChannel-1;

			// チャンネルの占拠を明示
			ChunkManager.chunk[ch] = this;
			//	↑このチャンクが使用中であることはここで予約が入る

			// volumeの設定(これは再生時に設定する)
			ChunkManager.SetVolume(ch + 1, Volume);
			
			return SDL.Mix_PlayChannel(ch, chunk, loopflag) == ch ?
				YanesdkResult.NoError : YanesdkResult.SdlError;
		}

		/// <summary>
		/// loadChunkで読み込んだサウンドをfadeさせる
		/// </summary>
		/// <param name="speed"></param>
		/// <returns></returns>
		private YanesdkResult PlayChunkFade(int speed)
		{
			CacheSystem.OnAccess(this);
			isPlayingLast = true;

			if (NoSound) return YanesdkResult.PreconditionError;
			int ch =
				(chunkChannel == 0) ?
				// おまかせchunkでの再生ならば空きチャンクを探す
					ch = ChunkManager.GetEmptyChunk(this)
				:
					ch = chunkChannel - 1;

			// チャンネルの占拠を明示
			ChunkManager.chunk[ch] = this;
			//	↑このチャンクが使用中であることはここで予約が入る

			// volumeの設定(これは再生時に設定する)
			ChunkManager.SetVolume(ch+1, Volume);
			
			return SDL.Mix_FadeInChannel(ch, chunk, loopflag,speed) == ch ?
				YanesdkResult.NoError : YanesdkResult.SdlError;
		}

		#endregion

		#region properties
		/// <summary>
		/// volumeの設定。0.0～1.0までの間で。
		/// 1.0なら100%の意味。
		/// 
		/// master volumeのほうも変更できる。
		///		出力volume = (master volumeの値)×(ここで設定されたvolumeの値)
		/// である。
		/// 
		/// ここで設定された値はLoad/Play等によっては変化しない。
		/// 再設定されるまで有効である。
		/// </summary>
		/// <param name="volume"></param>
		/// <returns></returns>
		public float Volume
		{
			get { return volume; }
			set
			{
				volume = value;

				// 再生中ならば、そのchunkのvolumeの再設定が必要だ。
				int ch = GetPlayingChunk();
				if (ch != -1)
					ChunkManager.SetVolume(ch, volume);
			}
		}
		private float volume = 1.0f;

		/// <summary>
		/// マスターvolumeの設定
		/// すべてのSoundクラスに影響する。
		///		出力volume = (master volumeの値)×(volumeの値)
		/// である。
		/// </summary>
		/// <param name="volume"></param>
		public static float MasterVolume
		{
			get { return ChunkManager.MasterVolume; }
			set { ChunkManager.MasterVolume = value; }
		}

		/// <summary>
		/// stopのフェード版
		/// </summary>
		/// <remarks>
		/// fadeoutのスピードを指定できる。speed の単位は[ms]。
		///	その他はstopと同じ。
		///	</remarks>
		/// <param name="speed"></param>
		/// <returns></returns>
		public YanesdkResult StopFade(int speed){
			if (NoSound)
				return YanesdkResult.NoError;

			return ChunkManager.StopFade(this, speed);
		}

		/// <summary>
		/// 再生中かを調べる
		/// </summary>
		/// <returns></returns>
		public bool IsPlaying()
		{
			// このメソッドはReconstructableから再三呼び出される可能性があるので、
			// 少しでも高速化しておきたい。
			// そのため、結果をcacheし、前回再生されていなくて、
			// そのあとPlayを呼び出していなければ即座にfalseを返す実装にする。

			if (!isPlayingLast)
				return false;

			bool isPlaying = GetPlayingChunk() != -1;
			isPlayingLast = isPlaying;

			return isPlaying;
		}

		/// <summary>
		/// 前回IsPlayingが呼び出されたときのcache結果。
		/// 
		/// falseならば無条件でIsPlayingはfalse
		/// trueならば再生されている可能性があるのでそれを調べる。
		/// </summary>
		private bool isPlayingLast = false;

		/// <summary>
		/// 自分が再生中のchunkを返す。
		/// -1   : なし
		///  0   : music chunk
		///  1-8 : sound chunk
		/// </summary>
		/// <returns></returns>
		public int GetPlayingChunk()
		{
			if (NoSound) return -1;
			if (ChunkManager.music == this && SDL.Mix_PlayingMusic() != 0) return 0;
			for (int i = 0; i < 8; ++i)
			{
				if (ChunkManager.chunk[i] == this && SDL.Mix_Playing(i) != 0) return i+1;
			}
			return -1; // not found
		}

		/// <summary>
		/// ループ回数の設定/取得
		/// </summary>
		/// <param name="loop">
		/// -1:endless
		/// 0 :1回のみ(default)
		/// 1 :2回
		/// 以下、再生したい回数-1を指定する。
		/// </param>
		/// <remarks>
		/// ここで設定した値は一度設定すれば再度この関数で
		/// 変更しない限り変わらない
		/// </remarks>
		public int Loop
		{
			set { loopflag = value; }
			get { return loopflag; }
		}

		/// <summary>
		/// 音なしモードなのか
		/// </summary>
		public static bool NoSound
		{
			get {
			//	return SoundConfig.noSound;
				using (AudioInit init = new AudioInit())
				{
					return init.NoSound;
				}
			}
		}

		/// <summary>
		/// SoundConfigへのsingletonなインスタンス
		/// </summary>
		/// <returns></returns>
		public static SoundConfig SoundConfig
		{
			get { return Singleton<SoundConfig>.Instance; }
		}

		/// <summary>
		/// SoundChunkManagerへのsingleton。
		/// 外部からChunkManagerをどうしてもいじりたいときにだけ使うべし。
		/// </summary>
		public static SoundChunkManager ChunkManager
		{
			get { return Singleton<SoundChunkManager>.Instance; }
		}

		#endregion
	
		#region overridden from base class
		/// <summary>
		///	loadで読み込んでいるサウンドのために確保されているバッファ長を返す
		/// </summary>
		/// <remarks>
		/// 全体で使用している容量を計算したりするのに使える．．かな？
		/// </remarks>
		/// <returns></returns>
		public override long ResourceSize
		{
			get { return getBufferSize_(); }
		}

		unsafe private long getBufferSize_() {
			if (chunk == IntPtr.Zero) return 0;
			SDL.Mix_Chunk* chunk_ = (SDL.Mix_Chunk*)chunk;
			if (chunk_->allocated != 0)
				return chunk_->alen;
			return 0;
		}

		protected override YanesdkResult OnReconstruct(object param)
		{
			SoundConstructAdaptor info = param as SoundConstructAdaptor;
			return Load(info.FileName,info.ChunkNo);
		}

		/// <summary>
		/// 再生中に解放されると困るので何とか穏便に頼む。
		/// </summary>
		public override bool Reconstructable { get { return constructInfo != null && !IsPlaying(); } }
			//	IsPlayingの判定に時間がかかると嫌だなぁ…
			//	まあ、そんなに大量のSEが存在することはありえないか。

		#endregion

		#region private
		private IntPtr music; // SDLのなんぞ
		private IntPtr chunk; // SDLのなんぞ
		private int chunkChannel;	// 再生するときのchunkの使用番号 
		private int loopflag;		// 再生をループさせるのか
		private FileSys.TmpFile tmpFile;	//	ファイルからしか再生できないものはテンポラリファイルを利用する
		#endregion

		#region internal classes

		/// <summary>
		/// SDL Audioの初期化用クラス
		/// </summary>
		internal class AudioInit : IDisposable
		{
			public AudioInit()
			{
				NoSound = init.Instance.Result < 0;
			}
			
			public void Dispose()
			{
				init.Dispose();
			}

			private RefSingleton<Yanesdk.Sdl.SDL_AUDIO_Initializer>
				init = new RefSingleton<Yanesdk.Sdl.SDL_AUDIO_Initializer>();

			/// <summary>
			/// サウンドデバイスはついていなければtrue。
			/// </summary>
			internal bool NoSound;
		}

		/// <summary>
		/// SDL Audioの初期化用
		/// </summary>
		private RefSingleton<AudioInit>
			init = new RefSingleton<AudioInit>();

		/// <summary>
		/// あるチャンクを再生しているインスタンスを把握しておく必要がある
		/// </summary>
		/// <remarks>
		/// Sound.Manager経由でアクセスしてちょうだい。
		/// </remarks>
		public class SoundChunkManager : IDisposable
		{
			public Sound music;		//	musicチャンネルを再生中のやつを入れておく
			public Sound[] chunk;	//	chunkチャンネルを再生中のやつを入れておく

			public SoundChunkManager()
			{
				chunk = new Sound[8];

				volumes = new float[9];
				for (int i = 0; i < 9; ++i)
					volumes[i] = 1.0f;
			}

			public void Dispose()
			{
			}

			/// <summary>
			/// 現在再生していないチャンクを探す
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			public int GetEmptyChunk(Sound s)
			{
				if (NoSound || chunk == null) return 0;
				for(int i=0;i<8;++i) {
					if (SDL.Mix_Playing(i) == 0)
					{
					//	chunk[i] = s;
						// ↑ここでやる必要ぜんぜんねーな
						return i;
					}
				}
				return 0; // 空きが無いので0番を潰すかぁ..(´Д`)
			}

			/// <summary>
			/// 現在再生しているチャンクを停止させる
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			public YanesdkResult Stop(Sound s) {
				if (s == music) {
				//	SDL.Mix_PauseMusic(); // Mix_HaltMusic();
					SDL.Mix_HaltMusic();
					music = null;
				}
				if (chunk == null)
					return YanesdkResult.PreconditionError;
				for(int i=0;i<8;++i) {
					if(s == chunk[i]) {
						//	SDL.Mix_Pause(i); 
						// ↑これだと IsPlayingでtrueが戻ってきてしまう。
						SDL.Mix_HaltChannel(i);
						// ↑なのでバッファを破棄する必要がある。
						chunk[i] = null;
					}
				}
				return YanesdkResult.NoError;
			}

			/// <summary>
			/// 現在再生しているチャンクを停止させる
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			public YanesdkResult Pause(Sound s)
			{
				if (s == music)
				{
					SDL.Mix_PauseMusic(); // Mix_HaltMusic();
					// バッファは破棄しないなりよ
				}
				if (chunk == null)
					return YanesdkResult.PreconditionError;
				for (int i = 0; i < 8; ++i)
				{
					if (s == chunk[i])
					{
						SDL.Mix_Pause(i); 
						// バッファは破棄しないなりよ
					}
				}
				return YanesdkResult.NoError;
			}

			/// <summary>
			/// 現在一時停止させているチャンクを再生させる
			/// 
			/// 一時停止させている間にVolumeが変更されている可能性があるので
			/// そのへんを考慮して再開。
			/// </summary>
			/// <param name="name"></param>
			/// <returns></returns>
			public YanesdkResult Resume(Sound s)
			{
				if (s == music)
				{
					SetVolume(0, s.Volume);
					SDL.Mix_ResumeMusic();
				}
				if (chunk == null)
					return YanesdkResult.PreconditionError;
				for (int i = 0; i < 8; ++i)
				{
					if (s == chunk[i])
					{
						SetVolume(i+1, s.Volume);
						SDL.Mix_Resume(i);
						break;
					}
				}
				return YanesdkResult.NoError;
			}

			/// <summary>
			/// 現在再生しているチャンクを停止させる
			/// </summary>
			/// <param name="name"></param>
			/// <param name="speed"></param>
			/// <returns></returns>
			public YanesdkResult StopFade(Sound s, int speed) {
				int hr = 0;
				if (s == music) {
					hr = SDL.Mix_FadeOutMusic(speed);
					music = null;
				}
				if (chunk == null)
					return YanesdkResult.PreconditionError;
				for(int i=0;i<8;++i){
					if (s == chunk[i]) {
						hr = SDL.Mix_FadeOutChannel(i,speed);
						chunk[i] = null;
					}
				}
				return hr == 0 ? YanesdkResult.NoError : YanesdkResult.SdlError;
			}

			/// <summary>
			/// すべてのchunkの停止
			/// </summary>
			public void StopAllChunk()
			{
				if (chunk == null) return;
				for(int i=0;i<8;++i) {
					if (chunk[i] != null) chunk[i].Stop();
				}
			}
			
			/// <summary>
			/// musicの停止
			/// </summary>
			/// <returns></returns>
			public YanesdkResult StopMusic()
			{
				if (music != null) return music.Stop();
				return YanesdkResult.NoError;
			}

			/// <summary>
			/// マスターvolumeの設定
			/// すべてのSoundクラスに影響する。
			///		出力volume = (master volumeの値)×(volumeの値)
			/// である。
			/// </summary>
			/// <param name="volume"></param>
			public float MasterVolume
			{
				get { return masterVolume; }
				set
				{
					float currentMasterVolume = masterVolume;
					if (masterVolume != value)
					{
						masterVolume = value;

						// 再生中のchunkに影響があるかも知れないので再設定すべき。

						for(int i=0;i<9;++i)
							// 現在の値と異なるので設定しなおす
							innerSetVolume(i, volumes[i]);

						if (onMasterVolumeChanged != null)
							onMasterVolumeChanged();
					}
				}
			}
			private float masterVolume = 1.0f;

			/// <summary>
			/// マスターVolumeが変更されたときにCallbackされるdelegate
			/// </summary>
			public delegate void OnMasterVolumeChangedDelegate();

			/// <summary>
			/// マスターVolumeが変更されたときにCallbackされるdelegateを登録しておく。
			/// </summary>
			public OnMasterVolumeChangedDelegate OnMasterVolumeChanged
			{
				get { return onMasterVolumeChanged; }
				set { onMasterVolumeChanged = value; }
			}
			private OnMasterVolumeChangedDelegate onMasterVolumeChanged;

			/// <summary>
			/// 特定のchunkのvolumeを設定する
			/// 0.0f～1.0fの間の値で。1.0fは100%。0.0fは無音。
			/// 
			/// ch == 0(music chuck),1～8(通常のchunk)
			/// 現在設定されている値と同じならば再設定はしない。
			/// </summary>
			/// <param name="volume"></param>
			public void SetVolume(int ch, float volume)
			{
				if (ch < 0 || 8 < ch)
					return;

				float currentVolume = volumes[ch];

				// 現在の音量値と異なるときのみ音量設定
				if (currentVolume != volume)
				{
					// 現在の値と異なるので設定する
					innerSetVolume(ch, volume);
				}
			}

			/// <summary>
			/// volume設定(内部的に用いる)
			/// </summary>
			/// <param name="ch"></param>
			/// <param name="volume"></param>
			private void innerSetVolume(int ch, float volume)
			{
				// どの値にしたいのか
				float vol = volume * masterVolume;

				if (ch == 0)
				{
					SDL.Mix_VolumeMusic((int)(vol * SDL.MIX_MAX_VOLUME));
				}
				else
				{
					SDL.Mix_Volume(ch - 1, (int)(vol * SDL.MIX_MAX_VOLUME));
				}

				// 設定した値を記憶しておく。(master volume掛け算前)
				volumes[ch] = volume;

				Console.WriteLine("ch {0} = {1} Volume", ch, (int)(vol * SDL.MIX_MAX_VOLUME));
			}

			/// <summary>
			/// 各chunkのvolume値。
			/// indexは、
			///  0   : music chunk
			///  1-8 : sound chunk
			/// である。
			/// 
			/// 保存されている値は master volumeを掛け算する前の値。
			/// </summary>
			float[] volumes;

		}

		#endregion
	}

}

