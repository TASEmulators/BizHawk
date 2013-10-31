using System;

using LuaInterface;

namespace BizHawk.MultiClient
{
	public abstract class LuaLibraryBase
	{
		public abstract string Name { get; }
		public abstract string[] Functions { get; }

		public virtual void LuaRegister(Lua lua, ILuaDocumentation docs = null)
		{
			lua.NewTable(Name);
			foreach (string methodName in Functions)
			{
				string func = Name + "." + methodName;
				var method = GetType().GetMethod(Name + "_" + methodName);

				lua.RegisterFunction(
					Name + "." + methodName,
					this,
					GetType().GetMethod(Name + "_" + methodName)
				);

				if (docs != null)
				{
					docs.Add(Name, methodName, method);
				}
			}
		}

		protected static int LuaInt(object lua_arg)
		{
			return Convert.ToInt32((double)lua_arg);
		}

		protected static uint LuaUInt(object lua_arg)
		{
			return Convert.ToUInt32((double)lua_arg);
		}
	}
}
