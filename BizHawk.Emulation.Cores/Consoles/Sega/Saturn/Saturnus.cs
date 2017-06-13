using BizHawk.Common;
using BizHawk.Common.BizInvoke;
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
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	[CoreAttributes("Saturnus", "Ryphecha", true, false, "0.9.44.1",
		"https://mednafen.github.io/releases/", false)]
	public class Saturnus : IEmulator, IVideoProvider, ISoundProvider,
		IInputPollable, IDriveLight, IStatable, IRegionable, ISaveRam,
		ISettable<Saturnus.Settings, Saturnus.SyncSettings>
	{
		private static readonly DiscSectorReaderPolicy _diskPolicy = new DiscSectorReaderPolicy
		{
			DeinterleavedSubcode = false
		};

		private PeRunner _exe;
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

				using (var sha256 = SHA256.Create())
				{
					sha256.ComputeHash(buff, 0x100, 0xd00);
					if (sha256.Hash.BytesToHexString() != "96B8EA48819CFA589F24C40AA149C224C420DCCF38B730F00156EFE25C9BBC8F")
						return false;
				}
			}
			return true;
		}

		public Saturnus(CoreComm comm, IEnumerable<Disc> disks, bool deterministic, Settings settings,
			SyncSettings syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
			settings = settings ?? new Settings();
			syncSettings = syncSettings ?? new SyncSettings();

			_disks = disks.ToArray();
			_diskReaders = disks.Select(d => new DiscSectorReader(d) { Policy = _diskPolicy }).ToArray();
			if (!CheckDisks(_diskReaders))
				throw new InvalidOperationException("Some disks are not valid");
			InitCallbacks();

			_exe = new PeRunner(new PeRunnerOptions
			{
				Path = comm.CoreFileProvider.DllPath(),
				Filename = "ss.wbx",
				SbrkHeapSizeKB = 128,
				SealedHeapSizeKB = 4096, // 512KB of bios, 2MB of kof95/ultraman carts
				InvisibleHeapSizeKB = 8 * 1024, // 4MB of framebuffer
				MmapHeapSizeKB = 0, // not used?
				PlainHeapSizeKB = 24 * 1024, // up to 16MB of cart ram
				StartAddress = LibSaturnus.StartAddress
			});
			_core = BizInvoker.GetInvoker<LibSaturnus>(_exe, _exe);

			SetFirmwareCallbacks();
			SetCdCallbacks();
			_core.SetAddMemoryDomainCallback(_addMemoryDomainCallback);
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

			_exe.Seal();
			SetCdCallbacks();
			_core.SetDisk(0, false);
			(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(
				new MemoryDomainList(_memoryDomains.Values.Cast<MemoryDomain>().ToList())
				{
					MainMemory = _memoryDomains["Work Ram Low"]
				});
			PutSettings(settings);
			_syncSettings = syncSettings;
			DeterministicEmulation = deterministic || !_syncSettings.UseRealTime;
		}

		public unsafe void FrameAdvance(IController controller, bool render, bool rendersound = true)
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
			SetInputCallback();

			fixed (short* _sp = _soundBuffer)
			fixed (int* _vp = _videoBuffer)
			fixed (byte* _cp = _controllerDeck.Poll(controller))
			{
				var info = new LibSaturnus.FrameAdvanceInfo
				{
					SoundBuf = (IntPtr)_sp,
					SoundBufMaxSize = _soundBuffer.Length / 2,
					Pixels = (IntPtr)_vp,
					Controllers = (IntPtr)_cp,
					ResetPushed = (short)(controller.IsPressed("Reset") ? 1 : 0)
				};

				_core.FrameAdvance(info);
				Frame++;
				IsLagFrame = info.InputLagged != 0;
				if (IsLagFrame)
					LagCount++;
				_numSamples = info.SoundBufSize;
				BufferWidth = info.Width;
				BufferHeight = info.Height;
			}
		}

		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
			{
				_exe.Dispose();
				_exe = null;
				_core = null;
				_disposed = true;
			}
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		public void ResetCounters()
		{
			Frame = 0;
		}

		public DisplayType Region => _isPal ? DisplayType.PAL : DisplayType.NTSC;
		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public string SystemId { get { return "SAT"; } }
		public bool DeterministicEmulation { get; private set; }
		public CoreComm CoreComm { get; }
		public ControllerDefinition ControllerDefinition { get; }

		#region ISettable

		public class Settings
		{
			// extern bool setting_ss_correct_aspect;
			[DefaultValue(true)]
			public bool CorrectAspectRatio { get; set; }

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
			public SaturnusControllerDeck.Device Port1 { get; set; }
			[DisplayName("Port 2 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.Gamepad)]
			public SaturnusControllerDeck.Device Port2 { get; set; }
			[DisplayName("Port 3 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			public SaturnusControllerDeck.Device Port3 { get; set; }
			[DisplayName("Port 4 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			public SaturnusControllerDeck.Device Port4 { get; set; }
			[DisplayName("Port 5 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			public SaturnusControllerDeck.Device Port5 { get; set; }
			[DisplayName("Port 6 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			public SaturnusControllerDeck.Device Port6 { get; set; }
			[DisplayName("Port 7 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if one or both multitaps are set")]
			public SaturnusControllerDeck.Device Port7 { get; set; }
			[DisplayName("Port 8 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			public SaturnusControllerDeck.Device Port8 { get; set; }
			[DisplayName("Port 9 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			public SaturnusControllerDeck.Device Port9 { get; set; }
			[DisplayName("Port 10 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			public SaturnusControllerDeck.Device Port10 { get; set; }
			[DisplayName("Port 11 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
			public SaturnusControllerDeck.Device Port11 { get; set; }
			[DisplayName("Port 12 Device")]
			[DefaultValue(SaturnusControllerDeck.Device.None)]
			[Description("Only used if both multitaps are set")]
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

		public bool PutSettings(Settings s)
		{
			var ret = Settings.NeedsReboot(_settings, s);
			_settings = s;
			var sls = _isPal ? s.ScanlineStartPal + 16 : s.ScanlineStartNtsc;
			var sle = _isPal ? s.ScanlineEndPal + 16 : s.ScanlineEndNtsc;
			_core.SetVideoParameters(s.CorrectAspectRatio, s.HBlend, s.HOverscan, sls, sle);
			return ret;
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSyncSettings(SyncSettings s)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, s);
			_syncSettings = s;
			return ret;
		}

		#endregion

		#region IMemoryDomains

		private readonly Dictionary<string, MemoryDomainIntPtrMonitor> _memoryDomains = new Dictionary<string, MemoryDomainIntPtrMonitor>();

		private void AddMemoryDomain(string name, IntPtr ptr, int size, bool writable)
		{
			_memoryDomains.Add(name, new MemoryDomainIntPtrMonitor(name, MemoryDomain.Endian.Big, ptr, size, writable, 2, _exe));
		}

		#endregion

		#region IStatable

		public bool BinarySaveStatesPreferred => true;

		public void SaveStateText(TextWriter writer)
		{
			var temp = SaveStateBinary();
			temp.SaveAsHexFast(writer);
			// write extra copy of stuff we don't use
			writer.WriteLine("Frame {0}", Frame);
		}

		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			state.ReadFromHexFast(hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			_exe.LoadStateBinary(reader);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			_activeDisk = reader.ReadInt32();
			_prevDiskSignal = reader.ReadBoolean();
			_nextDiskSignal = reader.ReadBoolean();
			// any managed pointers that we sent to the core need to be resent now!
			SetCdCallbacks();
			_core.SetInputCallback(null);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_exe.SaveStateBinary(writer);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
			writer.Write(_activeDisk);
			writer.Write(_prevDiskSignal);
			writer.Write(_nextDiskSignal);
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			ms.Close();
			return ms.ToArray();
		}

		#endregion

		#region Callbacks

		private LibSaturnus.FirmwareSizeCallback _firmwareSizeCallback;
		private LibSaturnus.FirmwareDataCallback _firmwareDataCallback;
		private LibSaturnus.CDTOCCallback _cdTocCallback;
		private LibSaturnus.CDSectorCallback _cdSectorCallback;
		private LibSaturnus.InputCallback _inputCallback;
		private LibSaturnus.AddMemoryDomainCallback _addMemoryDomainCallback;

		private void InitCallbacks()
		{
			_firmwareSizeCallback = FirmwareSize;
			_firmwareDataCallback = FirmwareData;
			_cdTocCallback = CDTOCCallback;
			_cdSectorCallback = CDSectorCallback;
			_inputCallback = InputCallbacks.Call;
			_addMemoryDomainCallback = AddMemoryDomain;
		}

		private void SetFirmwareCallbacks()
		{
			_core.SetFirmwareCallbacks(_firmwareSizeCallback, _firmwareDataCallback);
		}
		private void SetCdCallbacks()
		{
			_core.SetCDCallbacks(_cdTocCallback, _cdSectorCallback);
		}
		private void SetInputCallback()
		{
			_core.SetInputCallback(InputCallbacks.Count > 0 ? _inputCallback : null);
		}
		private void ClearAllCallbacks()
		{
			_core.SetFirmwareCallbacks(null, null);
			_core.SetCDCallbacks(null, null);
			_core.SetInputCallback(null);
			_core.SetAddMemoryDomainCallback(null);
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

		private void CDTOCCallback(int disk, [In, Out]LibSaturnus.TOC t)
		{
			// everything that's not commented, we're sure about
			var tin = _disks[disk].TOC;
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

		#region IVideoProvider

		private int[] _videoBuffer = new int[1024 * 1024];

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		private const int PalFpsNum = 1734687500;
		private const int PalFpsDen = 61 * 455 * 1251;
		private const int NtscFpsNum = 1746818182; // 1746818181.8
		private const int NtscFpsDen = 61 * 455 * 1051;

		public int VirtualWidth => BufferWidth; // TODO
		public int VirtualHeight => BufferHeight; // TODO
		public int BufferWidth { get; private set; } = 320;
		public int BufferHeight { get; private set; } = 240;
		public int VsyncNumerator => _isPal ? PalFpsNum : NtscFpsNum;
		public int VsyncDenominator => _isPal ? PalFpsDen : NtscFpsDen;
		public int BackgroundColor => unchecked((int)0xff000000);

		#endregion

		#region ISoundProvider

		private short[] _soundBuffer = new short[16384];
		private int _numSamples;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundBuffer;
			nsamp = _numSamples;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
		}

		public bool CanProvideAsync => false;
		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		#endregion

		#region ISaveRam

		private static readonly string[] SaveRamDomains = new[] { "Backup Ram", "Backup Cart" };

		private int SaveRamSize()
		{
			return ActiveSaveRamDomains()
				.Select(m => (int)m.Size)
				.Sum();
		}
		private IEnumerable<MemoryDomainIntPtrMonitor> ActiveSaveRamDomains()
		{
			return SaveRamDomains.Where(_memoryDomains.ContainsKey)
				.Select(s => _memoryDomains[s]);
		}

		public byte[] CloneSaveRam()
		{
			var ret = new byte[SaveRamSize()];
			var offs = 0;
			using (_exe.EnterExit())
			{
				foreach (var m in ActiveSaveRamDomains())
				{
					Marshal.Copy(m.Data, ret, offs, (int)m.Size);
					offs += (int)m.Size;
				}
			}
			return ret;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Length != SaveRamSize())
				throw new InvalidOperationException("Saveram was the wrong size!");
			var offs = 0;
			using (_exe.EnterExit())
			{
				foreach (var m in ActiveSaveRamDomains())
				{
					Marshal.Copy(data, offs, m.Data, (int)m.Size);
					offs += (int)m.Size;
				}
			}
		}

		public bool SaveRamModified => true;

		#endregion
	}
}
