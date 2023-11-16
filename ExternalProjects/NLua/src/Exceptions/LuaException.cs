using System;

namespace NLua.Exceptions
{
	/// <summary>
	/// Exceptions thrown by the Lua runtime
	/// </summary>
	[Serializable]
	public class LuaException : Exception
	{
		public LuaException (string message) : base(message)
		{
		}

		public LuaException (string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
