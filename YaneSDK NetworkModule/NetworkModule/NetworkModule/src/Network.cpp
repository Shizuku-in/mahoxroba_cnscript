#include "stdafx.h"
#include <zlib.h>
#include "TcpClient.h"
#include "TcpListener.h"
#include "MemoryCopy.h"
#include <NetworkModule/Network.h>
namespace Yanesdk { namespace Network {

namespace PacketType {
    /// パケット種別
    enum PacketType {
        /// 生存通知メッセージ
        KeepAliveNotify = 0,
        /// 切断通知メッセージ
        DisconnectNotify = 1,
        /// 通常メッセージ
        NormalMessage = 2,
        /// ネットワークID通知メッセージ
        NetworkIDNotify = 3,
    };
}

struct ClientInfo {
    boost::shared_ptr<TcpClient> Connection;
    bool IsReceiveNetworkID;
    uint64_t ActivityCheckCounter;

    ClientInfo(boost::shared_ptr<TcpClient> connection) {
        Connection = connection;
        IsReceiveNetworkID = false;
        ActivityCheckCounter = 0;
    }
};

/// Networkのメンバ変数やprivate関数など。
struct NetworkData {
    boost::optional<uint64_t> networkID;
    uint64_t keepAliveSendInterval;
    int writeBufferSize;
    int readBufferSize;
    std::string hostName;
    int port;
    int maxPacketSize;
    uint64_t nextHandle;
    /// handle値とClientInfoオブジェクトのマップ
    std::map<uint64_t, boost::shared_ptr<ClientInfo> > clientInfoMap;
    /// TCP接続を待機するリスナ。Listenメソッドの呼び出しで初期化される。
    std::auto_ptr<TcpListener> listener;

    /// <summary>
    /// 指定された接続にパケット種別を付加してデータを送信します。
    /// preferCompress == trueの時、データを圧縮してサイズが
    /// 元のサイズより減少する場合は圧縮データを送信します。
    /// </summary>
    /// <param name="client">送信するTcpClientオブジェクト</param>
    /// <param name="type">送信データの種別</param>
    /// <param name="data">送信データ</param>
    /// <param name="preferCompress">データを圧縮する場合はtrue</param>
    void InnerWrite(TcpClient& client, PacketType::PacketType type, const void* data, size_t dataLength, bool preferCompress) {
        //  パケット種別を書き込む
        std::vector<uint8_t> datatype = MemoryCopy::ToByteArrayFromInt((int)type);
        client.Write(&datatype[0], 0, (int)datatype.size());
        if (data != NULL) {
            //  送信データの圧縮
            RawMemory compressedData;
            if (preferCompress) {
                Deflate(compressedData, data, dataLength);
            }
            uint8_t datasize[8];
            if (compressedData.GetSize() != 0 && compressedData.GetSize() < dataLength) {
                //  圧縮データのサイズと展開後のサイズを書き込む
                MemoryCopy::SetInt(datasize, 0, (int)compressedData.GetSize());
                MemoryCopy::SetInt(datasize, 4, (int)dataLength);
                client.Write(datasize, 0, sizeof(datasize));
                //  圧縮データを書き込む
                client.Write(compressedData.Get(), 0, (int)compressedData.GetSize());
            } else {
                //  圧縮してもサイズが減少しなかったので元のデータを書き込む
                MemoryCopy::SetInt(datasize, 0, (int)dataLength);
                MemoryCopy::SetInt(datasize, 4, (int)dataLength);
                client.Write(datasize, 0, sizeof(datasize));
                client.Write(data, 0, (int)dataLength);
            }
        } else {
            //  送信データのサイズを書き込む（値は0）
            std::vector<uint8_t> datasize = MemoryCopy::ToByteArrayFromInt(0);
            client.Write(&datasize[0], 0, (int)datasize.size());
        }
    }

    /// <summary>
    /// Flush動作を行います。
    /// </summary>
    void InnerFlush(TcpClient& client) {
        client.Flush();
    }

