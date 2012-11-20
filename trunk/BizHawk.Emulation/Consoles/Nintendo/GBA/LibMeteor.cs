using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Nintendo.GBA
{
	/// <summary>
	/// bindings into libmeteor.dll
	/// </summary>
	public static class LibMeteor
	{
		/// <summary>
		/// reset the emulation core
		/// </summary>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_reset();

		/// <summary>
		/// signal that you are removing data from the sound buffer.
		/// the next time frameadvance() is called, writing will start from the beginning
		/// </summary>
		/// <returns>the valid length of the buffer, in bytes</returns>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint libmeteor_emptysound();

		/// <summary>
		/// set up buffers for libmeteor to dump data to.  these must be valid before every frameadvance
		/// </summary>
		/// <param name="vid">buffer to hold video data as BGRA32</param>
		/// <param name="vidlen">length in bytes.  must be at least 240 * 160 * 4</param>
		/// <param name="aud">buffer to hold audio data as stereo s16le</param>
		/// <param name="audlen">length in bytes.  must be 0 mod 4 (hold a full stereo sample set)</param>
		/// <returns>false if some problem.  buffers will not be valid in this case</returns>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool libmeteor_setbuffers(IntPtr vid, uint vidlen, IntPtr aud, uint audlen);

		/// <summary>
		/// initialize the library
		/// </summary>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_init();

		/// <summary>
		/// run emulation for one frame, updating sound and video along the way
		/// </summary>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_frameadvance();

		/// <summary>
		/// load a rom image
		/// </summary>
		/// <param name="data">raw rom data. need not persist past this call</param>
		/// <param name="datalen">length of data in bytes</param>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_loadrom(byte[] data, uint datalen);
		
		/// <summary>
		/// load a bios image
		/// </summary>
		/// <param name="data">raw bios data. need not persist past this call</param>
		/// <param name="datalen">length of data in bytes</param>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_loadbios(byte[] data, uint datalen);

		/// <summary>
		/// core callback to print meaningful (or meaningless) log messages
		/// </summary>
		/// <param name="msg">message to be printed</param>
		/// <param name="abort">true if emulation should be aborted</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MessageCallback(string msg, bool abort);

		/// <summary>
		/// set callback for log messages.  this can (and should) be called first
		/// </summary>
		/// <param name="cb"></param>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_setmessagecallback(MessageCallback cb);

		/// <summary>
		/// combination of button flags used by the key callback
		/// </summary>
		[Flags]
		public enum Buttons : ushort
		{
			BTN_A      = 0x001,
			BTN_B      = 0x002,
			BTN_SELECT = 0x004,
			BTN_START  = 0x008,
			BTN_RIGHT  = 0x010,
			BTN_LEFT   = 0x020,
			BTN_UP     = 0x040,
			BTN_DOWN   = 0x080,
			BTN_R      = 0x100,
			BTN_L      = 0x200
		}

		/// <summary>
		/// core callback to get input state
		/// </summary>
		/// <returns>buttons pressed bitfield</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate Buttons InputCallback();

		/// <summary>
		/// set callback for whenever input is requested
		/// </summary>
		/// <param name="callback"></param>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_setkeycallback(InputCallback callback);

		/// <summary>
		/// parameter to libmeteor_getmemoryarea
		/// </summary>
		public enum MemoryArea : int
		{
			bios = 0,
			ewram = 1,
			iwram = 2,
			palram = 3,
			vram = 4,
			oam = 5,
			rom = 6
		}

		/// <summary>
		/// return a pointer to a memory area
		/// </summary>
		/// <param name="which"></param>
		/// <returns>IntPtr.Zero if which is unrecognized</returns>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr libmeteor_getmemoryarea(MemoryArea which);
	}
}
