#ifndef __INETWORK_H__
#define __INETWORK_H__
#include "config.h"
#include "RawMemory.h"
namespace Yanesdk { namespace Network {

/// ネットワーク接続のinterface
class NETWORKMODULE_API INetwork {
public:
    virtual ~INetwork() {}
    
    /**
     *  VN_rootに接続を要求
     * 
     *  戻り値は、コネクションハンドル。
     *  接続に失敗した場合は0。
     */
    virtual uint64_t Connect() = 0;

    /**
     *  Client側からの接続をlisten(TCP/IPのlisten的な意味で)する。
     * 
     *  サーバー側でしかこのメソッドは呼び出さない。
     *  戻り値は、コネクションハンドル。
     *  
     *  確立したコネクションが気にいらないときは、
     *  Disconnectを呼び出して接続を切断することが出来る。
     *  
     *  接続を要求しているものがなければ0。
     */
    virtual uint64_t Listen() = 0;

    /**
     *  Listenで確立してあった接続を切断する。
     *  以後、そのhandleは無効となる。
     */
    virtual void Disconnect(uint64_t handle) = 0;

    /**
     *  確立したコネクションに対してデータ送信
     * 
     *  接続が切断された場合は、非0が返る。
     *  送信は1～2秒分、bufferingしてから送信される。
     *  
     *  Writeしたあと、Flushを呼び出すと、即座にbufferingしている
     *  ぶんを送信する。
     *  
     *  connection切断時に非0が返る。
     */
    int Write(uint64_t handle, const void* data, size_t dataLength) {
        return InnerWrite(handle, data, dataLength);
    }

    /// RawMemory版Write()
    int Write(uint64_t handle, const RawMemory& memory) {
        return InnerWrite(handle, memory.Get(), memory.GetSize());
    }

    /// std::vector版Write()
    int Write(uint64_t handle, const std::vector<uint8_t>& memory) {
        return InnerWrite(handle, 0 < memory.size() ? &memory[0] : NULL, memory.size());
    }

    /**
     *  write bufferをいますぐ送信する。
     * 
     *  @returns 接続が切断された場合は、非0が返る。
     */
    virtual int Flush(uint64_t handle) = 0;

    /**
     *  確立したコネクションに対してデータの読み込み。
     *  
     *  読み込むべきデータが無いならば
     *  data = nullが返る。
     *  
     *  connection切断時に非0が返る。
     */
    int Read(uint64_t handle, RawMemory& data) {
        return InnerRead(handle, data);
    }

    /// std::vector版Read()
    int Read(uint64_t handle, std::vector<uint8_t>& data) {
        RawMemory memory;
        int result = InnerRead(handle, memory);
        memory.AssignTo(data);
        return result;
    }

    /**
     *  接続先ホスト名とポートを指定します。
     * 
     *  @param hostname     ホスト名。受付側として利用する場合は無効
     *  @param portnumber   ポート番号
     */
    virtual void SetHostName(const char* hostname, int portnumber) = 0;

    /**
     *  IPアドレスを取得する
     *  
     *  @param handle       IPアドレスを取得するハンドル
     *  @param ipAddress    IPアドレス
     *  @returns            true:成功, false:失敗
     */
    virtual bool GetIpAddress(uint64_t handle, RawMemory& ipAddress) = 0;

    /// std::vector版GetIpAddress()
    int GetIpAddress(uint64_t handle, std::vector<uint8_t>& data) {
        RawMemory memory;
        bool result = GetIpAddress(handle, memory);
        memory.AssignTo(data);
        return result;
    }

    /**
     *  最大パケット長制限を設ける
     *  
     *  @param maxPacketLength  値>0なら制限値、値<=0なら制限しない
     */
    virtual void SetMaxPacketLength(int maxPacketLength) = 0;

protected:
    virtual int InnerRead(uint64_t handle, RawMemory& data) = 0;
    virtual int InnerWrite(uint64_t handle, const void* data, size_t dataLength) = 0;

};

}} // namespace
#endif // __INETWORK_H__
