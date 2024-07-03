using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive
{
	public abstract class LibPicoDrive : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public int Buttons;
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void CDReadCallback(int lba, IntPtr dest, bool audio);

		public enum Region : int
		{
			Auto = 0,
			JapanNTSC = 1,
			JapanPAL = 2,
			US = 4,
			Europe = 8
		}

		/// <param name="cd">If TRUE, load a CD and not a cart.</param>
		/// <param name="_32xPreinit">If TRUE, preallocate 32X data structures.  When set to false,
		///		32X games will still run, but will not have memory domains</param>
		[BizImport(CC)]
		public abstract bool Init(bool cd, bool _32xPreinit, Region regionAutoOrder, Region regionOverride);

		public const int CD_MAX_TRACKS = 100;

		[StructLayout(LayoutKind.Sequential)]
		public struct Track
		{
			public int start;
			public int end;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class TOC
		{
			public int end;
			public int last;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = CD_MAX_TRACKS)]
			public readonly Track[] tracks = new Track[CD_MAX_TRACKS];
		}

		[BizImport(CC)]
		public abstract void SetCDReadCallback(CDReadCallback callback);

		[BizImport(CC)]
		public abstract bool IsPal();

		[BizImport(CC)]
		public abstract bool Is32xActive();
	}
}
