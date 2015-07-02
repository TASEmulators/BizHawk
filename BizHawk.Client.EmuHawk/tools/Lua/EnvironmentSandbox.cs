using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.EmuHawk.tools.Lua
{
	public class EnvironmentSandbox
	{
		public static void Sandbox(Action callback)
		{
			string oldCurrentDirectory = Environment.CurrentDirectory;
			
			try
			{
				callback();
			}
			finally
			{
				Environment.CurrentDirectory = oldCurrentDirectory;
			}
		}
	}
}
