using System;
using System.Runtime.InteropServices;

namespace BizHawk.BizInvoke
{
	public static unsafe class BizInvokerUtilities
	{
#nullable disable //TODO If U1 and U2 were structs, this wouldn't be a problem. Would changing them to structs break this helper's functionality? --yoshi
		[StructLayout(LayoutKind.Explicit)]
		private class U
		{
			[FieldOffset(0)]
			public U1 First;
			[FieldOffset(0)]
			public U2 Second;
		}
#nullable restore
		[StructLayout(LayoutKind.Explicit)]
		private class U1
		{
			[FieldOffset(0)]
			public UIntPtr P;
		}
		[StructLayout(LayoutKind.Explicit)]
		private class U2
		{
			[FieldOffset(0)]
			public object Target;

			public U2(object target) => Target = target;
		}
		private class CF
		{
			public int FirstField;
		}
		/// <summary>
		/// Didn't I have code somewhere else to do this already?
		/// </summary>
		/// <returns></returns>
		public static unsafe int ComputeClassFieldOffset()
		{
			var c = new CF();
			int ret;
			fixed(int* fx = &c.FirstField)
			{
				var u = new U { Second = new U2(c) };
				ret = (int)((ulong)(UIntPtr)fx - (ulong)u.First.P);
			}
			return ret;
		}
		public static unsafe int ComputeStringOffset()
		{
			var s = new string(new char[0]);
			int ret;
			fixed(char* fx = s)
			{
				var u = new U { Second = new U2(s) };
				ret = (int)((ulong)(UIntPtr)fx - (ulong)u.First.P);
			}
			return ret;
		}
	}
}
