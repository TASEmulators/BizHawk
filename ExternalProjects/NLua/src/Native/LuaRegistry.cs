namespace NLua.Native
{
	/// <summary>
	/// Enum for pseudo-index used by registry table
	/// </summary>
	public enum LuaRegistry
	{
		/// <summary>
		/// pseudo-index used by registry table
		/// </summary>
		Index = -1000000 - 1000 // LUAI_MAXSTACK 1000000
	}

	/// <summary>
	/// Registry index 
	/// </summary>
	public enum LuaRegistryIndex
	{
		/// <summary>
		///  At this index the registry has the main thread of the state.
		/// </summary>
		MainThread = 1,
		/// <summary>
		/// At this index the registry has the global environment. 
		/// </summary>
		Globals = 2,
	}
}
