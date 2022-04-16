
YaneuraoGameSDK.NET

						programmed and designed by やねうらお


■　サイトURL


公式サイト
http://yanesdkdotnet.sourceforge.jp/

詳細なロードマップや、過去の更新履歴、技術情報、ライセンス情報などは
すべてWikiのほうでご覧になれます。
http://yanesdkdotnet.sourceforge.jp/wiki/

このreadme.txtの情報は最新の更新履歴を除いてwikiに移行します。
wikiのほうを中心にご覧ください。



■　Yanesdk.NET 改変履歴


2007/12/17  Version 1.56

	開発活動再開。
	
	1.50系のbugと細かな拡張がいろいろあるので、少しずつ修正して公開しはじめる。
	今回の公開は、自分用の管理用です(´ω｀)

	・TextureVector→Texturesと名前変更。
	・IScreen.Blt(ITexture)追加。

	・GlExtensionsで、OpenGLの拡張機能を取得したときに
		多少変な文字列が来ても大丈夫なように修正。
		(たまに同じ名前のGL拡張を2度返すドライバがある)

	・Yanesdk.Networkで
	>     _listener = new TcpListener(IPAddress.Loopback, _port);
	とやれば、localhostからの待ち受けのためにportをListenするときに
	WindowsXPなどのFireWallの警告ダイアログが
	出ないので、LoopBackというプロパティを用意して、この設定ができるようにした。
	
		
	※　次に反映する予定
	・掲示板で指摘をもらっているbug等は、まだ反映させていない。
	(1週間後ぐらいにすべて修正したものをupする予定です。すみませぬ..。)
	

2007/01/24
	
	・Lua.NET mono化終了。Lua.NETの作者に送りつける。
		次バージョンでLua.NET公式のほうで対応してくれるとのこと。
	・SurfaceのGetPixelを8bpp(カラーパレット)対応。
	これで256色パレットを用いているgifファイルの抜き色が正しく扱える。
	・Rect,Sizeをstructからclassに。
	・描画エンジン大改造中。
	→Draw/Geometryフォルダ追加。幾何関連のクラス群はすべてここに入れることにした。
	(ここには開発中のコード多々あり。まだすべては正常動作しない。
	正常動作しないものはプロジェクトファイルに含めていない。)


2007/01/06	Version 1.55(1.50系 final)

	安定しているのでこれにして1.50系は開発終了。
	1.60系では
	・スプライト
	・ユーザーフォント
	・マップ表示機能
	・シナリオ表示機能
	・Luaとの融合
	を予定している。


2006/12/25

	・FontでUTF8で渡すメソッドは 終端が\0であることをC#側で
		保証しなければならないので修正。


2006/12/22

	・DirEnumeratorはFileSys.DirectoryPolicyに従うように。

	・Movieクラスのフォルダを移動させた。

2006/12/21

	・DBColumnAttributte→DBColumnAttribute
		綴り間違ってた(´ω`)


2006/12/20	version 1.54(1.50系Finalβ)

	・LinuxとWindowsのバイナリを統合。
		DLLが見つからないときはAssemblyResolveで解決するようにした。
		解決するコードはSDLInitializerに含めた。
		→が、managed dllはAssemblyResolveイベントが発生しないことに
		いまさらながら気づいた(´ω`)

	・FileSysに書きかけのコード(IsExeRelative)が残っていたので除去。

	・Linux/MacOS用のconfigを用意。
		monoではconfigファイルは共通化できるようなので共通化した。

		Yanesdk.dll.config

		Linux/Mac環境で動かすときは、上記ファイルをYanesdk.dllと
		同じフォルダに配置すること。

	・MacOSで手と栗鼠の動作確認をした。(thx.Ozyさん)

	・MacOS用にコンパイルするときに警告が出ていたのを抑制した。
	
	・DLL_DEPLOY_LIBオプション廃止。DllManagerのDLL_CURRENTを書き変えることによって
		dll配置フォルダは動的に変更できるようにした。


2006/12/19

	・System.PlatformにプラットフォームIDを追加。

	・DLLファイル名の定義が分散していたので
		SDL関連はすべてSDL_Initializerに固めた。
		→　FileArchiverZipで用いているDLLファイル名については考え中。

	・CurrentDirectoryHelper完成。
		→FileSys,DllManagerに導入
			FileSys.DirectoryPolicy追加。
			DllManager.DirectoryPolicy追加。
			両方ともdefaultでは実行ファイルの配置されているフォルダ相対で
			読みに行くようになっている。

	・SDLでMacOS対応箇所、修正。
		※動作確認はまだとれておらず。
		→SDL_Initializerで初期化/終了処理を行なうように変更。
		
		SDL関連のunitの参照カウントをとるが、これが0になった瞬間に
		SQL_Quitを呼び出すので、参照カウントが0になるような使い方をしてはならない。
		普通は画面に描画するためにSurfaceなり何なりを持っているだろうから
		0にはならないと思うのだが…0になると気持ち悪いので、やはり終了メソッド追加。
		→SDLFrame.Quit追加。SDLWindowを用いる場合は最後に必ずこれを呼び出すべし。
		(そうしないとMacで正常に終了できない)



■　ここ以前の更新履歴についてはwikiのほうから参照できます。

