using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Sdl;
using SMPEGPtr = System.IntPtr; // SMPEG*

namespace Yanesdk.Movie
{
	/// <summary>
	/// SMPEGä÷åWÇÃÅB
	/// </summary>
	[CLSCompliant(false)]
	public static class SMPEG
	{
		const string DLL_SMPEG = Yanesdk.Sdl.SDL_Initializer.DLL_SMPEG;

		/* Used to get information about the SMPEG object */
		[StructLayout(LayoutKind.Sequential)]
		public struct SMPEG_Info
		{
			public int has_audio;
			public int has_video;
			public int width;
			public int height;
			public int current_frame;
			public double current_fps;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string audio_string;
			public int audio_current_frame;
			public UInt32 current_offset;
			public UInt32 total_size;
			public double current_time;
			public double total_time;
		}

		/* Possible MPEG status codes */
		public enum SMPEGstatus
		{
			SMPEG_ERROR = -1,
			SMPEG_STOPPED,
			SMPEG_PLAYING
		}

		/* Matches the declaration of SDL_UpdateRect() */
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SMPEG_DisplayCallback(/*SDL.SDL_Surface* */IntPtr dst, int x, int y, uint w, uint h);

		/* Create a new SMPEG object from an MPEG file.
		   On return, if 'info' is not NULL, it will be filled with information 
		   about the MPEG object.
		   This function returns a new SMPEG object.  Use SMPEG_error() to find out
		   whether or not there was a problem building the MPEG stream.
		   The sdl_audio parameter indicates if SMPEG should initialize the SDL audio
		   subsystem. If not, you will have to use the SMPEG_playaudio() function below
		   to extract the decoded data.
		 */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern SMPEGPtr SMPEG_new(string file, ref SMPEG_Info info, int sdl_audio);

		/* The same as above for a file descriptor */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern SMPEGPtr SMPEG_new_descr(int file, ref SMPEG_Info info, int sdl_audio);

		/*
		   The same as above but for a raw chunk of data.  SMPEG makes a copy of the
		   data, so the application is free to delete after a successful call to this
		   function.
		 */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern SMPEGPtr SMPEG_new_data(IntPtr data, int size, ref SMPEG_Info info, int sdl_audio);

		/* The same for a generic SDL_RWops structure. */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern SMPEGPtr SMPEG_new_rwops(SDL_RWops src, ref SMPEG_Info info, int sdl_audio);

		/* Get current information about an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_getinfo(SMPEGPtr mpeg, ref SMPEG_Info info);

		/* Enable or disable audio playback in MPEG stream */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_enableaudio(SMPEGPtr mpeg, int enable);

		/* Enable or disable video playback in MPEG stream */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_enablevideo(SMPEGPtr mpeg, int enable);

		/* Delete an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_delete(SMPEGPtr mpeg);

		/* Get the current status of an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern SMPEGstatus SMPEG_status(SMPEGPtr mpeg);

		/* Set the audio volume of an MPEG stream, in the range 0-100 */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_setvolume(SMPEGPtr mpeg, int volume);

		/* Set the destination surface for MPEG video playback
		   'surfLock' is a mutex used to synchronize access to 'dst', and can be NULL.
		   'callback' is a function called when an area of 'dst' needs to be updated.
		   If 'callback' is NULL, the default function (SDL_UpdateRect) will be used.
		*/
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_setdisplay(SMPEGPtr mpeg, /*SDL.SDL_Surface*/IntPtr dst, /*SDL.SDL_mutex*/IntPtr surfLock,
													SMPEG_DisplayCallback callback);

		/* Set or clear looping play on an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_loop(SMPEGPtr mpeg, int repeat);

		/* Scale pixel display on an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_scaleXY(SMPEGPtr mpeg, int width, int height);
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_scale(SMPEGPtr mpeg, int scale);
		public static void SMPEG_double(SMPEGPtr mpeg, bool on) {
			SMPEG_scale(mpeg, on ? 2 : 1);
		}

		/* Move the video display area within the destination surface */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_move(SMPEGPtr mpeg, int x, int y);

		/* Set the region of the video to be shown */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_setdisplayregion(SMPEGPtr mpeg, int x, int y, int w, int h);

		/* Play an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_play(SMPEGPtr mpeg);

		/* Pause/Resume playback of an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_pause(SMPEGPtr mpeg);

		/* Stop playback of an SMPEG object */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_stop(SMPEGPtr mpeg);

