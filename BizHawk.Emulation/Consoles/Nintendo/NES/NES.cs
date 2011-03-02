using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public partial class NES : IEmulator
	{
        //Game issues:
        //3-D World Runner - UNROM - weird lines in gameplay (scanlines off?)
        //JJ - Tobidase Daisakusen Part 2 (J) - same as 3-D World Runner
        //Castlevania II (U) - Black screen only
        //Zelda II (U) - Black screen only
        //Bard's Tale - The Tales of the Unkown (U) - Black screen only


        //the main rom class that contains all information necessary for the board to operate
		public class RomInfo
		{
			public enum EHeaderType
			{
				None, INes
			}

			public EHeaderType HeaderType;
			public int PRG_Size = -1, CHR_Size = -1;
			public int CRAM_Size = -1, PRAM_Size = -1;
			public string BoardName;
			public EMirrorType MirrorType;
			public bool Battery;

			public int MapperNumber; //it annoys me that this junky mapper number is even in here. it might be nice to wrap this class in something else to contain the MapperNumber

			public string MD5;

			public byte[] ROM, VROM;
		}

		public enum EMirrorType
		{
			Vertical, Horizontal,
			OneScreenA, OneScreenB,
			//unknown or controlled by the board
			External
		}

		public interface INESBoard
		{
			byte ReadPRG(int addr);
			byte ReadPPU(int addr);
			byte ReadPRAM(int addr);
			void WritePRG(int addr, byte value);
			void WritePPU(int addr, byte value);
			void WritePRAM(int addr, byte value);
			void Initialize(RomInfo romInfo, NES nes);
			byte[] SaveRam { get; }
			void SaveStateBinary(BinaryWriter bw);
			void LoadStateBinary(BinaryReader br);
		};

		public abstract class NESBoardBase : INESBoard
		{
			public virtual void Initialize(RomInfo romInfo, NES nes)
			{
				this.RomInfo = romInfo;
				this.NES = nes;
				SetMirrorType(romInfo.MirrorType);
			}
			public RomInfo RomInfo { get; set; }
			public NES NES { get; set; }

			public virtual void SaveStateBinary(BinaryWriter bw)
			{
				for (int i = 0; i < 4; i++) bw.Write(mirroring[i]);
			}
			public virtual void LoadStateBinary(BinaryReader br)
			{
				for (int i = 0; i < 4; i++) mirroring[i] = br.ReadInt32();
			}

			int[] mirroring = new int[4];
			protected void SetMirroring(int a, int b, int c, int d)
			{
				mirroring[0] = a;
				mirroring[1] = b;
				mirroring[2] = c;
				mirroring[3] = d;
			}

			protected void SetMirrorType(EMirrorType mirrorType)
			{
				switch (mirrorType)
				{
					case EMirrorType.Horizontal: SetMirroring(0, 0, 1, 1); break;
					case EMirrorType.Vertical: SetMirroring(0, 1, 0, 1); break;
					case EMirrorType.OneScreenA: SetMirroring(0, 0, 0, 0); break;
					case EMirrorType.OneScreenB: SetMirroring(1, 1, 1, 1); break;
					default: SetMirroring(-1, -1, -1, -1); break; //crash!
				}
			}

			int ApplyMirroring(int addr)
			{
				int block = (addr >> 10) & 3;
				block = mirroring[block];
				int ofs = addr & 0x3FF;
				return (block << 10) | ofs | 0x2000;
			}

			protected byte HandleNormalPRGConflict(int addr, byte value)
			{
				byte old_value = value;
				value &= ReadPRG(addr);
				Debug.Assert(old_value == value, "Found a test case of bus conflict. please report.");
				return value;
			}

			public virtual byte ReadPRG(int addr) { return RomInfo.ROM[addr];}
			public virtual void WritePRG(int addr, byte value) { }

			public virtual void WritePRAM(int addr, byte value) { }
			public virtual byte ReadPRAM(int addr) { return 0xFF; }


			public virtual void WritePPU(int addr, byte value)
			{
				if (addr < 0x2000)
				{
				}
				else
				{
					NES.ppu.ppu_defaultWrite(ApplyMirroring(addr), value);
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
					return NES.ppu.ppu_defaultRead(ApplyMirroring(addr));
				}
			}

			public virtual byte[] SaveRam { get { return null; } }
		}

		//hardware/state
		protected MOS6502 cpu;
		INESBoard board;
		public PPU ppu;
		byte[] ram;
		int cpu_accumulate;

		//user configuration 
		int[,] palette; //TBD!!
		IPortDevice[] ports;
		RomInfo romInfo;

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
			else if (addr < 0x8000) return board.ReadPRAM(addr);
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
			else if (addr < 0x8000) board.WritePRAM(addr,value);
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
						int r = emu.palette[palentry, 0];
						int g = emu.palette[palentry, 1];
						int b = emu.palette[palentry, 2];
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
            Controller.UpdateControls(Frame++);
            ppu.FrameAdvance();
		}

		protected void RunCpu(int cycles)
		{
			if (ppu.PAL)
				cycles *= 15;
			else
				cycles *= 16;

			cpu_accumulate += cycles;
			int todo = cpu_accumulate / 48;
			cpu_accumulate -= todo * 48;
			if(todo>0)
				cpu.Execute(todo);
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

        public int Frame { get; set; }

        private byte Port01 = 0xFF;
		public bool DeterministicEmulation { get { return true; } set { } }

		public byte[] SaveRam
		{
			get
			{
				if(board==null) return null;
				return board.SaveRam;
			}
		}
		public bool SaveRamModified
		{
			get { if (board == null) return false; if (board.SaveRam == null) return false; return true; }
			set { }
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

        public void SetControllersAsMnemonic(string mnemonic)
        {
            if (mnemonic.Length == 0) return;

            if (mnemonic[1] != '.')
                Controller.ForceButton("Up");
            if (mnemonic[2] != '.')
                Controller.ForceButton("Down");
            if (mnemonic[3] != '.')
                Controller.ForceButton("Left");
            if (mnemonic[4] != '.')
                Controller.ForceButton("Right");
            if (mnemonic[5] != '.')
                Controller.ForceButton("B");
            if (mnemonic[6] != '.')
                Controller.ForceButton("A");
            if (mnemonic[7] != '.')
                Controller.ForceButton("Select");
            if (mnemonic[8] != '.')
                Controller.ForceButton("Start");

            if (mnemonic[10] != '.' && mnemonic[10] != '0')
                Controller.ForceButton("Reset");
        }

        public string GetControllersAsMnemonic()
        {
            string input = "|";
            
            if (Controller.IsPressed("Up")) input += "U";
            else input += ".";
            if (Controller.IsPressed("Down")) input += "D";
            else input += ".";
            if (Controller.IsPressed("Left")) input += "L";
            else input += ".";
            if (Controller.IsPressed("Right")) input += "R";
            else input += ".";
            if (Controller.IsPressed("A")) input += "A";
            else input += ".";
            if (Controller.IsPressed("B")) input += "B";
            else input += ".";
            if (Controller.IsPressed("Select")) input += "s";
            else input += ".";
            if (Controller.IsPressed("Start")) input += "S";
            else input += ".";

            input += "|";

            if (Controller.IsPressed("Reset")) input += "R";
            else input += ".";

            input += "|";

            return input;
        }

		public class RomHeaderInfo
		{
			public int MapperNo, Mirroring, Num_PRG_Banks, Num_CHR_Banks, Num_PRAM_Banks;
			public bool Battery;
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
				ret.MapperNumber = (ROM_type>>4);
				ret.MapperNumber |= (ROM_type2 & 0xF0);
				int mirroring = (ROM_type&1);
				if((ROM_type&8)!=0) mirroring=2;
				if (mirroring == 0) ret.MirrorType = EMirrorType.Horizontal;
				else if (mirroring == 1) ret.MirrorType = EMirrorType.Vertical;
				else ret.MirrorType = EMirrorType.External;
				ret.PRG_Size = ROM_size;
				if (ret.PRG_Size == 0)
					ret.PRG_Size = 256;
				ret.CHR_Size = VROM_size;
				ret.Battery = (ROM_type & 2) != 0;

				fixed (iNES_HEADER* self = &this) ret.PRAM_Size = self->reserve[0] * 8;
				//0 is supposed to mean 1 (for compatibility, as this is an extension to original iNES format)
				if (ret.PRAM_Size == 0) ret.PRAM_Size = 8;

				Console.WriteLine("iNES header: map:{0}, mirror:{1}, PRG:{2}, CHR:{3}, CRAM:{4}, PRAM:{5}, bat:{6}", ret.MapperNumber, ret.MirrorType, ret.PRG_Size, ret.CHR_Size, ret.CRAM_Size, ret.PRAM_Size, ret.Battery ? 1 : 0);

				//fceux calls uppow2(PRG_Banks) here, and also ups the chr size as well
				//then it does something complicated that i don't understand with making sure it doesnt read too much data
				//fceux only allows this condition for mappers in the list "not_power2" which is only 228

				return ret;
			}
		}

		const bool ENABLE_DB = true;

		public unsafe void LoadGame(IGame game)
		{
			byte[] file = game.GetFileData();
			if (file.Length < 16) throw new InvalidOperationException("Alleged NES rom too small to be anything useful");
			fixed (byte* bfile = &file[0])
			{
				var header = (iNES_HEADER*)bfile;
				if (!header->CheckID()) throw new InvalidOperationException("iNES header not found");
				header->Cleanup();

				//now that we know we have an iNES header, we can try to ignore it.
				string hash;
				using (var md5 = System.Security.Cryptography.MD5.Create())
				{
					md5.TransformFinalBlock(file, 16, file.Length - 16);
					hash = Util.BytesToHexString(md5.Hash);
				}
				Console.WriteLine("headerless rom hash: {0}", hash);

				GameInfo gi = null;
				if (ENABLE_DB) gi = Database.CheckDatabase(hash);
				else Console.WriteLine("database check disabled");

				if (gi == null)
				{
					romInfo = header->Analyze();
					string board = BoardDetector.Detect(romInfo);
					if (board == null)
						throw new InvalidOperationException("Couldn't detect board type");
					romInfo.BoardName = board;
					Console.WriteLine("board detected as " + board);
				}
				else
				{
					Console.WriteLine("found game in database: {0}", gi.Name);
					romInfo = new RomInfo();
					romInfo.MD5 = hash;
					var dict = gi.ParseOptionsDictionary();
					if (dict.ContainsKey("board"))
						romInfo.BoardName = dict["board"];
					if (dict.ContainsKey("mirror"))
						switch (dict["mirror"])
						{
							case "V": romInfo.MirrorType = EMirrorType.Vertical; break;
							case "H": romInfo.MirrorType = EMirrorType.Horizontal; break;
							case "X": romInfo.MirrorType = EMirrorType.External; break;
							default: throw new InvalidOperationException();
						}
					else romInfo.MirrorType = EMirrorType.External;
					
					if (dict.ContainsKey("PRG"))
						romInfo.PRG_Size = int.Parse(dict["PRG"]);
					if (dict.ContainsKey("CHR"))
						romInfo.CHR_Size = int.Parse(dict["CHR"]);
					if (dict.ContainsKey("CRAM"))
						romInfo.CRAM_Size = int.Parse(dict["CRAM"]);
					if (dict.ContainsKey("PRAM"))
						romInfo.PRAM_Size = int.Parse(dict["PRAM"]);
					if (dict.ContainsKey("bat"))
						romInfo.Battery = true;
					if (dict.ContainsKey("bug"))
						Console.WriteLine("game is known to be BUGGED!!!");
				}

				//construct board (todo)
				switch (romInfo.BoardName)
				{
					case "NROM": board = new Boards.NROM(); break;
					case "UNROM": board = new Boards.UxROM("UNROM"); break;
					case "UOROM": board = new Boards.UxROM("UOROM"); break;
					case "CNROM": board = new Boards.CxROM("CNROM"); break;
					case "ANROM": board = new Boards.AxROM("ANROM"); break;
					case "AOROM": board = new Boards.AxROM("AOROM"); break;
					case "Discrete_74x377": board = new Boards.Discrete_74x377(); break;
					case "CPROM": board = new Boards.CPROM(); break;
					case "GxROM": board = new Boards.GxROM(); break;
					case "SGROM": board = new Boards.SxROM("SGROM"); break;
					case "SNROM": board = new Boards.SxROM("SNROM"); break;
					case "SL2ROM": board = new Boards.SxROM("SL2ROM"); break;
				}

				if (board == null) throw new InvalidOperationException("Couldn't classify NES rom");

				//we're going to go ahead and copy these out, just in case we need to pad them alter
				romInfo.ROM = new byte[romInfo.PRG_Size * 16 * 1024];
				Array.Copy(file, 16, romInfo.ROM, 0, romInfo.ROM.Length);
				if (romInfo.CHR_Size > 0)
				{
					romInfo.VROM = new byte[romInfo.CHR_Size * 8 * 1024];
					Array.Copy(file, 16 + romInfo.ROM.Length, romInfo.VROM, 0, romInfo.VROM.Length);
				}

				board.Initialize(romInfo, this);
			}

			HardReset();
		}

		public void SaveStateText(TextWriter writer)
		{
			writer.WriteLine("[NES]");
			byte[] lol = SaveStateBinary();
			writer.WriteLine("blob {0}", Util.BytesToHexString(lol));
			writer.WriteLine("[/NES]");
		}

		public void LoadStateText(TextReader reader)
		{
			byte[] blob = null;
			while (true)
			{
				string[] args = reader.ReadLine().Split(' ');
				if (args[0] == "blob")
					blob = Util.HexStringToBytes(args[1]);
				else if (args[0] == "[/NES]") break;
			}
			if (blob == null) throw new ArgumentException();
			LoadStateBinary(new BinaryReader(new MemoryStream(blob)));
		}


		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			using (var sw = new StringWriter())
			{
				cpu.SaveStateText(sw);
				sw.Flush();
				Util.WriteByteBuffer(bw, System.Text.Encoding.ASCII.GetBytes(sw.ToString()));
			}
			Util.WriteByteBuffer(bw,ram);
			bw.Write(cpu_accumulate);
			board.SaveStateBinary(bw);
			ppu.SaveStateBinary(bw);
			bw.Flush();
		}

		public void LoadStateBinary(BinaryReader br)
		{
			using (var sr = new StringReader(System.Text.Encoding.ASCII.GetString(Util.ReadByteBuffer(br, false))))
				cpu.LoadStateText(sr);
			ram = Util.ReadByteBuffer(br, false);
			cpu_accumulate = br.ReadInt32();
			board.LoadStateBinary(br);
			ppu.LoadStateBinary(br);
		}


	}
}

//todo
//http://blog.ntrq.net/?p=428
//cpu bus junk bits

//UBER DOC
//http://nocash.emubase.de/everynes.htm

//A VERY NICE board assignments list
//http://personales.epsg.upv.es/~jogilmo1/nes/TEXTOS/ARXIUS/BOARDTABLE.TXT

//why not make boards communicate over the actual board pinouts
//http://wiki.nesdev.com/w/index.php/Cartridge_connector

//a mappers list
//http://tuxnes.sourceforge.net/nesmapper.txt 
