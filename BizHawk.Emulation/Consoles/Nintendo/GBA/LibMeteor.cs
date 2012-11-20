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
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MessageCallback(string msg);

		/// <summary>
		/// set callback for log messages.  this can (and should) be called first
		/// </summary>
		/// <param name="cb"></param>
		[DllImport("libmeteor.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libmeteor_setmessagecallback(MessageCallback cb);
	}
}
