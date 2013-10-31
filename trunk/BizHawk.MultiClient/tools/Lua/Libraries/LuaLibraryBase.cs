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
				lua.RegisterFunction(func, this, method);

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

		/// <summary>
		/// LuaInterface requires the exact match of parameter count, except optional parameters. 
		/// So, if you want to support variable arguments, declare them as optional and pass
		/// them to this method.
		/// </summary>
		/// <param name="lua_args"></param>
		/// <returns></returns>
		protected static object[] LuaVarArgs(params object[] lua_args)
		{
			int n = lua_args.Length;
			int trim = 0;
			for (int i = n - 1; i >= 0; --i)
				if (lua_args[i] == null) ++trim;
			object[] lua_result = new object[n - trim];
			Array.Copy(lua_args, lua_result, n - trim);
			return lua_result;
		}
	}
}
