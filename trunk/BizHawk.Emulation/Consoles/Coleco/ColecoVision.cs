using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.Z80;
using BizHawk.Emulation.Sound;

namespace BizHawk.Emulation.Consoles.Coleco
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

		public ColecoVision(GameInfo game, byte[] rom, string biosPath, bool skipbios)
		{
			Cpu = new Z80A();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.ReadHardware = ReadPort;
			Cpu.WriteHardware = WritePort;
            Cpu.Logger = (s) => Log.Error("COL", s);
			//Cpu.Debug = true;

			VDP = new TMS9918A(Cpu);
			PSG = new SN76489();

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			BiosRom = File.ReadAllBytes(biosPath);

			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();
             
            if (game["NoSkip"])
                skipbios = false;
            Console.WriteLine("skipbios = {0}", skipbios);
			LoadRom(rom, skipbios);
			this.game = game;
			SetupMemoryDomains();
		}

		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }
		IList<MemoryDomain> memoryDomains;
		const ushort RamSizeMask = 0x03FF;
		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomain("Main RAM", Ram.Length, Endian.Little,
				addr => Ram[addr & RamSizeMask],
				(addr, value) => Ram[addr & RamSizeMask] = value);
			var VRamDomain = new MemoryDomain("Video RAM", VDP.VRAM.Length, Endian.Little,
				addr => VDP.VRAM[addr & 0x3FFF],
				(addr, value) => VDP.VRAM[addr & 0x3FFF] = value);
			var SystemBusDomain = new MemoryDomain("System Bus", 0x10000, Endian.Little,
				addr => Cpu.ReadMemory((ushort)addr),
				(addr, value) => Cpu.WriteMemory((ushort)addr, value));

			domains.Add(MainMemoryDomain);
			domains.Add(VRamDomain);
			domains.Add(SystemBusDomain);
			memoryDomains = domains.AsReadOnly();
		}

		public void FrameAdvance(bool render, bool renderSound)
		{
			Frame++;
			IsLagFrame = true;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);
			VDP.ExecuteFrame();
			PSG.EndFrame(Cpu.TotalExecutedCycles);

			if (IsLagFrame)
				LagCount++;
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
			writer.WriteLine("IsLagFrame {0}", IsLagFrame);
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
				else if (args[0] == "IsLagFrame")
					IsLagFrame = bool.Parse(args[1]);
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

		public void SaveStateBinary(BinaryWriter writer)
		{
			Cpu.SaveStateBinary(writer);
			PSG.SaveStateBinary(writer);
			VDP.SaveStateBinary(writer);

			writer.Write(Frame);
			writer.Write(_lagcount);
			writer.Write(IsLagFrame);
			writer.Write(Ram);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			Cpu.LoadStateBinary(reader);
			PSG.LoadStateBinary(reader);
			VDP.LoadStateBinary(reader);

			Frame = reader.ReadInt32();
			_lagcount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			Ram = reader.ReadBytes(Ram.Length);
		}

		public void Dispose() { }
		public void ResetFrameCounter() { }

		public string SystemId { get { return "Coleco"; } }
		public GameInfo game;
		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }
		public IVideoProvider VideoProvider { get { return VDP; } }
		public ISoundProvider SoundProvider { get { return PSG; } }

		public ISyncSoundProvider SyncSoundProvider { get { return null; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }
	}
}