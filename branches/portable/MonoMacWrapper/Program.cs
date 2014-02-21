using System;
using MonoMac.AppKit;

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

