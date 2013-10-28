using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
	}
}
