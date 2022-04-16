using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;

using System.Windows.Forms;
// CreateTableIfNotExistでテーブルがないときにダイアログを出したい

using Yanesdk.Ytl;

namespace Yanesdk.Database
{
	/// <summary>
	/// SQLBuilderの投げる例外クラス
	/// </summary>
	public class YanesdkDBException : YanesdkException
	{
	}


	/// <summary>
	/// SQL文の生成ヘルパ。
	/// 簡単なO/Rマッパーも兼ねる。
	/// </summary>
	/// <remarks>
	/// CreateTableの自動化については、元のアイデアは、akirameiさんによる。
	///	</remarks>
	public interface SQLBuilder
	{
		/// <summary>
		/// SQLのCREATE TABLE文をreflectionを使って自動生成する。
		/// </summary>
		/// <code>
		///		ORMapper or = new MySQL_ORMapper();
		///		string createTable = or.CreateTableHelper(typeof(SampleRecord));
		///
		///		Console.WriteLine(createTable);
		/// </code>
		/// <param name="type"></param>
		/// <returns></returns>
		string CreateTable(Type type);

		/// <summary>
		/// SQLのDrop Table(テーブルの削除)をreflectionを使って自動生成する。
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		string DropTable(Type type);

		/// <summary>
		/// すべてのrecordを選択するSQL文を生成する。
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		string SelectAllFromTable(Type type);

		/// <summary>
		/// すべてのrecordを選択するSQL文を生成する。
		/// 
		/// where句を指定できるようになっている。
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		string SelectAllFromTableWhere(Type type , string whereString);

		/// <summary>
		/// 特定のfieldを選択するSQL文を生成する。
		/// 
		/// fieldName==nullならば、"*"が指定される。
		/// where句はwhereString==nullならば生成されない。
		/// </summary>
		/// <param name="type"></param>
		/// <param name="fieldName"></param>
		/// <param name="whereString"></param>
		/// <returns></returns>
		string SelectSomeFieldFromTableWhere(Type type , string fieldName , string whereString);

		/// <summary>
		/// objectをInsertするSQL文を構築する。
		/// </summary>
		/// <param name="rec"></param>
		/// <returns></returns>
		string Insert(object user_record);

		/// <summary>
		/// UPDATE等に出てくるSET X = Y や WHERE X = Y
		/// のようにX = Y の部分で使う文字列を生成する。
		/// フィールド名XをこのメソッドのfiledNameで指定すると、
		/// =Yの部分をuser_recordから自動的に生成してくれる。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="user_record"></param>
		/// <param name="filedName"></param>
		/// <returns></returns>
		string FieldEqual(object user_record , string filedName);

		/// <summary>
		/// テーブル名を取得する
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		string GetTableName(Type type);

		/// <summary>
		/// Update文を生成する。
		/// 
		/// setStatement == nullならSET句は生成しない。
		/// wherePhrase == nullならWHERE句は生成しない
		/// </summary>
		/// <param name="type"></param>
		/// <param name="setStatement"></param>
		/// <param name="whereStatement"></param>
		/// <returns></returns>
		string Update(Type type , string setPhrase , string wherePhrase);

		/// <summary>
		/// Select * を行なったときに戻ってくるDataSetを型変換を行なって
		/// Listに放り込む。いわゆるO/Rマッピング。
		/// </summary>
		/// <remarks>
		/// エラーの場合は、YanesdkSQLBuilderException例外を投げる。
		/// 
		/// 文字のコードセットはsjisで渡されるようなので、
		/// DB接続直後に
		///	db.ExecuteNonQuery("set names sjis;");
		///	を行なわないといけない。
		///	これを怠るとDBのchar型に格納できる文字数が半分になってしまう。
		///	(defaultでlatin1になっているため)
		/*
		MySQL側でcodepageの設定をするべきか？
		例)たとえばmy.iniを以下の内容(932はshift-jis)してサービスを再起動。 
			[client] 
			efault-character-set=cp932 
			[mysql] 
			default-character-set=cp932 
			[mysqld] 
			default-character-set=cp932 
			skip-character-set-client-handshake 
		 */
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="dataSet"></param>
		/// <returns></returns>
		List<T> Convert<T>(DataSet dataSet) where T : new();

		/// <summary>
		/// 特定のfieldのみをO/Rしてくる。(上記のConvertの特殊版)
		/// </summary>
		/// <remarks>
		/// Select id,data from DataTable where ...;
		/// 　　　　↑ここで指定している"id,data"をこのメソッドのselectFieldとして渡してやる。
		/// </remarks>
		/// <remarks>
		/// エラーの場合は、YanesdkSQLBuilderException例外を投げる。
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="dataSet"></param>
		/// <param name="selectField"></param>
		/// <param name="list"></param>
		/// <returns></returns>
		List<T> Convert<T>(DataSet dataSet , string selectField) where T : new();

