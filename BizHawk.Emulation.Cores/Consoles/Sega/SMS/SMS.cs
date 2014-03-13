using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components;
using BizHawk.Emulation.Cores.Components.Z80;

/*****************************************************

  TODO: 
  + HCounter
  + Try to clean up the organization of the source code. 
  + Lightgun/Paddle/etc if I get really bored  
  + Mode 1 not implemented in VDP TMS modes. (I dont have a test case in SG1000 or Coleco)
  + Add Region to GameDB.
  + Still need a "disable bios for japan-only games when bios is enabled and region is export" functionality
  + Or a "force region to japan if game is only for japan" thing. Which one is better?
 
**********************************************************/

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IEmulator
	{
		// Constants
		public const int BankSize = 16384;

		// ROM
		public byte[] RomData;
		public byte RomBank0, RomBank1, RomBank2;
		public byte RomBanks;

		// SaveRAM
		public byte[] SaveRAM = new byte[BankSize * 2];
		public byte SaveRamBank;

		public byte[] BiosRom;

		public byte[] ReadSaveRam()
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
		public bool HasYM2413 = false;

		int _lagcount = 0;
		bool lagged = true;
		bool islag = false;
		public int Frame { get; set; }
		
		public void ResetCounters()
		{
			Frame = 0;
			_lagcount = 0;
			islag = false;
		}

		public List<KeyValuePair<string, int>> GetCpuFlagsAndRegisters()
		{
			return new List<KeyValuePair<string, int>>
			{
				new KeyValuePair<string, int>("A", Cpu.RegisterA),
				new KeyValuePair<string, int>("AF", Cpu.RegisterAF),
				new KeyValuePair<string, int>("B", Cpu.RegisterB),
				new KeyValuePair<string, int>("BC", Cpu.RegisterBC),
				new KeyValuePair<string, int>("C", Cpu.RegisterC),
				new KeyValuePair<string, int>("D", Cpu.RegisterD),
				new KeyValuePair<string, int>("DE", Cpu.RegisterDE),
				new KeyValuePair<string, int>("E", Cpu.RegisterE),
				new KeyValuePair<string, int>("F", Cpu.RegisterF),
				new KeyValuePair<string, int>("H", Cpu.RegisterH),
				new KeyValuePair<string, int>("HL", Cpu.RegisterHL),
				new KeyValuePair<string, int>("I", Cpu.RegisterI),
				new KeyValuePair<string, int>("IX", Cpu.RegisterIX),
				new KeyValuePair<string, int>("IY", Cpu.RegisterIY),
				new KeyValuePair<string, int>("L", Cpu.RegisterL),
				new KeyValuePair<string, int>("PC", Cpu.RegisterPC),
				new KeyValuePair<string, int>("R", Cpu.RegisterR),
				new KeyValuePair<string, int>("Shadow AF", Cpu.RegisterShadowAF),
				new KeyValuePair<string, int>("Shadow BC", Cpu.RegisterShadowBC),
				new KeyValuePair<string, int>("Shadow DE", Cpu.RegisterShadowDE),
				new KeyValuePair<string, int>("Shadow HL", Cpu.RegisterShadowHL),
				new KeyValuePair<string, int>("SP", Cpu.RegisterSP),
				new KeyValuePair<string, int>("Flag C", Cpu.RegisterF.Bit(0) ? 1 : 0),
				new KeyValuePair<string, int>("Flag N", Cpu.RegisterF.Bit(1) ? 1 : 0),
				new KeyValuePair<string, int>("Flag P/V", Cpu.RegisterF.Bit(2) ? 1 : 0),
				new KeyValuePair<string, int>("Flag 3rd", Cpu.RegisterF.Bit(3) ? 1 : 0),
				new KeyValuePair<string, int>("Flag H", Cpu.RegisterF.Bit(4) ? 1 : 0),
				new KeyValuePair<string, int>("Flag 5th", Cpu.RegisterF.Bit(5) ? 1 : 0),
				new KeyValuePair<string, int>("Flag Z", Cpu.RegisterF.Bit(6) ? 1 : 0),
				new KeyValuePair<string, int>("Flag S", Cpu.RegisterF.Bit(7) ? 1 : 0),
			};
		}
		
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }
		byte Port01 = 0xFF;
		byte Port02 = 0xFF;
		byte Port3E = 0xAF;
		byte Port3F = 0xFF;

		byte ForceStereoByte = 0xAD;
		bool IsGame3D = false;

		public DisplayType DisplayType { get; set; }
		public bool DeterministicEmulation { get { return true; } }

		public SMS(CoreComm comm, GameInfo game, byte[] rom, object settings, object syncSettings)
		{
			Settings = (SMSSettings)settings ?? new SMSSettings();
			SyncSettings = (SMSSyncSettings)syncSettings ?? new SMSSyncSettings();

			CoreComm = comm;
			
			IsGameGear = game.System == "GG";
		    RomData = rom;
            CoreComm.CpuTraceAvailable = true;
            
            if (RomData.Length % BankSize != 0)
                Array.Resize(ref RomData, ((RomData.Length / BankSize) + 1) * BankSize);
            RomBanks = (byte)(RomData.Length / BankSize);

            DisplayType = SyncSettings.UsePAL ? DisplayType.PAL : DisplayType.NTSC;
			if (game["PAL"] && DisplayType != DisplayType.PAL)
			{
				DisplayType = DisplayType.PAL;
				Console.WriteLine("Display was forced to PAL mode for game compatibility."); // TODO change to corecomm.notify when it exists
			}
			CoreComm.VsyncNum = DisplayType == DisplayType.NTSC ? 60 : 50;
			CoreComm.VsyncDen = 1;

			Region = SyncSettings.ExportRegion ? "Export" : "Japan";
			if (game["Japan"] && Region != "Japan")
			{
				Region = "Japan";
				Console.WriteLine("Region was forced to Japan for game compatibility."); // TODO corecomm.notify
			}

            if (game.NotInDatabase || game["FM"] && SyncSettings.EnableFM && !IsGameGear)
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
			else if (game["ExtRam"])
				InitExt2kMapper(int.Parse(game.OptionValue("ExtRam")));
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
				if (BiosRom != null && SyncSettings.UseBIOS)
					Port3E = 0xF7;

				if (SyncSettings.UseBIOS && BiosRom == null)
					Console.WriteLine("BIOS was selected, but rom image not available. BIOS not enabled."); // TODO corecomm.notify
			}
            
			SetupMemoryDomains();
		}

		public byte ReadPort(ushort port)
		{
			switch (port & 0xFF)
			{
				case 0x00: return ReadPort0();
				case 0x01: return Port01;
				case 0x02: return Port02;
				case 0x03: return 0x00;
				case 0x04: return 0xFF;
				case 0x05: return 0x00;
				case 0x06: return 0xFF;
				case 0x3E: return Port3E;
				case 0x7E: return Vdp.ReadVLineCounter();
				case 0x7F: break; // hline counter TODO
				case 0xBE: return Vdp.ReadData();
				case 0xBF: return Vdp.ReadVdpStatus();
				case 0xC0:
				case 0xDC: return ReadControls1();
				case 0xC1:
				case 0xDD: return ReadControls2();
				case 0xF2: return HasYM2413 ? YM2413.DetectionValue : (byte)0xFF;
			}
			return 0xFF;
		}

		public void WritePort(ushort port, byte value)
		{
			switch (port & 0xFF)
			{
				case 0x01: Port01 = value; break;
				case 0x02: Port02 = value; break;
				case 0x06: PSG.StereoPanning = value; break;
				case 0x3E: Port3E = value; break;
				case 0x3F: Port3F = value; break;
				case 0x7E:
				case 0x7F: PSG.WritePsgData(value, Cpu.TotalExecutedCycles); break;
				case 0xBE: Vdp.WriteVdpData(value); break;
				case 0xBD:
				case 0xBF: Vdp.WriteVdpControl(value); break;
				case 0xF0: if (HasYM2413) YM2413.RegisterLatch = value; break;
				case 0xF1: if (HasYM2413) YM2413.Write(value); break;
				case 0xF2: if (HasYM2413) YM2413.DetectionValue = value; break;
			}
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
				_lagcount++;
				islag = true;
			}
			else
				islag = false;
		}

		public void SaveStateText(TextWriter writer)
		{
			writer.WriteLine("[SMS]\n");
			Cpu.SaveStateText(writer);
			PSG.SaveStateText(writer);
			Vdp.SaveStateText(writer);

			writer.WriteLine("Frame {0}", Frame);
			writer.WriteLine("Lag {0}", _lagcount);
			writer.WriteLine("IsLag {0}", islag);
			writer.WriteLine("Bank0 {0}", RomBank0);
			writer.WriteLine("Bank1 {0}", RomBank1);
			writer.WriteLine("Bank2 {0}", RomBank2);
			writer.Write("RAM ");
			SystemRam.SaveAsHex(writer);
			writer.WriteLine("Port01 {0:X2}", Port01);
			writer.WriteLine("Port02 {0:X2}", Port02);
			writer.WriteLine("Port3E {0:X2}", Port3E);
			writer.WriteLine("Port3F {0:X2}", Port3F);
			int SaveRamLen = Util.SaveRamBytesUsed(SaveRAM);
			if (SaveRamLen > 0)
			{
				writer.Write("SaveRAM ");
				SaveRAM.SaveAsHex(writer, SaveRamLen);
			}
			if (ExtRam != null)
			{
				writer.Write("ExtRAM ");
				ExtRam.SaveAsHex(writer, ExtRam.Length);
			}
			if (HasYM2413)
			{
				writer.Write("FMRegs ");
				YM2413.opll.reg.SaveAsHex(writer);
			}
			writer.WriteLine("[/SMS]");
		}

		public void LoadStateText(TextReader reader)
		{
			while (true)
			{
				string[] args = reader.ReadLine().Split(' ');
				if (args[0].Trim() == "") continue;
				if (args[0] == "[SMS]") continue;
				if (args[0] == "[/SMS]") break;
				if (args[0] == "Bank0")
					RomBank0 = byte.Parse(args[1]);
				else if (args[0] == "Bank1")
					RomBank1 = byte.Parse(args[1]);
				else if (args[0] == "Bank2")
					RomBank2 = byte.Parse(args[1]);
				else if (args[0] == "Frame")
					Frame = int.Parse(args[1]);
				else if (args[0] == "Lag")
					_lagcount = int.Parse(args[1]);
				else if (args[0] == "IsLag")
					islag = bool.Parse(args[1]);
				else if (args[0] == "RAM")
					SystemRam.ReadFromHex(args[1]);
				else if (args[0] == "SaveRAM")
				{
					for (int i = 0; i < SaveRAM.Length; i++) SaveRAM[i] = 0;
					SaveRAM.ReadFromHex(args[1]);
				}
				else if (args[0] == "ExtRAM")
				{
					for (int i = 0; i < ExtRam.Length; i++) ExtRam[i] = 0;
					ExtRam.ReadFromHex(args[1]);
				}
				else if (args[0] == "FMRegs")
				{
					byte[] regs = new byte[YM2413.opll.reg.Length];
					regs.ReadFromHex(args[1]);
					for (byte i = 0; i < regs.Length; i++)
						YM2413.Write(i, regs[i]);
				}
				else if (args[0] == "Port01")
					Port01 = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "Port02")
					Port02 = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "Port3E")
					Port3E = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "Port3F")
					Port3F = byte.Parse(args[1], NumberStyles.HexNumber);
				else if (args[0] == "[Z80]")
					Cpu.LoadStateText(reader);
				else if (args[0] == "[PSG]")
					PSG.LoadStateText(reader);
				else if (args[0] == "[VDP]")
					Vdp.LoadStateText(reader);
				else
					Console.WriteLine("Skipping unrecognized identifier " + args[0]);
			}
		}

		public byte[] SaveStateBinary()
		{
			int buflen = 24808 + 16384 + 16384;
			if (ExtRam != null)
				buflen += ExtRam.Length;
			var buf = new byte[buflen];
			var stream = new MemoryStream(buf);
			var writer = new BinaryWriter(stream);
			SaveStateBinary(writer);
			if (stream.Length != buf.Length)
				throw new Exception(string.Format("savestate buffer underrun: {0} < {1}", stream.Length, buf.Length));
			writer.Close();
			return buf;
		}

		public bool BinarySaveStatesPreferred { get { return false; } }

		public void SaveStateBinary(BinaryWriter writer)
		{
			Cpu.SaveStateBinary(writer);
			PSG.SaveStateBinary(writer);
			Vdp.SaveStateBinary(writer);

			writer.Write(Frame);
			writer.Write(_lagcount);
			writer.Write(islag);
			writer.Write(RomBank0);
			writer.Write(RomBank1);
			writer.Write(RomBank2);
			writer.Write(SystemRam);
			writer.Write(SaveRAM);
			writer.Write(Port01);
			writer.Write(Port02);
			writer.Write(Port3E);
			writer.Write(Port3F);
			if (ExtRam != null)
				writer.Write(ExtRam);
			writer.Write(YM2413.opll.reg);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			Cpu.LoadStateBinary(reader);
			PSG.LoadStateBinary(reader);
			Vdp.LoadStateBinary(reader);

			Frame = reader.ReadInt32();
			_lagcount = reader.ReadInt32();
			islag = reader.ReadBoolean();
			RomBank0 = reader.ReadByte();
			RomBank1 = reader.ReadByte();
			RomBank2 = reader.ReadByte();
			SystemRam = reader.ReadBytes(SystemRam.Length);
			reader.Read(SaveRAM, 0, SaveRAM.Length);
			Port01 = reader.ReadByte();
			Port02 = reader.ReadByte();
			Port3E = reader.ReadByte();
			Port3F = reader.ReadByte();
			if (ExtRam != null)
				reader.Read(ExtRam, 0, ExtRam.Length);
			if (HasYM2413)
			{
				byte[] regs = new byte[YM2413.opll.reg.Length];
				reader.Read(regs, 0, regs.Length);
				for (byte i = 0; i < regs.Length; i++)
					YM2413.Write(i, regs[i]);
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

		readonly string[] validRegions = { "Export", "Japan" };

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
			var SaveRamDomain = new MemoryDomain("Save RAM", SaveRAM.Length, MemoryDomain.Endian.Little,
				addr => SaveRAM[addr],
				(addr, value) => { SaveRAM[addr] = value; SaveRamModified = true; });
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
			domains.Add(SaveRamDomain);
			domains.Add(SystemBusDomain);
			memoryDomains = new MemoryDomainList(domains);
		}

		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }

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
			public bool ExportRegion = true;
			public bool UsePAL = false;

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
					x.ExportRegion != y.ExportRegion ||
					x.UsePAL != y.UsePAL;
			}
		}
	}
}
