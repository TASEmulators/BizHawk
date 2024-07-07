namespace NLua.Native
{
	/// <summary>
	/// Lua Load/Call status return
	/// </summary>
	public enum LuaStatus
	{
		/// <summary>
		///  success
		/// </summary>
		OK = 0,
		/// <summary>
		/// Yield
		/// </summary>
		Yield = 1,
		/// <summary>
		/// a runtime error. 
		/// </summary>
		ErrRun = 2,
		/// <summary>
		/// syntax error during precompilation
		/// </summary>
		ErrSyntax = 3,
		/// <summary>
		///  memory allocation error. For such errors, Lua does not call the message handler. 
		/// </summary>
		ErrMem = 4,
		/// <summary>
		///  error while running the message handler. 
		/// </summary>
		ErrErr = 5,
	}
}
