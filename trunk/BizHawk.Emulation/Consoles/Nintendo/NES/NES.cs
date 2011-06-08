using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.M6502;

//TODO - redo all timekeeping in terms of master clock

namespace BizHawk.Emulation.Consoles.Nintendo
{

	public partial class NES : IEmulator
	{
		static readonly bool USE_DATABASE = true;

        //Game issues:
        //Air Wolf - Black screen (seems to be mapped properly, and not frozen, but graphics just dont show up. must be ppu bug)
		//Dragon warrior 3/4 certainly need some additional work done to the mapper wiring to get to the super big PRG (probably SXROM too)
		//Tecmo superbowl - wobbly "NFL" logo at the end of a game (even skipped game) [zeromus cant test this; how do you skip game?]

		//---
		//Game issues for tester to check off.
		//we have three compatibility levels, so you may want to leave games off the 'broken' list even though theyre still broken (aka level 2 'as good as any other emu)
		//1. broken
		//2. as good as any other emu
		//3. more fixed than other emus

		//Indiana Jones Temple of Doom - Pause menu flickering (in FCEUX as well, haven't tested other emulators) [looks same in nintendulator, i think this is OK]
		//3-D World Runner - UNROM - weird lines in gameplay (OK i think - should be similar to other emulators now)
		//JJ - Tobidase Daisakusen Part 2 (J) - same as 3-D World Runner
		//Knight Rider - very glitchy and seems to be a good timing case! (seems to run same as nintendulator and fceux now.. which may not be entirely accurate)

		//------
		//zeromus's new notes:
		//AD&D Hillsfar (U).nes black screen

		public NES()
		{
			BootGodDB.Initialize();
			SetPalette(Palettes.FCEUX_Standard);
			videoProvider = new MyVideoProvider(this);
		}

		public void WriteLogTimestamp()
		{
			Console.Write("[{0:d5}:{1:d3}:{2:d3}]", Frame, ppu.ppur.status.sl, ppu.ppur.status.cycle);
		}
		public void LogLine(string format, params object[] args)
		{
			Console.WriteLine("[{0:d5}:{1:d3}:{2:d3}] {3}", Frame, ppu.ppur.status.sl, ppu.ppur.status.cycle, string.Format(format, args));
		}

		NESWatch GetWatch(NESWatch.EDomain domain, int address)
		{
			if (domain == NESWatch.EDomain.Sysbus)
			{
				NESWatch ret = sysbus_watch[address] ?? new NESWatch(this,domain,address);
				sysbus_watch[address] = ret;
				return ret;
			}
			return null;
		}

		class NESWatch
		{
			public enum EDomain
			{
				Sysbus
			}
			public NESWatch(NES nes, EDomain domain, int address)
			{
				Address = address;
				Domain = domain;
				if (domain == EDomain.Sysbus)
				{
					watches = nes.sysbus_watch;
				}
			}
			public int Address;
			public EDomain Domain;

			public enum EFlags
			{
				None = 0,
				GameGenie = 1,
				ReadPrint = 2
			}
			EFlags flags;
			
			public void Sync()
			{
				if (flags == EFlags.None)
					watches[Address] = null;
				else watches[Address] = this;
			}

			public void SetGameGenie(int check, int replace)
			{
				flags |= EFlags.GameGenie;
				gg_check = check;
				gg_replace = replace;
				Sync();
			}

			public bool HasGameGenie { get { return (flags & EFlags.GameGenie) != 0; } }
			public byte ApplyGameGenie(byte curr)
			{
				if (!HasGameGenie) return curr;
				if (curr == gg_check || gg_check == -1) { Console.WriteLine("applied game genie"); return (byte)gg_replace; }
				else return curr;
			}

			public void RemoveGameGenie()
			{
				flags &= ~EFlags.GameGenie;
				Sync();
			}

			int gg_check, gg_replace;

			
			NESWatch[] watches;
		}

		class MyVideoProvider : IVideoProvider
		{
			NES emu;
			public MyVideoProvider(NES emu)
			{
				this.emu = emu;
			}

