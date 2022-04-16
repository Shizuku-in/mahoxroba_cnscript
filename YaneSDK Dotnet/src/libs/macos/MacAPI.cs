using System;
using System.Runtime.InteropServices;

namespace Yanesdk.Sdl.MacAPI
{
	[CLSCompliant(false)]
	public abstract class MacAPI
	{		
		[DllImport("/System/Library/Frameworks/Cocoa.framework/Cocoa", EntryPoint="NSApplicationLoad")]
        public static extern void NSApplicationLoad();
    
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern void GetCurrentProcess(ref IntPtr psn);
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern void TransformProcessType(ref IntPtr psn, uint type);
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern void SetFrontProcess(ref IntPtr psn);
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern void ExitToShell();
  	}
}
