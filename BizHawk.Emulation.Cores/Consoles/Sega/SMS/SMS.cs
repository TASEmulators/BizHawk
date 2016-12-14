using System;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.Cores.Components.Z80;

/*****************************************************
  TODO: 
  + HCounter
  + Try to clean up the organization of the source code. 
  + Lightgun/Paddle/etc if I get really bored  
  + Mode 1 not implemented in VDP TMS modes. (I dont have a test case in SG1000 or Coleco)
 
**********************************************************/

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	[CoreAttributes(
		"SMSHawk",
		"Vecna",
		isPorted: false,
		isReleased: true
		)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public sealed partial class SMS : IEmulator, ISaveRam, IStatable, IInputPollable, IRegionable,
		IDebuggable, ISettable<SMS.SMSSettings, SMS.SMSSyncSettings>, ICodeDataLogger
	{
		// Constants
		private const int BankSize = 16384;

		// ROM
		private byte[] RomData;
		private byte RomBank0, RomBank1, RomBank2, RomBank3;
		private byte RomBanks;
		private byte[] BiosRom;

		// Machine resources
		private Z80A Cpu;
		private byte[] SystemRam;
		public VDP Vdp;
		private SN76489 PSG;
		private YM2413 YM2413;
		public bool IsGameGear { get; set; }
		public bool IsSG1000 { get; set; }

		private bool HasYM2413 = false;

		private int frame = 0;
		
		public int Frame { get { return frame; } set { frame = value; } }

		private byte Port01 = 0xFF;
		private byte Port02 = 0xFF;
		private byte Port3E = 0xAF;
		private byte Port3F = 0xFF;

		private byte ForceStereoByte = 0xAD;
		private bool IsGame3D = false;

		public DisplayType Region { get; set; }

		[CoreConstructor("SMS", "SG", "GG")]
		public SMS(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			Settings = (SMSSettings)settings ?? new SMSSettings();
			SyncSettings = (SMSSyncSettings)syncSettings ?? new SMSSyncSettings();
			CoreComm = comm;
			MemoryCallbacks = new MemoryCallbackSystem();

			IsGameGear = game.System == "GG";
			IsSG1000 = game.System == "SG";
			RomData = rom;

			if (RomData.Length % BankSize != 0)
				Array.Resize(ref RomData, ((RomData.Length / BankSize) + 1) * BankSize);
			RomBanks = (byte)(RomData.Length / BankSize);

			Region = DetermineDisplayType(SyncSettings.DisplayType, game.Region);
			if (game["PAL"] && Region != DisplayType.PAL)
			{
				Region = DisplayType.PAL;
				CoreComm.Notify("Display was forced to PAL mode for game compatibility.");
			}
			if (IsGameGear) 
				Region = DisplayType.NTSC; // all game gears run at 60hz/NTSC mode
			CoreComm.VsyncNum = Region == DisplayType.NTSC ? 60 : 50;
			CoreComm.VsyncDen = 1;

			RegionStr = SyncSettings.ConsoleRegion;
			if (RegionStr == "Auto") RegionStr = DetermineRegion(game.Region);

			if (game["Japan"] && RegionStr != "Japan")
			{
				RegionStr = "Japan";
				CoreComm.Notify("Region was forced to Japan for game compatibility.");
			}

			if ((game.NotInDatabase || game["FM"]) && SyncSettings.EnableFM && !IsGameGear)
				HasYM2413 = true;

			if (Controller == null)
			{
				Controller = NullController.Instance;
			}

			Cpu = new Z80A();
			Cpu.RegisterSP = 0xDFF0;
			Cpu.ReadHardware = ReadPort;
			Cpu.WriteHardware = WritePort;
			Cpu.MemoryCallbacks = MemoryCallbacks;

			Vdp = new VDP(this, Cpu, IsGameGear ? VdpMode.GameGear : VdpMode.SMS, Region);
			(ServiceProvider as BasicServiceProvider).Register<IVideoProvider>(Vdp);
			PSG = new SN76489();
			YM2413 = new YM2413();
			SoundMixer = new SoundMixer(YM2413, PSG);
			if (HasYM2413 && game["WhenFMDisablePSG"])
				SoundMixer.DisableSource(PSG);
			ActiveSoundProvider = HasYM2413 ? (IAsyncSoundProvider)SoundMixer : PSG;
			_fakeSyncSound = new FakeSyncSound(ActiveSoundProvider, 735);
			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(_fakeSyncSound);

			SystemRam = new byte[0x2000];

			if (game["CMMapper"])
				InitCodeMastersMapper();
			else if (game["CMMapperWithRam"])
				InitCodeMastersMapperRam();
			else if (game["ExtRam"])
				InitExt2kMapper(int.Parse(game.OptionValue("ExtRam")));
			else if (game["KoreaMapper"])
				InitKoreaMapper();
			else if (game["MSXMapper"])
				InitMSXMapper();
			else if (game["NemesisMapper"])
				InitNemesisMapper();
			else if (game["TerebiOekaki"])
				InitTerebiOekaki();
			else
				InitSegaMapper();

			if (Settings.ForceStereoSeparation && !IsGameGear)
			{
				if (game["StereoByte"])
				{
					ForceStereoByte = byte.Parse(game.OptionValue("StereoByte"));
				}
				PSG.StereoPanning = ForceStereoByte;
			}

			if (SyncSettings.AllowOverlock && game["OverclockSafe"])
				Vdp.IPeriod = 512;

			if (Settings.SpriteLimit)
				Vdp.SpriteLimit = true;

			if (game["3D"])
				IsGame3D = true;

			if (game["BIOS"])
			{
				Port3E = 0xF7; // Disable cartridge, enable BIOS rom
				InitBiosMapper();
			}
			else if (game.System == "SMS")
			{
				BiosRom = comm.CoreFileProvider.GetFirmware("SMS", RegionStr, false);
				if (BiosRom != null && (game["RequireBios"] || SyncSettings.UseBIOS))
					Port3E = 0xF7;

				if (BiosRom == null && game["RequireBios"])
					throw new MissingFirmwareException("BIOS image not available. This game requires BIOS to function.");
				if (SyncSettings.UseBIOS && BiosRom == null)
					CoreComm.Notify("BIOS was selected, but rom image not available. BIOS not enabled.");
			}

			if (game["SRAM"])
				SaveRAM = new byte[int.Parse(game.OptionValue("SRAM"))];
			else if (game.NotInDatabase)
				SaveRAM = new byte[0x8000];

			SetupMemoryDomains();

			//this manages the linkage between the cpu and mapper callbacks so it needs running before bootup is complete
			((ICodeDataLogger)this).SetCDL(null);

			InputCallbacks = new InputCallbackSystem();

			Tracer = new TraceBuffer { Header = Cpu.TraceHeader };

			var serviceProvider = ServiceProvider as BasicServiceProvider;
			serviceProvider.Register<ITraceable>(Tracer);
			serviceProvider.Register<IDisassemblable>(new Disassembler());
			Vdp.ProcessOverscan();
		}

		private ITraceable Tracer { get; set; }

		string DetermineRegion(string gameRegion)
		{
			if (gameRegion == null)
				return "Export";
			if (gameRegion.IndexOf("USA") >= 0)
				return "Export";
			if (gameRegion.IndexOf("Europe") >= 0)
				return "Export";
			if (gameRegion.IndexOf("World") >= 0)
				return "Export";
			if (gameRegion.IndexOf("Brazil") >= 0)
				return "Export";
			if (gameRegion.IndexOf("Australia") >= 0)
				return "Export";
			return "Japan";
		}

		private DisplayType DetermineDisplayType(string display, string region)
		{
			if (display == "NTSC") return DisplayType.NTSC;
			if (display == "PAL") return DisplayType.PAL;
			if (region != null && region == "Europe") return DisplayType.PAL;
			return DisplayType.NTSC;
		}

		/// <summary>
		/// The ReadMemory callback for the mapper
		/// </summary>
		private Func<ushort, byte> ReadMemory;

		/// <summary>
		/// The WriteMemory callback for the wrapper
		/// </summary>
		private Action<ushort, byte> WriteMemory;

		/// <summary>
		/// A dummy FetchMemory that simply reads the memory
		/// </summary>
		private byte FetchMemory_StubThunk(ushort address, bool first)
		{
			return ReadMemory(address);
		}

		private byte ReadPort(ushort port)
		{
			port &= 0xFF;
			if (port < 0x40) // General IO ports
			{
				switch (port)
				{
					case 0x00: return ReadPort0();
					case 0x01: return Port01;
					case 0x02: return Port02;
					case 0x03: return 0x00;
					case 0x04: return 0xFF;
					case 0x05: return 0x00;
					case 0x06: return 0xFF;
					case 0x3E: return Port3E;
					default: return 0xFF;
				}
			}
			if (port < 0x80)  // VDP Vcounter/HCounter
			{
				if ((port & 1) == 0)
					return Vdp.ReadVLineCounter();
				else
					return 0x50; // TODO Vdp.ReadHLineCounter();
			}
			if (port < 0xC0) // VDP data/control ports
			{
				if ((port & 1) == 0)
					return Vdp.ReadData();
				else
					return Vdp.ReadVdpStatus();
			}
			switch (port) 
			{
				case 0xC0:
				case 0xDC: return ReadControls1();
				case 0xC1:
				case 0xDD: return ReadControls2();
				case 0xF2: return HasYM2413 ? YM2413.DetectionValue : (byte)0xFF;
				default: return 0xFF;
			}
		}

		private void WritePort(ushort port, byte value)
		{
			port &= 0xFF;
			if (port < 0x40) // general IO ports
			{
				switch (port & 0xFF)
				{
					case 0x01: Port01 = value; break;
					case 0x02: Port02 = value; break;
					case 0x06: PSG.StereoPanning = value; break;
					case 0x3E: Port3E = value; break;
					case 0x3F: Port3F = value; break;
				}
			}
			else if (port < 0x80) // PSG
				PSG.WritePsgData(value, Cpu.TotalExecutedCycles);
			else if (port < 0xC0) // VDP
			{
				if ((port & 1) == 0)
					Vdp.WriteVdpData(value);
				else
					Vdp.WriteVdpControl(value);
			}
			else if (port == 0xF0 && HasYM2413) YM2413.RegisterLatch = value;
			else if (port == 0xF1 && HasYM2413) YM2413.Write(value);
			else if (port == 0xF2 && HasYM2413) YM2413.DetectionValue = value;
		}

		private string _region;
		private string RegionStr
		{
			get { return _region; }
			set
			{
				if (value.NotIn(validRegions))
				{
					throw new Exception("Passed value " + value + " is not a valid region!");
				}

				_region = value;
			}
		}
		
		private readonly string[] validRegions = { "Export", "Japan", "Auto" };
	}
}