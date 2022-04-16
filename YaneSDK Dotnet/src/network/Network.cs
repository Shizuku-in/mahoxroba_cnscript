using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using Yanesdk.Ytl;
using System.Net.Sockets;
using System.Net;
using System.IO.Compression;

namespace Yanesdk.Network
{
    /// <summary>
    /// TCPで双方向通信を行うクラスです。
    /// </summary>
    public class Network : INetwork
    {
        /// <summary>
        /// 初期ハンドル値
        /// </summary>
        public static long INITIAL_HANDLE_VALUE = 1;

        /// <summary>
        /// パケット種別
        /// </summary>
        private enum PacketType
        {
            /// <summary>
            /// 生存通知メッセージ
            /// </summary>
            KeepAliveNotify = 0,
            /// <summary>
            /// 切断通知メッセージ
            /// </summary>
            DisconnectNotify = 1,
            /// <summary>
            /// 通常メッセージ
            /// </summary>
            NormalMessage = 2,
            /// <summary>
            /// ネットワークID通知メッセージ
            /// </summary>
            NetworkIDNotify = 3,
        }

        /// <summary>
        /// ネットワークIDを取得・設定します。
        /// ネットワークIDを使用しない場合はnull値です。
        /// </summary>
        /// <remarks>
        /// 異なるネットワークIDを持つインスタンス同士の通信はできません。
        /// サーバ側はListenメソッドを呼び出す前、クライアント側はConnectメソッドを呼び出す前に
        /// それぞれ設定しておく必要があります。
        /// </remarks>
        public long? NetworkID
        {
            get { return _networkID; }
            set { _networkID = value; }
        }
        private long? _networkID;

        /// <summary>
        /// 何回のReadメソッドの呼び出しで、KeepAliveメッセージを接続先に
        /// 送信するかどうかを取得・設定します。
		/// 
		/// なお、Readの呼び出し回数は、各handleごとにカウントします。
		/// 
		/// ディフォルトは 900で、秒間30回のReadを行なうとして、
		/// 900 = 30回×30秒であり、30秒ごとに切断検知のためのKeepAliveメッセージを送信します。
		/// /// </summary>
		/// <remarks>
		/// 0が設定されていると送信しません。
		/// 送信しない場合、切断を検知できないので、かならず設定するようにしてください。
		/// </remarks>
        public long KeepAliveSendInterval
        {
            get { return _keepAliveSendInterval; }
            set { _keepAliveSendInterval = value; }
        }
        private long _keepAliveSendInterval = 900;

		/// <summary>
		/// localhostからの待ち受けのためにportをListenするときに
		/// WindowsXPなどのFireWallの警告ダイアログが出ないようにするための
		/// LoopBackの指定。(default : false)
		/// </summary>
		/// <remarks>
		/// これを指定すると外部からは接続できなくなるので注意！
		/// </remarks>
		public bool LoopBack
		{
			get { return loopBack; }
			set { loopBack = value; }
		}
		private bool loopBack = false;


        public Network()
        {
        }

        /// <summary>
        /// サーバーに接続します。
        /// </summary>
        /// <returns>接続が確立された場合は非0の接続ハンドル</returns>
        public long Connect()
        {
            long handle = 0;
            try
            {
                TcpClient client = new TcpClient(_hostName, _port);
                if ( !client.Connected )
                {
                    //  失敗した
                    return 0;
                }
                //  timeoutを10secにセット
                client.SendTimeout = 10000;
                client.ReceiveTimeout = 10000;
                //  ハンドル取得
                handle = AddClient(client);

                //  NetworkIDを使用するなら送る
                if ( _networkID.HasValue )
                {
                    SendNetworkIDMessage(handle, client);
                }
                return handle;
            }
            catch
            {
                //  失敗したなら切断
                if ( handle != 0 )
                {
                    InnerDisconnect(handle, false); //  通信エラーと思われるので通知は送らない
                }
                return 0;
            }
        }

