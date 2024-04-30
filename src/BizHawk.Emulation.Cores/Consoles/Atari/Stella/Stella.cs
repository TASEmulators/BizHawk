using System;
using System.Linq;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Atari.Stella;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	[Core(CoreNames.Stella, "The Stella Team")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(ISaveRam) })]
	public partial class Stella : IEmulator, IDebuggable, IInputPollable, IRomInfo,
		ICreateGameDBEntries, ISettable<Stella.A2600Settings, Stella.A2600SyncSettings>
	{
		internal static class RomChecksums
		{
			public const string CongoBongo = "SHA1:3A77DB43B6583E8689435F0F14AA04B9E57BDDED";

			public const string KangarooNotInGameDB = "SHA1:982B8016B393A9AA7DD110295A53C4612ECF2141";

			public const string Tapper = "SHA1:E986E1818E747BEB9B33CE4DFF1CDC6B55BDB620";
		}

		[CoreConstructor(VSystemID.Raw.A26)]
		public Stella(CoreLoadParameters<Stella.A2600Settings, Stella.A2600SyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			SyncSettings = lp.SyncSettings ?? new A2600SyncSettings();
			Settings = lp.Settings ?? new A2600Settings();
			_controllerDeck = new Atari2600ControllerDeck(SyncSettings.Port1, SyncSettings.Port2);

			_elf = new WaterboxHost(new WaterboxOptions
			{
				Path = PathUtils.DllDirectoryPath,
				Filename = "stella.wbx",
				SbrkHeapSizeKB = 512,
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 4 * 1024,
				PlainHeapSizeKB = 4 * 1024,
				MmapHeapSizeKB = 1 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			var callingConventionAdapter = CallingConventionAdapters.MakeWaterbox(new Delegate[]
			{
				LoadCallback
			}, _elf);

			using (_elf.EnterExit())
			{
				Core = BizInvoker.GetInvoker<CInterface>(_elf, _elf, callingConventionAdapter);
				SyncSettings = lp.SyncSettings ?? new A2600SyncSettings();
				Settings = lp.Settings ?? new A2600Settings();

				CoreComm = lp.Comm;

				_romfile = lp.Roms.FirstOrDefault()?.RomData;

				var initResult = Core.stella_init(LoadCallback, SyncSettings.GetNativeSettings(lp.Game));

				if (!initResult) throw new Exception($"{nameof(Core.stella_init)}() failed");

				_elf.Seal();
			}
		}

		private CInterface.load_archive_cb LoadCallback;

		private readonly byte[] _romfile;
		private readonly CInterface Core;
		private readonly WaterboxHost _elf;
		private CoreComm CoreComm { get; }

		public string RomDetails { get; private set; }

		private readonly Atari2600ControllerDeck _controllerDeck;

		private ITraceable Tracer { get; }

		// ICreateGameDBEntries
		public CompactGameInfo GenerateGameDbEntry()
		{
			return new CompactGameInfo
			{
			};
		}

		// IBoardInfo
		private static bool DetectPal(GameInfo game, byte[] rom)
		{
			return true;
		}
	}
}
