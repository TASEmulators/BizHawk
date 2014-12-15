using System;

namespace BizHawk.Client.Common
{
	[AttributeUsage(AttributeTargets.Method)]
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

	[AttributeUsage(AttributeTargets.Class)]
	public class LuaLibraryAttributes : Attribute
	{
		public LuaLibraryAttributes(bool released)
		{
			Released = released;
		}

		public bool Released { get; set; }
	}
}
