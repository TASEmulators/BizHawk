using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Sega.Saturn
{
	public static class LibYabause
	{
		/// <summary>
		/// A,B,C,Start,DPad
		/// </summary>
		public enum Buttons1 : byte
		{
			B = 0x01,
			C = 0x02,
			A = 0x04,
			S = 0x08,
			U = 0x10,
			D = 0x20,
			L = 0x40,
			R = 0x80
		}

		/// <summary>
		/// X,Y,Z,Shoulders
		/// </summary>
		public enum Buttons2 : byte
		{
			L = 0x08,
			Z = 0x10,
			Y = 0x20,
			X = 0x40,
			R = 0x80
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="p11">player1</param>
		/// <param name="p12">player1</param>
		/// <param name="p21">player2</param>
		/// <param name="p22">player2</param>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_setpads(Buttons1 p11, Buttons2 p12, Buttons1 p21, Buttons2 p22);


		/// <summary>
		/// set video buffer
		/// </summary>
		/// <param name="buff">704x512x32bit, should persist over time</param>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_setvidbuff(IntPtr buff);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buff">persistent location of s16 interleaved</param>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_setsndbuff(IntPtr buff);

		/// <summary>
		/// soft reset, or something like that
		/// </summary>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_softreset();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="w">width of framebuffer</param>
		/// <param name="h">height of framebuffer</param>
		/// <param name="nsamp">number of sample pairs produced</param>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_frameadvance(out int w, out int h, out int nsamp);

		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void libyabause_deinit();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="intf">cd interface.  struct need not persist after call, but the function pointers better</param>
		/// <returns></returns>
		[DllImport("libyabause.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool libyabause_init(ref CDInterface intf);

		public struct CDInterface
		{
			public int DontTouch;
			public IntPtr DontTouch2;
			/// <summary>
			/// init cd functions
			/// </summary>
			/// <param name="unused"></param>
			/// <returns>0 on success, -1 on failure</returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int Init(string unused);
			public Init InitFunc;
			/// <summary>
			/// deinit cd functions
			/// </summary>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void DeInit();
			public DeInit DeInitFunc;
			/// <summary>
			/// 0 = cd present, spinning
			/// 1 = cd present, not spinning
			/// 2 = no cd
			/// 3 = tray open
			/// </summary>
			/// <returns></returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int GetStatus();
			public GetStatus GetStatusFunc;
			/// <summary>
			/// read all TOC entries
			/// </summary>
			/// <param name="dest">place to copy to</param>
			/// <returns>number of bytes written.  should be 408</returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int ReadTOC(IntPtr dest);
			public ReadTOC ReadTOCFunc;
			/// <summary>
			/// read a sector, should be 2352 bytes
			/// </summary>
			/// <param name="FAD"></param>
			/// <param name="dest"></param>
			/// <returns></returns>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate int ReadSectorFAD(int FAD, IntPtr dest);
			public ReadSectorFAD ReadSectorFADFunc;
			/// <summary>
			/// hint the next sector, for async loading
			/// </summary>
			/// <param name="FAD"></param>
			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void ReadAheadFAD(int FAD);
			public ReadAheadFAD ReadAheadFADFunc;
		}
	}
}
