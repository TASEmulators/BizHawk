using BizHawk.Common.BizInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.VB
{
	public abstract class LibVirtualBoyee
	{
		private const CallingConvention CC = CallingConvention.Cdecl;

		[StructLayout(LayoutKind.Sequential)]
		public struct Rect
		{
			public int X;
			public int Y;
			public int W;
			public int H;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class EmulateSpec
		{
			// Pitch(32-bit) must be equal to width and >= the "fb_width" specified in the MDFNGI struct for the emulated system.
			// Height must be >= to the "fb_height" specified in the MDFNGI struct for the emulated system.
			// The framebuffer pointed to by surface->pixels is written to by the system emulation code.
			public IntPtr Pixels;

			// Pointer to sound buffer, set by the driver code, that the emulation code should render sound to.
			// Guaranteed to be at least 500ms in length, but emulation code really shouldn't exceed 40ms or so.  Additionally, if emulation code
			// generates >= 100ms, 
			// DEPRECATED: Emulation code may set this pointer to a sound buffer internal to the emulation module.
			public IntPtr SoundBuf;

			// Number of cycles that this frame consumed, using MDFNGI::MasterClock as a time base.
			// Set by emulation code.
			public long MasterCycles;

			// Set by the system emulation code every frame, to denote the horizontal and vertical offsets of the image, and the size
			// of the image.  If the emulated system sets the elements of LineWidths, then the width(w) of this structure
			// is ignored while drawing the image.
			public Rect DisplayRect;

			// Maximum size of the sound buffer, in frames.  Set by the driver code.
			public int SoundBufMaxSize;

			// Number of frames currently in internal sound buffer.  Set by the system emulation code, to be read by the driver code.
			public int SoundBufSize;

			// 0 UDLR SelectStartBA UDLR(right dpad) LtrigRtrig 13
			public Buttons Buttons;
		}

		public enum MemoryArea : int
		{
			Wram, Sram, Rom
		}

		public enum Buttons : int
		{
			Up = 0x1,
			Down = 0x2,
			Left = 0x4,
			Right = 0x8,
			Select = 0x10,
			Start = 0x20,
			B = 0x40,
			A = 0x80,
			Up_R = 0x100,
			Down_R = 0x200,
			Left_R = 0x400,
			Right_R = 0x800,
			L = 0x1000,
			R = 0x2000
		}

		[BizImport(CC)]
		public abstract bool Load(byte[] rom, int length);

		[BizImport(CC)]
		public abstract void GetMemoryArea(MemoryArea which, ref IntPtr ptr, ref int size);

		[BizImport(CC)]
		public abstract void Emulate(EmulateSpec espec);

		[BizImport(CC)]
		public abstract void HardReset();
	}
}
