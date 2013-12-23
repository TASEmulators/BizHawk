using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components;
using BizHawk.Emulation.Common.Components.Z80;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public sealed partial class ColecoVision : IEmulator
	{
		// ROM
		public byte[] RomData;
		public int RomLength;

		public byte[] BiosRom;

		// Machine
		public Z80A Cpu;
		public TMS9918A VDP;
		public SN76489 PSG;
		public byte[] Ram = new byte[1024];

		public ColecoVision(CoreComm comm, GameInfo game, byte[] rom, bool skipbios)
		{
			CoreComm = comm;

			Cpu = new Z80A();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.ReadHardware = ReadPort;
			Cpu.WriteHardware = WritePort;

			VDP = new TMS9918A(Cpu);
			PSG = new SN76489();

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			string biosPath = CoreComm.CoreFileProvider.GetFirmwarePath("Coleco", "Bios", true, "Coleco BIOS file is required.");
			BiosRom = File.ReadAllBytes(biosPath);

			if (game["NoSkip"])
				skipbios = false;
			LoadRom(rom, skipbios);
			this.game = game;
			SetupMemoryDomains();
		}

		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }
		MemoryDomainList memoryDomains;
		const ushort RamSizeMask = 0x03FF;
		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomain("Main RAM", Ram.Length, MemoryDomain.Endian.Little,
				addr => Ram[addr & RamSizeMask],
				(addr, value) => Ram[addr & RamSizeMask] = value);
			var VRamDomain = new MemoryDomain("Video RAM", VDP.VRAM.Length, MemoryDomain.Endian.Little,
				addr => VDP.VRAM[addr & 0x3FFF],
				(addr, value) => VDP.VRAM[addr & 0x3FFF] = value);
			var SystemBusDomain = new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr => Cpu.ReadMemory((ushort)addr),
				(addr, value) => Cpu.WriteMemory((ushort)addr, value));

			domains.Add(MainMemoryDomain);
			domains.Add(VRamDomain);
			domains.Add(SystemBusDomain);
			memoryDomains = new MemoryDomainList(domains);
		}

		public void FrameAdvance(bool render, bool renderSound)
		{
			Frame++;
			islag = true;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);
			VDP.ExecuteFrame();
			PSG.EndFrame(Cpu.TotalExecutedCycles);

			if (islag)
				LagCount++;
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

		void LoadRom(byte[] rom, bool skipbios)
		{
			RomData = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
				RomData[i] = rom[i % rom.Length];

			// hack to skip colecovision title screen
			if (skipbios)
			{
				RomData[0] = 0x55;
				RomData[1] = 0xAA;
			}
		}

		byte ReadPort(ushort port)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
					return VDP.ReadData();
				return VDP.ReadVdpStatus();
			}

			if (port >= 0xE0)
			{
				if ((port & 1) == 0)
					return ReadController1();
				return ReadController2();
			}

			return 0xFF;
		}

		void WritePort(ushort port, byte value)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port <= 0xBF)
			{
				if ((port & 1) == 0)
					VDP.WriteVdpData(value);
				else
					VDP.WriteVdpControl(value);
				return;
			}

			if (port >= 0x80 && port <= 0x9F)
			{
				InputPortSelection = InputPortMode.Right;
				return;
			}

			if (port >= 0xC0 && port <= 0xDF)
			{
				InputPortSelection = InputPortMode.Left;
				return;
			}

			if (port >= 0xE0)
			{
				PSG.WritePsgData(value, Cpu.TotalExecutedCycles);
				return;
			}
		}

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified { get; set; }

		public bool DeterministicEmulation { get { return true; } }

		public void SaveStateText(TextWriter writer)
		{
			writer.WriteLine("[Coleco]\n");
			Cpu.SaveStateText(writer);
			PSG.SaveStateText(writer);
			VDP.SaveStateText(writer);

			writer.WriteLine("Frame {0}", Frame);
			writer.WriteLine("Lag {0}", _lagcount);
			writer.WriteLine("islag {0}", islag);
			writer.Write("RAM ");
			Ram.SaveAsHex(writer);
			writer.WriteLine("[/Coleco]");
		}

		public void LoadStateText(TextReader reader)
		{
			while (true)
			{
				string[] args = reader.ReadLine().Split(' ');
				if (args[0].Trim() == "") continue;
				if (args[0] == "[Coleco]") continue;
				if (args[0] == "[/Coleco]") break;
				else if (args[0] == "Frame")
					Frame = int.Parse(args[1]);
				else if (args[0] == "Lag")
					_lagcount = int.Parse(args[1]);
				else if (args[0] == "islag")
					islag = bool.Parse(args[1]);
				else if (args[0] == "RAM")
					Ram.ReadFromHex(args[1]);
				else if (args[0] == "[Z80]")
					Cpu.LoadStateText(reader);
				else if (args[0] == "[PSG]")
					PSG.LoadStateText(reader);
				else if (args[0] == "[VDP]")
					VDP.LoadStateText(reader);
				else
					Console.WriteLine("Skipping unrecognized identifier " + args[0]);
			}
		}

		public byte[] SaveStateBinary()
		{
			var buf = new byte[24802 + 16384 + 16384];
			var stream = new MemoryStream(buf);
			var writer = new BinaryWriter(stream);
			SaveStateBinary(writer);
			writer.Close();
			return buf;
		}

		public bool BinarySaveStatesPreferred { get { return false; } }

		public void SaveStateBinary(BinaryWriter writer)
		{
			Cpu.SaveStateBinary(writer);
			PSG.SaveStateBinary(writer);
			VDP.SaveStateBinary(writer);

			writer.Write(Frame);
			writer.Write(_lagcount);
			writer.Write(islag);
			writer.Write(Ram);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			Cpu.LoadStateBinary(reader);
			PSG.LoadStateBinary(reader);
			VDP.LoadStateBinary(reader);

			Frame = reader.ReadInt32();
			_lagcount = reader.ReadInt32();
			islag = reader.ReadBoolean();
			Ram = reader.ReadBytes(Ram.Length);
		}

		public void Dispose() { }
		public void ResetCounters()
		{
			Frame = 0;
			_lagcount = 0;
			islag = false;
		}

		public string SystemId { get { return "Coleco"; } }
		public GameInfo game;
		public CoreComm CoreComm { get; private set; }
		public IVideoProvider VideoProvider { get { return VDP; } }
		public ISoundProvider SoundProvider { get { return PSG; } }

		public string BoardName { get { return null; } }

		public ISyncSoundProvider SyncSoundProvider { get { return null; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public object GetSettings() { return null; }
		public object GetSyncSettings() { return null; }
		public bool PutSettings(object o) { return false; }
		public bool PutSyncSettings(object o) { return false; }
	}
}