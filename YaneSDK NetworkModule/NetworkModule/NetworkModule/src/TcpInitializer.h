#ifndef __TCPINITIALIZER_H__
#define __TCPINITIALIZER_H__
namespace Yanesdk { namespace Network {

class TcpInitializer {
    TcpInitializer();
public:
    ~TcpInitializer();

    static TcpInitializer& GetInstance();
    static void Initialize() { GetInstance(); }
};

}} // namespace
#endif // __TCPINITIALIZER_H__
