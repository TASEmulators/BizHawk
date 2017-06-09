using BizHawk.Common.BizInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	public abstract class LibSaturnus
	{
		// some of the internal code uses wizardry by which certain pointers in ss.wbx[.text]
		// must be greater than or equal to this address, but less than 4GB bigger than it
		public const ulong StartAddress = 0x36d00000000;

		const CallingConvention CC = CallingConvention.Cdecl;

		[StructLayout(LayoutKind.Sequential)]
		public class TOC
		{
			public int FirstTrack;
			public int LastTrack;
			public int DiskType;

			[StructLayout(LayoutKind.Sequential)]
			public struct Track
			{
				public int Adr;
				public int Control;
				public int Lba;
				public int Valid;
			}

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 101)]
			public Track[] Tracks;
		}

		[StructLayout(LayoutKind.Explicit)]
		public class FrameAdvanceInfo
		{
			[FieldOffset(0)]
			public IntPtr SoundBuf;
			[FieldOffset(8)]
			public IntPtr Pixels;
			[FieldOffset(16)]
			public IntPtr Controllers;
			[FieldOffset(24)]
			public long MasterCycles;
			[FieldOffset(32)]
			public int SoundBufMaxSize;
			[FieldOffset(36)]
			public int SoundBufSize;
			[FieldOffset(40)]
			public int Width;
			[FieldOffset(44)]
			public int Height;
			[FieldOffset(48)]
			public short ResetPushed;
			[FieldOffset(50)]
			public short InputLagged;
		};

		[UnmanagedFunctionPointer(CC)]
		public delegate int FirmwareSizeCallback(string filename);
		[UnmanagedFunctionPointer(CC)]
		public delegate void FirmwareDataCallback(string filename, IntPtr dest);
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDTOCCallback(int disk, [In, Out]TOC toc);
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDSectorCallback(int disk, int lba, IntPtr dest);
		[UnmanagedFunctionPointer(CC)]
		public delegate void InputCallback();
		[UnmanagedFunctionPointer(CC)]
		public delegate void AddMemoryDomainCallback(string name, IntPtr ptr, int size, bool writable);

		[BizImport(CC)]
		public abstract void SetFirmwareCallbacks(FirmwareSizeCallback sizecallback, FirmwareDataCallback datacallback);
		[BizImport(CC)]
		public abstract void SetCDCallbacks(CDTOCCallback toccallback, CDSectorCallback sectorcallback);
		[BizImport(CC)]
		public abstract bool Init(
			int numDisks,
			Saturnus.SyncSettings.CartType cartType,
			Saturnus.SyncSettings.RegionType regionDefault,
			bool regionAutodetect);
		[BizImport(CC)]
		public abstract void HardReset();
		[BizImport(CC)]
		public abstract void SetDisk(int disk, bool open);
		[BizImport(CC)]
		public abstract void FrameAdvance([In, Out]FrameAdvanceInfo f);
		[BizImport(CC)]
		public abstract void SetupInput(int[] portdevices, int[] multitaps);
		[BizImport(CC)]
		public abstract void SetInputCallback(InputCallback callback);
		[BizImport(CC)]
		public abstract void SetAddMemoryDomainCallback(AddMemoryDomainCallback callback);
		[BizImport(CC)]
		public abstract void SetRtc(long ticks, Saturnus.SyncSettings.LanguageType language);
		[BizImport(CC)]
		public abstract void SetVideoParameters(bool correctAspect, bool hBlend, bool hOverscan, int sls, int sle);
	}
}
