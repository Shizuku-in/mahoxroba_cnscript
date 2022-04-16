using System;
using Yanesdk.Ytl;
using Sdl;

namespace Yanesdk.Sound
{
	/// <summary>
	/// CD-ROMの再生(CDDA)。
	/// </summary>
	/// <remarks>
	///	複数ドライブの同時再生にも対応。
	///
	/// スレッドセーフではないのであちこちのスレッドから
	/// 同じドライブに対して再生したり停止したりしないように。
	/// 
	/// 使用例)
	/// <code>
	/// CDDA cdda = new CDDA();
	/// cdda.open(0);
	/// cdda.play();	//	再生開始!
	///
	/// while (!GameFrame.pollEvent()){
	///		int u = cdda.getCurrentPos();
	///		int m,name,f;
	///		cdda.FRAMES_TO_MSF(u,m,name,f);
	///		printf("%d:%d:%d\n",m,name,f);
	///	}
	/// cdda.Dispose();
	///</code>
	/// 
	/// 注意:
	/// GCは異なるスレッドで動作していて、異なるスレッドからのこのクラスのDispose
	/// を呼び出した場合、解放処理に失敗する(SDL側の制約)可能性があるので、
	/// GCから解放される前に明示的にDisposeを呼び出してください。
	/// 
	/// </remarks>
	public class CDDA : IDisposable
	{
		///	<summary>接続されているCD-ROMの数を返す。</summary>
		///	<remarks>
		///	接続されていないときは0。
		///	</remarks>
		public static int DriveNum
		{
			get {
				using (RefSingleton<CDDAControl> cont = new RefSingleton<CDDAControl>())
				{
					return cont.Instance.DriveNum;
				}
			}
		}

		/// <summary>
		/// CD-ROMをopenする。
		/// </summary>
		/// <param name="no"></param>
		/// <returns>openに失敗すれば非0が返る。</returns>
		/// <remarks>
		/// <para>
		/// システムディフォルトのCDは必ず no は 0。
		/// (getDriveNum で返ってくる値)-1 までを指定することが出来る。
		/// </para>
		/// </remarks>
		public YanesdkResult Open(int no){
			if (cdrom != IntPtr.Zero) return YanesdkResult.AlreadyDone; // already open
			if (DriveNum <= no) return YanesdkResult.InvalidParameter;
			cdrom = SDL.SDL_CDOpen(no);
			if (cdrom == IntPtr.Zero) return YanesdkResult.SdlError; // can't open
			return YanesdkResult.NoError;
		}

		///	<summary>CDを閉じる。</summary>
		/// <remarks> 
		///	CD使いおわったら閉じてください。
		///	デストラクタから close が呼び出されるので
		///	閉じ忘れても一応は安全ですが。
		/// </remarks>
		public void Close() {
			if (cdrom != IntPtr.Zero) {
				Stop();
				//	closeする前に停止させないと再生されたままになる(SDL)
				SDL.SDL_CDClose(cdrom);
				cdrom = IntPtr.Zero;
			}
			// Dispose(true);
		}

		/// <summary>
		/// CDを停止させる
		/// </summary>
		public void Stop(){
			if (cdrom != IntPtr.Zero) {
				SDL.SDL_CDStop(cdrom);
			}
		}

/*
		///	<summary>指定したトラックを再生します。</summary>
		/// <remarks>
		///	トラックナンバーは0～
		/// </remarks>
		public YanesdkResult PlayTrack(int track) {
			if (cdrom == IntPtr.Zero) return YanesdkResult.PreconditionError; // not opened
			if ( SDL.CD_INDRIVE(SDL.SDL_CDStatus(cdrom)) ) {
				int result = SDL.SDL_CDPlayTracks(cdrom, track, 0, track+1, 0);
				if (result != 0) {
					return YanesdkResult.SdlError;
				}
			}
			return YanesdkResult.NoError;
		}
*/
        // ↑は、GetTracks() / 2 までしか再生できない。