        /// <summary>
        /// クライアントに接続します。
        /// </summary>
        /// <returns>接続が確立された場合は非0の接続ハンドル</returns>
        public long Listen()
        {
            if ( _listener == null )
            {
				//  TcpListenerが作られていない場合は作成する

				if (LoopBack)
					_listener = new TcpListener(IPAddress.Loopback, _port);
				else
					_listener = new TcpListener(IPAddress.Any, _port);

                _listener.Start();
            }

            //  TcpListenerに接続要求が来ていないかチェック
            if ( !_listener.Pending() )
            {
                //  来ていない
                return 0;
            }

            TcpClient client = _listener.AcceptTcpClient();
            //  timeoutを10secにセット
            client.SendTimeout = 10000;
            client.ReceiveTimeout = 10000;
            //  ハンドル取得
            long handle = AddClient(client);

            //  NetworkIDを使用するなら送る
            if ( _networkID.HasValue )
            {
                try
                {
                    SendNetworkIDMessage(handle, client);
                }
                catch
                {
                    InnerDisconnect(handle, false); //  通信エラーと思われるので通知は送らない
                    return 0;
                }
            }
            return handle;
        }

        /// <summary>
        /// 指定された接続ハンドルに対応した接続を切断します。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        public void Disconnect(long handle)
        {
            InnerDisconnect(handle, true);
        }

        /// <summary>
        /// 指定された接続ハンドルに対応した接続を切断します。
		/// sendNotifyMessage == falseの場合、切断通知は送りません。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        /// <param name="sendNotifyMessage">切断通知を接続先に送信する場合はtrue</param>
        private void InnerDisconnect(long handle, bool sendNotifyMessage)
        {
            TcpClient client = GetTcpClient(handle);
            if ( client == null )
            {
                return;
            }
            if ( sendNotifyMessage )
            {
                //  切断メッセージを送る
                try
                {
                    client.SendBufferSize = 0;  //  即送信されるように
                    InnerWrite(client, PacketType.DisconnectNotify, null, false);
                    InnerFlush(client);
                    client.GetStream().Close();
                    client.Close();
                }
                catch
                {
                }
            }
            RemoveClient(handle);
        }

        public void Dispose()
        {
            if ( _listener != null )
            {
                _listener.Stop();
                _listener = null;
            }

            //  foreach中にそのコレクションの内容を変更すると例外が発生するので、
            //  keyをコピーした配列に対してforeachする。
			long[] handles = new long[_clientInfoMap.Count];
			_clientInfoMap.Keys.CopyTo(handles, 0);
            foreach ( long handle in handles )
            {
                InnerDisconnect(handle, true);
            }

        //    _removedHandleList.Clear();
        }

