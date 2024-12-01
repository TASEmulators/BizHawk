using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;
using BizHawk.Emulation.Cores.Components.Z80A;

/*****************************************************
  TODO: 
  + HCounter (Manually set for light phaser emulation... should be only case it's polled)
  + Try to clean up the organization of the source code. 
  + Mode 1 not implemented in VDP TMS modes. (I don't have a test case in SG1000 or Coleco)
 
**********************************************************/

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	[Core(CoreNames.SMSHawk, "Vecna")]
	public partial class SMS : IEmulator, ISoundProvider, ISaveRam, IInputPollable, IRegionable,
		IDebuggable, ISettable<SMS.SmsSettings, SMS.SmsSyncSettings>, ICodeDataLogger
	{
		[CoreConstructor(VSystemID.Raw.SMS)]
		[CoreConstructor(VSystemID.Raw.SG)]
		[CoreConstructor(VSystemID.Raw.GG)]
		public SMS(CoreComm comm, GameInfo game, byte[] rom, SmsSettings settings, SmsSyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			Settings = settings ?? new SmsSettings();
			SyncSettings = syncSettings ?? new SmsSyncSettings();

			IsGameGear = game.System == VSystemID.Raw.GG;
			IsGameGear_C = game.System == VSystemID.Raw.GG;
			IsSG1000 = game.System == VSystemID.Raw.SG;
			RomData = rom;

			if (RomData.Length % BankSize != 0)
			{
				Array.Resize(ref RomData, ((RomData.Length / BankSize) + 1) * BankSize);
			}

			RomBanks = (byte)(RomData.Length / BankSize);
			RomMask = RomData.Length - 1;
			if (RomMask == 0x5FFF) { RomMask = 0x7FFF; }
			if (RomMask == 0xBFFF) { RomMask = 0xFFFF; }

			Region = DetermineDisplayType(SyncSettings.DisplayType, game.Region);
			if (game["PAL"] && Region != DisplayType.PAL)
			{
				Region = DisplayType.PAL;
				comm.Notify("Display was forced to PAL mode for game compatibility.", null);
			}

			if (IsGameGear)
			{
				Region = DisplayType.NTSC; // all game gears run at 60hz/NTSC mode
			}

			_region = SyncSettings.ConsoleRegion;
			if (_region == SmsSyncSettings.Regions.Auto)
			{
				_region = DetermineRegion(game.Region);
			}

			if (game["Japan"] && _region != SmsSyncSettings.Regions.Japan)
			{
				_region = SmsSyncSettings.Regions.Japan;
				comm.Notify("Region was forced to Japan for game compatibility.", null);
			}

			if (game["Korea"] && _region != SmsSyncSettings.Regions.Korea)
			{
				_region = SmsSyncSettings.Regions.Korea;
				comm.Notify("Region was forced to Korea for game compatibility.", null);
			}

			if ((game.NotInDatabase || game["FM"]) && SyncSettings.EnableFm && !IsGameGear)
			{
				HasYM2413 = true;
			}

			Cpu = new Z80A<CpuLink>(new CpuLink(this));

			// set this before turning off GG system for GG_in_SMS games
			bool sms_reg_compat = !IsGameGear && (_region == SmsSyncSettings.Regions.Japan);

			if (game["GG_in_SMS"])
			{
				// skip setting the BIOS because this is a game gear game that puts the system
				// in SMS compatibility mode (it will fail the check sum if played on an actual SMS though.)
				IsGameGear = false;
				IsGameGear_C = true;
				game.System = VSystemID.Raw.GG;
				Console.WriteLine("Using SMS Compatibility mode for Game Gear System");
			}

			SystemId = game.System;

			Vdp = new VDP(this, Cpu, IsGameGear ? VdpMode.GameGear : VdpMode.SMS, Region, sms_reg_compat);
			ser.Register<IVideoProvider>(Vdp);
			PSG = new SN76489sms();
			YM2413 = new YM2413();
			//SoundMixer = new SoundMixer(YM2413, PSG);
			if (HasYM2413 && game["WhenFMDisablePSG"])
			{
				disablePSG = true;
			}

			BlipL.SetRates(3579545, 44100);
			BlipR.SetRates(3579545, 44100);

			ser.Register<ISoundProvider>(this);

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
			else if (game["EEPROM"])
				InitEEPROMMapper();
			else if(game["SG_EX_A"])
				Init_SG_EX_A();
			else if (game["SG_EX_B"])
				Init_SG_EX_B();
			else
				InitSegaMapper();

			if (Settings.ForceStereoSeparation && !IsGameGear)
			{
				if (game["StereoByte"])
				{
					ForceStereoByte = byte.Parse(game.OptionValue("StereoByte"));
				}

				PSG.Set_Panning(ForceStereoByte);
			}

			if (SyncSettings.AllowOverClock && game["OverclockSafe"])
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
			else if (game.System == VSystemID.Raw.SMS && !game["GG_in_SMS"])
			{
				BiosRom = comm.CoreFileProvider.GetFirmware(new("SMS", _region.ToString()));

				if (BiosRom == null)
				{
					throw new MissingFirmwareException("No BIOS found");
				}
				
				if (!game["RequireBios"] && !SyncSettings.UseBios)
				{
					// we are skipping the BIOS
					// but only if it won't break the game
				}
				else
				{
					Port3E = 0xF7;
				}
			}

			if (game["SRAM"])
			{
				SaveRAM = new byte[int.Parse(game.OptionValue("SRAM"))];
				Console.WriteLine(SaveRAM.Length);
			}
			else if (game.NotInDatabase)
			{
				SaveRAM = new byte[0x8000];
			}

			SetupMemoryDomains();

			//this manages the linkage between the cpu and mapper callbacks so it needs running before bootup is complete
			((ICodeDataLogger)this).SetCDL(null);

			Tracer = new TraceBuffer(Cpu.TraceHeader);

			ser.Register(Tracer);
			ser.Register<IDisassemblable>(Cpu);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			Vdp.ProcessOverscan();

			// Z80 SP initialization
			// stops a few SMS and GG games from crashing
			Cpu.Regs[Cpu.SPl] = 0xF0;
			Cpu.Regs[Cpu.SPh] = 0xDF;

			if (!IsSG1000)
			{
				ser.Register<ISmsGpuView>(new SmsGpuView(Vdp));
			}

			_controllerDeck = new SMSControllerDeck(SyncSettings.Port1, SyncSettings.Port2, IsGameGear_C, SyncSettings.UseKeyboard);

			// Sorta a hack but why not
			PortDEEnabled = SyncSettings.UseKeyboard && !IsGameGear_C;

			_controllerDeck.SetRegion(_controller, _region == SmsSyncSettings.Regions.Japan);
		}

		public void HardReset()
		{
		}

		// Constants
		private const int BankSize = 16384;

		// ROM
		public byte[] RomData;
		public int RomMask;
		private byte RomBank0, RomBank1, RomBank2, RomBank3;
		private byte Bios_bank;
		private readonly byte RomBanks;
		private readonly byte[] BiosRom;

		// Machine resources
		public Z80A<CpuLink> Cpu;
		public byte[] SystemRam;
		public VDP Vdp;
		public SN76489sms PSG;

		public bool IsGameGear { get; set; }
		public bool IsGameGear_C { get; set; }
		public bool IsSG1000 { get; set; }

		private readonly bool HasYM2413 = false;
		private bool disablePSG = false;
		private bool PortDEEnabled = false;
		private IController _controller = NullController.Instance;

		private int _frame = 0;

		private byte Port01 = 0xFF;
		public byte Port02 = 0xFF;
		public byte Port03 = 0x00;
		public byte Port04 = 0xFF;
		public byte Port05 = 0x00;
		private byte Port3E = 0xAF;
		private byte Port3F = 0xFF;
		private byte PortDE = 0x00;

		private readonly byte ForceStereoByte = 0xAD;
		private readonly bool IsGame3D = false;

		// Linked Play Only
		public bool start_pressed;
		public byte cntr_rd_0;
		public byte cntr_rd_1;
		public byte cntr_rd_2;
		public bool stand_alone = true;
		public bool p3_write;
		public bool p4_read;

		public DisplayType Region { get; set; }

		private readonly ITraceable Tracer;

		private SmsSyncSettings.Regions DetermineRegion(string gameRegion)
		{
			if (gameRegion == null)
				return SmsSyncSettings.Regions.Export;
			if (gameRegion.IndexOf("USA", StringComparison.Ordinal) >= 0)
				return SmsSyncSettings.Regions.Export;
			if (gameRegion.IndexOf("Europe", StringComparison.Ordinal) >= 0)
				return SmsSyncSettings.Regions.Export;
			if (gameRegion.IndexOf("World", StringComparison.Ordinal) >= 0)
				return SmsSyncSettings.Regions.Export;
			if (gameRegion.IndexOf("Brazil", StringComparison.Ordinal) >= 0)
				return SmsSyncSettings.Regions.Export;
			if (gameRegion.IndexOf("Australia", StringComparison.Ordinal) >= 0)
				return SmsSyncSettings.Regions.Export;
			if (gameRegion.IndexOf("Korea", StringComparison.Ordinal) >= 0)
				return SmsSyncSettings.Regions.Korea;
			return SmsSyncSettings.Regions.Japan;
		}

		private DisplayType DetermineDisplayType(SmsSyncSettings.DisplayTypes display, string region)
		{
			if (display == SmsSyncSettings.DisplayTypes.Ntsc) return DisplayType.NTSC;
			if (display == SmsSyncSettings.DisplayTypes.Pal) return DisplayType.PAL;
			if (region != null && region == "Europe") return DisplayType.PAL;
			return DisplayType.NTSC;
		}

		public byte ReadMemory(ushort addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessRead;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}

			return ReadMemoryMapper(addr);
		}

		public void WriteMemory(ushort addr, byte value)
		{
			WriteMemoryMapper(addr, value);

			if (MemoryCallbacks.HasWrites)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessWrite;
				MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
			}
		}

		public byte FetchMemory(ushort addr)
		{
			return ReadMemoryMapper(addr);
		}

		private void OnExecMemory(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		/// <summary>
		/// The ReadMemory callback for the mapper
		/// </summary>
		private Func<ushort, byte> ReadMemoryMapper;

		/// <summary>
		/// The WriteMemory callback for the wrapper
		/// </summary>
		private Action<ushort, byte> WriteMemoryMapper;

		private byte ReadPort(ushort port)
		{
			port &= 0xFF;
			if (port < 0x40) // General IO ports
			{
				
				switch (port)
				{
					case 0x00: if (stand_alone) { return ReadPort0(); } else { _lagged = false; return cntr_rd_0; }
					case 0x01: return Port01;
					case 0x02: return Port02;
					case 0x03: return Port03;
					case 0x04: p4_read = true; return Port04;
					case 0x05: return Port05;
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
					return Vdp.ReadHLineCounter();
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
				case 0xDC: if (stand_alone) { return ReadControls1(); } else { _lagged = false; return cntr_rd_1; }
				case 0xC1:
				case 0xDD: if (stand_alone) { return ReadControls2(); } else { _lagged = false; return cntr_rd_2; }
				case 0xDE: return PortDEEnabled ? PortDE : (byte)0xFF;
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
					case 0x03: p3_write = true; Port03 = value; break;
					case 0x04: /*Port04 = value;*/ break; // receive port, not sure what writing does
					case 0x05: Port05 = (byte)(value & 0xF8); break;
					case 0x06: PSG.Set_Panning(value); break;
					case 0x3E: Port3E = value; break;
					case 0x3F: Port3F = value; break;
				}
			}
			else if (port < 0x80) // PSG
				PSG.WriteReg(value);
			else if (port < 0xC0) // VDP
			{
				if ((port & 1) == 0)
					Vdp.WriteVdpData(value);
				else
					Vdp.WriteVdpControl(value);
			}
			else if (port == 0xDE && PortDEEnabled) PortDE = value;
			else if (port == 0xF0 && HasYM2413) YM2413.RegisterLatch = value;
			else if (port == 0xF1 && HasYM2413) YM2413.Write(value);
			else if (port == 0xF2 && HasYM2413) YM2413.DetectionValue = value;
		}

		private readonly SMSControllerDeck _controllerDeck;
		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		private readonly SmsSyncSettings.Regions _region;

		public class SmsGpuView : ISmsGpuView
		{
			private readonly VDP _vdp;

			public SmsGpuView(VDP vdp)
			{
				_vdp = vdp;
			}

			public byte[] PatternBuffer => _vdp.PatternBuffer;
			public int FrameHeight => _vdp.FrameHeight;
			public byte[] VRAM => _vdp.VRAM;
			public int[] Palette => _vdp.Palette;
			public int CalcNameTableBase() => _vdp.CalcNameTableBase();
		}
	}
}
