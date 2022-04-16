# region license
/*
    SDL - Simple DirectMedia Layer
    Copyright (C) 1997, 1998, 1999, 2000, 2001  Sam Lantinga

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Library General Public
    License as published by the Free Software Foundation; either
    version 2 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Library General Public License for more details.

    You should have received a copy of the GNU Library General Public
    License along with this library; if not, write to the Free
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

    Sam Lantinga
    slouken@devolution.com
*/
# endregion
#region using
using System;
using System.Runtime.InteropServices;
using System.Security;

using Uint8 = System.Byte;
using Sint8 = System.SByte;
using Uint16 = System.UInt16;
using Sint16 = System.Int16;
using Uint32 = System.UInt32;
using Sint32 = System.Int32;
using Uint64 = System.UInt64;
using Sint64 = System.Int64;
using HWND = System.IntPtr;
using UINT = System.UInt32;
using WPARAM = System.UInt32;
using LPARAM = System.UInt32;
using SDL_TimerID = System.IntPtr;
#endregion
namespace Sdl
{
	#region RWops関連
	// 以下の二つの構造体は、SDLのさまざまなメソッドが要求する
	// ファイルハンドルとそのオペレーションに関連した構造体である。
	// 非常に重要な構造体なので、くくりだしておく。

	/* This is the read/write operation structure -- very basic */
	[CLSCompliant(true)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_RWopsH
	{
		public IntPtr hMem;
		public IntPtr Handle;
		public int Length;
	}

	[CLSCompliant(true)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_RWops
	{
		/* Seek to 'offset' relative to whence, one of stdio'name whence values:
				SEEK_SET, SEEK_CUR, SEEK_END
				Returns the final offset in the data source.
				*/
		public /*_seek_func_t */IntPtr seek;
		//	int (*seek)(SDL_RWops *context, int offset, int whence);

		/* Read up to 'num' objects each of size 'objsize' from the data
				source to the area pointed at by 'ptr'.
				Returns the number of objects read, or -1 if the read failed.
				*/
		public /*_read_func_t */IntPtr read;
		//	int (*read)(SDL_RWops *context, void *ptr, int size, int maxnum);

		/* Write exactly 'num' objects each of size 'objsize' from the area
				pointed at by 'ptr' to data source.
				Returns 'num', or -1 if the write failed.
				*/
		public /*_write_func_t */IntPtr write;
		//	int (*write)(SDL_RWops *context, void *ptr, int size, int num);

		/* Close and free an allocated SDL_FSops structure */
		public /*_close_func_t */IntPtr close;
		//	int (*close)(SDL_RWops *context);

		public Int32 type;

		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_RWops_stdio
		{
			public int autoclose;
			public IntPtr fp;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_RWops_mem
		{
			public IntPtr base_;
			public IntPtr here;
			public IntPtr stop;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_RWops_unknown
		{
			public IntPtr data1;
		}

		[StructLayout(LayoutKind.Explicit, Pack = 4)]
		public struct SDL_RWops_hidden
		{
			[FieldOffset(0)]
			public SDL_RWops_stdio stdio;
			[FieldOffset(0)]
			public SDL_RWops_mem mem;
			[FieldOffset(0)]
			public SDL_RWops_unknown unknown;
		}

		public SDL_RWops_hidden hidden;
	}
	#endregion

	/// <summary>
	/// SDL関連のクラス
	/// </summary>
	[CLSCompliant(false)]
	// 効果が薄いので使用せず
	// [SuppressUnmanagedCodeSecurity]
	public abstract class SDL {
		//	static readonly SDLVideoInitializer initializer = new SDLVideoInitializer();

		#region DLL pathの設定

		/// <summary>
		/// これ、変更できない。変更してはならない。
		/// </summary>
		const string DLL_CURRENT = /* Yanesdk.System.DllManager.DLL_CURRENT */"";

		// SDLの母艦。全般的に必要。
		const string DLL_SDL = DLL_CURRENT + Yanesdk.Sdl.SDL_Initializer.DLL_SDL;
		
		// 描画系のクラスで必要。
		const string DLL_SDL_IMAGE = DLL_CURRENT + Yanesdk.Sdl.SDL_Initializer.DLL_SDL_IMAGE;

		// Sound関係のクラスで必要。
		const string DLL_SDL_MIXER = DLL_CURRENT + Yanesdk.Sdl.SDL_Initializer.DLL_SDL_MIXER;

		// 文字列描画クラスで必要。
		const string DLL_SDL_TTF = DLL_CURRENT + Yanesdk.Sdl.SDL_Initializer.DLL_SDL_TTF;

		// なお、DLL_SDL_CURRENTの値に基づいて、
		// SDL_mixer関係(ogg.dll,vorbis.dllなど)が読み込まれる。

		#endregion

		#region SDL

		/* As of version 0.5, SDL is loaded dynamically into the application */

		/* These are the flags which may be passed to SDL_Init() -- you should
		   specify the subsystems which you will be using in your application.
		*/
		public const uint SDL_INIT_TIMER		= 0x00000001;
		public const uint SDL_INIT_AUDIO		= 0x00000010;
		public const uint SDL_INIT_VIDEO		= 0x00000020;
		public const uint SDL_INIT_CDROM		= 0x00000100;
		public const uint SDL_INIT_JOYSTICK	= 0x00000200;
		public const uint SDL_INIT_NOPARACHUTE	= 0x00100000;	/* Don't catch fatal signals */
		public const uint SDL_INIT_EVENTTHREAD	= 0x01000000;	/* Not supported on all OS'name */
		public const uint SDL_INIT_EVERYTHING	= 0x0000FFFF;

		/* This function loads the SDL dynamically linked library and initializes 
		 * the subsystems specified by 'flags' (and those satisfying dependencies)
		 * Unless the SDL_INIT_NOPARACHUTE flag is set, it will install cleanup
		 * signal handlers for some commonly ignored fatal signals (like SIGSEGV)
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_Init(Uint32 flags);

		/* This function initializes specific SDL subsystems */
		[DllImport(DLL_SDL)]
		public static extern int SDL_InitSubSystem(Uint32 flags);

		/* This function cleans up specific SDL subsystems */
		[DllImport(DLL_SDL)]
		public static extern void SDL_QuitSubSystem(Uint32 flags);

		/* This function returns mask of the specified subsystems which have
		   been initialized.
		   If 'flags' is 0, it returns a mask of all initialized subsystems.
		*/
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_WasInit(Uint32 flags);

		/* This function cleans up all initialized subsystems and unloads the
		 * dynamically linked library.	You should call it upon all exit conditions.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_Quit();

		#endregion
		#region SDL_active
		/* The available application states */
		public const uint SDL_APPMOUSEFOCUS	= 0x01;		/* The app has mouse coverage */
		public const uint SDL_APPINPUTFOCUS	= 0x02;		/* The app has input focus */
		public const uint SDL_APPACTIVE		= 0x04;		/* The application is active */

		/* Function prototypes */
		/* 
		 * This function returns the current state of the application, which is a
		 * bitwise combination of SDL_APPMOUSEFOCUS, SDL_APPINPUTFOCUS, and
		 * SDL_APPACTIVE.  If SDL_APPACTIVE is set, then the user is able to
		 * see your application, otherwise it has been iconified or disabled.
		 */
		[DllImport(DLL_SDL)]
		public static extern Uint8 SDL_GetAppState();
		#endregion
		#region SDL_audio
		/* The calculated values in this structure are calculated by SDL_OpenAudio() */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_AudioSpec {
			public int freq;		/* DSP frequency -- samples per second */
			public Uint16 format;		/* Audio data format */
			public Uint8  channels;	/* Number of channels: 1 mono, 2 stereo */
			public Uint8  silence;		/* Audio buffer silence value (calculated) */
			public Uint16 samples;		/* Audio buffer size in samples (power of 2) */
			public Uint16 padding;		/* Necessary for some compile environments */
			public Uint32 size;		/* Audio buffer size in bytes (calculated) */
			/* This function is called when the audio device needs more data.
			   'stream' is a pointer to the audio data buffer
			   'len' is the length of that buffer in bytes.
			   Once the callback returns, the buffer will no longer be valid.
			   Stereo samples are stored in a LRLRLR ordering.
			*/
			public IntPtr callback;	/* void (*callback)(void *userdata, Uint8 *stream, int len); */
			public IntPtr userdata;	/* void  *userdata; */
		}

		/* Audio format flags (defaults to LSB byte order) */
		public const uint AUDIO_U8	= 0x0008;	/* Unsigned 8-bit samples */
		public const uint AUDIO_S8	= 0x8008;	/* Signed 8-bit samples */
		public const uint AUDIO_U16LSB	= 0x0010;	/* Unsigned 16-bit samples */
		public const uint AUDIO_S16LSB	= 0x8010;	/* Signed 16-bit samples */
		public const uint AUDIO_U16MSB	= 0x1010;	/* As above, but big-endian byte order */
		public const uint AUDIO_S16MSB	= 0x9010;	/* As above, but big-endian byte order */
		public const uint AUDIO_U16	= AUDIO_U16LSB;
		public const uint AUDIO_S16	= AUDIO_S16LSB;

		/* Native audio byte ordering */
		//const uint AUDIO_U16SYS	= AUDIO_U16LSB;
		//const uint AUDIO_S16SYS	= AUDIO_S16LSB;
		public const uint AUDIO_U16SYS	= AUDIO_U16MSB;
		public const uint AUDIO_S16SYS	= AUDIO_S16MSB;


		/* A structure to hold a set of audio conversion filters and buffers */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_AudioCVT {
			public int needed;			/* Set to 1 if conversion possible */
			public Uint16 src_format;		/* Source audio format */
			public Uint16 dst_format;		/* Target audio format */
			public double rate_incr;		/* Rate conversion increment */
			public IntPtr buf;	/* Uint8 *buf; */			/* Buffer to hold entire audio data */
			public int    len;			/* Length of original audio buffer */
			public int    len_cvt;			/* Length of converted audio buffer */
			public int    len_mult;		/* buffer must be len*len_mult big */
			public double len_ratio; 	/* Given len, final size is len*len_ratio */
			[MarshalAs(UnmanagedType.ByValArray,SizeConst=10)]
			public IntPtr[] filters;	/* void (*filters[10])(SDL_AudioCVT *cvt, Uint16 format); */
			public int filter_index;		/* Current audio conversion function */
		}


		/* Function prototypes */

		/* These functions are used internally, and should not be used unless you
		 * have a specific need to specify the audio driver you want to use.
		 * You should normally use SDL_Init() or SDL_InitSubSystem().
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_AudioInit(/*char* */string driver_name);
		[DllImport(DLL_SDL)]
		public static extern void SDL_AudioQuit();

		/* This function fills the given character buffer with the name of the
		 * current audio driver, and returns a pointer to it if the audio driver has
		 * been initialized.  It returns NULL if no driver has been initialized.
		 */

		[DllImport(DLL_SDL)]
		public static extern byte[] SDL_AudioDriverName(/*char * */byte[] namebuf, int maxlen);

		/*
		 * This function opens the audio device with the desired parameters, and
		 * returns 0 if successful, placing the actual hardware parameters in the
		 * structure pointed to by 'obtained'.  If 'obtained' is NULL, the audio
		 * data passed to the callback function will be guaranteed to be in the
		 * requested format, and will be automatically converted to the hardware
		 * audio format if necessary.  This function returns -1 if it failed 
		 * to open the audio device, or couldn't set up the audio thread.
		 * 
		 * When filling in the desired audio spec structure,
		 *  'desired->freq' should be the desired audio frequency in samples-per-second.
		 *  'desired->format' should be the desired audio format.
		 *  'desired->samples' is the desired size of the audio buffer, in samples.
		 *     This number should be a power of two, and may be adjusted by the audio
		 *     driver to a value more suitable for the hardware.  Good values seem to
		 *     range between 512 and 8096 inclusive, depending on the application and
		 *     CPU speed.  Smaller values yield faster response time, but can lead
		 *     to underflow if the application is doing heavy processing and cannot
		 *     fill the audio buffer in time.  A stereo sample consists of both right
		 *     and left channels in LR ordering.
		 *     Note that the number of samples is directly related to time by the
		 *     following formula:  ms = (samples*1000)/freq
		 *  'desired->size' is the size in bytes of the audio buffer, and is
		 *     calculated by SDL_OpenAudio().
		 *  'desired->silence' is the value used to set the buffer to silence,
		 *     and is calculated by SDL_OpenAudio().
		 *  'desired->callback' should be set to a function that will be called
		 *     when the audio device is ready for more data.  It is passed a pointer
		 *     to the audio buffer, and the length in bytes of the audio buffer.
		 *     This function usually runs in a separate thread, and so you should
		 *     protect data structures that it accesses by calling SDL_LockAudio()
		 *     and SDL_UnlockAudio() in your code.
		 *  'desired->userdata' is passed as the first parameter to your callback
		 *     function.
		 * 
		 * The audio device starts out playing silence when it'name opened, and should
		 * be enabled for playing by calling SDL_PauseAudio(0) when you are ready
		 * for your audio callback function to be called.  Since the audio driver
		 * may modify the requested size of the audio buffer, you should allocate
		 * any local mixing buffers after you open the audio device.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_OpenAudio(/* SDL_AudioSpec * */IntPtr desired, /* SDL_AudioSpec * */IntPtr obtained);

		/*
		 * Get the current audio state:
		 */
		public enum SDL_audiostatus : int {
			SDL_AUDIO_STOPPED = 0,
			SDL_AUDIO_PLAYING,
			SDL_AUDIO_PAUSED
		}
		[DllImport(DLL_SDL)]
		public static extern SDL_audiostatus SDL_GetAudioStatus();

		/*
		 * This function pauses and unpauses the audio callback processing.
		 * It should be called with a parameter of 0 after opening the audio
		 * device to start playing sound.  This is so you can safely initialize
		 * data for your callback function after opening the audio device.
		 * Silence will be written to the audio device during the pause.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_PauseAudio(int pause_on);

		/*
		 * This function loads a WAVE from the data source, automatically freeing
		 * that source if 'freesrc' is non-zero.  For example, to load a WAVE file,
		 * you could do:
		 *	SDL_LoadWAV_RW(SDL_RWFromFile("sample.wav", "rb"), 1, ...);
		 * 
		 * If this function succeeds, it returns the given SDL_AudioSpec,
		 * filled with the audio data format of the wave data, and sets
		 * 'audio_buf' to a malloc()'d buffer containing the audio data,
		 * and sets 'audio_len' to the length of that audio buffer, in bytes.
		 * You need to free the audio buffer with SDL_FreeWAV() when you are 
		 * done with it.
		 * 
		 * This function returns NULL and sets the SDL error message if the 
		 * wave file cannot be opened, uses an unknown data format, or is 
		 * corrupt.  Currently raw and MS-ADPCM WAVE files are supported.
		 */
		[DllImport(DLL_SDL)]
		public static extern /*SDL_AudioSpec * */IntPtr SDL_LoadWAV_RW(/* SDL_RWops * */IntPtr src, int freesrc,
			/* SDL_AudioSpec * */IntPtr spec, /* Uint8 ** */IntPtr audio_buf, /* Uint32 * */ref int audio_len);

		/* Compatibility convenience function -- loads a WAV from a file */
		public static /*SDL_AudioSpec * */IntPtr SDL_LoadWAV(/* char* */string file, /* SDL_AudioSpec* */IntPtr spec,
			/* Uint8 ** */IntPtr audio_buf, /* Uint32 * */ref int audio_len) {		
			return SDL_LoadWAV_RW(SDL_RWFromFile(file, "rb"), 1, spec,
				audio_buf, ref audio_len);
		}

		/*
		 * This function frees data previously allocated with SDL_LoadWAV_RW()
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_FreeWAV(/* Uint8 * */IntPtr audio_buf);

		/*
		 * This function takes a source format and rate and a destination format
		 * and rate, and initializes the 'cvt' structure with information needed
		 * by SDL_ConvertAudio() to convert a buffer of audio data from one format
		 * to the other.
		 * This function returns 0, or -1 if there was an error.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_BuildAudioCVT(/* SDL_AudioCVT * */IntPtr cvt,
			Uint16 src_format, Uint8 src_channels, int src_rate,
			Uint16 dst_format, Uint8 dst_channels, int dst_rate);

		/* Once you have initialized the 'cvt' structure using SDL_BuildAudioCVT(),
		 * created an audio buffer cvt->buf, and filled it with cvt->len bytes of
		 * audio data in the source format, this function will convert it in-place
		 * to the desired format.
		 * The data conversion may expand the size of the audio data, so the buffer
		 * cvt->buf should be allocated after the cvt structure is initialized by
		 * SDL_BuildAudioCVT(), and should be cvt->len*cvt->len_mult bytes long.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_ConvertAudio(/* SDL_AudioCVT * */IntPtr cvt);

		/*
		 * This takes two audio buffers of the playing audio format and mixes
		 * them, performing addition, volume adjustment, and overflow clipping.
		 * The volume ranges from 0 - 128, and should be set to SDL_MIX_MAXVOLUME
		 * for full audio volume.  Note this does not change hardware volume.
		 * This is provided for convenience -- you can mix your own audio data.
		 */
		public const uint SDL_MIX_MAXVOLUME = 128;
		[DllImport(DLL_SDL)]
		public static extern void SDL_MixAudio(/* Uint8 * */byte[] dst, /* Uint8 * */byte[] src, Uint32 len, int volume);

		/*
		 * The lock manipulated by these functions protects the callback function.
		 * During a LockAudio/UnlockAudio pair, you can be guaranteed that the
		 * callback function is not running.  Do not call these from the callback
		 * function or you will cause deadlock.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_LockAudio();
		[DllImport(DLL_SDL)]
		public static extern void SDL_UnlockAudio();

		/*
		 * This function shuts down audio processing and closes the audio device.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_CloseAudio();
		#endregion
		#region SDL_byteorder
		
		/* Macros for determining the byte-order of this platform */

		/* The two types of endianness */
		public const uint SDL_LIL_ENDIAN =	1234;
		public const uint SDL_BIG_ENDIAN =	4321;

		/* Pardon the mess, I'm trying to determine the endianness of this host.
		   I'm doing it by preprocessor defines rather than some sort of configure
		   script so that application code can use this too.  The "right" way would
		   be to dynamically generate this file on install, but that'name a lot of work.
		 */
		public static uint SDL_BYTEORDER = IsLittleEndian ? SDL_LIL_ENDIAN : SDL_BIG_ENDIAN;

		/// <summary>
		/// big endianかlittle endianかは動的に判定するようにコード追記。by やねうらお
		/// </summary>
		private static bool IsLittleEndian
		{
			get
			{
				bool littleEndian;
				unsafe
				{
					int n = 1;
					littleEndian = *(byte*)&n != 0;
				}
				return littleEndian;
			}
		}

		#endregion
		#region SDL_cdrom
		/* In order to use these functions, SDL_Init() must have been called
		   with the SDL_INIT_CDROM flag.  This causes SDL to scan the system
		   for CD-ROM drives, and load appropriate drivers.
		*/

		/* The maximum number of CD-ROM tracks on a disk */
		public const int SDL_MAX_TRACKS	= 99;

		/* The types of CD-ROM track possible */
		public const uint SDL_AUDIO_TRACK	= 0x00;
		public const uint SDL_DATA_TRACK	= 0x04;

		/* The possible states which a CD-ROM drive can be in. */
		public enum CDstatus : int {
			CD_TRAYEMPTY,
			CD_STOPPED,
			CD_PLAYING,
			CD_PAUSED,
			CD_ERROR = -1
		}

