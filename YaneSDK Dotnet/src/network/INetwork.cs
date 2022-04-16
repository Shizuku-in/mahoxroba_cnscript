using System;
using System.Collections.Generic;
using System.Text;

using Yanesdk.Ytl;			// for MemoryCopy

namespace Yanesdk.Network
{
	/// <summary>
	/// ネットワーク接続のinterface
	/// </summary>
	public interface INetwork : IDisposable
	{
		/// <summary>
		/// VN_rootに接続を要求
		/// </summary>
		/// <remarks>
		/// 戻り値は、コネクションハンドル。
		/// 接続に失敗した場合は0。
		/// </remarks>
		long Connect();

		/// <summary>
		/// Client側からの接続をlisten(TCP/IPのlisten的な意味で)する。
		/// </summary>
		/// <remarks>
		/// サーバー側でしかこのメソッドは呼び出さない。
		/// 戻り値は、コネクションハンドル。
		/// 
		/// 確立したコネクションが気にいらないときは、
		/// Disconnectを呼び出して接続を切断することが出来る。
		/// 
		/// 接続を要求しているものがなければ0。
		/// </remarks>
		long Listen();

		/// <summary>
		/// Listenで確立してあった接続を切断する。
		/// 以後、そのhandleは無効となる。
		/// </summary>
		/// <param name="handle"></param>
		void Disconnect(long handle);

		/// <summary>
		/// 確立したコネクションに対してデータ送信
		/// </summary>
		/// <remarks>
		/// 接続が切断された場合は、非0が返る。
		/// 送信は1～2秒分、bufferingしてから送信される。
		/// 
		/// Writeしたあと、Flushを呼び出すと、即座にbufferingしている
		/// ぶんを送信する。
		/// 
		/// connection切断時に非0が返る。
		/// </remarks>
		int Write(long handle, byte[] data);

		/// <summary>
		/// write bufferをいますぐ送信する。
		/// </summary>
		/// <param name="handle"></param>
		/// <returns>接続が切断された場合は、非0が返る。</returns>
		int Flush(long handle);

		/// <summary>
		/// 確立したコネクションに対してデータの読み込み。
		/// </summary>
		/// <remarks>
		/// 読み込むべきデータが無いならば
		/// data = nullが返る。
		/// 
		/// connection切断時に非0が返る。
		/// </remarks>
		int Read(long handle, out byte[] data);

		/// <summary>
		///		接続先ホスト名とポートを指定します。
		/// </summary>
		/// <param name="hostname">ホスト名。受付側として利用する場合は無効</param>
		/// <param name="portnumber">ポート番号</param>
		void SetHostName(string hostname, int portnumber);
	
		/// <summary>
		///		IPアドレスを取得する
		/// </summary>
		/// <param name="handle">IPアドレスを取得するハンドル</param>
		/// <param name="ipAddress">IPアドレス</param>
		/// <returns>true:成功, false:失敗</returns>
		bool GetIpAddress( long handle, out byte[] ipAddress );

		/// <summary>
		///		最大パケット長制限を設ける
		/// </summary>
		/// <param name="maxPacketLength">値>0なら制限値、値<=0なら制限しない</param>
		void SetMaxPacketLength(int maxPacketLength);

	}

	/// <summary>
	/// ネットワークの仮クライアント(mock object)
	///	開発時には、1台のマシンでサーバーとクライアントを動かしたり
	///	1台のマシンで複数のクライアントを動かしたりする。そのために
	/// 仮想的に接続するためのmockを用意する。
	/// </summary>
	public class NetworkMock : INetwork
	{
		public long Connect()
		{
			// attachしている奴に接続要求を出す
			long now = Now;
			this.neighbor.listenQueue.AddLast(new NetworkPacket(this, now));
			return now;
		}

		public long Listen()
		{
			// 接続要求が来ているか？
			if (listenQueue.First == null) return 0;

			// 接続要求があるので受け入れる
			NetworkPacket packet = listenQueue.First.Value;
			long now = Now;
			dic.Add(now,packet);

			NetworkPacket packet2 = new NetworkPacket(this, now);
			packet.Partner.dic.Add(packet.PartnerHandle, packet2);

			listenQueue.RemoveFirst(); // popしたので削除

			return now;
		}

		public void Disconnect(long handle)
		{
			// 相手側のhandleを除去
			if (dic.ContainsKey(handle) &&
				dic[handle].Partner.dic.ContainsKey(dic[handle].PartnerHandle))
			{
				dic[handle].Partner.dic.Remove(dic[handle].PartnerHandle);

				// 本当は相手のlisten queueからも除去しないといけない。
				// しかし、ここではそこまでエミュレーションする必要はないだろう。

				// 自分のhandleを除去
				dic.Remove(handle);
			}
				// デバッグ用だから、これでいいか…
				// すでにサーバー側でDisconnectが
				// 呼び出されたconnectionかも知れないし。
		}

		public int Write(long handle, byte[] data)
		{
			// 相手のdata queueに積む
			if (!dic.ContainsKey(handle))
				return 1;

			//	deep copyしておかないと送信元で書き換えた場合うまく動かない。
			byte[] data2 = new byte[data.Length];
			MemoryCopy.MemCopy(data2,0,data,0,data.Length);

			dic[handle].Partner.dic[dic[handle].PartnerHandle].Data.AddLast(data2);
			return 0;
		}

		public int Flush(long handle)
		{
			return 0;
		}

