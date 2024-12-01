using System.Text;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	public static class BizSwan
	{
		private const CallingConvention cc = CallingConvention.Cdecl;
		private const string dd = "bizswan.dll";

		/// <summary>
		/// create new instance
		/// </summary>
		[DllImport(dd, CallingConvention = cc)]
		public static extern IntPtr bizswan_new();

		/// <summary>
		/// delete instance, freeing all associated memory
		/// </summary>
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_delete(IntPtr core);

		/// <summary>
		/// hard reset
		/// </summary>
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_reset(IntPtr core);

		/// <summary>
		/// frame advance
		/// </summary>
		/// <param name="buttons">input to use on this frame</param>
		/// <param name="novideo">true to skip all video rendering</param>
		/// <param name="surface">uint32 video output buffer</param>
		/// <param name="soundbuff">int16 sound output buffer</param>
		/// <param name="soundbuffsize">[In] max hold size of soundbuff [Out] number of samples actually deposited</param>
		/// <param name="IsRotated">(out) true if the screen is rotated left 90</param>
		/// <returns>true if lagged</returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_advance(IntPtr core, Buttons buttons, bool novideo, int[] surface, short[] soundbuff, ref int soundbuffsize, ref bool IsRotated);

		/// <summary>
		/// load rom
		/// </summary>
		/// <param name="IsRotated">(out) true if screen is rotated left 90</param>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_load(IntPtr core, byte[] data, int length, [In] ref SyncSettings settings, ref bool IsRotated);

		/// <summary>
		/// get size of saveram
		/// </summary>
		[DllImport(dd, CallingConvention = cc)]
		public static extern int bizswan_saveramsize(IntPtr core);

		/// <summary>
		/// load saveram into core
		/// </summary>
		/// <param name="size">should be same as bizswan_saveramsize()</param>
		/// <returns>false if size mismatch</returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_saveramload(IntPtr core, byte[] data, int size);

		/// <summary>
		/// save saveram from core
		/// </summary>
		/// <param name="maxsize">should be same as bizswan_saveramsize()</param>
		/// <returns>false if size mismatch</returns>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_saveramsave(IntPtr core, byte[] data, int maxsize);

		/// <summary>
		/// put non-sync settings, can be done at any time
		/// </summary>
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_putsettings(IntPtr core, [In] ref Settings settings);

		/// <summary>
		/// get a memory area
		/// </summary>
		/// <param name="index">start at 0, increment until return is false</param>
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_getmemoryarea(IntPtr core, int index, out IntPtr name, out int size, out IntPtr data);

		[DllImport(dd, CallingConvention = cc)]
		public static extern int bizswan_binstatesize(IntPtr core);
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_binstatesave(IntPtr core, byte[] data, int length);
		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_binstateload(IntPtr core, byte[] data, int length);

		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_txtstatesave(IntPtr core, [In]ref TextStateFPtrs ff);
		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_txtstateload(IntPtr core, [In]ref TextStateFPtrs ff);

		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_setmemorycallbacks(IntPtr core, MemoryCallback rcb, MemoryCallback ecb, MemoryCallback wcb);

		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_setbuttoncallback(IntPtr core, ButtonCallback bcb);

		[UnmanagedFunctionPointer(cc)]
		public delegate void MemoryCallback(uint addr);

		[UnmanagedFunctionPointer(cc)]
		public delegate void ButtonCallback();

		/// <summary>
		/// return a CPU register
		/// </summary>
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
		}

		[Flags]
		public enum Buttons : uint
		{
			X1 = 0x00000001,
			X2 = 0x00000002,
			X3 = 0x00000004,
			X4 = 0x00000008,
			Y1 = 0x00000010,
			Y2 = 0x00000020,
			Y3 = 0x00000040,
			Y4 = 0x00000080,
			Start = 0x00000100,
			A = 0x00000200,
			B = 0x00000400,

			R_X1 = 0x00010000,
			R_X2 = 0x00020000,
			R_X3 = 0x00040000,
			R_X4 = 0x00080000,
			R_Y1 = 0x00100000,
			R_Y2 = 0x00200000,
			R_Y3 = 0x00400000,
			R_Y4 = 0x00800000,
			R_Start = 0x01000000,
			R_A = 0x02000000,
			R_B = 0x04000000,

			Rotate = 0x80000000,
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
			/// <summary>
			/// map bw shades to output colors, [0] = darkest, [15] = lightest
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
			public uint[] BWPalette;
			/// <summary>
			/// map color shades to output colors, bits 0-3 blue, bits 4-7 green, bits 8-11 red
			/// </summary>
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
			public uint[] ColorPalette;
		}
	}
}
