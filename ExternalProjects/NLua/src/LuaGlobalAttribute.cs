using System;

namespace NLua
{
	/// <summary>
	/// Marks a method for global usage in Lua scripts
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class LuaGlobalAttribute : Attribute
	{
		/// <summary>
		/// An alternative name to use for calling the function in Lua - leave empty for CLR name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// A description of the function
		/// </summary>
		public string Description { get; set; }
	}
}