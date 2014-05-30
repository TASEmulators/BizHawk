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
		/// <returns>true if lagged</returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_advance(IntPtr core, Buttons buttons, bool novideo, int[] surface, short[] soundbuff, ref int soundbuffsize);

		/// <summary>
		/// load rom
		/// </summary>
		/// <param name="core"></param>
		/// <param name="data"></param>
		/// <param name="length"></param>
		/// <param name="settings"></param>
		/// <param name="IsRotated">(out) true if screen is rotated left 90</param>
		/// <returns></returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_load(IntPtr core, byte[] data, int length, [In] ref SyncSettings settings, ref bool IsRotated);

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

		/// <summary>
		/// put non-sync settings, can be done at any time
		/// </summary>
		/// <param name="core"></param>
		/// <param name="settings"></param>
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_putsettings(IntPtr core, [In] ref Settings settings);

		/// <summary>
		/// get a memory area
		/// </summary>
		/// <param name="core"></param>
		/// <param name="index">start at 0, increment until return is false</param>
		/// <param name="name"></param>
		/// <param name="size"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_getmemoryarea(IntPtr core, int index, out IntPtr name, out int size, out IntPtr data);


		/// <summary>
		/// return a CPU register
		/// </summary>
		/// <param name="core"></param>
		/// <param name="which"></param>
		/// <returns></returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern uint bizswan_getnecreg(IntPtr core, NecRegs which);

		public const NecRegs NecRegsMin = NecRegs.PC;
		public const NecRegs NecRegsMax = NecRegs.DS0;

		public enum NecRegs : int
		{
			PC = 1,
			AW = 2,
			CW = 3,
			DW = 4,
			BW = 5,
			SP = 6,
			BP = 7,
			IX = 8,
			IY = 9,
			FLAGS = 10,
			DS1 = 11,
			PS = 12,
			SS = 13,
			DS0 = 14
		};

		[Flags]
		public enum Buttons : ushort
		{
			UpX = 0x0001,
			RightX = 0x0002,
			DownX = 0x0004,
			LeftX = 0x0008,
			UpY = 0x0010,
			RightY = 0x0020,
			DownY = 0x0040,
			LeftY = 0x0080,
			Start = 0x0100,
			A = 0x0200,
			B = 0x0400,
		}

		public enum Language : uint
		{
			Japanese = 0,
			English = 1
		}

		public enum Bloodtype : uint
		{
			A = 1,
			B = 2,
			O = 3,
			AB = 4
		}

		public enum Gender : uint
		{
			Male = 1,
			Female = 2
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct SyncSettings
		{
			public ulong initialtime; // inital time in unix format; only used when userealtime = false

			public uint byear;
			public uint bmonth;
			public uint bday;

			[MarshalAs(UnmanagedType.Bool)]
			public bool color; // true for color system
			[MarshalAs(UnmanagedType.Bool)]
			public bool userealtime; // true for use real real RTC instead of emulation pegged time

			public Language language;
			public Gender sex;
			public Bloodtype blood;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
			public byte[] name;

			public void SetName(string newname)
			{
				byte[] data = Encoding.ASCII.GetBytes(newname);
				name = new byte[17];
				Buffer.BlockCopy(data, 0, name, 0, Math.Min(data.Length, name.Length));
			}
		}

		[Flags]
		public enum LayerFlags : uint
		{
			BG = 1,
			FG = 2,
			Sprite = 4
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct Settings
		{
			public LayerFlags LayerMask; // 1 = show
		}
	}
}
