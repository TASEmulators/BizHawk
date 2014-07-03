using System.Linq;

namespace BizHawk.Common
{
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
