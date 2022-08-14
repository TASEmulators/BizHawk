using System;

namespace BizHawk.Client.Common
{
	/// <summary>Indicates a parameter/return is (or contains) a string which may include non-ASCII characters.</summary>
	[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
	public sealed class LuaArbitraryStringParamAttribute : LuaStringParamAttributeBase {}

	/// <summary>Indicates a parameter/return is (or contains) a string which may only include ASCII characters.</summary>
	[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
	public sealed class LuaASCIIStringParamAttribute : LuaStringParamAttributeBase {}

	[AttributeUsage(AttributeTargets.Parameter)]
	public sealed class LuaColorParamAttribute : Attribute {}

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
	public sealed class LuaDeprecatedMethodAttribute : Attribute {}

	/// <summary>Indicates a parameter/return is (or contains) a string which is one of a known few constants (and these may only include ASCII characters).</summary>
	[AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Parameter)]
	public sealed class LuaEnumStringParamAttribute : LuaStringParamAttributeBase {}

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

	public abstract class LuaStringParamAttributeBase : Attribute {}

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