		// MessageBoxの表示をしているので、Linux環境では動かないと思われ。
		// まあ、このメソッドの実装はサンプルコード程度の意味しかないのでなくてもいいかぁ…

		/// <summary>
		/// "__TableVersion"で示されるテーブル上に、
		/// createdTable(←これはDBTableAttributeを持つ)のバージョン記録(integer)がなければ
		/// 新規にtypeで示されるテーブルをCreateTableにて作成する。
		/// また、"__TableVersion" にそのテーブルのバージョンをINSERT(←SQL)する。
		/// 
		/// もし、createdTableのテーブルに関するバージョン記録があればcreatedTableの
		/// DBTableAttributeのversionプロパティの値を
		/// 比較し、もし古ければ、そのcreatedTableのテーブルをDROP(←SQL)して再度作成する。
		/// (そのときに、テーブルをdropして作り直すことを警告するダイアログを出す)
		/// 
		/// "__TableVersion"自体が無い場合は、まず、そのテーブルを作成する。
		/// </summary>
		/// <remarks>
		/// DBまわりでのエラー発生時には例外を投げる。
		/// </remarks>
		/// <param name="db"></param>
		/// <param name="type"></param>
		/// <param name="version_table"></param>
		void CreateTableIfNotExist(DBConnect db, Type createdTable);
	}


	/// <summary>
	/// MySQL用のORMapperの実装。
	/// 
	/// 各メソッドの説明は、SQLBuilder interfaceのほうにあるので
	/// そちらを参考にどうぞ。
	/// </summary>
	public class MySQL_SQLBuilder : SQLBuilder
	{

		// CreateTableでmemberとpropertyに対して同じオペレーションを行なわなければならないので
		// その共通部分を抜き出した。
		private void CreateTableHelper(MemberInfo info , Type type ,
			StringBuilder sb , StringBuilder primaryKeys , StringBuilder uniqueKeys)
		{
			DBColumnAttribute att2 =
				info.GetCustomAttributes(typeof(DBColumnAttribute) , false)[0] as DBColumnAttribute;

			if ( att2 == null )
				return ;

			string t;
			int size = att2.Size;

			// Typeに対してswitch文が使えないの、おかしくね？(´ω`)
			if ( type == typeof(int) )
			{
				if ( size == 0 )
					size = 11; // おまじない
				t = String.Format("INT({0})" , size);
			}
			else if ( type == typeof(string) )
			{
				if ( size <= ( 1 << 8 ) - 1 )
				{
					if ( att2.VarChar )
						t = String.Format("VARCHAR({0})" , size);
					else
						t = String.Format("CHAR({0})" , size);
				}
				else if ( size <= ( 1 << 16 ) - 1 )
					t = "TEXT";
				else
					t = "LONGTEXT";
			}
			else if ( type == typeof(DateTime) )
			{
				//	t = "DATE";
				t = "DATETIME";
			}
			else if ( type == typeof(byte[]) )
			{
				if ( size <= ( 1 << 8 ) - 1 )
					t = "TINYBLOB";
				else if ( size <= ( 1 << 16 ) - 1 )
					t = "BLOB";
				else if ( size <= ( 1 << 24 ) - 1 )
					t = "MEDIUMBLOB";
				else
					t = "LONGBLOB";
			}
			else if ( type == typeof(bool) )
			{
				t = "TINYINT(1)";
			}
			else if ( type == typeof(float) )
			{
				t = "FLOAT";
			}
			else if ( type == typeof(double) )
			{
				t = "DOUBLE";
			}
			else
			{
				return ;
			}

			// フィールド名は、名前がDBColumnAttributeで指定されていればそれを用いる。
			// さもなくば、メンバ名をそのまま用いる
			string fieldName = att2.Name != null ? att2.Name : info.Name;
			fieldName = '`' + fieldName.ToUpper() + '`';

			// NULL制約を示す文字列
			string isNullString = att2.Null ? "NULL" : "NOT NULL";

			// ディフォルト値について定義があれば、それを代入
			string defaultString = att2.Default != null ?
				"DEFAULT " + att2.Default + " " : "";

			// オプションの指定があれば、それを入れる
			string optionString = att2.Option != null ?
				" " + att2.Option : "";

			// AutoIncrementの指定があるか？
			if ( att2.AutoIncrement )
				optionString += " AUTO_INCREMENT ";

			sb.AppendFormat("{0}{1}{2}{3}{4},\n" ,
				fieldName.PadRight(12) , t.PadRight(16) ,
				defaultString ,
				isNullString ,
				optionString
			);

			// プライマリキーに指定されているならば、それを追加。
			if ( att2.PrimaryKey )
			{
				if ( primaryKeys.Length != 0 )
					primaryKeys.Append(",");
				primaryKeys.Append(fieldName);
			}
			// ユニークキーに指定されているならば、それを追加。
			if ( att2.UniqueKey )
				uniqueKeys.AppendFormat("UNIQUE({0}),\n" , fieldName);
		}

