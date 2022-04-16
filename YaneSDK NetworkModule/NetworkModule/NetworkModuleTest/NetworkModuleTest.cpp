/*
 * デバッグに使用したプログラムです。
 * 走り書きでごちゃごちゃしてますが、参考までに。
 *
 * 引数なしで起動するとローカルにサーバーを立てて適当なテストを行います。
 * 複数PC間でのテスト用に、引数にlocalhostと入れるとサーバーモード、
 * それ以外を入れた場合、それをホスト名と見なしてクライアントモードになります。
 *
 */
#include "stdafx.h"
#if 1
#  include <boost/test/minimal.hpp>
#else
#  define BOOST_CHECK(exp) ((void)0)
   int main(int argc, char* argv[]) { return test_main(argc, argv); }
#endif
#include <NetworkModule/Network.h>
#include <NetworkModule/NetworkMock.h>
#include <ctype.h>

using namespace Yanesdk::Network;

bool IsMatch(const uint8_t* x, const std::vector<uint8_t>& y, size_t n) {
    return y.size() == n && memcmp(x, &y[0], n) == 0;
}

void TestINetwork(INetwork& n1, INetwork& n2, INetwork& n3) {
    // n2にとってのn1-n2間のhandle
    uint64_t h_2to1 = n2.Connect(); BOOST_CHECK(h_2to1 != 0);

    // n3にとってのn1-n3間のhandle
    uint64_t h_3to1 = n3.Connect(); BOOST_CHECK(h_3to1 != 0);

    // n1にとってのn1-n2間のhandle
    uint64_t h_1to2 = n1.Listen(); BOOST_CHECK(h_1to2 != 0);

    // n1にとってのn1-n3間のhandle
    uint64_t h_1to3 = n1.Listen(); BOOST_CHECK(h_1to3 != 0);

    std::vector<uint8_t> data;

    uint8_t ip[] = {127, 0, 0, 1};
    n1.GetIpAddress(h_1to2, data);  BOOST_CHECK(IsMatch(ip, data, sizeof(ip)));
    n1.GetIpAddress(h_1to3, data);  BOOST_CHECK(IsMatch(ip, data, sizeof(ip)));
    n2.GetIpAddress(h_2to1, data);  BOOST_CHECK(IsMatch(ip, data, sizeof(ip)));
    n3.GetIpAddress(h_3to1, data);  BOOST_CHECK(IsMatch(ip, data, sizeof(ip)));

    uint8_t data1[4] = { 1, 2, 3, 4 };
    uint8_t data2[33]; for (int i = 0; i < sizeof(data2); i++) data2[i] = (uint8_t)rand();
    uint8_t data3[3] = { 2, 4, 6 };
    uint8_t data4[5] = { 3, 5, 7, 9, 11 };

    n2.Write(h_2to1, data1, sizeof(data1));
    n3.Write(h_3to1, data2, sizeof(data2));
    n2.Write(h_2to1, data3, sizeof(data3));
    n3.Write(h_3to1, data4, sizeof(data4));

    n1.Read(h_1to2, data);  BOOST_CHECK(IsMatch(data1, data, sizeof(data1)));
    n1.Read(h_1to2, data);  BOOST_CHECK(IsMatch(data3, data, sizeof(data3)));
    n1.Read(h_1to2, data);  BOOST_CHECK(data.size() == 0);

    n1.Read(h_1to3, data);  BOOST_CHECK(IsMatch(data2, data, sizeof(data2)));
    n1.Read(h_1to3, data);  BOOST_CHECK(IsMatch(data4, data, sizeof(data4)));
    n1.Read(h_1to3, data);  BOOST_CHECK(data.size() == 0);
}

char* safe_gets(char* p, size_t n) {
    int c, i = 0;
    while (1) {
        c = getchar();
        if (c == '\n' || c == EOF) { // 改行または入力終端に到達
            if (0 < n) p[i] = '\0';
            break;
        } else {
            p[i++] = (char)c;
            if (n <= (size_t)i + 1) { // バッファ終端に到達
                if (0 < n) p[n - 1] = '\0';
                do c = getchar(); while (c != '\n' && c != EOF);
                break;
            }
        }
    }
    return p;
}

void OnExit() {
    puts("");
    puts("Press Enter to continue...");
    getchar();
}

