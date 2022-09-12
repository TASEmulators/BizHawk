using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public abstract class LibVirtualJaguar : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Settings
		{
			public bool NTSC;
			public bool UseBIOS;
			public bool UseFastBlitter;
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public Buttons Player1;
			public Buttons Player2;
			public bool Reset;
		}

		[BizImport(CC)]
		public abstract bool Init(ref Settings s, IntPtr bios, IntPtr rom, int romsize);

		[BizImport(CC)]
		public abstract bool SaveRamIsDirty();

		[BizImport(CC)]
		public abstract void GetSaveRam(byte[] dst);

		[BizImport(CC)]
		public abstract void PutSaveRam(byte[] src);

		[Flags]
		public enum Buttons : uint
		{
			Up = 1 << 0,
			Down = 1 << 1,
			Left = 1 << 2,
			Right = 1 << 3,
			Asterisk = 1 << 4,
			_7 = 1 << 5,
			_4 = 1 << 6,
			_1 = 1 << 7,
			_0 = 1 << 8,
			_8 = 1 << 9,
			_5 = 1 << 10,
			_2 = 1 << 11,
			Pound = 1 << 12,
			_9 = 1 << 13,
			_6 = 1 << 14,
			_3 = 1 << 15,
			A = 1 << 16,
			B = 1 << 17,
			C = 1 << 18,
			Option = 1 << 19,
			Pause = 1 << 20,
		}
	}
}
