#include "stdafx.h"
#include <zlib.h>
#include "TcpClient.h"
#include "TcpListener.h"
#include "MemoryCopy.h"
#include <NetworkModule/Network.h>
namespace Yanesdk { namespace Network {

namespace PacketType {
    /// �p�P�b�g���
    enum PacketType {
        /// �����ʒm���b�Z�[�W
        KeepAliveNotify = 0,
        /// �ؒf�ʒm���b�Z�[�W
        DisconnectNotify = 1,
        /// �ʏ탁�b�Z�[�W
        NormalMessage = 2,
        /// �l�b�g���[�NID�ʒm���b�Z�[�W
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

/// Network�̃����o�ϐ���private�֐��ȂǁB
struct NetworkData {
    boost::optional<uint64_t> networkID;
    uint64_t keepAliveSendInterval;
    int writeBufferSize;
    int readBufferSize;
    std::string hostName;
    int port;
    int maxPacketSize;
    uint64_t nextHandle;
    /// handle�l��ClientInfo�I�u�W�F�N�g�̃}�b�v
    std::map<uint64_t, boost::shared_ptr<ClientInfo> > clientInfoMap;
    /// TCP�ڑ���ҋ@���郊�X�i�BListen���\�b�h�̌Ăяo���ŏ����������B
    std::auto_ptr<TcpListener> listener;

    /// <summary>
    /// �w�肳�ꂽ�ڑ��Ƀp�P�b�g��ʂ�t�����ăf�[�^�𑗐M���܂��B
    /// preferCompress == true�̎��A�f�[�^�����k���ăT�C�Y��
    /// ���̃T�C�Y��茸������ꍇ�͈��k�f�[�^�𑗐M���܂��B
    /// </summary>
    /// <param name="client">���M����TcpClient�I�u�W�F�N�g</param>
    /// <param name="type">���M�f�[�^�̎��</param>
    /// <param name="data">���M�f�[�^</param>
    /// <param name="preferCompress">�f�[�^�����k����ꍇ��true</param>
    void InnerWrite(TcpClient& client, PacketType::PacketType type, const void* data, size_t dataLength, bool preferCompress) {
        //  �p�P�b�g��ʂ���������
        std::vector<uint8_t> datatype = MemoryCopy::ToByteArrayFromInt((int)type);
        client.Write(&datatype[0], 0, (int)datatype.size());
        if (data != NULL) {
            //  ���M�f�[�^�̈��k
            RawMemory compressedData;
            if (preferCompress) {
                Deflate(compressedData, data, dataLength);
            }
            uint8_t datasize[8];
            if (compressedData.GetSize() != 0 && compressedData.GetSize() < dataLength) {
                //  ���k�f�[�^�̃T�C�Y�ƓW�J��̃T�C�Y����������
                MemoryCopy::SetInt(datasize, 0, (int)compressedData.GetSize());
                MemoryCopy::SetInt(datasize, 4, (int)dataLength);
                client.Write(datasize, 0, sizeof(datasize));
                //  ���k�f�[�^����������
                client.Write(compressedData.Get(), 0, (int)compressedData.GetSize());
            } else {
                //  ���k���Ă��T�C�Y���������Ȃ������̂Ō��̃f�[�^����������
                MemoryCopy::SetInt(datasize, 0, (int)dataLength);
                MemoryCopy::SetInt(datasize, 4, (int)dataLength);
                client.Write(datasize, 0, sizeof(datasize));
                client.Write(data, 0, (int)dataLength);
            }
        } else {
            //  ���M�f�[�^�̃T�C�Y���������ށi�l��0�j
            std::vector<uint8_t> datasize = MemoryCopy::ToByteArrayFromInt(0);
            client.Write(&datasize[0], 0, (int)datasize.size());
        }
    }

    /// <summary>
    /// Flush������s���܂��B
    /// </summary>
    void InnerFlush(TcpClient& client) {
        client.Flush();
    }

