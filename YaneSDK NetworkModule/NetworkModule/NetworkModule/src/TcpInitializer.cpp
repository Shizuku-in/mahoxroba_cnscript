#include "stdafx.h"
#include "TcpInitializer.h"
namespace Yanesdk { namespace Network {

TcpInitializer::TcpInitializer() {
    ACE::init();

    // Linuxなどでは、切断されたソケットに対して読み書きをしようとすると、
    // シグナルSIGPIPEが発生する。そこでabortされては困るので、無視するように設定しておく。
    // SDL_netなどではライブラリ側でやってくれるのだが、何故かACEはやってくれないような…。
#if defined(SIGPIPE) && SIGPIPE != 0 // ←SIGPIPEが未定義な環境ではACE側で勝手に0に定義されちゃう
	void (* handler)(int);
	handler = signal(SIGPIPE, SIG_IGN);
	if (handler != SIG_DFL) {
		signal(SIGPIPE, handler);
	}
#endif
}

TcpInitializer::~TcpInitializer() {
    ACE::fini();
}

TcpInitializer& TcpInitializer::GetInstance() {
    static TcpInitializer instance;
    return instance;
}

}} // namespace
