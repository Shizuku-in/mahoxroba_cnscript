#include "stdafx.h"
#include "TcpClient.h"
#include "TcpInitializer.h"
#include "TcpListener.h"
namespace Yanesdk { namespace Network {

TcpListener::TcpListener(int portNumber) : ready(false), portNumber(portNumber) {
    TcpInitializer::Initialize();
}

TcpListener::~TcpListener() {
}

void TcpListener::Start() {
    Stop();
    ACE_INET_Addr addr((u_short)portNumber);
    if (accepter.open(addr, 1) != -1) {
        ready = true;
    }
}

void TcpListener::Stop() {
    accepter.close();
    ready = false;
}

bool TcpListener::Pending() {
    if (!ready) return false;
    if (client) return true;
    ACE_Time_Value timeout(0, 0);
    boost::shared_ptr<TcpClient> p(new TcpClient); // ”÷–­‚É–³‘Ê‚Á‚Û‚¢‚Ì‚¾‚ªcB
    if (accepter.accept(p->GetStream(), NULL, &timeout) != -1) {
        client = p;
        return true;
    }
    return false;
}

boost::shared_ptr<TcpClient> TcpListener::AcceptTcpClient() {
 // if (!client) throw std::exception();
    boost::shared_ptr<TcpClient> p = client;
    client.reset();
    return p;
}

}} // namespace