        /// 正しく動くように、修正したバージョン by まこ
        /// 
        ///	<summary>指定したトラックを再生します。</summary>
        /// <remarks>
        ///	トラックナンバーは0～
        /// </remarks>
        public YanesdkResult PlayTrack(int track)
        {
            if (cdrom == IntPtr.Zero) return YanesdkResult.PreconditionError; // not opened
            if (SDL.CD_INDRIVE(SDL.SDL_CDStatus(cdrom)))
            {
                #region まこコメント
                ///
                /// SDL_CDPlayTracks(cdrom, start_track, start_frame, ntracks, nframes);
                /// 
                /// start_tracks : 開始トラック
                /// start_frame : 開始フレーム
                /// ntracks : 再生トラック数
                /// nframes : 再生フレーム数
                ///
                /// で、ntracksには、終了トラックではなく、再生トラック数を入れる。
                /// だから、track + 1では無く、通常1を入れる。
                /// 多分、SDLのサンプルを見て、
                /// 
                /// <CODE>
                /// /* CD 全体を再生 */
                /// if (CD_INDRIVE(SDL_CDStatus(cdrom)))
                ///    SDL_CDPlayTracks(cdrom, 0, 0, 0, 0);
                ///
                /// /* 最初のトラックを再生 */
                /// if (CD_INDRIVE(SDL_CDStatus(cdrom)))
                ///    SDL_CDPlayTracks(cdrom, 0, 0, 1, 0);
                /// </CODE>
                /// 
                /// だから、任意のトラックを再生するには、
                ///
                /// <CODE>
                /// if (CD_INDRIVE(SDL_CDStatus(cdrom)))
                ///    SDL_CDPlayTracks(cdrom, track, 0, track + 1, 0);
                /// </CODE>
                ///
                /// だと思ったのではないかと(＾＾；
                #endregion
                int result = SDL.SDL_CDPlayTracks(cdrom, track, 0, 1, 0);
                if (result != 0)
                {
                    return YanesdkResult.SdlError;
                }
            }
            return YanesdkResult.NoError;
        }

        /// 便利そうだったので、追加してみました by まこ
        ///	<summary>指定したトラックから、指定したトラック数再生します。</summary>
        /// <remarks>
        ///	トラックナンバーは0～
        /// 
        /// [track, track + n)を再生します。
        /// 
        /// track + n がトラック数を超えていたら、再生に失敗します。
        /// そのときは、InvalidParameterを返していますが、
        /// あれだったら、適当に変えてください。
        /// </remarks>
        public YanesdkResult PlayTracks(int track, int n)
        {
            if (cdrom == IntPtr.Zero) return YanesdkResult.PreconditionError; // not opened
            if (SDL.CD_INDRIVE(SDL.SDL_CDStatus(cdrom)))
            {
                int result = SDL.SDL_CDPlayTracks(cdrom, track, 0, n, 0);
                if (result != 0)
                {
                    if (n + track > GetTracks()) return YanesdkResult.InvalidParameter;

                    return YanesdkResult.SdlError;
                }
            }
            return YanesdkResult.NoError;
        }

        /// <summary>
		/// 先頭から再生する
		/// </summary>
		/// <returns></returns>
		public YanesdkResult Play()
		{
			return PlayTrack(0);
		}

		///	<summary>CD再生中かを返す。</summary>
		/// <remarks>
		///	playTrackのあと呼び出すと良い。
		/// </remarks>
		public bool IsPlaying(){
			if (cdrom == IntPtr.Zero) return false;
			return SDL.SDL_CDStatus(cdrom) == SDL.CDstatus.CD_PLAYING;
		}

		/// <summary>
		/// CDのeject
		/// </summary>
		/// <remarks>
		/// openしているCDに対してしかejectできない。
		/// </remarks>
		/// <returns></returns>
		public YanesdkResult Eject() {
			if (cdrom != IntPtr.Zero) {
				Stop();
				int result = SDL.SDL_CDEject(cdrom);
				if (result != 0) {
					return YanesdkResult.SdlError; // can't play
				}
				return YanesdkResult.NoError;
			} else {
				//	CDないで?
				return YanesdkResult.PreconditionError;
			}
		}

		/// <summary>
		/// CDのpause
		/// </summary>
		/// <remarks>
		/// 再生中でなければpauseできない。
		/// </remarks>
		/// <returns></returns>
		public YanesdkResult Pause() {
			if (cdrom != IntPtr.Zero && IsPlaying()) {
				int result = SDL.SDL_CDPause(cdrom);
				if (result != 0) {
					return YanesdkResult.SdlError; // can't play
				}
				return YanesdkResult.NoError;
			} else {
				//	CDないで?
				return YanesdkResult.PreconditionError;
			}
		}

		/// <summary>
		/// CDのresume
		/// </summary>
		/// <remarks>
		/// pauseで止めた再生を再開する
		/// </remarks>
		/// <returns></returns>
		public YanesdkResult	Resume() {
			if (cdrom != IntPtr.Zero) {
				int result = SDL.SDL_CDResume(cdrom);
				if (result != 0) {
					return YanesdkResult.SdlError; // can't play
				}
				return YanesdkResult.NoError;
			} else {
				//	CDないで?
				return YanesdkResult.PreconditionError;
			}
		}

/*
		/// <summary>
		/// 現在の再生トラック番号取得。
		/// </summary>
		/// <returns>非再生ならば-1</returns>
		public int GetPlayingTrack() {
			return GetPlayingTrack();
		}
*/
        // ↑は、無限再帰してるっちゅうねん！

