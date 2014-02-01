using System;

namespace BizHawk.Client.Common
{
	public class LuaMethodAttributes : Attribute
	{
		public string Name { get; set; }
		public string Description { get; set; }

		public LuaMethodAttributes(string name, string description)
		{
			Name = name;
			Description = description;
		}
	}
}
