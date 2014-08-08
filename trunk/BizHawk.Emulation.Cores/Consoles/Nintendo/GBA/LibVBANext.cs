using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public class LibVBANext
	{
		const string dllname = "libvbanext.dll";
		const CallingConvention cc = CallingConvention.Cdecl;

		[Flags]
		public enum Buttons : int
		{
			A = 1,
			B = 2,
			Select = 4,
			Start = 8,
			Right = 16,
			Left = 32,
			Up = 64,
			Down = 128,
			R = 256,
			L = 512
		}

		/// <summary>
		/// create a new context
		/// </summary>
		/// <returns></returns>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern IntPtr Create();

		/// <summary>
		/// destroy a context
		/// </summary>
		/// <param name="g"></param>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void Destroy(IntPtr g);

		/// <summary>
		/// load a rom
		/// </summary>
		/// <param name="g"></param>
		/// <param name="romfile"></param>
		/// <param name="romfilelen"></param>
		/// <param name="biosfile"></param>
		/// <param name="biosfilelen"></param>
		/// <returns>success</returns>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool LoadRom(IntPtr g, byte[] romfile, uint romfilelen, byte[] biosfile, uint biosfilelen);

		/// <summary>
		/// hard reset
		/// </summary>
		/// <param name="g"></param>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void Reset(IntPtr g);
		
		/// <summary>
		/// frame advance
		/// </summary>
		/// <param name="g"></param>
		/// <param name="input"></param>
		/// <param name="videobuffer">240x160 packed argb32</param>
		/// <param name="audiobuffer">buffer to recieve stereo audio</param>
		/// <param name="numsamp">number of samples created</param>
		/// <returns>true if lagged</returns>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool FrameAdvance(IntPtr g, Buttons input, int[] videobuffer, short[] audiobuffer, out int numsamp);
	}
}
