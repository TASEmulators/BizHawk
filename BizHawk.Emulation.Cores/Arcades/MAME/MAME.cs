using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	[Core(
		name: "MAME",
		author: "MAMEDev",
		isPorted: true,
		portedVersion: "0.209",
		portedUrl: "https://github.com/mamedev/mame.git",
		singleInstance: false)]
	public partial class MAME : IEmulator
	{
		[CoreConstructor("MAME")]
		public MAME()
		{
			UInt32 test = LibMAME.mame_five();
			var args = CommandLineToArgs("mame -window");
			LibMAME.mame_launch(args.Length, args);
		}

		[DllImport("shell32.dll", SetLastError = true)]
		public static extern IntPtr CommandLineToArgvW(
		[MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

		public static string[] CommandLineToArgs(string commandLine)
		{
			int argc;
			var argv = CommandLineToArgvW(commandLine, out argc);

			if (argv == IntPtr.Zero)
			{
				throw new System.ComponentModel.Win32Exception();
			}
			try
			{
				var args = new string[argc];
				for (var i = 0; i < args.Length; i++)
				{
					var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
					args[i] = Marshal.PtrToStringUni(p);
				}

				return args;
			}
			finally
			{
				Marshal.FreeHGlobal(argv);
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ControllerDefinition ControllerDefinition { get; private set; }

		public int Frame { get; private set; }

		public string SystemId => "MAME";

		public bool DeterministicEmulation => true;

		public CoreComm CoreComm { get; private set; }

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			//throw new NotImplementedException();
			return false;
		}

		public void ResetCounters()
		{
			//throw new NotImplementedException();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MAME() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

	}
}
