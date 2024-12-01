using BizHawk.Common.NumberExtensions;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// mostly jacked from nestopia's NstBoardBandaiDatach.cpp
	// very dirty, needs cleanup and such

	public class DatachBarcode : IEmulatorService
	{
		private static readonly byte[,] prefixParityType = new byte[10, 6]
		{
			{8,8,8,8,8,8}, {8,8,0,8,0,0},
			{8,8,0,0,8,0}, {8,8,0,0,0,8},
			{8,0,8,8,0,0}, {8,0,0,8,8,0},
			{8,0,0,0,8,8}, {8,0,8,0,8,0},
			{8,0,8,0,0,8}, {8,0,0,8,0,8}
		};

		private static readonly byte[,] dataLeftOdd = new byte[10, 7]
		{
			{8,8,8,0,0,8,0}, {8,8,0,0,8,8,0},
			{8,8,0,8,8,0,0}, {8,0,0,0,0,8,0},
			{8,0,8,8,8,0,0}, {8,0,0,8,8,8,0},
			{8,0,8,0,0,0,0}, {8,0,0,0,8,0,0},
			{8,0,0,8,0,0,0}, {8,8,8,0,8,0,0}
		};

		private static readonly byte[,] dataLeftEven = new byte[10, 7]
		{
			{8,0,8,8,0,0,0}, {8,0,0,8,8,0,0},
			{8,8,0,0,8,0,0}, {8,0,8,8,8,8,0},
			{8,8,0,0,0,8,0}, {8,0,0,0,8,8,0},
			{8,8,8,8,0,8,0}, {8,8,0,8,8,8,0},
			{8,8,8,0,8,8,0}, {8,8,0,8,0,0,0}
		};

		private static readonly byte[,] dataRight = new byte[10, 7]
		{
			{0,0,0,8,8,0,8}, {0,0,8,8,0,0,8},
			{0,0,8,0,0,8,8}, {0,8,8,8,8,0,8},
			{0,8,0,0,0,8,8}, {0,8,8,0,0,0,8},
			{0,8,0,8,8,8,8}, {0,8,8,8,0,8,8},
			{0,8,8,0,8,8,8}, {0,0,0,8,0,8,8}
		};

		private const int MIN_DIGITS = 8;
		private const int MAX_DIGITS = 13;
		private const int CC_INTERVAL = 1000;

		private int cycles;
		private byte output;
		private int stream_idx;
		private byte[] data = new byte[0];

		private byte streamoutput => data[stream_idx];

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(DatachBarcode));
			ser.Sync(nameof(cycles), ref cycles);
			ser.Sync(nameof(output), ref output);
			ser.Sync(nameof(stream_idx), ref stream_idx);
			ser.Sync(nameof(data), ref data, false);
			ser.EndSection();
		}


		public void Reset()
		{
			cycles = 0;
			stream_idx = 0;
			data = new byte[0];
		}

		public bool IsTransferring()
		{
			return stream_idx < data.Length;
		}
		private static bool IsDigtsSupported(int count)
		{
			return count.In(MIN_DIGITS, MAX_DIGITS);
		}

		public static bool ValidString(string s, out string why)
		{
			if (s == null)
				throw new ArgumentNullException(nameof(s));
			if (!s.Length.In(MIN_DIGITS, MAX_DIGITS))
			{
				why = $"String must be {MIN_DIGITS} or {MAX_DIGITS} digits long!";
				return false;
			}
			foreach (char c in s)
			{
				if (c < '0' || c > '9')
				{
					why = "String must be numeric only!";
					return false;
				}
			}
			why = "String is OK.";
			return true;
		}

		public void Transfer(string s)
		{
			if (!ValidString(s, out var why))
				throw new InvalidOperationException(why);

			Reset();

			byte[] code = new byte[16];

			for (int i = 0; i < s.Length; i++)
			{
				if (s[i] >= '0' && s[i] <= '9')
					code[i] = (byte)(s[i] - '0');
				else
					throw new InvalidOperationException("s must be numeric only");
			}

			var result = new System.IO.MemoryStream();

			for (int i = 0; i < 33; i++)
				result.WriteByte(8);

			result.WriteByte(0);
			result.WriteByte(8);
			result.WriteByte(0);

			int sum = 0;

			if (s.Length == MAX_DIGITS)
			{
				for (int i = 0; i < 6; i++)
				{
					if (prefixParityType[code[0], i] != 0)
					{
						for (int j = 0; j < 7; j++)
							result.WriteByte(dataLeftOdd[code[i + 1], j]);
					}
					else
					{
						for (int j = 0; j < 7; j++)
							result.WriteByte(dataLeftEven[code[i + 1], j]);
					}
				}

				result.WriteByte(8);
				result.WriteByte(0);
				result.WriteByte(8);
				result.WriteByte(0);
				result.WriteByte(8);

				for (int i = 7; i < 12; i++)
					for (int j = 0; j < 7; j++)
						result.WriteByte(dataRight[code[i], j]);

				for (int i = 0; i < 12; i++)
					sum += code[i] * ((i & 1) != 0 ? 3 : 1);
			}
			else // s.Length == MIN_DIGITS
			{
				for (int i = 0; i < 4; i++)
					for (int j = 0; j < 7; j++)
						result.WriteByte(dataLeftOdd[code[i], j]);

				result.WriteByte(8);
				result.WriteByte(0);
				result.WriteByte(8);
				result.WriteByte(0);
				result.WriteByte(8);

				for (int i = 4; i < 7; i++)
					for (int j = 0; j < 7; j++)
						result.WriteByte(dataRight[code[i], j]);


				for (int i = 0; i < 7; i++)
					sum += code[i] * ((i & 1) != 0 ? 3 : 1);
			}
			sum = (10 - (sum % 10)) % 10;

			for (int j = 0; j < 7; j++)
				result.WriteByte(dataRight[sum, j]);

			result.WriteByte(0);
			result.WriteByte(8);
			result.WriteByte(0);

			for (int i = 0; i < 32; i++)
				result.WriteByte(8);

			data = result.ToArray();

			cycles = CC_INTERVAL;
			output = streamoutput;
		}

		public void Clock()
		{
			if (cycles <= 0 || !IsTransferring())
				return;
			cycles--;
			if (cycles <= 0)
			{
				stream_idx++;
				if (IsTransferring())
				{
					output = streamoutput;
					cycles = CC_INTERVAL;
				}
				else
				{
					output = 0;
				}
			}
		}

		/// <summary>
		/// d3
		/// </summary>
		public bool GetOutput()
		{
			return output == 8;
		}
	}
}
