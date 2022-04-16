#ifndef __TCPLISTENER_H__
#define __TCPLISTENER_H__
namespace Yanesdk { namespace Network {

class TcpClient;

/**
 *  �o���邾��C#�̃R�[�h�Ɠ����悤�Ȋ����Ɏ������邽�߂̃N���X�B
 *  System.Net.Sockets.TcpListener�����B
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