		public int Read(long handle, out byte[] data)
		{
			// dataはFIFO
			if (!dic.ContainsKey(handle)){
				data = null;
				return 1;
			}
			if (dic[handle].Data.Count == 0)
			{
				data = null;
				return 0;
			}
			data = dic[handle].Data.First.Value;
			dic[handle].Data.RemoveFirst(); // ひとつpopしたので先頭からひとつ消す

			/*
			// ログをとってデバッグの助けにするだとか。
			Console.WriteLine("Network.Read : handle = "+handle.ToString() + " ,DataLength = " + data.Length );
			*/

			return 0;
		}

		/// <summary>
		///		接続先ホスト名とポートを指定します。
		/// </summary>
		/// <param name="hostname">ホスト名。受付側として利用する場合は無効</param>
		/// <param name="portnumber">ポート番号</param>
		public void SetHostName(string hostname, int portnumber) { }

		/// <summary>
		/// 動作テストのために、Connectしたときに別のNetworkMockのobjectがListenで
		/// 接続を確立できるようにattachする。
		/// </summary>
		/// <param name="parent"></param>
		public void Attach(NetworkMock neighbor)
		{
			this.neighbor = neighbor;
		}
		private NetworkMock neighbor;

		/// <summary>
		/// Attachしている相手と、その相手側のハンドルを格納する構造体
		/// </summary>
		private class NetworkPacket
		{
			public NetworkMock Partner; // 接続相手
			public long	PartnerHandle;	// 接続相手側のhandle

			public NetworkPacket(NetworkMock partner, long partnerHandle)
			{
				this.Partner = partner;
				this.PartnerHandle = partnerHandle;
			}

			/// <summary>
			/// 相手から送られてきたデータのqueue
			/// </summary>
			public LinkedList<byte[]> Data = new LinkedList<byte[]>();
		}

		/// <summary>
		/// handleからNetworkTest(Attachしている相手と、その相手側のハンドルと送られてきたデータ)へのmap
		/// </summary>
		private Dictionary<long, NetworkPacket> dic = new Dictionary<long, NetworkPacket>();

		/// <summary>
		/// Listenするqueue。相手からの接続要求がここに入っている。
		/// 接続相手と接続相手側のhandleが入っている。
		/// </summary>
		private LinkedList<NetworkPacket> listenQueue = new LinkedList<NetworkPacket>();

		/// <summary>
		/// 切断するのを実装しちゃう。
		/// </summary>
		public void Dispose()
		{
		//	long[] keys = new long[dic.Count]; // スレッドセーフでねぇっすけども(´ω`)
		//	dic.Keys.CopyTo(keys,0);
		//	foreach (long handle in keys)
		//		Disconnect(handle);
		
			// ↑これだと二つずつ消えるからDisconnectで例外が飛んで気持ち悪い

			long[] keys = new long[dic.Count];
			while (dic.Count != 0)
			{
				dic.Keys.CopyTo(keys,0); // ひとつだけでいいんだけども(´ω`)
				Disconnect(keys[0]);
			}
			

		}

		/// <summary>
		/// ハンドルの生成のためのincremental counter
		/// </summary>
		public long Now
		{
			get { return ++now; }
		}
		private long now = 0;

		/// <summary>
		///		IPアドレスを取得する
		/// </summary>
		/// <param name="handle">IPアドレスを取得するハンドル</param>
		/// <param name="ipAddress">IPアドレス</param>
		/// <returns>true:成功, false:失敗</returns>
		public bool GetIpAddress(long handle, out byte[] ipAddress) 
		{
			ipAddress = new byte[4] { 127, 0, 0, 1 };
			return true;
		}

		/// <summary>
		///		最大パケット長制限を設ける
		/// </summary>
		/// <param name="maxPacketLength">値>0なら制限値、値<=0なら制限しない</param>
		public void SetMaxPacketLength(int maxPacketLength) { }
	}

	/*
		/// <summary>
		/// 上記のクラスのテスト用に用いる
		/// </summary>
		public static void Test1()
		{
			NetworkMock n1 = new NetworkMock();
			NetworkMock n2 = new NetworkMock();
			NetworkMock n3 = new NetworkMock();
			// nodeが2つあって、n1がserver,n2,n3がclientとする。

			// connectionする側からattachしておく。
			n2.Attach(n1);
			n3.Attach(n1);

			// -- 以下、実際のクラスと同じように動作するはずである。

			// n2にとってのn1-n2間のhandle
			long h_2to1 = n2.Connect();

			// n3にとってのn1-n3間のhandle
			long h_3to1 = n3.Connect();

			// n1にとってのn1-n2間のhandle
			long h_1to2 = n1.Listen();

			// n1にとってのn1-n3間のhandle
			long h_1to3 = n1.Listen();

			byte[] data1 = new byte[] { 1, 2, 3, 4 };
			byte[] data2 = new byte[] { 5, 4, 3, 2 };
			byte[] data3 = new byte[] { 2, 4, 6, 8 };
			byte[] data4 = new byte[] { 3, 5, 7, 9 };

			n2.Write(h_2to1, data1);
			n3.Write(h_3to1, data2);
			n2.Write(h_2to1, data3);
			n3.Write(h_3to1, data4);

			byte[] data;

			n1.Read(h_1to2, out data);
			n1.Read(h_1to2, out data);
			n1.Read(h_1to2, out data);

			n1.Read(h_1to3, out data);
			n1.Read(h_1to3, out data);
			n1.Read(h_1to3, out data);
		}
	 */
}
