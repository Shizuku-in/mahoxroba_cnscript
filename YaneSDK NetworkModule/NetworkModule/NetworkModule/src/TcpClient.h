#ifndef __TCPCLIENT_H__
#define __TCPCLIENT_H__
#include <NetworkModule/RawMemory.h>
namespace Yanesdk { namespace Network {

/**
 *  出来るだけC#のコードと同じような感じに実装するためのクラス。
 *  System.Net.Sockets.TcpClient と System.Net.Sockets.NetworkStream 相当。
 */
class TcpClient {
    bool connected;
    int sendTimeout;
    int recvTimeout;
    ACE_SOCK_Connector connector;
    ACE_SOCK_Stream stream;
    bool dataAvailable;
    uint8_t dataBuffer[1]; // dataAvailableチェック用バッファ
public:
    TcpClient();
    TcpClient(const std::string& hostName, int portNumber);
    ~TcpClient();

    void Connect(const std::string& hostName, int portNumber);
    bool Connected() const;

    void Close();

    void SetSendTimeout(int n);
    void SetReceiveTimeout(int n);
    int GetSendBufferSize();
    int GetReceiveBufferSize();
    void SetSendBufferSize(int n);
    void SetReceiveBufferSize(int n);

    bool DataAvailable();
    int Read(void* data, int n, int size);
    void Write(const void* data, int n, int size);
    void Flush();

    bool GetIpAddress(RawMemory& memory);

    ACE_SOCK_Stream& GetStream() { return stream; }
};

}} // namespace
#endif // __TCPCLIENT_H__
