using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public static class C64Util
	{
		public static string ToBinary(int n, int charsmin)
		{
			var result = new StringBuilder(string.Empty);

			while (n > 0 || charsmin > 0)
			{
				result.Insert(0, (n & 0x1) != 0 ? "1" : "0");
				n >>= 1;
				if (charsmin > 0)
					charsmin--;
			}

			return result.ToString();
		}

		public static string ToHex(int n, int charsmin)
		{
            var result = new StringBuilder(string.Empty);

            while (n > 0 || charsmin > 0)
			{
                result.Insert(0, "0123456789ABCDEF".Substring(n & 0xF, 1));
				n >>= 4;
				if (charsmin > 0)
					charsmin--;
			}

			return result.ToString();
		}
	}
}
