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
			UP      = 1 <<  0,
			DOWN    = 1 <<  1,
			LEFT    = 1 <<  2,
			RIGHT   = 1 <<  3,
			B       = 1 <<  4,
			A       = 1 <<  5,
			C_UP    = 1 <<  6,
			C_DOWN  = 1 <<  7,
			C_LEFT  = 1 <<  8,
			C_RIGHT = 1 <<  9,
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

		[StructLayout(LayoutKind.Explicit)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			[FieldOffset(44)]
			public Buttons P1Buttons;
			[FieldOffset(48)]
			public short P1XAxis;
			[FieldOffset(50)]
			public short P1YAxis;

			[FieldOffset(52)]
			public Buttons P2Buttons;
			[FieldOffset(56)]
			public short P2XAxis;
			[FieldOffset(58)]
			public short P2YAxis;

			[FieldOffset(60)]
			public Buttons P3Buttons;
			[FieldOffset(64)]
			public short P3XAxis;
			[FieldOffset(68)]
			public short P3YAxis;

			[FieldOffset(70)]
			public Buttons P4Buttons;
			[FieldOffset(74)]
			public short P4XAxis;
			[FieldOffset(76)]
			public short P4YAxis;

			[FieldOffset(78)]
			public bool Reset;
			[FieldOffset(79)]
			public bool Power;
		}

		[BizImport(CC)]
		public abstract bool Init(ControllerType[] controllers, bool pal);
	}
}
