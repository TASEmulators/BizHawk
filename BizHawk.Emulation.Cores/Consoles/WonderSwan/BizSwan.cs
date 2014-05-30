using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	public static class BizSwan
	{
		const CallingConvention cc = CallingConvention.Cdecl;
		const string dd = "bizswan.dll";

		/// <summary>
		/// create new instance
		/// </summary>
		/// <returns></returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern IntPtr bizswan_new();

		/// <summary>
		/// delete instance, freeing all associated memory
		/// </summary>
		/// <param name="core"></param>
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_delete(IntPtr core);

		/// <summary>
		/// hard reset
		/// </summary>
		/// <param name="core"></param>
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_reset(IntPtr core);

		/// <summary>
		/// frame advance
		/// </summary>
		/// <param name="core"></param>
		/// <param name="buttons">input to use on this frame</param>
		/// <param name="novideo">true to skip all video rendering</param>
		/// <param name="surface">uint32 video output buffer</param>
		/// <param name="soundbuff">int16 sound output buffer</param>
		/// <param name="soundbuffsize">[In] max hold size of soundbuff [Out] number of samples actually deposited</param>
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_advance(IntPtr core, Buttons buttons, bool novideo, int[] surface, short[] soundbuff, ref int soundbuffsize);

		/// <summary>
		/// load rom
		/// </summary>
		/// <param name="core"></param>
		/// <param name="data"></param>
		/// <param name="length"></param>
		/// <param name="settings"></param>
		/// <returns></returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_load(IntPtr core, byte[] data, int length, [In] ref SyncSettings settings);

		/// <summary>
		/// get size of saveram
		/// </summary>
		/// <param name="core"></param>
		/// <returns></returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern int bizswan_saveramsize(IntPtr core);

		/// <summary>
		/// load saveram into core
		/// </summary>
		/// <param name="core"></param>
		/// <param name="data"></param>
		/// <param name="size">should be same as bizswan_saveramsize()</param>
		/// <returns>false if size mismatch</returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_saveramload(IntPtr core, byte[] data, int size);

		/// <summary>
		/// save saveram from core
		/// </summary>
		/// <param name="core"></param>
		/// <param name="data"></param>
		/// <param name="maxsize">should be same as bizswan_saveramsize()</param>
		/// <returns>false if size mismatch</returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_saveramsave(IntPtr core, byte[] data, int maxsize);

		[Flags]
		public enum Buttons : ushort
		{
			UpX = 0x0001,
			DownX = 0x0002,
			LeftX = 0x0004,
			RightX = 0x0008,
			UpY = 0x0010,
			DownY = 0x0020,
			LeftY = 0x0040,
			RightY = 0x0080,
			Start = 0x0100,
			B = 0x0200,
			A = 0x0400,
		}

		public enum Language : byte
		{
			Japanese = 0,
			English = 1
		}

		public enum Bloodtype : byte
		{
			A = 1,
			B = 2,
			O = 3,
			AB = 4
		}

		public enum Gender : byte
		{
			Male = 1,
			Female = 2
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct SyncSettings
		{
			public ushort byear;
			public byte bmonth;
			public byte bday;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
			public byte[] name;
			public Language language;
			public Gender sex;
			public Bloodtype blood;
			[MarshalAs(UnmanagedType.U1)]
			public bool rotateinput;
			[MarshalAs(UnmanagedType.U1)]
			public bool color; // true for color system
			[MarshalAs(UnmanagedType.U1)]
			public bool userealtime; // true for use real real RTC instead of emulation pegged time
			public ulong initialtime; // inital time in unix format; only used when userealtime = false

			public void SetName(string newname)
			{
				byte[] data = Encoding.ASCII.GetBytes(newname);
				name = new byte[17];
				Buffer.BlockCopy(data, 0, name, 0, Math.Min(data.Length, name.Length));
			}
		}
	}
}
