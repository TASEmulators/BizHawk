using System;
using System.Linq;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public abstract class LuaLibraryBase
	{
		public abstract string Name { get; }

		public virtual void LuaRegister(Lua lua, ILuaDocumentation docs = null)
		{
			lua.NewTable(Name);

			var luaAttr = typeof(LuaMethodAttributes);

			var methods = GetType()
							.GetMethods()
							.Where(m => m.GetCustomAttributes(luaAttr, false).Any());

			foreach (var method in methods)
			{
				var luaMethodAttr = method.GetCustomAttributes(luaAttr, false).First() as LuaMethodAttributes;
				var luaName = Name + "." + luaMethodAttr.Name;
				lua.RegisterFunction(luaName, this, method);

				if (docs != null)
				{
					docs.Add(Name, luaMethodAttr.Name, method, luaMethodAttr.Description);
				}
			}
		}

		protected static int LuaInt(object luaArg)
		{
			return (int)(double)luaArg;
		}

		protected static uint LuaUInt(object luaArg)
		{
			return (uint)(double)luaArg;
		}

		/// <summary>
		/// LuaInterface requires the exact match of parameter count, except optional parameters. 
		/// So, if you want to support variable arguments, declare them as optional and pass
		/// them to this method.
		/// </summary>
		protected static object[] LuaVarArgs(params object[] luaArgs)
		{
			int n = luaArgs.Length;
			int trim = 0;
			for (int i = n - 1; i >= 0; --i)
			{
				if (luaArgs[i] == null)
				{
					++trim;
				}
			}

			var lua_result = new object[n - trim];
			Array.Copy(luaArgs, lua_result, n - trim);
			return lua_result;
		}
	}
}
