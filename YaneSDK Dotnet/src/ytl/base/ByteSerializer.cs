using System;
using System.Collections.Generic;
using System.Text;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// ByteSerializerが投げる例外
	/// </summary>
	public class ByteSerializerException : Exception
	{ }

	/// <summary>
	/// オブジェクトをbyte[]に詰め込んだり、byte[]から埋め込まれたオブジェクトを
	/// 取り出したりする。
	/// 
	/// まだ作りかけ。未テスト。
	/// </summary>
	public class ByteSerializer
	{
		/// <summary>
		/// オブジェクトをbyte[]に詰め込む。
		/// 
		/// 対応しているobjectの型は、short,int,string,byte[]のいずれか。
		/// 拡張したいならコピペして勝手に使ってちょうだい。
		/// </summary>
		/// <param name="o1"></param>
		/// <returns></returns>
		public static byte[] Serialize(object o1)
		{
			int size = GetSize(o1);

			byte[] a = new byte[size];
			Write(a, 0, o1);

			return a;
		}

		/// <summary>
		/// Serialize(object o1)の2変数版
		/// </summary>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		/// <returns></returns>
		public static byte[] Serialize(object o1, object o2)
		{
			int size1 = GetSize(o1);
			int size2 = GetSize(o2);

			byte[] a = new byte[size1 + size2];
			Write(a, 0, o1);
			Write(a, size1, o2);

			return a;
		}

		/// <summary>
		/// Serialize(object o1)の3変数版
		/// </summary>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		/// <param name="o3"></param>
		/// <returns></returns>
		public static byte[] Serialize(object o1, object o2, object o3)
		{
			int size1 = GetSize(o1);
			int size2 = GetSize(o2);
			int size3 = GetSize(o3);

			byte[] a = new byte[size1 + size2 + size3];
			Write(a, 0, o1);
			Write(a, size1, o2);
			Write(a, size1 + size2, o3);

			return a;
		}

		/// Serialize(object o1)の4変数版
		public static byte[] Serialize(object o1, object o2, object o3, object o4)
		{
			int size1 = GetSize(o1);
			int size2 = GetSize(o2);
			int size3 = GetSize(o3);
			int size4 = GetSize(o4);

			byte[] a = new byte[size1 + size2 + size3 + size4];
			Write(a, 0, o1);
			Write(a, size1, o2);
			Write(a, size1 + size2, o3);
			Write(a, size1 + size2 + size3, o4);

			return a;
		}

		/// <summary>
		/// objectのサイズを得る。
		/// 
		/// 対応しているobjectの型は、short,int,string,byte[]のいずれか。
		/// 対応していないものが引数に渡されるとByteSerializerExceptionを投げる。
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		public static int GetSize(object o)
		{
			Type t = o.GetType();

			if (t == typeof(short)) return 2;
			if (t == typeof(int)) return 4;
			if (t == typeof(string)) return (o as string).Length*2 + 4; // 4とはstringのLengthを表す空間
			if (t == typeof(byte[])) return (o as byte[]).Length + 4; // 4とはbyte[]のCountを表す空間

			throw new ByteSerializerException(); // サポート外
		}

		/// <summary>
		/// 配列aのindexの位置にobjectの内容を書き込む。
		/// aは十分に確保してあると仮定している。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="index"></param>
		/// <param name="o"></param>
		public static void Write(byte[] a, int index, object o)
		{
			Type t = o.GetType();

			if (t == typeof(short))
				MemoryCopy.SetShort(a, index, (short)o);
			else if (t == typeof(int))
				MemoryCopy.SetInt(a, index, (int)o);
			else if (t == typeof(string))
			{
				string s = o as string;
				MemoryCopy.SetInt(a, index, s.Length);
				MemoryCopy.SetString(a, index + sizeof(int) ,s , s.Length);
			}
			else if (t == typeof(byte[]))
			{
				byte[] a2 = o as byte[];
				MemoryCopy.SetInt(a, index, a2.Length);
				MemoryCopy.MemCopy(a, index + sizeof(int), a2, 0,a2.Length);
			}

			throw new ByteSerializerException(); // サポート外
		}

		/// <summary>
		/// Serializeしたものを復元する。
		/// 変数indexとして配列aのどこからを復元ポイントとするかを指定できる。
		/// 
		/// 対応しているobjectの型は、short,int,string,byte[]のいずれか。
		/// t1としてobjectの型を渡す。
		/// サポート外のobjectを渡されるとByteSerializerExceptionを投げる。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="index"></param>
		/// <param name="o"></param>
		public static void Deserialize(byte[] a, int index, Type t1,out object o1)
		{
			o1 = Read(a, ref index,t1);
		}

		/// <summary>
		/// Deserializeのobject2つとれる版。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="index"></param>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		public static void Deserialize(byte[] a, int index, Type t1,out object o1,Type t2,out object o2)
		{
			o1 = Read(a, ref index,t1);
			o2 = Read(a, ref index,t2);
		}

		/// <summary>
		/// Deserializeのobject3つとれる版。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="index"></param>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		public static void Deserialize(byte[] a, int index,Type t1,out object o1,Type t2,out object o2,
			Type t3,out object o3)
		{
			o1 = Read(a, ref index,t1);
			o2 = Read(a, ref index,t2);
			o3 = Read(a, ref index,t3);
		}

		/// <summary>
		/// Deserializeのobject4つとれる版。
		/// </summary>
		/// <param name="a"></param>
		/// <param name="index"></param>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		/// <param name="o3"></param>
		/// <param name="o4"></param>
		public static void Deserialize(byte[] a, int index,Type t1,out object o1,Type t2,out object o2,
			Type t3,out object o3,Type t4,out object o4)
		{
			o1 = Read(a, ref index,t1);
			o2 = Read(a, ref index, t2);
			o3 = Read(a, ref index, t3);
			o4 = Read(a, ref index, t4);
		}

		public static object Read(byte[] a, ref int index,Type t)
		{
			if (t == typeof(short))
			{
				short s = MemoryCopy.GetShort(a, index);
				index += sizeof(short);
				return (object)s;
			}
			else if (t == typeof(int))
			{
				int i = MemoryCopy.GetInt(a, index);
				index += sizeof(int);
				return (object)i;
			}
			else if (t == typeof(string))
			{
				string s ;
				int length;
				length = MemoryCopy.GetInt(a, index);
				index += sizeof(int);
				s = MemoryCopy.GetString(a, index , length);
				index += length * 2;
				return (object)s;
			}
			else if (t == typeof(byte[]))
			{
				int length;
				length = MemoryCopy.GetInt(a, index);
				index += sizeof(int);
				byte[] a2 = new byte[length];
				MemoryCopy.MemCopy(a2, index, a, index, length);
				index += length;
				return (object)a2;
			}

			throw new ByteSerializerException(); // サポート外
		}
	
	}
}
