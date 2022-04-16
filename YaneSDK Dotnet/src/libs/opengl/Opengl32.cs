using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenGl
{
	/// <summary>
	/// WindowのHDCと関連付けるための拡張。
	/// opengl32.dllを利用する。Windowsプラットフォーム限定。
	/// このクラスのAPIを使うとlinux環境下では動かなくなる。
	/// </summary>
	[CLSCompliant(false)]
	public class Opengl32
	{
		//cf. http://www.moonwink.com/h/?q=node/2

		#region const open読み込みdllの位置はここ。
		public const string OGL_DLL = "opengl32.dll";	// Import library for OpenGL on Win32
		#endregion

		// opengl32.dll unmanaged Win32 DLL
		[DllImport(OGL_DLL)] public static extern IntPtr wglGetCurrentContext();
		[DllImport(OGL_DLL)]
		public static extern int wglMakeCurrent(IntPtr hdc, IntPtr hglrc);
		[DllImport(OGL_DLL)]
		public static extern IntPtr wglCreateContext(IntPtr hdc);
		[DllImport(OGL_DLL)]
		public static extern int wglDeleteContext(IntPtr hglrc);

		[DllImport(OGL_DLL)]
		public static extern IntPtr wglGetCurrentDC();

		[DllImport(OGL_DLL)]
		public static extern int wglShareLists(IntPtr hglrc1 , IntPtr hglrc2);

	}
}
