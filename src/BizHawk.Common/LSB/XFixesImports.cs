using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class XfixesImports
	{
		private const string XFIXES = "libXfixes.so.3";

		// docs claims this returns Status, actually returns Bool (1 is success, 0 is failure)
		[DllImport(XFIXES)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool XFixesQueryVersion(IntPtr display, out int major_version_return, out int minor_version_return);

		[Flags]
		public enum BarrierDirection : int
		{
			BarrierNone = 0,
			BarrierPositiveX = 1 << 0,
			BarrierPositiveY = 1 << 1,
			BarrierNegativeX = 1 << 2,
			BarrierNegativeY = 1 << 3,
		}

		[DllImport(XFIXES)]
		public static extern IntPtr XFixesCreatePointerBarrier(IntPtr display, IntPtr win, int x1, int y1, int x2, int y2, BarrierDirection directions, int num_deivces, IntPtr devices);

		[DllImport(XFIXES)]
		public static extern void XFixesDestroyPointerBarrier(IntPtr display, IntPtr b);
	}
}
