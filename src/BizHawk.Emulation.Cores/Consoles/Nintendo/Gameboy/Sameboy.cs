using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;
using System;

using System.ComponentModel;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	[Core(CoreNames.SameBoy, "LIJI32", true, true, "efc11783c7fb6da66e1dd084e41ba6a85c0bd17e",
		"https://sameboy.github.io/", false)]
	public class Sameboy : WaterboxCore,
		IGameboyCommon, ISaveRam,
		ISettable<Sameboy.Settings, Sameboy.SyncSettings>
	{
		/// <summary>
		/// the nominal length of one frame
		/// </summary>
		private const int TICKSPERFRAME = 35112;

		/// <summary>
		/// number of ticks per second (GB, CGB)
		/// </summary>
		private const int TICKSPERSECOND = 2097152;

		/// <summary>
		/// number of ticks per second (SGB)
		/// </summary>
		private const int TICKSPERSECOND_SGB = 2147727;

		private readonly LibSameboy _core;
		private readonly bool _cgb;
		private readonly bool _sgb;

		private readonly IntPtr[] _cachedGpuPointers = new IntPtr[4];

		[CoreConstructor("SGB")]
		public Sameboy(byte[] rom, CoreComm comm, Settings settings, SyncSettings syncSettings, bool deterministic)
			: this(rom, comm, true, settings, syncSettings, deterministic)
		{ }

		[CoreConstructor("GB")]
		[CoreConstructor("GBC")]
		public Sameboy(CoreComm comm, byte[] rom, Settings settings, SyncSettings syncSettings, bool deterministic)
			: this(rom, comm, false, settings, syncSettings, deterministic)
		{ }

		public Sameboy(byte[] rom, CoreComm comm, bool sgb, Settings settings, SyncSettings syncSettings, bool deterministic)
			: base(comm, new Configuration
			{
				DefaultWidth = sgb && (settings == null || settings.ShowSgbBorder) ? 256 : 160,
				DefaultHeight = sgb && (settings == null || settings.ShowSgbBorder) ? 224 : 144,
				MaxWidth = sgb ? 256 : 160,
				MaxHeight = sgb ? 224 : 144,
				MaxSamples = 1024,
				DefaultFpsNumerator = sgb ? TICKSPERSECOND_SGB : TICKSPERSECOND,
				DefaultFpsDenominator = TICKSPERFRAME,
				SystemId = sgb ? "SGB" : "GB"
			})
		{
			_corePrinterCallback = PrinterCallbackRelay;
			_coreScanlineCallback = ScanlineCallbackRelay;

			_core = PreInit<LibSameboy>(new WaterboxOptions
			{
				Filename = "sameboy.wbx",
				SbrkHeapSizeKB = 192,
				InvisibleHeapSizeKB = 12,
				SealedHeapSizeKB = 9 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 1024,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _corePrinterCallback, _coreScanlineCallback });

			_cgb = (rom[0x143] & 0xc0) == 0xc0 && !sgb;
			_sgb = sgb;
			Console.WriteLine("Automaticly detected CGB to " + _cgb);
			_syncSettings = syncSettings ?? new SyncSettings();
			_settings = settings ?? new Settings();

			var bios = _syncSettings.UseRealBIOS && !sgb
				? comm.CoreFileProvider.GetFirmware(_cgb ? "GBC" : "GB", "World", true)
				: Util.DecompressGzipFile(new MemoryStream(_cgb ? Resources.SameboyCgbBoot.Value : Resources.SameboyDmgBoot.Value));

			var spc = sgb
				? Util.DecompressGzipFile(new MemoryStream(Resources.SgbCartPresent_SPC.Value))
				: null;

			_exe.AddReadonlyFile(rom, "game.rom");
			_exe.AddReadonlyFile(bios, "boot.rom");

			if (!_core.Init(_cgb, spc, spc?.Length ?? 0))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			_exe.RemoveReadonlyFile("game.rom");
			_exe.RemoveReadonlyFile("boot.rom");

			_core.GetGpuMemory(_cachedGpuPointers);

			PostInit();

			DeterministicEmulation = deterministic || !_syncSettings.UseRealTime;
			InitializeRtc(_syncSettings.InitialTime);
		}

		private static readonly ControllerDefinition _gbDefinition;
		private static readonly ControllerDefinition _sgbDefinition;
		public override ControllerDefinition ControllerDefinition => _sgb ? _sgbDefinition : _gbDefinition;

		private static ControllerDefinition CreateControllerDefinition(int p)
		{
			var ret = new ControllerDefinition { Name = "Gameboy Controller" };
			for (int i = 0; i < p; i++)
			{
				ret.BoolButtons.AddRange(
					new[] { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" }
						.Select(s => $"P{i + 1} {s}"));
			}
			return ret;
		}

		static Sameboy()
		{
			_gbDefinition = CreateControllerDefinition(1);
			_sgbDefinition = CreateControllerDefinition(4);
		}

		private LibSameboy.Buttons GetButtons(IController c)
		{
			LibSameboy.Buttons b = 0;
			for (int i = _sgb ? 4 : 1; i > 0; i--)
			{
				if (c.IsPressed($"P{i} Up"))
					b |= LibSameboy.Buttons.UP;
				if (c.IsPressed($"P{i} Down"))
					b |= LibSameboy.Buttons.DOWN;
				if (c.IsPressed($"P{i} Left"))
					b |= LibSameboy.Buttons.LEFT;
				if (c.IsPressed($"P{i} Right"))
					b |= LibSameboy.Buttons.RIGHT;
				if (c.IsPressed($"P{i} A"))
					b |= LibSameboy.Buttons.A;
				if (c.IsPressed($"P{i} B"))
					b |= LibSameboy.Buttons.B;
				if (c.IsPressed($"P{i} Select"))
					b |= LibSameboy.Buttons.SELECT;
				if (c.IsPressed($"P{i} Start"))
					b |= LibSameboy.Buttons.START;
				if (_sgb)
				{
					// The SGB SNES side code enforces U+D/L+R prohibitions
					if (((uint)b & 0x30) == 0x30)
						b &= unchecked((LibSameboy.Buttons)~0x30);
					if (((uint)b & 0xc0) == 0xc0)
						b &= unchecked((LibSameboy.Buttons)~0xc0);
				}
				if (i != 1)
				{
					b = (LibSameboy.Buttons)((uint)b << 8);
				}
			}
			return b;
		}

		public new bool SaveRamModified => _core.HasSaveRam();

		public new byte[] CloneSaveRam()
		{
			_exe.AddTransientFile(new byte[0], "save.ram");
			_core.GetSaveRam();
			return _exe.RemoveTransientFile("save.ram");
		}

		public new void StoreSaveRam(byte[] data)
		{
			_exe.AddReadonlyFile(data, "save.ram");
			_core.PutSaveRam();
			_exe.RemoveReadonlyFile("save.ram");
		}

		private Settings _settings;
		private SyncSettings _syncSettings;

		public class Settings
		{
			[DisplayName("Show SGB Border")]
			[DefaultValue(true)]
			public bool ShowSgbBorder { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public static bool NeedsReboot(Settings x, Settings y)
			{
				return false;
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class SyncSettings
		{
			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.  Only relevant when UseRealTime is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime InitialTime { get; set; }

			[DisplayName("Use RealTime")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time.  Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[Description("If true, real BIOS files will be used.  Ignored in SGB mode.")]
			[DefaultValue(false)]
			public bool UseRealBIOS { get; set; }

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
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibSameboy.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Keys = GetButtons(controller)
			};
		}

		protected override unsafe void FrameAdvancePost()
		{
			if (_frontendScanlineCallback != null && _scanlineCallbackLine == -1)
				_frontendScanlineCallback(_core.GetIoReg(0x40));

			if (_sgb && !_settings.ShowSgbBorder)
			{
				fixed(int *buff = _videoBuffer)
				{
					int* dst = buff;
					int* src = buff + (224 - 144) / 2 * 256 + (256 - 160) / 2;
					for (int j = 0; j < 144; j++)
					{
						for (int i = 0; i < 160; i++)
						{
							*dst++ = *src++;
						}
						src += 256 - 160;
					}
				}
				BufferWidth = 160;
				BufferHeight = 144;
			}
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			UpdateCoreScanlineCallback(false);
			UpdateCorePrinterCallback();
		}

		public bool IsCGBMode() => _cgb;

		public IGPUMemoryAreas LockGPU()
		{
			_exe.Enter();
			try
			{
				return new GPUMemoryAreas(_exe)
				{
					Vram = _cachedGpuPointers[0],
					Oam = _cachedGpuPointers[1],
					Sppal = _cachedGpuPointers[3],
					Bgpal = _cachedGpuPointers[2]
				};
			}
			catch
			{
				_exe.Exit();
				throw;
			}
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			private IMonitor _monitor;
			public IntPtr Vram { get; init; }

			public IntPtr Oam { get; init; }

			public IntPtr Sppal { get; init; }

			public IntPtr Bgpal { get; init; }

			public GPUMemoryAreas(IMonitor monitor)
			{
				_monitor = monitor;
			}

			public void Dispose()
			{
				_monitor?.Exit();
				_monitor = null;
			}
		}

		private readonly ScanlineCallback _coreScanlineCallback;
		private ScanlineCallback _frontendScanlineCallback;
		private int _scanlineCallbackLine;

		private void ScanlineCallbackRelay(byte lcdc)
		{
			_frontendScanlineCallback?.Invoke(lcdc);
		}

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			_frontendScanlineCallback = callback;
			_scanlineCallbackLine = line;
			UpdateCoreScanlineCallback(true);
		}

		private void UpdateCoreScanlineCallback(bool now)
		{
			if (_frontendScanlineCallback == null)
			{
				_core.SetScanlineCallback(null, -1);
			}
			else
			{
				if (_scanlineCallbackLine >= 0 && _scanlineCallbackLine <= 153)
				{
					_core.SetScanlineCallback(_coreScanlineCallback, _scanlineCallbackLine);
				}
				else
				{
					_core.SetScanlineCallback(null, -1);
					if (_scanlineCallbackLine == -2 && now)
					{
						_frontendScanlineCallback(_core.GetIoReg(0x40));
					}
				}
			}
		}
		
		private readonly PrinterCallback _corePrinterCallback;
		private PrinterCallback _frontendPrinterCallback;

		private void PrinterCallbackRelay(IntPtr image, byte height, byte top_margin, byte bottom_margin, byte exposure)
		{
			_frontendPrinterCallback?.Invoke(image, height, top_margin, bottom_margin, exposure);
		}
		
		public void SetPrinterCallback(PrinterCallback callback)
		{
			_frontendPrinterCallback = callback;
			UpdateCorePrinterCallback();
		}

		private void UpdateCorePrinterCallback()
		{
			_core.SetPrinterCallback(_frontendPrinterCallback != null ? _corePrinterCallback : null);
		}
	}
}
