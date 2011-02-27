using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public partial class NES : IEmulator
	{
		public interface INESBoard
		{
			byte ReadPRG(int addr);
			byte ReadPPU(int addr);
			void WritePRG(int addr, byte value);
			void WritePPU(int addr, byte value);
			void Initialize(RomInfo romInfo, NES nes);
		};

		public abstract class NESBoardBase : INESBoard
		{
			public void Initialize(RomInfo romInfo, NES nes)
			{
				this.RomInfo = romInfo;
				this.NES = nes;
				switch (romInfo.Mirroring)
				{
					case 0: SetMirroring(0, 0, 1, 1); break;
					case 1: SetMirroring(0, 1, 0, 1); break;
					default: SetMirroring(-1, -1, -1, -1); break; //crash!
				}
			}
			public RomInfo RomInfo { get; set; }
			public NES NES { get; set; }

			int[] mirroring = new int[4];
			protected void SetMirroring(int a, int b, int c, int d)
			{
				mirroring[0] = a;
				mirroring[1] = b;
				mirroring[2] = c;
				mirroring[3] = d;
			}


			public virtual byte ReadPRG(int addr) { return RomInfo.ROM[addr];}
			public virtual void WritePRG(int addr, byte value) { }


			public virtual void WritePPU(int addr, byte value)
			{
				if (addr < 0x2000)
				{
				}
				else
				{
					int block = (addr >> 10) & 3;
					block = mirroring[block];
					int ofs = addr & 0x3FF;
					NES.ppu.NTARAM[(block << 10) | ofs] = value;
				}
			}

			public virtual byte ReadPPU(int addr)
			{
				if (addr < 0x2000)
				{
					return RomInfo.VROM[addr];
				}
				else
				{
					int block = (addr >> 10)&3;
					block = mirroring[block];
					int ofs = addr & 0x3FF;
					return NES.ppu.NTARAM[(block << 10) | ofs];
				}
			}
		}

		//hardware
		protected MOS6502 cpu;
		INESBoard board;
		PPU ppu;
		RomInfo romInfo;
		byte[] ram;

		IPortDevice[] ports;

		//user configuration 
		int[,] palette; //TBD!!

		public byte ReadPPUReg(int addr)
		{
			return ppu.ReadReg(addr);
		}

		public byte ReadReg(int addr)
		{
			switch (addr)
			{
				case 0x4016: 
				case 0x4017:
					return read_joyport(addr);
				default:
					//Console.WriteLine("read register: {0:x4}", addr);
					break;

			}
			return 0xFF;
		}

		void WritePPUReg(int addr, byte val)
		{
			ppu.WriteReg(addr,val);
		}
		
		void WriteReg(int addr, byte val)
		{
			switch (addr)
			{
				case 0x4014: Exec_OAMDma(val); break;
				case 0x4016:
					ports[0].Write(val & 1);
					ports[1].Write(val & 1);
					break;
				default:
					//Console.WriteLine("wrote register: {0:x4} = {1:x2}", addr, val);
					break;
			}
		}

		byte read_joyport(int addr)
		{
			//read joystick port
			//many todos here
			if (addr == 0x4016)
			{
				byte ret = ports[0].Read();
				return ret;
			}
			else return 0;
		}

		void Exec_OAMDma(byte val)
		{
			ushort addr = (ushort)(val << 8);
			for (int i = 0; i < 256; i++)
			{
				byte db = ReadMemory((ushort)addr);
				WriteMemory(0x2004, db);
				addr++;
			}
			cpu.PendingCycles-=512;
		}

		public byte ReadMemory(ushort addr)
		{
			if (addr < 0x0800) return ram[addr];
			else if (addr < 0x1000) return ram[addr - 0x0800];
			else if (addr < 0x1800) return ram[addr - 0x1000];
			else if (addr < 0x2000) return ram[addr - 0x1800];
			else if (addr < 0x4000) return ReadPPUReg(addr & 7);
			else if (addr < 0x4020) return ReadReg(addr); //we're not rebasing the register just to keep register names canonical
			else if (addr < 0x6000) return 0xFF; //exp rom
			else if (addr < 0x8000) return 0xFF; //sram
			else return board.ReadPRG(addr - 0x8000);
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x0800) ram[addr] = value;
			else if (addr < 0x1000) ram[addr - 0x0800] = value;
			else if (addr < 0x1800) ram[addr - 0x1000] = value;
			else if (addr < 0x2000) ram[addr - 0x1800] = value;
			else if (addr < 0x4000) WritePPUReg(addr & 7,value);
			else if (addr < 0x4020) WriteReg(addr, value);  //we're not rebasing the register just to keep register names canonical
			else if (addr < 0x6000) { } //exp rom
			else if (addr < 0x8000) { } //sram
			else board.WritePRG(addr - 0x8000, value);
		}


		public NES()
		{
			palette = Palettes.FCEUX_Standard;
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
				int[] pixels = new int[256 * 256];
				int i = 0;
				for (int y = 0; y < 256; y++)
					for (int x = 0; x < 256; x++)
					{
						int pixel = emu.ppu.xbuf[i];
						int deemph = pixel >> 8;
						int palentry = pixel & 0xFF;
						int r = emu.palette[pixel, 0];
						int g = emu.palette[pixel, 1];
						int b = emu.palette[pixel, 2];
						Palettes.ApplyDeemphasis(ref r, ref g, ref b, deemph);
						pixels[i] = (r<<16)|(g<<8)|b;
						i++;
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
				BoolButtons = { "A","B","Select","Start","Left","Up","Down","Right", "Reset" }
			};

		public ControllerDefinition ControllerDefinition { get { return NESController; } }

		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value; }
		}

		public void FrameAdvance(bool render)
		{
			//TODO!
			//cpu.Execute(10000);
			ppu.FrameAdvance();
		}

		protected void RunCpu(int cycles)
		{
			cpu.Execute(cycles);
		}

		interface IPortDevice
		{
			void Write(int value);
			byte Read();
			void Update();
		}

		//static INPUTC GPC = { ReadGP, 0, StrobeGP, UpdateGP, 0, 0, LogGP, LoadGP };
		class JoypadPortDevice : NullPortDevice
		{
			int state;
			NES nes;
			public JoypadPortDevice(NES nes)
			{
				this.nes = nes;
			}
			void Strobe()
			{
				value = 0;
				foreach (string str in new string[] { "Right", "Left", "Down", "Up", "Start", "Select", "B", "A" })
				{
					value <<= 1;
					value |= nes.Controller.IsPressed(str) ? 1 : 0;
				}
			}
			public override void Write(int value)
			{
				if (state == 1 && value == 0)
					Strobe();
				state = value;
			}
			public override byte Read()
			{
				int ret = value&1;
				value >>= 1;
				return (byte)ret;
			}
			public override void Update()
			{

			}
			int value;
		}

		class NullPortDevice : IPortDevice
		{
			public virtual void Write(int value)
			{
			}
			public virtual byte Read()
			{
				return 0xFF;
			}
			public virtual void Update()
			{
			}
		}

		public void HardReset()
		{
			cpu = new MOS6502();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			ppu = new PPU(this);
			ram = new byte[0x800];
			ports = new IPortDevice[2];
			ports[0] = new JoypadPortDevice(this);
			ports[1] = new NullPortDevice();

			//fceux uses this technique, which presumably tricks some games into thinking the memory is randomized
			for (int i = 0; i < 0x800; i++)
			{
				if ((i & 4) != 0) ram[i] = 0xFF; else ram[i] = 0x00;
			}

			//in this emulator, reset takes place instantaneously
			cpu.PC = (ushort)(ReadMemory(0xFFFC) | (ReadMemory(0xFFFD) << 8));

			//cpu.debug = true;
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
		public IList<MemoryDomain> MemoryDomains { get { return new List<MemoryDomain>(); } }
		public MemoryDomain MainMemory
		{
			get
			{
				return new MemoryDomain("x", 8, Endian.Little,
										addr => 0,
										(addr, value) => { });
			}
		}


		public object Query(EmulatorQuery query)
		{
			return null;
		}

        public string GetControllersAsMnemonic()
        {
            return "|........|........|0|"; //TODO: implement
        }

		public class RomInfo
		{
			public int MapperNo, Mirroring, Num_PRG_Banks, Num_CHR_Banks;
			public byte[] ROM, VROM;
		}

		unsafe struct iNES_HEADER {
			public fixed byte ID[4]; /*NES^Z*/
			public byte ROM_size;
			public byte VROM_size;
			public byte ROM_type;
			public byte ROM_type2;
			public fixed byte reserve[8];

			public bool CheckID()
			{
				fixed (iNES_HEADER* self = &this)
					return 0==Util.memcmp(self, "NES\x1A", 4);
			}

			//some cleanup code recommended by fceux
			public void Cleanup()
			{
				fixed (iNES_HEADER* self = &this)
				{
					if (0==Util.memcmp((char*)(self) + 0x7, "DiskDude", 8))
					{
						Util.memset((char*)(self) + 0x7, 0, 0x9);
					}

					if (0 == Util.memcmp((char*)(self) + 0x7, "demiforce", 9))
					{
						Util.memset((char*)(self) + 0x7, 0, 0x9);
					}

					if (0 == Util.memcmp((char*)(self) + 0xA, "Ni03", 4))
					{
						if (0 == Util.memcmp((char*)(self) + 0x7, "Dis", 3))
							Util.memset((char*)(self) + 0x7, 0, 0x9);
						else
							Util.memset((char*)(self) + 0xA, 0, 0x6);
					}
				}
			}

			public RomInfo Analyze()
			{
				var ret = new RomInfo();
				ret.MapperNo = (ROM_type>>4);
				ret.MapperNo|=(ROM_type2&0xF0);
				ret.Mirroring = (ROM_type&1);
				if((ROM_type&8)!=0) ret.Mirroring=2;
				ret.Num_PRG_Banks = ROM_size;
				if (ret.Num_PRG_Banks == 0)
					ret.Num_PRG_Banks = 256;
				ret.Num_CHR_Banks = VROM_size;
				
				//fceux calls uppow2(PRG_Banks) here, and also ups the chr size as well
				//then it does something complicated that i don't understand with making sure it doesnt read too much data
				//fceux only allows this condition for mappers in the list "not_power2" which is only 228

				return ret;
			}
		}

		INESBoard Classify(RomInfo info)
		{
			//you may think that this should be table driven.. but im not so sure. 
			//i think this should be a backstop eventually, with other classification happening from the game database.
			//if the gamedatabase has an exact answer for a game then the board can be determined..
			//otherwise we might try to find a general case handler below.

			if (info.MapperNo == 0 && info.Num_CHR_Banks == 1 && info.Num_PRG_Banks == 2 && info.Mirroring == 1) return new Boards.NROM();
			return null;
		}

		public unsafe void LoadGame(IGame game)
		{
			byte[] file = game.GetRomData();
			if (file.Length < 16) throw new InvalidOperationException("Alleged NES rom too small to be anything useful");
			fixed (byte* bfile = &file[0])
			{
				var header = (iNES_HEADER*)bfile;
				if (!header->CheckID()) throw new InvalidOperationException("iNES header not found");
				header->Cleanup();

				romInfo = header->Analyze();
				board = Classify(romInfo);
				if (board == null) throw new InvalidOperationException("Couldn't classify NES rom");
				board.Initialize(romInfo, this);

				//we're going to go ahead and copy these out, just in case we need to pad them alter
				romInfo.ROM = new byte[romInfo.Num_PRG_Banks * 16 * 1024];
				romInfo.VROM = new byte[romInfo.Num_CHR_Banks * 8 * 1024];
				Array.Copy(file, 16, romInfo.ROM, 0, romInfo.ROM.Length);
				Array.Copy(file, 16 + romInfo.ROM.Length, romInfo.VROM, 0, romInfo.VROM.Length);
			}

			HardReset();
		}
	}
}