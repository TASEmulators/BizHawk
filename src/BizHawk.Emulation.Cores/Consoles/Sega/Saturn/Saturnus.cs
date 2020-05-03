using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	[Core("Saturnus", "Mednafen Team", true, true, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class Saturnus : WaterboxCore,
		IDriveLight, IRegionable, 
		ISettable<Saturnus.Settings, Saturnus.SyncSettings>
	{
		private static readonly DiscSectorReaderPolicy _diskPolicy = new DiscSectorReaderPolicy
		{
			DeinterleavedSubcode = false
		};

		private LibSaturnus _core;
		private Disc[] _disks;
		private DiscSectorReader[] _diskReaders;
		private bool _isPal;
		private SaturnusControllerDeck _controllerDeck;
		private int _activeDisk;
		private bool _prevDiskSignal;
		private bool _nextDiskSignal;

		private static bool CheckDisks(IEnumerable<DiscSectorReader> readers)
		{
			var buff = new byte[2048 * 16];
			foreach (var r in readers)
			{
				for (int i = 0; i < 16; i++)
				{
					if (r.ReadLBA_2048(i, buff, 2048 * i) != 2048)
						return false;
				}

				if (Encoding.ASCII.GetString(buff, 0, 16) != "SEGA SEGASATURN ")
					return false;

				using var sha256 = SHA256.Create();
				sha256.ComputeHash(buff, 0x100, 0xd00);
				if (sha256.Hash.BytesToHexString() != "96B8EA48819CFA589F24C40AA149C224C420DCCF38B730F00156EFE25C9BBC8F")
					return false;
			}
			return true;
		}

		[CoreConstructor("SAT")]
		public Saturnus(CoreComm comm, byte[] rom)
			:base(comm, new Configuration())
		{
			throw new InvalidOperationException("To load a Saturn game, please load the CUE file and not the BIN file.");
		}

		public Saturnus(CoreComm comm, IEnumerable<Disc> disks, bool deterministic, Settings settings,
			SyncSettings syncSettings)
			:base(comm, new Configuration
			{
				MaxSamples = 8192,
				DefaultWidth = 320,
				DefaultHeight = 240,
				MaxWidth = 1024,
				MaxHeight = 1024,
				SystemId = "SAT"
			})
		{
			settings = settings ?? new Settings();
			syncSettings = syncSettings ?? new SyncSettings();

			_disks = disks.ToArray();
			_diskReaders = disks.Select(d => new DiscSectorReader(d) { Policy = _diskPolicy }).ToArray();
			if (!CheckDisks(_diskReaders))
				throw new InvalidOperationException("Some disks are not valid");
			InitCallbacks();

			_core = PreInit<LibSaturnus>(new PeRunnerOptions
			{
				Filename = "ss.wbx",
				SbrkHeapSizeKB = 128,
				SealedHeapSizeKB = 4096, // 512KB of bios, 2MB of kof95/ultraman carts
				InvisibleHeapSizeKB = 8 * 1024, // 4MB of framebuffer
				MmapHeapSizeKB = 0, // not used?
				PlainHeapSizeKB = 24 * 1024, // up to 16MB of cart ram
				StartAddress = LibSaturnus.StartAddress,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
			});

			SetFirmwareCallbacks();
			SetCdCallbacks();

			if (!_core.Init(_disks.Length, syncSettings.ExpansionCart, syncSettings.Region, syncSettings.RegionAutodetect))
				throw new InvalidOperationException("Core rejected the disks!");
			ClearAllCallbacks();

			_controllerDeck = new SaturnusControllerDeck(new[]
			{
				syncSettings.Port1Multitap,
				syncSettings.Port2Multitap
			}, new[]
			{
				syncSettings.Port1,
				syncSettings.Port2,
				syncSettings.Port3,
				syncSettings.Port4,
				syncSettings.Port5,
				syncSettings.Port6,
				syncSettings.Port7,
				syncSettings.Port8,
				syncSettings.Port9,
				syncSettings.Port10,
				syncSettings.Port11,
				syncSettings.Port12
			}, _core);
			ControllerDefinition = _controllerDeck.Definition;
			ControllerDefinition.Name = "Saturn Controller";
			ControllerDefinition.BoolButtons.AddRange(new[]
			{
				"Power", "Reset", "Previous Disk", "Next Disk"
			});

			_core.SetRtc((long)syncSettings.InitialTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
				syncSettings.Language);

			PostInit();
			SetCdCallbacks();
			_core.SetDisk(0, false);
			PutSettings(settings);
			_syncSettings = syncSettings;
			DeterministicEmulation = deterministic || !_syncSettings.UseRealTime;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var prevDiskSignal = controller.IsPressed("Previous Disk");
			var nextDiskSignal = controller.IsPressed("Next Disk");
			var newDisk = _activeDisk;
			if (prevDiskSignal && !_prevDiskSignal)
				newDisk--;
			if (nextDiskSignal && !_nextDiskSignal)
				newDisk++;
			_prevDiskSignal = prevDiskSignal;
			_nextDiskSignal = nextDiskSignal;
			if (newDisk < -1)
				newDisk = -1;
			if (newDisk >= _disks.Length)
				newDisk = _disks.Length - 1;
			if (newDisk != _activeDisk)
			{
				_core.SetDisk(newDisk == -1 ? 0 : newDisk, newDisk == -1);
				_activeDisk = newDisk;
			}

			// if not reset, the core will maintain its own deterministic increasing time each frame
			if (!DeterministicEmulation)
			{
				_core.SetRtc((long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
					_syncSettings.Language);
			}

			DriveLightOn = false;
			if (controller.IsPressed("Power"))
				_core.HardReset();

			_core.SetControllerData(_controllerDeck.Poll(controller));

			SetVideoParameters();

			return new LibSaturnus.FrameInfo { ResetPushed = controller.IsPressed("Reset") ? 1 : 0 };
		}

		public DisplayType Region => _isPal ? DisplayType.PAL : DisplayType.NTSC;

		#region ISettable

		public class Settings
		{
			public enum ResolutionModeTypes
			{
				[Display(Name = "Pixel Pro")]
				PixelPro,
				[Display(Name = "Hardcode Debug")]
				HardcoreDebug,
				[Display(Name = "Mednafen (5:4 AR)")]
				Mednafen,
				[Display(Name = "Tweaked Mednafen (5:4 AR)")]
				TweakedMednafen,
			}

			[DisplayName("Resolution Mode")]
			[DefaultValue(ResolutionModeTypes.PixelPro)]
			[Description("Method for managing varying resolutions")]
			public ResolutionModeTypes ResolutionMode { get; set; }

			// extern bool setting_ss_h_blend;
			[DisplayName("Horizontal Blend")]
			[DefaultValue(false)]
			[Description("Use horizontal blend filter")]
			public bool HBlend { get; set; }

			// extern bool setting_ss_h_overscan;
			[DisplayName("Horizontal Overscan")]
			[DefaultValue(true)]
			[Description("Show horiziontal overscan area")]
			public bool HOverscan { get; set; }

			// extern int setting_ss_slstart;
			[DefaultValue(0)]
			[Description("First scanline to display in NTSC mode")]
			[Range(0, 239)]
			public int ScanlineStartNtsc { get; set; }
			// extern int setting_ss_slend;
			[DefaultValue(239)]
			[Description("Last scanline to display in NTSC mode")]
			[Range(0, 239)]
			public int ScanlineEndNtsc { get; set; }
			// extern int setting_ss_slstartp;
			[DefaultValue(0)]
			[Description("First scanline to display in PAL mode")]
			[Range(-16, 271)]
			public int ScanlineStartPal { get; set; }
			// extern int setting_ss_slendp;
			[DefaultValue(255)]
			[Description("Last scanline to display in PAL mode")]
			[Range(-16, 271)]
			public int ScanlineEndPal { get; set; }

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public static bool NeedsReboot(Settings x, Settings y)
			{
				return false;
			}
		}

		public class SyncSettings
		{
			public enum CartType
			{
				[Display(Name = "Autodetect.  Will use Backup Ram Cart if no others are appropriate")]
				Autodetect = -1,
				[Display(Name = "None")]
				None,
				[Display(Name = "Backup Ram Cart")]
				Backup,
				[Display(Name = "1 Meg Ram Cart")]
				Ram1Meg,
				[Display(Name = "4 Meg Ram Cart")]
				Ram4Meg,
				[Display(Name = "King of Fighters 95 Rom Cart")]
				Kof95,
				[Display(Name = "Ultraman Rom Cart")]
				Ultraman,
				[Display(Name = "CS1 16 Meg Ram Cart")]
				Ram16Meg
			}

			// extern int setting_ss_cart;
			[DefaultValue(CartType.Autodetect)]
			[Description("What to plug into the Saturn expansion slot")]
			public CartType ExpansionCart { get; set; }

			[DisplayName("Port 1 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.Gamepad)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port1 { get; set; }

			[DisplayName("Port 2 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.Gamepad)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port2 { get; set; }

			[DisplayName("Port 3 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port3 { get; set; }

			[DisplayName("Port 4 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port4 { get; set; }

			[DisplayName("Port 5 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port5 { get; set; }

			[DisplayName("Port 6 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port6 { get; set; }

			[DisplayName("Port 7 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port7 { get; set; }

			[DisplayName("Port 8 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port8 { get; set; }

			[DisplayName("Port 9 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port9 { get; set; }

			[DisplayName("Port 10 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port10 { get; set; }

			[DisplayName("Port 11 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port11 { get; set; }

			[DisplayName("Port 12 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SaturnusControllerDeck.Device Port12 { get; set; }

			[DisplayName("Port 1 Multitap")]
			[Description("If true, Ports 1-6 will be used for multitap")]
			[DefaultValue(false)]
			public bool Port1Multitap { get; set; }
			[DisplayName("Port 2 Multitap")]
			[Description("If true, Ports 2-7 (or 7-12, depending on Port 1 Multitap) will be used for multitap")]
			[DefaultValue(false)]
			public bool Port2Multitap { get; set; }

			// extern bool setting_ss_region_autodetect;
			[DisplayName("Region Autodetect")]
			[Description("Attempt to guess the game region")]
			[DefaultValue(true)]
			public bool RegionAutodetect { get; set; }

			public enum RegionType
			{
				[Display(Name = "Japan")]
				Japan = 1,
				[Display(Name = "Asia NTSC (Taiwan, Phillipines)")]
				Taiwan = 2,
				[Display(Name = "North America")]
				Murka = 4,
				[Display(Name = "Latin America NTSC (Brazil)")]
				Brazil = 5,
				[Display(Name = "South Korea")]
				Korea = 6,
				[Display(Name = "Asia PAL (China, Middle East)")]
				China = 10,
				[Display(Name = "Europe")]
				Yurop = 12,
				[Display(Name = "Latin America PAL")]
				SouthAmerica = 13
			}

			// extern int setting_ss_region_default;
			[DisplayName("Default Region")]
			[Description("Used when Region Autodetect is disabled or fails")]
			[DefaultValue(RegionType.Japan)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public RegionType Region { get; set; }

			public enum LanguageType
			{
				English = 0,
				German = 1,
				French = 2,
				Spanish = 3,
				Italian = 4,
				Japanese = 5,
			}

			[DisplayName("Language")]
			[Description("Language of the system.  Only affects some games in some regions.")]
			[DefaultValue(LanguageType.Japanese)]
			public LanguageType Language { get; set; }

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.  Only relevant when UseRealTime is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime InitialTime { get; set; }

			[DisplayName("Use RealTime")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time.  Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		private Settings _settings;
		private SyncSettings _syncSettings;

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings s)
		{
			var ret = Settings.NeedsReboot(_settings, s);
			_settings = s;


			//todo natt - is this safe? this is now called before every frameadvance
			//(the correct aspect ratio is no longer an option for other reasons)
			//_core.SetVideoParameters(s.CorrectAspectRatio, s.HBlend, s.HOverscan, sls, sle);

			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings s)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, s);
			_syncSettings = s;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private void SetVideoParameters()
		{
			var s = _settings;
			var sls = _isPal ? s.ScanlineStartPal + 16 : s.ScanlineStartNtsc;
			var sle = _isPal ? s.ScanlineEndPal + 16 : s.ScanlineEndNtsc;

			bool correctAspect = true;
			if (_settings.ResolutionMode == Settings.ResolutionModeTypes.PixelPro)
				correctAspect = false;

			_core.SetVideoParameters(correctAspect, _settings.HBlend, _settings.HOverscan, sls, sle);
		}

		#endregion

		#region IStatable

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_activeDisk);
			writer.Write(_prevDiskSignal);
			writer.Write(_nextDiskSignal);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_activeDisk = reader.ReadInt32();
			_prevDiskSignal = reader.ReadBoolean();
			_nextDiskSignal = reader.ReadBoolean();
			// any managed pointers that we sent to the core need to be resent now!
			SetCdCallbacks();

			//todo natt: evaluate the philosophy of this. 
			//i think it's sound: loadstate will replace the last values set by frontend; so this should re-assert them.
			//or we could make sure values from the frontend are stored in a segment designed with the appropriate waterboxing rules, but that seems tricky...
			//anyway, in this case, I did it before frameadvance instead, so that's just as good (probably?)
			//PutSettings(_settings);
		}

		#endregion

		#region Callbacks

		private LibSaturnus.FirmwareSizeCallback _firmwareSizeCallback;
		private LibSaturnus.FirmwareDataCallback _firmwareDataCallback;
		private LibSaturnus.CDTOCCallback _cdTocCallback;
		private LibSaturnus.CDSectorCallback _cdSectorCallback;

		private void InitCallbacks()
		{
			_firmwareSizeCallback = FirmwareSize;
			_firmwareDataCallback = FirmwareData;
			_cdTocCallback = CDTOCCallback;
			_cdSectorCallback = CDSectorCallback;
		}

		private void SetFirmwareCallbacks()
		{
			_core.SetFirmwareCallbacks(_firmwareSizeCallback, _firmwareDataCallback);
		}
		private void SetCdCallbacks()
		{
			_core.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
		}
		private void ClearAllCallbacks()
		{
			_core.SetFirmwareCallbacks(null, null);
			_core.SetCDCallbacks(null, null);
			_core.SetInputCallback(null);
		}

		private string TranslateFirmwareName(string filename)
		{
			switch (filename)
			{
				case "ss.cart.kof95_path":
					return "KOF95";
				case "ss.cart.ultraman_path":
					return "ULTRAMAN";
				case "BIOS_J":
				case "BIOS_A":
					return "J";
				case "BIOS_E":
					_isPal = true;
					return "E";
				case "BIOS_U":
					return "U";
				default:
					throw new InvalidOperationException("Unknown BIOS file");
			}
		}
		private byte[] GetFirmware(string filename)
		{
			return CoreComm.CoreFileProvider.GetFirmware("SAT", TranslateFirmwareName(filename), true);
		}

		private int FirmwareSize(string filename)
		{
			return GetFirmware(filename).Length;
		}
		private void FirmwareData(string filename, IntPtr dest)
		{
			var data = GetFirmware(filename);
			Marshal.Copy(data, 0, dest, data.Length);
		}

		public static void SetupTOC(LibSaturnus.TOC t, DiscTOC tin)
		{
			// everything that's not commented, we're sure about
			t.FirstTrack = tin.FirstRecordedTrackNumber;
			t.LastTrack = tin.LastRecordedTrackNumber;
			t.DiskType = (int)tin.Session1Format;
			for (int i = 0; i < 101; i++)
			{
				t.Tracks[i].Adr = tin.TOCItems[i].Exists ? 1 : 0; // ????
				t.Tracks[i].Lba = tin.TOCItems[i].LBA;
				t.Tracks[i].Control = (int)tin.TOCItems[i].Control;
				t.Tracks[i].Valid = tin.TOCItems[i].Exists ? 1 : 0;
			}
		}

		private void CDTOCCallback(int disk, [In, Out]LibSaturnus.TOC t)
		{
			SetupTOC(t, _disks[disk].TOC);
		}
		private void CDSectorCallback(int disk, int lba, IntPtr dest)
		{
			var buff = new byte[2448];
			_diskReaders[disk].ReadLBA_2448(lba, buff, 0);
			Marshal.Copy(buff, 0, dest, 2448);
			DriveLightOn = true;
		}

		public bool DriveLightEnabled => true;
		public bool DriveLightOn { get; private set; }

		#endregion

		private const int PalFpsNum = 1734687500;
		private const int PalFpsDen = 61 * 455 * 1251;
		private const int NtscFpsNum = 1746818182; // 1746818181.8
		private const int NtscFpsDen = 61 * 455 * 1051;
		public override int VsyncNumerator => _isPal ? PalFpsNum : NtscFpsNum;
		public override int VsyncDenominator => _isPal ? PalFpsDen : NtscFpsDen;

		private int _virtualWidth;
		private int _virtualHeight;
		public override int VirtualWidth => _virtualWidth;
		public override int VirtualHeight => _virtualHeight;

		bool _useResizedBuffer;
		int[] _resizedBuffer;

		public override int[] GetVideoBuffer()
		{
			if (_useResizedBuffer) return _resizedBuffer;
			else return _videoBuffer;
		}

		void AllocResizedBuffer(int width, int height)
		{
			_useResizedBuffer = true;
			if (_resizedBuffer == null || width * height != _resizedBuffer.Length)
				_resizedBuffer = new int[width * height];
		}

		protected override unsafe void FrameAdvancePost()
		{
			//TODO: can we force the videoprovider to add a prescale instead of having to do it in software here?
			//TODO: if not, actually do it in software, instead of relying on the virtual sizes
			//TODO: find a reason why relying on the virtual sizes is actually not good enough?

			//TODO: export VDP2 display area width from emulator and add option to aggressively crop overscan - OR - add that logic to core

			//mednafen, for reference:
			//if (PAL)
			//{
			//	gi->nominal_width = (ShowHOverscan ? 365 : 354);
			//	gi->fb_height = 576;
			//}
			//else
			//{
			//	gi->nominal_width = (ShowHOverscan ? 302 : 292);
			//	gi->fb_height = 480;
			//}
			//gi->nominal_height = LineVisLast + 1 - LineVisFirst;
			//gi->lcm_width = (ShowHOverscan ? 10560 : 10240);
			//gi->lcm_height = (LineVisLast + 1 - LineVisFirst) * 2;
			//if (!CorrectAspect)
			//{
			//	gi->nominal_width = (ShowHOverscan ? 352 : 341);
			//	gi->lcm_width = gi->nominal_width * 2;
			//}

			_useResizedBuffer = false;

			bool isHorz2x = false, isVert2x = false;

			//note: these work with PAL also
			//these are all with CorrectAR.
			//with IncorrectAR, only 352 and 341 will show up
			//that is, 330 is forced to 352 so mednafen's presentation can display it pristine no matter what mode it is
			//(it's mednafen's equivalent of a more debuggish mode, I guess)
			if (BufferWidth == 352) { } //a large basic framebuffer size 
			else if (BufferWidth == 341) { } //a large basic framebuffer size with overscan cropped
			else if (BufferWidth == 330) { } //a small basic framebuffer
			else if (BufferWidth == 320) { } //a small basic framebuffer with overscan cropped
			else isHorz2x = true;

			int slStart = _isPal ? _settings.ScanlineStartPal : _settings.ScanlineStartNtsc;
			int slEnd = _isPal ? _settings.ScanlineEndPal : _settings.ScanlineEndNtsc;
			int slHeight = (slEnd - slStart) + 1;

			if (BufferHeight == slHeight) { }
			else isVert2x = true;

			if (_settings.ResolutionMode == Settings.ResolutionModeTypes.PixelPro)
			{
				//this is the tricky one: need to adapt the framebuffer size
			}

			switch (_settings.ResolutionMode)
			{
				case Settings.ResolutionModeTypes.HardcoreDebug:
					_virtualWidth = BufferWidth;
					_virtualHeight = BufferHeight;
					break;

				case Settings.ResolutionModeTypes.Mednafen:
					//this is mednafen's "correct AR" case.
					//note that this will shrink a width from 330 to 302 (that's the nature of mednafen)
					//that makes the bios saturn orb get stretched vertically
					//note: these are never high resolution (use a 2x window size if you want to see high resolution content)
					if (_isPal)
					{
						_virtualWidth = (_settings.HOverscan ? 365 : 354); 
						_virtualHeight = slHeight;
					}
					else
					{
						_virtualWidth = (_settings.HOverscan ? 302 : 292);
						_virtualHeight = slHeight;
					}
					break;

				case Settings.ResolutionModeTypes.TweakedMednafen:
					//same as mednafen but stretch up
					//base case is 330x254
					if (_isPal)
					{
						//this can be the same as Mednafen mode? we don't shrink down in any case, so it must be OK
						_virtualWidth = (_settings.HOverscan ? 365 : 354);
						_virtualHeight = slHeight;
					}
					else
					{
						//ideally we want a height of 254, but we may have changed the overscan settings
						_virtualWidth = (_settings.HOverscan ? 330 : 320);
						_virtualHeight = BufferHeight * 254 / 240; 
					}
					break;

				case Settings.ResolutionModeTypes.PixelPro:
					//mednafen makes sure we always get 352 or 341 (if overscan cropped)
					//really the only thing we do
					//(that's not the best solution for us [not what psx does], but it will do for now)
					_virtualWidth = BufferWidth * (isHorz2x ? 1 : 2); //not a mistake, we scale to make it bigger if it isn't already
					_virtualHeight = BufferHeight * (isVert2x ? 1 : 2); //not a mistake

					if (isHorz2x && isVert2x) { } //nothing to do
					else if (isHorz2x && !isVert2x)
					{
						//needs double sizing vertically
						AllocResizedBuffer(_virtualWidth, _virtualHeight);
						for (int y = 0; y < BufferHeight; y++)
						{
							Buffer.BlockCopy(_videoBuffer, BufferWidth * y * 4, _resizedBuffer, BufferWidth * (y * 2 + 0) * 4, BufferWidth * 4);
							Buffer.BlockCopy(_videoBuffer, BufferWidth * y * 4, _resizedBuffer, BufferWidth * (y * 2 + 1) * 4, BufferWidth * 4);
						}
					}
					else if (!isHorz2x && isVert2x)
					{
						//needs double sizing horizontally
						AllocResizedBuffer(_virtualWidth, _virtualHeight);
						fixed (int* _pdst = &_resizedBuffer[0])
						{
							fixed (int* _psrc = &_videoBuffer[0])
							{
								int* psrc = _psrc;
								int* pdst = _pdst;
								for (int y = 0; y < BufferHeight; y++)
								{
									for (int x = 0; x < BufferWidth; x++) { *pdst++ = *psrc; *pdst++ = *psrc++; }
								}
							}
						}
					}
					else
					{
						//needs double sizing horizontally and vertically
						AllocResizedBuffer(_virtualWidth, _virtualHeight);
						fixed (int* _pdst = &_resizedBuffer[0])
						{
							fixed (int* _psrc = &_videoBuffer[0])
							{
								int* psrc = _psrc;
								int* pdst = _pdst;
								for (int y = 0; y < BufferHeight; y++)
								{
									for (int x = 0; x < BufferWidth; x++) { *pdst++ = psrc[x]; *pdst++ = psrc[x]; }
									for (int x = 0; x < BufferWidth; x++) { *pdst++ = psrc[x]; *pdst++ = psrc[x]; }
									psrc += BufferWidth;
								}
							}
						}
					}

					BufferWidth = _virtualWidth;
					BufferHeight = _virtualHeight;

					break;
			}

		}
	}
}
