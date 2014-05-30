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

		[DllImport(dd, CallingConvention = cc)]
		public static extern IntPtr bizswan_new();

		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_delete(IntPtr core);

		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_reset(IntPtr core);

		[DllImport(dd, CallingConvention = cc)]
		public static extern void bizswan_advance(IntPtr core, Buttons buttons, bool novideo, int[] surface, short[] soundbuff, ref int soundbuffsize);

		[DllImport(dd, CallingConvention = cc)]
		public static extern bool bizswan_load(IntPtr core, byte[] data, int length, [In] ref Settings settings);

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
		public struct Settings
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

			public void SetName(string newname)
			{
				byte[] data = Encoding.ASCII.GetBytes(newname);
				name = new byte[17];
				Buffer.BlockCopy(data, 0, name, 0, Math.Min(data.Length, name.Length));
			}
		}
	}
}
