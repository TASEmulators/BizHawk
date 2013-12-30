using System;
using System.Linq;

namespace BizHawk.Client.Common
{
	public static class StringHelpers
	{
		public static int HowMany(string str, char c)
		{
			return !String.IsNullOrEmpty(str) ? str.Count(t => t == c) : 0;
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
		public static int GetNumDigits(Int32 i)
		{
			if (i < 0x100)
			{
				return 2;
			}
			else if (i < 0x10000)
			{
				return 4;
			}
			else if (i < 0x1000000)
			{
				return 6;
			}
			else
			{
				return 8;
			}
		}

		public static uint MaxHexValueFromMaxDigits(Int32 i)
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
