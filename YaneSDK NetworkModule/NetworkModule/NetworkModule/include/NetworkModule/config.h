#ifndef __CONFIG_H__
#define __CONFIG_H__

// VC++用マクロ
#ifndef NETWORKMODULE_API
#  ifdef _MSC_VER
#    ifdef NETWORKMODULE_EXPORTS
#      define NETWORKMODULE_API __declspec(dllexport)
#    else
#      define NETWORKMODULE_API __declspec(dllimport)
#    endif
#  else
#    define NETWORKMODULE_API
#  endif
#endif

// 型の定義
#include <boost/cstdint.hpp>
namespace Yanesdk { namespace Network {
using boost::int8_t;
using boost::uint8_t;
using boost::int16_t;
using boost::uint16_t;
using boost::int32_t;
using boost::uint32_t;
using boost::int64_t;
using boost::uint64_t;
}} // namespace

#endif // __CONFIG_H__
