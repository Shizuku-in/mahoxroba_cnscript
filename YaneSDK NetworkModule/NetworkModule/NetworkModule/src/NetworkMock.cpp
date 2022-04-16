#include "stdafx.h"
#include <NetworkModule/NetworkMock.h>
namespace Yanesdk { namespace Network {

/// Attach���Ă��鑊��ƁA���̑��葤�̃n���h�����i�[����\����
struct NetworkPacket {
    NetworkMock& Partner; // �ڑ�����
    uint64_t PartnerHandle;    // �ڑ����葤��handle
    /// ���肩�瑗���Ă����f�[�^��queue
    std::list<std::vector<uint8_t> > Data;

    NetworkPacket(NetworkMock& partner, uint64_t partnerHandle) : Partner(partner) {
        PartnerHandle = partnerHandle;
    }
};

/// NetworkMock�̃����o�ϐ���private�֐��ȂǁB
struct NetworkMockData {
    uint64_t now;
    NetworkMock* neighbor;
    /// handle����NetworkTest(Attach���Ă��鑊��ƁA���̑��葤�̃n���h���Ƒ����Ă����f�[�^)�ւ�map
    std::map<uint64_t, boost::shared_ptr<NetworkPacket> > dic;
    /// Listen����queue�B���肩��̐ڑ��v���������ɓ����Ă���B�ڑ�����Ɛڑ����葤��handle�������Ă���B
    std::list<boost::shared_ptr<NetworkPacket> > listenQueue;
};


NetworkMock::NetworkMock() : data(new NetworkMockData()) {
    data->now = 0;
}

/// �ؒf����̂����������Ⴄ�B
NetworkMock::~NetworkMock() {
 // uint64_t[] keys = new uint64_t[data->dic.Count]; // �X���b�h�Z�[�t�ł˂��������ǂ�(�L��`)
 // data->dic.Keys.CopyTo(keys, 0);
 // foreach (uint64_t handle in keys)
 //     Disconnect(handle);

    // �����ꂾ�Ɠ�������邩��Disconnect�ŗ�O�����ŋC��������

    std::map<uint64_t, boost::shared_ptr<NetworkPacket> >::iterator
        it = data->dic.begin(), itEnd = data->dic.end();
    while (it != itEnd) {
        uint64_t n = (it++)->first;
        Disconnect(n);
    }

    delete data;
}

/// �n���h���̐����̂��߂�incremental counter
uint64_t NetworkMock::GetNow() {
    return ++data->now;
}

/// ����e�X�g�̂��߂ɁAConnect�����Ƃ��ɕʂ�NetworkMock��object��Listen�Őڑ����m���ł���悤��attach����B
void NetworkMock::Attach(NetworkMock& neighbor) {
    data->neighbor = &neighbor;
}

uint64_t NetworkMock::Connect() {
    // attach���Ă���z�ɐڑ��v�����o��
    uint64_t now = GetNow();
    data->neighbor->data->listenQueue.push_back(
        boost::shared_ptr<NetworkPacket>(new NetworkPacket(*this, now)));
    return now;
}

uint64_t NetworkMock::Listen() {
    // �ڑ��v�������Ă��邩�H
    if (data->listenQueue.front() == NULL) return 0;

    // �ڑ��v��������̂Ŏ󂯓����
    boost::shared_ptr<NetworkPacket> packet = data->listenQueue.front();
    uint64_t now = GetNow();
    data->dic.insert(std::make_pair(now, packet));

    boost::shared_ptr<NetworkPacket> packet2(new NetworkPacket(*this, now));
    packet->Partner.data->dic.insert(std::make_pair(packet->PartnerHandle, packet2));

    data->listenQueue.pop_front(); // pop�����̂ō폜

    return now;
}

void NetworkMock::Disconnect(uint64_t handle) {
    // ���葤��handle������
    std::map<uint64_t, boost::shared_ptr<NetworkPacket> >::iterator it = data->dic.find(handle);
    if (it != data->dic.end() &&
        it->second->Partner.data->dic.find(it->second->PartnerHandle) != it->second->Partner.data->dic.end()) {
        it->second->Partner.data->dic.erase(it->second->PartnerHandle);

        // �{���͑����listen queue������������Ȃ��Ƃ����Ȃ��B
        // �������A�����ł͂����܂ŃG�~�����[�V��������K�v�͂Ȃ����낤�B

        // ������handle������
        data->dic.erase(handle);
    }
        // �f�o�b�O�p������A����ł������c
        // ���łɃT�[�o�[����Disconnect��
        // �Ăяo���ꂽconnection�����m��Ȃ����B
}

int NetworkMock::InnerRead(uint64_t handle, RawMemory& memory) {
    // data��FIFO
    if (data->dic.find(handle) == data->dic.end()){
        memory.Resize(0);
        return 1;
    }
    if (data->dic[handle]->Data.size() == 0) {
        memory.Resize(0);
        return 0;
    }
    memory.CopyFrom(data->dic[handle]->Data.front());
    data->dic[handle]->Data.pop_front(); // �ЂƂ�pop�����̂Ő擪����ЂƂ���
    return 0;
}

int NetworkMock::InnerWrite(uint64_t handle, const void* memory, size_t dataLength) {
    // �����data queue�ɐς�
    if (data->dic.find(handle) == data->dic.end())
        return 1;

    // deep copy���Ă����Ȃ��Ƒ��M���ŏ����������ꍇ���܂������Ȃ��B
    std::vector<uint8_t> data2((uint8_t*)memory, (uint8_t*)memory + dataLength);

    data->dic[handle]->Partner.data->dic[data->dic[handle]->PartnerHandle]->Data.push_back(data2);
    return 0;
}

int NetworkMock::Flush(uint64_t handle) {
    return 0;
}

/// <summary>
/// IP�A�h���X���擾����
/// </summary>
/// <param name="handle">IP�A�h���X���擾����n���h��</param>
/// <param name="ipAddress">IP�A�h���X</param>
/// <returns>true:����, false:���s</returns>
bool NetworkMock::GetIpAddress(uint64_t handle, RawMemory& ipAddress) {
    uint8_t data[4] = { 127, 0, 0, 1 };
    ipAddress.Resize(0);
    ipAddress.Append(data, sizeof(data));
    return true;
}

/// <summary>
/// �ڑ���z�X�g���ƃ|�[�g���w�肵�܂��B
/// </summary>
/// <param name="hostname">�z�X�g���B��t���Ƃ��ė��p����ꍇ�͖���</param>
/// <param name="portnumber">�|�[�g�ԍ�</param>
void NetworkMock::SetHostName(const char* hostname, int portnumber) {
}

/// <summary>
/// �ő�p�P�b�g��������݂���
/// </summary>
/// <param name="maxPacketLength">�l>0�Ȃ琧���l�A�l<=0�Ȃ琧�����Ȃ�</param>
void NetworkMock::SetMaxPacketLength(int maxPacketLength) {
}

}} // namespace