    /// <summary>
    /// 指定された接続ハンドルに対応した接続を切断します。
    /// sendNotifyMessage == falseの場合、切断通知は送りません。
    /// </summary>
    /// <param name="handle">接続ハンドル</param>
    /// <param name="sendNotifyMessage">切断通知を接続先に送信する場合はtrue</param>
    void InnerDisconnect(uint64_t handle, bool sendNotifyMessage) {
        TcpClient* client = GetTcpClient(handle);
        if (client == NULL) return;

        if (sendNotifyMessage) {
            //  切断メッセージを送る
            try {
                client->SetSendBufferSize(0); // 即送信されるように
                InnerWrite(*client, PacketType::DisconnectNotify, NULL, 0, false);
                InnerFlush(*client);
                client->Close();
            } catch (...) {
            }
        }
        RemoveClient(handle);
    }

    /// <summary>
    /// 指定されたTcpClientオブジェクトをTcpClientマップに追加し、接続ハンドルを生成して返します。
    /// </summary>
    /// <param name="client">接続が確立されているTcpClientオブジェクト</param>
    /// <returns>接続ハンドル</returns>
    uint64_t AddClient(const boost::shared_ptr<TcpClient>& client) {
        uint64_t handle = nextHandle++;
        clientInfoMap[handle].reset(new ClientInfo(client));
        return handle;
    }

    /// <summary>
    /// 指定された接続ハンドルに対応したTcpClientオブジェクトをTcpClientマップから削除します。
    /// TcpClientオブジェクトは前もって閉じておく必要があります。
    /// </summary>
    /// <param name="handle">接続ハンドル</param>
    void RemoveClient(uint64_t handle) {
        CheckValidHandle(handle);
        clientInfoMap.erase(handle);
     // _removedHandleList.Add(handle);
    }

    /// <summary>
    /// 指定された接続ハンドルが有効なものかをチェックし、有効でない場合は例外を発生させます。
    /// </summary>
    /// <param name="handle">接続ハンドル</param>
    /// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
    void CheckValidHandle(uint64_t handle) {
        if (clientInfoMap.find(handle) ==  clientInfoMap.end()) {
            throw std::exception();
        }
    }

    /// <summary>
    /// 指定された接続ハンドルに対応したTcpClientオブジェクトを取得します。
    /// </summary>
    /// <param name="handle">接続ハンドル</param>
    /// <returns>接続ハンドルに対応したTcpClientオブジェクト</returns>
    TcpClient* GetTcpClient(uint64_t handle) {
        std::map<uint64_t, boost::shared_ptr<ClientInfo> >::iterator it = clientInfoMap.find(handle);
        return it == clientInfoMap.end() ? NULL : it->second->Connection.get();
    }

    /// データを圧縮します。
    void Deflate(RawMemory& dst, const void* src, size_t srcSize) {
        z_stream z = {0};
     // if (deflateInit2(&z, Z_BEST_COMPRESSION, MAX_MEM_LEVEL, -MAX_WBITS, 8, Z_DEFAULT_STRATEGY) != Z_OK)
        if (deflateInit2(&z, Z_DEFAULT_COMPRESSION, Z_DEFLATED, -MAX_WBITS, 8, Z_DEFAULT_STRATEGY) != Z_OK)
            throw std::exception();

        uint8_t buf[0x1000];

        z.next_in = (Bytef*)src;
        z.avail_in = (uInt)srcSize;
        z.next_out = buf;
        z.avail_out = sizeof(buf);
        while (true) {
            int status = deflate(&z, Z_FINISH);
            if (status == Z_STREAM_END) break;
            if (status != Z_OK) {
                deflateEnd(&z);
                throw std::exception();
            }
            if (z.avail_out == 0) {
                dst.Append(buf, sizeof(buf));
                z.next_out = buf;
                z.avail_out = sizeof(buf);
            }
        }
        size_t remain = sizeof(buf) - z.avail_out;
        if (0 < remain) {
            dst.Append(buf, remain);
        }

        if (deflateEnd(&z) != Z_OK) {
            throw std::exception();
        }
    }

