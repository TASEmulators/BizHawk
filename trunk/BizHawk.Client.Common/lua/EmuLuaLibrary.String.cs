using System;
namespace BizHawk.Client.Common
{
	public class StringLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "string"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"hex",
					"binary",
				};
			}
		}
		public static string string_hex(object num)
		{
			string hex = String.Format("{0:X}", LuaLong(num));
			if (hex.Length == 1) hex = "0" + hex;
			return hex;
		}

		public static string string_binary(object num)
		{
			string binary = Convert.ToString( LuaLong(num), 2);
			binary = binary.TrimStart('0');
			return binary;
		}

	}
}

