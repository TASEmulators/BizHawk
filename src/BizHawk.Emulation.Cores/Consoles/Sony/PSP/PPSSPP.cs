using System.Drawing;
using System.IO;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	[PortedCore(CoreNames.Encore, "", "nightly-2104", "https://github.com/CasualPokePlayer/encore", singleInstance: true)]
	[ServiceNotApplicable(typeof(IRegionable))]
	public partial class PPSSPP : IRomInfo
	{
		private static DynamicLibraryImportResolver _resolver;
		private static LibPPSSPP _core;

		// This is a hack, largely just so we can forcefully evict file handles Encore keeps open even after shutdown.
		// While keeping these file handles open is mostly harmless, this ends up being bad when recording a movie.
		// These file handles would be in the user folder, and the user folder must be cleared out when recording a movie!
		private static void ResetEncoreResolver()
		{
			_resolver?.Dispose();
			_resolver = new(OSTailoredCode.IsUnixHost ? "libppsspp.so" : "libppsspp.dll", hasLimitedLifetime: true);
			_core = BizInvoker.GetInvoker<LibPPSSPP>(_resolver, CallingConventionAdapters.Native);
		}

		private static PPSSPP CurrentCore;

		private readonly LibPPSSPP.ConfigCallbackInterface _configCallbackInterface;
		private readonly LibPPSSPP.InputCallbackInterface _inputCallbackInterface;
		private readonly IntPtr _context;
		private readonly PPSSPPVideoProvider _ppssppVideoProvider;

		public Rectangle TouchScreenRectangle { get; private set; }
		public bool TouchScreenRotated { get; private set; }
		public bool TouchScreenEnabled { get; private set; }

		[CoreConstructor(VSystemID.Raw.PSP)]
		public PPSSPP(CoreLoadParameters<Settings, SyncSettings> lp)
		{
			if (lp.Roms.Exists(static r => HawkFile.PathContainsPipe(r.RomPath)))
			{
				throw new InvalidOperationException("3DS does not support compressed ROMs");
			}

			CurrentCore?.Dispose();
			ResetEncoreResolver();

			CurrentCore = this;

			_serviceProvider = new(this);
			_settings = lp.Settings ?? new();
			_syncSettings = lp.SyncSettings ?? new();

			// copy firmware over to the user folder
			// this must be done before PPSSPP_CreateContext is called!

			_inputCallbackInterface.GetButton = GetButtonCallback;
			_inputCallbackInterface.GetAxis = GetAxisCallback;

			_context = _core.PPSSPP_CreateContext(ref _configCallbackInterface, ref _inputCallbackInterface);

			_ppssppVideoProvider = new(_core, _context);

			_serviceProvider.Register<IVideoProvider>(_ppssppVideoProvider);

			var romPath = lp.Roms[0].RomPath;

			var errorMessage = new byte[1024];
			if (!_core.PPSSPP_LoadROM(_context, romPath, errorMessage, errorMessage.Length))
			{
				Dispose();
				throw new($"{Encoding.UTF8.GetString(errorMessage).TrimEnd('\0')}");
			}

			InitMemoryDomains();
			// for some reason, if a savestate is created on frame 0, Encore will crash if another savestate is made after loading that state
			// advance one frame to avoid that issue
			_core.PPSSPP_RunFrame(_context);
			OnVideoRefresh();
		}

		public string RomDetails { get; }
	}
}
