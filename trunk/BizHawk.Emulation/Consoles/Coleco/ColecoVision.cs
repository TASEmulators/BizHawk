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

		public ColecoVision(GameInfo game, byte[] rom, string biosPath)
		{
			Cpu = new Z80A();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.ReadHardware = ReadPort;
			Cpu.WriteHardware = WritePort;
			Cpu.Logger = (s) => Console.WriteLine(s);
			//Cpu.Debug = true;

			VDP = new TMS9918A(Cpu);
			PSG = new SN76489();

			// TODO: hack to allow bios-less operation would be nice, no idea if its feasible
			BiosRom = File.ReadAllBytes(biosPath);

			CoreOutputComm = new CoreOutputComm();
			CoreInputComm = new CoreInputComm();

			LoadRom(rom);
			this.game = game;

			Reset();
		}

		public void FrameAdvance(bool render, bool renderSound)
		{
			PSG.BeginFrame(Cpu.TotalExecutedCycles);
			VDP.ExecuteFrame();
			PSG.EndFrame(Cpu.TotalExecutedCycles);
		}

		void LoadRom(byte[] rom)
		{
			RomData = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
				RomData[i] = rom[i % rom.Length];
		}

		void Reset()
		{
			/*Cpu.RegisterPC = Cpu.ReadWord(0x800A);
			Console.WriteLine("code start vector = {0:X4}", Cpu.RegisterPC);*/
		}

		byte ReadPort(ushort port)
		{
			port &= 0xFF;
			//Console.WriteLine("Read port {0:X2}", port);

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
					return VDP.ReadData();
				return VDP.ReadVdpStatus();
			}

			return 0xFF;
		}

		void WritePort(ushort port, byte value)
		{
			port &= 0xFF;

			if (port >= 0xA0 && port < 0xC0)
			{
				if ((port & 1) == 0)
					VDP.WriteVdpData(value);
				else
					VDP.WriteVdpControl(value);
				return;
			}

			if (port >= 0xE0)
			{
				PSG.WritePsgData(value, Cpu.TotalExecutedCycles);
				return;
			}


			//Console.WriteLine("Write port {0:X2}:{1:X2}", port, value);
		}

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified { get; set; }

		public bool DeterministicEmulation { get { return true; } }
		public void SaveStateText(TextWriter writer) { }
		public void LoadStateText(TextReader reader) { }
		public void SaveStateBinary(BinaryWriter bw) { }
		public void LoadStateBinary(BinaryReader br) { }

		public byte[] SaveStateBinary()
		{
			return new byte[0];
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

		public IList<MemoryDomain> MemoryDomains { get { return null; } }
		public MemoryDomain MainMemory { get { return null; } }
	}
}
