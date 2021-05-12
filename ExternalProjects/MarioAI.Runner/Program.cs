using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.DirectX;
using BizHawk.Bizware.OpenTK3;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace MarioAI.Runner
{
    static class Program
	{


		static Program()
		{
			var dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");

			SetDllDirectory(dllDir);

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}

		static void Main(string[] args)
        {
			N64Settings n64Settings = new N64Settings();

			N64SyncSettings n64SyncSettings = new N64SyncSettings()
			{
				Core = N64SyncSettings.CoreType.Interpret
			};

			GameInfo gameInfo = new GameInfo() { Region = "US", Status = RomStatus.GoodDump } ;

			var file = File.ReadAllBytes("C:\\temp\\mario.n64");

			N64 emulator = new N64(gameInfo, file, n64Settings, n64SyncSettings);

			IGL igl = TryInitIGL(EDispMethod.SlimDX9);

			Config config = new Config();

			InputManager inputManager = new InputManager();

			CustomRenderer renderForm = new CustomRenderer(config, igl, emulator, inputManager);

			renderForm.Show();

			var controller = new DebugController();

			for (int i = 0; i < 200; i++)
			{
				Console.WriteLine("Current Frame: {0}", emulator.Frame);

				Console.WriteLine("Current Frame is skipframe: {0}", emulator.IsLagFrame);

				emulator.FrameAdvance(controller, false, false);

				Console.WriteLine("Next Frame: {0}", emulator.Frame);
			}
		}

		private static IGL TryInitIGL(EDispMethod dispMethod)
		{
			switch (dispMethod)
			{
				case EDispMethod.SlimDX9:

					IGL_SlimDX9 glSlimDX;

					try
					{
						glSlimDX = new IGL_SlimDX9();
					}
					catch (Exception ex)
					{
						return TryInitIGL(EDispMethod.GdiPlus);
					}

					return CheckRenderer(glSlimDX);

				default:
				case EDispMethod.GdiPlus:
					return new IGL_GdiPlus();
			}
		}

		private static IGL CheckRenderer(IGL gl)
		{
			try
			{
				using (gl.CreateRenderer())
				{
					return gl;
				}
			}
			catch (Exception ex)
			{
				return TryInitIGL(EDispMethod.GdiPlus);
			}
		}

		//declared here instead of a more usual place to avoid dependencies on the more usual place
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetDllDirectory(string lpPathName);

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var requested = args.Name;

			lock (AppDomain.CurrentDomain)
			{
				var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == requested);
				if (firstAsm != null) return firstAsm;

				//load missing assemblies by trying to find them in the dll directory
				var dllname = $"{new AssemblyName(requested).Name}.dll";
				var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				var simpleName = new AssemblyName(requested).Name;
				if (simpleName == "NLua" || simpleName == "KopiLua") directory = Path.Combine(directory, "nlua");
				var fname = Path.Combine(directory, dllname);
				//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;
			}
		}
	}
}
