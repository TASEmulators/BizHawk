using BizHawk.Common.BizInvoke;
using System;
using System.Collections.Generic;
using System.Drawing;
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

		[StructLayout(LayoutKind.Explicit)] // TODO: find out why Sequential is sometimes ignored on the native layout
		public class EmulateSpec
		{
			// Pitch(32-bit) must be equal to width and >= the "fb_width" specified in the MDFNGI struct for the emulated system.
			// Height must be >= to the "fb_height" specified in the MDFNGI struct for the emulated system.
			// The framebuffer pointed to by surface->pixels is written to by the system emulation code.
			[FieldOffset(0)]
			public IntPtr Pixels;

			// Pointer to sound buffer, set by the driver code, that the emulation code should render sound to.
			// Guaranteed to be at least 500ms in length, but emulation code really shouldn't exceed 40ms or so.  Additionally, if emulation code
			// generates >= 100ms, 
			// DEPRECATED: Emulation code may set this pointer to a sound buffer internal to the emulation module.
			[FieldOffset(8)]
			public IntPtr SoundBuf;

			// Number of cycles that this frame consumed, using MDFNGI::MasterClock as a time base.
			// Set by emulation code.
			[FieldOffset(16)]
			public long MasterCycles;

			// Set by the system emulation code every frame, to denote the horizontal and vertical offsets of the image, and the size
			// of the image.  If the emulated system sets the elements of LineWidths, then the width(w) of this structure
			// is ignored while drawing the image.
			[FieldOffset(24)]
			public Rect DisplayRect;

			// Maximum size of the sound buffer, in frames.  Set by the driver code.
			[FieldOffset(40)]
			public int SoundBufMaxSize;

			// Number of frames currently in internal sound buffer.  Set by the system emulation code, to be read by the driver code.
			[FieldOffset(44)]
			public int SoundBufSize;

			// 0 UDLR SelectStartBA UDLR(right dpad) LtrigRtrig 13
			[FieldOffset(48)]
			public Buttons Buttons;

			[FieldOffset(52)]
			public bool Lagged;
		}

		public enum MemoryArea : int
		{
			Wram, Sram, Rom
		}

		public enum Buttons : int
		{
			Up = 0x200,
			Down = 0x100,
			Left = 0x80,
			Right = 0x40,
			Select = 0x800,
			Start = 0x400,
			B = 0x2,
			A = 0x1,
			Up_R = 0x10,
			Down_R = 0x200,
			Left_R = 0x1000,
			Right_R = 0x2000,
			L = 0x8,
			R = 0x4
		}

		[StructLayout(LayoutKind.Sequential)]
		public class NativeSettings
		{
			public int InstantReadHack;
			public int DisableParallax;
			public int ThreeDeeMode;
			public int SwapViews;
			public int AnaglyphPreset;
			public int AnaglyphCustomLeftColor;
			public int AnaglyphCustomRightColor;
			public int NonAnaglyphColor;
			public int LedOnScale;
			public int InterlacePrescale;
			public int SideBySideSeparation;

			private static int ConvertColor(Color c)
			{
				return c.ToArgb();
			}

			public static NativeSettings FromFrontendSettings(VirtualBoyee.Settings s, VirtualBoyee.SyncSettings ss)
			{
				return new NativeSettings
				{
					InstantReadHack = ss.InstantReadHack ? 1 : 0,
					DisableParallax = ss.DisableParallax ? 1 : 0,
					ThreeDeeMode = (int)s.ThreeDeeMode,
					SwapViews = s.SwapViews ? 1 : 0,
					AnaglyphPreset = (int)s.AnaglyphPreset,
					AnaglyphCustomLeftColor = ConvertColor(s.AnaglyphCustomLeftColor),
					AnaglyphCustomRightColor = ConvertColor(s.AnaglyphCustomRightColor),
					NonAnaglyphColor = ConvertColor(s.NonAnaglyphColor),
					LedOnScale = s.LedOnScale,
					InterlacePrescale = s.InterlacePrescale,
					SideBySideSeparation = s.SideBySideSeparation
				};
			}
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void InputCallback();

		[BizImport(CC)]
		public abstract bool Load(byte[] rom, int length, [In]NativeSettings settings);

		[BizImport(CC)]
		public abstract void GetMemoryArea(MemoryArea which, ref IntPtr ptr, ref int size);

		[BizImport(CC)]
		public abstract void Emulate(EmulateSpec espec);

		[BizImport(CC)]
		public abstract void HardReset();

		[BizImport(CC)]
		public abstract void SetInputCallback(InputCallback callback);
	}
}
