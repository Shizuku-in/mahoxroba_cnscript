#include "stdafx.h"
#include "MemoryCopy.h"
#include "TcpInitializer.h"
#include "TcpClient.h"
namespace Yanesdk { namespace Network {

TcpClient::TcpClient() : sendTimeout(0), recvTimeout(0), dataAvailable(false) {
    connected = false;
    TcpInitializer::Initialize();
}

TcpClient::TcpClient(const std::string& hostName, int portNumber) : connected(false), sendTimeout(0), recvTimeout(0), dataAvailable(false) {
    connected = false;
    TcpInitializer::Initialize();
    Connect(hostName, portNumber);
}

TcpClient::~TcpClient() {
    Close();
}

void TcpClient::Connect(const std::string& hostName, int portNumber) {
    Close();
    ACE_INET_Addr addr((u_short)portNumber, hostName.c_str());
    if (connector.connect(stream, addr) != -1) {
        connected = true;
    }
}

bool TcpClient::Connected() const {
    return connected;
}

void TcpClient::Close() {
    stream.close();
    connected = false;
    dataAvailable = false;
}

void TcpClient::SetSendTimeout(int n) {
    sendTimeout = n;
  #ifndef NDEBUG
    sendTimeout = 1000;
  #endif
}

void TcpClient::SetReceiveTimeout(int n) {
    recvTimeout = n;
  #ifndef NDEBUG
    recvTimeout = 1000;
  #endif
}

int TcpClient::GetSendBufferSize() {
    int n = 0;
    int len = sizeof(n);
    stream.get_option(SOL_SOCKET, SO_SNDBUF, &n, &len);
    return n;
}

int TcpClient::GetReceiveBufferSize() {
    int n = 0;
    int len = sizeof(n);
    stream.get_option(SOL_SOCKET, SO_RCVBUF, &n, &len);
    return n;
}

void TcpClient::SetSendBufferSize(int n) {
    stream.set_option(SOL_SOCKET, SO_SNDBUF, &n, sizeof(n));
}

void TcpClient::SetReceiveBufferSize(int n) {
    stream.set_option(SOL_SOCKET, SO_RCVBUF, &n, sizeof(n));
}

namespace {
    static inline int InnerRead(ACE_SOCK_Stream& stream, void* data, int n, int size, const ACE_Time_Value& timeout) {
        return stream.recv_n((uint8_t*)data + n, size, &timeout);
    }

    static inline int InnerWrite(ACE_SOCK_Stream& stream, const void* data, int n, int size, const ACE_Time_Value& timeout) {
        return stream.send_n((const uint8_t*)data + n, size, &timeout);
    }
}

bool TcpClient::DataAvailable() {
    if (!dataAvailable) {
        // 試しに１バイトだけ取得してみる
        ssize_t result = InnerRead(stream, dataBuffer, 0, 1, ACE_Time_Value(0, 0));
        if (result == 1) {
            // データ取得出来た
            dataAvailable = true;
        }
    }
    return dataAvailable;
}

int TcpClient::Read(void* data, int n, int size) {
    if (0 < size && dataAvailable) {
        dataAvailable = false;
        *(uint8_t*)data = dataBuffer[0];
        if (size == 1) {
            return 1;
        } else {
            ssize_t result = Read(data, n + 1, size - 1);
            if (0 < result) {
                result += 1;
            }
            return result;
        }
    } else {
      #ifndef NDEBUG
        errno = 0;
      #endif
        int result = InnerRead(stream, data, n, size, ACE_Time_Value(recvTimeout / 1000, (recvTimeout % 1000) * 1000));
      #ifndef NDEBUG
        if (result < 0) {
            fprintf(stderr, "recv error: errno = %d\n", errno);
            fflush(stderr);
        }
      #endif
        if (result != size) throw std::exception();
        return result;
    }
}

void TcpClient::Write(const void* data, int n, int size) {
  #ifndef NDEBUG
    errno = 0;
  #endif
    int result = InnerWrite(stream, data, n, size, ACE_Time_Value(sendTimeout / 1000, (sendTimeout % 1000) * 1000));
  #ifndef NDEBUG
    if (result < 0) {
        fprintf(stderr, "send error: errno = %d\n", errno);
        fflush(stderr);
    }
  #endif
    if (result != size) throw std::exception();
}

void TcpClient::Flush() {
}

bool TcpClient::GetIpAddress(RawMemory& memory) {
    ACE_INET_Addr addr;
    if (stream.get_remote_addr(addr) == -1) return false;
    memory.Resize(4);
    ACE_UINT32 n = addr.get_ip_address();
    memory.Get()[0] = (uint8_t)(n >> 24);
    memory.Get()[1] = (uint8_t)(n >> 16);
    memory.Get()[2] = (uint8_t)(n >> 8);
    memory.Get()[3] = (uint8_t)n;
    return true;
}

}} // namespace
