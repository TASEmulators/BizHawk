using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public abstract class LibVirtualJaguar : LibWaterboxCore
	{
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

		[StructLayout(LayoutKind.Sequential)]
		public struct TOC
		{
			public byte Padding0;
			public byte Padding1;
			public byte MinTrack;
			public byte MaxTrack;
			public byte NumSessions;
			public byte LastLeadOutMins;
			public byte LastLeadOutSecs;
			public byte LastLeadOutFrames;

			[StructLayout(LayoutKind.Sequential)]
			public struct Track
			{
				public byte TrackNum;
				public byte StartMins;
				public byte StartSecs;
				public byte StartFrames;
				public byte SessionNum;
				public byte DurMins;
				public byte DurSecs;
				public byte DurFrames;
			}

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 127)]
			public Track[] Tracks;
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void CDTOCCallback(IntPtr dst);

		[UnmanagedFunctionPointer(CC)]
		public delegate void CDReadCallback(int lba, IntPtr dst);

		[BizImport(CC)]
		public abstract void SetCdCallbacks(CDTOCCallback cdtc, CDReadCallback cdrc);

		[BizImport(CC)]
		public abstract void InitWithCd(ref Settings s, IntPtr bios, IntPtr memtrack);

		[BizImport(CC)]
		public abstract bool SaveRamIsDirty();

		[BizImport(CC)]
		public abstract void GetSaveRam(byte[] dst);

		[BizImport(CC)]
		public abstract void PutSaveRam(byte[] src);

		[UnmanagedFunctionPointer(CC)]
		public delegate void MemoryCallback(uint addr);

		[BizImport(CC)]
		public abstract void SetMemoryCallbacks(MemoryCallback rcb, MemoryCallback wcb, MemoryCallback ecb);

		[UnmanagedFunctionPointer(CC)]
		public delegate void M68KTraceCallback(IntPtr regs);

		[UnmanagedFunctionPointer(CC)]
		public delegate void RISCTraceCallback(uint pc, IntPtr regs);

		[BizImport(CC)]
		public abstract void SetTraceCallbacks(M68KTraceCallback ctcb, RISCTraceCallback gtcb, RISCTraceCallback dtcb);

		[BizImport(CC)]
		public abstract void GetRegisters(IntPtr regs);

		public enum M68KRegisters : int
		{
			D0, D1, D2, D3, D4, D5, D6, D7,
			A0, A1, A2, A3, A4, A5, A6, A7,
			PC, SR,
		}

		[BizImport(CC)]
		public abstract void SetRegister(int which, int val);
	}
}
