using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk
{
	public static class StringHelpers
	{
		public static int HowMany(string str, char c)
		{
			if (!String.IsNullOrEmpty(str))
			{
				int count = 0;
				for (int x = 0; x < str.Length; x++)
				{
					if (str[x] == c)
						count++;
				}
				return count;
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
