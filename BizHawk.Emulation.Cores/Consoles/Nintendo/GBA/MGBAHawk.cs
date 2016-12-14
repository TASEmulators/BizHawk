using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[CoreAttributes("mGBA", "endrift", true, true, "0.5.0", "https://mgba.io/", false)]
	[ServiceNotApplicable(typeof(IDriveLight), typeof(IRegionable))]
	public class MGBAHawk : IEmulator, IVideoProvider, ISoundProvider, IGBAGPUViewable,
		ISaveRam, IStatable, IInputPollable, ISettable<MGBAHawk.Settings, MGBAHawk.SyncSettings>
	{
		private IntPtr _core;
		private byte[] _saveScratch = new byte[262144];

		[CoreConstructor("GBA")]
		public MGBAHawk(byte[] file, CoreComm comm, SyncSettings syncSettings, Settings settings, bool deterministic, GameInfo game)
		{
			_syncSettings = syncSettings ?? new SyncSettings();
			_settings = settings ?? new Settings();
			DeterministicEmulation = deterministic;

			byte[] bios = comm.CoreFileProvider.GetFirmware("GBA", "Bios", false);
			DeterministicEmulation &= bios != null;

			if (DeterministicEmulation != deterministic)
			{
				throw new InvalidOperationException("A BIOS is required for deterministic recordings!");
			}
			if (!DeterministicEmulation && bios != null && !_syncSettings.RTCUseRealTime && !_syncSettings.SkipBios)
			{
				// in these situations, this core is deterministic even though it wasn't asked to be
				DeterministicEmulation = true;
			}

			if (bios != null && bios.Length != 16384)
			{
				throw new InvalidOperationException("BIOS must be exactly 16384 bytes!");
			}
			var skipBios = !DeterministicEmulation && _syncSettings.SkipBios;

			_core = LibmGBA.BizCreate(bios, file, file.Length, GetOverrideInfo(game), skipBios);
			if (_core == IntPtr.Zero)
			{
				throw new InvalidOperationException("BizCreate() returned NULL!  Bad BIOS? and/or ROM?");
			}
			try
			{
				CreateMemoryDomains(file.Length);
				var ser = new BasicServiceProvider(this);
				ser.Register<IDisassemblable>(new ArmV4Disassembler());
				ser.Register<IMemoryDomains>(MemoryDomains);

				ServiceProvider = ser;
				CoreComm = comm;

				CoreComm.VsyncNum = 262144;
				CoreComm.VsyncDen = 4389;
				CoreComm.NominalWidth = 240;
				CoreComm.NominalHeight = 160;
				PutSettings(_settings);
			}
			catch
			{
				LibmGBA.BizDestroy(_core);
				throw;
			}
		}

		private static LibmGBA.OverrideInfo GetOverrideInfo(GameInfo game)
		{
			if (!game.OptionPresent("mgbaNeedsOverrides"))
			{
				// the gba game db predates the mgba core in bizhawk, but was never used by the mgba core,
				// which had its own handling for overrides
				// to avoid possible regressions, we don't want to be overriding things that we already
				// know work in mgba, so unless this parameter is set, we do nothing
				return null;
			}

			var ret = new LibmGBA.OverrideInfo();
			if (game.OptionPresent("flashSize"))
			{
				switch (game.GetIntValue("flashSize"))
				{
					case 65536: ret.Savetype = LibmGBA.SaveType.Flash512; break;
					case 131072: ret.Savetype = LibmGBA.SaveType.Flash1m; break;
					default: throw new InvalidOperationException("Unknown flashSize");
				}
			}
			else if (game.OptionPresent("saveType"))
			{
				switch (game.GetIntValue("saveType"))
				{
					// 3 specifies either flash 512 or 1024, but in vba-over.ini, the latter will have a flashSize as well
					case 3: ret.Savetype = LibmGBA.SaveType.Flash512; break;
					case 4: ret.Savetype = LibmGBA.SaveType.Eeprom; break;
					default: throw new InvalidOperationException("Unknown saveType");
				}
			}

			if (game.GetInt("rtcEnabled", 0) == 1)
			{
				ret.Hardware |= LibmGBA.Hardware.Rtc;
			}
			if (game.GetInt("mirroringEnabled", 0) == 1)
			{
				throw new InvalidOperationException("Don't know what to do with mirroringEnabled!");
			}
			if (game.OptionPresent("idleLoop"))
			{
				ret.IdleLoop = (uint)game.GetHexValue("idleLoop");
			}
			return ret;
		}

		MemoryDomainList MemoryDomains;

		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public ControllerDefinition ControllerDefinition { get { return GBA.GBAController; } }
		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;
			if (Controller.IsPressed("Power"))
			{
				LibmGBA.BizReset(_core);
				//BizReset caused memorydomain pointers to change.
				WireMemoryDomainPointers();
			}

			IsLagFrame = LibmGBA.BizAdvance(_core, VBANext.GetButtons(Controller), videobuff, ref nsamp, soundbuff,
				RTCTime(),
				(short)Controller.GetFloat("Tilt X"),
				(short)Controller.GetFloat("Tilt Y"),
				(short)Controller.GetFloat("Tilt Z"),
				(byte)(255 - Controller.GetFloat("Light Sensor")));

			if (IsLagFrame)
				LagCount++;
			// this should be called in hblank on the appropriate line, but until we implement that, just do it here
			if (_scanlinecb != null)
				_scanlinecb();
		}

		public int Frame { get; private set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get; private set; }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (_core != IntPtr.Zero)
			{
				LibmGBA.BizDestroy(_core);
				_core = IntPtr.Zero;
			}
		}

		#region IVideoProvider
		public int VirtualWidth { get { return 240; } }
		public int VirtualHeight { get { return 160; } }
		public int BufferWidth { get { return 240; } }
		public int BufferHeight { get { return 160; } }
		public int BackgroundColor
		{
			get { return unchecked((int)0xff000000); }
		}
		public int[] GetVideoBuffer()
		{
			return videobuff;
		}
		private readonly int[] videobuff = new int[240 * 160];
		#endregion

		#region ISoundProvider

		private readonly short[] soundbuff = new short[2048];
		private int nsamp;
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = this.nsamp;
			samples = soundbuff;
			DiscardSamples();
		}
		public void DiscardSamples()
		{
			nsamp = 0;
		}

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		#endregion

		#region IMemoryDomains

		unsafe byte PeekWRAM(IntPtr xwram, long addr) { return ((byte*)xwram)[addr];}
		unsafe void PokeWRAM(IntPtr xwram, long addr, byte value) { ((byte*)xwram)[addr] = value; }

		void WireMemoryDomainPointers()
		{
			var s = new LibmGBA.MemoryAreas();
			LibmGBA.BizGetMemoryAreas(_core, s);

			_iwram.Data = s.iwram;
			_ewram.Data = s.wram;
			_bios.Data = s.bios;
			_palram.Data = s.palram;
			_vram.Data = s.vram;
			_oam.Data = s.oam;
			_rom.Data = s.rom;
			_sram.Data = s.sram;
			_sram.SetSize(s.sram_size);

			// special combined ram memory domain

			_cwram.Peek =
				delegate(long addr)
				{
					if (addr < 0 || addr >= (256 + 32) * 1024)
						throw new IndexOutOfRangeException();
					if (addr >= 256 * 1024)
						return PeekWRAM(s.iwram, addr & 32767);
					else
						return PeekWRAM(s.wram, addr);
				};
			_cwram.Poke =
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= (256 + 32) * 1024)
						throw new IndexOutOfRangeException();
					if (addr >= 256 * 1024)
						PokeWRAM(s.iwram, addr & 32767, val);
					else
						PokeWRAM(s.wram, addr, val);
				};

			_gpumem = new GBAGPUMemoryAreas
			{
				mmio = s.mmio,
				oam = s.oam,
				palram = s.palram,
				vram = s.vram
			};
		}

		private MemoryDomainIntPtr _iwram;
		private MemoryDomainIntPtr _ewram;
		private MemoryDomainIntPtr _bios;
		private MemoryDomainIntPtr _palram;
		private MemoryDomainIntPtr _vram;
		private MemoryDomainIntPtr _oam;
		private MemoryDomainIntPtr _rom;
		private MemoryDomainIntPtr _sram;
		private MemoryDomainDelegate _cwram;

		private void CreateMemoryDomains(int romsize)
		{
			var LE = MemoryDomain.Endian.Little;

			var mm = new List<MemoryDomain>();
			mm.Add(_iwram = new MemoryDomainIntPtr("IWRAM", LE, IntPtr.Zero, 32 * 1024, true, 4));
			mm.Add(_ewram = new MemoryDomainIntPtr("EWRAM", LE, IntPtr.Zero, 256 * 1024, true, 4));
			mm.Add(_bios = new MemoryDomainIntPtr("BIOS", LE, IntPtr.Zero, 16 * 1024, false, 4));
			mm.Add(_palram = new MemoryDomainIntPtr("PALRAM", LE, IntPtr.Zero, 1024, true, 4));
			mm.Add(_vram = new MemoryDomainIntPtr("VRAM", LE, IntPtr.Zero, 96 * 1024, true, 4));
			mm.Add(_oam = new MemoryDomainIntPtr("OAM", LE, IntPtr.Zero, 1024, true, 4));
			mm.Add(_rom = new MemoryDomainIntPtr("ROM", LE, IntPtr.Zero, romsize, false, 4));
			mm.Add(_sram = new MemoryDomainIntPtr("SRAM", LE, IntPtr.Zero, 0, true, 4)); //size will be fixed in wireup
			mm.Add(_cwram = new MemoryDomainDelegate("Combined WRAM", (256 + 32) * 1024, LE, null, null, 4));

			MemoryDomains = new MemoryDomainList(mm);
			WireMemoryDomainPointers();
		}

		#endregion

		private Action _scanlinecb;

		private GBAGPUMemoryAreas _gpumem;

		public GBAGPUMemoryAreas GetMemoryAreas()
		{
			return _gpumem;
		}

		[FeatureNotImplemented]
		public void SetScanlineCallback(Action callback, int scanline)
		{
			_scanlinecb = callback;
		}

		#region ISaveRam

		public byte[] CloneSaveRam()
		{
			int len = LibmGBA.BizGetSaveRam(_core, _saveScratch, _saveScratch.Length);
			if (len == _saveScratch.Length)
				throw new InvalidOperationException("Save buffer not long enough");
			if (len == 0)
				return null;

			var ret = new byte[len];
			Array.Copy(_saveScratch, ret, len);
			return ret;
		}

		private static byte[] LegacyFix(byte[] saveram)
		{
			// at one point vbanext-hawk had a special saveram format which we want to load.
			var br = new BinaryReader(new MemoryStream(saveram, false));
			br.ReadBytes(8); // header;
			int flashSize = br.ReadInt32();
			int eepromsize = br.ReadInt32();
			byte[] flash = br.ReadBytes(flashSize);
			byte[] eeprom = br.ReadBytes(eepromsize);
			if (flash.Length == 0)
				return eeprom;
			else if (eeprom.Length == 0)
				return flash;
			else
			{
				// well, isn't this a sticky situation!
				return flash; // woops
			}
		}

		public void StoreSaveRam(byte[] data)
		{
			if (data.Take(8).SequenceEqual(Encoding.ASCII.GetBytes("GBABATT\0")))
			{
				data = LegacyFix(data);
			}
			LibmGBA.BizPutSaveRam(_core, data, data.Length);
		}

		public bool SaveRamModified
		{
			get
			{
				return LibmGBA.BizGetSaveRam(_core, _saveScratch, _saveScratch.Length) > 0;
			}
		}

		#endregion

		private byte[] _savebuff = new byte[0];
		private byte[] _savebuff2 = new byte[13];

		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		public void SaveStateText(TextWriter writer)
		{
			var tmp = SaveStateBinary();
			BizHawk.Common.BufferExtensions.BufferExtensions.SaveAsHexFast(tmp, writer);
		}
		public void LoadStateText(TextReader reader)
		{
			string hex = reader.ReadLine();
			byte[] state = new byte[hex.Length / 2];
			BizHawk.Common.BufferExtensions.BufferExtensions.ReadFromHexFast(state, hex);
			LoadStateBinary(new BinaryReader(new MemoryStream(state)));
		}

		private void StartSaveStateBinaryInternal()
		{
			IntPtr p = IntPtr.Zero;
			int size = 0;
			if (!LibmGBA.BizStartGetState(_core, ref p, ref size))
				throw new InvalidOperationException("Core failed to save!");
			if (size != _savebuff.Length)
			{
				_savebuff = new byte[size];
				_savebuff2 = new byte[size + 13];
			}
			LibmGBA.BizFinishGetState(p, _savebuff, size);
		}

		private void FinishSaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_savebuff.Length);
			writer.Write(_savebuff, 0, _savebuff.Length);

			// other variables
			writer.Write(IsLagFrame);
			writer.Write(LagCount);
			writer.Write(Frame);
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			StartSaveStateBinaryInternal();
			FinishSaveStateBinaryInternal(writer);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			int length = reader.ReadInt32();
			if (length != _savebuff.Length)
			{
				_savebuff = new byte[length];
				_savebuff2 = new byte[length + 13];
			}
			reader.Read(_savebuff, 0, length);
			if (!LibmGBA.BizPutState(_core, _savebuff, length))
				throw new InvalidOperationException("Core rejected the savestate!");

			// other variables
			IsLagFrame = reader.ReadBoolean();
			LagCount = reader.ReadInt32();
			Frame = reader.ReadInt32();
		}

		public byte[] SaveStateBinary()
		{
			StartSaveStateBinaryInternal();
			var ms = new MemoryStream(_savebuff2, true);
			var bw = new BinaryWriter(ms);
			FinishSaveStateBinaryInternal(bw);
			bw.Flush();
			ms.Close();
			return _savebuff2;
		}

		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		[FeatureNotImplemented]
		public IInputCallbackSystem InputCallbacks
		{
			get { throw new NotImplementedException(); }
		}

		private long RTCTime()
		{
			if (!DeterministicEmulation && _syncSettings.RTCUseRealTime)
			{
				return (long)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			}
			long basetime = (long)_syncSettings.RTCInitialTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			long increment = Frame * 4389L >> 18;
			return basetime + increment;
		}

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public bool PutSettings(Settings o)
		{
			LibmGBA.Layers mask = 0;
			if (o.DisplayBG0) mask |= LibmGBA.Layers.BG0;
			if (o.DisplayBG1) mask |= LibmGBA.Layers.BG1;
			if (o.DisplayBG2) mask |= LibmGBA.Layers.BG2;
			if (o.DisplayBG3) mask |= LibmGBA.Layers.BG3;
			if (o.DisplayOBJ) mask |= LibmGBA.Layers.OBJ;
			LibmGBA.BizSetLayerMask(_core, mask);

			LibmGBA.Sounds smask = 0;
			if (o.PlayCh0) smask |= LibmGBA.Sounds.CH0;
			if (o.PlayCh1) smask |= LibmGBA.Sounds.CH1;
			if (o.PlayCh2) smask |= LibmGBA.Sounds.CH2;
			if (o.PlayCh3) smask |= LibmGBA.Sounds.CH3;
			if (o.PlayChA) smask |= LibmGBA.Sounds.CHA;
			if (o.PlayChB) smask |= LibmGBA.Sounds.CHB;
			LibmGBA.BizSetSoundMask(_core, smask);

			_settings = o;
			return false;
		}

		private Settings _settings;

		public class Settings
		{
			[DisplayName("Display BG Layer 0")]
			[DefaultValue(true)]
			public bool DisplayBG0 { get; set; }
			[DisplayName("Display BG Layer 1")]
			[DefaultValue(true)]
			public bool DisplayBG1 { get; set; }
			[DisplayName("Display BG Layer 2")]
			[DefaultValue(true)]
			public bool DisplayBG2 { get; set; }
			[DisplayName("Display BG Layer 3")]
			[DefaultValue(true)]
			public bool DisplayBG3 { get; set; }
			[DisplayName("Display Sprite Layer")]
			[DefaultValue(true)]
			public bool DisplayOBJ { get; set; }

			[DisplayName("Play Square 1")]
			[DefaultValue(true)]
			public bool PlayCh0 { get; set; }
			[DisplayName("Play Square 2")]
			[DefaultValue(true)]
			public bool PlayCh1 { get; set; }
			[DisplayName("Play Wave")]
			[DefaultValue(true)]
			public bool PlayCh2 { get; set; }
			[DisplayName("Play Noise")]
			[DefaultValue(true)]
			public bool PlayCh3 { get; set; }
			[DisplayName("Play Direct Sound A")]
			[DefaultValue(true)]
			public bool PlayChA { get; set; }
			[DisplayName("Play Direct Sound B")]
			[DefaultValue(true)]
			public bool PlayChB { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret;
		}

		private SyncSettings _syncSettings;

		public class SyncSettings
		{
			[DisplayName("Skip BIOS")]
			[Description("Skips the BIOS intro.  Not applicable when a BIOS is not provided.")]
			[DefaultValue(true)]
			public bool SkipBios { get; set; }

			[DisplayName("RTC Use Real Time")]
			[Description("Causes the internal clock to reflect your system clock.  Only relevant when a game has an RTC chip.  Forced to false for movie recording.")]
			[DefaultValue(true)]
			public bool RTCUseRealTime { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("The initial time of emulation.  Only relevant when a game has an RTC chip and \"RTC Use Real Time\" is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			public DateTime RTCInitialTime { get; set; }

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}
		}
	}
}
