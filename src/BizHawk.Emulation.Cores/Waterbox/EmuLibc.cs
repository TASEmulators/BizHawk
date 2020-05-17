using System;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Waterbox
{
	/// <summary>
	/// implementation for special functions defined in emulibc.h
	/// </summary>
	internal class EmuLibc
	{
		private readonly Waterbox _parent;
		public EmuLibc(Waterbox parent)
		{
			_parent = parent;
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__walloc_sealed")]
		public IntPtr AllocSealed(UIntPtr size)
		{
			return Z.US(_parent._sealedheap.Allocate((ulong)size, 16));
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__walloc_invisible")]
		public IntPtr AllocInvisible(UIntPtr size)
		{
			return Z.US(_parent._invisibleheap.Allocate((ulong)size, 16));
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__walloc_plain")]
		public IntPtr AllocPlain(UIntPtr size)
		{
			return Z.US(_parent._plainheap.Allocate((ulong)size, 16));
		}

		[BizExport(CallingConvention.Cdecl, EntryPoint = "__w_debug_puts")]
		public void DebugPuts(IntPtr s)
		{
			Console.WriteLine("_debug_puts:" + Marshal.PtrToStringAnsi(s));
		}
	}
}
