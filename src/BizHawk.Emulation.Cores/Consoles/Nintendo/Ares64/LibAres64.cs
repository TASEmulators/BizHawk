using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	public abstract class LibAres64 : LibWaterboxCore
	{
		[Flags]
		public enum Buttons : uint
		{
			UP      = 1 << 00,
			DOWN    = 1 << 01,
			LEFT    = 1 << 02,
			RIGHT   = 1 << 03,
			B       = 1 << 04,
			A       = 1 << 05,
			C_UP    = 1 << 06,
			C_DOWN  = 1 << 07,
			C_LEFT  = 1 << 08,
			C_RIGHT = 1 << 09,
			L       = 1 << 10,
			R       = 1 << 11,
			Z       = 1 << 12,
			START   = 1 << 13,
		}

		public enum ControllerType : uint
		{
			Unplugged,
			Standard,
			Mempak,
			Rumblepak,
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public Buttons P1Buttons;
			public short P1XAxis;
			public short P1YAxis;

			public Buttons P2Buttons;
			public short P2XAxis;
			public short P2YAxis;

			public Buttons P3Buttons;
			public short P3XAxis;
			public short P3YAxis;

			public Buttons P4Buttons;
			public short P4XAxis;
			public short P4YAxis;
		}

		[BizImport(CC)]
		public abstract bool Init(ControllerType[] controllers, bool pal);
	}
}