		public string SelectSomeFieldFromTableWhere(Type type , string fieldName , string whereString)
		{
			// テーブル名
			string tableName = GetTableName(type);

			// 生成するSQL文
			StringBuilder sb = new StringBuilder();

			if (fieldName == null)
			{
				fieldName = "*"; // 無条件セレクト
			}

			sb.AppendFormat("SELECT {0} FROM {1}"
				, fieldName
				,tableName);

			if ( whereString != null )
				sb.AppendFormat(" WHERE {0}" , whereString);

			sb.Append(';');

			return sb.ToString();
		}


		public string CreateTable(Type type)
		{
			// テーブル名
			string tableName = GetTableName(type);

			// 生成するSQL文
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("CREATE TABLE {0} (\n" , tableName);

			StringBuilder primaryKeys = new StringBuilder();
			StringBuilder uniqueKeys = new StringBuilder();

			// フィールドをそれぞれ調べる
			foreach ( FieldInfo info in type.GetFields() )
				CreateTableHelper(info , info.FieldType , sb , primaryKeys , uniqueKeys);

			// propertyもそれぞれ調べる
			foreach ( PropertyInfo info in type.GetProperties() )
				CreateTableHelper(info , info.PropertyType , sb , primaryKeys , uniqueKeys);

			// PRIMARY KEYとUNIQUE制約
			if ( primaryKeys.Length != 0 )
				sb.AppendFormat("PRIMARY KEY ({0}),\n" , primaryKeys);
			if ( uniqueKeys.Length != 0 )
				sb.Append(uniqueKeys);

			sb.Remove(sb.Length - 2 , 1); // 終端の','を削除
			sb.Append(")");

			// MySQL用の指定
			sb.Append("ENGINE=InnoDB DEFAULT CHARSET=UCS2");

			sb.Append(";");

			return sb.ToString();
		}

		public string DropTable(Type type)
		{
			// テーブル名
			string tableName = GetTableName(type);

			// 生成するSQL文
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("DROP TABLE {0};" , tableName);

			return sb.ToString();
		}

		public string SelectAllFromTable(Type type)
		{
			// テーブル名
			string tableName = GetTableName(type);

			// 生成するSQL文
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("SELECT * FROM {0};" , tableName);

			return sb.ToString();
		}

		public string SelectAllFromTableWhere(Type type , string whereString)
		{
			// テーブル名
			string tableName = GetTableName(type);

			// 生成するSQL文
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("SELECT * FROM {0}\nWHERE {1};" , tableName,whereString);

			return sb.ToString();
		}

		/// <summary>
		/// objectをSQLで使う文字列に変換する
		/// </summary>
		/// <param name="type"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
		public string ToSQLString(object obj)
		{
			string valueString;

			Type type = obj.GetType();

			// Typeに対してswitch文が使えないの、おかしくね？(´ω`)
			if ( type == typeof(int) )
			{
				valueString = ( ( int ) obj ).ToString();
			}
			else if ( type == typeof(string) )
			{
				string s = ( string ) obj;
				if ( s == null )
					valueString = "NULL";
				else
				{
					// SQL injection等の対策のためescapeしておく必要がある。
					s = s.Replace("\\","\\\\");
					s = s.Replace("'" , "\\'");
				//	s = s.Replace("\"" , "\\\""); // ←このescapeはいらんぽ？
				//	s = s.Replace("_" , "\\_"); // ←LIKE演算子用のescape
				//	s = s.Replace("%" , "\\%"); // ←LIKE演算子用のescape
					valueString = '\'' + s + '\'';
				}
			}
			else if ( type == typeof(DateTime) )
			{
				valueString = ( ( DateTime ) obj ).ToString("yyyy-MM-dd HH:mm:ss");
				valueString = '\'' + valueString + '\'';
				// 0001/01/01 0:00:00形式を
				// '0001-01-01 0:00:00'と変換する必要あり。
				// Replace('/' , '-')をするよか、Formatを指定したほうがよさげ..
			}
			else if ( type == typeof(byte[]) )
			{
				byte[] a = ( byte[] ) obj;
				if ( a == null )
					valueString = "NULL";
				else
					valueString = "0x" + ByteToString(a);
				// これcopyが2回発生するオーバーヘッドはどうなんよ…(´ω`)
			}
			else if ( type == typeof(bool) )
			{
				valueString = ( ( bool ) obj ).ToString();
			}
			else if ( type == typeof(float) )
			{
				valueString = ( ( float ) obj ).ToString();
			}
			else if ( type == typeof(double) )
			{
				valueString = ( ( double ) obj ).ToString();
			}
			else
			{
				valueString = null;
			}
			return valueString;
		}

