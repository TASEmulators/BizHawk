using System.IO;
using System.Reflection;

﻿using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;

#if NETCOREAPP
using System.Linq;
using System.Runtime.Loader;
#else
using System;
#endif

namespace BizHawk.Experiment.GTKClient
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			// assembly resolver: copy-pasted from EmuHawk/Program.cs, removed Lua-specific hacks, and added .NET Core version
#if NETCOREAPP
			AssemblyLoadContext.Default.Resolving += (context, requested) =>
			{
				var firstAsm = context.Assemblies.FirstOrDefault(asm => asm.FullName == requested.FullName);
				if (firstAsm != null) return firstAsm;

				//load missing assemblies by trying to find them in the dll directory
				var dllname = $"{requested.Name}.dll";
				var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "dll");
				var fname = Path.Combine(directory, dllname);
				//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;
			};
#else
			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				var requested = args.Name;
				lock (AppDomain.CurrentDomain)
				{
					var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == requested);
					if (firstAsm != null) return firstAsm;

					//load missing assemblies by trying to find them in the dll directory
					var dllname = $"{new AssemblyName(requested).Name}.dll";
					var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
					var fname = Path.Combine(directory, dllname);
					//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
					return File.Exists(fname) ? Assembly.LoadFile(fname) : null;
				}
			};
#endif
			MainLoop();
		}

		private static void MainLoop() {
			Gtk.Application.Init();
			Forms.Init();

			var app = new App();
			var window = new FormsWindow();
			window.LoadApplication(app);
			window.SetApplicationTitle("GTK Client Experiment");
			window.Show();
			Gtk.Application.Run();
		}
	}
}
