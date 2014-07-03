using System.Linq;

namespace BizHawk.Common
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
	}
}
