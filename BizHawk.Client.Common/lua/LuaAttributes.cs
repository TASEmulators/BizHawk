using System;

namespace BizHawk.Client.Common
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class LuaMethodAttribute : Attribute
	{
		public LuaMethodAttribute(string name, string description)
		{
			Name = name;
			Description = description;
		}

		public string Name { get; }
		public string Description { get; }
	}

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class LuaMethodExampleAttribute : Attribute
	{
		public LuaMethodExampleAttribute(string example)
		{
			Example = example;
		}

		public string Example { get; }
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class LuaLibraryAttribute : Attribute
	{
		public LuaLibraryAttribute(bool released)
		{
			Released = released;
		}

		public bool Released { get; }
	}
}
