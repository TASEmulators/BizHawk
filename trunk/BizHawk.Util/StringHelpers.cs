using System;
using System.Linq;

namespace BizHawk
{
	public static class StringHelpers
	{
		public static int HowMany(string str, char c)
		{
			if (!String.IsNullOrEmpty(str))
			{
				return str.Count(t => t == c);
			}
			else
			{
				return 0;
			}
		}

		public static int HowMany(string str, string s)
		{
			int count = 0;
			for (int x = 0; x < (str.Length - s.Length); x++)
			{
				if (str.Substring(x, s.Length) == s)
					count++;
			}
			return count;
		}
	}
}
