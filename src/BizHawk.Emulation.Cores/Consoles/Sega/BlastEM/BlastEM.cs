using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Consoles.Sega.BlastEm
{
	[PortedCore(CoreNames.BlastEm, "mikepavone", "1234567", "https://www.retrodev.com/repos/blastem/file/tip")]
	public class BlastEm : WaterboxCore, IRegionable, ISettable<object, BlastEm.SyncSettings>
	{
		private readonly LibBlastEm _core;
		private readonly bool _isPal = false;

		[CoreConstructor(VSystemID.Raw.GEN, Priority = CorePriority.Low)]
		public BlastEm(CoreComm comm, GameInfo game, byte[] rom, bool deterministic, SyncSettings syncSettings)
			: this(comm, game, rom, null, deterministic, syncSettings)
		{ }

		private BlastEm(CoreComm comm, GameInfo game, byte[] rom, Disc cd, bool deterministic, SyncSettings syncSettings)
			: base(comm, new Configuration
			{
				MaxSamples = 2048,
				DefaultWidth = 320,
				DefaultHeight = 224,
				MaxWidth = 320,
				MaxHeight = 480,
				SystemId = VSystemID.Raw.GEN,
			})
		{
			_syncSettings = syncSettings ?? new SyncSettings();

			_core = PreInit<LibBlastEm>(new WaterboxOptions
			{
				Filename = "libblastem.wbx",
				SbrkHeapSizeKB = 3 * 512,
				SealedHeapSizeKB = 10 * 1024,
				MmapHeapSizeKB = 4 * 1024,
				PlainHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 12 * 1024,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, Array.Empty<Delegate>());

			if (cd == null)
			{
				_exe.AddReadonlyFile(rom, "romfile.md");
			}

			if (!_core.Init(cd != null, false, LibBlastEm.Region.Auto, _syncSettings.RegionOverride))
				throw new InvalidOperationException("Core rejected the file!");

			PostInit();
			ControllerDefinition = SegaController;
			DeterministicEmulation = deterministic;

			_isPal = false;
			VsyncNumerator = _isPal ? 53203424 : 53693175;
			VsyncDenominator = _isPal ? 3420 * 313 : 3420 * 262;
		}

		public static readonly ControllerDefinition SegaController = new ControllerDefinition("BlastEM Genesis Controller")
		{
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 C", "P1 Start", "P1 X", "P1 Y", "P1 Z", "P1 Mode",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 A", "P2 B", "P2 C", "P2 Start", "P2 X", "P2 Y", "P2 Z", "P2 Mode",
				"Power", "Reset",
			},
		}.MakeImmutable();

		private static readonly string[] ButtonOrders =
		{
			"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B", "P1 C", "P1 A", "P1 Start", "P1 Z", "P1 Y", "P1 X", "P1 Mode",
			"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B", "P2 C", "P2 A", "P2 Start", "P2 Z", "P2 Y", "P2 X", "P2 Mode",
			"Power", "Reset",
		};

		[CoreSettings]
		public class SyncSettings
		{
			[DefaultValue(LibBlastEm.Region.Auto)]
			public LibBlastEm.Region RegionOverride { get; set; }

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		private SyncSettings _syncSettings;

		public object GetSettings()
		{
			return new object();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(object o)
		{
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public DisplayType Region => _isPal ? DisplayType.PAL : DisplayType.NTSC;
		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var b = 0;
			var v = 1;
			foreach (var s in ButtonOrders)
			{
				if (controller.IsPressed(s))
					b |= v;
				v <<= 1;
			}
			return new LibBlastEm.FrameInfo { Buttons = b };
		}
	}
}