		/* Rewind the play position of an SMPEG object to the beginning of the MPEG */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_rewind(SMPEGPtr mpeg);

		/* Seek 'bytes' bytes in the MPEG stream */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_seek(SMPEGPtr mpeg, int bytes);

		/* Skip 'seconds' seconds in the MPEG stream */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_skip(SMPEGPtr mpeg, float seconds);

		/* Render a particular frame in the MPEG video
		   API CHANGE: This function no longer takes a target surface and position.
					   Use SMPEG_setdisplay() and SMPEG_move() to set this information.
		*/
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_renderFrame(SMPEGPtr mpeg, int framenum);

		/* Render the last frame of an MPEG video */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_renderFinal(SMPEGPtr mpeg, /*SDL.SDL_Surface*/IntPtr dst, int x, int y);

		/* Set video filter */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern /*SMPEG_Filter* */IntPtr SMPEG_filter(SMPEGPtr mpeg, ref SMPEG_Filter filter);
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern /*SMPEG_Filter* */IntPtr SMPEG_filter(SMPEGPtr mpeg, /*SMPEG_Filter* */IntPtr filter);

		/* Return NULL if there is no error in the MPEG stream, or an error message
		   if there was a fatal error in the MPEG stream for the SMPEG object.
		*/
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public static extern /* char* */ string SMPEG_error(SMPEGPtr mpeg);

		/* Exported callback function for audio playback.
		   The function takes a buffer and the amount of data to fill, and returns
		   the amount of data in bytes that was actually written.  This will be the
		   amount requested unless the MPEG audio has finished.
		*/
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SMPEG_playAudio(SMPEGPtr mpeg, /* UInt8* */ IntPtr stream, int len);

		/* Wrapper for SMPEG_playAudio() that can be passed to SDL and SDL_mixer */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_playAudioSDL(SMPEGPtr mpeg, /* UInt8* */ IntPtr stream, int len);

		/* Get the best SDL audio spec for the audio stream */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SMPEG_wantedSpec(SMPEGPtr mpeg, /*SDL.SDL_AudioSpec*/IntPtr wanted);
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern int SMPEG_wantedSpec(SMPEGPtr mpeg, ref SDL.SDL_AudioSpec wanted);

		/* Inform SMPEG of the actual SDL audio spec used for sound playback */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_actualSpec(SMPEGPtr mpeg, /*SDL.SDL_AudioSpec*/IntPtr spec);
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern void SMPEG_actualSpec(SMPEGPtr mpeg, ref SDL.SDL_AudioSpec spec);

		/* SMPEG filter info flags */
		public const int SMPEG_FILTER_INFO_MB_ERROR = 1;
		public const int SMPEG_FILTER_INFO_PIXEL_ERROR = 2;

		/* Filter info from SMPEG */
		[StructLayout(LayoutKind.Sequential)]
		public struct SMPEG_FilterInfo
		{
			/* Uint16* */IntPtr yuv_mb_square_error;
			/* Uint16* */IntPtr yuv_pixel_square_error;
		}

		/* The filter definition itself */
		[StructLayout(LayoutKind.Sequential)]
		public struct SMPEG_Filter
		{
			public UInt32 flags;
			public IntPtr data;
			public SMPEG_FilterCallback callback;
			public SMPEG_FilterDestroy destroy;
		}

		/* Callback functions for the filter */
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SMPEG_FilterCallback(/*SDL.SDL_Overlay* */IntPtr dest,
			/*SDL.SDL_Overlay* */IntPtr source, /*SDL.SDL_Rect* */IntPtr region,
			ref SMPEG_FilterInfo filter_info, IntPtr data);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SMPEG_FilterDestroy(/*SMPEG_Filter* */IntPtr filter);

		/* The null filter (default). It simply copies the source rectangle to the video overlay. */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern /*SMPEG_Filter* */IntPtr SMPEGfilter_null();

		/* The bilinear filter. A basic low-pass filter that will produce a smoother image. */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern /*SMPEG_Filter* */IntPtr SMPEGfilter_bilinear();

		/* The deblocking filter. It filters block borders and non-intra coded blocks to reduce blockiness */
		[DllImport(DLL_SMPEG, CallingConvention = CallingConvention.Cdecl)]
		public static extern /*SMPEG_Filter* */IntPtr SMPEGfilter_deblocking();
	}
}
