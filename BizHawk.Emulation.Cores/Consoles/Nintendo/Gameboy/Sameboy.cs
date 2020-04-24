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
	[Core("SameBoy", "LIJI32", true, true, "efc11783c7fb6da66e1dd084e41ba6a85c0bd17e",
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

		private LibSameboy _core;
		private bool _cgb;
		private bool _sgb;

		[CoreConstructor("SGB")]
		public Sameboy(byte[] rom, CoreComm comm, Settings settings, SyncSettings syncSettings, bool deterministic)
			: this(rom, comm, true, settings, syncSettings, deterministic)
		{ }

		[CoreConstructor("GB")]
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
			_core = PreInit<LibSameboy>(new PeRunnerOptions
			{
				Filename = "sameboy.wbx",
				SbrkHeapSizeKB = 192,
				InvisibleHeapSizeKB = 12,
				SealedHeapSizeKB = 9 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 1024
			});

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

			PostInit();

			var scratch = new IntPtr[4];
			_core.GetGpuMemory(scratch);
			_gpuMemory = new GPUMemoryAreas(scratch[0], scratch[1], scratch[3], scratch[2], _exe);

			DeterministicEmulation = deterministic || !_syncSettings.UseRealTime;
			InitializeRtc(_syncSettings.InitialTime);
		}

		#region Controller

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
				if (i != 1)
					b = (LibSameboy.Buttons)((uint)b << 8);
			}
			return b;
		}

		#endregion

		#region ISaveram

		public new bool SaveRamModified => _core.HasSaveRam();

		public new byte[] CloneSaveRam()
		{
			_exe.AddTransientFile(null, "save.ram");
			_core.GetSaveRam();
			return _exe.RemoveTransientFile("save.ram");
		}

		public new void StoreSaveRam(byte[] data)
		{
			_exe.AddReadonlyFile(data, "save.ram");
			_core.PutSaveRam();
			_exe.RemoveReadonlyFile("save.ram");
		}

		#endregion

		#region ISettable

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

		public bool PutSettings(Settings o)
		{
			var ret = Settings.NeedsReboot(_settings, o);
			_settings = o;
			return ret;
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret;
		}

		#endregion

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
			if (_scanlineCallback != null && _scanlineCallbackLine == -1)
				_scanlineCallback(_core.GetIoReg(0x40));

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
			_core.SetPrinterCallback(_printerCallback);
		}

		public bool IsCGBMode() => _cgb;

		private GPUMemoryAreas _gpuMemory;

		public GPUMemoryAreas GetGPU() => _gpuMemory;
		private ScanlineCallback _scanlineCallback;
		private int _scanlineCallbackLine;

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			_scanlineCallback = callback;
			_scanlineCallbackLine = line;
			UpdateCoreScanlineCallback(true);
		}

		private void UpdateCoreScanlineCallback(bool now)
		{
			if (_scanlineCallback == null)
			{
				_core.SetScanlineCallback(null, -1);
			}
			else
			{
				if (_scanlineCallbackLine >= 0 && _scanlineCallbackLine <= 153)
				{
					_core.SetScanlineCallback(_scanlineCallback, _scanlineCallbackLine);
				}
				else
				{
					_core.SetScanlineCallback(null, -1);
					if (_scanlineCallbackLine == -2 && now)
					{
						_scanlineCallback(_core.GetIoReg(0x40));
					}
				}
			}
		}
		
		private PrinterCallback _printerCallback;
		
		public void SetPrinterCallback(PrinterCallback callback)
		{
			_printerCallback = callback;
			_core.SetPrinterCallback(callback);
		}
	}
}
