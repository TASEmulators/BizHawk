using System;

namespace NLua.Method
{
	public class LuaDelegate
	{
		public LuaFunction Function;
		public Type[] ReturnTypes;

		public LuaDelegate()
		{
			Function = null;
			ReturnTypes = null;
		}

		public object CallFunction(object[] args, object[] inArgs, int[] outArgs)
		{
			// args is the return array of arguments, inArgs is the actual array
			// of arguments passed to the function (with in parameters only), outArgs
			// has the positions of out parameters
			object returnValue;
			int iRefArgs;
			object[] returnValues = Function.Call(inArgs, ReturnTypes);

			if (ReturnTypes[0] == typeof(void))
			{
				returnValue = null;
				iRefArgs = 0;
			}
			else
			{
				returnValue = returnValues[0];
				iRefArgs = 1;
			}

			// Sets the value of out and ref parameters (from
			// the values returned by the Lua function).
			for (int i = 0; i < outArgs.Length; i++)
			{
				args[outArgs[i]] = returnValues[iRefArgs];
				iRefArgs++;
			}

			return returnValue;
		}
	}
}