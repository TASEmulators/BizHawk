using System.Text;

namespace BizHawk.Common
{
	public static class Mershul
	{
		/// <remarks>
		/// TODO: Update to a version of .nyet that includes this
		/// </remarks>
		public static unsafe string? PtrToStringUtf8(IntPtr p)
		{
			if (p == IntPtr.Zero)
				return null;
			byte* b = (byte*)p;
			int len = 0;
			while (*b++ != 0)
				len++;
			return Encoding.UTF8.GetString((byte*)p, len);
		}
	}
}