int test_main(int argc, char* argv[]) {
    // exeを直に起動したときとかに終了時にすぐウィンドウ閉じないように。
    atexit(&OnExit);

    if (argc == 2) {
        if (strcmp(argv[1], "localhost") == 0) {
            // サーバーのテスト
            // usage: NetworkModuleText.exe localhost
            //
            puts("Starting Server...");

            Network server;
            server.SetHostName(NULL, 54321);

            std::vector<uint64_t> clients;
            while (true) {
                uint64_t n = server.Listen();
                if (0 < n) {
                    clients.push_back(n);
                }
                for (size_t i = 0, n = clients.size(); i < n; i++) {
                    uint64_t handle = clients[i];
                    std::vector<uint8_t> data;
                    // クライアントから受信
                    if (server.Read(handle, data) != 0) {
                        clients[i] = 0;
                        continue;
                    }
                    if (data.size() == 0) continue; // データ受信しなかったら次のクライアントへ
                    // デバッグ用に画面に表示したり
                    printf("Receive from client(%d): %s\n", (int)handle, (const char*)&data[0]);
                    for (size_t j = 0, n = data.size() - 1; j < n; j++) data[j] = (uint8_t)toupper(data[j]);
                        // ↑めんどいので２バイト文字考慮はナシ
                    printf("Send to client(%d): %s\n", (int)handle, (const char*)&data[0]);
                    // クライアントへ送信
                    if (server.Write(handle, data) != 0) {
                        clients[i] = 0;
                        continue;
                    }
                }
                // 切断されたクライアントのハンドルを削除
                for (std::vector<uint64_t>::iterator it = clients.begin(); it != clients.end(); ) {
                    if (*it == 0) {
                        it = clients.erase(it);
                    } else {
                        ++it;
                    }
                }
            }
        } else {
            // クライアントのテスト(先にサーバーを起動しておく)
            // usage: NetworkModuleText.exe ホスト名
            //
            Network client;
            client.SetHostName(argv[1], 54321);
            uint64_t handle = client.Connect();
            BOOST_CHECK(0 < handle);

            while (true) {
                // 標準入力から一行取得して送信
                printf("\nInput a line: ");
                char sz[0x40000];
                safe_gets(sz, sizeof(sz));
                printf("Send to server(%d): %s\n", (int)handle, sz);
                client.Write(handle, sz, strlen(sz) + 1);
                // サーバーから戻ってくるのをチェック
                std::vector<uint8_t> data;
                while (client.Read(handle, data) == 0) {
                    if (data.size() == 0) continue;

                    printf("Receive from server(%d): %s\n", (int)handle, (const char*)&data[0]);
                    BOOST_CHECK(data.size() == strlen(sz) + 1);
                    for (size_t i = 0, n = data.size() - 1; i < n; i++) {
                        BOOST_CHECK(data[i] == (uint8_t)toupper(sz[i]));
                    }
                    break;
                }
            }
        }
    } else {
        // ローカルでのテスト
        // usage: NetworkModuleText.exe
        //
        puts("testing NetworkMock...");
        {
            NetworkMock n1, n2, n3;
            // nodeが2つあって、n1がserver,n2,n3がclientとする。
            // connectionする側からattachしておく。
            n2.Attach(n1);
            n3.Attach(n1);
            TestINetwork(n1, n2, n3);
        }
        puts("testing Network...");
        {
            Network n1, n2, n3;
            n1.SetHostName(NULL, 45678);
            n2.SetHostName("localhost", 45678);
            n3.SetHostName("127.0.0.1", 45678);
            // 一度サーバー側がListenしないとConnect出来ない
            BOOST_CHECK(n1.Listen() == 0);
            TestINetwork(n1, n2, n3);
        }
        puts("testing Network(2)...");
        {
            Network server, client;
            server.SetHostName(NULL, 45678);
            client.SetHostName("localhost", 45678);
            BOOST_CHECK(server.Listen() == 0);
            uint64_t c = client.Connect();
            uint64_t s = server.Listen();
            BOOST_CHECK(0 < c);
            BOOST_CHECK(0 < s);

            client.SetReadBufferSize(16384 * 32);

            uint8_t data[16384 * 8]; for (int i = 0; i < sizeof(data); i++) data[i] = (uint8_t)rand();
            std::vector<uint8_t> vct;
            BOOST_CHECK(server.Write(s, data, sizeof(data) / 4) == 0);
            BOOST_CHECK(server.Write(s, data, sizeof(data) / 2) == 0);
            BOOST_CHECK(server.Write(s, data, sizeof(data)) == 0);
            BOOST_CHECK(server.Write(s, data, sizeof(data)) == 0);
            BOOST_CHECK(client.Read(c, vct) == 0);
            BOOST_CHECK(client.Read(c, vct) == 0);
            BOOST_CHECK(client.Read(c, vct) == 0);
            BOOST_CHECK(client.Read(c, vct) == 0);

            client.Disconnect(c);
            server.Disconnect(s);
            c = client.Connect();
            s = server.Listen();

            client.SetWriteBufferSize(16384 * 32);

            BOOST_CHECK(client.Write(c, data, sizeof(data) / 4) == 0);
            BOOST_CHECK(client.Write(c, data, sizeof(data) / 2) == 0);
            BOOST_CHECK(client.Write(c, data, sizeof(data)) == 0);
            BOOST_CHECK(client.Write(c, data, sizeof(data)) == 0);
            BOOST_CHECK(server.Read(s, vct) == 0);
            BOOST_CHECK(server.Read(s, vct) == 0);
            BOOST_CHECK(server.Read(s, vct) == 0);
            BOOST_CHECK(server.Read(s, vct) == 0);
        }
    }
    return 0;
}

