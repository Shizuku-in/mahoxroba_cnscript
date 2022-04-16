#ifndef __RAWMEMORY_H__
#define __RAWMEMORY_H__
#include <vector>
#include <boost/noncopyable.hpp>
#include "config.h"
#pragma warning(push)
#pragma warning(disable : 4275)
namespace Yanesdk { namespace Network {

/**
 * 生のメモリ。
 *
 * std::vectorを使ってしまうと、DLLのバイナリの互換性が微妙になってしまうので、
 * std::vectorはヘッダでのみ使って、ソース側ではこのクラスを用いる。
 */
class NETWORKMODULE_API RawMemory : private boost::noncopyable {
    uint8_t* memory;
    size_t size;
public:
    RawMemory();
    ~RawMemory();

    /// メモリのリサイズ
    void Resize(size_t n);

    /// ポインタの取得
    uint8_t* Get() const { return memory; }
    /// サイズの取得
    size_t GetSize() const { return size; }

    // 以下は無くてもいいけどあれば便利かもしれないヘルパーとか。

    /// 追加
    void Append(void* data, size_t dataLength) {
        size_t old = GetSize();
        Resize(old + dataLength);
        memcpy(Get() + old, data, dataLength);
    }

    /// コピー
    void CopyFrom(const RawMemory& from) {
        Resize(0);
        Resize(from.GetSize());
        memcpy(Get(), from.Get(), from.GetSize());
    }
    /// コピー
    void CopyFrom(const std::vector<uint8_t>& from) {
        Resize(0);
        if (0 < from.size()) {
            Resize(from.size());
            memcpy(Get(), &from[0], from.size());
        }
    }
    /// イテレータからのコピー
    template<class IT> void CopyFrom(IT it, IT itEnd) {
        Resize(0);
        if (it != itEnd) {
            Resize(itEnd - it);
            //std::copy(it, itEnd, GetBegin());
            for (uint8_t* p = GetBegin(); it != itEnd; ++it) *p++ = *it;
        }
    }

    /// STL用
    uint8_t* GetBegin() const { return Get(); }
    /// STL用
    uint8_t* GetEnd() const { return Get() + GetSize(); }

    /// std::vectorへの変換
    std::vector<uint8_t> ToArray() const {
        // コピーなので大きくなると重いが、
        // どうせネットワークで送受信する程度なので…。
        return std::vector<uint8_t>(GetBegin(), GetEnd());
    }

    /// std::vectorへのコピー
    void AssignTo(std::vector<uint8_t>& data) const {
        data.assign(GetBegin(), GetEnd());
    }
};

}} // namespace
#pragma warning(pop)
#endif // __RAWMEMORY_H__
