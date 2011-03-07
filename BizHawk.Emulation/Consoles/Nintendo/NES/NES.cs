using System;
using System.Linq;
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
        //Dragon Warrior (SAROM) - Black screen only
        //Family Feud - Fails to get past intro screen
        //Air Wolf - Black screen
        //Boyo and His Blob - Hangs on intro screen
        //Goal = graphics garble (looks identical to Zelda 2 problem)
        //Nobunaga's Ambition - black screen
		//Knight Rider - very glitchy and seems to be a good timing case!
		//Dragon warrior 3/4 certainly need some additional work done to the mapper wiring to get to the super big PRG (probably SXROM too)

		[AttributeUsage(AttributeTargets.Class)]
		public class INESBoardImplAttribute : Attribute {}
		static List<Type> INESBoardImplementors = new List<Type>();

		static NES()
		{
			foreach (Type type in typeof(NES).Assembly.GetTypes())
			{
				var attrs = type.GetCustomAttributes(typeof(INESBoardImplAttribute), true);
				if (attrs.Length == 0) continue;
				if (type.IsAbstract) continue;
				INESBoardImplementors.Add(type);
			}
		}

		static Type FindBoard(BootGodDB.Cart cart)
		{
			NES nes = new NES();
			foreach (var type in INESBoardImplementors)
			{
				INESBoard board = (INESBoard)Activator.CreateInstance(type);
				board.Create(nes);
				if (board.Configure(cart))
					return type;
			}
			return null;
		}


        //the main rom class that contains all information necessary for the board to operate
		public class RomInfo
		{
			public enum EInfoSource
			{
				None, INesHeader, GameDatabase
			}

			public EInfoSource InfoSource;
			public int PRG_Size = -1, CHR_Size = -1;
			public int CRAM_Size = -1, PRAM_Size = -1;
			public string BoardName;
			public EMirrorType MirrorType;
			public bool Battery;

			public int MapperNumber; //it annoys me that this junky mapper number is even in here. it might be nice to wrap this class in something else to contain the MapperNumber

			public string MD5;

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
			void Create(NES nes);
			bool Configure(BootGodDB.Cart cart);
			void InstallRoms(byte[] ROM, byte[] VROM);
			byte ReadPRG(int addr);
			byte ReadPPU(int addr); byte PeekPPU(int addr);
			byte ReadPRAM(int addr);
			void WritePRG(int addr, byte value);
			void WritePPU(int addr, byte value);
			void WritePRAM(int addr, byte value);
			byte[] SaveRam { get; }
            byte[] PRam { get; }
            byte[] CRam { get; }
			byte[] ROM { get; }
			byte[] VROM { get; }
			void SaveStateBinary(BinaryWriter bw);
			void LoadStateBinary(BinaryReader br);
		};

		[INESBoardImpl]
		public abstract class NESBoardBase : INESBoard
		{
			public virtual void Create(NES nes)
			{
				this.NES = nes;
			}
			public abstract bool Configure(BootGodDB.Cart cart);
			public void InstallRoms(byte[] ROM, byte[] VROM)
			{
				this.ROM = ROM;
				this.VROM = VROM;
			}

			public RomInfo BoardInfo { get { return NES.romInfo; } }
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

			protected void SetMirrorType(int pad_h, int pad_v)
			{
				if (pad_h == 0)
					if (pad_v == 0)
						SetMirrorType(EMirrorType.OneScreenA);
					else SetMirrorType(EMirrorType.Horizontal);
				else
					if (pad_v == 0)
						SetMirrorType(EMirrorType.Vertical);
					else SetMirrorType(EMirrorType.OneScreenB);
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
				return (block << 10) | ofs;
			}

			protected byte HandleNormalPRGConflict(int addr, byte value)
			{
				byte old_value = value;
				value &= ReadPRG(addr);
				Debug.Assert(old_value == value, "Found a test case of bus conflict. please report.");
				return value;
			}

			public virtual byte ReadPRG(int addr) { return ROM[addr];}
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
					NES.CIRAM[ApplyMirroring(addr)] = value;
				}
			}

			public virtual byte PeekPPU(int addr) { return ReadPPU(addr); }

			public virtual byte ReadPPU(int addr)
			{
				if (addr < 0x2000)
				{
					return VROM[addr];
				}
				else
				{
					return NES.CIRAM[ApplyMirroring(addr)];
				}
			}

			public virtual byte[] SaveRam { get { return null; } }
            public virtual byte[] PRam { get { return null; } }
            public virtual byte[] CRam { get { return null; } }

			public byte[] ROM { get; set; }
			public byte[] VROM { get; set; }

			protected void Assert(bool test, string comment, params object[] args)
			{
				if (!test) throw new Exception(string.Format(comment, args));
			}
			protected void Assert(bool test)
			{
				if (!test) throw new Exception("assertion failed in board setup!");
			}
		}

		//hardware/state
		protected MOS6502 cpu;
		INESBoard board;
		public PPU ppu;
		byte[] ram;
		protected byte[] CIRAM;
		int cpu_accumulate;
		string game_name;

		//user configuration 
		int[,] palette; //TBD!!
		IPortDevice[] ports;
		RomInfo romInfo = new RomInfo();

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
			BootGodDB.Initialize();
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
				int[] pixels = new int[256 * 240];
				int i = 0;
				for (int y = 0; y < 240; y++)
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
			public int BufferHeight { get { return 240; } }
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
			CIRAM = new byte[0x800];
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

        private IList<MemoryDomain> memoryDomains;

        private void SetupMemoryDomains()
        {
            var domains = new List<MemoryDomain>();
            var WRAM = new MemoryDomain("WRAM", 0x800, Endian.Little,
                addr => ram[addr & 0x07FF], (addr, value) => ram[addr & 0x07FF] = value);
            var MainMemory = new MemoryDomain("System Bus", 0x10000, Endian.Little,
                addr => ReadMemory((ushort)addr), (addr, value) => WriteMemory((ushort)addr, value));
            var PPUBus = new MemoryDomain("PPU Bus", 0x4000, Endian.Little,
                addr => ppu.ppubus_read(addr), (addr, value) => ppu.ppubus_write(addr, value));
            //TODO: board PRG, PRAM & SaveRAM, or whatever useful things from the board

            

            domains.Add(WRAM);
            domains.Add(MainMemory);
            domains.Add(PPUBus);

            if (board.SaveRam != null)
            {
                var BatteryRam = new MemoryDomain("Battery RAM", board.SaveRam.Length, Endian.Little,
                    addr => board.SaveRam[addr], (addr, value) => board.SaveRam[addr] = value);
                domains.Add(BatteryRam);
            }

            var PRGROM = new MemoryDomain("PRG Rom", romInfo.PRG_Size * 16384, Endian.Little,
                addr => board.ROM[addr], (addr, value) => board.ROM[addr] = value);
            domains.Add(PRGROM);

            if (romInfo.CHR_Size > 0)
            {
                var CHRROM = new MemoryDomain("CHR Rom", romInfo.CHR_Size * 8192, Endian.Little,
					addr => board.VROM[addr], (addr, value) => board.VROM[addr] = value);
                domains.Add(CHRROM);
            }

            if (board.CRam != null)
            {
                var CRAM = new MemoryDomain("CRAM", board.CRam.Length, Endian.Little,
                    addr => board.CRam[addr], (addr, value) => board.CRam[addr] = value);
                domains.Add(CRAM);
            }

            if (board.PRam != null)
            {
                var PRAM = new MemoryDomain("PRAM", board.PRam.Length, Endian.Little,
                    addr => board.PRam[addr], (addr, value) => board.PRam[addr] = value);
                domains.Add(PRAM);
            }

            memoryDomains = domains.AsReadOnly();
        }

		public string SystemId { get { return "NES"; } }
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }


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


		//turning this off probably doesnt work right now due to asserts in boards finding things set by the iNES header parsing
		//need to separate those fields
		const bool ENABLE_DB = true;

		public string GameName { get { return game_name; } }

		BootGodDB.Cart IdentifyFromGameDB(string hash)
		{
			GameInfo gi = Database.CheckDatabase(hash);
			if (gi == null) return null;

			BootGodDB.Game game = new BootGodDB.Game();
			BootGodDB.Cart cart = new BootGodDB.Cart();
			game.carts.Add(cart);

            var dict = gi.ParseOptionsDictionary();
			game.name = gi.Name;
			cart.game = game;
			cart.board_type = dict["board"];
			if(dict.ContainsKey("PRG"))
				cart.prg_size = short.Parse(dict["PRG"]);
			if(dict.ContainsKey("CHR"))
				cart.chr_size = short.Parse(dict["CHR"]);

			return cart;
		}

		public unsafe void LoadGame(IGame game)
		{
			byte[] file = game.GetFileData();
			if (file.Length < 16) throw new Exception("Alleged NES rom too small to be anything useful");
			if (file.Take(4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("UNIF")))
				throw new Exception("You've tried to open a UNIF rom. We don't have any UNIF roms to test with. Please consult the developers.");
			fixed (byte* bfile = &file[0])
			{
				var header = (iNES_HEADER*)bfile;
				if (!header->CheckID()) throw new InvalidOperationException("iNES header not found");
				header->Cleanup();

				//now that we know we have an iNES header, we can try to ignore it.
				string hash_sha1;
				string hash_md5;
				using (var sha1 = System.Security.Cryptography.SHA1.Create())
				{
					sha1.TransformFinalBlock(file, 16, file.Length - 16);
					hash_sha1 = "sha1:" + Util.BytesToHexString(sha1.Hash);
				}
				using (var md5 = System.Security.Cryptography.MD5.Create())
				{
					md5.TransformFinalBlock(file, 16, file.Length - 16);
					hash_md5 = "md5:" + Util.BytesToHexString(md5.Hash);
				}
				Console.WriteLine("headerless rom hash: {0}", hash_sha1);
				Console.WriteLine("headerless rom hash: {0}", hash_md5);

				//check the bootgod database
				BootGodDB.Initialize();
				List<BootGodDB.Cart> choices = BootGodDB.Instance.Identify(hash_sha1);
				BootGodDB.Cart choice;
				if (choices.Count == 0)
				{
					//try generating a bootgod cart descriptor from the game database
					choice = IdentifyFromGameDB(hash_md5);
					if (choice == null) 
						choice = IdentifyFromGameDB(hash_sha1);
					if (choice == null) 
						throw new Exception("couldnt identify");
					else
						Console.WriteLine("Chose board from gamedb: ");
				}
				else
				{
					Console.WriteLine("Chose board from nescartdb:");
					//pick the first board for this hash arbitrarily. it probably doesn't make a difference
					choice = choices[0];
				}

				Console.WriteLine(choice.game);
				Console.WriteLine(choice);
				game_name = choice.game.name;

				//todo - generate better name with region and system

				//find a INESBoard to handle this
				Type boardType = FindBoard(choice);
				if (boardType == null)
				{
					throw new Exception("No class implements the necessary board type: " + choice.board_type);
				}
				board = (INESBoard)Activator.CreateInstance(boardType);

				board.Create(this);
				board.Configure(choice);

				byte[] rom, vrom = null;
				rom = new byte[romInfo.PRG_Size * 1024];
				Array.Copy(file, 16, rom, 0, rom.Length);
				if (romInfo.CHR_Size > 0)
				{
					vrom = new byte[romInfo.CHR_Size * 1024];
					Array.Copy(file, 16 + rom.Length, vrom, 0, vrom.Length);
				}
				board.InstallRoms(rom, vrom);

				HardReset();
				SetupMemoryDomains();
			}
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
			Util.WriteByteBuffer(bw, ram);
			Util.WriteByteBuffer(bw, CIRAM);
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
			CIRAM = Util.ReadByteBuffer(br, false);
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

//some ppu tests
//http://nesdev.parodius.com/bbs/viewtopic.php?p=4571&sid=db4c7e35316cc5d734606dd02f11dccb