		/* Given a status, returns true if there'name a disk in the drive */
		public static bool CD_INDRIVE(CDstatus status) { return status > 0; }

		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_CDtrack {
			public Uint8 id;		/* Track number */
			public Uint8 type;		/* Data or audio track */
			public Uint16 unused;
			public Uint32 length;		/* Length, in frames, of this track */
			public Uint32 offset;		/* Offset, in frames, from start of disk */
		}

		/* This structure is only current as of the last call to SDL_CDStatus() */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_CD {
			public int id;			/* Private drive identifier */
			public CDstatus status;	/* Current drive status */

			/* The rest of this structure is only valid if there'name a CD in drive */
			public int numtracks;		/* Number of tracks on disk */
			public int cur_track;		/* Current track position */
			public int cur_frame;		/* Current frame offset within current track */
			// [MarshalAs(UnmanagedType.ByValArray, SizeConst=SDL_MAX_TRACKS+1)]
			public IntPtr track;	// SDL_CDtrack[SDL_MAX_TRACKS+1]
		}

		/* Conversion functions from frames to Minute/Second/Frames and vice versa */
		public const uint CD_FPS	= 75;
		public static void FRAMES_TO_MSF(int f, out int M, out int S, out int F) {
			int value = f;
			F = (int)(value % CD_FPS);
			value /= (int)CD_FPS;
			S = value % 60;
			value /= 60;
			M = value;
		}

		public static int MSF_TO_FRAMES(int M, int S, int F) {
			return (int)(M * 60 * CD_FPS + S * CD_FPS + F);
		}

		/* CD-audio API functions: */

		/* Returns the number of CD-ROM drives on the system, or -1 if
		   SDL_Init() has not been called with the SDL_INIT_CDROM flag.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CDNumDrives();

		/* Returns a human-readable, system-dependent identifier for the CD-ROM.
		   Example:
			"/dev/cdrom"
			"E:"
			"/dev/disk/ide/1/master"
		*/
		[DllImport(DLL_SDL)]
		public static extern /*char * */string SDL_CDName(int drive);

		/* Opens a CD-ROM drive for access.  It returns a drive handle on success,
		   or NULL if the drive was invalid or busy.  This newly opened CD-ROM
		   becomes the default CD used when other CD functions are passed a NULL
		   CD-ROM handle.
		   Drives are numbered starting with 0.  Drive 0 is the system default CD-ROM.
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_CD * */IntPtr SDL_CDOpen(int drive);

		/* This function returns the current status of the given drive.
		   If the drive has a CD in it, the table of contents of the CD and current
		   play position of the CD will be stored in the SDL_CD structure.
		*/
		[DllImport(DLL_SDL)]
		public static extern CDstatus SDL_CDStatus(/* SDL_CD * */IntPtr cdrom);

