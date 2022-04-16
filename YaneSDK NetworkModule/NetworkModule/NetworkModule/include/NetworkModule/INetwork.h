#ifndef __INETWORK_H__
#define __INETWORK_H__
#include "config.h"
#include "RawMemory.h"
namespace Yanesdk { namespace Network {

/// �l�b�g���[�N�ڑ���interface
class NETWORKMODULE_API INetwork {
public:
    virtual ~INetwork() {}
    
    /**
     *  VN_root�ɐڑ���v��
     * 
     *  �߂�l�́A�R�l�N�V�����n���h���B
     *  �ڑ��Ɏ��s�����ꍇ��0�B
     */
    virtual uint64_t Connect() = 0;

    /**
     *  Client������̐ڑ���listen(TCP/IP��listen�I�ȈӖ���)����B
     * 
     *  �T�[�o�[���ł������̃��\�b�h�͌Ăяo���Ȃ��B
     *  �߂�l�́A�R�l�N�V�����n���h���B
     *  
     *  �m�������R�l�N�V�������C�ɂ���Ȃ��Ƃ��́A
     *  Disconnect���Ăяo���Đڑ���ؒf���邱�Ƃ��o����B
     *  
     *  �ڑ���v�����Ă�����̂��Ȃ����0�B
     */
    virtual uint64_t Listen() = 0;

    /**
     *  Listen�Ŋm�����Ă������ڑ���ؒf����B
     *  �Ȍ�A����handle�͖����ƂȂ�B
     */
    virtual void Disconnect(uint64_t handle) = 0;

    /**
     *  �m�������R�l�N�V�����ɑ΂��ăf�[�^���M
     * 
     *  �ڑ����ؒf���ꂽ�ꍇ�́A��0���Ԃ�B
     *  ���M��1�`2�b���Abuffering���Ă��瑗�M�����B
     *  
     *  Write�������ƁAFlush���Ăяo���ƁA������buffering���Ă���
     *  �Ԃ�𑗐M����B
     *  
     *  connection�ؒf���ɔ�0���Ԃ�B
     */
    int Write(uint64_t handle, const void* data, size_t dataLength) {
        return InnerWrite(handle, data, dataLength);
    }

    /// RawMemory��Write()
    int Write(uint64_t handle, const RawMemory& memory) {
        return InnerWrite(handle, memory.Get(), memory.GetSize());
    }

    /// std::vector��Write()
    int Write(uint64_t handle, const std::vector<uint8_t>& memory) {
        return InnerWrite(handle, 0 < memory.size() ? &memory[0] : NULL, memory.size());
    }

    /**
     *  write buffer�����܂������M����B
     * 
     *  @returns �ڑ����ؒf���ꂽ�ꍇ�́A��0���Ԃ�B
     */
    virtual int Flush(uint64_t handle) = 0;

    /**
     *  �m�������R�l�N�V�����ɑ΂��ăf�[�^�̓ǂݍ��݁B
     *  
     *  �ǂݍ��ނׂ��f�[�^�������Ȃ��
     *  data = null���Ԃ�B
     *  
     *  connection�ؒf���ɔ�0���Ԃ�B
     */
    int Read(uint64_t handle, RawMemory& data) {
        return InnerRead(handle, data);
    }

    /// std::vector��Read()
    int Read(uint64_t handle, std::vector<uint8_t>& data) {
        RawMemory memory;
        int result = InnerRead(handle, memory);
        memory.AssignTo(data);
        return result;
    }

    /**
     *  �ڑ���z�X�g���ƃ|�[�g���w�肵�܂��B
     * 
     *  @param hostname     �z�X�g���B��t���Ƃ��ė��p����ꍇ�͖���
     *  @param portnumber   �|�[�g�ԍ�
     */
    virtual void SetHostName(const char* hostname, int portnumber) = 0;

    /**
     *  IP�A�h���X���擾����
     *  
     *  @param handle       IP�A�h���X���擾����n���h��
     *  @param ipAddress    IP�A�h���X
     *  @returns            true:����, false:���s
     */
    virtual bool GetIpAddress(uint64_t handle, RawMemory& ipAddress) = 0;

    /// std::vector��GetIpAddress()
    int GetIpAddress(uint64_t handle, std::vector<uint8_t>& data) {
        RawMemory memory;
        bool result = GetIpAddress(handle, memory);
        memory.AssignTo(data);
        return result;
    }

    /**
     *  �ő�p�P�b�g��������݂���
     *  
     *  @param maxPacketLength  �l>0�Ȃ琧���l�A�l<=0�Ȃ琧�����Ȃ�
     */
    virtual void SetMaxPacketLength(int maxPacketLength) = 0;

protected:
    virtual int InnerRead(uint64_t handle, RawMemory& data) = 0;
    virtual int InnerWrite(uint64_t handle, const void* data, size_t dataLength) = 0;

};

}} // namespace
#endif // __INETWORK_H__
