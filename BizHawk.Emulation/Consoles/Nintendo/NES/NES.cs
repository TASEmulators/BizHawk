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
        public RomStatus RomStatus;
		//Game issues:
		//Tecmo superbowl - wobbly "NFL" logo at the end of a game (even skipped game) [zeromus cant test this; how do you skip game?]
		//Bigfoot (U) seems not to work
		//Bill and ted's excellent video game adventure (U) doesnt work until more detailed emulation exists (check 001.txt)

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
		//Air Wolf - big graphical glitch. seems to be a real bug, but it should never have been released with this. need to verify for sure that it is a real bug?

		public NES(GameInfo game, byte[] rom)
		{
			CoreOutputComm = new CoreOutputComm();
			BootGodDB.Initialize();
			SetPalette(Palettes.FCEUX_Standard);
			videoProvider = new MyVideoProvider(this);
		    Init(game, rom);
		}

        private NES()
        {
            BootGodDB.Initialize();
        }

		public void WriteLogTimestamp()
		{
			if (ppu != null)
				Console.Write("[{0:d5}:{1:d3}:{2:d3}]", Frame, ppu.ppur.status.sl, ppu.ppur.status.cycle);
		}
		public void LogLine(string format, params object[] args)
		{
			if (ppu != null)
				Console.WriteLine("[{0:d5}:{1:d3}:{2:d3}] {3}", Frame, ppu.ppur.status.sl, ppu.ppur.status.cycle, string.Format(format, args));
		}

		NESWatch GetWatch(NESWatch.EDomain domain, int address)
		{
			if (domain == NESWatch.EDomain.Sysbus)
			{
				NESWatch ret = sysbus_watch[address] ?? new NESWatch(this, domain, address);
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

		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }

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
				int backdrop = emu.CoreInputComm.NES_BackdropColor;
				bool useBackdrop = (backdrop & 0xFF000000) != 0;
				//TODO - we could recalculate this on the fly (and invalidate/recalculate it when the palette is changed)
				for (int i = 0; i < 256 * 240; i++)
				{
					short pixel = emu.ppu.xbuf[i];
					if ((pixel & 0x8000) != 0 && useBackdrop)
					{
						pixels[i] = backdrop;
					}
					else pixels[i] = emu.palette_compiled[pixel & 0x7FFF];
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
				BoolButtons = { "P1 A","P1 B","P1 Select","P1 Start","P1 Left","P1 Up","P1 Down","P1 Right", "Reset", 
				"P2 A", "P2 B", "P2 Select", "P2 Start", "P2 Up", "P2 Down", "P2 Left", "P2 Right"}
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
			int player;
			public JoypadPortDevice(NES nes, int player)
			{
				this.nes = nes;
				this.player = player;
			}
			void Strobe()
			{
				value = 0;
				foreach (string str in new string[] { "P" + (player + 1).ToString() + " Right", "P" + (player + 1).ToString() + " Left", 
					"P" + (player + 1).ToString() +  " Down", "P" + (player + 1).ToString() +  " Up", "P" + (player + 1).ToString() +  " Start", 
					"P" + (player + 1).ToString() +  " Select", "P" + (player + 1).ToString() +  " B", "P" + (player + 1).ToString() +  " A" })
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
				int ret = value & 1;
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

		public void ResetFrameCounter()
		{
			_frame = 0;
		}

		public long Timestamp { get; private set; }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }

		public bool DeterministicEmulation { get { return true; } set { } }

		public byte[] SaveRam
		{
			get
			{
				if (board == null) return null;
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

		public string GameName { get { return game_name; } }

		public enum EDetectionOrigin
		{
			None, BootGodDB, GameDB, INES
		}

		StringWriter LoadReport;
		void LoadWriteLine(string format, params object[] arg)
		{
			Console.WriteLine(format, arg);
			LoadReport.WriteLine(format, arg);
		}
		void LoadWriteLine(object arg) { LoadWriteLine("{0}", arg); }
        
		public unsafe void Init(GameInfo gameInfo, byte[] rom)
		{
			LoadReport = new StringWriter();
			LoadWriteLine("------");
			LoadWriteLine("BEGIN NES rom analysis:");
			byte[] file = rom;
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

				LoadWriteLine("Found iNES header:");
				CartInfo iNesHeaderInfo = header->Analyze();
				LoadWriteLine("Since this is iNES we can confidently parse PRG/CHR banks to hash.");

				LoadWriteLine("headerless rom hash: {0}", hash_sha1);
				LoadWriteLine("headerless rom hash: {0}", hash_md5);

				Type boardType = null;
				CartInfo choice = null;
				if (USE_DATABASE)
					choice = IdentifyFromBootGodDB(hash_sha1);
				if (choice == null)
				{
					LoadWriteLine("Could not locate game in nescartdb");
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
						LoadWriteLine("Could not locate game in bizhawk gamedb");
						LoadWriteLine("Attempting inference from iNES header");
						choice = iNesHeaderInfo;
						string iNES_board = iNESBoardDetector.Detect(choice);
						if (iNES_board == null)
							throw new Exception("couldnt identify NES rom");
						choice.board_type = iNES_board;

						//try spinning up a board with 8K wram and with 0K wram to see if one answers
						try
						{
							boardType = FindBoard(choice, origin);
						}
						catch { }
						if (boardType == null)
						{
							if (choice.wram_size == 8) choice.wram_size = 0;
							else if (choice.wram_size == 0) choice.wram_size = 8;
							try
							{
								boardType = FindBoard(choice, origin);
							}
							catch { }
							if (boardType != null)
								LoadWriteLine("Ambiguous iNES wram size resolved as {0}k", choice.wram_size);
						}

						LoadWriteLine("Chose board from iNES heuristics: " + iNES_board);
						choice.game.name = gameInfo.Name;
						origin = EDetectionOrigin.INES;
					}
					else
					{
						origin = EDetectionOrigin.GameDB;
						LoadWriteLine("Chose board from bizhawk gamedb: " + choice.board_type);
					}
				}
				else
				{
					LoadWriteLine("Chose board from nescartdb:");
					LoadWriteLine(choice);
					origin = EDetectionOrigin.BootGodDB;
				}

				//TODO - generate better name with region and system
				game_name = choice.game.name;

				//find a INESBoard to handle this
				boardType = FindBoard(choice, origin);
				if (boardType == null)
					throw new Exception("No class implements the necessary board type: " + choice.board_type);

				if (choice.DB_GameInfo != null)
					if (choice.DB_GameInfo.Status == RomStatus.BadDump)
						choice.bad = true;

				LoadWriteLine("Final game detection results:");
				LoadWriteLine(choice);
				LoadWriteLine("\"" + game_name + "\"");
				LoadWriteLine("Implemented by: class " + boardType.Name);
				if (choice.bad)
				{
					LoadWriteLine("~~ ONE WAY OR ANOTHER, THIS DUMP IS KNOWN TO BE *BAD* ~~");
					LoadWriteLine("~~ YOU SHOULD FIND A BETTER FILE ~~");
				}

				LoadWriteLine("END NES rom analysis");
				LoadWriteLine("------");

				board = (INESBoard)Activator.CreateInstance(boardType);

				cart = choice;
				board.Create(this);
				board.Configure(origin);

				if (origin == EDetectionOrigin.BootGodDB)
				{
					RomStatus = RomStatus.GoodDump;
					CoreOutputComm.RomStatusAnnotation = "Identified from BootGod's database";
				}
				if (origin == EDetectionOrigin.INES)
				{
					RomStatus = RomStatus.NotInDatabase;
					CoreOutputComm.RomStatusAnnotation = "Inferred from iNES header; potentially wrong";
				}
				if (origin == EDetectionOrigin.GameDB)
				{
					if (choice.bad)
					{
						RomStatus = RomStatus.BadDump;
					}
					else
					{
						RomStatus = choice.DB_GameInfo.Status;
					}
				}

				LoadReport.Flush();
				CoreOutputComm.RomStatusDetails = LoadReport.ToString();


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
			apu.SyncState(ser);
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

		public void Dispose() { }
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