			int[] pixels = new int[256 * 240];
			public int[] GetVideoBuffer()
			{
				//TODO - we could recalculate this on the fly (and invalidate/recalculate it when the palette is changed)
				for (int i = 0; i < 256*240; i++)
				{
                    pixels[i] = emu.palette_compiled[emu.ppu.xbuf[i]];
				}
				return pixels;
			}
			public int BufferWidth { get { return 256; } }
			public int BufferHeight { get { return 240; } }
			public int BackgroundColor { get { return 0; } }
		}

		MyVideoProvider videoProvider;
		public IVideoProvider VideoProvider { get { return videoProvider; } }
		public ISoundProvider SoundProvider { get { return apu; } }

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
				//Console.WriteLine("STROBE");
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


		int _frame;
        int _lagcount;
        bool lagged = true;
        bool islag = false;
		public int Frame { get { return _frame; } set { _frame = value; } }
		public long Timestamp { get; private set; }
        public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
        public bool IsLagFrame { get { return islag; } }

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
			var RAM = new MemoryDomain("RAM", 0x800, Endian.Little,
                addr => ram[addr & 0x07FF], (addr, value) => ram[addr & 0x07FF] = value);
            var SystemBus = new MemoryDomain("System Bus", 0x10000, Endian.Little,
                addr => ReadMemory((ushort)addr), (addr, value) => WriteMemory((ushort)addr, value));
            var PPUBus = new MemoryDomain("PPU Bus", 0x4000, Endian.Little,
                addr => ppu.ppubus_peek(addr), (addr, value) => ppu.ppubus_write(addr, value));
			var CIRAMdomain = new MemoryDomain("CIRAM (nametables)", 0x800, Endian.Little,
				addr => CIRAM[addr & 0x07FF], (addr, value) => CIRAM[addr & 0x07FF] = value);

			SystemBus.GetFreeze = addr => sysbus_freeze[addr];
			SystemBus.SetFreeze = (addr, value) => sysbus_freeze[addr] = value;

            RAM.GetFreeze = addr => sysbus_freeze[addr & 0x07FF];
            RAM.SetFreeze = (addr, value) => sysbus_freeze[addr & 0x07FF] = value;

            PPUBus.GetFreeze = addr => ppu.ppubus_freeze[addr];
            PPUBus.SetFreeze = (addr, value) => ppu.ppubus_freeze[addr] = value;

			//demo a game genie code
			GetWatch(NESWatch.EDomain.Sysbus, 0xB424).SetGameGenie(-1, 0x10);
			GetWatch(NESWatch.EDomain.Sysbus, 0xB424).RemoveGameGenie();

            domains.Add(RAM);
			domains.Add(SystemBus);
            domains.Add(PPUBus);
			domains.Add(CIRAMdomain);

            if (board.SaveRam != null)
            {
                var BatteryRam = new MemoryDomain("Battery RAM", board.SaveRam.Length, Endian.Little,
                    addr => board.SaveRam[addr], (addr, value) => board.SaveRam[addr] = value);
                domains.Add(BatteryRam);
            }

            var PRGROM = new MemoryDomain("PRG ROM", cart.prg_size * 1024, Endian.Little,
                addr => board.ROM[addr], (addr, value) => board.ROM[addr] = value);
            domains.Add(PRGROM);

			if (board.VROM != null)
            {
                var CHRROM = new MemoryDomain("CHR VROM", cart.chr_size * 1024, Endian.Little,
					addr => board.VROM[addr], (addr, value) => board.VROM[addr] = value);
                domains.Add(CHRROM);
            }

            if (board.VRAM != null)
            {
				var VRAM = new MemoryDomain("VRAM", board.VRAM.Length, Endian.Little,
                    addr => board.VRAM[addr], (addr, value) => board.VRAM[addr] = value);
				domains.Add(VRAM);
            }

