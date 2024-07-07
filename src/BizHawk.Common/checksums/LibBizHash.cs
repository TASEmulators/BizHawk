using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	public static class LibBizHash
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[UnmanagedFunctionPointer(cc)]
		public delegate uint CalcCRC(uint current, IntPtr buffer, int len);

		[DllImport("libbizhash", CallingConvention = cc)]
		public static extern IntPtr BizCalcCrcFunc();

		[DllImport("libbizhash", CallingConvention = cc)]
		public static extern bool BizSupportsShaInstructions();

		[DllImport("libbizhash", CallingConvention = cc)]
		public static extern void BizCalcSha1(IntPtr state, byte[] data, int len);
	}
}