		/* Play the given CD starting at 'start_track' and 'start_frame' for 'ntracks'
		   tracks and 'nframes' frames.  If both 'ntrack' and 'nframe' are 0, play 
		   until the end of the CD.  This function will skip data tracks.
		   This function should only be called after calling SDL_CDStatus() to 
		   get track information about the CD.
		   For example:
			// Play entire CD:
			if ( CD_INDRIVE(SDL_CDStatus(cdrom)) )
				SDL_CDPlayTracks(cdrom, 0, 0, 0, 0);
			// Play last track:
			if ( CD_INDRIVE(SDL_CDStatus(cdrom)) ) {
				SDL_CDPlayTracks(cdrom, cdrom->numtracks-1, 0, 0, 0);
			}
			// Play first and second track and 10 seconds of third track:
			if ( CD_INDRIVE(SDL_CDStatus(cdrom)) )
				SDL_CDPlayTracks(cdrom, 0, 0, 2, 10);

		   This function returns 0, or -1 if there was an error.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_CDPlayTracks(/* SDL_CD * */IntPtr cdrom,
			int start_track, int start_frame, int ntracks, int nframes);

		/* Play the given CD starting at 'start' frame for 'length' frames.
		   It returns 0, or -1 if there was an error.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_CDPlay(/* SDL_CD * */IntPtr cdrom, int start, int length);

		/* Pause play -- returns 0, or -1 on error */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CDPause(/* SDL_CD * */IntPtr cdrom);

		/* Resume play -- returns 0, or -1 on error */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CDResume(/* SDL_CD * */IntPtr cdrom);

		/* Stop play -- returns 0, or -1 on error */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CDStop(/* SDL_CD * */IntPtr cdrom);

		/* Eject CD-ROM -- returns 0, or -1 on error */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CDEject(/* SDL_CD * */IntPtr cdrom);

		/* Closes the handle for the CD-ROM drive */
		[DllImport(DLL_SDL)]
		public static extern void SDL_CDClose(/* SDL_CD * */IntPtr cdrom);
		#endregion
		#region SDL_copying
		/*
			SDL - Simple DirectMedia Layer
			Copyright (C) 1997, 1998, 1999, 2000, 2001  Sam Lantinga

			This library is free software; you can redistribute it and/or
			modify it under the terms of the GNU Library General Public
			License as published by the Free Software Foundation; either
			version 2 of the License, or (at your option) any later version.

			This library is distributed in the hope that it will be useful,
			but WITHOUT ANY WARRANTY; without even the implied warranty of
			MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
			Library General Public License for more details.

			You should have received a copy of the GNU Library General Public
			License along with this library; if not, write to the Free
			Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

			Sam Lantinga
			slouken@devolution.com
		*/
		#endregion
		#region SDL_endian
		/* Use inline functions for compilers that support them, and static
			functions for those that do not.  Because these functions become
			static for compilers that do not support inline functions, this
			header should only be included in files that actually use them.
		*/

		public static Uint16 SDL_Swap16(Uint16 D) {
			return (Uint16)((D<<8)|(D>>8));
		}

		public static Uint32 SDL_Swap32(Uint32 D) {
			return((D<<24)|((D<<8)&0x00FF0000)|((D>>8)&0x0000FF00)|(D>>24));
		}

		public static Uint64 SDL_Swap64(Uint64 val) {
			Uint32 hi, lo;
			/* Separate into high and low 32-bit values and swap them */
			lo = (Uint32)(val&0xFFFFFFFF);
			val >>= 32;
			hi = (Uint32)(val&0xFFFFFFFF);
			val = SDL_Swap32(lo);
			val <<= 32;
			val |= SDL_Swap32(hi);
			return(val);
		}

		/* Byteswap item from the specified endianness to the native endianness */
		//#define SDL_SwapLE16(X)	(X)
		//#define SDL_SwapLE32(X)	(X)
		//#define SDL_SwapLE64(X)	(X)
		//#define SDL_SwapBE16(X)	SDL_Swap16(X)
		//#define SDL_SwapBE32(X)	SDL_Swap32(X)
		//#define SDL_SwapBE64(X)	SDL_Swap64(X)
		public static Uint16 SDL_SwapLE16(Uint16 X) { return SDL_Swap16(X); }
		public static Uint32 SDL_SwapLE32(Uint32 X) { return SDL_Swap32(X); }
		public static Uint64 SDL_SwapLE64(Uint64 X) { return SDL_Swap64(X); }
		public static Uint16 SDL_SwapBE16(Uint16 X) { return (X); }
		public static Uint32 SDL_SwapBE32(Uint32 X) { return (X); }
		public static Uint64 SDL_SwapBE64(Uint64 X) { return (X); }

		/* Read an item of the specified endianness and return in native format */
		[DllImport(DLL_SDL)]
		public static extern Uint16 SDL_ReadLE16(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL)]
		public static extern Uint16 SDL_ReadBE16(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_ReadLE32(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_ReadBE32(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL)]
		public static extern Uint64 SDL_ReadLE64(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL)]
		public static extern Uint64 SDL_ReadBE64(/* SDL_RWops * */IntPtr src);

		/* Write an item of native format to the specified endianness */
		[DllImport(DLL_SDL)]
		public static extern int SDL_WriteLE16(/* SDL_RWops * */IntPtr dst, Uint16 value);
		[DllImport(DLL_SDL)]
		public static extern int SDL_WriteBE16(/* SDL_RWops * */IntPtr dst, Uint16 value);
		[DllImport(DLL_SDL)]
		public static extern int SDL_WriteLE32(/* SDL_RWops * */IntPtr dst, Uint32 value);
		[DllImport(DLL_SDL)]
		public static extern int SDL_WriteBE32(/* SDL_RWops * */IntPtr dst, Uint32 value);
		[DllImport(DLL_SDL)]
		public static extern int SDL_WriteLE64(/* SDL_RWops * */IntPtr dst, Uint64 value);
		[DllImport(DLL_SDL)]
		public static extern int SDL_WriteBE64(/* SDL_RWops * */IntPtr dst, Uint64 value);
		#endregion
		#region SdlError
		/* Simple error message routines for SDL */

		/* Public functions */
		[DllImport(DLL_SDL)]
		public static extern void SDL_SetError(/*char * */string fmt, params object[] objs);
		[DllImport(DLL_SDL)]
		public static extern /*char * */string SDL_GetError();
		[DllImport(DLL_SDL)]
		public static extern void SDL_ClearError();

		/* Private error message function - used internally */
		//#define SDL_OutOfMemory()	SDL_Error(SDL_ENOMEM)

		public enum SDL_errorcode : int {
			SDL_ENOMEM,
			SDL_EFREAD,
			SDL_EFWRITE,
			SDL_EFSEEK,
			SDL_LASTERROR
		}
		[DllImport(DLL_SDL)]
		public static extern void SDL_Error(SDL_errorcode code);

		#endregion
		#region SDL_events
		/* Event enumerations */
		public const uint SDL_NOEVENT = 0;			/* Unused (do not remove) */
		public const uint SDL_ACTIVEEVENT = 1;			/* Application loses/gains visibility */
		public const uint SDL_KEYDOWN = 2;			/* Keys pressed */
		public const uint SDL_KEYUP = 3;			/* Keys released */
		public const uint SDL_MOUSEMOTION = 4;			/* Mouse moved */
		public const uint SDL_MOUSEBUTTONDOWN = 5;		/* Mouse Button pressed */
		public const uint SDL_MOUSEBUTTONUP = 6;		/* Mouse Button released */
		public const uint SDL_JOYAXISMOTION = 7;		/* Joystick axis motion */
		public const uint SDL_JOYBALLMOTION = 8;		/* Joystick trackball motion */
		public const uint SDL_JOYHATMOTION = 9;		/* Joystick hat position change */
		public const uint SDL_JOYBUTTONDOWN = 10;		/* Joystick Button pressed */
		public const uint SDL_JOYBUTTONUP = 11;			/* Joystick Button released */
		public const uint SDL_QUIT = 12;			/* User-requested quit */
		public const uint SDL_SYSWMEVENT = 12;			/* System specific event */
		public const uint SDL_EVENT_RESERVEDA = 13;		/* Reserved for future use.. */
		public const uint SDL_EVENT_RESERVEDB = 14;		/* Reserved for future use.. */
		public const uint SDL_VIDEORESIZE = 15;			/* User resized video mode */
		public const uint SDL_VIDEOEXPOSE = 16;			/* Screen needs to be redrawn */
		public const uint SDL_EVENT_RESERVED = 17;		/* Reserved for future use.. */
		public const uint SDL_EVENT_RESERVED3 = 18;		/* Reserved for future use.. */
		public const uint SDL_EVENT_RESERVED4 = 19;		/* Reserved for future use.. */
		public const uint SDL_EVENT_RESERVED5 = 20;		/* Reserved for future use.. */
		public const uint SDL_EVENT_RESERVED6 = 21;		/* Reserved for future use.. */
		public const uint SDL_EVENT_RESERVED7 = 22;		/* Reserved for future use.. */
		/* Events SDL_USEREVENT through SDL_MAXEVENTS-1 are for your use */
		public const uint SDL_USEREVENT = 24;
		/* This last event is only for bounding internal arrays
		   It is the number of bits in the event mask datatype -- Uint32
			 */
		public const uint SDL_NUMEVENTS = 32;

		/* Predefined event masks */
		public static uint SDL_EVENTMASK(uint X) { return (uint)(1 << (int)(X)); }

		public const uint SDL_ACTIVEEVENTMASK	= 1 << (int)SDL_ACTIVEEVENT;
		public const uint SDL_KEYDOWNMASK		= 1 << (int)SDL_KEYDOWN;
		public const uint SDL_KEYUPMASK		= 1 << (int)SDL_KEYUP;
		public const uint SDL_MOUSEMOTIONMASK	= 1 << (int)SDL_MOUSEMOTION;
		public const uint SDL_MOUSEBUTTONDOWNMASK	= 1 << (int)SDL_MOUSEBUTTONDOWN;
		public const uint SDL_MOUSEBUTTONUPMASK	= 1 << (int)SDL_MOUSEBUTTONUP;
		public const uint SDL_MOUSEEVENTMASK	= 
			(1 << (int)SDL_MOUSEMOTION) | (1 << (int)SDL_MOUSEBUTTONDOWN)|(1 << (int)SDL_MOUSEBUTTONUP);
		public const uint SDL_JOYAXISMOTIONMASK	= (1 << (int)SDL_JOYAXISMOTION);
		public const uint SDL_JOYBALLMOTIONMASK	= (1 << (int)SDL_JOYBALLMOTION);
		public const uint SDL_JOYHATMOTIONMASK	= (1 << (int)SDL_JOYHATMOTION);
		public const uint SDL_JOYBUTTONDOWNMASK	= (1 << (int)SDL_JOYBUTTONDOWN);
		public const uint SDL_JOYBUTTONUPMASK	= 1 << (int)SDL_JOYBUTTONUP;
		public const uint SDL_JOYEVENTMASK	= 
			(1 << (int)SDL_JOYAXISMOTION)|(1 << (int)SDL_JOYBALLMOTION)|(1 << (int)SDL_JOYHATMOTION)|
			(1 << (int)SDL_JOYBUTTONDOWN)|(1 << (int)SDL_JOYBUTTONUP);
		public const uint SDL_VIDEORESIZEMASK	= 1 << (int)SDL_VIDEORESIZE;
		public const uint SDL_VIDEOEXPOSEMASK	= 1 << (int)SDL_VIDEOEXPOSE;
		public const uint SDL_QUITMASK		= 1 << (int)SDL_QUIT;
		public const uint SDL_SYSWMEVENTMASK	= 1 << (int)SDL_SYSWMEVENT;
		public const uint SDL_ALLEVENTS	= 0xFFFFFFFF;

		/* Application visibility event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_ActiveEvent {
			public Uint8 type;	/* SDL_ACTIVEEVENT */
			public Uint8 gain;	/* Whether given states were gained or lost (1/0) */
			public Uint8 state;	/* A mask of the focus states */
		}

		/* Keyboard event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_KeyboardEvent {
			public Uint8 type;	/* SDL_KEYDOWN or SDL_KEYUP */
			public Uint8 which;	/* The keyboard device index */
			public Uint8 state;	/* SDL_PRESSED or SDL_RELEASED */
			public SDL_keysym keysym;
		}

		/* Mouse motion event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_MouseMotionEvent {
			public Uint8 type;	/* SDL_MOUSEMOTION */
			public Uint8 which;	/* The mouse device index */
			public Uint8 state;	/* The current Button state */
			public Uint16 x, y;	/* The X/Y coordinates of the mouse */
			public Sint16 xrel;	/* The relative motion in the X direction */
			public Sint16 yrel;	/* The relative motion in the Y direction */
		}

		/* Mouse Button event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_MouseButtonEvent {
			public Uint8 type;	/* SDL_MOUSEBUTTONDOWN or SDL_MOUSEBUTTONUP */
			public Uint8 which;	/* The mouse device index */
			public Uint8 button;	/* The mouse Button index */
			public Uint8 state;	/* SDL_PRESSED or SDL_RELEASED */
			public Uint16 x, y;	/* The X/Y coordinates of the mouse at press time */
		}

		/* Joystick axis motion event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_JoyAxisEvent {
			public Uint8 type;	/* SDL_JOYAXISMOTION */
			public Uint8 which;	/* The joystick device index */
			public Uint8 axis;	/* The joystick axis index */
			public Sint16 value;	/* The axis value (range: -32768 to 32767) */
		}

		/* Joystick trackball motion event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_JoyBallEvent {
			public Uint8 type;	/* SDL_JOYBALLMOTION */
			public Uint8 which;	/* The joystick device index */
			public Uint8 ball;	/* The joystick trackball index */
			public Sint16 xrel;	/* The relative motion in the X direction */
			public Sint16 yrel;	/* The relative motion in the Y direction */
		}

		/* Joystick hat position change event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_JoyHatEvent {
			public Uint8 type;	/* SDL_JOYHATMOTION */
			public Uint8 which;	/* The joystick device index */
			public Uint8 hat;	/* The joystick hat index */
			public Uint8 value;	/* The hat position value:
				8   1   2
				7   0   3
				6   5   4
			   Note that zero means the POV is centered.
			*/
		}

		/* Joystick Button event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_JoyButtonEvent {
			public Uint8 type;	/* SDL_JOYBUTTONDOWN or SDL_JOYBUTTONUP */
			public Uint8 which;	/* The joystick device index */
			public Uint8 button;	/* The joystick Button index */
			public Uint8 state;	/* SDL_PRESSED or SDL_RELEASED */
		}

		/* The "window resized" event
		   When you get this event, you are responsible for setting a new video
		   mode with the new width and height.
		 */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_ResizeEvent {
			public Uint8 type;	/* SDL_VIDEORESIZE */
			public int w;		/* New width */
			public int h;		/* New height */
		}

		/* The "screen redraw" event */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_ExposeEvent {
			public Uint8 type;	/* SDL_VIDEOEXPOSE */
		}

		/* The "quit requested" event */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_QuitEvent {
			public Uint8 type;	/* SDL_QUIT */
		}

		/* A user-defined event type */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_UserEvent {
			public Uint8 type;	/* SDL_USEREVENT through SDL_NUMEVENTS-1 */
			public int code;	/* User defined event code */
			public /*void * */IntPtr data1;	/* User defined data pointer */
			public /*void * */IntPtr data2;	/* User defined data pointer */
		}

		/* If you want to use this event, you should include SDL_syswm.h */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_SysWMEvent {
			public Uint8 type;
			public /*SDL_SysWMmsg * */IntPtr msg;
		}

		/* General event structure */
		[StructLayout(LayoutKind.Explicit, Pack = 4)]
		public struct SDL_Event {
			[FieldOffset(0)] public Uint8 type;
			[FieldOffset(0)] public SDL_ActiveEvent active;
			[FieldOffset(0)] public SDL_KeyboardEvent key;
			[FieldOffset(0)] public SDL_MouseMotionEvent motion;
			[FieldOffset(0)] public SDL_MouseButtonEvent button;
			[FieldOffset(0)] public SDL_JoyAxisEvent jaxis;
			[FieldOffset(0)] public SDL_JoyBallEvent jball;
			[FieldOffset(0)] public SDL_JoyHatEvent jhat;
			[FieldOffset(0)] public SDL_JoyButtonEvent jbutton;
			[FieldOffset(0)] public SDL_ResizeEvent resize;
			[FieldOffset(0)] public SDL_ExposeEvent expose;
			[FieldOffset(0)] public SDL_QuitEvent quit;
			[FieldOffset(0)] public SDL_UserEvent user;
			[FieldOffset(0)] public SDL_SysWMEvent syswm;
		}

		/* Function prototypes */

		/* Pumps the event loop, gathering events from the input devices.
		This function updates the event queue and internal input device state.
		This should only be run in the thread that sets the video mode.
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_PumpEvents();

		/* Checks the event queue for messages and optionally returns them.
		If 'action' is SDL_ADDEVENT, up to 'numevents' events will be added to
		the back of the event queue.
		If 'action' is SDL_PEEKEVENT, up to 'numevents' events at the front
		of the event queue, matching 'mask', will be returned and will not
		be removed from the queue.
		If 'action' is SDL_GETEVENT, up to 'numevents' events at the front 
		of the event queue, matching 'mask', will be returned and will be
		removed from the queue.
		This function returns the number of events actually stored, or -1
		if there was an error.  This function is thread-safe.
		*/
		public enum SDL_eventaction : int {
			SDL_ADDEVENT,
			SDL_PEEKEVENT,
			SDL_GETEVENT
		}
		/* */
		[DllImport(DLL_SDL)]
		public static extern int SDL_PeepEvents(/* SDL_Event * */SDL_Event[] events, int numevents, SDL_eventaction action, Uint32 mask);

		/* Polls for currently pending events, and returns 1 if there are any pending
			events, or 0 if there are none available.  If 'event' is not NULL, the next
			event is removed from the queue and stored in that area.
			*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_PollEvent(ref SDL_Event event_);

		/* Waits indefinitely for the next available event, returning 1, or 0 if there
			was an error while waiting for events.  If 'event' is not NULL, the next
			event is removed from the queue and stored in that area.
			*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_WaitEvent(/* SDL_Event * */ref SDL_Event event_);

		/* Add an event to the event queue.
			This function returns 0, or -1 if the event couldn't be added to
			the event queue.  If the event queue is full, this function fails.
			*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_PushEvent(/* SDL_Event * */ref SDL_Event event_);

		/*
			This function sets up a filter to process all events before they
			change internal state and are posted to the internal event queue.

			The filter is protypted as:
		*/
		public delegate int SDL_EventFilter(/*SDL_Event */ref SDL_Event event_);
		/*
			If the filter returns 1, then the event will be added to the internal queue.
			If it returns 0, then the event will be dropped from the queue, but the 
			internal state will still be updated.  This allows selective filtering of
			dynamically arriving events.

			WARNING:  Be very careful of what you do in the event filter function, as 
					it may run in a different thread!

			There is one caveat when dealing with the SDL_QUITEVENT event type.  The
			event filter is only called when the window manager desires to close the
			application window.  If the event filter returns 1, then the window will
			be closed, otherwise the window will remain open if possible.
			If the quit event is generated by an interrupt signal, it will bypass the
			internal queue and be delivered to the application at the next event poll.
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_SetEventFilter(SDL_EventFilter filter);

		/*
			Return the current event filter - can be used to "chain" filters.
			If there is no event filter set, this function returns NULL.
		*/
		[DllImport(DLL_SDL)]
		public static extern SDL_EventFilter SDL_GetEventFilter();

		/*
			This function allows you to set the state of processing certain events.
			If 'state' is set to SDL_IGNORE, that event will be automatically dropped
			from the event queue and will not event be filtered.
			If 'state' is set to SDL_ENABLE, that event will be processed normally.
			If 'state' is set to SDL_QUERY, SDL_EventState() will return the 
			current processing state of the specified event.
		*/
		public const uint SDL_QUERY	= uint.MaxValue;
		public const uint SDL_IGNORE	= 0;
		public const uint SDL_DISABLE	= 0;
		public const uint SDL_ENABLE	= 1;
		[DllImport(DLL_SDL)]
		public static extern Uint8 SDL_EventState(Uint8 type, int state);
		#endregion
		#region SDL_getenv
		/* Put a variable of the form "name=value" into the environment */
		[DllImport(DLL_SDL)]
		public static extern int SDL_putenv(/*char * */string variable);
		public static int putenv(/* char* */string X) { return SDL_putenv(X); }

		/* Retrieve a variable named "name" from the environment */
		[DllImport(DLL_SDL)]
		public static extern /* char * */string SDL_getenv(/* char * */string name);
		public static /* char * */string getenv(/* char* */string X) { return SDL_getenv(X); }
		#endregion
		#region SDL_image
		/* Load an image from an SDL data source.
		The 'type' may be one of: "BMP", "GIF", "PNG", etc.

		If the image format supports a transparent pixel, SDL will set the
		colorkey for the surface.  You can enable RLE acceleration on the
		surface afterwards by calling:
		SDL_SetColorKey(image, SDL_RLEACCEL, image->format->colorkey);
		*/
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr  IMG_LoadTyped_RW(/* SDL_RWops * */IntPtr src, int freesrc, /* char * */ string type);
		/* Convenience functions */
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_Load(/* char * */string file);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_Load_RW(/* SDL_RWops * */IntPtr src, int freesrc);

		/* Invert the alpha of a surface for use with OpenGL
		   This function is now a no-op, and only provided for backwards compatibility.
		*/
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_InvertAlpha(int on);

		/* Functions to detect a file type, given a seekable source */
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isBMP(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isPNM(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isXPM(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isXCF(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isPCX(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isGIF(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isJPG(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isTIF(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isPNG(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern int IMG_isLBM(/* SDL_RWops * */IntPtr src);

		/* Individual loading functions */
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadBMP_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadPNM_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadXPM_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadXCF_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadPCX_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadGIF_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadJPG_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadTIF_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadPNG_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadTGA_RW(/* SDL_RWops * */IntPtr src);
		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_LoadLBM_RW(/* SDL_RWops * */IntPtr src);

		[DllImport(DLL_SDL_IMAGE)]
		public static extern /* SDL_Surface * */IntPtr IMG_ReadXPMFromArray(/* char ** */IntPtr xpm);

		/* We'll use SDL for reporting errors */
		//#define IMG_SetError	SDL_SetError
		public static /*char* */string IMG_GetError() {
			return SDL_GetError();
		}

		#endregion
		#region SDL_joystick
		/* In order to use these functions, SDL_Init() must have been called
		with the SDL_INIT_JOYSTICK flag.  This causes SDL to scan the system
		for joysticks, and load appropriate drivers.
		*/

		/* The joystick structure used to identify an SDL joystick */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Joystick { }

		/* Function prototypes */
		/*
		 * Count the number of joysticks attached to the system
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_NumJoysticks();

		/*
		 * Get the implementation dependent name of a joystick.
		 * This can be called before any joysticks are opened.
		 * If no name can be found, this function returns NULL.
		 */
		[DllImport(DLL_SDL)]		
		public static extern /*char * */string SDL_JoystickName(int device_index);

		/*
		 * Open a joystick for use - the index passed as an argument refers to
		 * the N'th joystick on the system.  This index is the value which will
		 * identify this joystick in future joystick events.
		 * 
		 * This function returns a joystick identifier, or NULL if an error occurred.
		 */
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Joystick * */IntPtr SDL_JoystickOpen(int device_index);

		/*
		 * Returns 1 if the joystick has been opened, or 0 if it has not.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickOpened(int device_index);

		/*
		 * Get the device index of an opened joystick.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickIndex(/* SDL_Joystick * */IntPtr joystick);

		/*
		 * Get the number of general axis controls on a joystick
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickNumAxes(/* SDL_Joystick * */IntPtr joystick);

		/*
		 * Get the number of trackballs on a joystick
		 * Joystick trackballs have only relative motion events associated
		 * with them and their state cannot be polled.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickNumBalls(/* SDL_Joystick * */IntPtr joystick);

		/*
		 * Get the number of POV hats on a joystick
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickNumHats(/* SDL_Joystick * */IntPtr joystick);

		/*
		 * Get the number of buttons on a joystick
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickNumButtons(/* SDL_Joystick * */IntPtr joystick);

		/*
		 * Update the current state of the open joysticks.
		 * This is called automatically by the event loop if any joystick
		 * events are enabled.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_JoystickUpdate();

		/*
		 * Enable/disable joystick event polling.
		 * If joystick events are disabled, you must call SDL_JoystickUpdate()
		 * yourself and check the state of the joystick when you want joystick
		 * information.
		 * The state can be one of SDL_QUERY, SDL_ENABLE or SDL_IGNORE.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickEventState(int state);

		/*
		 * Get the current state of an axis control on a joystick
		 * The state is a value ranging from -32768 to 32767.
		 * The axis indices start at index 0.
		 */
		[DllImport(DLL_SDL)]
		public static extern Sint16 SDL_JoystickGetAxis(/* SDL_Joystick * */IntPtr joystick, int axis);

		/*
		 * Get the current state of a POV hat on a joystick
		 * The return value is one of the following positions:
		 */
		public const uint SDL_HAT_CENTERED	= 0x00;
		public const uint SDL_HAT_UP		= 0x01;
		public const uint SDL_HAT_RIGHT	= 0x02;
		public const uint SDL_HAT_DOWN		= 0x04;
		public const uint SDL_HAT_LEFT		= 0x08;
		public const uint SDL_HAT_RIGHTUP		= (SDL_HAT_RIGHT|SDL_HAT_UP);
		public const uint SDL_HAT_RIGHTDOWN	= (SDL_HAT_RIGHT|SDL_HAT_DOWN);
		public const uint SDL_HAT_LEFTUP		= (SDL_HAT_LEFT|SDL_HAT_UP);
		public const uint SDL_HAT_LEFTDOWN		= (SDL_HAT_LEFT|SDL_HAT_DOWN);
		/*
		 * The hat indices start at index 0.
		 */
		[DllImport(DLL_SDL)]
		public static extern Uint8 SDL_JoystickGetHat(/* SDL_Joystick * */IntPtr joystick, int hat);

		/*
		 * Get the ball axis change since the last poll
		 * This returns 0, or -1 if you passed it invalid parameters.
		 * The ball indices start at index 0.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_JoystickGetBall(/* SDL_Joystick * */IntPtr joystick, int ball, /* int * */ref int dx, /* int * */ref int dy);

		/*
		 * Get the current state of a Button on a joystick
		 * The Button indices start at index 0.
		 */
		[DllImport(DLL_SDL)]
		public static extern Uint8 SDL_JoystickGetButton(/* SDL_Joystick * */IntPtr joystick, int button);

		/*
		 * Close a joystick previously opened with SDL_JoystickOpen()
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_JoystickClose(/* SDL_Joystick * */IntPtr joystick);
		#endregion
		#region SDL_keyboard
		/* Keysym structure
		- The scancode is hardware dependent, and should not be used by general
			applications.	If no hardware scancode is available, it will be 0.

		- The 'unicode' translated character is only available when character
			translation is enabled by the SDL_EnableUNICODE() API.  If non-zero,
			this is a UNICODE character corresponding to the keypress.  If the
			high 9 bits of the character are 0, then this maps to the equivalent
			ASCII character:
			char ch;
			if ( (keysym.unicode & 0xFF80) == 0 ) {
				ch = keysym.unicode & 0x7F;
			} else {
				An international character..
			}
		*/
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_keysym {
			public Uint8 scancode;			/* hardware specific scancode */
			public SDLKey sym;			/* SDL virtual keysym */
			public SDLMod mod;			/* current key modifiers */
			public Uint16 unicode;			/* translated character */
		}

		/* This is the mask which refers to all hotkey bindings */
		public const uint SDL_ALL_HOTKEYS		= 0xFFFFFFFF;

		/* Function prototypes */
		/*
		 * Enable/Disable UNICODE translation of keyboard input.
		 * This translation has some overhead, so translation defaults off.
		 * If 'enable' is 1, translation is enabled.
		 * If 'enable' is 0, translation is disabled.
		 * If 'enable' is -1, the translation state is not changed.
		 * It returns the previous state of keyboard translation.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_EnableUNICODE(int enable);

		/*
		 * Enable/Disable keyboard repeat.	Keyboard repeat defaults to off.
		 * 'delay' is the initial delay in ms between the time when a key is
		 * pressed, and keyboard repeat begins.
		 * 'interval' is the time in ms between keyboard repeat events.
		 */
		public const uint SDL_DEFAULT_REPEAT_DELAY		= 500;
		public const uint SDL_DEFAULT_REPEAT_INTERVAL	= 30;
		/*
		 * If 'delay' is set to 0, keyboard repeat is disabled.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_EnableKeyRepeat(int delay, int interval);

		/*
		 * Get a snapshot of the current state of the keyboard.
		 * Returns an array of keystates, indexed by the SDLK_* syms.
		 * Used:
		 * 	Uint8 *keystate = SDL_GetKeyState(NULL);
		 *	if ( keystate[SDLK_RETURN] ) ... <RETURN> is pressed.
		 */
		[DllImport(DLL_SDL)]
		public static extern /* Uint8 * */IntPtr SDL_GetKeyState(/* int * */IntPtr numkeys);

		/*
		 * Get the current key modifier state
		 */
		[DllImport(DLL_SDL)]
		public static extern SDLMod SDL_GetModState();

		/*
		 * Set the current key modifier state
		 * This does not change the keyboard state, only the key modifier flags.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_SetModState(SDLMod modstate);

		/*
		 * Get the name of an SDL virtual keysym
		 */
		[DllImport(DLL_SDL)]
		public static extern /* char * */ string SDL_GetKeyName(SDLKey key);
		#endregion
		#region SDL_keysym
		/* What we really want is a mapping of every raw key on the keyboard.
			To support international keyboards, we use the range 0xA1 - 0xFF
			as international virtual keycodes.  We'll follow in the footsteps of X11...
			The names of the keys
		*/
 
		public enum SDLKey : int {
			/* The keyboard syms have been cleverly chosen to map to ASCII */
			SDLK_UNKNOWN		= 0,
			SDLK_FIRST		= 0,
			SDLK_BACKSPACE		= 8,
			SDLK_TAB		= 9,
			SDLK_CLEAR		= 12,
			SDLK_RETURN		= 13,
			SDLK_PAUSE		= 19,
			SDLK_ESCAPE		= 27,
			SDLK_SPACE		= 32,
			SDLK_EXCLAIM		= 33,
			SDLK_QUOTEDBL		= 34,
			SDLK_HASH		= 35,
			SDLK_DOLLAR		= 36,
			SDLK_AMPERSAND		= 38,
			SDLK_QUOTE		= 39,
			SDLK_LEFTPAREN		= 40,
			SDLK_RIGHTPAREN		= 41,
			SDLK_ASTERISK		= 42,
			SDLK_PLUS		= 43,
			SDLK_COMMA		= 44,
			SDLK_MINUS		= 45,
			SDLK_PERIOD		= 46,
			SDLK_SLASH		= 47,
			SDLK_0			= 48,
			SDLK_1			= 49,
			SDLK_2			= 50,
			SDLK_3			= 51,
			SDLK_4			= 52,
			SDLK_5			= 53,
			SDLK_6			= 54,
			SDLK_7			= 55,
			SDLK_8			= 56,
			SDLK_9			= 57,
			SDLK_COLON		= 58,
			SDLK_SEMICOLON		= 59,
			SDLK_LESS		= 60,
			SDLK_EQUALS		= 61,
			SDLK_GREATER		= 62,
			SDLK_QUESTION		= 63,
			SDLK_AT			= 64,
			/* 
			   Skip uppercase letters
			 */
			SDLK_LEFTBRACKET	= 91,
			SDLK_BACKSLASH		= 92,
			SDLK_RIGHTBRACKET	= 93,
			SDLK_CARET		= 94,
			SDLK_UNDERSCORE		= 95,
			SDLK_BACKQUOTE		= 96,
			SDLK_a			= 97,
			SDLK_b			= 98,
			SDLK_c			= 99,
			SDLK_d			= 100,
			SDLK_e			= 101,
			SDLK_f			= 102,
			SDLK_g			= 103,
			SDLK_h			= 104,
			SDLK_i			= 105,
			SDLK_j			= 106,
			SDLK_k			= 107,
			SDLK_l			= 108,
			SDLK_m			= 109,
			SDLK_n			= 110,
			SDLK_o			= 111,
			SDLK_p			= 112,
			SDLK_q			= 113,
			SDLK_r			= 114,
			SDLK_s			= 115,
			SDLK_t			= 116,
			SDLK_u			= 117,
			SDLK_v			= 118,
			SDLK_w			= 119,
			SDLK_x			= 120,
			SDLK_y			= 121,
			SDLK_z			= 122,
			SDLK_DELETE		= 127,
			/* End of ASCII mapped keysyms */

			/* International keyboard syms */
			SDLK_WORLD_0		= 160,		/* 0xA0 */
			SDLK_WORLD_1		= 161,
			SDLK_WORLD_2		= 162,
			SDLK_WORLD_3		= 163,
			SDLK_WORLD_4		= 164,
			SDLK_WORLD_5		= 165,
			SDLK_WORLD_6		= 166,
			SDLK_WORLD_7		= 167,
			SDLK_WORLD_8		= 168,
			SDLK_WORLD_9		= 169,
			SDLK_WORLD_10		= 170,
			SDLK_WORLD_11		= 171,
			SDLK_WORLD_12		= 172,
			SDLK_WORLD_13		= 173,
			SDLK_WORLD_14		= 174,
			SDLK_WORLD_15		= 175,
			SDLK_WORLD_16		= 176,
			SDLK_WORLD_17		= 177,
			SDLK_WORLD_18		= 178,
			SDLK_WORLD_19		= 179,
			SDLK_WORLD_20		= 180,
			SDLK_WORLD_21		= 181,
			SDLK_WORLD_22		= 182,
			SDLK_WORLD_23		= 183,
			SDLK_WORLD_24		= 184,
			SDLK_WORLD_25		= 185,
			SDLK_WORLD_26		= 186,
			SDLK_WORLD_27		= 187,
			SDLK_WORLD_28		= 188,
			SDLK_WORLD_29		= 189,
			SDLK_WORLD_30		= 190,
			SDLK_WORLD_31		= 191,
			SDLK_WORLD_32		= 192,
			SDLK_WORLD_33		= 193,
			SDLK_WORLD_34		= 194,
			SDLK_WORLD_35		= 195,
			SDLK_WORLD_36		= 196,
			SDLK_WORLD_37		= 197,
			SDLK_WORLD_38		= 198,
			SDLK_WORLD_39		= 199,
			SDLK_WORLD_40		= 200,
			SDLK_WORLD_41		= 201,
			SDLK_WORLD_42		= 202,
			SDLK_WORLD_43		= 203,
			SDLK_WORLD_44		= 204,
			SDLK_WORLD_45		= 205,
			SDLK_WORLD_46		= 206,
			SDLK_WORLD_47		= 207,
			SDLK_WORLD_48		= 208,
			SDLK_WORLD_49		= 209,
			SDLK_WORLD_50		= 210,
			SDLK_WORLD_51		= 211,
			SDLK_WORLD_52		= 212,
			SDLK_WORLD_53		= 213,
			SDLK_WORLD_54		= 214,
			SDLK_WORLD_55		= 215,
			SDLK_WORLD_56		= 216,
			SDLK_WORLD_57		= 217,
			SDLK_WORLD_58		= 218,
			SDLK_WORLD_59		= 219,
			SDLK_WORLD_60		= 220,
			SDLK_WORLD_61		= 221,
			SDLK_WORLD_62		= 222,
			SDLK_WORLD_63		= 223,
			SDLK_WORLD_64		= 224,
			SDLK_WORLD_65		= 225,
			SDLK_WORLD_66		= 226,
			SDLK_WORLD_67		= 227,
			SDLK_WORLD_68		= 228,
			SDLK_WORLD_69		= 229,
			SDLK_WORLD_70		= 230,
			SDLK_WORLD_71		= 231,
			SDLK_WORLD_72		= 232,
			SDLK_WORLD_73		= 233,
			SDLK_WORLD_74		= 234,
			SDLK_WORLD_75		= 235,
			SDLK_WORLD_76		= 236,
			SDLK_WORLD_77		= 237,
			SDLK_WORLD_78		= 238,
			SDLK_WORLD_79		= 239,
			SDLK_WORLD_80		= 240,
			SDLK_WORLD_81		= 241,
			SDLK_WORLD_82		= 242,
			SDLK_WORLD_83		= 243,
			SDLK_WORLD_84		= 244,
			SDLK_WORLD_85		= 245,
			SDLK_WORLD_86		= 246,
			SDLK_WORLD_87		= 247,
			SDLK_WORLD_88		= 248,
			SDLK_WORLD_89		= 249,
			SDLK_WORLD_90		= 250,
			SDLK_WORLD_91		= 251,
			SDLK_WORLD_92		= 252,
			SDLK_WORLD_93		= 253,
			SDLK_WORLD_94		= 254,
			SDLK_WORLD_95		= 255,		/* 0xFF */

			/* Numeric keypad */
			SDLK_KP0		= 256,
			SDLK_KP1		= 257,
			SDLK_KP2		= 258,
			SDLK_KP3		= 259,
			SDLK_KP4		= 260,
			SDLK_KP5		= 261,
			SDLK_KP6		= 262,
			SDLK_KP7		= 263,
			SDLK_KP8		= 264,
			SDLK_KP9		= 265,
			SDLK_KP_PERIOD		= 266,
			SDLK_KP_DIVIDE		= 267,
			SDLK_KP_MULTIPLY	= 268,
			SDLK_KP_MINUS		= 269,
			SDLK_KP_PLUS		= 270,
			SDLK_KP_ENTER		= 271,
			SDLK_KP_EQUALS		= 272,

			/* Arrows + Home/End pad */
			SDLK_UP			= 273,
			SDLK_DOWN		= 274,
			SDLK_RIGHT		= 275,
			SDLK_LEFT		= 276,
			SDLK_INSERT		= 277,
			SDLK_HOME		= 278,
			SDLK_END		= 279,
			SDLK_PAGEUP		= 280,
			SDLK_PAGEDOWN		= 281,

			/* Function keys */
			SDLK_F1			= 282,
			SDLK_F2			= 283,
			SDLK_F3			= 284,
			SDLK_F4			= 285,
			SDLK_F5			= 286,
			SDLK_F6			= 287,
			SDLK_F7			= 288,
			SDLK_F8			= 289,
			SDLK_F9			= 290,
			SDLK_F10		= 291,
			SDLK_F11		= 292,
			SDLK_F12		= 293,
			SDLK_F13		= 294,
			SDLK_F14		= 295,
			SDLK_F15		= 296,

			/* Key state modifier keys */
			SDLK_NUMLOCK		= 300,
			SDLK_CAPSLOCK		= 301,
			SDLK_SCROLLOCK		= 302,
			SDLK_RSHIFT		= 303,
			SDLK_LSHIFT		= 304,
			SDLK_RCTRL		= 305,
			SDLK_LCTRL		= 306,
			SDLK_RALT		= 307,
			SDLK_LALT		= 308,
			SDLK_RMETA		= 309,
			SDLK_LMETA		= 310,
			SDLK_LSUPER		= 311,		/* Left "Windows" key */
			SDLK_RSUPER		= 312,		/* Right "Windows" key */
			SDLK_MODE		= 313,		/* "Alt Gr" key */
			SDLK_COMPOSE		= 314,		/* Multi-key compose key */

			/* Miscellaneous function keys */
			SDLK_HELP		= 315,
			SDLK_PRINT		= 316,
			SDLK_SYSREQ		= 317,
			SDLK_BREAK		= 318,
			SDLK_MENU		= 319,
			SDLK_POWER		= 320,		/* Power Macintosh power key */
			SDLK_EURO		= 321,		/* Some european keyboards */
			SDLK_UNDO		= 322,		/* Atari keyboard has Undo */

			/* Add any other keys here */

			SDLK_LAST
		}

		/* Enumeration of valid key mods (possibly OR'd together) */
		[Flags]
		public enum SDLMod : int {
			KMOD_NONE  = 0x0000,
			KMOD_LSHIFT= 0x0001,
			KMOD_RSHIFT= 0x0002,
			KMOD_LCTRL = 0x0040,
			KMOD_RCTRL = 0x0080,
			KMOD_LALT  = 0x0100,
			KMOD_RALT  = 0x0200,
			KMOD_LMETA = 0x0400,
			KMOD_RMETA = 0x0800,
			KMOD_NUM   = 0x1000,
			KMOD_CAPS  = 0x2000,
			KMOD_MODE  = 0x4000,
			KMOD_RESERVED = 0x8000
		}

		public const SDLMod KMOD_CTRL	= (SDLMod.KMOD_LCTRL|SDLMod.KMOD_RCTRL);
		public const SDLMod KMOD_SHIFT	= (SDLMod.KMOD_LSHIFT|SDLMod.KMOD_RSHIFT);
		public const SDLMod KMOD_ALT		= (SDLMod.KMOD_LALT|SDLMod.KMOD_RALT);
		public const SDLMod KMOD_META	= (SDLMod.KMOD_LMETA|SDLMod.KMOD_RMETA);
		#endregion
		#region SDL_mixer
		/* Printable format: "%d.%d.%d", MAJOR, MINOR, PATCHLEVEL
		 */
		public const int MIX_MAJOR_VERSION = 1;
		public const int MIX_MINOR_VERSION = 2;
		public const int MIX_PATCHLEVEL = 5;

		/* This function gets the version of the dynamically linked SDL_mixer library.
		   it should NOT be used to fill a version structure, instead you should
		   use the MIX_VERSION() macro.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern /* SDL_version * */IntPtr Mix_Linked_Version();


		/* The default mixer has 8 simultaneous mixing channels */
		public const int MIX_CHANNELS = 8;

		/* Good default values for a PC soundcard */
		public const int MIX_DEFAULT_FREQUENCY = 22050;
		public const int MIX_DEFAULT_FORMAT = (int)AUDIO_S16LSB;
		public const int MIX_DEFAULT_CHANNELS = 2;
		public const int MIX_MAX_VOLUME = 128; /* Volume of a chunk */

		/* The internal format for an audio chunk */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct Mix_Chunk {
			public int allocated;
			public /* Uint8 * */IntPtr abuf;
			public Uint32 alen;
			public Uint8 volume;		/* Per-sample volume, 0-128 */
		}

		/* The different fading types supported */
		public enum Mix_Fading : int {
			MIX_NO_FADING,
			MIX_FADING_OUT,
			MIX_FADING_IN
		}

		public enum Mix_MusicType : int {
			MUS_NONE,
			MUS_CMD,
			MUS_WAV,
			MUS_MOD,
			MUS_MID,
			MUS_OGG,
			MUS_MP3
		}

		/* The internal format for a music chunk interpreted via mikmod */
		/* it'name mayby enough */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct Mix_Music {}

		/* Open the mixer with a certain audio format */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_OpenAudio(int frequency, Uint16 format, int channels, int chunksize);

		/* Dynamically change the number of channels managed by the mixer.
		   If decreasing the number of channels, the upper channels are
		   stopped.
		   This function returns the new number of allocated channels.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_AllocateChannels(int numchans);

		/* Find out what the actual audio device parameters are.
		   This function returns 1 if the audio has been opened, 0 otherwise.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_QuerySpec(/* int * */ref int frequency,/* Uint16 * */ref Uint16 format,/* int * */ref int channels);

		/* Load a wave file or a music (.mod .s3m .it .xm) file */
		[DllImport(DLL_SDL_MIXER)]
		public static extern /* Mix_Chunk * */IntPtr Mix_LoadWAV_RW(/* SDL_RWops * */IntPtr src, int freesrc);

		public static /* Mix_Chunk * */IntPtr Mix_LoadWAV(/* char * */string file) {
			return Mix_LoadWAV_RW(SDL_RWFromFile(file, "rb"), 1);
		}

		[DllImport(DLL_SDL_MIXER)]
		public static extern /* Mix_Music * */IntPtr  Mix_LoadMUS(/* char * */string file);


//		// 新しいバージョンのSDL_mixerでは標準でこのメソッドが使える。
//		[DllImport(DLL_SDL_MIXER)]
//		public static extern /* Mix_Chunk * */IntPtr Mix_LoadMUS_RW(/* SDL_RWops * */IntPtr src);


		/* Load a wave file of the mixer format from a memory buffer */
		[DllImport(DLL_SDL_MIXER)]
		public static extern /* Mix_Chunk * */IntPtr Mix_QuickLoad_WAV(/* Uint8 * */IntPtr mem);

		/* Load raw audio data of the mixer format from a memory buffer */
		[DllImport(DLL_SDL_MIXER)]
		public static extern /* Mix_Chunk * */IntPtr Mix_QuickLoad_RAW(/* Uint8 * */IntPtr mem, Uint32 len);

		/* Free an audio chunk previously loaded */
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_FreeChunk(/* Mix_Chunk * */IntPtr chunk);
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_FreeMusic(/* Mix_Music * */IntPtr music);

		/* Find out the music format of a mixer music, or the currently playing
		   music, if 'music' is NULL.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern Mix_MusicType Mix_GetMusicType(/* Mix_Music * */IntPtr music);

		/* Set a function that is called after all mixing is performed.
		   This can be used to provide real-time visual display of the audio stream
		   or add a custom mixer filter for the stream data.
		*/
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)] // 追加 by aki.
		public delegate void mix_func_t(/* void * */IntPtr udata, /* Uint8 * */IntPtr stream, int len);
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_SetPostMix(mix_func_t mix_func, /* void * */IntPtr arg);

		/* Add your own music player or additional mixer function.
		   If 'mix_hook' is NULL, the default music player is re-enabled.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_HookMusic(mix_func_t mix_func, /* void * */IntPtr arg);

		/* Add your own callback when the music has finished playing.
		   This callback is only called if the music finishes naturally.
		*/
		public delegate void music_finished_t();
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_HookMusicFinished(music_finished_t music_finished);

		/* Get a pointer to the user data for the current music hook */
		[DllImport(DLL_SDL_MIXER)]
		public static extern /* void * */IntPtr Mix_GetMusicHookData();

		/*
		 * Add your own callback when a channel has finished playing. NULL
		 *	to disable callback. The callback may be called from the mixer'name audio	
		 *	callback or it could be called as a result of Mix_HaltChannel(), etc.
		 *	do not call SDL_LockAudio() from this callback; you will either be	
		 *	inside the audio callback, or SDL_mixer will explicitly lock the audio
		 *	before calling your callback.
		 */
		public delegate void channel_finished_t(int channel);
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_ChannelFinished(channel_finished_t channel_finished);


		/* Special Effects API by ryan c. gordon. (icculus@linuxgames.com) */

		public const int MIX_CHANNEL_POST = -2;

		/* This is the format of a special effect callback:
		 *
		 *	 myeffect(int chan, void *stream, int len, void *udata);
		 *
		 * (chan) is the channel number that your effect is affecting. (stream) is
		 *	the buffer of data to work upon. (len) is the size of (stream), and
		 *	(udata) is a user-defined bit of data, which you pass as the last arg of
		 *	Mix_RegisterEffect(), and is passed back unmolested to your callback.
		 *	Your effect changes the contents of (stream) based on whatever parameters
		 *	are significant, or just leaves it be, if you prefer. You can do whatever
		 *	you like to the buffer, though, and it will continue in its changed state
		 *	down the mixing pipeline, through any other effect functions, then finally
		 *	to be mixed with the rest of the channels and music for the final output
		 *	stream.
		 *
		 * DO NOT EVER call SDL_LockAudio() from your callback function!
		 */
		public delegate void Mix_EffectFunc_t(int chan, /* void * */IntPtr stream, int len, /* void * */IntPtr udata);

		/*
		 * This is a callback that signifies that a channel has finished all its
		 *	loops and has completed playback. This gets called if the buffer
		 *	plays out normally, or if you call Mix_HaltChannel(), implicitly stop
		 *	a channel via Mix_AllocateChannels(), or unregister a callback while
		 *	it'name still playing.
		 *
		 * DO NOT EVER call SDL_LockAudio() from your callback function!
		 */
		public delegate void Mix_EffectDone_t(int chan, /* void * */IntPtr udata);


		/* Register a special effect function. At mixing time, the channel data is
		 *	copied into a buffer and passed through each registered effect function.
		 *	After it passes through all the functions, it is mixed into the final
		 *	output stream. The copy to buffer is performed once, then each effect
		 *	function performs on the output of the previous effect. Understand that
		 *	this extra copy to a buffer is not performed if there are no effects
		 *	registered for a given chunk, which saves CPU cycles, and any given
		 *	effect will be extra cycles, too, so it is crucial that your code run
		 *	fast. Also note that the data that your function is given is in the
		 *	format of the sound device, and not the format you gave to Mix_OpenAudio(),
		 *	although they may in reality be the same. This is an unfortunate but
		 *	necessary speed concern. Use Mix_QuerySpec() to determine if you can
		 *	handle the data before you register your effect, and take appropriate
		 *	actions.
		 * You may also specify a callback (Mix_EffectDone_t) that is called when
		 *	the channel finishes playing. This gives you a more fine-grained control
		 *	than Mix_ChannelFinished(), in case you need to free effect-specific
		 *	resources, etc. If you don't need this, you can specify NULL.
		 * You may set the callbacks before or after calling Mix_PlayChannel().
		 * Things like Mix_SetPanning() are just internal special effect functions,
		 *	so if you are using that, you've already incurred the overhead of a copy
		 *	to a separate buffer, and that these effects will be in the queue with
		 *	any functions you've registered. The list of registered effects for a
		 *	channel is Reset when a chunk finishes playing, so you need to explicitly
		 *	set them with each call to Mix_PlayChannel*().
		 * You may also register a special effect function that is to be run after
		 *	final mixing occurs. The rules for these callbacks are identical to those
		 *	in Mix_RegisterEffect, but they are run after all the channels and the
		 *	music have been mixed into a single stream, whereas channel-specific
		 *	effects run on a given channel before any other mixing occurs. These
		 *	global effect callbacks are call "posteffects". Posteffects only have
		 *	their Mix_EffectDone_t function called when they are unregistered (since
		 *	the main output stream is never "done" in the same sense as a channel).
		 *	You must unregister them manually when you've had enough. Your callback
		 *	will be told that the channel being mixed is (MIX_CHANNEL_POST) if the
		 *	processing is considered a posteffect.
		 *
		 * After all these effects have finished processing, the callback registered
		 *	through Mix_SetPostMix() runs, and then the stream goes to the audio
		 *	device. 
		 *
		 * DO NOT EVER call SDL_LockAudio() from your callback function!
		 *
		 * returns zero if error (no such channel), nonzero if added.
		 *	Error messages can be retrieved from Mix_GetError().
		 */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_RegisterEffect(int chan, Mix_EffectFunc_t f,
			Mix_EffectDone_t d, /* void * */IntPtr arg);


		/* You may not need to call this explicitly, unless you need to stop an
		 *	effect from processing in the middle of a chunk'name playback.
		 * Posteffects are never implicitly unregistered as they are for channels,
		 *	but they may be explicitly unregistered through this function by
		 *	specifying MIX_CHANNEL_POST for a channel.
		 * returns zero if error (no such channel or effect), nonzero if removed.
		 *	Error messages can be retrieved from Mix_GetError().
		 */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_UnregisterEffect(int channel, Mix_EffectFunc_t f);


		/* You may not need to call this explicitly, unless you need to stop all
		 *	effects from processing in the middle of a chunk'name playback. Note that
		 *	this will also shut off some internal effect processing, since
		 *	Mix_SetPanning() and others may use this API under the hood. This is
		 *	called internally when a channel completes playback.
		 * Posteffects are never implicitly unregistered as they are for channels,
		 *	but they may be explicitly unregistered through this function by
		 *	specifying MIX_CHANNEL_POST for a channel.
		 * returns zero if error (no such channel), nonzero if all effects removed.
		 *	Error messages can be retrieved from Mix_GetError().
		 */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_UnregisterAllEffects(int channel);


		public const string MIX_EFFECTSMAXSPEED = "MIX_EFFECTSMAXSPEED";

		/*
		 * These are the internally-defined mixing effects. They use the same API that
		 *	effects defined in the application use, but are provided here as a
		 *	convenience. Some effects can reduce their quality or use more memory in
		 *	the name of speed; to enable this, make sure the environment variable
		 *	MIX_EFFECTSMAXSPEED (see above) is defined before you call
		 *	Mix_OpenAudio().
		 */


		/* Set the panning of a channel. The left and right channels are specified
		 *	as integers between 0 and 255, quietest to loudest, respectively.
		 *
		 * Technically, this is just individual volume control for a sample with
		 *	two (stereo) channels, so it can be used for more than just panning.
		 *	If you want real panning, call it like this:
		 *
		 *	 Mix_SetPanning(channel, left, 255 - left);
		 *
		 * ...which isn't so hard.
		 *
		 * Setting (channel) to MIX_CHANNEL_POST registers this as a posteffect, and
		 *	the panning will be done to the final mixed stream before passing it on
		 *	to the audio device.
		 *
		 * This uses the Mix_RegisterEffect() API internally, and returns without
		 *	registering the effect function if the audio device is not configured
		 *	for stereo output. Setting both (left) and (right) to 255 causes this
		 *	effect to be unregistered, since that is the data'name normal state.
		 *
		 * returns zero if error (no such channel or Mix_RegisterEffect() fails),
		 *	nonzero if panning effect enabled. Note that an audio device in mono
		 *	mode is a no-op, but this call will return successful in that case.
		 *	Error messages can be retrieved from Mix_GetError().
		 */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_SetPanning(int channel, Uint8 left, Uint8 right);


		/* Set the position of a channel. (angle) is an integer from 0 to 360, that
		 *	specifies the location of the sound in relation to the listener. (angle)
		 *	will be reduced as neccesary (540 becomes 180 degrees, -100 becomes 260).
		 *	Angle 0 is due north, and rotates clockwise as the value increases.
		 *	For efficiency, the precision of this effect may be limited (angles 1
		 *	through 7 might all produce the same effect, 8 through 15 are equal, etc).
		 *	(distance) is an integer between 0 and 255 that specifies the space
		 *	between the sound and the listener. The larger the number, the further
		 *	away the sound is. Using 255 does not guarantee that the channel will be
		 *	culled from the mixing process or be completely silent. For efficiency,
		 *	the precision of this effect may be limited (distance 0 through 5 might
		 *	all produce the same effect, 6 through 10 are equal, etc). Setting (angle)
		 *	and (distance) to 0 unregisters this effect, since the data would be
		 *	unchanged.
		 *
		 * If you need more precise positional audio, consider using OpenAL for
		 *	spatialized effects instead of SDL_mixer. This is only meant to be a
		 *	basic effect for simple "3D" games.
		 *
		 * If the audio device is configured for mono output, then you won't get
		 *	any effectiveness from the angle; however, distance attenuation on the
		 *	channel will still occur. While this effect will function with stereo
		 *	voices, it makes more sense to use voices with only one channel of sound,
		 *	so when they are mixed through this effect, the positioning will sound
		 *	correct. You can convert them to mono through SDL before giving them to
		 *	the mixer in the first place if you like.
		 *
		 * Setting (channel) to MIX_CHANNEL_POST registers this as a posteffect, and
		 *	the positioning will be done to the final mixed stream before passing it
		 *	on to the audio device.
		 *
		 * This is a convenience wrapper over Mix_SetDistance() and Mix_SetPanning().
		 *
		 * returns zero if error (no such channel or Mix_RegisterEffect() fails),
		 *	nonzero if position effect is enabled.
		 *	Error messages can be retrieved from Mix_GetError().
		 */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_SetPosition(int channel, Sint16 angle, Uint8 distance);


		/* Set the "distance" of a channel. (distance) is an integer from 0 to 255
		 *	that specifies the location of the sound in relation to the listener.
		 *	Distance 0 is overlapping the listener, and 255 is as far away as possible
		 *	A distance of 255 does not guarantee silence; in such a case, you might
		 *	want to try changing the chunk'name volume, or just cull the sample from the
		 *	mixing process with Mix_HaltChannel().
		 * For efficiency, the precision of this effect may be limited (distances 1
		 *	through 7 might all produce the same effect, 8 through 15 are equal, etc).
		 *	(distance) is an integer between 0 and 255 that specifies the space
		 *	between the sound and the listener. The larger the number, the further
		 *	away the sound is.
		 * Setting (distance) to 0 unregisters this effect, since the data would be
		 *	unchanged.
		 * If you need more precise positional audio, consider using OpenAL for
		 *	spatialized effects instead of SDL_mixer. This is only meant to be a
		 *	basic effect for simple "3D" games.
		 *
		 * Setting (channel) to MIX_CHANNEL_POST registers this as a posteffect, and
		 *	the distance attenuation will be done to the final mixed stream before
		 *	passing it on to the audio device.
		 *
		 * This uses the Mix_RegisterEffect() API internally.
		 *
		 * returns zero if error (no such channel or Mix_RegisterEffect() fails),
		 *	nonzero if position effect is enabled.
		 *	Error messages can be retrieved from Mix_GetError().
		 */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_SetDistance(int channel, Uint8 distance);


		/* Causes a channel to reverse its stereo. This is handy if the user has his
		 *	speakers hooked up backwards, or you would like to have a minor bit of
		 *	psychedelia in your sound code.  :)  Calling this function with (flip)
		 *	set to non-zero reverses the chunks'name usual channels. If (flip) is zero,
		 *	the effect is unregistered.
		 *
		 * This uses the Mix_RegisterEffect() API internally, and thus is probably
		 *	more CPU intensive than having the user just plug in his speakers
		 *	correctly. Mix_SetReverseStereo() returns without registering the effect
		 *	function if the audio device is not configured for stereo output.
		 *
		 * If you specify MIX_CHANNEL_POST for (channel), then this the effect is used
		 *	on the final mixed stream before sending it on to the audio device (a
		 *	posteffect).
		 *
		 * returns zero if error (no such channel or Mix_RegisterEffect() fails),
		 *	nonzero if reversing effect is enabled. Note that an audio device in mono
		 *	mode is a no-op, but this call will return successful in that case.
		 *	Error messages can be retrieved from Mix_GetError().
		 */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_SetReverseStereo(int channel, int flip);

		/* end of effects API. --ryan. */


		/* Reserve the first channels (0 -> n-1) for the application, i.e. don't allocate
		   them dynamically to the next sample if requested with a -1 value below.
		   Returns the number of reserved channels.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_ReserveChannels(int num);

		/* Channel grouping functions */

		/* Attach a tag to a channel. A tag can be assigned to several mixer
		   channels, to form groups of channels.
		   If 'tag' is -1, the tag is removed (actually -1 is the tag used to
		   represent the group of all the channels).
		   Returns true if everything was OK.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_GroupChannel(int which, int tag);
		/* Assign several consecutive channels to a group */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_GroupChannels(int from, int to, int tag);
		/* Finds the first available channel in a group of channels,
		   returning -1 if none are available.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_GroupAvailable(int tag);
		/* Returns the number of channels in a group. This is also a subtle
		   way to get the total number of channels when 'tag' is -1
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_GroupCount(int tag);
		/* Finds the "oldest" sample playing in a group of channels */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_GroupOldest(int tag);
		/* Finds the "most recent" (i.e. last) sample playing in a group of channels */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_GroupNewer(int tag);

		/* The same as above, but the sound is played at most 'ticks' milliseconds */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_PlayChannelTimed(int channel, /* Mix_Chunk * */IntPtr chunk, int loops, int ticks);
		/* Play an audio chunk on a specific channel.
		   If the specified channel is -1, play on the first free channel.
		   If 'loops' is greater than zero, loop the sound that many times.
		   If 'loops' is -1, loop inifinitely (~65000 times).
		   Returns which channel was used to play the sound.
		*/
		public static int Mix_PlayChannel(int channel, /* Mix_Chunk* */IntPtr chunk, int loops) {
			return Mix_PlayChannelTimed(channel,chunk,loops,-1);
		}
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_PlayMusic(/* Mix_Music * */IntPtr music, int loops);

		/* Fade in music or a channel over "ms" milliseconds, same semantics as the "Play" functions */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_FadeInMusic(/* Mix_Music * */IntPtr music, int loops, int ms);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_FadeInMusicPos(/* Mix_Music * */IntPtr music, int loops, int ms, double position);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_FadeInChannelTimed(int channel, /* Mix_Chunk * */IntPtr chunk, int loops, int ms, int ticks);
		public static int Mix_FadeInChannel(int channel, /* Mix_Chunk* */IntPtr chunk, int loops, int ms) {
			return Mix_FadeInChannelTimed(channel,chunk,loops,ms,-1);
		}

		/* Set the volume in the range of 0-128 of a specific channel or chunk.
		   If the specified channel is -1, set volume for all channels.
		   Returns the original volume.
		   If the specified volume is -1, just return the current volume.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_Volume(int channel, int volume);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_VolumeChunk(/* Mix_Chunk * */IntPtr chunk, int volume);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_VolumeMusic(int volume);

		/* Halt playing of a particular channel */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_HaltChannel(int channel);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_HaltGroup(int tag);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_HaltMusic();

		/* Change the expiration delay for a particular channel.
		   The sample will stop playing after the 'ticks' milliseconds have elapsed,
		   or remove the expiration if 'ticks' is -1
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_ExpireChannel(int channel, int ticks);

		/* Halt a channel, fading it out progressively till it'name silent
		   The ms parameter indicates the number of milliseconds the fading
		   will take.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_FadeOutChannel(int which, int ms);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_FadeOutGroup(int tag, int ms);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_FadeOutMusic(int ms);

		/* Query the fading status of a channel */
		[DllImport(DLL_SDL_MIXER)]
		public static extern Mix_Fading Mix_FadingMusic();
		[DllImport(DLL_SDL_MIXER)]
		public static extern Mix_Fading Mix_FadingChannel(int which);

		/* Pause/Resume a particular channel */
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_Pause(int channel);
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_Resume(int channel);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_Paused(int channel);

		/* Pause/Resume the music stream */
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_PauseMusic();
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_ResumeMusic();
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_RewindMusic();
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_PausedMusic();

		/* Set the current position in the music stream.
		   This returns 0 if successful, or -1 if it failed or isn't implemented.
		   This function is only implemented for MOD music formats (set pattern
		   order number) and for OGG music (set position in seconds), at the
		   moment.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_SetMusicPosition(double position);

		/* Check the status of a specific channel.
		   If the specified channel is -1, check all channels.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_Playing(int channel);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_PlayingMusic();

		/* Stop music and set external music playback command */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_SetMusicCMD(/* char * */string command);

		/* Synchro value is set by MikMod from modules while playing */
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_SetSynchroValue(int value);
		[DllImport(DLL_SDL_MIXER)]
		public static extern int Mix_GetSynchroValue();

		/* Get the Mix_Chunk currently associated with a mixer channel
		   Returns NULL if it'name an invalid channel, or there'name no chunk associated.
		*/
		[DllImport(DLL_SDL_MIXER)]
		public static extern /* Mix_Chunk * */IntPtr Mix_GetChunk(int channel);

		/* Close the mixer, halting all playing audio */
		[DllImport(DLL_SDL_MIXER)]
		public static extern void Mix_CloseAudio();

		/* We'll use SDL for reporting errors */
		//	void Mix_SetError	SDL_SetError
		public static /* char * */string Mix_GetError() {
			return SDL_GetError();
		}

		#endregion
		#region SDL_mouse
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Cursor {
			public SDL_Rect area;			/* The area of the mouse cursor */
			public Sint16 hot_x, hot_y;		/* The "tip" of the cursor */
			public /* Uint8 * */IntPtr data;			/* B/W cursor data */
			public /* Uint8 * */IntPtr mask;			/* B/W cursor mask */
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
			public /* Uint8 * */IntPtr[] save;			/* Place to save cursor area */
			public /* void * */IntPtr wm_cursor;		/* Window-manager cursor */
		}

		/* Function prototypes */
		/*
		 * Retrieve the current state of the mouse.
		 * The current Button state is returned as a Button bitmask, which can
		 * be tested using the SDL_BUTTON(X) macros, and x and y are set to the
		 * current mouse cursor position.  You can pass NULL for either x or y.
		 */
		[DllImport(DLL_SDL)]
		public static extern Uint8 SDL_GetMouseState(/* int * */out int x, /* int * */out int y);

		/**
		 * マウスの現在位置を正しく取得できないため、Win32APIを使用
		 */
		public static void GetMouseState(out int x, out int y) 
		{
			if (Yanesdk.System.Platform.IsLinux)
				SDL_GetMouseState(out x, out y);
			else
				GetMouseStateImpl(out x, out y);
		}

		private struct Point 
		{
			public int x;
			public int y;
		}

		[DllImport("user32")]
		private static extern int GetCursorPos(ref Point pt);

		[DllImport("user32")]
		private static extern int ScreenToClient(IntPtr hwnd, ref Point pt);

		unsafe private static void GetMouseStateImpl(out int x, out int y) 
		{
			Point pt = new Point();
			GetCursorPos(ref pt);
			SDL.SDL_SysWMinfo* pinfo = stackalloc SDL.SDL_SysWMinfo[1];
			SDL.SDL_GetWMInfo((IntPtr)(pinfo));
			ScreenToClient(pinfo->window, ref pt);

			x = pt.x;
			y = pt.y;

			if (x < 0)
				x = 0;
			else if ( x >= screen_width )
				x = screen_width -1 ;

			if (y < 0)
				y = 0;
			else if ( y >= screen_height )
				y = screen_height - 1;
		}

		/*
		 * Retrieve the current state of the mouse.
		 * The current Button state is returned as a Button bitmask, which can
		 * be tested using the SDL_BUTTON(X) macros, and x and y are set to the
		 * mouse deltas since the last call to SDL_GetRelativeMouseState().
		 */
		[DllImport(DLL_SDL)]
		public static extern Uint8 SDL_GetRelativeMouseState(/* int * */ref int x, /* int * */ref int y);

		/*
		 * Set the position of the mouse cursor (generates a mouse motion event)
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_WarpMouse(Uint16 x, Uint16 y);

		/*
		 * Create a cursor using the specified data and mask (in MSB format).
		 * The cursor width must be a multiple of 8 bits.
		 * 
		 * The cursor is created in black and white according to the following:
		 * data  mask    resulting pixel on screen
		 *  0     1       White
		 *  1     1       Black
		 *  0     0       Transparent
		 *  1     0       Inverted color if possible, black if not.
		 * 
		 * Cursors created with this function must be freed with SDL_FreeCursor().
		 */
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Cursor * */IntPtr SDL_CreateCursor
			(/* Uint8 * */ref Uint8 data, /* Uint8 * */ref Uint8 mask, int w, int h, int hot_x, int hot_y);

		/*
		 * Set the currently active cursor to the specified one.
		 * If the cursor is currently visible, the change will be immediately 
		 * represented on the display.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_SetCursor(/* SDL_Cursor * */IntPtr cursor);

		/*
		 * Returns the currently active cursor.
		 */
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Cursor * */IntPtr SDL_GetCursor();

		/*
		 * Deallocates a cursor created with SDL_CreateCursor().
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_FreeCursor(/* SDL_Cursor * */IntPtr cursor);

		/*
		 * Toggle whether or not the cursor is shown on the screen.
		 * The cursor start off displayed, but can be turned off.
		 * SDL_ShowCursor() returns 1 if the cursor was being displayed
		 * before the call, or 0 if it was not.  You can query the current
		 * state by passing a 'toggle' value of -1.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_ShowCursor(int toggle);

		/* Used as a mask when testing buttons in buttonstate
		   Button 1:	Left mouse Button
		   Button 2:	Middle mouse Button
		   Button 3:	Right mouse Button
		 */
		public static uint SDL_BUTTON(uint X) { return (uint)(SDL_PRESSED << (int)(X-1)); }
		public const uint SDL_BUTTON_LEFT		= 1;
		public const uint SDL_BUTTON_MIDDLE	= 2;
		public const uint SDL_BUTTON_RIGHT		= 3;
		public const uint SDL_BUTTON_LMASK		= SDL_PRESSED << (int)(SDL_BUTTON_LEFT - 1);
		public const uint SDL_BUTTON_MMASK		= SDL_PRESSED << (int)(SDL_BUTTON_MIDDLE - 1);
		public const uint SDL_BUTTON_RMASK		= SDL_PRESSED << (int)(SDL_BUTTON_RIGHT - 1);
		#endregion
		#region SDL_mutex
		/* Synchronization functions which can time out return this value
		if they time out.
		*/
		public const uint SDL_MUTEX_TIMEDOUT	= 1;

		/* This is the timeout value which corresponds to never time out */
		public const uint SDL_MUTEX_MAXWAIT	= 0xFFFFFFFF;


		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		/* Mutex functions                                               */
		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_mutex { }

		/* Create a mutex, initialized unlocked */
		[DllImport(DLL_SDL)]
		public static extern /*SDL_mutex * */IntPtr SDL_CreateMutex();

		/* Lock the mutex  (Returns 0, or -1 on error) */
		public static int SDL_LockMutex(/* SDL_mutex * */IntPtr m) { return SDL_mutexP(m); }
		[DllImport(DLL_SDL)]
		public static extern int SDL_mutexP(/* SDL_mutex * */IntPtr mutex);

		/* Unlock the mutex  (Returns 0, or -1 on error) */
		public static int SDL_UnlockMutex(/* SDL_mutex* */IntPtr m) { return SDL_mutexV(m); }
		[DllImport(DLL_SDL)]
		public static extern int SDL_mutexV(/* SDL_mutex * */IntPtr mutex);

		/* Destroy a mutex */
		[DllImport(DLL_SDL)]
		public static extern void SDL_DestroyMutex(/* SDL_mutex * */IntPtr mutex);


		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		/* Semaphore functions                                           */
		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_sem { }

		/* Create a semaphore, initialized with value, returns NULL on failure. */
		[DllImport(DLL_SDL)]
		public static extern /* SDL_sem * */IntPtr SDL_CreateSemaphore(Uint32 initial_value);

		/* Destroy a semaphore */
		[DllImport(DLL_SDL)]
		public static extern void SDL_DestroySemaphore(/* SDL_sem * */IntPtr sem);

		/* This function suspends the calling thread until the semaphore pointed 
		 * to by sem has a positive count. It then atomically decreases the semaphore
		 * count.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_SemWait(/* SDL_sem * */IntPtr sem);

		/* Non-blocking variant of SDL_SemWait(), returns 0 if the wait succeeds,
		   SDL_MUTEX_TIMEDOUT if the wait would block, and -1 on error.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SemTryWait(/* SDL_sem * */IntPtr sem);

		/* Variant of SDL_SemWait() with a timeout in milliseconds, returns 0 if
		   the wait succeeds, SDL_MUTEX_TIMEDOUT if the wait does not succeed in
		   the allotted time, and -1 on error.
		   On some platforms this function is implemented by looping with a delay
		   of 1 ms, and so should be avoided if possible.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SemWaitTimeout(/* SDL_sem * */IntPtr sem, Uint32 ms);

		/* Atomically increases the semaphore'name count (not blocking), returns 0,
		   or -1 on error.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_SemPost(/* SDL_sem * */IntPtr sem);

		/* Returns the current count of the semaphore */
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_SemValue(/* SDL_sem * */IntPtr sem);


		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		/* Condition variable functions                                  */
		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_cond { }

		/* Create a condition variable */
		[DllImport(DLL_SDL)]
		public static extern /* SDL_cond * */IntPtr SDL_CreateCond();

		/* Destroy a condition variable */
		[DllImport(DLL_SDL)]
		public static extern void SDL_DestroyCond(/* SDL_cond * */IntPtr cond);

		/* Restart one of the threads that are waiting on the condition variable,
		   returns 0 or -1 on error.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CondSignal(/* SDL_cond * */IntPtr cond);

		/* Restart all threads that are waiting on the condition variable,
		   returns 0 or -1 on error.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CondBroadcast(/* SDL_cond * */IntPtr cond);

		/* Wait on the condition variable, unlocking the provided mutex.
		   The mutex must be locked before entering this function!
		   Returns 0 when it is signaled, or -1 on error.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_CondWait(/* SDL_cond * */IntPtr cond, /* SDL_mutex * */IntPtr mut);

		/* Waits for at most 'ms' milliseconds, and returns 0 if the condition
		   variable is signaled, SDL_MUTEX_TIMEDOUT if the condition is not
		   signaled in the allotted time, and -1 on error.
		   On some platforms this function is implemented by looping with a delay
		   of 1 ms, and so should be avoided if possible.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_CondWaitTimeout(/* SDL_cond * */IntPtr cond, /* SDL_mutex * */IntPtr mutex, Uint32 ms);

		#endregion
		#region SDL_quit
		
		/* 
		  An SDL_QUITEVENT is generated when the user tries to close the application
		  window.  If it is ignored or filtered out, the window will remain open.
		  If it is not ignored or filtered, it is queued normally and the window
		  is allowed to close.  When the window is closed, screen updates will 
		  complete, but have no effect.

		  SDL_Init() installs signal handlers for SIGINT (keyboard interrupt)
		  and SIGTERM (system termination request), if handlers do not already
		  exist, that generate SDL_QUITEVENT events as well.  There is no way
		  to determine the cause of an SDL_QUITEVENT, but setting a signal
		  handler in your application will override the default generation of
		  quit events for that signal.
		*/

		/* There are no functions directly affecting the quit event */
		public static int SDL_QuitRequested() {
			SDL_PumpEvents();
			return SDL_PeepEvents(null, 0, SDL_eventaction.SDL_PEEKEVENT, SDL_QUITMASK);
		}
		#endregion
		#region SDL_rwops

		// typedef int (*_seek_func_t)(SDL_RWops *context, int offset, int whence);
		// typedef int (*_read_func_t)(SDL_RWops *context, void *ptr, int size, int maxnum);
		// typedef int (*_write_func_t)(SDL_RWops *context, void *ptr, int size, int num);
		// typedef int (*_close_func_t)(SDL_RWops *context);


		/* Functions to create SDL_RWops structures from various data sources */

		[DllImport(DLL_SDL)]
		public static extern /*SDL_RWops * */IntPtr SDL_RWFromFile(/*char * */string file, /*char * */string mode);

		[DllImport(DLL_SDL)]
		public static extern /*SDL_RWops * */IntPtr SDL_RWFromFP(/* void * */IntPtr fp, int autoclose);

		[DllImport(DLL_SDL)]
		public static extern /* SDL_RWops * */IntPtr SDL_RWFromMem(/* void * */IntPtr mem, int size);

		public static SDL_RWopsH SDL_RWFromMem(byte[] mem) {
			SDL_RWopsH h = new SDL_RWopsH();
			h.hMem = Marshal.AllocHGlobal(mem.Length);
			Marshal.Copy(mem, 0, h.hMem, mem.Length);
			h.Handle = SDL_RWFromMem(h.hMem, mem.Length);
			h.Length = mem.Length;
			return h;
		}

		[DllImport(DLL_SDL)]
		public static extern /* SDL_RWops * */ IntPtr SDL_AllocRW();
		[DllImport(DLL_SDL)]
		public static extern void SDL_FreeRW(/* SDL_RWops * */IntPtr area);

		public static int SDL_RWseek(/* SDL_RWops * */IntPtr ctx, int offset, int whence) {
			// C#では関数ポインタをコール出来ないので実装出来ない
			throw new NotImplementedException();
		}

		public static int SDL_RWtell(/* SDL_RWops * */IntPtr ctx) {
			// C#では関数ポインタをコール出来ないので実装出来ない
			throw new NotImplementedException();
		}

		public static int SDL_RWread(/* SDL_RWops * */IntPtr ctx, /* void* */ IntPtr ptr, int size, int n) {
			// C#では関数ポインタをコール出来ないので実装出来ない
			throw new NotImplementedException();
		}

		public static int SDL_RWwrite(/* SDL_RWops * */IntPtr ctx, /* void* */IntPtr ptr, int size, int n) {
			// C#では関数ポインタをコール出来ないので実装出来ない
			throw new NotImplementedException();
		}

		public static int SDL_RWclose(/* SDL_RWops * */IntPtr ctx) {
			// C#では関数ポインタをコール出来ないので実装出来ない
			throw new NotImplementedException();
		}

		#endregion
		#region SDL_syswm
		/* Your application has access to a special type of event 'SDL_SYSWMEVENT',
		which contains window-manager specific information and arrives whenever
		an unhandled window event occurs.  This event is ignored by default, but
		you can enable it with SDL_EventState()
		*/


		/* The windows custom event structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_SysWMmsg {
			public SDL_version version;	
			public HWND hwnd;				/* The window for the message */
			public UINT msg;				/* The type of message */
			public WPARAM wParam;			/* WORD message parameter */
			public LPARAM lParam;			/* LONG message parameter */
		}

		/* The windows custom window manager information structure */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_SysWMinfo {
			public SDL_version version;
			public HWND window;			/* The Win32 display window */
		}

		/* Function prototypes */
		/*
		 * This function gives you custom hooks into the window manager information.
		 * It fills the structure pointed to by 'info' with custom information and
		 * returns 1 if the function is implemented.  If it'name not implemented, or
		 * the version member of the 'info' structure is invalid, it returns 0. 
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_GetWMInfo(/* SDL_SysWMinfo * */IntPtr info);
		#endregion
		#region SDL_thread
		/* The SDL thread structure, defined in SDL_thread.c */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Thread { }


		public delegate int thread_proc_t(IntPtr data);
		/* Create a thread */
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Thread * */IntPtr SDL_CreateThread(thread_proc_t f, /* void * */IntPtr data);

		/* Get the 32-bit thread identifier for the current thread */
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_ThreadID();

		/* Get the 32-bit thread identifier for the specified thread,
		   equivalent to SDL_ThreadID() if the specified thread is NULL.
		 */
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_GetThreadID(/* SDL_Thread * */IntPtr thread);

		/* Wait for a thread to finish.
		   The return code for the thread function is placed in the area
		   pointed to by 'status', if 'status' is not NULL.
		 */
		[DllImport(DLL_SDL)]
		public static extern void SDL_WaitThread(/* SDL_Thread * */IntPtr thread, /* int * */ref int status);

		/* Forcefully kill a thread without worrying about its state */
		[DllImport(DLL_SDL)]
		public static extern void SDL_KillThread(/* SDL_Thread * */IntPtr thread);
		#endregion
		#region SDL_timer

		/* This is the OS scheduler timeslice, in milliseconds */
		public const uint SDL_TIMESLICE	= 10;

		/* This is the maximum resolution of the SDL timer on all platforms */
		public const uint TIMER_RESOLUTION	= 10;	/* Experimentally determined */

		/* Get the number of milliseconds since the SDL library initialization.
		 * Note that this value wraps if the program runs for more than ~49 days.
		 */ 
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_GetTicks();

		/* Wait a specified number of milliseconds before returning */
		[DllImport(DLL_SDL)]
		public static extern void SDL_Delay(Uint32 ms);

		/* Function prototype for the timer callback function */
		public delegate Uint32 SDL_TimerCallback(Uint32 interval);

		/* Set a callback to run after the specified number of milliseconds has
		 * elapsed. The callback function is passed the current timer interval
		 * and returns the next timer interval.  If the returned value is the 
		 * same as the one passed in, the periodic alarm continues, otherwise a
		 * new alarm is scheduled.  If the callback returns 0, the periodic alarm
		 * is cancelled.
		 * 
		 * To cancel a currently running timer, call SDL_SetTimer(0, NULL);
		 * 
		 * The timer callback function may run in a different thread than your
		 * main code, and so shouldn't call any functions from within itself.
		 * 
		 * The maximum resolution of this timer is 10 ms, which means that if
		 * you request a 16 ms timer, your callback will run approximately 20 ms
		 * later on an unloaded system.  If you wanted to set a flag signaling
		 * a frame Update at 30 frames per second (every 33 ms), you might set a 
		 * timer for 30 ms:
		 *   SDL_SetTimer((33/10)*10, flag_update);
		 * 
		 * If you use this function, you need to pass SDL_INIT_TIMER to SDL_Init().
		 * 
		 * Under UNIX, you should not use raise or use SIGALRM and this function
		 * in the same program, as it is implemented using setitimer().  You also
		 * should not use this function in multi-threaded applications as signals
		 * to multi-threaded apps have undefined behavior in some implementations.
		 */
		[DllImport(DLL_SDL)]
		public static extern int SDL_SetTimer(Uint32 interval, SDL_TimerCallback callback);

		/* New timer API, supports multiple timers
		 * Written by Stephane Peter <megastep@lokigames.com>
		 */

		/* Function prototype for the new timer callback function.
		 * The callback function is passed the current timer interval and returns
		 * the next timer interval.  If the returned value is the same as the one
		 * passed in, the periodic alarm continues, otherwise a new alarm is
		 * scheduled.  If the callback returns 0, the periodic alarm is cancelled.
		 */
		public delegate Uint32 SDL_NewTimerCallback(Uint32 interval, /* void * */IntPtr param);

		/* Definition of the timer ID type */

		/* Add a new timer to the pool of timers already running.
		   Returns a timer ID, or NULL when an error occurs.
		 */
		[DllImport(DLL_SDL)]
		public static extern SDL_TimerID SDL_AddTimer(Uint32 interval, SDL_NewTimerCallback callback, /* void * */IntPtr param);

		/* Remove one of the multiple timers knowing its ID.
		 * Returns a boolean value indicating success.
		 */
		[DllImport(DLL_SDL)]
		public static extern SDL_bool SDL_RemoveTimer(SDL_TimerID t);
		#endregion
		#region SDL_ttf
		/* The internal structure containing font information */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct TTF_Font {}

		/* Initialize the TTF engine - returns 0 if successful, -1 on error */
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_Init();

		/* Open a font file and create a font of the specified point size */
		[DllImport(DLL_SDL_TTF)]
		public static extern /* TTF_Font * */IntPtr TTF_OpenFont(/* char * */string file, int ptsize);
		[DllImport(DLL_SDL_TTF)]
		public static extern /* TTF_Font * */IntPtr TTF_OpenFontIndex(/* char * */string file, int ptsize, long index);

		[DllImport(DLL_SDL_TTF)]
		public static extern /* TTF_Font * */IntPtr TTF_OpenFontRW(/* SDL_RWops * */IntPtr src, int freesrc, int ptsize);
		[DllImport(DLL_SDL_TTF)]
		public static extern /* TTF_Font * */IntPtr TTF_OpenFontIndexRW(/* SDL_RWops * */IntPtr src, int freesrc, int ptsize, long index);

		/* Set and retrieve the font style
		   This font style is implemented by modifying the font glyphs, and
		   doesn't reflect any inherent properties of the truetype font file.
		*/
		public const int TTF_STYLE_NORMAL = 0x00;
		public const int TTF_STYLE_BOLD = 0x01;
		public const int TTF_STYLE_ITALIC = 0x02;
		public const int TTF_STYLE_UNDERLINE = 0x04;
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_GetFontStyle(/* TTF_Font * */IntPtr font);
		[DllImport(DLL_SDL_TTF)]
		public static extern void TTF_SetFontStyle(/* TTF_Font * */IntPtr font, int style);

		/* Get the total height of the font - usually equal to point size */
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_FontHeight(/* TTF_Font * */IntPtr font);

		/* Get the offset from the baseline to the top of the font
		   This is a positive value, relative to the baseline.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_FontAscent(/* TTF_Font * */IntPtr font);

		/* Get the offset from the baseline to the bottom of the font
		   This is a negative value, relative to the baseline.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_FontDescent(/* TTF_Font * */IntPtr font);

		/* Get the recommended spacing between lines of text for this font */
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_FontLineSkip(/* TTF_Font * */IntPtr font);

		/* Get the number of faces of the font */
		[DllImport(DLL_SDL_TTF)]
		public static extern long TTF_FontFaces(/* TTF_Font * */IntPtr font);

		/* Get the font face attributes, if any */
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_FontFaceIsFixedWidth(/* TTF_Font * */IntPtr font);
		[DllImport(DLL_SDL_TTF)]
		public static extern /* char * */string TTF_FontFaceFamilyName(/* TTF_Font * */IntPtr font);
		[DllImport(DLL_SDL_TTF)]
		public static extern /* char * */string TTF_FontFaceStyleName(/* TTF_Font * */IntPtr font);

		/* Get the metrics (dimensions) of a glyph */
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_GlyphMetrics(/* TTF_Font * */IntPtr font, Uint16 ch,
			/* int * */ref int minx, /* int * */ref int maxx,
			/* int * */ref int miny, /* int * */ref int maxy, /* int * */ref int advance);

		/* Get the dimensions of a rendered string of text */
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_SizeText(/* TTF_Font * */IntPtr font,/*  char * */string text, /* int * */ref int w, /* int * */ref int h);
		[DllImport(DLL_SDL_TTF)]
		public static extern int TTF_SizeUTF8(/* TTF_Font * */IntPtr font, /* char * */byte[] text, /* int * */ref int w, /* int * */ref int h);
		[DllImport(DLL_SDL_TTF, CharSet=CharSet.Unicode)]
		public static extern int TTF_SizeUNICODE(/* TTF_Font * */IntPtr font, /* Uint16 * */string text, /* int * */ref int w, /* int * */ref int h);

		/* Create an 8-bit palettized surface and render the given text at
		   fast quality with the given font and color.	The 0 pixel is the
		   colorkey, giving a transparent background, and the 1 pixel is set
		   to the text color.
		   This function returns the new surface, or NULL if there was an error.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderText_Solid(/* TTF_Font * */IntPtr font,
			/* char * */string text, SDL_Color fg);
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderUTF8_Solid(/* TTF_Font * */IntPtr font,
			/* char * */byte[] text, SDL_Color fg);
		[DllImport(DLL_SDL_TTF, CharSet = CharSet.Unicode)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderUNICODE_Solid(/* TTF_Font * */IntPtr font,
			/* Uint16 * */string text, SDL_Color fg);

		/* Create an 8-bit palettized surface and render the given glyph at
		   fast quality with the given font and color.	The 0 pixel is the
		   colorkey, giving a transparent background, and the 1 pixel is set
		   to the text color.  The glyph is rendered without any padding or
		   centering in the X direction, and aligned normally in the Y direction.
		   This function returns the new surface, or NULL if there was an error.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderGlyph_Solid(/* TTF_Font * */IntPtr font,
			Uint16 ch, SDL_Color fg);

		/* Create an 8-bit palettized surface and render the given text at
		   high quality with the given font and colors.  The 0 pixel is background,
		   while other pixels have varying degrees of the foreground color.
		   This function returns the new surface, or NULL if there was an error.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderText_Shaded(/* TTF_Font * */IntPtr font,
			/* char * */string text, SDL_Color fg, SDL_Color bg);
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderUTF8_Shaded(/* TTF_Font * */IntPtr font,
			/* char * */byte[] text, SDL_Color fg, SDL_Color bg);
		[DllImport(DLL_SDL_TTF, CharSet = CharSet.Unicode)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderUNICODE_Shaded(/* TTF_Font * */IntPtr font,
			/* Uint16 * */string text, SDL_Color fg, SDL_Color bg);

		/* Create an 8-bit palettized surface and render the given glyph at
		   high quality with the given font and colors.  The 0 pixel is background,
		   while other pixels have varying degrees of the foreground color.
		   The glyph is rendered without any padding or centering in the X
		   direction, and aligned normally in the Y direction.
		   This function returns the new surface, or NULL if there was an error.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderGlyph_Shaded(/* TTF_Font * */IntPtr font,
			/* Uint16 */char ch, SDL_Color fg, SDL_Color bg);

		/* Create a 32-bit ARGB surface and render the given text at high quality,
		   using alpha blending to dither the font with the given color.
		   This function returns the new surface, or NULL if there was an error.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderText_Blended(/* TTF_Font * */IntPtr font,
			/* char * */string text, SDL_Color fg);
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderUTF8_Blended(/* TTF_Font * */IntPtr font,
			/* char * */IntPtr text, SDL_Color fg);
		[DllImport(DLL_SDL_TTF, CharSet = CharSet.Unicode)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderUNICODE_Blended(/* TTF_Font * */IntPtr font,
			/* Uint16 * */string text, SDL_Color fg);

		/* Create a 32-bit ARGB surface and render the given glyph at high quality,
		   using alpha blending to dither the font with the given color.
		   The glyph is rendered without any padding or centering in the X
		   direction, and aligned normally in the Y direction.
		   This function returns the new surface, or NULL if there was an error.
		*/
		[DllImport(DLL_SDL_TTF)]
		public static extern /* SDL_Surface * */IntPtr TTF_RenderGlyph_Blended(/* TTF_Font * */IntPtr font,
			/* Uint16 */char ch, SDL_Color fg);

		/* For compatibility with previous versions, here are the old functions */
		public static /* SDL_Surface* */IntPtr TTF_RenderText(/* TTF_Font* */IntPtr font, /* char* */string text,
			SDL_Color fg, SDL_Color bg) {
			return TTF_RenderText_Shaded(font, text, fg, bg);
		}
		public static /* SDL_Surface* */IntPtr TTF_RenderUTF8(/* TTF_Font* */IntPtr font, /* char* */byte[] text,
			SDL_Color fg, SDL_Color bg) {
			return TTF_RenderUTF8_Shaded(font, text, fg, bg);
		}
		public static /* SDL_Surface* */IntPtr TTF_RenderUNICODE(/* TTF_Font* */IntPtr font, /* Uint16* */string text,
			SDL_Color fg, SDL_Color bg) {
			return TTF_RenderUNICODE_Shaded(font, text, fg, bg);
		}

		/* Close an opened font file */
		[DllImport(DLL_SDL_TTF)]
		public static extern void TTF_CloseFont(/* TTF_Font * */IntPtr font);

		/* De-initialize the TTF engine */
		[DllImport(DLL_SDL_TTF)]
		public static extern void TTF_Quit();

		/* We'll use SDL for reporting errors */
		//#define TTF_SetError	SDL_SetError
		public static /* char* */string TTF_GetError() {
			return SDL_GetError();
		}
		#endregion
		#region SDL_types
		/* General data types used by the SDL library */

		/* Basic data types */

		public enum SDL_bool : int{
			SDL_FALSE = 0,
			SDL_TRUE  = 1
		}

		/* General keyboard/mouse state definitions */
		public const int SDL_PRESSED = 0x01;
		public const int SDL_RELEASED = 0x00;

		#endregion
		#region SDL_version
		
		/* Printable format: "%d.%d.%d", MAJOR, MINOR, PATCHLEVEL
		*/
		public const uint SDL_MAJOR_VERSION	= 1;
		public const uint SDL_MINOR_VERSION	= 2;
		public const uint SDL_PATCHLEVEL	= 3;

		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_version {
			public Uint8 major;
			public Uint8 minor;
			public Uint8 patch;
		}

		/* This macro can be used to fill a version structure with the compile-time
		 * version of the SDL library.
		 */
		public static void SDL_VERSION(/* SDL_version* */ref SDL_version X) {
			X.major = (byte)SDL_MAJOR_VERSION;
			X.minor = (byte)SDL_MINOR_VERSION;
			X.patch = (byte)SDL_PATCHLEVEL;
		}

		/* This macro turns the version numbers into a numeric value:
		   (1,2,3) -> (1203)
		   This assumes that there will never be more than 100 patchlevels
		*/
		public static uint SDL_VERSIONNUM(Uint8 X, Uint8 Y, Uint8 Z) {
			return (uint)(X * 1000 + Y * 100 + Z);
		}

		/* This is the version number macro for the current SDL version */
		public const uint SDL_COMPILEDVERSION = SDL_MAJOR_VERSION * 1000 +
			SDL_MINOR_VERSION * 100 +
			SDL_PATCHLEVEL;

		/* This macro will evaluate to true if compiled with SDL at least X.Y.Z */
		public static bool SDL_VERSION_ATLEAST(Uint8 X, Uint8 Y, Uint8 Z) {
			return (SDL_COMPILEDVERSION >= SDL_VERSIONNUM(X, Y, Z));
		}

		/* This function gets the version of the dynamically linked SDL library.
		   it should NOT be used to fill a version structure, instead you should
		   use the SDL_Version() macro.
		 */
		[DllImport(DLL_SDL)]
		public static extern /* SDL_version * */IntPtr SDL_Linked_Version();

		
		#endregion
		#region SDL_video
		/* Transparency definitions: These define alpha as the opacity of a surface */
		public const uint SDL_ALPHA_OPAQUE = 255;
		public const uint SDL_ALPHA_TRANSPARENT = 0;

		/* Useful data types */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Rect {
			public Sint16 x, y;
			public Uint16 w, h;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Color {
			public Uint8 r;
			public Uint8 g;
			public Uint8 b;
			public Uint8 unused;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Palette {
			public int 	  ncolors;
			public /* SDL_Color * */IntPtr colors;
		}

		/* Everything in the pixel format structure is read-only */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_PixelFormat {
			public /* SDL_Palette * */IntPtr palette;
			public Uint8  BitsPerPixel;
			public Uint8  BytesPerPixel;
			public Uint8  Rloss;
			public Uint8  Gloss;
			public Uint8  Bloss;
			public Uint8  Aloss;
			public Uint8  Rshift;
			public Uint8  Gshift;
			public Uint8  Bshift;
			public Uint8  Ashift;
			public Uint32 Rmask;
			public Uint32 Gmask;
			public Uint32 Bmask;
			public Uint32 Amask;

			/* RGB color key information */
			public Uint32 colorkey;
			/* Alpha value information (per-surface alpha) */
			public Uint8  alpha;
		}

		/* typedef for private surface blitting functions */
		public delegate int SDL_blit(/* SDL_Surface * */IntPtr src, /* SDL_Rect * */ref SDL_Rect srcrect, 
			/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */ref SDL_Rect dstrect);

		/* This structure should be treated as read-only, except for 'pixels',
		   which, if not NULL, contains the raw pixel data for the surface.
		*/
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Surface {
			public Uint32 flags;				/* Read-only */
			public /* SDL_PixelFormat * */IntPtr format;		/* Read-only */
			public int w, h;				/* Read-only */
			public Uint16 pitch;				/* Read-only */
			public /* void * */IntPtr pixels;				/* Read-write */
			public int offset;				/* Private */

			/* Hardware-specific surface info */
			public /* void * */IntPtr hwdata;

			/* clipping information */
			public SDL_Rect clip_rect;			/* Read-only */
			public Uint32 unused1;				/* for binary compatibility */

			/* Allow recursive locks */
			public Uint32 locked;				/* Private */

			/* info for fast blit mapping to other surfaces */
			public /* void * */IntPtr map;		/* Private */

			/* format version, bumped at every change to invalidate blit maps */
			public uint format_version;		/* Private */

			/* Reference count -- used when freeing surface */
			public int refcount;				/* Read-mostly */
		}

		/* These are the currently supported flags for the SDL_surface */
		/* Available for SDL_CreateRGBSurface() or SDL_SetVideoMode() */
		public const uint SDL_SWSURFACE	= 0x00000000;	/* Surface is in system memory */
		public const uint SDL_HWSURFACE	= 0x00000001;	/* Surface is in video memory */
		public const uint SDL_ASYNCBLIT	= 0x00000004;	/* Use asynchronous blits if possible */
		/* Available for SDL_SetVideoMode() */
		public const uint SDL_ANYFORMAT	= 0x10000000;	/* Allow any video depth/pixel-format */
		public const uint SDL_HWPALETTE	= 0x20000000;	/* Surface has exclusive palette */
		public const uint SDL_DOUBLEBUF	= 0x40000000;	/* Set up double-buffered video mode */
		public const uint SDL_FULLSCREEN	= 0x80000000;	/* Surface is a full screen display */
		public const uint SDL_OPENGL		= 0x00000002;	/* Create an OpenGL rendering context */
		public const uint SDL_OPENGLBLIT	= 0x0000000A;	/* Create an OpenGL rendering context and use it for blitting */
		public const uint SDL_RESIZABLE	= 0x00000010;	/* This video mode may be resized */
		public const uint SDL_NOFRAME	= 0x00000020;	/* No window caption or edge frame */
		/* Used internally (read-only) */
		public const uint SDL_HWACCEL	= 0x00000100;	/* Blit uses hardware acceleration */
		public const uint SDL_SRCCOLORKEY	= 0x00001000;	/* Blit uses a source color key */
		public const uint SDL_RLEACCELOK	= 0x00002000;	/* Private flag */
		public const uint SDL_RLEACCEL	= 0x00004000;	/* Surface is RLE encoded */
		public const uint SDL_SRCALPHA	= 0x00010000;	/* Blit uses source alpha blending */
		public const uint SDL_PREALLOC	= 0x01000000;	/* Surface uses preallocated memory */

		/* Evaluates to true if the surface needs to be locked before access */
		public static bool SDL_MUSTLOCK(/* SDL_Surface * */IntPtr surface) {
			return SDL_MUSTLOCK_(surface);
		}
		unsafe private static bool SDL_MUSTLOCK_(IntPtr surface_) {
			SDL_Surface* surface = (SDL_Surface*)surface_;
			return surface->offset != 0 || ((surface->flags &
				(SDL_HWSURFACE | SDL_ASYNCBLIT | SDL_RLEACCEL)) != 0);
		}

		/* Useful for determining the video hardware capabilities */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_VideoInfo {
			public Uint32 flags;
			//		Uint32 hw_available :1;	/* Flag: Can you create hardware surfaces? */
			//		Uint32 wm_available :1;	/* Flag: Can you talk to a window manager? */
			//		Uint32 UnusedBits1	:6;
			//		Uint32 UnusedBits2	:1;
			//		Uint32 blit_hw		:1;	/* Flag: Accelerated blits HW -. HW */
			//		Uint32 blit_hw_CC	:1;	/* Flag: Accelerated blits with Colorkey */
			//		Uint32 blit_hw_A	:1;	/* Flag: Accelerated blits with Alpha */
			//		Uint32 blit_sw		:1;	/* Flag: Accelerated blits SW -. HW */
			//		Uint32 blit_sw_CC	:1;	/* Flag: Accelerated blits with Colorkey */
			//		Uint32 blit_sw_A	:1;	/* Flag: Accelerated blits with Alpha */
			//		Uint32 blit_fill	:1;	/* Flag: Accelerated color fill */
			//		Uint32 UnusedBits3	:16;
			public Uint32 video_mem;	/* The total amount of video memory (in K) */
			public /* SDL_PixelFormat * */IntPtr vfmt;	/* Value: The format of the video surface */
		}


		/* The most common video overlay formats.
		   For an explanation of these pixel formats, see:
			http://www.webartz.com/fourcc/indexyuv.htm

		   For information on the relationship between color spaces, see:
		   http://www.neuro.sfc.keio.ac.jp/~aly/polygon/info/color-space-faq.html
		 */
		public const uint SDL_YV12_OVERLAY = 0x32315659;	/* Planar mode: Y + V + U  (3 planes) */
		public const uint SDL_IYUV_OVERLAY = 0x56555949;	/* Planar mode: Y + U + V  (3 planes) */
		public const uint SDL_YUY2_OVERLAY = 0x32595559;	/* Packed mode: Y0+U0+Y1+V0 (1 plane) */
		public const uint SDL_UYVY_OVERLAY = 0x59565955;	/* Packed mode: U0+Y0+V0+Y1 (1 plane) */
		public const uint SDL_YVYU_OVERLAY = 0x55595659;	/* Packed mode: Y0+V0+Y1+U0 (1 plane) */

		/* The YUV hardware video overlay */
		[StructLayout(LayoutKind.Sequential, Pack = 4)] public struct SDL_Overlay {
			public Uint32 format;				/* Read-only */
			public int w, h;				/* Read-only */
			public int planes;				/* Read-only */
			public /* Uint16 * */IntPtr pitches;			/* Read-only */
			public /* Uint8 ** */IntPtr pixels;				/* Read-write */

			/* Hardware-specific surface info */
			public /* void * */IntPtr hwfuncs;
			public /* void * */IntPtr hwdata;

			/* Special flags */
			[StructLayout(LayoutKind.Explicit, Pack = 4)]
			public struct SDL_Overlay_flags {
				[FieldOffset(0)]public byte hw_overlay;
				[FieldOffset(0)]public Uint32 _dummy;
			}
		//		Uint32 hw_overlay :1;	/* Flag: This overlay hardware accelerated? */
		//		Uint32 UnusedBits :31;
		}


		/* Public enumeration for setting the OpenGL window attributes. */
		public enum SDL_GLattr : int{
			SDL_GL_RED_SIZE,
			SDL_GL_GREEN_SIZE,
			SDL_GL_BLUE_SIZE,
			SDL_GL_ALPHA_SIZE,
			SDL_GL_BUFFER_SIZE,
			SDL_GL_DOUBLEBUFFER,
			SDL_GL_DEPTH_SIZE,
			SDL_GL_STENCIL_SIZE,
			SDL_GL_ACCUM_RED_SIZE,
			SDL_GL_ACCUM_GREEN_SIZE,
			SDL_GL_ACCUM_BLUE_SIZE,
			SDL_GL_ACCUM_ALPHA_SIZE
		}

		/* flags for SDL_SetPalette() */
		public const uint SDL_LOGPAL = 0x01;
		public const uint SDL_PHYSPAL = 0x02;

		/* Function prototypes */

		/* These functions are used internally, and should not be used unless you
		* have a specific need to specify the video driver you want to use.
		* You should normally use SDL_Init() or SDL_InitSubSystem().
		*	
		* SDL_VideoInit() initializes the video subsystem -- sets up a connection
		* to the window manager, etc, and determines the current video mode and
		* pixel format, but does not initialize a window or graphics mode.
		* Note that event handling is activated by this routine.
		*	
		* If you use both sound and video in your application, you need to call
		* SDL_Init() before opening the sound device, otherwise under Win32 DirectX,
		* you won't be able to set full-screen display modes.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_VideoInit(/* char * */string driver_name, Uint32 flags);
		[DllImport(DLL_SDL)]
		public static extern void SDL_VideoQuit();

		/* This function fills the given character buffer with the name of the
		* video driver, and returns a pointer to it if the video driver has
		* been initialized.  It returns NULL if no driver has been initialized.
		*/
		[DllImport(DLL_SDL)]
		public static extern /* char * */byte[] SDL_VideoDriverName(/* char * */byte[] namebuf, int maxlen);

		/*
		* This function returns a pointer to the current display surface.
		* If SDL is doing format conversion on the display surface, this
		* function returns the publicly visible surface, not the real video
		* surface.
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Surface * */IntPtr SDL_GetVideoSurface();

		/*
		* This function returns a read-only pointer to information about the
		* video hardware.	If this is called before SDL_SetVideoMode(), the 'vfmt'
		* member of the returned structure will contain the pixel format of the
		* "best" video mode.
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_VideoInfo * */IntPtr SDL_GetVideoInfo();

		/*	
		* Check to see if a particular video mode is supported.
		* It returns 0 if the requested mode is not supported under any bit depth,
		* or returns the bits-per-pixel of the closest available mode with the
		* given width and height.	If this bits-per-pixel is different from the
		* one used when setting the video mode, SDL_SetVideoMode() will succeed,
		* but will emulate the requested bits-per-pixel with a shadow surface.
		*	
		* The arguments to SDL_VideoModeOK() are the same ones you would pass to
		* SDL_SetVideoMode()
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_VideoModeOK(int width, int height, int bpp, Uint32 flags);

		/*
		* Return a pointer to an array of available screen dimensions for the
		* given format and video flags, sorted largest to smallest.  Returns 
		* NULL if there are no dimensions available for a particular format, 
		* or (SDL_Rect **)-1 if any dimension is okay for the given format.
		*	
		* If 'format' is NULL, the mode list will be for the format given	
		* by SDL_GetVideoInfo().vfmt
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Rect ** */IntPtr  SDL_ListModes(/* SDL_PixelFormat * */IntPtr format, Uint32 flags);

		/*
		* Set up a video mode with the specified width, height and bits-per-pixel.
		*	
		* If 'bpp' is 0, it is treated as the current display bits per pixel.
		*	
		* If SDL_ANYFORMAT is set in 'flags', the SDL library will try to set the
		* requested bits-per-pixel, but will return whatever video pixel format is
		* available.  The default is to emulate the requested pixel format if it
		* is not natively available.
		*	
		* If SDL_HWSURFACE is set in 'flags', the video surface will be placed in
		* video memory, if possible, and you may have to call SDL_LockSurface()
		* in order to access the raw framebuffer.	Otherwise, the video surface
		* will be created in system memory.
		*	
		* If SDL_ASYNCBLIT is set in 'flags', SDL will try to perform rectangle
		* updates asynchronously, but you must always lock before accessing pixels.
		* SDL will wait for updates to complete before returning from the lock.
		*	
		* If SDL_HWPALETTE is set in 'flags', the SDL library will guarantee
		* that the colors set by SDL_SetColors() will be the colors you get.
		* Otherwise, in 8-bit mode, SDL_SetColors() may not be able to set all
		* of the colors exactly the way they are requested, and you should look
		* at the video surface structure to determine the actual palette.
		* If SDL cannot guarantee that the colors you request can be set,	
		* i.e. if the colormap is shared, then the video surface may be created
		* under emulation in system memory, overriding the SDL_HWSURFACE flag.
		*	
		* If SDL_FULLSCREEN is set in 'flags', the SDL library will try to set
		* a fullscreen video mode.  The default is to create a windowed mode
		* if the current graphics system has a window manager.
		* If the SDL library is able to set a fullscreen video mode, this flag 
		* will be set in the surface that is returned.
		*	
		* If SDL_DOUBLEBUF is set in 'flags', the SDL library will try to set up
		* two surfaces in video memory and swap between them when you call 
		* SDL_Flip().	This is usually slower than the normal single-buffering
		* scheme, but prevents "tearing" artifacts caused by modifying video 
		* memory while the monitor is refreshing.	It should only be used by 
		* applications that redraw the entire screen on every Update.
		*	
		* If SDL_RESIZABLE is set in 'flags', the SDL library will allow the
		* window manager, if any, to resize the window at runtime.  When this
		* occurs, SDL will send a SDL_VIDEORESIZE event to you application,
		* and you must respond to the event by re-calling SDL_SetVideoMode()
		* with the requested size (or another size that suits the application).
		*	
		* If SDL_NOFRAME is set in 'flags', the SDL library will create a window
		* without any title bar or frame decoration.  Fullscreen video modes have
		* this flag set automatically.
		*	
		* This function returns the video framebuffer surface, or NULL if it fails.
		*	
		* If you rely on functionality provided by certain video flags, check the
		* flags of the returned surface to make sure that functionality is available.
		* SDL will fall back to reduced functionality if the exact flags you wanted
		* are not available.
		*/
		[DllImport(DLL_SDL, EntryPoint = "SDL_SetVideoMode")]
		private static extern /* SDL_Surface * */IntPtr SDL_SetVideoMode_(int width, int height, int bpp, Uint32 flags);

		// スクリーンサイズを設定しておき、そいつでマウス入力をclippingする必要がある
		private static int screen_width;
		private static int screen_height;
		public static /* SDL_Surface * */IntPtr SDL_SetVideoMode(int width , int height , int bpp , Uint32 flags)
		{
			IntPtr result = SDL_SetVideoMode_(width,height,bpp,flags);
			
			if (result != IntPtr.Zero)
			{
				// 画面モードの切り替えに成功したので、この画面の幅と高さを記憶しておく。
				screen_width = width;
				screen_height = height;
			}
			return result;
		}

		/*
		* Makes sure the given list of rectangles is updated on the given screen.
		* If 'x', 'y', 'w' and 'h' are all 0, SDL_UpdateRect will Update the entire
		* screen.
		* These functions should not be called while 'screen' is locked.
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_UpdateRects(/* SDL_Surface * */IntPtr screen, int numrects, /* SDL_Rect * */ref SDL_Rect rects);
		[DllImport(DLL_SDL)]
		public static extern void SDL_UpdateRect(/* SDL_Surface * */IntPtr screen, Sint32 x, Sint32 y, Uint32 w, Uint32 h);

		/*
		* On hardware that supports double-buffering, this function sets up a flip
		* and returns.  The hardware will wait for vertical retrace, and then swap
		* video buffers before the next video surface blit or lock will return.
		* On hardware that doesn not support double-buffering, this is equivalent
		* to calling SDL_UpdateRect(screen, 0, 0, 0, 0);
		* The SDL_DOUBLEBUF flag must have been passed to SDL_SetVideoMode() when
		* setting the video mode for this function to perform hardware flipping.
		* This function returns 0 if successful, or -1 if there was an error.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_Flip(/* SDL_Surface * */IntPtr screen);

		/*
		* Set the gamma correction for each of the color channels.
		* The gamma values range (approximately) between 0.1 and 10.0
		*	
		* If this function isn't supported directly by the hardware, it will
		* be emulated using gamma ramps, if available.  If successful, this
		* function returns 0, otherwise it returns -1.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SetGamma(float red, float green, float blue);

		/*
		* Set the gamma translation table for the red, green, and blue channels
		* of the video hardware.  Each table is an array of 256 16-bit quantities,
		* representing a mapping between the input and output for that channel.
		* The input is the index into the array, and the output is the 16-bit
		* gamma value at that index, scaled to the output color precision.
		*	
		* You may pass NULL for any of the channels to leave it unchanged.
		* If the call succeeds, it will return 0.	If the display driver or
		* hardware does not support gamma translation, or otherwise fails,
		* this function will return -1.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SetGammaRamp(/* Uint16 * */ref Uint16 red, /* Uint16 * */ref Uint16 green, /* Uint16 * */ref Uint16 blue);

		/*
		* Retrieve the current values of the gamma translation tables.
		*	
		* You must pass in valid pointers to arrays of 256 16-bit quantities.
		* Any of the pointers may be NULL to ignore that channel.
		* If the call succeeds, it will return 0.	If the display driver or
		* hardware does not support gamma translation, or otherwise fails,
		* this function will return -1.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_GetGammaRamp(/* Uint16 * */ref Uint16 red, /* Uint16 * */ref Uint16 green, /* Uint16 * */ref Uint16 blue);

		/*
		* Sets a portion of the colormap for the given 8-bit surface.	If 'surface'
		* is not a palettized surface, this function does nothing, returning 0.
		* If all of the colors were set as passed to SDL_SetColors(), it will
		* return 1.  If not all the color entries were set exactly as given,
		* it will return 0, and you should look at the surface palette to
		* determine the actual color palette.
		*	
		* When 'surface' is the surface associated with the current display, the
		* display colormap will be updated with the requested colors.	If	
		* SDL_HWPALETTE was set in SDL_SetVideoMode() flags, SDL_SetColors()
		* will always return 1, and the palette is guaranteed to be set the way
		* you desire, even if the window colormap has to be warped or run under
		* emulation.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SetColors(/* SDL_Surface * */IntPtr surface, 
		/* SDL_Color * */ref SDL_Color colors, int firstcolor, int ncolors);

		/*
		* Sets a portion of the colormap for a given 8-bit surface.
		* 'flags' is one or both of:
		* SDL_LOGPAL  -- set logical palette, which controls how blits are mapped
		*				  to/from the surface,
		* SDL_PHYSPAL -- set physical palette, which controls how pixels look on
		*				  the screen
		* Only screens have physical palettes. Separate change of physical/logical
		* palettes is only possible if the screen has SDL_HWPALETTE set.
		*	
		* The return value is 1 if all colours could be set as requested, and 0
		* otherwise.
		*	
		* SDL_SetColors() is equivalent to calling this function with
		*	   flags = (SDL_LOGPAL|SDL_PHYSPAL).
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SetPalette(/* SDL_Surface * */IntPtr surface, int flags,
		/* SDL_Color * */ref SDL_Color colors, int firstcolor, int ncolors);

		/*
		* Maps an RGB triple to an opaque pixel value for a given pixel format
		*/
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_MapRGB(/* SDL_PixelFormat * */IntPtr format, Uint8 r, Uint8 g, Uint8 b);

		/*
		* Maps an RGBA quadruple to a pixel value for a given pixel format
		*/
		[DllImport(DLL_SDL)]
		public static extern Uint32 SDL_MapRGBA(/* SDL_PixelFormat * */IntPtr format, Uint8 r, Uint8 g, Uint8 b, Uint8 a);

		/*
		* Maps a pixel value into the RGB components for a given pixel format
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_GetRGB(Uint32 pixel, /* SDL_PixelFormat * */IntPtr fmt,
		/* Uint8 * */ref Uint8 r, /* Uint8 * */ref Uint8 g, /* Uint8 * */ref Uint8 b);

		/*
		* Maps a pixel value into the RGBA components for a given pixel format
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_GetRGBA(Uint32 pixel, /* SDL_PixelFormat * */IntPtr fmt,
		/* Uint8 * */ref Uint8 r, /* Uint8 * */ref Uint8 g, /* Uint8 * */ref Uint8 b, /* Uint8 * */ref Uint8 a);

		/*
		* Allocate and free an RGB surface (must be called after SDL_SetVideoMode)
		* If the depth is 4 or 8 bits, an empty palette is allocated for the surface.
		* If the depth is greater than 8 bits, the pixel format is set using the
		* flags '[RGB]mask'.
		* If the function runs out of memory, it will return NULL.
		*	
		* The 'flags' tell what kind of surface to create.
		* SDL_SWSURFACE means that the surface should be created in system memory.
		* SDL_HWSURFACE means that the surface should be created in video memory,
		* with the same format as the display surface.  This is useful for surfaces
		* that will not change much, to take advantage of hardware acceleration
		* when being blitted to the display surface.
		* SDL_ASYNCBLIT means that SDL will try to perform asynchronous blits with
		* this surface, but you must always lock it before accessing the pixels.
		* SDL will wait for current blits to finish before returning from the lock.
		* SDL_SRCCOLORKEY indicates that the surface will be used for colorkey blits.
		* If the hardware supports acceleration of colorkey blits between
		* two surfaces in video memory, SDL will try to place the surface in
		* video memory. If this isn't possible or if there is no hardware
		* acceleration available, the surface will be placed in system memory.
		* SDL_SRCALPHA means that the surface will be used for alpha blits and 
		* if the hardware supports hardware acceleration of alpha blits between
		* two surfaces in video memory, to place the surface in video memory
		* if possible, otherwise it will be placed in system memory.
		* If the surface is created in video memory, blits will be _much_ faster,
		* but the surface format must be identical to the video surface format,
		* and the only way to access the pixels member of the surface is to use
		* the SDL_LockSurface() and SDL_UnlockSurface() calls.
		* If the requested surface actually resides in video memory, SDL_HWSURFACE
		* will be set in the flags member of the returned surface.  If for some
		* reason the surface could not be placed in video memory, it will not have
		* the SDL_HWSURFACE flag set, and will be created in system memory instead.
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Surface * */IntPtr SDL_CreateRGBSurface
		(Uint32 flags, int width, int height, int depth, 
		Uint32 Rmask, Uint32 Gmask, Uint32 Bmask, Uint32 Amask);
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Surface * */IntPtr SDL_CreateRGBSurfaceFrom(/* void * */IntPtr pixels,
		int width, int height, int depth, int pitch,
		Uint32 Rmask, Uint32 Gmask, Uint32 Bmask, Uint32 Amask);
		[DllImport(DLL_SDL)]
		public static extern void SDL_FreeSurface(/* SDL_Surface * */IntPtr surface);

		public static /* SDL_Surface * */IntPtr SDL_AllocSurface
		(Uint32 flags, int width, int height, int depth, 
			Uint32 Rmask, Uint32 Gmask, Uint32 Bmask, Uint32 Amask) {
			return SDL_CreateRGBSurface(flags, width, height, depth,
				Rmask, Gmask, Bmask, Amask);
		}			

		/*
		* SDL_LockSurface() sets up a surface for directly accessing the pixels.
		* Between calls to SDL_LockSurface()/SDL_UnlockSurface(), you can write
		* to and read from 'surface.pixels', using the pixel format stored in	
		* 'surface.format'.  Once you are done accessing the surface, you should 
		* use SDL_UnlockSurface() to release it.
		*	
		* Not all surfaces require locking.  If SDL_MUSTLOCK(surface) evaluates
		* to 0, then you can read and write to the surface at any time, and the
		* pixel format of the surface will not change.  In particular, if the
		* SDL_HWSURFACE flag is not given when calling SDL_SetVideoMode(), you
		* will not need to lock the display surface before accessing it.
		*	
		* No operating system or library calls should be made between lock/unlock
		* pairs, as critical system locks may be held during this time.
		*	
		* SDL_LockSurface() returns 0, or -1 if the surface couldn't be locked.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_LockSurface(/* SDL_Surface * */IntPtr surface);
		[DllImport(DLL_SDL)]
		public static extern void SDL_UnlockSurface(/* SDL_Surface * */IntPtr surface);

		/*
		* Load a surface from a seekable SDL data source (memory or file.)
		* If 'freesrc' is non-zero, the source will be closed after being read.
		* Returns the new surface, or NULL if there was an error.
		* The new surface should be freed with SDL_FreeSurface().
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Surface * */IntPtr SDL_LoadBMP_RW(/* SDL_RWops * */IntPtr src, int freesrc);

		/* Convenience macro -- load a surface from a file */
		public static /* SDL_Surface * */IntPtr SDL_LoadBMP(/* char* */string file) {
			return SDL_LoadBMP_RW(SDL_RWFromFile(file, "rb"), 1);
		}

		/*
		* Save a surface to a seekable SDL data source (memory or file.)
		* If 'freedst' is non-zero, the source will be closed after being written.
		* Returns 0 if successful or -1 if there was an error.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SaveBMP_RW
		(/* SDL_Surface * */IntPtr surface, /* SDL_RWops * */IntPtr dst, int freedst);

		/* Convenience macro -- save a surface to a file */
		public static int SDL_SaveBMP(/* SDL_Surface * */IntPtr surface, /* char* */string file) {
			return SDL_SaveBMP_RW(surface, SDL_RWFromFile(file, "wb"), 1);
		}

		/*
		* Sets the color key (transparent pixel) in a blittable surface.
		* If 'flag' is SDL_SRCCOLORKEY (optionally OR'd with SDL_RLEACCEL), 
		* 'key' will be the transparent pixel in the source image of a blit.
		* SDL_RLEACCEL requests RLE acceleration for the surface if present,
		* and removes RLE acceleration if absent.
		* If 'flag' is 0, this function clears any current color key.
		* This function returns 0, or -1 if there was an error.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SetColorKey
		(/* SDL_Surface * */IntPtr surface, Uint32 flag, Uint32 key);

		/*
		* This function sets the alpha value for the entire surface, as opposed to
		* using the alpha component of each pixel. This value measures the range
		* of transparency of the surface, 0 being completely transparent to 255
		* being completely opaque. An 'alpha' value of 255 causes blits to be
		* opaque, the source pixels copied to the destination (the default). Note
		* that per-surface alpha can be combined with colorkey transparency.
		*	
		* If 'flag' is 0, alpha blending is disabled for the surface.
		* If 'flag' is SDL_SRCALPHA, alpha blending is enabled for the surface.
		* OR:ing the flag with SDL_RLEACCEL requests RLE acceleration for the
		* surface; if SDL_RLEACCEL is not specified, the RLE accel will be removed.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_SetAlpha(/* SDL_Surface * */IntPtr surface, Uint32 flag, Uint8 alpha);

		/*
		* Sets the clipping rectangle for the destination surface in a blit.
		*	
		* If the clip rectangle is NULL, clipping will be disabled.
		* If the clip rectangle doesn't intersect the surface, the function will
		* return SDL_FALSE and blits will be completely clipped.  Otherwise the
		* function returns SDL_TRUE and blits to the surface will be clipped to
		* the intersection of the surface area and the clipping rectangle.
		*	
		* Note that blits are automatically clipped to the edges of the source
		* and destination surfaces.
		*/
		[DllImport(DLL_SDL)]
		public static extern SDL_bool SDL_SetClipRect(/* SDL_Surface * */IntPtr surface, /* SDL_Rect * */ref SDL_Rect rect);

		/*
		* Gets the clipping rectangle for the destination surface in a blit.
		* 'rect' must be a pointer to a valid rectangle which will be filled
		* with the correct values.
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_GetClipRect(/* SDL_Surface * */IntPtr surface, /* SDL_Rect * */ref SDL_Rect rect);

		/*
		* Creates a new surface of the specified format, and then copies and maps	
		* the given surface to it so the blit of the converted surface will be as	
		* fast as possible.  If this function fails, it returns NULL.
		*	
		* The 'flags' parameter is passed to SDL_CreateRGBSurface() and has those 
		* semantics.  You can also pass SDL_RLEACCEL in the flags parameter and
		* SDL will try to RLE accelerate colorkey and alpha blits in the resulting
		* surface.
		*	
		* This function is used internally by SDL_DisplayFormat().
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Surface * */IntPtr SDL_ConvertSurface
		(/* SDL_Surface * */IntPtr src, /* SDL_PixelFormat * */IntPtr fmt, Uint32 flags);

		/*
		* This performs a fast blit from the source surface to the destination
		* surface.  It assumes that the source and destination rectangles are
		* the same size.  If either 'srcrect' or 'dstrect' are NULL, the entire
		* surface (src or dst) is copied.	The final blit rectangles are saved
		* in 'srcrect' and 'dstrect' after all clipping is performed.
		* If the blit is successful, it returns 0, otherwise it returns -1.
		*	
		* The blit function should not be called on a locked surface.
		*	
		* The blit semantics for surfaces with and without alpha and colorkey
		* are defined as follows:
		*	
		* RGBA.RGB:
		*	   SDL_SRCALPHA set:
		* 	alpha-blend (using alpha-channel).
		* 	SDL_SRCCOLORKEY ignored.
		*	   SDL_SRCALPHA not set:
		* 	copy RGB.
		* 	if SDL_SRCCOLORKEY set, only copy the pixels matching the
		* 	RGB values of the source colour key, ignoring alpha in the
		* 	comparison.
		*	
		* RGB.RGBA:
		*	   SDL_SRCALPHA set:
		* 	alpha-blend (using the source per-surface alpha value);
		* 	set destination alpha to opaque.
		*	   SDL_SRCALPHA not set:
		* 	copy RGB, set destination alpha to opaque.
		*	   both:
		* 	if SDL_SRCCOLORKEY set, only copy the pixels matching the
		* 	source colour key.
		*	
		* RGBA.RGBA:
		*	   SDL_SRCALPHA set:
		* 	alpha-blend (using the source alpha channel) the RGB values;
		* 	leave destination alpha untouched. [Note: is this correct?]
		* 	SDL_SRCCOLORKEY ignored.
		*	   SDL_SRCALPHA not set:
		* 	copy all of RGBA to the destination.
		* 	if SDL_SRCCOLORKEY set, only copy the pixels matching the
		* 	RGB values of the source colour key, ignoring alpha in the
		* 	comparison.
		*	
		* RGB.RGB: 
		*	   SDL_SRCALPHA set:
		* 	alpha-blend (using the source per-surface alpha value).
		*	   SDL_SRCALPHA not set:
		* 	copy RGB.
		*	   both:
		* 	if SDL_SRCCOLORKEY set, only copy the pixels matching the
		* 	source colour key.
		*	
		* If either of the surfaces were in video memory, and the blit returns -2,
		* the video memory was lost, so it should be reloaded with artwork and 
		* re-blitted:
			while ( SDL_BlitSurface(image, imgrect, screen, dstrect) == -2 ) {
				while ( SDL_LockSurface(image) < 0 )
					Sleep(10);
				-- Write image pixels to image.pixels --
				SDL_UnlockSurface(image);
			}
		* This happens under DirectX 5.0 when the system switches away from your
		* fullscreen application.	The lock will also fail until you have access
		* to the video memory again.
		*/
		/* You should call SDL_BlitSurface() unless you know exactly how SDL
		blitting works internally and how to use the other blit functions.
		*/

		/* This is the public blit function, SDL_BlitSurface(), and it performs
		rectangle validation and clipping before passing it to SDL_LowerBlit()
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_UpperBlit
		(/* SDL_Surface * */IntPtr src, /* SDL_Rect * */IntPtr srcrect,
		/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */IntPtr dstrect);
		/* This is a semi-private blit function and it performs low-level surface
		blitting only.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_LowerBlit
		(/* SDL_Surface * */IntPtr src, /* SDL_Rect * */IntPtr srcrect,
		/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */IntPtr dstrect);

		unsafe private static int SDL_BlitSurface_
			(/* SDL_Surface * */IntPtr src, /* SDL_Rect * */ref SDL_Rect srcrect,
			/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */ref SDL_Rect dstrect) {
			fixed (SDL_Rect* s = &srcrect) {
				fixed (SDL_Rect* d = &dstrect) {
					return SDL_UpperBlit(src, (IntPtr)s, dst, (IntPtr)d);
				}
			}
		}

		public static int SDL_BlitSurface
		(/* SDL_Surface * */IntPtr src, /* SDL_Rect * */ref SDL_Rect srcrect,
		/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */ref SDL_Rect dstrect) {
			return SDL_BlitSurface_(src, ref srcrect, dst, ref dstrect);
		}
		public static int SDL_BlitSurface
			(/* SDL_Surface * */IntPtr src, /* SDL_Rect * */IntPtr srcrect,
			/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */IntPtr dstrect) {
			return SDL_UpperBlit(src, srcrect, dst, dstrect);
		}
				 
		/*
		* This function performs a fast fill of the given rectangle with 'color'
		* The given rectangle is clipped to the destination surface clip area
		* and the final fill rectangle is saved in the passed in pointer.
		* If 'dstrect' is NULL, the whole surface will be filled with 'color'
		* The color should be a pixel of the format used by the surface, and 
		* can be generated by the SDL_MapRGB() function.
		* This function returns 0 on success, or -1 on error.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_FillRect
		(/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */IntPtr dstrect, Uint32 color);
		public static int SDL_FillRect
		(/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */ref SDL_Rect dstrect, Uint32 color) {
			return SDL_FillRect_(dst, ref dstrect, color);
		}
		unsafe private static int SDL_FillRect_
		(/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */ref SDL_Rect dstrect, Uint32 color) {
			fixed (SDL_Rect* d = &dstrect) {
				return SDL_FillRect(dst, (IntPtr)d, color);
			}
		}

		/*	
		* This function takes a surface and copies it to a new surface of the
		* pixel format and colors of the video framebuffer, suitable for fast
		* blitting onto the display surface.  It calls SDL_ConvertSurface()
		*	
		* If you want to take advantage of hardware colorkey or alpha blit
		* acceleration, you should set the colorkey and alpha value before
		* calling this function.
		*	
		* If the conversion fails or runs out of memory, it returns NULL
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Surface * */IntPtr SDL_DisplayFormat(/* SDL_Surface * */IntPtr surface);

		/*	
		* This function takes a surface and copies it to a new surface of the
		* pixel format and colors of the video framebuffer (if possible),
		* suitable for fast alpha blitting onto the display surface.
		* The new surface will always have an alpha channel.
		*	
		* If you want to take advantage of hardware colorkey or alpha blit
		* acceleration, you should set the colorkey and alpha value before
		* calling this function.
		*	
		* If the conversion fails or runs out of memory, it returns NULL
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Surface * */IntPtr SDL_DisplayFormatAlpha(/* SDL_Surface * */IntPtr surface);


		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		/* YUV video surface overlay functions										 */
		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

		/* This function creates a video output overlay
		Calling the returned surface an overlay is something of a misnomer because
		the contents of the display surface underneath the area where the overlay
		is shown is undefined - it may be overwritten with the converted YUV data.
		*/
		[DllImport(DLL_SDL)]
		public static extern /* SDL_Overlay * */IntPtr SDL_CreateYUVOverlay(int width, int height,
		Uint32 format, /* SDL_Surface * */IntPtr display);

		/* Lock an overlay for direct access, and unlock it when you are done */
		[DllImport(DLL_SDL)]
		public static extern int SDL_LockYUVOverlay(/* SDL_Overlay * */IntPtr overlay);
		[DllImport(DLL_SDL)]
		public static extern void SDL_UnlockYUVOverlay(/* SDL_Overlay * */IntPtr overlay);

		/* Blit a video overlay to the display surface.
		The contents of the video surface underneath the blit destination are
		not defined. 	
		The width and height of the destination rectangle may be different from
		that of the overlay, but currently only 2x scaling is supported.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_DisplayYUVOverlay(/* SDL_Overlay * */IntPtr overlay, /* SDL_Rect * */ref SDL_Rect dstrect);

		/* Free a video overlay */
		[DllImport(DLL_SDL)]
		public static extern void SDL_FreeYUVOverlay(/* SDL_Overlay * */IntPtr overlay);


		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		/* OpenGL support functions.												 */
		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

		/*
		* Dynamically load a GL driver, if SDL is built with dynamic GL.
		*	
		* SDL links normally with the OpenGL library on your system by default,
		* but you can compile it to dynamically load the GL driver at runtime.
		* If you do this, you need to retrieve all of the GL functions used in
		* your program from the dynamic library using SDL_GL_GetProcAddress().
		*	
		* This is disabled in default builds of SDL.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_GL_LoadLibrary(/* char * */string path);

		/*
		* Get the address of a GL function (for extension functions)
		*/
		[DllImport(DLL_SDL)]
		public static extern /* void * */IntPtr SDL_GL_GetProcAddress(/* char* */string proc);

		/*
		* Set an attribute of the OpenGL subsystem before intialization.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_GL_SetAttribute(SDL_GLattr attr, int value);

		/*
		* Get an attribute of the OpenGL subsystem from the windowing
		* interface, such as glX. This is of course different from getting
		* the values from SDL'name internal OpenGL subsystem, which only
		* stores the values you request before initialization.
		*	
		* Developers should track the values they pass into SDL_GL_SetAttribute
		* themselves if they want to retrieve these values.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_GL_GetAttribute(SDL_GLattr attr, /* int* */ref int value);

		/*
		* Swap the OpenGL buffers, if double-buffering is supported.
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_GL_SwapBuffers();

		/*
		* Internal functions that should not be called unless you have read
		* and understood the source code for these functions.
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_GL_UpdateRects(int numrects, /* SDL_Rect* */ref SDL_Rect rects);
		[DllImport(DLL_SDL)]
		public static extern void SDL_GL_Lock();
		[DllImport(DLL_SDL)]
		public static extern void SDL_GL_Unlock();

		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */
		/* These functions allow interaction with the window manager, if any.		 */
		/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

		/*
		* Sets/Gets the title and icon text of the display window
		*/
		[DllImport(DLL_SDL)]
		// public static extern void SDL_WM_SetCaption(/* char * */string title, /* char * */string icon);
		public static extern void SDL_WM_SetCaption(/* char * */byte[] title, /* char * */string icon);
		[DllImport(DLL_SDL)]
		public static extern void SDL_WM_GetCaption(/* char ** */ref string title, /* char ** */ref string icon);

		/*
		* Sets the icon for the display window.
		* This function must be called before the first call to SDL_SetVideoMode().
		* It takes an icon surface, and a mask in MSB format.
		* If 'mask' is NULL, the entire icon surface will be used as the icon.
		*/
		[DllImport(DLL_SDL)]
		public static extern void SDL_WM_SetIcon(/* SDL_Surface * */IntPtr icon, /* Uint8 * */IntPtr mask);

		/*
		* This function iconifies the window, and returns 1 if it succeeded.
		* If the function succeeds, it generates an SDL_APPACTIVE loss event.
		* This function is a noop and returns 0 in non-windowed environments.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_WM_IconifyWindow();

		/*
		* Toggle fullscreen mode without changing the contents of the screen.
		* If the display surface does not require locking before accessing
		* the pixel information, then the memory pointers will not change.
		*	
		* If this function was able to toggle fullscreen mode (change from 
		* running in a window to fullscreen, or vice-versa), it will return 1.
		* If it is not implemented, or fails, it returns 0.
		*	
		* The next call to SDL_SetVideoMode() will set the mode fullscreen
		* attribute based on the flags parameter - if SDL_FULLSCREEN is not
		* set, then the display will be windowed by default where supported.
		*	
		* This is currently only implemented in the X11 video driver.
		*/
		[DllImport(DLL_SDL)]
		public static extern int SDL_WM_ToggleFullScreen(/* SDL_Surface * */IntPtr surface);

		/*
		* This function allows you to set and query the input grab state of
		* the application.  It returns the new input grab state.
		*/
		public enum SDL_GrabMode : int {
			SDL_GRAB_QUERY = -1,
			SDL_GRAB_OFF = 0,
			SDL_GRAB_ON = 1,
			SDL_GRAB_FULLSCREEN	/* Used internally */
		}
		/*
		* Grabbing means that the mouse is confined to the application window,
		* and nearly all keyboard input is passed directly to the application,
		* and not interpreted by a window manager, if any.
		*/
		[DllImport(DLL_SDL)]
		public static extern SDL_GrabMode SDL_WM_GrabInput(SDL_GrabMode mode);

		/* Not in public API at the moment - do not use! */
		[DllImport(DLL_SDL)]
		public static extern int SDL_SoftStretch(/* SDL_Surface * */IntPtr src, /* SDL_Rect * */ref SDL_Rect srcrect,
		/* SDL_Surface * */IntPtr dst, /* SDL_Rect * */ref SDL_Rect dstrect);
		#endregion
	}

	/*
	internal class SDLVideoInitializer : IDisposable {
		public SDLVideoInitializer() {
			success = SDL.SDL_InitSubSystem(SDL.SDL_INIT_VIDEO) == 0;
		}

		~SDLVideoInitializer() { Dispose(false); }

		private bool success;
		private bool disposed;

		protected void Dispose(bool disposing) {
			if (!disposed) {
				if (disposing) {
				}
				if (success) SDL.SDL_QuitSubSystem(SDL.SDL_INIT_VIDEO);
				disposed = true;
			}
		}

		#region IDisposable メンバ

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
	*/
}