            if (board.WRAM != null)
            {
				var WRAM = new MemoryDomain("WRAM", board.WRAM.Length, Endian.Little,
                    addr => board.WRAM[addr], (addr, value) => board.WRAM[addr] = value);
				domains.Add(WRAM);
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

		public string GameName { get { return game_name; } }

		public enum EDetectionOrigin
		{
			None, BootGodDB, GameDB, INES
		}

		public unsafe void LoadGame(IGame game)
		{
			byte[] file = game.GetFileData();
			if (file.Length < 16) throw new Exception("Alleged NES rom too small to be anything useful");
			if (file.Take(4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("UNIF")))
				throw new Exception("You've tried to open a UNIF rom. We don't have any UNIF roms to test with. Please consult the developers.");
			fixed (byte* bfile = &file[0])
			{
				var origin = EDetectionOrigin.None;

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

				CartInfo choice = null;
				if(USE_DATABASE)
					choice = IdentifyFromBootGodDB(hash_sha1);
				if (choice == null)
				{
					if (USE_DATABASE)
					{
						choice = IdentifyFromGameDB(hash_md5);
						if (choice == null)
						{
							choice = IdentifyFromGameDB(hash_sha1);
						}
					}
					if (choice == null)
					{
						Console.WriteLine("Attempting inference from iNES header");
						choice = header->Analyze();
						string iNES_board = iNESBoardDetector.Detect(choice);
						if (iNES_board == null)
							throw new Exception("couldnt identify NES rom");
						Console.WriteLine("trying board " + iNES_board);
						choice.board_type = iNES_board;
						choice.game.name = game.Name;
						origin = EDetectionOrigin.INES;
					}
					else
					{
						origin = EDetectionOrigin.GameDB;
						Console.WriteLine("Chose board from gamedb: " + board);
					}
				}
				else
				{
					Console.WriteLine("Chose board from nescartdb: " + choice.board_type);
					origin = EDetectionOrigin.BootGodDB;
				}

				Console.WriteLine(choice.game);
				Console.WriteLine(choice);

				//todo - generate better name with region and system
				game_name = choice.game.name;

				//find a INESBoard to handle this
				Type boardType = null;
				boardType = FindBoard(choice, origin);
				if (boardType == null)
					throw new Exception("No class implements the necessary board type: " + choice.board_type);

				board = (INESBoard)Activator.CreateInstance(boardType);

				cart = choice;
				board.Create(this);
				board.Configure(origin);

				//create the board's rom and vrom
				board.ROM = new byte[choice.prg_size * 1024];
				Array.Copy(file, 16, board.ROM, 0, board.ROM.Length);
				if (choice.chr_size > 0)
				{
					board.VROM = new byte[choice.chr_size * 1024];
					Array.Copy(file, 16 + board.ROM.Length, board.VROM, 0, board.VROM.Length);
				}

				//create the vram and wram if necessary
				if (cart.wram_size != 0)
					board.WRAM = new byte[cart.wram_size * 1024];
				if (cart.vram_size != 0)
					board.VRAM = new byte[cart.vram_size * 1024];

				HardReset();
				SetupMemoryDomains();
			}
		}

		void SyncState(Serializer ser)
		{
			ser.BeginSection("NES");
			ser.Sync("Frame", ref _frame);
            ser.Sync("Lag", ref _lagcount);
			cpu.SyncState(ser);
			ser.Sync("ram", ref ram, false);
			ser.Sync("CIRAM", ref CIRAM, false);
			ser.Sync("cpu_accumulate", ref cpu_accumulate);
			ser.Sync("_irq_apu", ref _irq_apu);
			ser.Sync("_irq_cart", ref _irq_cart);
			sync_irq();
            //string inp = GetControllersAsMnemonic();  TODO sorry bout that
            //ser.SyncFixedString("input", ref inp, 32);
			board.SyncState(ser);
			ppu.SyncState(ser);
			ser.EndSection();
		}

		public void SaveStateText(TextWriter writer) { SyncState(Serializer.CreateTextWriter(writer)); }
		public void LoadStateText(TextReader reader) { SyncState(Serializer.CreateTextReader(reader)); }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

        public void Dispose() {}
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