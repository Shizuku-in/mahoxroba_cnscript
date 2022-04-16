
ネットワークモジュール for YaneuraoGameSDK.NET

■ 使い方

Visual C++ 2005なら、
・NetworkModule\include をインクルードパスに追加
・release\NetworkModule.lib をリンクするよう設定
・実行ファイルと同じ場所に release\*.dll を置く
という手順で使えます。

他のコンパイラの場合は、以下を参考にコンパイルして使ってください。


■ Visual C++ 2005以外でのコンパイル方法

・boostをインストール
・ace/include に ACE_wrappers/ace をコピー
  ※ ace/include/ace/ACE.h などとなるように。
  ( http://download.dre.vanderbilt.edu/ の ACE-5.5.zip )
・ACEとzlib( http://zlib.net/ )をコンパイルし、
  ACE.lib, zdll.lib をリンクするよう設定
・ace/include, zlib/include, NetworkModule/includeをインクルードパスに追加
・NDEBUGを定義して、NetworkModule/src/*.cppをコンパイル


■ Linuxなどでのコンパイル方法

・boost, zlib, ACEをインストール
・NetworkModule/include をインクルードパスに追加
・NDEBUGを定義して、NetworkModule/src/*.cppをコンパイル

例(gcc):

 gcc -fPIC -shared -Wl,-soname,libNetworkModule.so -o libNetworkModule.so \
    -DNDEBUG -I./NetworkModule/include -lz -lACE NetworkModule/src/*.cpp

 gcc -lNetworkModule -L. -I./NetworkModule/include NetworkModuleTest/*.cpp


■ Visual C++ 2005でのコンパイル方法(念のため)

・boostをインストール
・ace/include に ACE_wrappers/ace をコピー
  ※ ace/include/ace/ACE.h などとなるように。
  ( http://download.dre.vanderbilt.edu/ の ACE-5.5.zip )
・NetworkModule.sln をビルド


