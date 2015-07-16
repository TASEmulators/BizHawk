using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.tools.Lua
{
	class LuaSandbox
	{
		protected static Action<string> Logger;

		public static void SetLogger(Action<string> logger)
		{
			Logger = logger;
		}
		
		public static void Sandbox(Action callback, Action exceptionCallback = null)
		{
			try
			{
				EnvironmentSandbox.Sandbox(callback);
			}
			catch (LuaException ex)
			{
				Logger(ex.ToString());
				if (exceptionCallback != null)
				{
					exceptionCallback();
				}
			}
		}
	}
}
