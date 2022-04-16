using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Yanesdk.Ytl
{
	/// <summary>
	/// C/C++的なポインタを実現する
	/// </summary>
	/// <remarks>
	/// http://d.hatena.ne.jp/yaneurao/20060424
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	public class Pointer<T>
	{
		/// <summary>
		/// 配列の先頭を指すポインタを生成する
		/// </summary>
		/// <param name="a"></param>
		/// <param name="index"></param>
		public Pointer(T[] array)
		{
			this.array = array;
			this.index = 0;
		}

		/// <summary>
		/// 配列と、その配列のどの要素を指すポインタを生成するかを指定する
		/// </summary>
		/// <param name="a"></param>
		/// <param name="index"></param>
		public Pointer(T[] array,int index)
		{
			this.array = array;
			this.index = index;
		}

		/// <summary>
		/// ポインタに整数加算
		/// </summary>
		/// <remarks>ポインタ同士の加減算は出来ないし、出来てはならない。
		/// </remarks>
		/// <param name="pointer"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static Pointer<T> operator + (Pointer<T> pointer,int n)
		{
			return new Pointer<T>(pointer.array,pointer.index+n);
		}

		/// <summary>
		/// ポインタに整数減算
		/// </summary>
		/// <remarks>ポインタ同士の加減算は出来ないし、出来てはならない。
		/// </remarks>
		/// <param name="pointer"></param>
		/// <param name="n"></param>
		/// <returns></returns>
		public static Pointer<T> operator -(Pointer<T> pointer , int n)
		{
			return new Pointer<T>(pointer.array , pointer.index - n);
		}

		
		/// <summary>
		/// Cloneメソッド
		/// </summary>
		/// <returns></returns>
		public Pointer<T> Clone()
		{
			return new Pointer<T>(this.array , this.index);
		}

		/// <summary>
		/// 参照はがし
		/// </summary>
		public T Value
		{
			get { return array[index]; }
			set { array[index] = value; }
		}

		/// <summary>
		/// このポインタが指している要素からの相対で値を設定する。
		/// </summary>
		/// <param name="index">相対値。</param>
		/// <param name="t"></param>
		public void SetElement(int index, T t)
		{
			array[this.index + index] = t;
		}

		/// <summary>
		/// このポインタが指している要素からの相対で値を取得する。
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T GetElement(int index)
		{
			return array[this.index + index];
		}

		/// <summary>
		/// C#だと制約がきつすぎて、あまり無茶なことが出来ないが…
		/// IntPtrがもらえればunsafe文脈で structにcastして用いることが出来るだろう。
		/// </summary>
		/// <returns></returns>
		public IntPtrForStruct GetIntPtrForStruct()
		{
			GCHandle handle = GCHandle.Alloc(array , GCHandleType.Pinned);
			IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(array, index);

			return new IntPtrForStruct(handle,ptr);
		}

		/* // pointerのテストコード
		{
			{
				byte[] d = new byte[256];
				Pointer<byte> p = new Pointer<byte>(d , 5);
				using ( IntPtrForStruct s = p.GetIntPtrForStruct() )
				{
					unsafe
					{
						XYZ* xyz = ( XYZ* ) s.Ptr;
						{
							xyz->x = 123;
							xyz->y = 234;
						}
					}
				}
			}
		}
		struct XYZ
		{
			public int x;
			public int y;
			public int z;
		}
		*/

		/// <summary>
		/// 現在このポインタが指している配列オブジェクトを取得。
		/// </summary>
		public T[] Array
		{
			get { return array; }
		}
		private T[] array;

		/// <summary>
		/// 現在このポインタが指しているindex位置を取得。
		/// </summary>
		public int Index
		{
			get { return index; }
		}
		private int index;
	}

	/// <summary>
	/// PointerクラスのGetIntPtrForStructで用いるための構造体
	/// </summary>
	/// <remarks>
	/// Pointerクラスの外に出すのは、OOPの観点からすればおかしいが、
	/// 利便性の上からは許されるだろう…
	/// </remarks>
	public class IntPtrForStruct : IDisposable
	{
		private GCHandle handle;

		/// <summary>
		/// このIntPtrをunsafe文脈で structにcastして使うべし。
		/// </summary>
		public IntPtr Ptr
		{
			get { return ptr; }
		}
		private IntPtr ptr;

		public IntPtrForStruct(GCHandle handle , IntPtr ptr)
		{
			this.handle = handle;
			this.ptr = ptr;
		}

		public void Dispose()
		{
			this.handle.Free();
		}
	}

}
