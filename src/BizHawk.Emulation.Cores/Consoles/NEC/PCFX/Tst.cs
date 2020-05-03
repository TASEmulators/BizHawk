using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCFX
{
	[Core("T. S. T.", "Mednafen Team", true, true, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class Tst : WaterboxCore, IDriveLight,
		ISettable<Tst.Settings, Tst.SyncSettings>
	{
		private static readonly DiscSectorReaderPolicy _diskPolicy = new DiscSectorReaderPolicy
		{
			DeinterleavedSubcode = false
		};

		private LibTst _core;
		private Disc[] _disks;
		private DiscSectorReader[] _diskReaders;
		private LibSaturnus.CDTOCCallback _cdTocCallback;
		private LibSaturnus.CDSectorCallback _cdSectorCallback;
		private TstControllerDeck _controllerDeck;

		[CoreConstructor("PCFX")]
		public Tst(CoreComm comm, byte[] rom)
			: base(comm, new Configuration())
		{
			throw new InvalidOperationException("To load a PC-FX game, please load the CUE file and not the BIN file.");
		}

		public Tst(CoreComm comm, IEnumerable<Disc> disks, Settings settings, SyncSettings syncSettings)
			: base(comm, new Configuration
			{
				DefaultFpsNumerator = 7159091,
				DefaultFpsDenominator = 455 * 263,
				DefaultWidth = 256,
				DefaultHeight = 232,
				MaxWidth = 1024,
				MaxHeight = 480,
				MaxSamples = 2048,
				SystemId = "PCFX"
			})
		{
			var bios = comm.CoreFileProvider.GetFirmware("PCFX", "BIOS", true);
			if (bios.Length != 1024 * 1024)
				throw new InvalidOperationException("Wrong size BIOS file!");

			_disks = disks.ToArray();
			_diskReaders = disks.Select(d => new DiscSectorReader(d) { Policy = _diskPolicy }).ToArray();
			_cdTocCallback = CDTOCCallback;
			_cdSectorCallback = CDSectorCallback;
			_settings = settings ?? new Settings();
			_syncSettings = syncSettings ?? new SyncSettings();
			BufferHeight = _settings.ScanlineEnd - _settings.ScanlineStart + 1;

			_core = PreInit<LibTst>(new PeRunnerOptions
			{
				Filename = "pcfx.wbx",
				SbrkHeapSizeKB = 512,
				SealedHeapSizeKB = 2 * 1024,
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4 * 1024,
				MmapHeapSizeKB = 6 * 1024,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
			});

			SetCdCallbacks();
			SetNativeSettingsBeforeInit();
			if (!_core.Init(_disks.Length, bios))
				throw new InvalidOperationException("Core rejected the CDs!");
			ClearCdCallbacks();

			PostInit();
			SetCdCallbacks();
			_controllerDeck = new TstControllerDeck(new[] { _syncSettings.Port1, _syncSettings.Port2 });
			ControllerDefinition = _controllerDeck.Definition;
			SetLayerSettings();
		}

		public override int VirtualWidth => VirtualHeight > 240 ? 586 : 293;

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			SetCdCallbacks();
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			DriveLightOn = false;
			var ret = new LibTst.FrameInfo();
			var controls = _controllerDeck.GetData(controller);
			ret.Port1Buttons = controls[0];
			ret.Port2Buttons = controls[1];
			ret.ConsoleButtons = controls[2];
			return ret;
		}

		private void CDTOCCallback(int disk, [In, Out]LibSaturnus.TOC t)
		{
			Saturnus.SetupTOC(t, _disks[disk].TOC);
		}
		private void CDSectorCallback(int disk, int lba, IntPtr dest)
		{
			var buff = new byte[2448];
			_diskReaders[disk].ReadLBA_2448(lba, buff, 0);
			Marshal.Copy(buff, 0, dest, 2448);
			DriveLightOn = true;
		}

		private void SetCdCallbacks()
		{
			_core.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
		}
		private void ClearCdCallbacks()
		{
			_core.SetCDCallbacks(null, null);
		}

		public bool DriveLightEnabled => true;
		public bool DriveLightOn { get; private set; }

		#region ISettable

		public class Settings
		{
			[Description("Emulate a buggy ADPCM codec that makes some games sound better")]
			[DefaultValue(false)]
			public bool AdpcmEmulateBuggyCodec { get; set; }

			[Description("Suppress clicks on ADPCM channel resets")]
			[DefaultValue(true)]
			public bool AdpcmSuppressChannelResetClicks { get; set; }

			public enum DotClockWidths
			{
				Fastest = 256,
				Fast = 341,
				Good = 1024
			}
			[Description("Quality for high-width resolution output")]
			[DefaultValue(DotClockWidths.Good)]
			public DotClockWidths HiResEmulation { get; set; }

			[Description("Disable the hardware limit of 16 sprites per scanline")]
			[DefaultValue(false)]
			public bool DisableSpriteLimit { get; set; }
			[Description("Increase quality of some YUV output.  Can cause graphical glitches")]
			[DefaultValue(false)]
			public bool ChromaInterpolation { get; set; }

			[Description("First scanline to render")]
			[DefaultValue(4)]
			[Range(0, 8)]
			public int ScanlineStart { get; set; }

			[Description("Last scanline to render")]
			[DefaultValue(235)]
			[Range(231, 239)]
			public int ScanlineEnd { get; set; }

			[Description("Show layer BG0")]
			[DefaultValue(true)]
			public bool ShowLayerBG0
			{
				get => _showLayerBG0;
				set => _showLayerBG0 = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerBG0;

			[Description("Show layer BG1")]
			[DefaultValue(true)]
			public bool ShowLayerBG1
			{
				get => _showLayerBG1;
				set => _showLayerBG1 = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerBG1;

			[Description("Show layer BG2")]
			[DefaultValue(true)]
			public bool ShowLayerBG2
			{
				get => _showLayerBG2;
				set => _showLayerBG2 = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerBG2;

			[Description("Show layer BG3")]
			[DefaultValue(true)]
			public bool ShowLayerBG3
			{
				get => _showLayerBG3;
				set => _showLayerBG3 = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerBG3;

			[Description("Show layer VDC-A BG")]
			[DefaultValue(true)]
			public bool ShowLayerVDCABG
			{
				get => _showLayerVDCABG;
				set => _showLayerVDCABG = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerVDCABG;

			[Description("Show layer VDC-A SPR")]
			[DefaultValue(true)]
			public bool ShowLayerVDCASPR
			{
				get => _showLayerVDCASPR;
				set => _showLayerVDCASPR = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerVDCASPR;

			[Description("Show layer VDC-B BG")]
			[DefaultValue(true)]
			public bool ShowLayerVDCBBG
			{
				get => _showLayerVDCBBG;
				set => _showLayerVDCBBG = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerVDCBBG;

			[Description("Show layer VDC-B SPR")]
			[DefaultValue(true)]
			public bool ShowLayerVDCBSPR
			{
				get => _showLayerVDCBSPR;
				set => _showLayerVDCBSPR = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerVDCBSPR;

			[Description("Show layer RAINBOW")]
			[DefaultValue(true)]
			public bool ShowLayerRAINBOW
			{
				get => _showLayerRAINBOW;
				set => _showLayerRAINBOW = value;
			}
			[DeepEqualsIgnore]
			private bool _showLayerRAINBOW;

			[Description("Pixel Pro.  Overrides HiResEmulation if set")]
			[DefaultValue(false)]
			public bool PixelPro { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public static bool NeedsReboot(Settings x, Settings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class SyncSettings
		{
			[Description("Speed of the CD-ROM drive.  Can decrease load times")]
			[DefaultValue(2)]
			[Range(2, 10)]
			public int CdSpeed { get; set; }

			public enum CpuType
			{
				Fast,
				Accurate,
				Auto
			}
			[Description("CPU emulation accuracy.  Auto chooses per game from a database")]
			[DefaultValue(CpuType.Auto)]
			public CpuType CpuEmulation { get; set; }

			[Description("Input device for the left port")]
			[DefaultValue(ControllerType.Gamepad)]
			public ControllerType Port1 { get; set; }

			[Description("Input device for the right port")]
			[DefaultValue(ControllerType.Gamepad)]
			public ControllerType Port2 { get; set; }

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

		private Settings _settings;
		private SyncSettings _syncSettings;

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			var ret = Settings.NeedsReboot(_settings, o);
			_settings = o;
			SetLayerSettings();
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private void SetNativeSettingsBeforeInit()
		{
			var s = new LibTst.FrontendSettings
			{
				AdpcmEmulateBuggyCodec = _settings.AdpcmEmulateBuggyCodec ? 1 : 0,
				AdpcmSuppressChannelResetClicks = _settings.AdpcmSuppressChannelResetClicks ? 1 : 0,
				HiResEmulation = (int)_settings.HiResEmulation,
				DisableSpriteLimit = _settings.DisableSpriteLimit ? 1 : 0,
				ChromaInterpolation = _settings.ChromaInterpolation ? 1 : 0,
				ScanlineStart = _settings.ScanlineStart,
				ScanlineEnd = _settings.ScanlineEnd,
				CdSpeed = _syncSettings.CdSpeed,
				CpuEmulation = (int)_syncSettings.CpuEmulation,
				Port1 = (int)_syncSettings.Port1,
				Port2 = (int)_syncSettings.Port2,
				PixelPro = _settings.PixelPro ? 1 : 0
			};
			_core.PutSettingsBeforeInit(s);
		}

		private void SetLayerSettings()
		{
			var l = LibTst.Layers.None;
			if (_settings.ShowLayerBG0)
				l |= LibTst.Layers.BG0;
			if (_settings.ShowLayerBG1)
				l |= LibTst.Layers.BG1;
			if (_settings.ShowLayerBG2)
				l |= LibTst.Layers.BG2;
			if (_settings.ShowLayerBG3)
				l |= LibTst.Layers.BG3;
			if (_settings.ShowLayerVDCABG)
				l |= LibTst.Layers.VDCA_BG;
			if (_settings.ShowLayerVDCASPR)
				l |= LibTst.Layers.VDCA_SPR;
			if (_settings.ShowLayerVDCBBG)
				l |= LibTst.Layers.VDCB_BG;
			if (_settings.ShowLayerVDCBSPR)
				l |= LibTst.Layers.VDCB_SPR;
			if (_settings.ShowLayerRAINBOW)
				l |= LibTst.Layers.RAINBOW;
			_core.EnableLayers(l);
		}

		#endregion
	}
}
