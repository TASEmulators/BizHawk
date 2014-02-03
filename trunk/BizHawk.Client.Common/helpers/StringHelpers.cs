using System.Linq;

namespace BizHawk.Client.Common
{
	// TODO: these classes are worthless or need to be extensions, decide which
	public static class StringHelpers
	{
		public static int HowMany(string str, char c)
		{
			return !string.IsNullOrEmpty(str) ? str.Count(t => t == c) : 0;
		}

		public static int HowMany(string str, string s)
		{
			var count = 0;
			for (int i = 0; i < (str.Length - s.Length); i++)
			{
				if (str.Substring(i, s.Length) == s)
				{
					count++;
				}
			}

			return count;
		}
	}

	// TODO: put it in its own file
	public static class IntHelpers // TODO: a less lame name
	{
		public static int GetNumDigits(int i)
		{
			if (i < 0x100)
			{
				return 2;
			}
			
			if (i < 0x10000)
			{
				return 4;
			}
			
			if (i < 0x1000000)
			{
				return 6;
			}
			
			return 8;
		}

		public static uint MaxHexValueFromMaxDigits(int i)
		{
			switch (i)
			{
				case 0:
					return 0;
				case 1:
					return 0xF;
				case 2:
					return 0xFF;
				case 3:
					return 0xFFF;
				case 4:
					return 0xFFFF;
				case 5:
					return 0xFFFFF;
				case 6:
					return 0xFFFFFF;
				case 7:
					return 0xFFFFFFF;
				case 8:
					return 0xFFFFFFFF;
			}

			return int.MaxValue;
		}
	}
}
