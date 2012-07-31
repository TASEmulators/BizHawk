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

		public void LoadExecutive_ROM()
		{
			FileStream fs = new FileStream("C:/erom.int", FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			byte[] erom = r.ReadBytes(8192);
			int index = 0;
			// Combine every two bytes into a word.
			while (index + 1 < erom.Length)
				Executive_ROM[index / 2] = (ushort)((erom[index++] << 8) | erom[index++]);
			r.Close();
			fs.Close();
		}

		public void LoadGraphics_ROM()
		{
			FileStream fs = new FileStream("C:/grom.int", FileMode.Open, FileAccess.Read);
			BinaryReader r = new BinaryReader(fs);
			byte[] grom = r.ReadBytes(2048);
			for (int index = 0; index < grom.Length; index++)
				Graphics_ROM[index] = grom[index];
			r.Close();
			fs.Close();
		}

		public Intellivision(GameInfo game, byte[] rom)
		{
			Rom = rom;
			Game = game;
			LoadExecutive_ROM();
			LoadGraphics_ROM();
			Cart = new Intellicart();
			if (Cart.Parse(Rom) == -1)
			{
				Cart = new Cartridge();
				Cart.Parse(Rom);
			}

			Cpu = new CP1610();
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.RegisterPC = 0x1000;
			Cpu.LogData();

			CoreOutputComm = new CoreOutputComm();
		}

		public void FrameAdvance(bool render)
		{
			Cpu.Execute(999); // execute some cycles. this will do nothing useful until a memory mapper is created.
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