		// Insertでmemberとpropertyに対して同じオペレーションを行なわなければならないので
		// その共通部分を抜き出した。
		private void InsertHelper(MemberInfo info , object obj , 
			StringBuilder fields , StringBuilder values)
		{
			DBColumnAttribute att =
				info.GetCustomAttributes(typeof(DBColumnAttribute) , false)[0] as DBColumnAttribute;

			if ( att == null )
				return;

			// 勝手に設定される値は設定してはいけない
			if ( att.AutoIncrement )
				return;

			// フィールド名は、名前がDBColumnAttributeで指定されていればそれを用いる。
			// さもなくば、メンバ名をそのまま用いる
			string fieldName = att.Name != null ? att.Name : info.Name;
			fieldName = '`' + fieldName.ToUpper() + '`';

			fields.Append(fieldName + ",\n");

			string valueString;
			valueString = ToSQLString(obj);
			if ( valueString == null )
				return;

			values.Append(valueString + ",\n");
		}

		public string Insert(object user_record)
		{
			// SQL文
			StringBuilder sb = new StringBuilder();

			StringBuilder fields = new StringBuilder();
			StringBuilder values = new StringBuilder();

			// reflectionでやってみる。
			
			// フィールドをそれぞれ調べる
			foreach ( FieldInfo info in user_record.GetType().GetFields() )
				InsertHelper(info , info.GetValue(user_record) , /*info.FieldType ,*/ fields , values);

			// propertyもそれぞれ調べる
			foreach ( PropertyInfo info in user_record.GetType().GetProperties() )
				InsertHelper(info , info.GetValue(user_record , null) ,/* info.PropertyType ,*/ fields , values);

			if (fields.Length >= 2)
				fields.Remove(fields.Length - 2 , 1); // 終端の','を削除

			if (values.Length >= 2)
				values.Remove(values.Length - 2 , 1); // 終端の','を削除

			sb.AppendFormat("INSERT INTO {0} (\n{1}) VALUES (\n{2});" ,
				GetTableName(user_record.GetType()) ,
				fields ,
				values
			);

			return sb.ToString();
		}

		// Convertでmemberとpropertyに対して同じオペレーションを行なわなければならないので
		// その共通部分を抜き出した。
		private void ConvertHelper<T>(MemberInfo info ,Type type,int count,ref int j,DataSet dataSet,List<T> list)
		{
			DBColumnAttribute att =
				info.GetCustomAttributes(typeof(DBColumnAttribute) , false)[0] as DBColumnAttribute;

			if ( att == null )
				return;

			for ( int i = 0 ; i < count ; ++i )
			{
				object o = dataSet.Tables[0].Rows[i][j];
				ConvertHelperHelper(ref o);

				{
					PropertyInfo info2 = info as PropertyInfo;
					if ( info2 != null )
						info2.SetValue(list[i] , o , null);
					else
					{
						FieldInfo info3 = info as FieldInfo;
					//	if ( info3 != null ) // どっちかに決まってるから、このチェック無駄
							info3.SetValue(list[i] , o);
					}
				}
			}
			++j;
		}

		private void ConvertHelperHelper(ref object o)
		{
			// Typeに対してswitch文が使えないの、おかしくね？(´ω`)
			Type type = o.GetType();
			if ( type == typeof(int) )
			{
				//	info.SetValue(list[i] , o);
			}
			else if ( type == typeof(string) )
			{
				//	info.SetValue(list[i] , o);
			}
			else if ( type == typeof(DateTime) )
			{
				//	valueString = ( ( DateTime ) info.GetValue(user_record) ).ToString();
				//	valueString = '\'' + valueString.Replace('/' , '-') + '\'';
				// 0001/01/01 0:00:00形式を
				// '0001-01-01 0:00:00'と変換する必要あり。
				//	info.SetValue(list[i] , o);
			}
			else if ( type == typeof(byte[]) )
			{
				//	info.SetValue(list[i] , o);
			}
			else if ( type == typeof(bool) )
			{
				bool b = ( ( short ) o ) != 0;
				o = ( object ) b; // recast
				//	info.SetValue(list[i] , ( object ) b);
			}
			else if ( type == typeof(float) )
			{
				//	info.SetValue(list[i] , o);
			}
			else if ( type == typeof(double) )
			{
				//	info.SetValue(list[i] , o);
			}
			else
			{
				o = null;
			}
		}

		// Convertでmemberとpropertyに対して同じオペレーションを行なわなければならないので
		// その共通部分を抜き出した。
		private void ConvertHelper2<T>(MemberInfo info , Type type , int count , int j ,
			string name,DataSet dataSet , List<T> list)
		{
			DBColumnAttribute att =
				info.GetCustomAttributes(typeof(DBColumnAttribute) , false)[0] as DBColumnAttribute;

			if ( att == null )
				return;

			// フィールド名は、名前がDBColumnAttributeで指定されていればそれを用いる。
			// さもなくば、メンバ名をそのまま用いる
			string fieldName = att.Name != null ? att.Name : info.Name;

			if ( name != fieldName.ToUpper() )
				return;

			for ( int i = 0 ; i < count ; ++i )
			{
				object o = dataSet.Tables[0].Rows[i][j];
				ConvertHelperHelper(ref o);

				{
					PropertyInfo info2 = info as PropertyInfo;
					if ( info2 != null )
						info2.SetValue(list[i] , o , null);
					else
					{
						FieldInfo info3 = info as FieldInfo;
						//	if ( info3 != null ) // どっちかに決まってるから、このチェック無駄
						info3.SetValue(list[i] , o);
					}
				}
			}
		}


