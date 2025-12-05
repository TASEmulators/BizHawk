namespace BizHawk.Client.Common
{
	public interface INamedLuaFunction
	{
		Guid Guid { get; }

		string GuidStr { get; }

		string Name { get; }

		/// <summary>
		/// Will be called when the Lua function is unregistered / removed from the list of active callbacks.
		/// The intended use case is to support callback systems that don't directly support Lua.
		/// Here's what that looks like:
		/// 1) A NamedLuaFunction is created and added to it's owner's list of registered functions, as normal with all Lua functions.
		/// 2) A C# function is created for this specific NamedLuaFunction, which calls the Lua function via <see cref="Call(object[])"/> and possibly does other related Lua setup and cleanup tasks.
		/// 3) That C# function is added to the non-Lua callback system.
		/// 4) <see cref="OnRemove"/> is assigned an <see cref="Action"/> that removes the C# function from the non-Lua callback.
		/// </summary>
		Action OnRemove { get; set; }

		/// <summary>
		/// Calls the Lua function with the given arguments.
		/// </summary>
		object[] Call(object[] args);
	}
}
