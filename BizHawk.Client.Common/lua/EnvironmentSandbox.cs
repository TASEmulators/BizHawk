using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
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