		/// <summary>
		/// O/Rマッピングを行なう。
		/// </summary>
		/// <remarks>
		/// DataSetがemptyでも、空のListを返す。
		/// 
		/// 失敗すればYanesdkDBException例外を投げる
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="dataSet"></param>
		/// <returns></returns>
		public List<T> Convert<T>(DataSet dataSet) where T : new()
		{
			List<T> list = new List<T>();

			int count = dataSet.Tables[0].Rows.Count;
			for(int i=0;i<count;++i)
				list.Add(new T());

			try
			{
				// reflectionでやってみる。
				// 省略しているのだから、この順番(CreateTableのときに指定した順)でデータがやってくるはず。

				int j = 0;

				// フィールドをそれぞれ調べる
				foreach ( FieldInfo info in typeof(T).GetFields() )
					ConvertHelper<T>(info , info.FieldType , count , ref j , dataSet,list);

				// propertyもそれぞれ調べる
				foreach ( PropertyInfo info in typeof(T).GetProperties() )
					ConvertHelper<T>(info , info.PropertyType , count , ref j , dataSet,list);
			}
			catch
			{
				throw new YanesdkDBException();
			//	return YanesdkResult.HappenSomeError;
			}

			return list;
		}

		/// <summary>
		/// O/Rマッピングを行なう。
		/// </summary>
		/// <remarks>
		/// DataSetがemptyでも、空のListを返す。
		/// 
		/// 失敗すればYanesdkDBException例外を投げる
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="dataSet"></param>
		/// <returns></returns>
		public List<T> Convert<T>(DataSet dataSet , string selectField) where T : new()
		{
			List<T> list = new List<T>();

			int count = dataSet.Tables[0].Rows.Count;
			for ( int i = 0 ; i < count ; ++i )
				list.Add(new T());

			string[] fieldNames = selectField.Split(',');

			try
			{
				// reflectionでやってみる。
				// selectFieldの順番でデータがやってくるはず。

				for ( int j = 0 ; j < fieldNames.Length ; ++j )
				{
					string name = fieldNames[j].ToUpper();

					// フィールドをそれぞれ調べる
					foreach ( FieldInfo info in typeof(T).GetFields() )
						ConvertHelper2<T>(info , info.FieldType , count , j , name , dataSet , list);

					// propertyもそれぞれ調べる
					foreach ( PropertyInfo info in typeof(T).GetProperties() )
						ConvertHelper2<T>(info , info.PropertyType , count , j , name , dataSet , list);
				}
			}
			catch
			{
				throw new YanesdkDBException();
//				return YanesdkResult.HappenSomeError;
			}

			return list;
		}

		// Convertでmemberとpropertyに対して同じオペレーションを行なわなければならないので
		// その共通部分を抜き出した。
		private void FieldEqualHelper(MemberInfo info , object obj,string field,StringBuilder sb)
		{
			DBColumnAttribute att =
				info.GetCustomAttributes(typeof(DBColumnAttribute) , false)[0] as DBColumnAttribute;

			if ( att == null )
				return;

			// フィールド名は、名前がDBColumnAttributeで指定されていればそれを用いる。
			// さもなくば、メンバ名をそのまま用いる
			string fieldName = att.Name != null ? att.Name.ToUpper() : info.Name.ToUpper();

			if ( field != fieldName )
				return; // これではないようだ

			fieldName = '`' + fieldName + '`';
		
			// こいつのオブジェクトを取得する必要がある

			sb.AppendFormat("{0}={1}",
				fieldName,
				ToSQLString(obj)
			);
		}

		public string FieldEqual(object user_record , string filedName)
		{
			StringBuilder sb = new StringBuilder();

			string field = filedName.ToUpper();

			// フィールドをそれぞれ調べる
			foreach ( FieldInfo info in user_record.GetType().GetFields() )
				FieldEqualHelper(info , info.GetValue(user_record) , field , sb);

			// propertyもそれぞれ調べる
			foreach ( PropertyInfo info in user_record.GetType().GetProperties() )
				FieldEqualHelper(info , info.GetValue(user_record , null) , field , sb);

			return sb.ToString();
		}

		public string Update(Type type , string setPhrase , string wherePhrase)
		{
			StringBuilder sb = new StringBuilder();
			
			sb.AppendFormat("UPDATE {0}" , GetTableName(type));
			if ( setPhrase != null )
				sb.AppendFormat(" SET {0}" , setPhrase);
			if ( wherePhrase != null )
				sb.AppendFormat(" WHERE {0}" , wherePhrase);

			sb.Append(";");

			return sb.ToString();
		}
		
