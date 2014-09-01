using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components;
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
	public sealed partial class SMS : IEmulator, IMemoryDomains
	{
		// Constants
		public const int BankSize = 16384;

		// ROM
		public byte[] RomData;
		public byte RomBank0, RomBank1, RomBank2, RomBank3;
		public byte RomBanks;

		// SaveRAM
		public byte[] SaveRAM;
		public byte SaveRamBank;

		public byte[] BiosRom;

		public byte[] CloneSaveRam()
		{
			if (SaveRAM != null)
				return (byte[])SaveRAM.Clone();
			else
				return null;
		}
		public void StoreSaveRam(byte[] data)
		{
			if (SaveRAM != null)
				Array.Copy(data, SaveRAM, data.Length);
		}
		public void ClearSaveRam()
		{
			if (SaveRAM != null)
				SaveRAM = new byte[SaveRAM.Length];
		}
		public bool SaveRamModified { get; set; }

		// Machine resources
		public Z80A Cpu;
		public byte[] SystemRam;
		public VDP Vdp;
		public SN76489 PSG;
		public YM2413 YM2413;
		public SoundMixer SoundMixer;
		public bool IsGameGear = false;
		public bool IsSG1000 = false;

		public bool HasYM2413 = false;

		int frame = 0;
		int lagCount = 0;
		bool lagged = true;
		bool isLag = false;
		public int Frame { get { return frame; } set { frame = value; } }
		public int LagCount { get { return lagCount; } set { lagCount = value; } }
		public bool IsLagFrame { get { return isLag; } }
		byte Port01 = 0xFF;
		byte Port02 = 0xFF;
		byte Port3E = 0xAF;
		byte Port3F = 0xFF;

		byte ForceStereoByte = 0xAD;
		bool IsGame3D = false;

		public DisplayType DisplayType { get; set; }
		public bool DeterministicEmulation { get { return true; } }

		[CoreConstructor("SMS", "SG", "GG")]
		public SMS(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			Settings = (SMSSettings)settings ?? new SMSSettings();
			SyncSettings = (SMSSyncSettings)syncSettings ?? new SMSSyncSettings();
			CoreComm = comm;

			IsGameGear = game.System == "GG";
			IsSG1000 = game.System == "SG";
		    RomData = rom;
            CoreComm.CpuTraceAvailable = true;
            
            if (RomData.Length % BankSize != 0)
                Array.Resize(ref RomData, ((RomData.Length / BankSize) + 1) * BankSize);
            RomBanks = (byte)(RomData.Length / BankSize);

            DisplayType = DetermineDisplayType(SyncSettings.DisplayType, game.Region);
			if (game["PAL"] && DisplayType != DisplayType.PAL)
			{
				DisplayType = DisplayType.PAL;
				CoreComm.Notify("Display was forced to PAL mode for game compatibility.");
			}
			if (IsGameGear) 
				DisplayType = DisplayType.NTSC; // all game gears run at 60hz/NTSC mode
			CoreComm.VsyncNum = DisplayType == DisplayType.NTSC ? 60 : 50;
			CoreComm.VsyncDen = 1;

			Region = SyncSettings.ConsoleRegion;
			if (Region == "Auto") Region = DetermineRegion(game.Region);

			if (game["Japan"] && Region != "Japan")
			{
				Region = "Japan";
				CoreComm.Notify("Region was forced to Japan for game compatibility.");
			}

            if ((game.NotInDatabase || game["FM"]) && SyncSettings.EnableFM && !IsGameGear)
                HasYM2413 = true;

            if (Controller == null)
                Controller = NullController.GetNullController();

            Cpu = new Z80A();
            Cpu.RegisterSP = 0xDFF0;
            Cpu.ReadHardware = ReadPort;
            Cpu.WriteHardware = WritePort;

            Vdp = new VDP(this, Cpu, IsGameGear ? VdpMode.GameGear : VdpMode.SMS, DisplayType);
            PSG = new SN76489();
            YM2413 = new YM2413();
            SoundMixer = new SoundMixer(YM2413, PSG);
            if (HasYM2413 && game["WhenFMDisablePSG"])
                SoundMixer.DisableSource(PSG);
            ActiveSoundProvider = HasYM2413 ? (ISoundProvider)SoundMixer : PSG;

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
				BiosRom = comm.CoreFileProvider.GetFirmware("SMS", Region, false);
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
		}

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

		DisplayType DetermineDisplayType(string display, string region)
		{
			if (display == "NTSC") return DisplayType.NTSC;
			if (display == "PAL") return DisplayType.PAL;
			if (region != null && region == "Europe") return DisplayType.PAL;
			return DisplayType.NTSC;
		}

		public void ResetCounters()
		{
			Frame = 0;
			lagCount = 0;
			isLag = false;
		}

		public byte ReadPort(ushort port)
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

		public void WritePort(ushort port, byte value)
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

		public void FrameAdvance(bool render, bool rendersound)
		{
			lagged = true;
			Frame++;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);
			Cpu.Debug = CoreComm.Tracer.Enabled;
			if (!IsGameGear)
				PSG.StereoPanning = Settings.ForceStereoSeparation ? ForceStereoByte : (byte) 0xFF;

			if (Cpu.Debug && Cpu.Logger == null) // TODO, lets not do this on each frame. But lets refactor CoreComm/CoreComm first
				Cpu.Logger = (s) => CoreComm.Tracer.Put(s);

			if (IsGameGear == false)
				Cpu.NonMaskableInterrupt = Controller["Pause"];

			if (IsGame3D && Settings.Fix3D)
				Vdp.ExecFrame((Frame & 1) == 0);
			else 
				Vdp.ExecFrame(render);

			PSG.EndFrame(Cpu.TotalExecutedCycles);
			if (lagged)
			{
				lagCount++;
				isLag = true;
			}
			else
				isLag = false;
		}

		public bool BinarySaveStatesPreferred { get { return false; } }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }
		public void SaveStateText(TextWriter tw) { SyncState(Serializer.CreateTextWriter(tw)); }
		public void LoadStateText(TextReader tr) { SyncState(Serializer.CreateTextReader(tr)); }
		
		void SyncState(Serializer ser)
		{
			ser.BeginSection("SMS");
			Cpu.SyncState(ser);
			Vdp.SyncState(ser);
			PSG.SyncState(ser);
			ser.Sync("RAM", ref SystemRam, false);
			ser.Sync("RomBank0", ref RomBank0);
			ser.Sync("RomBank1", ref RomBank1);
			ser.Sync("RomBank2", ref RomBank2);
			ser.Sync("RomBank3", ref RomBank3);
			ser.Sync("Port01", ref Port01);
			ser.Sync("Port02", ref Port02);
			ser.Sync("Port3E", ref Port3E);
			ser.Sync("Port3F", ref Port3F);

			if (SaveRAM != null)
			{
				ser.Sync("SaveRAM", ref SaveRAM, false);
				ser.Sync("SaveRamBank", ref SaveRamBank);
			}
			if (ExtRam != null)
				ser.Sync("ExtRAM", ref ExtRam, true);
			if (HasYM2413)
				YM2413.SyncState(ser);

			ser.Sync("Frame", ref frame);
			ser.Sync("LagCount", ref lagCount);
			ser.Sync("IsLag", ref isLag);

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
				stateBuffer = stream.ToArray();
				writer.Close();
				return stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Close();
				return stateBuffer;
			}
		}

		public IVideoProvider VideoProvider { get { return Vdp; } }
		public CoreComm CoreComm { get; private set; }

		ISoundProvider ActiveSoundProvider;
		public ISoundProvider SoundProvider { get { return ActiveSoundProvider; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(ActiveSoundProvider, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public string SystemId { get { return "SMS"; } }

		public string BoardName { get { return null; } }

		string region;
		public string Region
		{
			get { return region; }
			set
			{
				if (value.NotIn(validRegions))
					throw new Exception("Passed value " + value + " is not a valid region!");
				region = value;
			}
		}
		
		readonly string[] validRegions = { "Export", "Japan", "Auto" };

		MemoryDomainList memoryDomains;

		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomain("Main RAM", SystemRam.Length, MemoryDomain.Endian.Little,
				addr => SystemRam[addr],
				(addr, value) => SystemRam[addr] = value);
			var VRamDomain = new MemoryDomain("Video RAM", Vdp.VRAM.Length, MemoryDomain.Endian.Little,
				addr => Vdp.VRAM[addr],
				(addr, value) => Vdp.VRAM[addr] = value);
			
			var SystemBusDomain = new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
				(addr) =>
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					return Cpu.ReadMemory((ushort)addr);
				},
				(addr, value) =>
				{
					if (addr < 0 || addr >= 65536)
						throw new ArgumentOutOfRangeException();
					Cpu.WriteMemory((ushort)addr, value);
				});

			domains.Add(MainMemoryDomain);
			domains.Add(VRamDomain);
			domains.Add(SystemBusDomain);

			if (SaveRAM != null)
			{
				var SaveRamDomain = new MemoryDomain("Save RAM", SaveRAM.Length, MemoryDomain.Endian.Little,
					addr => SaveRAM[addr],
					(addr, value) => { SaveRAM[addr] = value; SaveRamModified = true; });
				domains.Add(SaveRamDomain);
			}
			if (ExtRam != null)
			{
				var ExtRamDomain = new MemoryDomain("Cart (Volatile) RAM", ExtRam.Length, MemoryDomain.Endian.Little,
					addr => ExtRam[addr],
					(addr, value) => { ExtRam[addr] = value; });
				domains.Add(ExtRamDomain);
			}
			memoryDomains = new MemoryDomainList(domains);
		}

		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", Cpu.RegisterA },
				{ "AF", Cpu.RegisterAF },
				{ "B", Cpu.RegisterB },
				{ "BC", Cpu.RegisterBC },
				{ "C", Cpu.RegisterC },
				{ "D", Cpu.RegisterD },
				{ "DE", Cpu.RegisterDE },
				{ "E", Cpu.RegisterE },
				{ "F", Cpu.RegisterF },
				{ "H", Cpu.RegisterH },
				{ "HL", Cpu.RegisterHL },
				{ "I", Cpu.RegisterI },
				{ "IX", Cpu.RegisterIX },
				{ "IY", Cpu.RegisterIY },
				{ "L", Cpu.RegisterL },
				{ "PC", Cpu.RegisterPC },
				{ "R", Cpu.RegisterR },
				{ "Shadow AF", Cpu.RegisterShadowAF },
				{ "Shadow BC", Cpu.RegisterShadowBC },
				{ "Shadow DE", Cpu.RegisterShadowDE },
				{ "Shadow HL", Cpu.RegisterShadowHL },
				{ "SP", Cpu.RegisterSP },
				{ "Flag C", Cpu.RegisterF.Bit(0) ? 1 : 0 },
				{ "Flag N", Cpu.RegisterF.Bit(1) ? 1 : 0 },
				{ "Flag P/V", Cpu.RegisterF.Bit(2) ? 1 : 0 },
				{ "Flag 3rd", Cpu.RegisterF.Bit(3) ? 1 : 0 },
				{ "Flag H", Cpu.RegisterF.Bit(4) ? 1 : 0 },
				{ "Flag 5th", Cpu.RegisterF.Bit(5) ? 1 : 0 },
				{ "Flag Z", Cpu.RegisterF.Bit(6) ? 1 : 0 },
				{ "Flag S", Cpu.RegisterF.Bit(7) ? 1 : 0 },
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Cpu.RegisterA = (byte)value;
					break;
				case "AF":
					Cpu.RegisterAF = (byte)value;
					break;
				case "B":
					Cpu.RegisterB = (byte)value;
					break;
				case "BC":
					Cpu.RegisterBC = (byte)value;
					break;
				case "C":
					Cpu.RegisterC = (byte)value;
					break;
				case "D":
					Cpu.RegisterD = (byte)value;
					break;
				case "DE":
					Cpu.RegisterDE = (byte)value;
					break;
				case "E":
					Cpu.RegisterE = (byte)value;
					break;
				case "F":
					Cpu.RegisterF = (byte)value;
					break;
				case "H":
					Cpu.RegisterH = (byte)value;
					break;
				case "HL":
					Cpu.RegisterHL = (byte)value;
					break;
				case "I":
					Cpu.RegisterI = (byte)value;
					break;
				case "IX":
					Cpu.RegisterIX = (byte)value;
					break;
				case "IY":
					Cpu.RegisterIY = (byte)value;
					break;
				case "L":
					Cpu.RegisterL = (byte)value;
					break;
				case "PC":
					Cpu.RegisterPC = (ushort)value;
					break;
				case "R":
					Cpu.RegisterR = (byte)value;
					break;
				case "Shadow AF":
					Cpu.RegisterShadowAF = (byte)value;
					break;
				case "Shadow BC":
					Cpu.RegisterShadowBC = (byte)value;
					break;
				case "Shadow DE":
					Cpu.RegisterShadowDE = (byte)value;
					break;
				case "Shadow HL":
					Cpu.RegisterShadowHL = (byte)value;
					break;
				case "SP":
					Cpu.RegisterSP = (byte)value;
					break;
			}
		}

		public void Dispose() { }

		public object GetSettings() { return Settings.Clone(); }
		public object GetSyncSettings() { return SyncSettings.Clone(); }
		public bool PutSettings(object o)
		{
			SMSSettings n = (SMSSettings)o;
			bool ret = SMSSettings.RebootNeeded(Settings, n);
			Settings = n;
			return ret;
		}
		public bool PutSyncSettings(object o)
		{
			SMSSyncSettings n = (SMSSyncSettings)o;
			bool ret = SMSSyncSettings.RebootNeeded(SyncSettings, n);
			SyncSettings = n;
			return ret;	
		}

		public SMSSettings Settings;
		public SMSSyncSettings SyncSettings;

		public class SMSSettings
		{
			// Game settings
			public bool ForceStereoSeparation = false;
			public bool SpriteLimit = false;
			public bool Fix3D = true;
			// GG settings
			public bool ShowClippedRegions = false;
			public bool HighlightActiveDisplayRegion = false;
			// graphics settings
			public bool DispBG = true;
			public bool DispOBJ = true;

			public SMSSettings Clone()
			{
				return (SMSSettings)MemberwiseClone();
			}
			public static bool RebootNeeded(SMSSettings x, SMSSettings y)
			{
				return false;
			}
		}

		public class SMSSyncSettings
		{
			public bool EnableFM = true;
			public bool AllowOverlock = false;
			public bool UseBIOS = false;
			public string ConsoleRegion = "Export";
			public string DisplayType = "NTSC";

			public SMSSyncSettings Clone()
			{
				return (SMSSyncSettings)MemberwiseClone();
			}
			public static bool RebootNeeded(SMSSyncSettings x, SMSSyncSettings y)
			{
				return
					x.EnableFM != y.EnableFM ||
					x.AllowOverlock != y.AllowOverlock ||
					x.UseBIOS != y.UseBIOS ||
					x.ConsoleRegion != y.ConsoleRegion ||
					x.DisplayType != y.DisplayType;
			}
		}
	}
}