using System;
using System.Runtime.InteropServices;

namespace BizHawk.BizInvoke
{
	public static unsafe class BizInvokerUtilities
	{
		[StructLayout(LayoutKind.Explicit)]
		private class U
		{
			[FieldOffset(0)]
			public readonly U1? First;

			[FieldOffset(0)]
			public readonly U2 Second;

			public U(U2 second)
			{
				// being a union type, these assignments affect the value of both fields so they need to be in this order
				First = null;
				Second = second;
			}
		}

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
		public static int ComputeClassFieldOffset()
		{
			var c = new CF();
			int ret;
			fixed(int* fx = &c.FirstField)
			{
				U u = new(new U2(c));
				ret = (int) ((ulong) (UIntPtr) fx - (ulong) u.First!.P);
			}
			return ret;
		}

		public static int ComputeStringOffset()
		{
			var s = new string(new char[0]);
			int ret;
			fixed(char* fx = s)
			{
				U u = new(new U2(s));
				ret = (int) ((ulong) (UIntPtr) fx - (ulong) u.First!.P);
			}
			return ret;
		}
	}
}
