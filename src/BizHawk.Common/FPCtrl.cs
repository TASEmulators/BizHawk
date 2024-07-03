using System.IO;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	// .NET is weird and sets the x87 control register to double precision
	// It does this even on Linux which normally expects it set to extended x87 precision
	// Even stranger is that this appears to be completely unneeded on x86-64
	// On Windows, x87 registers are prohibited to be used in kernel code, and
	// MSVC will not use the x87 registers (and presumably this extends to userland OS code)
	// .NET presumbly follows MSVC and would not use the x87 registers (why would it? SSE is available! long double doesn't exist!)
	// This thus only screws over code compiled with MinGW (which use extended x87 precision for long double)
	// Or screws over any unmanaged code on Linux, which doubly includes all waterbox cores (!!!)
	// Of course in practice, this only applies to any code using long double, which is rarely used typically
	// But musl (used for waterbox cores) does end up using it for float formating in the printf family
	// This can extend to issues in games: https://github.com/TASEmulators/BizHawk/issues/3726
	public static class FPCtrl
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void FixFPCtrlDelegate();

		private static readonly MemoryBlock? _memory;
		private static readonly FixFPCtrlDelegate? _fixFpCtrl;

		static FPCtrl()
		{
			// not usable outside of x64
			if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
			{
				return;
			}

			// generate assembly for fixing the x87 control register
			_memory = new(4096);
			_memory.Protect(_memory.Start, _memory.Size, MemoryBlock.Protection.RW);
			var ss = _memory.GetStream(_memory.Start, 64, true);
			var bw = new BinaryWriter(ss);

			// FYI: The push/pop is only needed on Windows, but doesn't do any harm on Linux

			bw.Write((byte)0x50); // push rax
			bw.Write(0x06247CD9); // fnstcw word[rsp + 6]
			bw.Write(0x07244C80); // or byte[rsp + 7], 3
			bw.Write((byte)0x03);
			bw.Write(0x06246CD9); // fldcw word[rsp + 6]
			bw.Write((byte)0x58); // pop rax
			bw.Write((byte)0xC3); // ret

			_memory.Protect(_memory.Start, _memory.Size, MemoryBlock.Protection.RX);
			_fixFpCtrl = Marshal.GetDelegateForFunctionPointer<FixFPCtrlDelegate>((IntPtr)_memory.Start);
		}

		public static void FixFPCtrl()
		{
			// not usable outside of x64
			if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
			{
				return;
			}

			_fixFpCtrl!();
		}
	}
}
