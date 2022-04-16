#ifndef __NETWORK_H__
#define __NETWORK_H__
#include <boost/optional.hpp>
#include <boost/noncopyable.hpp>
#include "INetwork.h"
#pragma warning(push)
#pragma warning(disable : 4275)
namespace Yanesdk { namespace Network {

struct NetworkData;

/// TCPで双方向通信を行うクラス
class NETWORKMODULE_API Network : public INetwork, private boost::noncopyable {
    NetworkData* data;
public:
    /// 初期ハンドル値
    static uint64_t INITIAL_HANDLE_VALUE;

    Network();
    ~Network();

    /**
     *  ネットワークIDを取得・設定します。
     *  ネットワークIDを使用しない場合はnull値です。
     *
     *  異なるネットワークIDを持つインスタンス同士の通信はできません。
     *  サーバ側はListenメソッドを呼び出す前、クライアント側はConnectメソッドを呼び出す前に
     *  それぞれ設定しておく必要があります。
     */
    void SetNetworkID(boost::optional<uint64_t> n);
    boost::optional<uint64_t> GetNetworkID() const;

    /**
     *  何回のReadメソッドの呼び出しで、KeepAliveメッセージを接続先に送信するかどうかを取得・設定します。
     *
     *  なお、Readの呼び出し回数は、各handleごとにカウントします。
     *
     *  ディフォルトは 900で、秒間30回のReadを行なうとして、
     *  900 = 30回×30秒であり、30秒ごとに切断検知のためのKeepAliveメッセージを送信します。
     *
     *  0が設定されていると送信しません。
     *  送信しない場合、切断を検知できないので、かならず設定するようにしてください。
     */
    void SetKeepAliveSendInterval(uint64_t n);
    uint64_t GetKeepAliveSendInterval() const;

    /**
     *  送信バッファのサイズを指定します。既定では8192バイトです。
     * 
     *  受信側でReadメソッドが定期的に呼び出されなかったり、
     *  呼び出しのインターバルが甘かったりして、
     *  受信側の受信バッファに残ったままのバイト数が
     *  ReadBufferSizeの値を超えると、バッファあふれとなり
     *  受信元との接続は切断されます。
     */
    void SetWriteBufferSize(int size);
    int GetWriteBufferSize() const;

    /**
     *  受信バッファのサイズを指定します。
     *  既定では8192バイトです。
     *
     *  Readメソッドが定期的に呼び出されなかったり、
     *  呼び出しのインターバルが甘かったりして、
     *  受信バッファに残ったままのバイト数が
     *  ReadBufferSizeの値を超えると、バッファあふれとなり
     *  送信元との接続は切断されます。
     */
    void SetReadBufferSize(int size);
    int GetReadBufferSize() const;

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
#endif // __NETWORK_H__