    /// <summary>
    /// �w�肳�ꂽ�ڑ��n���h���ɑΉ������ڑ���ؒf���܂��B
    /// sendNotifyMessage == false�̏ꍇ�A�ؒf�ʒm�͑���܂���B
    /// </summary>
    /// <param name="handle">�ڑ��n���h��</param>
    /// <param name="sendNotifyMessage">�ؒf�ʒm��ڑ���ɑ��M����ꍇ��true</param>
    void InnerDisconnect(uint64_t handle, bool sendNotifyMessage) {
        TcpClient* client = GetTcpClient(handle);
        if (client == NULL) return;

        if (sendNotifyMessage) {
            //  �ؒf���b�Z�[�W�𑗂�
            try {
                client->SetSendBufferSize(0); // �����M�����悤��
                InnerWrite(*client, PacketType::DisconnectNotify, NULL, 0, false);
                InnerFlush(*client);
                client->Close();
            } catch (...) {
            }
        }
        RemoveClient(handle);
    }

    /// <summary>
    /// �w�肳�ꂽTcpClient�I�u�W�F�N�g��TcpClient�}�b�v�ɒǉ����A�ڑ��n���h���𐶐����ĕԂ��܂��B
    /// </summary>
    /// <param name="client">�ڑ����m������Ă���TcpClient�I�u�W�F�N�g</param>
    /// <returns>�ڑ��n���h��</returns>
    uint64_t AddClient(const boost::shared_ptr<TcpClient>& client) {
        uint64_t handle = nextHandle++;
        clientInfoMap[handle].reset(new ClientInfo(client));
        return handle;
    }

    /// <summary>
    /// �w�肳�ꂽ�ڑ��n���h���ɑΉ�����TcpClient�I�u�W�F�N�g��TcpClient�}�b�v����폜���܂��B
    /// TcpClient�I�u�W�F�N�g�͑O�����ĕ��Ă����K�v������܂��B
    /// </summary>
    /// <param name="handle">�ڑ��n���h��</param>
    void RemoveClient(uint64_t handle) {
        CheckValidHandle(handle);
        clientInfoMap.erase(handle);
     // _removedHandleList.Add(handle);
    }

    /// <summary>
    /// �w�肳�ꂽ�ڑ��n���h�����L���Ȃ��̂����`�F�b�N���A�L���łȂ��ꍇ�͗�O�𔭐������܂��B
    /// </summary>
    /// <param name="handle">�ڑ��n���h��</param>
    /// <exception cref="ArgumentException">�w�肳�ꂽ�n���h���͎g�p����Ă��܂���</exception>
    void CheckValidHandle(uint64_t handle) {
        if (clientInfoMap.find(handle) ==  clientInfoMap.end()) {
            throw std::exception();
        }
    }

    /// <summary>
    /// �w�肳�ꂽ�ڑ��n���h���ɑΉ�����TcpClient�I�u�W�F�N�g���擾���܂��B
    /// </summary>
    /// <param name="handle">�ڑ��n���h��</param>
    /// <returns>�ڑ��n���h���ɑΉ�����TcpClient�I�u�W�F�N�g</returns>
    TcpClient* GetTcpClient(uint64_t handle) {
        std::map<uint64_t, boost::shared_ptr<ClientInfo> >::iterator it = clientInfoMap.find(handle);
        return it == clientInfoMap.end() ? NULL : it->second->Connection.get();
    }

    /// �f�[�^�����k���܂��B
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

