using System;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.CP1610;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision : IEmulator
	{
		byte[] Rom;
		GameInfo Game;

		CP1610 Cpu;
		ICart Cart;
		STIC Stic;
		PSG Psg;

		public void Connect()
		{
			Cpu.SetIntRM(Stic.GetSr1());
			Cpu.SetBusRq(Stic.GetSr2());
			Stic.SetSst(Cpu.GetBusAk());
		}

		public void LoadExecutiveRom()
		{
			FileStream fs = new FileStream("C:/erom.int", FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			byte[] erom = r.ReadBytes(8192);
			int index = 0;
			// Combine every two bytes into a word.
			while (index + 1 < erom.Length)
				ExecutiveRom[index / 2] = (ushort)((erom[index++] << 8) | erom[index++]);
			r.Close();
			fs.Close();
		}

		public void LoadGraphicsRom()
		{
			FileStream fs = new FileStream("C:/grom.int", FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			byte[] grom = r.ReadBytes(2048);
			for (int index = 0; index < grom.Length; index++)
				GraphicsRom[index] = grom[index];
			r.Close();
			fs.Close();
		}

		public Intellivision(GameInfo game, byte[] rom)
		{
			Rom = rom;
			Game = game;
			LoadExecutiveRom();
			LoadGraphicsRom();
			Cart = new Intellicart();
			if (Cart.Parse(Rom) == -1)
			{
				Cart = new Cartridge();
				Cart.Parse(Rom);
			}

			Cpu = new CP1610();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.Reset();

			Stic = new STIC();
			Stic.Reset();

			Psg = new PSG();

			Connect();

			CoreOutputComm = new CoreOutputComm();

			Cpu.LogData();
		}

		public void FrameAdvance(bool render)
		{
			Cpu.AddPendingCycles(14394 + 3791);
			while (Cpu.GetPendingCycles() > 0)
			{
				int cycles = Cpu.Execute();
				Stic.Execute(cycles);
				Connect();
				Cpu.LogData();
			}
		}



		// This is all crap to worry about later.

		public IVideoProvider VideoProvider { get { return new NullEmulator(); } }
		public ISoundProvider SoundProvider { get { return NullSound.SilenceProvider; } }

		public ControllerDefinition ControllerDefinition
		{
			get { return null; }
		}

		public IController Controller { get; set; }


		public int Frame
		{
			get { return 0; }
		}

		public int LagCount
		{
			get { return 0; }
			set { }
		}

		public bool IsLagFrame { get { return false; } }
		public string SystemId
		{
			get { return "INTV"; }
		}

		public bool DeterministicEmulation { get; set; }

		public byte[] SaveRam { get { return null; } }

		public bool SaveRamModified
		{
			get { return false; }
			set { }
		}

		public void ResetFrameCounter()
		{
		}

		public void SaveStateText(TextWriter writer)
		{
			throw new NotImplementedException();
		}

		public void LoadStateText(TextReader reader)
		{
			throw new NotImplementedException();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }

		public IList<MemoryDomain> MemoryDomains
		{
			get { throw new NotImplementedException(); }
		}

		public MemoryDomain MainMemory
		{
			get { throw new NotImplementedException(); }
		}

		public void Dispose()
		{
		}
	}
}