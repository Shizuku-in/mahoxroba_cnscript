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
 *  �l�b�g���[�N�̉��N���C�A���g(mock object)
 *
 *  �J�����ɂ́A1��̃}�V���ŃT�[�o�[�ƃN���C�A���g�𓮂�������
 *  1��̃}�V���ŕ����̃N���C�A���g�𓮂������肷��B���̂��߂�
 *  ���z�I�ɐڑ����邽�߂�mock��p�ӂ���B
 */
class NETWORKMODULE_API NetworkMock : public INetwork,  private boost::noncopyable {
    NetworkMockData* data;
public:

    NetworkMock();
    ~NetworkMock();

    /// �n���h���̐����̂��߂�incremental counter
    uint64_t GetNow();

    /// ����e�X�g�̂��߂ɁAConnect�����Ƃ��ɕʂ�NetworkMock��object��Listen�Őڑ����m���ł���悤��attach����B
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