		/// <summary>
		/// テーブル名を取得する。
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public string GetTableName(Type type)
		{
			if ( !type.IsDefined(typeof(DBTableAttribute) , false) )
				return null;
			DBTableAttribute att1 =
				type.GetCustomAttributes(typeof(DBTableAttribute) , false)[0] as DBTableAttribute;

			// テーブル名
			string tableName = att1.Name != null ? att1.Name : type.Name;

			return '`'+tableName.ToUpper()+'`';
		}

		/// <summary>
		///  テーブルのバージョンを取得する
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public double GetTableVersion(Type type)
		{
			if (!type.IsDefined(typeof(DBTableAttribute), false))
				return 0;
			DBTableAttribute att1 =
				type.GetCustomAttributes(typeof(DBTableAttribute), false)[0] as DBTableAttribute;

			// バージョンナンバー
			double tableVersion = att1.Version;
			
			return tableVersion;
		}


		/// <summary>
		/// byte[]からstring(16進数)への変換。
		/// </summary>
		/// <param name="a"></param>
		public static string ByteToString(byte[] a)
		{
			StringBuilder sb = new StringBuilder();
			foreach ( byte b in a )
				sb.Append(b.ToString("X2"));
			return sb.ToString();
		}

		// MessageBoxの表示をしているので、Linux環境では動かないと思われ。

		/// <summary>
		/// "__TableVersion"で示されるテーブル上に、
		/// createdTable(←これはDBTableAttributeを持つ)のバージョン記録(integer)がなければ
		/// 新規にtypeで示されるテーブルをCreateTableにて作成する。
		/// また、"__TableVersion" にそのテーブルのバージョンをINSERT(←SQL)する。
		/// 
		/// もし、createdTableのテーブルに関するバージョン記録があればcreatedTableの
		/// DBTableAttributeのversionプロパティの値を
		/// 比較し、もし古ければ、そのcreatedTableのテーブルをDROP(←SQL)して再度作成する。
		/// (そのときに、テーブルをdropして作り直すことを警告するダイアログを出す)
		/// 
		/// "__TableVersion"自体が無い場合は、まず、そのテーブルを作成する。
		/// </summary>
		/// <remarks>
		/// DBまわりでのエラー発生時には例外を投げる。
		/// 
		/// MessageBoxを表示しているのでLinuxでは動かない。
		/// Linux環境下で動かした場合も例外を投げる。
		/// </remarks>
		/// <param name="db"></param>
		/// <param name="type"></param>
		/// <param name="version_table"></param>
		public void CreateTableIfNotExist(DBConnect db, Type createdTable)
		{
			// Windows専用である
			if (System.Platform.PlatformID != Yanesdk.System.PlatformID.Windows)
				throw new YanesdkDBException();

			// まずVersionTableの有無を調べる

			// 比較対象のテーブル
			string createdTableName =
				GetTableName(createdTable);

			double createdTableVersion =
				GetTableVersion(createdTable);

			// バージョンテーブルを調べる
			VersionTable versionRecord = new VersionTable();
			versionRecord.name = createdTableName;
			versionRecord.version = createdTableVersion;

			// 初回実行か？
			bool isFirst = true;

			// 2度目の実行なのでここ。
		Retry: ;

			// 名前が一致するレコードをとってくる
			try
			{
				string selectString = this.SelectAllFromTableWhere(
					versionRecord.GetType(), this.FieldEqual(versionRecord, "name"));

				List<VersionTable> list;
				list = this.Convert<VersionTable>(db.ExecuteQuery(selectString));

				if (list.Count == 0)
				{
					try
					{
						// 該当フィールドがないので新規作成
						string createString = this.CreateTable(createdTable);
						db.ExecuteNonQuery(createString);
					}
					catch
					{
						// これに失敗するということは、新規レコードではないくせに
						// Version情報がなかったのだと思われる。

						// だもんで無視するよーん
					}

					// テーブル作成に成功したならVersionTableに該当レコードを追加。
					string insertString = this.Insert(versionRecord);
					db.ExecuteQuery(insertString);
				}
				else if (list[0].version < createdTableVersion)
				{
					// バージョンが古いなら、このテーブルをdropして再作成する必要がある。

					// ダイアログを出して確認する
				
					DialogResult result = 
						MessageBox.Show(versionRecord.name
						+"テーブルが古いバージョンです。一度このテーブルを削除して作り直しますか？"
							,"注意",MessageBoxButtons.YesNo);

					if (result == DialogResult.Yes)
					{
						try
						{	 
							string dropString = this.DropTable(createdTable);
							db.ExecuteNonQuery(dropString);
						}
						catch
						{
							// そのテーブル自体が存在しないかも知れないので
							// このエラーは無視する
						}

						// 再作成
						string createString = this.CreateTable(createdTable);
						db.ExecuteNonQuery(createString);

						// dropに成功したなら、VersionTableの該当フィールドを変更しておく
						string updateString = this.Update(versionRecord.GetType(),
							this.FieldEqual(versionRecord, "version"),
							this.FieldEqual(versionRecord, "name")
						);
						db.ExecuteQuery(updateString);
					}
				}
				else
				{
					// 一致したので何もする必要がない
				}
			}
			catch
			{
				if (isFirst)
				{
					// そのVersionRecord自体が無いみたいなので、それを作るのが先決か。
					string sql = this.CreateTable(typeof(VersionTable));
					db.ExecuteNonQuery(sql);
					// ↑このCreateに失敗するのはなんぞおかしい

					isFirst = false;

					goto Retry;
				}

				throw; // 再throw
			}
		}
	}

