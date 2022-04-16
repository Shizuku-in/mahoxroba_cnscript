#include "stdafx.h"
#include <NetworkModule/RawMemory.h>
namespace Yanesdk { namespace Network {

using namespace std; // 念のため。

RawMemory::RawMemory() : memory(NULL), size(0) {
}

RawMemory::~RawMemory() {
    free(memory);
}

/// メモリのリサイズ
void RawMemory::Resize(size_t n) {
    memory = (uint8_t*)realloc(memory, n);
    size = n;
}

}} // namespace
