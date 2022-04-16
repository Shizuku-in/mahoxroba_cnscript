#ifndef __TCPLISTENER_H__
#define __TCPLISTENER_H__
namespace Yanesdk { namespace Network {

class TcpClient;

/**
 *  出来るだけC#のコードと同じような感じに実装するためのクラス。
 *  System.Net.Sockets.TcpListener相当。
 */
class TcpListener {
    int portNumber;
    ACE_SOCK_Acceptor accepter;
    boost::shared_ptr<TcpClient> client;
    bool ready;
public:
    TcpListener(int portNumber);
    ~TcpListener();

    void Start();
    void Stop();

    bool Pending();

    boost::shared_ptr<TcpClient> AcceptTcpClient();
};

}} // namespace
#endif // __TCPLISTENER_H__
