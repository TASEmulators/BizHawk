using System;

namespace BizHawk.Client.Common
{
	[AttributeUsage(AttributeTargets.Method)]
	public class LuaMethodExample : Attribute
	{
		public LuaMethodExample(string name, string example)
		{
			Name = name;
			Example = example;
		}

		public string Name { get; }
		public string Example { get; }
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class LuaLibraryExample : Attribute
	{
		public LuaLibraryExample(bool released)
		{
			Released = released;
		}

		public bool Released { get; }
	}
}
