namespace NLua.GenerateEventAssembly
{
	/// <summary>
	/// Common interface for types generated from tables. The method
	/// returns the table that overrides some or all of the type's methods.
	/// </summary>
	public interface ILuaGeneratedType
	{
		LuaTable LuaInterfaceGetLuaTable();
	}
}