        /// <summary>
        /// 指定された接続ハンドルに対応した接続からデータを受信します。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        /// <param name="data">受信データが書き込まれるバッファ</param>
        /// <returns>
        ///  0 :正常完了
        /// -1 :ハンドルが無効
        /// -2 :受信データの種別取得に失敗
        /// -3 :受信データのサイズ取得に失敗
        /// -4 :受信データのサイズが最大パケット長を超えている
        /// -5 :実際に受信されたデータのサイズがおかしい
        /// -6 :相手から切断された
        /// -7 :ネットワークIDが違うため切断した
        /// -99:通信エラー
        /// </returns>
        /// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
        public int Read(long handle, out byte[] data)
        {
            data = null;
            TcpClient client = GetTcpClient(handle);
            if ( client == null )
            {
                return -1;
            }
            ClientInfo info = _clientInfoMap[handle];
            try
            {
                NetworkStream stream = client.GetStream();
                if ( !stream.DataAvailable )
                {
                    //  受信データが無いときに活性チェック（＝自身の生存通知）を行なう
                    if ( KeepAliveSendInterval > 0 && ++info.ActivityCheckCounter >= KeepAliveSendInterval )
                    {
                        info.ActivityCheckCounter = 0;
                        try
                        {
                            InnerWrite(client, PacketType.KeepAliveNotify, null, false);
                            InnerFlush(client);
                        }
                        catch
                        {
                            //  通信障害と思われる
                            InnerDisconnect(handle, false);
                            return -6;
                        }
                    }
                    return 0;
                }

                byte[] buf = new byte[4];

                //  データ種別の取得
                if ( stream.Read(buf, 0, 4) != 4 )
                {
                    Disconnect(handle);
                    return -2;
                }
                int type = MemoryCopy.GetInt(buf, 0);

                //  データサイズの取得
                if ( stream.Read(buf, 0, 4) != 4 )
                {
                    Disconnect(handle);
                    return -3;
                }
                int size = MemoryCopy.GetInt(buf, 0);

                //  最大パケット長を超えていないかチェック
                if ( _maxPacketSize > 0 && size > _maxPacketSize )
                {
                    Disconnect(handle);
                    return -4;
                }

                //  データの取得
                int originalSize = 0;
                byte[] buffer = null;
                if ( size > 0 )
                {
                    //  展開後データサイズの取得
                    if ( stream.Read(buf, 0, 4) != 4 )
                    {
                        Disconnect(handle);
                        return -3;
                    }
                    originalSize = MemoryCopy.GetInt(buf, 0);

                    //  圧縮データの取得
                    buffer = new byte[size];
                    int remain = size;
                    int readed = 0;
                    while ( remain != 0 )
                    {
                        int len = stream.Read(buffer, readed, remain);
                        readed += len;
                        remain -= len;
                        if ( len == 0 ) { break; }
                    }
                    if ( readed != size )
                    {
                        //  中途半端にしか読めていない
                        Disconnect(handle); //  通信エラーの可能性もあるので通知を送らない方がよいのかどうかは難しいところ。
                        return -5;
                    }
                }

                //  データ種別に応じたディスパッチ
                switch ( type )
                {
                case (int)PacketType.KeepAliveNotify:
                    //  生存通知
                    return 0;
                case (int)PacketType.DisconnectNotify:
                    //  切断通知
                    InnerDisconnect(handle, false); //  相手はもういないのだから通知する必要はない
                    return -6;
                case (int)PacketType.NormalMessage:
                    //  通常データ
                    if ( NetworkID != null )
                    {
                        //  認証済みでないならデータを捨てて切断
                        if ( !_clientInfoMap[handle].IsReceiveNetworkID )
                        {
                            //  ネットワークIDが違うので切断する
                            Disconnect(handle);
                            return -7;
                        }
                    }
                    if ( size > 0 && originalSize != size )
                    {
                        //  展開が必要
                        byte[] tmp = new byte[originalSize];
                        Deflate(buffer, tmp);
                        data = tmp;
                    }
                    else
                    {
                        //  圧縮されていない
                        data = buffer;
                    }
                    return 0;
                case (int)PacketType.NetworkIDNotify:
                    if ( NetworkID != null )
                    {
                        long senderNetworkID = MemoryCopy.GetLong(buffer, 0);
                        if ( NetworkID == senderNetworkID )
                        {
                            //  認証済みとしてマーク
                            _clientInfoMap[handle].IsReceiveNetworkID = true;
                        }
                        else
                        {
                            //  ネットワークIDが違うので切断する
                            Disconnect(handle);
                            return -7;
                        }
                    }
                    return 0;
                }
            }
            catch
            {
                InnerDisconnect(handle, false);
                return -99;
            }
            return 0;
        }

        /// <summary>
        /// 指定された接続ハンドルに対応した接続にデータを送信します。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        /// <param name="data">送信データ</param>
        /// <returns>
        ///  0:正常完了
        /// -1:ハンドルが無効
        /// -2:エラーが発生
        /// </returns>
        /// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
        public int Write(long handle, byte[] data)
        {
            TcpClient client = GetTcpClient(handle);
            if ( client == null )
            {
                return -1;
            }
            try
            {
                InnerWrite(client, PacketType.NormalMessage, data, true);
            }
            catch
            {
                InnerDisconnect(handle, false);
                return -2;
            }
            return 0;
        }