	/// <summary>
	/// テーブル自体の属性
	/// </summary>
	/// <remarks>
	/// DBColumnAttributeと組み合わせて使う。
	/// </remarks>
	public class DBTableAttribute : Attribute
	{
		/// <summary>
		/// テーブル名。これを設定しなければ、変数名が(大文字化されて)
		/// そのままテーブル名になる。
		/// </summary>
		public string Name
		{
			get { return name; }
			set { name = value; }
		}
		private string name;

		public double Version
		{
			get { return version; }
			set { version = value; }
		}
		private double version;

	}

	/// <summary>
	/// DBのフィールドに対する属性
	/// </summary>
	/// <remarks>
	/// DBTableAttributeと組み合わせて使う。
	/// </remarks>
	public class DBColumnAttribute : Attribute
	{
		/// <summary>
		/// フィールド名。これを設定しなければ、変数名が(大文字化されて)
		/// そのままフィールド名になる。
		/// </summary>
		public string Name
		{
			get { return name; }
			set { name = value; } 
		}
		private string name;

		/// <summary>
		/// フィールドのサイズ
		/// </summary>
		/// <remarks>
		/// 設定しなければ0がdefaultで入っているが、
		/// 型に応じて適切なdefault値に変更される。
		/// </remarks>
		public int Size
		{
			get { return size; }
			set { size = value; }
		}
		private int size;

		/// <summary>
		/// このフィールドがDB上でNullを取りうるか？
		/// </summary>
		/// <remarks>
		/// 省略すればfalse
		/// </remarks>
		public bool Null
		{
			get { return nullable; }
			set { nullable = value; }
		}
		private bool nullable;

		/// <summary>
		/// ディフォルトの値について設定が必要な場合は、これで指定する。
		/// </summary>
		public string Default
		{
			get { return defaultString; }
			set { defaultString = value; }
		}
		private string defaultString;

		/// <summary>
		/// オプションの指定が必要な場合は、これで指定する。
		/// </summary>
		public string Option
		{
			get { return option; }
			set { option = value; }
		}
		private string option;


		/// <summary>
		/// プライマリキーなのか？
		/// </summary>
		/// <remarks>
		/// default = false
		/// 
		/// プライマリキーはunique制約を兼ねるので
		/// プライマリキーに指定すればunique制約の指定は不要。
		/// </remarks>
		public bool PrimaryKey
		{
			get { return primaryKey; }
			set { primaryKey = value; }
		}
		private bool primaryKey;


		/// <summary>
		/// ユニーク制約があるのか？
		/// </summary>
		/// <remarks>
		/// default = false
		/// </remarks>
		public bool UniqueKey
		{
			get { return uniqueKey; }
			set { uniqueKey = value; }
		}
		private bool uniqueKey;

		/// <summary>
		/// 自動的に加算する
		/// </summary>
		public bool AutoIncrement
		{
			get { return autoIncrement; }
			set { autoIncrement = value; }
		}
		private bool autoIncrement;

		/// <summary>
		/// stringはVarCharとして確保するのか？
		/// default では false。つまり固定長のchar。
		/// </summary>
		public bool VarChar
		{
			get { return varChar; }
			set { varChar = value; }
		}
		private bool varChar;

	}

	/// <summary>
	/// DBTableAttributeとDBColumnAttributeの使用例
	/// </summary>
	/// <remarks>
	/// 各テーブルのバージョンを管理するテーブル
	///	</remarks>
	[DBTableAttribute(Name = "__TableVersion",Version = 1.00)]
	public class VersionTable
	{
		// テーブル名
		[DBColumnAttribute(UniqueKey = true, Size = 20)]
		public string name;

		[DBColumnAttribute]
		public double version;
	}

	/// <summary>
	/// DBTableAttributeとDBColumnAttributeの使用例
	/// </summary>
	[DBTableAttribute(Name = "UserTable",Version = 1.00)]
	public class SampleRecord
	{
		[DBColumnAttribute(PrimaryKey=true,AutoIncrement=true)]
		public int id;

