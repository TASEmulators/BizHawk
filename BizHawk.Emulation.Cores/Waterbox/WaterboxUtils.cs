using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public static class WaterboxUtils
	{
		/// <summary>
		/// copy `len` bytes from `src` to `dest`
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dst"></param>
		/// <param name="len"></param>
		public static void CopySome(Stream src, Stream dst, long len)
		{
			var buff = new byte[4096];
			while (len > 0)
			{
				int r = src.Read(buff, 0, (int)Math.Min(len, 4096));
				dst.Write(buff, 0, r);
				len -= r;
			}
		}

		public static byte[] Hash(byte[] data)
		{
			using (var h = SHA1.Create())
			{
				return h.ComputeHash(data);
			}
		}

		public static byte[] Hash(Stream s)
		{
			using (var h = SHA1.Create())
			{
				return h.ComputeHash(s);
			}
		}

	}
}