    /// ���k���ꂽ�f�[�^���𓀂��܂��B
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
    /// NetworkID���b�Z�[�W�𑗐M���܂��B
    /// ���M�Ɏ��s�����ꍇ�͗�O���������܂��B
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
/// �T�[�o�[�ɐڑ����܂��B
/// </summary>
/// <returns>�ڑ����m�����ꂽ�ꍇ�͔�0�̐ڑ��n���h��</returns>
uint64_t Network::Connect() {
    uint64_t handle = 0;
    try {
        boost::shared_ptr<TcpClient> client(new TcpClient(data->hostName, data->port));
        if (!client->Connected()) {
            //  ���s����
            return 0;
        }
        //  timeout��10sec�ɃZ�b�g
        client->SetSendTimeout(10000);
        client->SetReceiveTimeout(10000);
        client->SetSendBufferSize(data->writeBufferSize);
        client->SetReceiveBufferSize(data->readBufferSize);
        //  �n���h���擾
        handle = data->AddClient(client);

        //  NetworkID���g�p����Ȃ瑗��
        if (data->networkID) {
            data->SendNetworkIDMessage(handle, *client);
        }
        return handle;
    } catch (...) {
        //  ���s�����Ȃ�ؒf
        if (handle != 0) {
            data->InnerDisconnect(handle, false); //  �ʐM�G���[�Ǝv����̂Œʒm�͑���Ȃ�
        }
        return 0;
    }
}

/// <summary>
/// �N���C�A���g�ɐڑ����܂��B
/// </summary>
/// <returns>�ڑ����m�����ꂽ�ꍇ�͔�0�̐ڑ��n���h��</returns>
uint64_t Network::Listen() {
    if (data->listener.get() == NULL) {
        //  TcpListener������Ă��Ȃ��ꍇ�͍쐬����
        data->listener.reset(new TcpListener(data->port));
        data->listener->Start();
    }

    //  TcpListener�ɐڑ��v�������Ă��Ȃ����`�F�b�N
    if (!data->listener->Pending()) {
        //  ���Ă��Ȃ�
        return 0;
    }

    boost::shared_ptr<TcpClient> client = data->listener->AcceptTcpClient();
    //  timeout��10sec�ɃZ�b�g
    client->SetSendTimeout(10000);
    client->SetReceiveTimeout(10000);
    client->SetSendBufferSize(data->writeBufferSize);
    client->SetReceiveBufferSize(data->readBufferSize);
    //  �n���h���擾
    uint64_t handle = data->AddClient(client);

    //  NetworkID���g�p����Ȃ瑗��
    if (data->networkID) {
        try {
            data->SendNetworkIDMessage(handle, *client);
        } catch (...) {
            data->InnerDisconnect(handle, false); //  �ʐM�G���[�Ǝv����̂Œʒm�͑���Ȃ�
            return 0;
        }
    }
    return handle;
}

void Network::Disconnect(uint64_t handle) {
    data->InnerDisconnect(handle, true);
}

/// <summary>
/// �w�肳�ꂽ�ڑ��n���h���ɑΉ������ڑ�����f�[�^����M���܂��B
/// </summary>
/// <param name="handle">�ڑ��n���h��</param>
/// <param name="data">��M�f�[�^���������܂��o�b�t�@</param>
/// <returns>
///  0 :���튮��
/// -1 :�n���h��������
/// -2 :��M�f�[�^�̎�ʎ擾�Ɏ��s
/// -3 :��M�f�[�^�̃T�C�Y�擾�Ɏ��s
/// -4 :��M�f�[�^�̃T�C�Y���ő�p�P�b�g���𒴂��Ă���
/// -5 :���ۂɎ�M���ꂽ�f�[�^�̃T�C�Y����������
/// -6 :���肩��ؒf���ꂽ
/// -7 :�l�b�g���[�NID���Ⴄ���ߐؒf����
/// -99:�ʐM�G���[
/// </returns>
/// <exception cref="ArgumentException">�w�肳�ꂽ�n���h���͎g�p����Ă��܂���</exception>
int Network::InnerRead(uint64_t handle, RawMemory& memory) {
    TcpClient* client = data->GetTcpClient(handle);
    if (client == NULL) {
        return -1;
    }
    boost::shared_ptr<ClientInfo> info = data->clientInfoMap[handle];
    try {
        if (!client->DataAvailable()) {
            //  ��M�f�[�^�������Ƃ��Ɋ����`�F�b�N�i�����g�̐����ʒm�j���s�Ȃ�
            if (data->keepAliveSendInterval > 0 && ++info->ActivityCheckCounter >= data->keepAliveSendInterval) {
                info->ActivityCheckCounter = 0;
                try {
                    data->InnerWrite(*client, PacketType::KeepAliveNotify, NULL, 0, false);
                    data->InnerFlush(*client);
                } catch (...) {
                    //  �ʐM��Q�Ǝv����
                    data->InnerDisconnect(handle, false);
                    return -6;
                }
            }
            return 0;
        }

        uint8_t buf[4];

        //  �f�[�^��ʂ̎擾
        if (client->Read(buf, 0, 4) != 4) {
            Disconnect(handle);
            return -2;
        }
        int type = MemoryCopy::GetInt(buf, 0);

        //  �f�[�^�T�C�Y�̎擾
        if (client->Read(buf, 0, 4) != 4) {
            Disconnect(handle);
            return -3;
        }
        int size = MemoryCopy::GetInt(buf, 0);

        //  �ő�p�P�b�g���𒴂��Ă��Ȃ����`�F�b�N
        if (data->maxPacketSize > 0 && size > data->maxPacketSize) {
            Disconnect(handle);
            return -4;
        }

        //  �f�[�^�̎擾
        int originalSize = 0;
        RawMemory buffer;
        if (size > 0) {
            //  �W�J��f�[�^�T�C�Y�̎擾
            if (client->Read(buf, 0, 4) != 4) {
                Disconnect(handle);
                return -3;
            }
            originalSize = MemoryCopy::GetInt(buf, 0);

            //  ���k�f�[�^�̎擾
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
                //  ���r���[�ɂ����ǂ߂Ă��Ȃ�
                Disconnect(handle); //  �ʐM�G���[�̉\��������̂Œʒm�𑗂�Ȃ������悢�̂��ǂ����͓���Ƃ���B
                return -5;
            }
        }

        //  �f�[�^��ʂɉ������f�B�X�p�b�`
        switch (type) {
        case PacketType::KeepAliveNotify:
            //  �����ʒm
            return 0;
        case PacketType::DisconnectNotify:
            //  �ؒf�ʒm
            data->InnerDisconnect(handle, false); //  ����͂������Ȃ��̂�����ʒm����K�v�͂Ȃ�
            return -6;
        case PacketType::NormalMessage:
            //  �ʏ�f�[�^
            if (data->networkID) {
                //  �F�؍ς݂łȂ��Ȃ�f�[�^���̂ĂĐؒf
                if (!data->clientInfoMap[handle]->IsReceiveNetworkID) {
                    //  �l�b�g���[�NID���Ⴄ�̂Őؒf����
                    Disconnect(handle);
                    return -7;
                }
            }
            if (size > 0 && originalSize != size) {
                //  �W�J���K�v
                data->Inflate(memory, buffer.Get(), buffer.GetSize());
            } else {
                //  ���k����Ă��Ȃ�
                memory.CopyFrom(buffer);
            }
            return 0;
        case PacketType::NetworkIDNotify:
            if (data->networkID) {
                uint64_t senderNetworkID = MemoryCopy::GetLong(buffer.Get(), 0);
                if (*data->networkID == senderNetworkID) {
                    //  �F�؍ς݂Ƃ��ă}�[�N
                    data->clientInfoMap[handle]->IsReceiveNetworkID = true;
                } else {
                    //  �l�b�g���[�NID���Ⴄ�̂Őؒf����
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
/// �w�肳�ꂽ�ڑ��n���h���ɑΉ������ڑ��Ƀf�[�^�𑗐M���܂��B
/// </summary>
/// <param name="handle">�ڑ��n���h��</param>
/// <param name="data">���M�f�[�^</param>
/// <returns>
///  0:���튮��
/// -1:�n���h��������
/// -2:�G���[������
/// </returns>
/// <exception cref="ArgumentException">�w�肳�ꂽ�n���h���͎g�p����Ă��܂���</exception>
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
/// �w�肳�ꂽ�ڑ��n���h���ɑΉ������ڑ��̑���M�f�[�^��Flush���܂��B
/// </summary>
/// <param name="handle">�ڑ��n���h��</param>
/// <returns>
///  0:���튮��
/// -1:�n���h��������
/// -2:�G���[������
/// </returns>
/// <exception cref="ArgumentException">�w�肳�ꂽ�n���h���͎g�p����Ă��܂���</exception>
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
/// �w�肳�ꂽ�ڑ��n���h���ɑΉ������ڑ���IP�A�h���X���擾���܂��B
/// </summary>
/// <param name="handle">�ڑ��n���h��</param>
/// <param name="ipAddress">IP�A�h���X</param>
/// <returns>
/// true :���튮��
/// false:�G���[������
/// </returns>
/// <exception cref="ArgumentException">�w�肳�ꂽ�n���h���͎g�p����Ă��܂���</exception>
bool Network::GetIpAddress(uint64_t handle, RawMemory& ipAddress) {
    TcpClient* client = data->GetTcpClient(handle);
    return client != NULL && client->GetIpAddress(ipAddress);
}

/// <summary>
/// Connect���\�b�h�ɂ���Đڑ�����z�X�g���ƃ|�[�g�ԍ���ݒ肵�܂��B
/// </summary>
/// <param name="hostname">�z�X�g���܂���IP�A�h���X</param>
/// <param name="portnumber">�|�[�g�ԍ�</param>
void Network::SetHostName(const char* hostname, int portnumber) {
    data->hostName = hostname == NULL ? std::string() : hostname;
    data->port = portnumber;
}

void Network::SetMaxPacketLength(int maxPacketLength) {
    data->maxPacketSize = maxPacketLength;
}

}} // namespace
