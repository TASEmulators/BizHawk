using System;
using System.Collections.Generic;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
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
	public sealed partial class PCEngine : IEmulator, ISaveRam, IStatable, IInputPollable,
		IDebuggable, ISettable<PCEngine.PCESettings, PCEngine.PCESyncSettings>, IDriveLight, ICodeDataLogger
	{
		[CoreConstructor("PCE", "SGX")]
		public PCEngine(CoreComm comm, GameInfo game, byte[] rom, object Settings, object syncSettings)
		{

			MemoryCallbacks = new MemoryCallbackSystem();
			CoreComm = comm;

			switch (game.System)
			{
				default:
				case "PCE":
					SystemId = "PCE";
					Type = NecSystemType.TurboGrafx;
					break;
				case "SGX":
					SystemId = "SGX";
					Type = NecSystemType.SuperGrafx;
					break;
			}
			this.Settings = (PCESettings)Settings ?? new PCESettings();
			_syncSettings = (PCESyncSettings)syncSettings ?? new PCESyncSettings();
			Init(game, rom);
			SetControllerButtons();
		}

		public PCEngine(CoreComm comm, GameInfo game, Disc disc, object Settings, object syncSettings)
		{
			CoreComm = comm;
			MemoryCallbacks = new MemoryCallbackSystem();
			DriveLightEnabled = true;
			SystemId = "PCECD";
			Type = NecSystemType.TurboCD;
			this.disc = disc;
			this.Settings = (PCESettings)Settings ?? new PCESettings();
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
			CoreComm.RomStatusDetails = string.Format("{0}\r\nDisk partial hash:{1}", game.Name, new DiscSystem.DiscHasher(disc).OldHash());
			SetControllerButtons();
		}

		// ROM
		private byte[] RomData;
		private int RomLength;
		private Disc disc;

		// Machine
		public NecSystemType Type;
		internal HuC6280 Cpu;
		public VDC VDC1, VDC2;
		public VCE VCE;
		private VPC VPC;
		private ScsiCDBus SCSI;
		private ADPCM ADPCM;

		public HuC6280PSG PSG;
		internal CDAudio CDAudio;
		private SoundMixer SoundMixer;

		private bool TurboGrafx { get { return Type == NecSystemType.TurboGrafx; } }
		private bool SuperGrafx { get { return Type == NecSystemType.SuperGrafx; } }
		private bool TurboCD { get { return Type == NecSystemType.TurboCD; } }

		// BRAM
		private bool BramEnabled = false;
		private bool BramLocked = true;
		private byte[] BRAM;

		// Memory system
		private byte[] Ram;       // PCE= 8K base ram, SGX= 64k base ram
		private byte[] CDRam;     // TurboCD extra 64k of ram
		private byte[] SuperRam;  // Super System Card 192K of additional RAM
		private byte[] ArcadeRam; // Arcade Card 2048K of additional RAM

		private bool ForceSpriteLimit;

		// 21,477,270  Machine clocks / sec
		//  7,159,090  Cpu cycles / sec

		private ITraceable Tracer { get; set; }

		private void Init(GameInfo game, byte[] rom)
		{
			Controller = NullController.Instance;
			Cpu = new HuC6280(MemoryCallbacks);
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
				soundProvider = new FakeSyncSound(PSG, 735);
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
				soundProvider = new FakeSyncSound(PSG, 735);
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
				soundProvider = new FakeSyncSound(SoundMixer, 735);
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
				Cpu.Logger = (s) => Tracer.Put(new TraceInfo
				{
					Disassembly = string.Format("{0:X1}:{1}", SF2MapperLatch, s),
					RegisterInfo = ""
				});
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
				ArcadeCardRewindHack = Settings.ArcadeCardRewindHack;
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
			if (TurboCD && (Settings.EqualizeVolume || game["EqualizeVolumes"] || game.NotInDatabase))
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

			Tracer = new TraceBuffer { Header = Cpu.TraceHeader };
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			ser.Register<ITraceable>(Tracer);
			ser.Register<IDisassemblable>(Cpu);
			ser.Register<IVideoProvider>((IVideoProvider)VPC ?? VDC1);
			ser.Register<ISoundProvider>(soundProvider);
			SetupMemoryDomains();
		}

		private int frame;

		private static Dictionary<string, int> SizesFromHuMap(IEnumerable<HuC6280.MemMapping> mm)
		{
			Dictionary<string, int> sizes = new Dictionary<string, int>();
			foreach (var m in mm)
			{
			if (!sizes.ContainsKey(m.Name) || m.MaxOffs >= sizes[m.Name])
				sizes[m.Name] = m.MaxOffs;
			}

			var keys = new List<string>(sizes.Keys);
			foreach (var key in keys)
			{
				// becase we were looking at offsets, and each bank is 8192 big, we need to add that size
				sizes[key] += 8192;
			}
			return sizes;
		}

		private void CheckSpriteLimit()
		{
			bool spriteLimit = ForceSpriteLimit | Settings.SpriteLimit;
			VDC1.PerformSpriteLimit = spriteLimit;
			if (VDC2 != null)
				VDC2.PerformSpriteLimit = spriteLimit;
		}

		private ISoundProvider soundProvider;

		private string Region { get; set; }
	}
}
