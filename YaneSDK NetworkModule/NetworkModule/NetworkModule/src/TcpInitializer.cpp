#include "stdafx.h"
#include "TcpInitializer.h"
namespace Yanesdk { namespace Network {

TcpInitializer::TcpInitializer() {
    ACE::init();

    // Linux�Ȃǂł́A�ؒf���ꂽ�\�P�b�g�ɑ΂��ēǂݏ��������悤�Ƃ���ƁA
    // �V�O�i��SIGPIPE����������B������abort����Ă͍���̂ŁA��������悤�ɐݒ肵�Ă����B
    // SDL_net�Ȃǂł̓��C�u�������ł���Ă����̂����A���̂�ACE�͂���Ă���Ȃ��悤�ȁc�B
#if defined(SIGPIPE) && SIGPIPE != 0 // ��SIGPIPE������`�Ȋ��ł�ACE���ŏ����0�ɒ�`���ꂿ�Ⴄ
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
