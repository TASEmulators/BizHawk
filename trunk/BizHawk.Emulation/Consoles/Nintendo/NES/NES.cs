using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class NES : IEmulator
	{
		//hardware
		MOS6502 cpu = new MOS6502();
		byte[] rom;
		byte[] ram;

		public byte ReadPPUReg(int addr)
		{
			return 0xFF;
		}

		public byte ReadReg(int addr)
		{
			return 0xFF;
		}

		public byte ReadMemory(ushort addr)
		{
			if (addr < 0x0800) return ram[addr];
			if (addr < 0x1000) return ram[addr - 0x0800];
			if (addr < 0x1800) return ram[addr - 0x1000];
			if (addr < 0x2000) return ram[addr - 0x1800];
			if (addr < 0x4000) return ReadPPUReg(addr & 7);
			if (addr < 0x4020) return ReadReg(addr - 0x4020);
			if (addr < 0x6000) return 0xFF; //exp rom
			if (addr < 0x8000) return 0xFF; //sram
			return 0xFF; //got tired of doing this
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x0800) ram[addr] = value;
			if (addr < 0x1000) ram[addr - 0x0800] = value;
			if (addr < 0x1800) ram[addr - 0x1000] = value;
			if (addr < 0x2000) ram[addr - 0x1800] = value;
		}


		public NES()
		{
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
		}

		class MyVideoProvider : IVideoProvider
		{
			NES emu;
			public MyVideoProvider(NES emu)
			{
				this.emu = emu;
			}

			public int[] GetVideoBuffer()
			{
				int testval = 0;
				if (emu.Controller.IsPressed("DOWN")) testval = 0xFF;

				int[] pixels = new int[256 * 256];
				int i = 0;
				for (int y = 0; y < 256; y++)
					for (int x = 0; x < 256; x++)
					{
						pixels[i++] = testval;
					}
				return pixels;
			}
			public int BufferWidth { get { return 256; } }
			public int BufferHeight { get { return 256; } }
			public int BackgroundColor { get { return 0; } }
		}
		public IVideoProvider VideoProvider { get { return new MyVideoProvider(this); } }


		public ISoundProvider SoundProvider { get { return new NullEmulator(); } }

		public static readonly ControllerDefinition NESController =
			new ControllerDefinition
			{
				Name = "NES Controls",
				BoolButtons = { "A","B","SELECT","START","LEFT","UP","DOWN","RIGHT", "RESET" }
			};

		public ControllerDefinition ControllerDefinition { get { return NESController; } }

		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value; }
		}

		public void LoadGame(IGame game)
		{
			rom = game.GetRomData();
			
			//parse iNes and UNIF!
			//setup banks and stuff! oh crap what a pain! lets start with non-mapper games and add mappers later.

			HardReset();
		}

		public void FrameAdvance(bool render)
		{
			//TODO!
			//cpu.Execute(10000);
		}

		public void HardReset()
		{
			cpu.Reset();
			ram = new byte[0x800];
			for (int i = 0; i < 0x800; i++)
				ram[i] = 0xFF;
		}

		public int Frame
		{
			get { return 0; }
		}
		public bool DeterministicEmulation { get { return true; } set { } }

		public byte[] SaveRam { get { return null; } }
		public bool SaveRamModified
		{
			get { return false; }
			set { }
		}

		public void SaveStateText(TextWriter writer)
		{
		}

		public void LoadStateText(TextReader reader)
		{
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
		}

		public void LoadStateBinary(BinaryReader reader)
		{
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public string SystemId { get { return "NES"; } }
		public IList<MemoryDomain> MemoryDomains { get { throw new NotImplementedException(); } }
		public MemoryDomain MainMemory { get { throw new NotImplementedException(); } }


		public object Query(EmulatorQuery query)
		{
			return null;
		}
	}
}