		[DBColumnAttribute(UniqueKey = true , Size = 10)]
		public string name;

		[DBColumnAttribute(UniqueKey = true , Size = 20 , VarChar = true)]
		public string varname;

		[DBColumnAttribute]
		public DateTime birth;

		[DBColumnAttribute(Default = "FALSE")]
		public bool isLogin;

		[DBColumnAttribute(Size = 64)]
		public byte[] data;

		[DBColumnAttribute]
		public int id_num;

		[DBColumnAttribute]
		public float num;

		[DBColumnAttribute]
		public double num2;

		// プロパティでも可能
		[DBColumnAttribute]
		public int Propnum
		{
			get { return propnum; }
			set { propnum = value; }
		}
		private int propnum;
	}

	#region サンプルコード
	// 以下、おまけというか、サンプルと言うか…
	/*
		public void Init2()
		{
			char c = Path.DirectorySeparatorChar;
			Console.WriteLine(c);

			// throw new NotSupportedException();

			DBConnect db = new ODBCConnect();
			db.Connect("TaisenOnline");

			SQLBuilder sql = new MySQL_SQLBuilder();
			string createTable = sql.CreateTable(typeof(SampleRecord));
			Console.WriteLine(createTable);

			string dropTable = sql.DropTable(typeof(SampleRecord));
			Console.WriteLine(dropTable);

			string selectTable = sql.SelectAllFromTable(typeof(SampleRecord));
			Console.WriteLine(selectTable);

			SampleRecord user = new SampleRecord();

			user.name = "\"'やね'うらお\"";
			user.id_num = 2345;
			user.num = 123.45f;
			user.num2 = 123.45;
			user.isLogin = true;
			user.varname = "おっすおら'ゴクウ'だぜよろしく";
			user.data = new byte[10] { 9 , 8 , 7 , 6 , 5 , 4 , 3 , 2 , 1 , 0 };
			user.birth = new DateTime(2006 , 10 , 26,23,59,58);

			string insertTable = sql.Insert(user);
			Console.WriteLine(insertTable);

			string fieldEqual = sql.FieldEqual(user , "data");
			string fieldEqual2 = sql.FieldEqual(user , "varname");
			string updateString = sql.Update(user.GetType() , fieldEqual , fieldEqual2);
			Console.WriteLine(updateString);
			//↑は、UPDATE `USERTABLE` SET `DATA`=0x09080706050403020100 WHERE `VARNAME`='おっすおらゴクウ！！';
			// となる

			string selectField = "id,data";
			string selectString = sql.SelectSomeFieldFromTableWhere(user.GetType() , selectField , null);
			Console.WriteLine(selectString);

			try
			{
				List<SampleRecord> list;
				int lines;
				lines = db.ExecuteNonQuery("set names sjis;"); // これがないとchar型に格納できる文字列が半分になる
				lines = db.ExecuteNonQuery(dropTable);
				lines = db.ExecuteNonQuery(createTable);
				lines = db.ExecuteNonQuery(insertTable);
				Console.WriteLine(lines.ToString());

				list = sql.Convert<SampleRecord>(db.ExecuteQuery(selectString) , selectField);
				list = sql.Convert<SampleRecord>(db.ExecuteQuery(selectTable));

				Console.WriteLine(list[0].name);
			}
			catch
			{

			}

			//	int lines;
			//	db.ExecuteNonQuery("update userdatas set isLogin = false where id = 2;" , out lines);

			db.Close();

		}

		}
	 */
	#endregion

	#region サンプルコード続編
	/*
		namespace WindowsApplication5_DBTest_
		{
			public partial class Form1 : Form
			{
				public Form1()
				{
					InitializeComponent();
				}
				
				private void button1_Click(object sender, EventArgs e)
				{
					DBConnect db = new ODBCConnect();
					db.Connect("TaisenOnline");

					SQLBuilder sql = new MySQL_SQLBuilder();

					sql.CreateTableIfNotExist(db, typeof(TestRecord));

					db.Dispose();
				}
			}

			[DBTableAttribute(Name = "TestTable",Version = 1)]
			public class TestRecord
			{
				[DBColumnAttribute(PrimaryKey = true, AutoIncrement = true)]
				public int id;

				[DBColumnAttribute(UniqueKey = true, Size = 10)]
				public string name;

				[DBColumnAttribute(UniqueKey = true, Size = 20, VarChar = true)]
				public string varname;
			}

			[DBTableAttribute(Name = "TestTable2", Version = 1)]
			public class TestRecord2
			{
				[DBColumnAttribute(PrimaryKey = true, AutoIncrement = true)]
				public int id;

				[DBColumnAttribute(UniqueKey = true, Size = 10)]
				public string name;

				[DBColumnAttribute(UniqueKey = true, Size = 20, VarChar = true)]
				public string varname;
			}

		}

	*/
	#endregion
}
