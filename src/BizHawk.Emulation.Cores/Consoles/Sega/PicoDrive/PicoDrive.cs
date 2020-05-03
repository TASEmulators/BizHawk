using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;
using System;
using System.Runtime.InteropServices;
using System.IO;
using BizHawk.Common;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive
{
	[Core("PicoDrive", "notaz", true, true,
		"0e352905c7aa80b166933970abbcecfce96ad64e", "https://github.com/notaz/picodrive", false)]
	public class PicoDrive : WaterboxCore, IDriveLight, IRegionable, ISettable<object, PicoDrive.SyncSettings>
	{
		private LibPicoDrive _core;
		private LibPicoDrive.CDReadCallback _cdcallback;
		private Disc _cd;
		private DiscSectorReader _cdReader;
		private bool _isPal;

		[CoreConstructor("GEN")]
		public PicoDrive(CoreComm comm, GameInfo game, byte[] rom, bool deterministic, SyncSettings syncSettings)
			: this(comm, game, rom, null, deterministic, syncSettings)
		{ }

		public PicoDrive(CoreComm comm, GameInfo game, Disc cd, bool deterministic, SyncSettings syncSettings)
			: this(comm, game, null, cd, deterministic, syncSettings)
		{ }

		private PicoDrive(CoreComm comm, GameInfo game, byte[] rom, Disc cd, bool deterministic, SyncSettings syncSettings)
			: base(comm, new Configuration
			{
				MaxSamples = 2048,
				DefaultWidth = 320,
				DefaultHeight = 224,
				MaxWidth = 320,
				MaxHeight = 480,
				SystemId = "GEN"
			})
		{
			var biosg = comm.CoreFileProvider.GetFirmware("32X", "G", false);
			var biosm = comm.CoreFileProvider.GetFirmware("32X", "M", false);
			var bioss = comm.CoreFileProvider.GetFirmware("32X", "S", false);
			var has32xBios = biosg != null && biosm != null && bioss != null;
			if (deterministic && !has32xBios)
				throw new InvalidOperationException("32X BIOS files are required for deterministic mode");
			deterministic |= has32xBios;

			_syncSettings = syncSettings ?? new SyncSettings();

			_core = PreInit<LibPicoDrive>(new PeRunnerOptions
			{
				Filename = "picodrive.wbx",
				SbrkHeapSizeKB = 64,
				SealedHeapSizeKB = 18 * 1024,
				InvisibleHeapSizeKB = 1024,
				MmapHeapSizeKB = 4096,
				PlainHeapSizeKB = 64,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
			});

			if (has32xBios)
			{
				_exe.AddReadonlyFile(biosg, "32x.g");
				_exe.AddReadonlyFile(biosm, "32x.m");
				_exe.AddReadonlyFile(bioss, "32x.s");
				Console.WriteLine("Using supplied 32x BIOS files");
			}
			if (cd != null)
			{
				_exe.AddReadonlyFile(comm.CoreFileProvider.GetFirmware("GEN", "CD_BIOS_EU", true), "cd.eu");
				_exe.AddReadonlyFile(comm.CoreFileProvider.GetFirmware("GEN", "CD_BIOS_US", true), "cd.us");
				_exe.AddReadonlyFile(comm.CoreFileProvider.GetFirmware("GEN", "CD_BIOS_JP", true), "cd.jp");
				_exe.AddReadonlyFile(gpgx.GPGX.GetCDData(cd), "toc");
				_cd = cd;
				_cdReader = new DiscSectorReader(_cd);
				_cdcallback = CDRead;
				_core.SetCDReadCallback(_cdcallback);
				DriveLightEnabled = true;
			}
			else
			{
				_exe.AddReadonlyFile(rom, "romfile.md");
			}

			var regionAutoOrder = (LibPicoDrive.Region)(
				(int)_syncSettings.FirstChoice |
				(int)_syncSettings.SecondChoice << 4 |
				(int)_syncSettings.ThirdChoice << 8);

			if (!_core.Init(cd != null, game["32X"], regionAutoOrder, _syncSettings.RegionOverride))
				throw new InvalidOperationException("Core rejected the file!");

			if (cd != null)
			{
				_exe.RemoveReadonlyFile("cd.eu");
				_exe.RemoveReadonlyFile("cd.us");
				_exe.RemoveReadonlyFile("cd.jp");
				_exe.RemoveReadonlyFile("toc");
				_core.SetCDReadCallback(null);
			}
			else
			{
				_exe.RemoveReadonlyFile("romfile.md");
			}
			if (has32xBios)
			{
				_exe.RemoveReadonlyFile("32x.g");
				_exe.RemoveReadonlyFile("32x.m");
				_exe.RemoveReadonlyFile("32x.s");
			}

			PostInit();
			ControllerDefinition = PicoDriveController;
			DeterministicEmulation = deterministic;
			_core.SetCDReadCallback(_cdcallback);

			_isPal = _core.IsPal();
			VsyncNumerator = _isPal ? 53203424 : 53693175;
			VsyncDenominator = _isPal ? 3420 * 313 : 3420 * 262;

			Is32XActive = game["32X"];
		}

		public bool Is32XActive { get; }

		public static readonly ControllerDefinition PicoDriveController = new ControllerDefinition
		{
			Name = "PicoDrive Genesis Controller",
			BoolButtons =
			{
				"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 A", "P1 B", "P1 C", "P1 Start", "P1 X", "P1 Y", "P1 Z", "P1 Mode",
				"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 A", "P2 B", "P2 C", "P2 Start", "P2 X", "P2 Y", "P2 Z", "P2 Mode",
				"Power", "Reset"
			}
		};

		private static readonly string[] ButtonOrders =
		{
			"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 B", "P1 C", "P1 A", "P1 Start", "P1 Z", "P1 Y", "P1 X", "P1 Mode",
			"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 B", "P2 C", "P2 A", "P2 Start", "P2 Z", "P2 Y", "P2 X", "P2 Mode",
			"Power", "Reset"
		};

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
			DriveLightOn = false;
			return new LibPicoDrive.FrameInfo { Buttons = b };
		}

		private void CDRead(int lba, IntPtr dest, bool audio)
		{
			if (audio)
			{
				byte[] data = new byte[2352];
				if (lba < _cd.Session1.LeadoutLBA && lba >= _cd.Session1.Tracks[2].LBA)
				{
					_cdReader.ReadLBA_2352(lba, data, 0);
				}
				Marshal.Copy(data, 0, dest, 2352);
			}
			else
			{
				byte[] data = new byte[2048];
				_cdReader.ReadLBA_2048(lba, data, 0);
				Marshal.Copy(data, 0, dest, 2048);
				DriveLightOn = true;
			}
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_core.SetCDReadCallback(_cdcallback);
		}

		#region ISettable

		public class SyncSettings
		{
			[DefaultValue(LibPicoDrive.Region.Auto)]
			[Description("If set, force the console to this region")]
			public LibPicoDrive.Region RegionOverride { get; set; }

			[DefaultValue(LibPicoDrive.Region.Auto)]
			[Description("When region is set to automatic, highest priority region to use if the game supports multiple regions")]
			public LibPicoDrive.Region FirstChoice { get; set; }

			[DefaultValue(LibPicoDrive.Region.Auto)]
			[Description("When region is set to automatic, second highest priority region to use if the game supports multiple regions")]
			public LibPicoDrive.Region SecondChoice { get; set; }

			[DefaultValue(LibPicoDrive.Region.Auto)]
			[Description("When region is set to automatic, lowest priority region to use if the game supports multiple regions")]
			public LibPicoDrive.Region ThirdChoice { get; set; }

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

		#endregion

		#region IDriveLight

		public bool DriveLightEnabled { get; }
		public bool DriveLightOn { get; private set; }

		#endregion

		#region IRegionable

		public DisplayType Region => _isPal ? DisplayType.PAL : DisplayType.NTSC;

		#endregion
	}
}
