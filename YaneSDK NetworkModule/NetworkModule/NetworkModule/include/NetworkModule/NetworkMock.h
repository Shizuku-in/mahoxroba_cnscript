#ifndef __NETWORKMOCK_H__
#define __NETWORKMOCK_H__
#include <boost/noncopyable.hpp>
#include <boost/shared_ptr.hpp>
#include "INetwork.h"
#pragma warning(push)
#pragma warning(disable : 4275)
namespace Yanesdk { namespace Network {

struct NetworkMockData;

/**
 *  ネットワークの仮クライアント(mock object)
 *
 *  開発時には、1台のマシンでサーバーとクライアントを動かしたり
 *  1台のマシンで複数のクライアントを動かしたりする。そのために
 *  仮想的に接続するためのmockを用意する。
 */
class NETWORKMODULE_API NetworkMock : public INetwork,  private boost::noncopyable {
    NetworkMockData* data;
public:

    NetworkMock();
    ~NetworkMock();

    /// ハンドルの生成のためのincremental counter
    uint64_t GetNow();

    /// 動作テストのために、Connectしたときに別のNetworkMockのobjectがListenで接続を確立できるようにattachする。
    void Attach(NetworkMock& neighbor);

    uint64_t Connect();
    uint64_t Listen();
    void Disconnect(uint64_t handle);

    int Flush(uint64_t handle);

    bool GetIpAddress(uint64_t handle, RawMemory& ipAddress);
    void SetHostName(const char* hostname, int portnumber);
    void SetMaxPacketLength(int maxPacketLength);

protected:
    int InnerRead(uint64_t handle, RawMemory& data);
    int InnerWrite(uint64_t handle, const void* data, size_t dataLength);
};

}} // namespace
#pragma warning(pop)
#endif // __NETWORKMOCK_H__
