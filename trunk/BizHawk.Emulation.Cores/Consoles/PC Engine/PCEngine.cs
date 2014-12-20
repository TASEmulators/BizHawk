using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components;

using BizHawk.Emulation.Cores.Components.H6280;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public enum NecSystemType { TurboGrafx, TurboCD, SuperGrafx }

	[CoreAttributes(
		"PCEHawk",
		"Vecna",
		isPorted: false,
		isReleased: true
		)]
	public sealed partial class PCEngine : IEmulator, IMemoryDomains, ISaveRam, IStatable, IInputPollable,
		IDebuggable, ISettable<PCEngine.PCESettings, PCEngine.PCESyncSettings>, IDriveLight
	{
		// ROM
		public byte[] RomData;
		public int RomLength;
		Disc disc;

		// Machine
		public NecSystemType Type;
		public HuC6280 Cpu;
		public VDC VDC1, VDC2;
		public VCE VCE;
		public VPC VPC;
		public ScsiCDBus SCSI;
		public ADPCM ADPCM;

		public HuC6280PSG PSG;
		public CDAudio CDAudio;
		public SoundMixer SoundMixer;
		public MetaspuSoundProvider SoundSynchronizer;

		bool TurboGrafx { get { return Type == NecSystemType.TurboGrafx; } }
		bool SuperGrafx { get { return Type == NecSystemType.SuperGrafx; } }
		bool TurboCD { get { return Type == NecSystemType.TurboCD; } }

		// BRAM
		bool BramEnabled = false;
		bool BramLocked = true;
		byte[] BRAM;

		// Memory system
		public byte[] Ram;       // PCE= 8K base ram, SGX= 64k base ram
		public byte[] CDRam;     // TurboCD extra 64k of ram
		public byte[] SuperRam;  // Super System Card 192K of additional RAM
		public byte[] ArcadeRam; // Arcade Card 2048K of additional RAM

		string systemid = "PCE";
		bool ForceSpriteLimit;

		// 21,477,270  Machine clocks / sec
		//  7,159,090  Cpu cycles / sec

		[CoreConstructor("PCE", "SGX")]
		public PCEngine(CoreComm comm, GameInfo game, byte[] rom, object Settings, object syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			Tracer = new TraceBuffer();
			MemoryCallbacks = new MemoryCallbackSystem();
			CoreComm = comm;

			switch (game.System)
			{
				case "PCE":
					systemid = "PCE";
					Type = NecSystemType.TurboGrafx;
					break;
				case "SGX":
					systemid = "SGX";
					Type = NecSystemType.SuperGrafx;
					break;
			}
			this._settings = (PCESettings)Settings ?? new PCESettings();
			_syncSettings = (PCESyncSettings)syncSettings ?? new PCESyncSettings();
			Init(game, rom);
			SetControllerButtons();
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public string BoardName { get { return null; } }

		public ITracer Tracer { get; private set; }
		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		public PCEngine(CoreComm comm, GameInfo game, Disc disc, object Settings, object syncSettings)
		{
			CoreComm = comm;
			ServiceProvider = new BasicServiceProvider(this);
			Tracer = new TraceBuffer();
			MemoryCallbacks = new MemoryCallbackSystem();
			DriveLightEnabled = true;
			systemid = "PCECD";
			Type = NecSystemType.TurboCD;
			this.disc = disc;
			this._settings = (PCESettings)Settings ?? new PCESettings();
			_syncSettings = (PCESyncSettings)syncSettings ?? new PCESyncSettings();

			GameInfo biosInfo;
			byte[] rom = CoreComm.CoreFileProvider.GetFirmwareWithGameInfo("PCECD", "Bios", true, out biosInfo,
				"PCE-CD System Card not found. Please check the BIOS settings in Config->Firmwares.");

			if (biosInfo.Status == RomStatus.BadDump)
			{
				CoreComm.ShowMessage(
					"The PCE-CD System Card you have selected is known to be a bad dump. This may cause problems playing PCE-CD games.\n\n"
					+ "It is recommended that you find a good dump of the system card. Sorry to be the bearer of bad news!");
			}
			else if (biosInfo.NotInDatabase)
			{
				CoreComm.ShowMessage(
					"The PCE-CD System Card you have selected is not recognized in our database. That might mean it's a bad dump, or isn't the correct rom.");
			}
			else if (biosInfo["BIOS"] == false)
			{
				//zeromus says: someone please write a note about how this could possibly happen.
				//it seems like this is a relic of using gameDB for storing whether something is a bios? firmwareDB should be handling it now.
				CoreComm.ShowMessage(
					"The PCE-CD System Card you have selected is not a BIOS image. You may have selected the wrong rom. FYI-Please report this to developers, I don't think this error message should happen.");
			}

			if (biosInfo["SuperSysCard"])
			{
				game.AddOption("SuperSysCard");
			}

			if (game["NeedSuperSysCard"] && game["SuperSysCard"] == false)
			{
				CoreComm.ShowMessage(
					"This game requires a version 3.0 System card and won't run with the system card you've selected. Try selecting a 3.0 System Card in the firmware configuration.");
				throw new Exception();
			}

			game.FirmwareHash = rom.HashSHA1();

			Init(game, rom);
			// the default RomStatusDetails don't do anything with Disc
			CoreComm.RomStatusDetails = string.Format("{0}\r\nDisk partial hash:{1}", game.Name, disc.GetHash());
			SetControllerButtons();
		}

		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; internal set; }

		void Init(GameInfo game, byte[] rom)
		{
			Controller = NullController.GetNullController();
			Cpu = new HuC6280(this);
			VCE = new VCE();
			VDC1 = new VDC(this, Cpu, VCE);
			PSG = new HuC6280PSG();
			SCSI = new ScsiCDBus(this, disc);

			Cpu.Logger = (s) => Tracer.Put(s);

			if (TurboGrafx)
			{
				Ram = new byte[0x2000];
				Cpu.ReadMemory21 = ReadMemory;
				Cpu.WriteMemory21 = WriteMemory;
				Cpu.WriteVDC = VDC1.WriteVDC;
				soundProvider = PSG;
				CDAudio = new CDAudio(null, 0);
			}

			else if (SuperGrafx)
			{
				VDC2 = new VDC(this, Cpu, VCE);
				VPC = new VPC(this, VDC1, VDC2, VCE, Cpu);
				Ram = new byte[0x8000];
				Cpu.ReadMemory21 = ReadMemorySGX;
				Cpu.WriteMemory21 = WriteMemorySGX;
				Cpu.WriteVDC = VDC1.WriteVDC;
				soundProvider = PSG;
				CDAudio = new CDAudio(null, 0);
			}

			else if (TurboCD)
			{
				Ram = new byte[0x2000];
				CDRam = new byte[0x10000];
				ADPCM = new ADPCM(this, SCSI);
				Cpu.ReadMemory21 = ReadMemoryCD;
				Cpu.WriteMemory21 = WriteMemoryCD;
				Cpu.WriteVDC = VDC1.WriteVDC;
				CDAudio = new CDAudio(disc);
				SetCDAudioCallback();
				PSG.MaxVolume = short.MaxValue * 3 / 4;
				SoundMixer = new SoundMixer(PSG, CDAudio, ADPCM);
				SoundSynchronizer = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);
				soundProvider = SoundSynchronizer;
				Cpu.ThinkAction = (cycles) => { SCSI.Think(); ADPCM.Think(cycles); };
			}

			if (rom.Length == 0x60000)
			{
				// 384k roms require special loading code. Why ;_;
				// In memory, 384k roms look like [1st 256k][Then full 384k]
				RomData = new byte[0xA0000];
				var origRom = rom;
				for (int i = 0; i < 0x40000; i++)
					RomData[i] = origRom[i];
				for (int i = 0; i < 0x60000; i++)
					RomData[i + 0x40000] = origRom[i];
				RomLength = RomData.Length;
			}
			else if (rom.Length > 1024 * 1024)
			{
				// If the rom is bigger than 1 megabyte, switch to Street Fighter 2 mapper
				Cpu.ReadMemory21 = ReadMemorySF2;
				Cpu.WriteMemory21 = WriteMemorySF2;
				RomData = rom;
				RomLength = RomData.Length;
				// user request: current value of the SF2MapperLatch on the tracelogger
				Cpu.Logger = (s) => Tracer.Put(string.Format("{0:X1}:{1}", SF2MapperLatch, s));
			}
			else
			{
				// normal rom.
				RomData = rom;
				RomLength = RomData.Length;
			}

			if (game["BRAM"] || Type == NecSystemType.TurboCD)
			{
				BramEnabled = true;
				BRAM = new byte[2048];

				// pre-format BRAM. damn are we helpful.
				BRAM[0] = 0x48; BRAM[1] = 0x55; BRAM[2] = 0x42; BRAM[3] = 0x4D;
				BRAM[4] = 0x00; BRAM[5] = 0x88; BRAM[6] = 0x10; BRAM[7] = 0x80;
			}

			if (game["SuperSysCard"])
				SuperRam = new byte[0x30000];

			if (game["ArcadeCard"])
			{
				ArcadeRam = new byte[0x200000];
				ArcadeCard = true;
				ArcadeCardRewindHack = _settings.ArcadeCardRewindHack;
				for (int i = 0; i < 4; i++)
					ArcadePage[i] = new ArcadeCardPage();
			}

			if (game["PopulousSRAM"])
			{
				PopulousRAM = new byte[0x8000];
				Cpu.ReadMemory21 = ReadMemoryPopulous;
				Cpu.WriteMemory21 = WriteMemoryPopulous;
			}

			// the gamedb can force sprite limit on, ignoring settings
			if (game["ForceSpriteLimit"] || game.NotInDatabase)
				ForceSpriteLimit = true;

			if (game["CdVol"])
				CDAudio.MaxVolume = int.Parse(game.OptionValue("CdVol"));
			if (game["PsgVol"])
				PSG.MaxVolume = int.Parse(game.OptionValue("PsgVol"));
			if (game["AdpcmVol"])
				ADPCM.MaxVolume = int.Parse(game.OptionValue("AdpcmVol"));
			// the gamedb can also force equalizevolumes on
			if (TurboCD && (_settings.EqualizeVolume || game["EqualizeVolumes"] || game.NotInDatabase))
				SoundMixer.EqualizeVolumes();

			// Ok, yes, HBlankPeriod's only purpose is game-specific hax.
			// 1) At least they're not coded directly into the emulator, but instead data-driven.
			// 2) The games which have custom HBlankPeriods work without it, the override only
			//    serves to clean up minor gfx anomalies.
			// 3) There's no point in haxing the timing with incorrect values in an attempt to avoid this.
			//    The proper fix is cycle-accurate/bus-accurate timing. That isn't coming to the C# 
			//    version of this core. Let's just acknolwedge that the timing is imperfect and fix
			//    it in the least intrusive and most honest way we can.

			if (game["HBlankPeriod"])
				VDC1.HBlankCycles = game.GetIntValue("HBlankPeriod");

			// This is also a hack. Proper multi-res/TV emulation will be a native-code core feature.

			if (game["MultiResHack"])
				VDC1.MultiResHack = game.GetIntValue("MultiResHack");

			Cpu.ResetPC();
			SetupMemoryDomains();
		}

		int lagCount;
		int frame;
		bool lagged = true;
		bool isLag = false;
		public int Frame { get { return frame; } set { frame = value; } }
		public int LagCount { get { return lagCount; } set { lagCount = value; } }
		public bool IsLagFrame { get { return isLag; } }

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		public void ResetCounters()
		{
			// this should just be a public setter instead of a new method.
			Frame = 0;
			lagCount = 0;
			isLag = false;
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			lagged = true;
			DriveLightOn = false;
			Frame++;
			CheckSpriteLimit();
			PSG.BeginFrame(Cpu.TotalExecutedCycles);

			Cpu.Debug = Tracer.Enabled;

			if (SuperGrafx)
				VPC.ExecFrame(render);
			else
				VDC1.ExecFrame(render);

			PSG.EndFrame(Cpu.TotalExecutedCycles);
			if (TurboCD)
				SoundSynchronizer.PullSamples(SoundMixer);

			if (lagged)
			{
				lagCount++;
				isLag = true;
			}
			else
				isLag = false;
		}

		void CheckSpriteLimit()
		{
			bool spriteLimit = ForceSpriteLimit | _settings.SpriteLimit;
			VDC1.PerformSpriteLimit = spriteLimit;
			if (VDC2 != null)
				VDC2.PerformSpriteLimit = spriteLimit;
		}

		public CoreComm CoreComm { get; private set; }

		public IVideoProvider VideoProvider
		{
			get { return (IVideoProvider)VPC ?? VDC1; }
		}

		ISoundProvider soundProvider;
		public ISoundProvider SoundProvider
		{
			get { return soundProvider; }
		}
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(soundProvider, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public string SystemId { get { return systemid; } }
		public string Region { get; set; }
		public bool DeterministicEmulation { get { return true; } }

		public byte[] CloneSaveRam()
		{
			if (BRAM != null)
				return (byte[])BRAM.Clone();
			else
				return null;
		}

		public void StoreSaveRam(byte[] data)
		{
			if (BRAM != null)
				Array.Copy(data, BRAM, data.Length);
		}

		public bool SaveRamModified { get; private set; }

		public bool BinarySaveStatesPreferred { get { return false; } }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }
		public void SaveStateText(TextWriter tw) { SyncState(Serializer.CreateTextWriter(tw)); }
		public void LoadStateText(TextReader tr) { SyncState(Serializer.CreateTextReader(tr)); }

		void SyncState(Serializer ser)
		{
			ser.BeginSection("PCEngine");
			Cpu.SyncState(ser);
			VCE.SyncState(ser);
			VDC1.SyncState(ser, 1);
			PSG.SyncState(ser);

			if (SuperGrafx)
			{
				VPC.SyncState(ser);
				VDC2.SyncState(ser, 2);
			}

			if (TurboCD)
			{
				ADPCM.SyncState(ser);
				CDAudio.SyncState(ser);
				SCSI.SyncState(ser);

				ser.Sync("CDRAM", ref CDRam, false);
				if (SuperRam != null)
					ser.Sync("SuperRAM", ref SuperRam, false);
				if (ArcadeCard)
					ArcadeCardSyncState(ser);
			}

			ser.Sync("RAM", ref Ram, false);
			ser.Sync("IOBuffer", ref IOBuffer);
			ser.Sync("CdIoPorts", ref CdIoPorts, false);
			ser.Sync("BramLocked", ref BramLocked);

			ser.Sync("Frame", ref frame);
			ser.Sync("Lag", ref lagCount);
			ser.Sync("IsLag", ref isLag);
			if (Cpu.ReadMemory21 == ReadMemorySF2)
				ser.Sync("SF2MapperLatch", ref SF2MapperLatch);
			if (PopulousRAM != null)
				ser.Sync("PopulousRAM", ref PopulousRAM, false);
			if (BRAM != null)
				ser.Sync("BRAM", ref BRAM, false);

			ser.EndSection();
		}

		byte[] stateBuffer;
		public byte[] SaveStateBinary()
		{
			if (stateBuffer == null)
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Flush();
				stateBuffer = stream.ToArray();
				writer.Close();
				return stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Flush();
				writer.Close();
				return stateBuffer;
			}
		}

		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(10);
			int mainmemorymask = Ram.Length - 1;
			var MainMemoryDomain = new MemoryDomain("Main Memory", Ram.Length, MemoryDomain.Endian.Little,
				addr => Ram[addr],
				(addr, value) => Ram[addr] = value);
			domains.Add(MainMemoryDomain);

			var SystemBusDomain = new MemoryDomain("System Bus", 0x200000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 0x200000)
						throw new ArgumentOutOfRangeException();
					return Cpu.ReadMemory21(addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 0x200000)
						throw new ArgumentOutOfRangeException();
					Cpu.WriteMemory21(addr, value);
				});
			domains.Add(SystemBusDomain);

			var RomDomain = new MemoryDomain("ROM", RomLength, MemoryDomain.Endian.Little,
				addr => RomData[addr],
				(addr, value) => RomData[addr] = value);
			domains.Add(RomDomain);
			

			if (BRAM != null)
			{
				var BRAMMemoryDomain = new MemoryDomain("Battery RAM", Ram.Length, MemoryDomain.Endian.Little,
					addr => BRAM[addr],
					(addr, value) => BRAM[addr] = value);
				domains.Add(BRAMMemoryDomain);
			}

			if (TurboCD)
			{
				var CDRamMemoryDomain = new MemoryDomain("TurboCD RAM", CDRam.Length, MemoryDomain.Endian.Little,
					addr => CDRam[addr],
					(addr, value) => CDRam[addr] = value);
				domains.Add(CDRamMemoryDomain);

				var AdpcmMemoryDomain = new MemoryDomain("ADPCM RAM", ADPCM.RAM.Length, MemoryDomain.Endian.Little,
					addr => ADPCM.RAM[addr],
					(addr, value) => ADPCM.RAM[addr] = value);
				domains.Add(AdpcmMemoryDomain);

				if (SuperRam != null)
				{
					var SuperRamMemoryDomain = new MemoryDomain("Super System Card RAM", SuperRam.Length, MemoryDomain.Endian.Little,
						addr => SuperRam[addr],
						(addr, value) => SuperRam[addr] = value);
					domains.Add(SuperRamMemoryDomain);
				}
			}

			if (ArcadeCard)
			{
				var ArcadeRamMemoryDomain = new MemoryDomain("Arcade Card RAM", ArcadeRam.Length, MemoryDomain.Endian.Little,
						addr => ArcadeRam[addr],
						(addr, value) => ArcadeRam[addr] = value);
				domains.Add(ArcadeRamMemoryDomain);
			}

			if (PopulousRAM != null)
			{
				var PopulusRAMDomain = new MemoryDomain("Cart Battery RAM", PopulousRAM.Length, MemoryDomain.Endian.Little,
					addr => PopulousRAM[addr],
					(addr, value) => PopulousRAM[addr] = value);
				domains.Add(PopulusRAMDomain);
			}

			memoryDomains = new MemoryDomainList(domains);
		}

		MemoryDomainList memoryDomains;
		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }

		public IDictionary<string, Register> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, Register>
			{
				{ "A", Cpu.A },
				{ "X", Cpu.X },
				{ "Y", Cpu.Y },
				{ "PC", Cpu.PC },
				{ "S", Cpu.S },
				{ "MPR-0", Cpu.MPR[0] },
				{ "MPR-1", Cpu.MPR[1] },
				{ "MPR-2", Cpu.MPR[2] },
				{ "MPR-3", Cpu.MPR[3] },
				{ "MPR-4", Cpu.MPR[4] },
				{ "MPR-5", Cpu.MPR[5] },
				{ "MPR-6", Cpu.MPR[6] },
				{ "MPR-7", Cpu.MPR[7] }
			};
		}

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			if (disc != null)
				disc.Dispose();
		}

		public PCESettings _settings;
		private PCESyncSettings _syncSettings;

		public PCESettings GetSettings() { return _settings.Clone(); }
		public PCESyncSettings GetSyncSettings() { return _syncSettings.Clone(); }
		public bool PutSettings(PCESettings o)
		{
			bool ret;
			if (o.ArcadeCardRewindHack != _settings.ArcadeCardRewindHack ||
				o.EqualizeVolume != _settings.EqualizeVolume)
				ret = true;
			else
				ret = false;

			_settings = o;
			return ret;
		}

		public bool PutSyncSettings(PCESyncSettings o)
		{
			bool ret = PCESyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			// SetControllerButtons(); // not safe to change the controller during emulation, so instead make it a reboot event
			return ret;
		}

		public class PCESettings
		{
			public bool ShowBG1 = true;
			public bool ShowOBJ1 = true;
			public bool ShowBG2 = true;
			public bool ShowOBJ2 = true;

			// these three require core reboot to use
			public bool SpriteLimit = false;
			public bool EqualizeVolume = false;
			public bool ArcadeCardRewindHack = true;

			public PCESettings Clone()
			{
				return (PCESettings)MemberwiseClone();
			}
		}

		public class PCESyncSettings
		{
			public ControllerSetting[] Controllers = 
			{
				new ControllerSetting { IsConnected = true },
				new ControllerSetting { IsConnected = false },
				new ControllerSetting { IsConnected = false },
				new ControllerSetting { IsConnected = false },
				new ControllerSetting { IsConnected = false }
			};

			public PCESyncSettings Clone()
			{
				var ret = new PCESyncSettings();
				for (int i = 0; i < Controllers.Length; i++)
				{
					ret.Controllers[i].IsConnected = Controllers[i].IsConnected;
				}
				return ret;
			}

			public class ControllerSetting
			{
				public bool IsConnected { get; set; }
			}

			public static bool NeedsReboot(PCESyncSettings x, PCESyncSettings y)
			{
				for (int i = 0; i < x.Controllers.Length; i++)
				{
					if (x.Controllers[i].IsConnected != y.Controllers[i].IsConnected)
						return true;
				}
				return false;
			}
		}
	}
}
