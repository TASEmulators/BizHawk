using System;

namespace NLua.Exceptions
{
	/// <summary>
	/// Exceptions thrown by the Lua runtime because of errors in the script
	/// </summary>
	/// 
	[Serializable]
	public class LuaScriptException : LuaException
	{
		/// <summary>
		/// Returns true if the exception has occured as the result of a .NET exception in user code
		/// </summary>
		public bool IsNetException { get;  }

		private readonly string _source;

		/// <summary>
		/// The position in the script where the exception was triggered.
		/// </summary>
		public override string Source => _source;

		/// <summary>
		/// Creates a new Lua-only exception.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="source">The position in the script where the exception was triggered.</param>
		public LuaScriptException(string message, string source) : base(message)
		{
			_source = source;
		}

		/// <summary>
		/// Creates a new .NET wrapping exception.
		/// </summary>
		/// <param name="innerException">The .NET exception triggered by user-code.</param>
		/// <param name="source">The position in the script where the exception was triggered.</param>
		public LuaScriptException(Exception innerException, string source)
			: base("A .NET exception occured in user-code", innerException)
		{
			_source = source;
			IsNetException = true;
		}

		// Prepend the error source
		public override string ToString()
			=> GetType().FullName + ": " + _source + Message;
	}
}
