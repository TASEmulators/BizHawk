namespace BizHawk.Common
{
	public static class BitReverse
	{
		static BitReverse()
		{
			MakeByte8();
		}

		public static byte[] Byte8;

		public static uint Reverse32(uint v)
		{
			return (uint)((Byte8[v & 0xff] << 24) |
					(Byte8[(v >> 8) & 0xff] << 16) |
					(Byte8[(v >> 16) & 0xff] << 8) |
					(Byte8[(v >> 24) & 0xff]));
		}

		private static void MakeByte8()
		{
			int bits = 8;
			const int n = 1 << 8;
			Byte8 = new byte[n];

			int m = 1;
			int a = n >> 1;
			int j = 2;

			Byte8[0] = 0;
			Byte8[1] = (byte)a;

			while ((--bits) != 0)
			{
				m <<= 1;
				a >>= 1;
				for (int i = 0; i < m; i++)
				{
					Byte8[j++] = (byte)(Byte8[i] + a);
				}
			}
		}
	}
}