        /// <summary>
        /// 指定された接続にパケット種別を付加してデータを送信します。
        /// preferCompress == trueの時、データを圧縮してサイズが
        /// 元のサイズより減少する場合は圧縮データを送信します。
        /// </summary>
        /// <param name="client">送信するTcpClientオブジェクト</param>
        /// <param name="type">送信データの種別</param>
        /// <param name="data">送信データ</param>
        /// <param name="preferCompress">データを圧縮する場合はtrue</param>
        private static void InnerWrite(TcpClient client, PacketType type, byte[] data, bool preferCompress)
        {
            NetworkStream stream = client.GetStream();
            //  パケット種別を書き込む
            byte[] datatype = MemoryCopy.ToByteArrayFromInt((int)type);
            stream.Write(datatype, 0, datatype.Length);
            if ( data != null )
            {
                //  送信データの圧縮
                byte[] compressedData = null;
                if ( preferCompress )
                {
                    using ( MemoryStream ms = new MemoryStream() )
                    using ( DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress) )
                    {
                        ds.Write(data, 0, data.Length);
                        ds.Close(); //  FlushでなくCloseでないといけないらしい。。
                        compressedData = ms.ToArray();
                    }
                }
                byte[] datasize = new byte[8];
                if ( compressedData != null && compressedData.Length < data.Length )
                {
                    //  圧縮データのサイズと展開後のサイズを書き込む
                    MemoryCopy.SetInt(datasize, 0, compressedData.Length);
                    MemoryCopy.SetInt(datasize, 4, data.Length);
                    stream.Write(datasize, 0, datasize.Length);
                    //  圧縮データを書き込む
                    stream.Write(compressedData, 0, compressedData.Length);
                }
                else
                {
                    //  圧縮してもサイズが減少しなかったので元のデータを書き込む
                    MemoryCopy.SetInt(datasize, 0, data.Length);
                    MemoryCopy.SetInt(datasize, 4, data.Length);
                    stream.Write(datasize, 0, datasize.Length);
                    stream.Write(data, 0, data.Length);
                }
            }
            else
            {
                //  送信データのサイズを書き込む（値は0）
                byte[] datasize = MemoryCopy.ToByteArrayFromInt(0);
                stream.Write(datasize, 0, datasize.Length);
            }
        }

        /// <summary>
        /// 指定された接続ハンドルに対応した接続の送受信データをFlushします。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        /// <returns>
        ///  0:正常完了
        /// -1:ハンドルが無効
        /// -2:エラーが発生
        /// </returns>
        /// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
        public int Flush(long handle)
        {
            TcpClient client = GetTcpClient(handle);
            if ( client == null )
            {
                return -1;
            }
            try
            {
                InnerFlush(client);
            }
            catch
            {
                InnerDisconnect(handle, false);
                return -2;
            }
            return 0;
        }

        /// <summary>
        /// Flush動作を行います。
        /// </summary>
        /// <param name="client"></param>
        private void InnerFlush(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            stream.Flush();
            //  ↑NetworkStream.Flushメソッドは実際には何もしない
        }

