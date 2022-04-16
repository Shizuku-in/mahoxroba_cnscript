#ifndef __RAWMEMORY_H__
#define __RAWMEMORY_H__
#include <vector>
#include <boost/noncopyable.hpp>
#include "config.h"
#pragma warning(push)
#pragma warning(disable : 4275)
namespace Yanesdk { namespace Network {

/**
 * ���̃������B
 *
 * std::vector���g���Ă��܂��ƁADLL�̃o�C�i���̌݊����������ɂȂ��Ă��܂��̂ŁA
 * std::vector�̓w�b�_�ł̂ݎg���āA�\�[�X���ł͂��̃N���X��p����B
 */
class NETWORKMODULE_API RawMemory : private boost::noncopyable {
    uint8_t* memory;
    size_t size;
public:
    RawMemory();
    ~RawMemory();

    /// �������̃��T�C�Y
    void Resize(size_t n);

    /// �|�C���^�̎擾
    uint8_t* Get() const { return memory; }
    /// �T�C�Y�̎擾
    size_t GetSize() const { return size; }

    // �ȉ��͖����Ă��������ǂ���Ε֗���������Ȃ��w���p�[�Ƃ��B

    /// �ǉ�
    void Append(void* data, size_t dataLength) {
        size_t old = GetSize();
        Resize(old + dataLength);
        memcpy(Get() + old, data, dataLength);
    }

    /// �R�s�[
    void CopyFrom(const RawMemory& from) {
        Resize(0);
        Resize(from.GetSize());
        memcpy(Get(), from.Get(), from.GetSize());
    }
    /// �R�s�[
    void CopyFrom(const std::vector<uint8_t>& from) {
        Resize(0);
        if (0 < from.size()) {
            Resize(from.size());
            memcpy(Get(), &from[0], from.size());
        }
    }
    /// �C�e���[�^����̃R�s�[
    template<class IT> void CopyFrom(IT it, IT itEnd) {
        Resize(0);
        if (it != itEnd) {
            Resize(itEnd - it);
            //std::copy(it, itEnd, GetBegin());
            for (uint8_t* p = GetBegin(); it != itEnd; ++it) *p++ = *it;
        }
    }

    /// STL�p
    uint8_t* GetBegin() const { return Get(); }
    /// STL�p
    uint8_t* GetEnd() const { return Get() + GetSize(); }

    /// std::vector�ւ̕ϊ�
    std::vector<uint8_t> ToArray() const {
        // �R�s�[�Ȃ̂ő傫���Ȃ�Əd�����A
        // �ǂ����l�b�g���[�N�ő���M������x�Ȃ̂Łc�B
        return std::vector<uint8_t>(GetBegin(), GetEnd());
    }

    /// std::vector�ւ̃R�s�[
    void AssignTo(std::vector<uint8_t>& data) const {
        data.assign(GetBegin(), GetEnd());
    }
};

}} // namespace
#pragma warning(pop)
#endif // __RAWMEMORY_H__
