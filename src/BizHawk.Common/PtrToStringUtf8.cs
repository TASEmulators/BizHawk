using System;
using System.Text;

namespace BizHawk.Common
{
	public static class Mershul
	{
		/// <summary>
		/// TODO: Update to a version of .nyet that includes this
		/// </summary>
		public static unsafe string PtrToStringUtf8(IntPtr p)
		{
			byte* b = (byte*)p;
			int len = 0;
			while (*b++ != 0)
				len++;
			return Encoding.UTF8.GetString((byte*)p, len);
		}
	}
}