        /// <summary>
        /// 指定された接続ハンドルに対応した接続のIPアドレスを取得します。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        /// <param name="ipAddress">IPアドレス</param>
        /// <returns>
        /// true :正常完了
        /// false:エラーが発生
        /// </returns>
        /// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
        public bool GetIpAddress(long handle, out byte[] ipAddress)
        {
            TcpClient client = GetTcpClient(handle);
            try
            {
                IPEndPoint endpoint = (IPEndPoint)client.Client.RemoteEndPoint;
                ipAddress = endpoint.Address.GetAddressBytes();
            }
            catch
            {
                ipAddress = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Connectメソッドによって接続するホスト名とポート番号を設定します。
        /// </summary>
        /// <param name="hostname">ホスト名またはIPアドレス</param>
        /// <param name="portnumber">ポート番号</param>
        public void SetHostName(string hostname, int portnumber)
        {
            _hostName = hostname;
            _port = portnumber;
        }
        private string _hostName = "127.0.0.1";
        private int _port = 0;

        /// <summary>
        /// 受信する最大パケット長を設定します。
        /// </summary>
        /// <param name="maxPacketLength">0なら無制限、非0なら制限するサイズ</param>
        public void SetMaxPacketLength(int maxPacketLength)
        {
            _maxPacketSize = maxPacketLength;
        }
        private int _maxPacketSize = 0;

        /// <summary>
        /// 指定されたTcpClientオブジェクトをTcpClientマップに追加し、接続ハンドルを生成して返します。
        /// </summary>
        /// <param name="client">接続が確立されているTcpClientオブジェクト</param>
        /// <returns>接続ハンドル</returns>
        private long AddClient(TcpClient client)
        {

            long handle;
            /*
			if ( _removedHandleList.Count > 0 )
            {
                handle = _removedHandleList[0];
                _removedHandleList.RemoveAt(0);
            }
            else
            {
				handle = _clientInfoMap.Count + INITIAL_HANDLE_VALUE;
            }
            if ( _clientInfoMap.ContainsKey(handle) )
            {
                //  このクラスの実装が誤っていなければこの例外が発生することは無い
                throw new ApplicationException("使用中のハンドルを返そうとしました。");
            }
			 */
			handle = nextHandle++;

            _clientInfoMap[handle] = new ClientInfo(client);
            return handle;
        }

		long nextHandle = INITIAL_HANDLE_VALUE;

        /// <summary>
        /// 指定された接続ハンドルに対応したTcpClientオブジェクトをTcpClientマップから削除します。
        /// TcpClientオブジェクトは前もって閉じておく必要があります。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        private void RemoveClient(long handle)
        {
            CheckValidHandle(handle);
            _clientInfoMap.Remove(handle);
        //    _removedHandleList.Add(handle);
        }

        /// <summary>
        /// 指定された接続ハンドルが有効なものかをチェックし、有効でない場合は例外を発生させます。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        /// <exception cref="ArgumentException">指定されたハンドルは使用されていません</exception>
        private void CheckValidHandle(long handle)
        {
            if ( !_clientInfoMap.ContainsKey(handle) )
            {
                throw new ArgumentException("指定されたハンドルは使用されていません。");
            }
        }

        /// <summary>
        /// 指定された接続ハンドルに対応したTcpClientオブジェクトを取得します。
        /// </summary>
        /// <param name="handle">接続ハンドル</param>
        /// <returns>接続ハンドルに対応したTcpClientオブジェクト</returns>
        private TcpClient GetTcpClient(long handle)
        {
            if ( !_clientInfoMap.ContainsKey(handle) )
            {
                return null;
            }
            TcpClient client = _clientInfoMap[handle].Connection;
            return client;
        }

        /// <summary>
        /// DeflateStreamクラスによって圧縮されたデータを解凍します。
        /// </summary>
        /// <param name="compressedData">圧縮データ</param>
        /// <param name="originalData">解凍されたデータが書き込まれるバッファ</param>
        private void Deflate(byte[] compressedData, byte[] originalData)
        {
            using ( MemoryStream ms = new MemoryStream(compressedData, 0, compressedData.Length, false) )
            using ( DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress) )
            {
                int i = 0;
                int remain = originalData.Length;
                while ( remain != 0 )
                {
                    int len = ds.Read(originalData, i, remain);
                    i += len;
                    remain -= len;
                    if ( len == 0 ) { break; }
                }
            }
        }

        /// <summary>
        /// NetworkIDメッセージを送信します。
        /// 送信に失敗した場合は例外が発生します。
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="client"></param>
        private void SendNetworkIDMessage(long handle, TcpClient client)
        {
            InnerWrite(client, PacketType.NetworkIDNotify, MemoryCopy.ToByteArrayFromLong(_networkID.Value), false);
            InnerFlush(client);
        }


        private class ClientInfo
        {
            public TcpClient Connection;
            public bool IsReceiveNetworkID = false;
            public long ActivityCheckCounter = 0;
            public ClientInfo(TcpClient connection)
            {
                Connection = connection;
            }
        }

        /// <summary>
        /// TCP接続を待機するリスナ
        /// Listenメソッドの呼び出しで初期化される。
        /// </summary>
        private TcpListener _listener = null;
        /// <summary>
        /// handle値とClientInfoオブジェクトのマップ
        /// </summary>
        private Dictionary<long, ClientInfo> _clientInfoMap = new Dictionary<long, ClientInfo>();
        /// <summary>

		/*
		/// 破棄されたハンドル値のリスト
        /// 再利用のために使用される。
        /// </summary>
        private List<long> _removedHandleList = new List<long>();
		*/
    }
}