    /// 圧縮されたデータを解凍します。
    void Inflate(RawMemory& dst, const void* src, size_t srcSize) {
        z_stream z = {0};
        if (inflateInit2(&z, -MAX_WBITS) != Z_OK)
            throw std::exception();

        uint8_t buf[0x1000];

        z.next_in = (Bytef*)src;
        z.avail_in = (uInt)srcSize;
        z.next_out = buf;
        z.avail_out = sizeof(buf);
        while (true) {
            int status = inflate(&z, Z_NO_FLUSH);
            if (status == Z_STREAM_END) break;
            if (status != Z_OK) {
                inflateEnd(&z);
                throw std::exception();
            }
            if (z.avail_out == 0) {
                dst.Append(buf, sizeof(buf));
                z.next_out = buf;
                z.avail_out = sizeof(buf);
            }
        }
        size_t remain = sizeof(buf) - z.avail_out;
        if (0 < remain) {
            dst.Append(buf, remain);
        }

        if (inflateEnd(&z) != Z_OK) {
            throw std::exception();
        }
    }

    /// <summary>
    /// NetworkIDメッセージを送信します。
    /// 送信に失敗した場合は例外が発生します。
    /// </summary>
    /// <param name="handle"></param>
    /// <param name="client"></param>
    void SendNetworkIDMessage(uint64_t handle, TcpClient& client) {
        std::vector<uint8_t> data = MemoryCopy::ToByteArrayFromLong(*networkID);
        InnerWrite(client, PacketType::NetworkIDNotify, &data[0], data.size(), false);
        InnerFlush(client);
    }

};

uint64_t Network::INITIAL_HANDLE_VALUE = 1;

Network::Network() : data(new NetworkData()) {
    data->keepAliveSendInterval = 900;
    data->writeBufferSize = 8192;
    data->readBufferSize = 8192;
    data->hostName = "127.0.0.1";
    data->port = 0;
    data->maxPacketSize = 0;
    data->nextHandle = INITIAL_HANDLE_VALUE;
}

Network::~Network() {
    if (data->listener.get() != NULL) {
        data->listener->Stop();
        data->listener.reset();
    }

    std::map<uint64_t, boost::shared_ptr<ClientInfo> >::iterator
        it = data->clientInfoMap.begin(),
        itEnd = data->clientInfoMap.end();
    while (it != itEnd) {
        uint64_t n = (it++)->first;
        data->InnerDisconnect(n, true);
    }

 // _removedHandleList.Clear();

    delete data;
}

boost::optional<uint64_t> Network::GetNetworkID() const {
    return data->networkID;
}

void Network::SetNetworkID(boost::optional<uint64_t> n) {
    data->networkID = n;
}

uint64_t Network::GetKeepAliveSendInterval() const {
    return data->keepAliveSendInterval;
}

void Network::SetKeepAliveSendInterval(uint64_t n) {
    data->keepAliveSendInterval = n;
}

void Network::SetWriteBufferSize(int size) {
    data->writeBufferSize = size;
    for (std::map<uint64_t, boost::shared_ptr<ClientInfo> >::iterator
        it = data->clientInfoMap.begin(), itEnd = data->clientInfoMap.end(); it != itEnd; ++it) {
        it->second->Connection->SetSendBufferSize(size);
    }
}

int Network::GetWriteBufferSize() const {
    return data->writeBufferSize;
}

void Network::SetReadBufferSize(int size) {
    data->readBufferSize = size;
    for (std::map<uint64_t, boost::shared_ptr<ClientInfo> >::iterator
        it = data->clientInfoMap.begin(), itEnd = data->clientInfoMap.end(); it != itEnd; ++it) {
        it->second->Connection->SetReceiveBufferSize(size);
    }
}

int Network::GetReadBufferSize() const {
    return data->readBufferSize;
}

/// <summary>
/// サーバーに接続します。
/// </summary>
/// <returns>接続が確立された場合は非0の接続ハンドル</returns>
uint64_t Network::Connect() {
    uint64_t handle = 0;
    try {
        boost::shared_ptr<TcpClient> client(new TcpClient(data->hostName, data->port));
        if (!client->Connected()) {
            //  失敗した
            return 0;
        }
        //  timeoutを10secにセット
        client->SetSendTimeout(10000);
        client->SetReceiveTimeout(10000);
        client->SetSendBufferSize(data->writeBufferSize);
        client->SetReceiveBufferSize(data->readBufferSize);
        //  ハンドル取得
        handle = data->AddClient(client);

        //  NetworkIDを使用するなら送る
        if (data->networkID) {
            data->SendNetworkIDMessage(handle, *client);
        }
        return handle;
    } catch (...) {
        //  失敗したなら切断
        if (handle != 0) {
            data->InnerDisconnect(handle, false); //  通信エラーと思われるので通知は送らない
        }
        return 0;
    }
}

/// <summary>
/// クライアントに接続します。
/// </summary>
/// <returns>接続が確立された場合は非0の接続ハンドル</returns>
uint64_t Network::Listen() {
    if (data->listener.get() == NULL) {
        //  TcpListenerが作られていない場合は作成する
        data->listener.reset(new TcpListener(data->port));
        data->listener->Start();
    }

    //  TcpListenerに接続要求が来ていないかチェック
    if (!data->listener->Pending()) {
        //  来ていない
        return 0;
    }

    boost::shared_ptr<TcpClient> client = data->listener->AcceptTcpClient();
    //  timeoutを10secにセット
    client->SetSendTimeout(10000);
    client->SetReceiveTimeout(10000);
    client->SetSendBufferSize(data->writeBufferSize);
    client->SetReceiveBufferSize(data->readBufferSize);
    //  ハンドル取得
    uint64_t handle = data->AddClient(client);

    //  NetworkIDを使用するなら送る
    if (data->networkID) {
        try {
            data->SendNetworkIDMessage(handle, *client);
        } catch (...) {
            data->InnerDisconnect(handle, false); //  通信エラーと思われるので通知は送らない
            return 0;
        }
    }
    return handle;
}

void Network::Disconnect(uint64_t handle) {
    data->InnerDisconnect(handle, true);
}

/// <summary>
/// 指定された接続ハンドルに対応した接続からデータを受信します。
/// </summary>
/// <param name="handle">接続ハンドル</param>
/// <param name="data">受信データが書き込まれるバッファ</param>
/// <returns>
///  0 :正常完了
/// -1 :ハンドルが無効
/// -2 :受信データの種別取得に失敗
/// -3 :受信データのサイズ取得に失敗
/// -4 :受信データのサイズが最大パケット長を超えている
/// -5 :実際に受信されたデータのサイズがおかしい
/// -6 :相手から切断された
/// -7 :ネットワークIDが違うため切断した
/// -99:通信エラー
/// </returns>
/// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
int Network::InnerRead(uint64_t handle, RawMemory& memory) {
    TcpClient* client = data->GetTcpClient(handle);
    if (client == NULL) {
        return -1;
    }
    boost::shared_ptr<ClientInfo> info = data->clientInfoMap[handle];
    try {
        if (!client->DataAvailable()) {
            //  受信データが無いときに活性チェック（＝自身の生存通知）を行なう
            if (data->keepAliveSendInterval > 0 && ++info->ActivityCheckCounter >= data->keepAliveSendInterval) {
                info->ActivityCheckCounter = 0;
                try {
                    data->InnerWrite(*client, PacketType::KeepAliveNotify, NULL, 0, false);
                    data->InnerFlush(*client);
                } catch (...) {
                    //  通信障害と思われる
                    data->InnerDisconnect(handle, false);
                    return -6;
                }
            }
            return 0;
        }

        uint8_t buf[4];

        //  データ種別の取得
        if (client->Read(buf, 0, 4) != 4) {
            Disconnect(handle);
            return -2;
        }
        int type = MemoryCopy::GetInt(buf, 0);

        //  データサイズの取得
        if (client->Read(buf, 0, 4) != 4) {
            Disconnect(handle);
            return -3;
        }
        int size = MemoryCopy::GetInt(buf, 0);

        //  最大パケット長を超えていないかチェック
        if (data->maxPacketSize > 0 && size > data->maxPacketSize) {
            Disconnect(handle);
            return -4;
        }

        //  データの取得
        int originalSize = 0;
        RawMemory buffer;
        if (size > 0) {
            //  展開後データサイズの取得
            if (client->Read(buf, 0, 4) != 4) {
                Disconnect(handle);
                return -3;
            }
            originalSize = MemoryCopy::GetInt(buf, 0);

            //  圧縮データの取得
            buffer.Resize(size);
            int remain = size;
            int readed = 0;
            while (remain != 0) {
                int len = client->Read(buffer.Get(), readed, remain);
                readed += len;
                remain -= len;
                if (len == 0) { break; }
            }
            if (readed != size) {
                //  中途半端にしか読めていない
                Disconnect(handle); //  通信エラーの可能性もあるので通知を送らない方がよいのかどうかは難しいところ。
                return -5;
            }
        }

        //  データ種別に応じたディスパッチ
        switch (type) {
        case PacketType::KeepAliveNotify:
            //  生存通知
            return 0;
        case PacketType::DisconnectNotify:
            //  切断通知
            data->InnerDisconnect(handle, false); //  相手はもういないのだから通知する必要はない
            return -6;
        case PacketType::NormalMessage:
            //  通常データ
            if (data->networkID) {
                //  認証済みでないならデータを捨てて切断
                if (!data->clientInfoMap[handle]->IsReceiveNetworkID) {
                    //  ネットワークIDが違うので切断する
                    Disconnect(handle);
                    return -7;
                }
            }
            if (size > 0 && originalSize != size) {
                //  展開が必要
                data->Inflate(memory, buffer.Get(), buffer.GetSize());
            } else {
                //  圧縮されていない
                memory.CopyFrom(buffer);
            }
            return 0;
        case PacketType::NetworkIDNotify:
            if (data->networkID) {
                uint64_t senderNetworkID = MemoryCopy::GetLong(buffer.Get(), 0);
                if (*data->networkID == senderNetworkID) {
                    //  認証済みとしてマーク
                    data->clientInfoMap[handle]->IsReceiveNetworkID = true;
                } else {
                    //  ネットワークIDが違うので切断する
                    Disconnect(handle);
                    return -7;
                }
            }
            return 0;
        }
    } catch (...) {
        data->InnerDisconnect(handle, false);
        return -99;
    }
    return 0;
}


/// <summary>
/// 指定された接続ハンドルに対応した接続にデータを送信します。
/// </summary>
/// <param name="handle">接続ハンドル</param>
/// <param name="data">送信データ</param>
/// <returns>
///  0:正常完了
/// -1:ハンドルが無効
/// -2:エラーが発生
/// </returns>
/// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
int Network::InnerWrite(uint64_t handle, const void* memory, size_t memoryLength) {
    TcpClient* client = data->GetTcpClient(handle);
    if (client == NULL) {
        return -1;
    }
    try {
        data->InnerWrite(*client, PacketType::NormalMessage, memory, memoryLength, true);
    } catch (...) {
        data->InnerDisconnect(handle, false);
        return -2;
    }
    return 0;
}

/// <summary>
/// 指定された接続ハンドルに対応した接続の送受信データをFlushします。
/// </summary>
/// <param name="handle">接続ハンドル</param>
/// <returns>
///  0:正常完了
/// -1:ハンドルが無効
/// -2:エラーが発生
/// </returns>
/// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
int Network::Flush(uint64_t handle) {
    TcpClient* client = data->GetTcpClient(handle);
    if (client == NULL) {
        return -1;
    }
    try {
        data->InnerFlush(*client);
    } catch (...) {
        data->InnerDisconnect(handle, false);
        return -2;
    }
    return 0;
}

/// <summary>
/// 指定された接続ハンドルに対応した接続のIPアドレスを取得します。
/// </summary>
/// <param name="handle">接続ハンドル</param>
/// <param name="ipAddress">IPアドレス</param>
/// <returns>
/// true :正常完了
/// false:エラーが発生
/// </returns>
/// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
bool Network::GetIpAddress(uint64_t handle, RawMemory& ipAddress) {
    TcpClient* client = data->GetTcpClient(handle);
    return client != NULL && client->GetIpAddress(ipAddress);
}

/// <summary>
/// Connectメソッドによって接続するホスト名とポート番号を設定します。
/// </summary>
/// <param name="hostname">ホスト名またはIPアドレス</param>
/// <param name="portnumber">ポート番号</param>
void Network::SetHostName(const char* hostname, int portnumber) {
    data->hostName = hostname == NULL ? std::string() : hostname;
    data->port = portnumber;
}

void Network::SetMaxPacketLength(int maxPacketLength) {
    data->maxPacketSize = maxPacketLength;
}

}} // namespace