        /// 正しい関数を呼ぶように修正 by まこ
        ///
        /// <summary>
        /// 現在の再生トラック番号取得。
        /// </summary>
        /// <returns>非再生ならば-1</returns>
        public int GetPlayingTrack()
        {
            return getPlayingTrack_();
        }

/*
		unsafe private int getPlayingTrack_() {
			if (!IsPlaying()) { return -1; }
			if (cdrom != IntPtr.Zero) {
				SDL.SDL_CD* cd = (SDL.SDL_CD*)cdrom;
				return cd->numtracks;
			}
			return -1; // どうなっとるんや..
		}
*/
        // ↑は、返している情報が違う。

        /// 正しく動くように、修正したバージョン by まこ
        /// 
        /// numtracksではなく、ちゃんと、cur_trackを返すように修正
        unsafe private int getPlayingTrack_()
        {
            if (!IsPlaying()) { return -1; }
            if (cdrom != IntPtr.Zero)
            {
                SDL.SDL_CD* cd = (SDL.SDL_CD*)cdrom;
                return cd->cur_track;
            }
            return -1; // どうなっとるんや..
        }

        /// どうせだから、追加してみました。by まこ
        /// 
        /// <summary>
        /// トラック総数を取得
        /// </summary>
        /// <returns>非再生ならば-1</returns>
        public int GetTracks()
        {
            return getTracks_();
        }

        /// どうせだから、追加してみました。by まこ
        unsafe private int getTracks_()
        {
            if (cdrom != IntPtr.Zero)
            {
                if (SDL.CD_INDRIVE(SDL.SDL_CDStatus(cdrom)))
                {
                    SDL.SDL_CD* cd = (SDL.SDL_CD*)cdrom;
                    return cd->numtracks;
                }
            }
            return -1; // どうなっとるんや..
        }

		/// <summary>
		/// 現在の再生ポジション取得
		/// </summary>
		///	<remarks>
		/// 非再生ならば-1。
		///	この戻り値をFRAMES_TO_MSFに食わせれば
		///	CDの先頭からの時間が取得できる。
		///
		///	ここで返されるフレームについては十分な分解能があるとは限らない。
		///	(少なくとも秒ぐらいは正しく取得できるだろうけど)
		/// </remarks><returns></returns>
		unsafe private int getCurrentPos_(){
			if (!IsPlaying()) { return -1; }
			if (cdrom != IntPtr.Zero) {
				SDL.SDL_CD* cd = (SDL.SDL_CD*)cdrom;
				return cd->cur_frame;
			}
			return -1; // どうなっとるんや..
		}


		/// <summary>
		/// FRAMEからMSFに変換。
		/// </summary>
		///	getCurrentPosの戻り値を渡して使う。
		///	f : フレーム[in]
		///	M,S,F : 分,秒,フレーム(秒間75フレームと仮定)[out]
		/// <param name="f"></param>
		/// <param name="M"></param>
		/// <param name="S"></param>
		/// <param name="F"></param>
		static void FRAMES_TO_MSF(int f, out int M, out int S, out int F) {
			const uint CD_FPS	= 75;
			int value = f;
			F = (int)(value % CD_FPS);
			value /= (int)CD_FPS;
			S = value % 60;
			value /= 60;
			M = value;
		}

		/// <summary>
		/// Disposeを呼び出したときにCloseされることは保証される。
		/// Dispose呼び出し後に再度Openすることは出来ない。
		/// </summary>
		public void Dispose()
		{
			if (cdrom != IntPtr.Zero)
			{
				Close(); // stopさせてからcloseしないとね
				//	SDL.SDL_CDClose(cdrom);
			}
			control.Dispose();
		}

		/// <summary>
		/// SDLのCD構造体を直接取得。
		/// </summary>
		/// <remarks>
		/// このクラスのメソッドで足りなければ自分でSDL_CDを用いて直接
		///	なんとかしる。
		/// </remarks>
		/// <returns></returns>
		public IntPtr SDL_CD { get { return cdrom; } }

		private IntPtr cdrom;

		/// <summary>
		/// CDDAControl(参照カウント型singletonになっている)の取得。
		/// </summary>
		/// <returns></returns>
		private CDDAControl getControl()
		{
			return Singleton<CDDAControl>.Instance;
		}
		private RefSingleton<CDDAControl> control = new RefSingleton<CDDAControl>();

		private class CDDAControl : IDisposable
		{
			public CDDAControl()
			{
				if (cdinit.Instance.Result < 0)
				{
					driveNum = 0;
					return;
				}
				driveNum = SDL.SDL_CDNumDrives();
				if (driveNum == -1)
				{
					driveNum = 0; // -1とかになってると後々ややこしいナリよ
				}
			}

			/// <summary>
			/// ドライブ数の取得。
			/// </summary>
			public int DriveNum { get { return driveNum; } } 
			private int	driveNum;

			public void Dispose()
			{
				cdinit.Dispose();
			}

			private RefSingleton<Yanesdk.Sdl.SDL_CDROM_Initializer>
				cdinit = new RefSingleton<Yanesdk.Sdl.SDL_CDROM_Initializer>();
		}
	}
}
