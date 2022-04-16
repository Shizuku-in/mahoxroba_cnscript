#ifndef __NETWORK_H__
#define __NETWORK_H__
#include <boost/optional.hpp>
#include <boost/noncopyable.hpp>
#include "INetwork.h"
#pragma warning(push)
#pragma warning(disable : 4275)
namespace Yanesdk { namespace Network {

struct NetworkData;

/// TCP�őo�����ʐM���s���N���X
class NETWORKMODULE_API Network : public INetwork, private boost::noncopyable {
    NetworkData* data;
public:
    /// �����n���h���l
    static uint64_t INITIAL_HANDLE_VALUE;

    Network();
    ~Network();

    /**
     *  �l�b�g���[�NID���擾�E�ݒ肵�܂��B
     *  �l�b�g���[�NID���g�p���Ȃ��ꍇ��null�l�ł��B
     *
     *  �قȂ�l�b�g���[�NID�����C���X�^���X���m�̒ʐM�͂ł��܂���B
     *  �T�[�o����Listen���\�b�h���Ăяo���O�A�N���C�A���g����Connect���\�b�h���Ăяo���O��
     *  ���ꂼ��ݒ肵�Ă����K�v������܂��B
     */
    void SetNetworkID(boost::optional<uint64_t> n);
    boost::optional<uint64_t> GetNetworkID() const;

    /**
     *  �����Read���\�b�h�̌Ăяo���ŁAKeepAlive���b�Z�[�W��ڑ���ɑ��M���邩�ǂ������擾�E�ݒ肵�܂��B
     *
     *  �Ȃ��ARead�̌Ăяo���񐔂́A�ehandle���ƂɃJ�E���g���܂��B
     *
     *  �f�B�t�H���g�� 900�ŁA�b��30���Read���s�Ȃ��Ƃ��āA
     *  900 = 30��~30�b�ł���A30�b���Ƃɐؒf���m�̂��߂�KeepAlive���b�Z�[�W�𑗐M���܂��B
     *
     *  0���ݒ肳��Ă���Ƒ��M���܂���B
     *  ���M���Ȃ��ꍇ�A�ؒf�����m�ł��Ȃ��̂ŁA���Ȃ炸�ݒ肷��悤�ɂ��Ă��������B
     */
    void SetKeepAliveSendInterval(uint64_t n);
    uint64_t GetKeepAliveSendInterval() const;

    /**
     *  ���M�o�b�t�@�̃T�C�Y���w�肵�܂��B����ł�8192�o�C�g�ł��B
     * 
     *  ��M����Read���\�b�h������I�ɌĂяo����Ȃ�������A
     *  �Ăяo���̃C���^�[�o�����Â������肵�āA
     *  ��M���̎�M�o�b�t�@�Ɏc�����܂܂̃o�C�g����
     *  ReadBufferSize�̒l�𒴂���ƁA�o�b�t�@���ӂ�ƂȂ�
     *  ��M���Ƃ̐ڑ��͐ؒf����܂��B
     */
    void SetWriteBufferSize(int size);
    int GetWriteBufferSize() const;

    /**
     *  ��M�o�b�t�@�̃T�C�Y���w�肵�܂��B
     *  ����ł�8192�o�C�g�ł��B
     *
     *  Read���\�b�h������I�ɌĂяo����Ȃ�������A
     *  �Ăяo���̃C���^�[�o�����Â������肵�āA
     *  ��M�o�b�t�@�Ɏc�����܂܂̃o�C�g����
     *  ReadBufferSize�̒l�𒴂���ƁA�o�b�t�@���ӂ�ƂȂ�
     *  ���M���Ƃ̐ڑ��͐ؒf����܂��B
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
