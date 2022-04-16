#include "stdafx.h"
#include <NetworkModule/RawMemory.h>
namespace Yanesdk { namespace Network {

using namespace std; // �O�̂��߁B

RawMemory::RawMemory() : memory(NULL), size(0) {
}

RawMemory::~RawMemory() {
    free(memory);
}

/// �������̃��T�C�Y
void RawMemory::Resize(size_t n) {
    memory = (uint8_t*)realloc(memory, n);
    size = n;
}

}} // namespace
