using System;
using MonoMac.AppKit;
using BizHawk.MultiClient;

namespace MonoMacWrapper
{
	class Program
	{
		public static void Main (string [] args)
		{
			NSApplication.Init();
			NSApplication.Main(args);
		}
	}
}

