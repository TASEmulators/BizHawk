using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class LuaFunctionList : List<NamedLuaFunction>
	{
		public NamedLuaFunction this[string guid]
		{
			get
			{
				return this.FirstOrDefault(x => x.GUID.ToString() == guid) ?? null;
			}
		}

		public void RemoveFunction(NamedLuaFunction function)
		{
			Global.Emulator.CoreComm.InputCallback.Remove(function.Callback);
			Remove(function);
		}
	}
}
