#ifndef __MEMORYCOPY_H__
#define __MEMORYCOPY_H__
#include <NetworkModule/config.h>
namespace Yanesdk { namespace Network {

class RawMemory;

class MemoryCopy : private boost::noncopyable {
    MemoryCopy();
    ~MemoryCopy();
public:

    static int GetInt(const uint8_t* a, int index) {
        return a[index] + ( a[index + 1] << 8 ) + ( a[index + 2] << 16 ) + ( a[index + 3] << 24 );
    }
    static uint64_t GetLong(const uint8_t* a, int index) {
        uint64_t u = (uint64_t)GetInt(a , index);
        u += (uint64_t)GetInt(a , index + 4) << 32;
        return u;
    }

    static void SetInt(uint8_t* a, int index, int n) {
        a[index + 0] = ( uint8_t ) ( n & 0xff );
        a[index + 1] = ( uint8_t ) ( ( n >> 8 ) & 0xff );
        a[index + 2] = ( uint8_t ) ( ( n >> 16 ) & 0xff );
        a[index + 3] = ( uint8_t ) ( ( n >> 24 ) & 0xff );
    }

    static std::vector<uint8_t> ToByteArrayFromInt(int i) {
        return ToByteHelper(i, 4);
    }
    static std::vector<uint8_t> ToByteArrayFromLong(uint64_t l) {
        return ToByteHelper(l, 8);
    }

    static std::vector<uint8_t> ToByteHelper(uint64_t l, int size) {
        std::vector<uint8_t> by(size);
        for (int i = 0; i < size; ++i) {
            by[i] = (uint8_t)(l & 0xff);
            l >>= 8;
        }
        return by;
    }
};

}} // namespace
#endif // __MEMORYCOPY_H__
