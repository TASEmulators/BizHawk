using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class LuaSandbox
	{
		protected static Action<string> Logger;

		public static void SetLogger(Action<string> logger)
		{
			Logger = logger;
		}

		public static void SetCurrentDirectory(string dir)
		{
			CurrentDirectory = dir;
		}

		static string CurrentDirectory;

		public static void Sandbox(Action callback, Action exceptionCallback = null)
		{
			string savedEnvironmentCurrDir = null;
			try
			{
				//so. lets talk about current directories.
				//ideally we'd have one current directory per script. but things get hairy.
				//events and callbacks can get setup and it isn't clear what script they belong to.
				//moreover we don't really have a sense of sandboxing individual scripts, they kind of all get run together in the same VM, i think
				//so let's just try keeping one 'current directory' for all lua. it's an improvement over lua's 'current directory' for the process, interfering with the core emulator's
				savedEnvironmentCurrDir = Environment.CurrentDirectory;
				if(System.IO.Directory.Exists(CurrentDirectory)) //race condition for great justice
					Environment.CurrentDirectory = CurrentDirectory;

				EnvironmentSandbox.Sandbox(callback);
				CurrentDirectory = Environment.CurrentDirectory;
			}
			catch (LuaException ex)
			{
				Logger(ex.ToString());
				if (exceptionCallback != null)
				{
					exceptionCallback();
				}
			}
			finally
			{
				if (savedEnvironmentCurrDir != null)
				{
					if (System.IO.Directory.Exists(savedEnvironmentCurrDir)) //race condition for great justice
						Environment.CurrentDirectory = savedEnvironmentCurrDir;
				}
			}
		}
	}
}
