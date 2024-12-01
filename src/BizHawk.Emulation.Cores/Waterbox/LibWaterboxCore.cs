using BizHawk.BizInvoke;

using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract class LibWaterboxCore
	{
		public const CallingConvention CC = CallingConvention.Cdecl;

		[StructLayout(LayoutKind.Sequential)]
		public class FrameInfo
		{
			/// <summary>
			/// pointer to the video buffer; set by frontend, filled by backend
			/// </summary>
			public IntPtr VideoBuffer;
			/// <summary>
			/// pointer to the sound buffer; set by frontend, filled by backend
			/// </summary>
			public IntPtr SoundBuffer;
			/// <summary>
			/// total number of cycles emulated this frame; set by backend
			/// </summary>
			public long Cycles;
			/// <summary>
			/// width of the output image; set by backend
			/// </summary>
			public int Width;
			/// <summary>
			/// height of the output image; set by backend
			/// </summary>
			public int Height;
			/// <summary>
			/// total number of sample pairs produced; set by backend
			/// </summary>
			public int Samples;
			/// <summary>
			/// true if controllers were not read; set by backend
			/// </summary>
			public int Lagged;
		}

		[Flags]
		public enum MemoryDomainFlags : long
		{
			None = 0,
			/// <summary>
			/// if false, the domain MUST NOT be written to.
			/// in some cases, a segmentation violation might occur
			/// </summary>
			Writable = 1,
			/// <summary>
			/// if true, this memory domain should be used in saveram.
			/// can be ignored if the core provides its own saveram implementation
			/// </summary>
			Saverammable = 2,
			/// <summary>
			/// if true, domain is filled with ones (FF) by default, instead of zeros.
			/// used in calculating SaveRamModified
			/// </summary>
			OneFilled = 4,
			/// <summary>
			/// desginates the default memory domain
			/// </summary>
			Primary = 8,
			/// <summary>
			/// if true, the most significant bytes are first in multibyte words
			/// </summary>
			YugeEndian = 16,
			/// <summary>
			/// native wordsize.  only a hint
			/// </summary>
			WordSize1 = 32,
			/// <summary>
			/// native wordsize.  only a hint
			/// </summary>
			WordSize2 = 64,
			/// <summary>
			/// native wordsize.  only a hint
			/// </summary>
			WordSize4 = 128,
			/// <summary>
			/// native wordsize.  only a hint
			/// </summary>
			WordSize8 = 256,
			/// <summary>
			/// for a yuge endian domain, if true, bytes are stored word-swapped from their native ordering
			/// </summary>
			Swapped = 512,
			/// <summary>
			/// If true, Data is a function to call and not a pointer
			/// </summary>
			FunctionHook = 1024,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MemoryArea
		{
			/// <summary>
			/// pointer to the data in memory, or a function hook to call
			/// </summary>
			public IntPtr Data;
			/// <summary>
			/// null terminated strnig naming the memory domain
			/// </summary>
			public IntPtr Name;
			/// <summary>
			/// size of the domain
			/// </summary>
			public long Size;
			public MemoryDomainFlags Flags;
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void MemoryFunctionHook(IntPtr buffer, long address, long count, bool write);

		[UnmanagedFunctionPointer(CC)]
		public delegate void EmptyCallback();

		[BizImport(CC)]
		public abstract void FrameAdvance([In, Out] FrameInfo frame);

		[BizImport(CC)]
		public abstract void GetMemoryAreas([In, Out] MemoryArea[] areas);

		[BizImport(CC)]
		public abstract void SetInputCallback(EmptyCallback callback);
	}
}
