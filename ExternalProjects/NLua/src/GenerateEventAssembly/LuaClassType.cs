using System;

namespace NLua.GenerateEventAssembly
{
	/// <summary>
	/// Structure to store a type and the return types of
	/// its methods (the type of the returned value and out/ref
	///  parameters).
	/// </summary>
	internal struct LuaClassType
	{
		public Type klass;
		public Type[][] returnTypes;
	}
}