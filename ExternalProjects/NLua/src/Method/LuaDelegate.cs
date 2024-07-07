using System;

namespace NLua.Method
{
	// ReSharper disable once ClassNeverInstantiated.Global
	public class LuaDelegate
	{
		public LuaFunction Function;
		public Type[] ReturnTypes;

		// ReSharper disable once UnusedMember.Global
		// ReSharper disable once ParameterTypeCanBeEnumerable.Global
		public object CallFunction(object[] args, object[] inArgs, int[] outArgs)
		{
			// args is the return array of arguments, inArgs is the actual array
			// of arguments passed to the function (with in parameters only), outArgs
			// has the positions of out parameters
			object returnValue;
			int iRefArgs;
			var returnValues = Function.Call(inArgs, ReturnTypes);

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
			foreach (var t in outArgs)
			{
				args[t] = returnValues[iRefArgs];
				iRefArgs++;
			}

			return returnValue;
		}
	}
}