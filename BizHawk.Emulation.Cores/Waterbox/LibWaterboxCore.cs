using BizHawk.Common;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MemoryArea
		{
			/// <summary>
			/// pointer to the data in memory
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
		public delegate void EmptyCallback();

		public unsafe class WaterboxMemoryDomain : MemoryDomain
		{
			private readonly IntPtr _data;
			private readonly IMonitor _monitor;
			private readonly long _addressMangler;

			public override byte PeekByte(long addr)
			{
				if ((ulong)addr < (ulong)Size)
				{
					using (_monitor.EnterExit())
					{
						return ((byte*)_data)[addr ^ _addressMangler];
					}
				}

				throw new ArgumentOutOfRangeException(nameof(addr));
			}

			public override void PokeByte(long addr, byte val)
			{
				if (Writable)
				{
					if ((ulong)addr < (ulong)Size)
					{
						using (_monitor.EnterExit())
						{
							((byte*)_data)[addr ^ _addressMangler] = val;
						}
					}
					else
					{
						throw new ArgumentOutOfRangeException(nameof(addr));
					}
				}
			}

			public WaterboxMemoryDomain(MemoryArea m, IMonitor monitor)
			{
				Name = Marshal.PtrToStringAnsi(m.Name);
				EndianType = (m.Flags & MemoryDomainFlags.YugeEndian) != 0 ? Endian.Big : Endian.Little;
				_data = m.Data;
				Size = m.Size;
				Writable = (m.Flags & MemoryDomainFlags.Writable) != 0;
				if ((m.Flags & MemoryDomainFlags.WordSize1) != 0)
					WordSize = 1;
				else if ((m.Flags & MemoryDomainFlags.WordSize2) != 0)
					WordSize = 2;
				else if ((m.Flags & MemoryDomainFlags.WordSize4) != 0)
					WordSize = 4;
				else if ((m.Flags & MemoryDomainFlags.WordSize8) != 0)
					WordSize = 8;
				else
					throw new InvalidOperationException("Unknown word size for memory domain");
				_monitor = monitor;
				if ((m.Flags & MemoryDomainFlags.Swapped) != 0 && EndianType == Endian.Big)
				{
					_addressMangler = WordSize - 1;
				}
				else
				{
					_addressMangler = 0;
				}
			}
		}

		[BizImport(CC)]
		public abstract void FrameAdvance([In, Out] FrameInfo frame);

		[BizImport(CC)]
		public abstract void GetMemoryAreas([In, Out] MemoryArea[] areas);

		[BizImport(CC)]
		public abstract void SetInputCallback(EmptyCallback callback);
	}

	/// <summary>
	/// if a core implements this, it will be used for saveramming instead of memory domains
	/// </summary>
	interface ICustomSaveram
	{
		int GetSaveramSize();
		void PutSaveram(byte[] data, int size);
		void GetSaveram(byte[] data, int size);
	}
}
