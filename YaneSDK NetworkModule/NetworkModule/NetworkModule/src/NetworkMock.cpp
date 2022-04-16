#include "stdafx.h"
#include <NetworkModule/NetworkMock.h>
namespace Yanesdk { namespace Network {

/// Attachしている相手と、その相手側のハンドルを格納する構造体
struct NetworkPacket {
    NetworkMock& Partner; // 接続相手
    uint64_t PartnerHandle;    // 接続相手側のhandle
    /// 相手から送られてきたデータのqueue
    std::list<std::vector<uint8_t> > Data;

    NetworkPacket(NetworkMock& partner, uint64_t partnerHandle) : Partner(partner) {
        PartnerHandle = partnerHandle;
    }
};

/// NetworkMockのメンバ変数やprivate関数など。
struct NetworkMockData {
    uint64_t now;
    NetworkMock* neighbor;
    /// handleからNetworkTest(Attachしている相手と、その相手側のハンドルと送られてきたデータ)へのmap
    std::map<uint64_t, boost::shared_ptr<NetworkPacket> > dic;
    /// Listenするqueue。相手からの接続要求がここに入っている。接続相手と接続相手側のhandleが入っている。
    std::list<boost::shared_ptr<NetworkPacket> > listenQueue;
};


NetworkMock::NetworkMock() : data(new NetworkMockData()) {
    data->now = 0;
}

/// 切断するのを実装しちゃう。
NetworkMock::~NetworkMock() {
 // uint64_t[] keys = new uint64_t[data->dic.Count]; // スレッドセーフでねぇっすけども(´ω`)
 // data->dic.Keys.CopyTo(keys, 0);
 // foreach (uint64_t handle in keys)
 //     Disconnect(handle);

    // ↑これだと二つずつ消えるからDisconnectで例外が飛んで気持ち悪い

    std::map<uint64_t, boost::shared_ptr<NetworkPacket> >::iterator
        it = data->dic.begin(), itEnd = data->dic.end();
    while (it != itEnd) {
        uint64_t n = (it++)->first;
        Disconnect(n);
    }

    delete data;
}

/// ハンドルの生成のためのincremental counter
uint64_t NetworkMock::GetNow() {
    return ++data->now;
}

/// 動作テストのために、Connectしたときに別のNetworkMockのobjectがListenで接続を確立できるようにattachする。
void NetworkMock::Attach(NetworkMock& neighbor) {
    data->neighbor = &neighbor;
}

uint64_t NetworkMock::Connect() {
    // attachしている奴に接続要求を出す
    uint64_t now = GetNow();
    data->neighbor->data->listenQueue.push_back(
        boost::shared_ptr<NetworkPacket>(new NetworkPacket(*this, now)));
    return now;
}

uint64_t NetworkMock::Listen() {
    // 接続要求が来ているか？
    if (data->listenQueue.front() == NULL) return 0;

    // 接続要求があるので受け入れる
    boost::shared_ptr<NetworkPacket> packet = data->listenQueue.front();
    uint64_t now = GetNow();
    data->dic.insert(std::make_pair(now, packet));

    boost::shared_ptr<NetworkPacket> packet2(new NetworkPacket(*this, now));
    packet->Partner.data->dic.insert(std::make_pair(packet->PartnerHandle, packet2));

    data->listenQueue.pop_front(); // popしたので削除

    return now;
}

void NetworkMock::Disconnect(uint64_t handle) {
    // 相手側のhandleを除去
    std::map<uint64_t, boost::shared_ptr<NetworkPacket> >::iterator it = data->dic.find(handle);
    if (it != data->dic.end() &&
        it->second->Partner.data->dic.find(it->second->PartnerHandle) != it->second->Partner.data->dic.end()) {
        it->second->Partner.data->dic.erase(it->second->PartnerHandle);

        // 本当は相手のlisten queueからも除去しないといけない。
        // しかし、ここではそこまでエミュレーションする必要はないだろう。

        // 自分のhandleを除去
        data->dic.erase(handle);
    }
        // デバッグ用だから、これでいいか…
        // すでにサーバー側でDisconnectが
        // 呼び出されたconnectionかも知れないし。
}

int NetworkMock::InnerRead(uint64_t handle, RawMemory& memory) {
    // dataはFIFO
    if (data->dic.find(handle) == data->dic.end()){
        memory.Resize(0);
        return 1;
    }
    if (data->dic[handle]->Data.size() == 0) {
        memory.Resize(0);
        return 0;
    }
    memory.CopyFrom(data->dic[handle]->Data.front());
    data->dic[handle]->Data.pop_front(); // ひとつpopしたので先頭からひとつ消す
    return 0;
}

int NetworkMock::InnerWrite(uint64_t handle, const void* memory, size_t dataLength) {
    // 相手のdata queueに積む
    if (data->dic.find(handle) == data->dic.end())
        return 1;

    // deep copyしておかないと送信元で書き換えた場合うまく動かない。
    std::vector<uint8_t> data2((uint8_t*)memory, (uint8_t*)memory + dataLength);

    data->dic[handle]->Partner.data->dic[data->dic[handle]->PartnerHandle]->Data.push_back(data2);
    return 0;
}

int NetworkMock::Flush(uint64_t handle) {
    return 0;
}

/// <summary>
/// IPアドレスを取得する
/// </summary>
/// <param name="handle">IPアドレスを取得するハンドル</param>
/// <param name="ipAddress">IPアドレス</param>
/// <returns>true:成功, false:失敗</returns>
bool NetworkMock::GetIpAddress(uint64_t handle, RawMemory& ipAddress) {
    uint8_t data[4] = { 127, 0, 0, 1 };
    ipAddress.Resize(0);
    ipAddress.Append(data, sizeof(data));
    return true;
}

/// <summary>
/// 接続先ホスト名とポートを指定します。
/// </summary>
/// <param name="hostname">ホスト名。受付側として利用する場合は無効</param>
/// <param name="portnumber">ポート番号</param>
void NetworkMock::SetHostName(const char* hostname, int portnumber) {
}

/// <summary>
/// 最大パケット長制限を設ける
/// </summary>
/// <param name="maxPacketLength">値>0なら制限値、値<=0なら制限しない</param>
void NetworkMock::SetMaxPacketLength(int maxPacketLength) {
}

}} // namespace
