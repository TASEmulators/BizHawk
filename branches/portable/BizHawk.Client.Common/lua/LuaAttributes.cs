using System;

namespace BizHawk.Client.Common
{
	public class LuaMethodAttributes : Attribute
	{
		public LuaMethodAttributes(string name, string description)
		{
			Name = name;
			Description = description;
		}

		public string Name { get; set; }
		public string Description { get; set; }
	}

	public class LuaLibraryAttributes : Attribute
	{
		public LuaLibraryAttributes(bool released)
		{
			Released = released;
		}

		public bool Released { get; set; }
	}
}
