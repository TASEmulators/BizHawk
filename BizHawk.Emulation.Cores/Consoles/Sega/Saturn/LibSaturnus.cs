using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	public abstract class LibSaturnus : LibWaterboxCore
	{
		// some of the internal code uses wizardry by which certain pointers in ss.wbx[.text]
		// must be greater than or equal to this address, but less than 4GB bigger than it
		public const ulong StartAddress = 0x36d00000000;

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

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public int ResetPushed;
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate int FirmwareSizeCallback(string filename);
		[UnmanagedFunctionPointer(CC)]
		public delegate void FirmwareDataCallback(string filename, IntPtr dest);
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDTOCCallback(int disk, [In, Out]TOC toc);
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDSectorCallback(int disk, int lba, IntPtr dest);

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
		public abstract void SetControllerData(byte[] controllerData);
		[BizImport(CC)]
		public abstract void SetupInput(int[] portdevices, int[] multitaps);
		[BizImport(CC)]
		public abstract void SetRtc(long ticks, Saturnus.SyncSettings.LanguageType language);
		[BizImport(CC)]
		public abstract void SetVideoParameters(bool correctAspect, bool hBlend, bool hOverscan, int sls, int sle);
	}
}
