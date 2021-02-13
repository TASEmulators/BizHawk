using System;

namespace BizHawk.Bizware.BizwareGL
{
	[Flags]
	public enum ClearBufferMask
	{
		None = 0x0000,
		DepthBufferBit = 0x0100,
		AccumBufferBit = 0x0200,
		StencilBufferBit = 0x0400,
		ColorBufferBit = 0x4000,
		CoverageBufferBitNv = 0x8000,
	}
}
