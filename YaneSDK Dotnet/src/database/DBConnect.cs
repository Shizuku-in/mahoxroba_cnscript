using System;
using System.Collections.Generic;
using System.Text;

using System.Data;
using System.Data.Odbc;

using Yanesdk.Ytl;

namespace Yanesdk.Database
{
	/// <summary>
	/// IDbXXX(ODBCも可)経由でデータベースにコネクトするクラス
	/// </summary>
	/// <remarks>
	/// これをさらに抽象化した、O/Rマッパーとして
	/// ObjectRecordMapperクラスがあるので、そちらも参考にすること。
	/// </remarks>
	public class DBConnect : IDisposable
	{
		/// <summary>
		/// DBに接続する。
		/// ODBCドライバに渡す"DSN="の直後の文字列(DSNとはDataSourceName)を渡してやること
		/// </summary>
		/// <param name="DBName"></param>
		public YanesdkResult Connect(string DBName)
		{
			Close();

			connection = CreateConnection("DSN=" + DBName);
			try
			{
				connection.Open();
			}
			catch
			{
				connection = null;
				return YanesdkResult.HappenSomeError;
			}
			return YanesdkResult.NoError;
		}


		/// <summary>
		/// ODBCドライバへのconnection(Connectで確立したもの)を切断する。
		/// </summary>
		public YanesdkResult Close()
		{
			if ( connection != null )
			{
				try
				{
					connection.Close();
				}
				catch
				{
					return YanesdkResult.HappenSomeError;
				}
				finally
				{
					connection.Dispose();
					connection = null;
				}
			}
			return YanesdkResult.NoError;
		}

		/// <summary>
		/// トランザクションを開始する
		/// </summary>
		/// <remarks>
		/// トランザクションに対応しているODBCドライバである必要がある
		/// </remarks>
		public YanesdkResult BeginTransaction()
		{
			if ( connection == null )
				return YanesdkResult.PreconditionError;

			try
			{
				transaction = connection.BeginTransaction();
			}
			catch
			{
				transaction = null;
				return YanesdkResult.HappenSomeError;
			}
			return YanesdkResult.NoError;
		}

		/// <summary>
		/// トランザクションを終了する
		/// </summary>
		/// <remarks>
		/// このときにDBへcommitされる。
		/// </remarks>
		/// <returns></returns>
		public YanesdkResult EndTransaction()
		{
			if ( transaction == null )
				return YanesdkResult.PreconditionError;
			try
			{
				transaction.Commit();
			}
			catch
			{
				return YanesdkResult.HappenSomeError;
			}
			finally
			{
				transaction = null;
			}
			return YanesdkResult.NoError;
		}

		/// <summary>
		/// オブジェクトを破棄する。破棄するときに、コネクションは解除する。
		/// </summary>
		public void Dispose()
		{
			EndTransaction();
			Close();
		}

		/// <summary>
		/// SQLのQueryを実行する。
		/// 
		/// SQLのUpdate文など、返りがintであるものを呼ぶときに使う。
		/// 
		/// transaction中であれば、EndTransactionまでcommitは保留される。
		/// </summary>
		/// <remarks>
		/// 失敗すればYanesdkDBException例外を投げる
		/// </remarks>
		/// <param name="queryString">クエリ文字列</param>
		/// <returns>クエリの結果影響を受けたrecord数</returns>
		public int ExecuteNonQuery(string queryString)
		{
			if ( connection == null )
				throw new YanesdkDBException();

			IDbCommand command = CreateCommand();
			command.Connection = connection;

			// トランザクション中であれば、それを設定する
			if ( transaction != null )
			{
				command.Transaction = transaction;
			}
			command.CommandText = queryString;
			try
			{
				return command.ExecuteNonQuery();
			}
			catch
			{
				throw new YanesdkDBException();
			}
		}

		/// <summary>
		/// SQLのQueryを実行する。
		/// </summary>
		/// <remarks>
		/// 失敗すればYanesdkDBException例外を投げる
		/// </remarks>
		/// <param name="queryString"></param>
		/// <param name="dataSet"></param>
		/// <returns></returns>
		public DataSet ExecuteQuery(string queryString)
		{
			if ( connection == null )
				throw new YanesdkDBException();

			IDbDataAdapter adapter =
				CreateDataAdapter(queryString , connection);

			DataSet dataSet;
			try
			{
				dataSet = new DataSet();
				adapter.Fill(dataSet);
			}
			catch
			{
				throw new YanesdkDBException();
			}
			return dataSet;
		}

		/// <summary>
		/// ODBCのコネクション
		/// </summary>
		private IDbConnection connection;

		/// <summary>
		/// ODBCのトランザクション
		/// </summary>
		/// <remarks>
		/// そのDBがトランザクションに対応しているとは限らないが(´ω`)
		/// </remarks>
		private IDbTransaction transaction;

		/// <summary>
		/// DBに合わせて、XXXXConnectionをnewして返す
		/// </summary>
		/// <param name="dbname"></param>
		/// <returns></returns>
		protected virtual IDbConnection CreateConnection(string dbname)
		{
			return null;
		}

		/// <summary>
		/// DBに合わせてXXXXCommandをnewして返す
		/// </summary>
		/// <returns></returns>
		protected virtual IDbCommand CreateCommand()
		{
			return null;
		}

		/// <summary>
		/// DBに合わせてXXXDataAdapterをnewして返す
		/// </summary>
		/// <param name="queryString"></param>
		/// <param name="connection"></param>
		/// <returns></returns>
		protected virtual IDbDataAdapter CreateDataAdapter(
			string queryString , IDbConnection connection)
		{
			return null;
		}

	}

	/// <summary>
	/// ODBC用のDBConnectクラス
	/// </summary>
	/// <remarks>
	/// また、ODBCドライバ経由でMySQLに接続するには、
	/// MySQL ODBCドライバをMySQL公式からdownloadしてきて、
	/// Windowsの管理ツール→ODBCのところから登録すること。
	/// ・MySQL ODBCドライバ
	///	http://dev.mysql.com/downloads/connector/odbc/3.51.html
	/// 
	/// MySQLはODBC経由だと少し遅いかも知れない。
	/// TableAdapterを使わないのであれば「Connector/net」を使ったほうがいいかも。
	///	</remarks>
	public class ODBCConnect : DBConnect
	{
		protected override IDbConnection CreateConnection(string dbname)
		{
			return new OdbcConnection(dbname);
		}

		protected override IDbCommand CreateCommand()
		{
			return new OdbcCommand();
		}

		protected override IDbDataAdapter CreateDataAdapter(
			string queryString , IDbConnection connection)
		{
			return new OdbcDataAdapter(queryString , connection as OdbcConnection);
		}
	}

}
