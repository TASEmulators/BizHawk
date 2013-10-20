using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

//TODO - redo all timekeeping in terms of master clock

namespace BizHawk.Emulation.Consoles.Nintendo
{

	public partial class NES : IEmulator
	{
		static readonly bool USE_DATABASE = true;
		public RomStatus RomStatus;

		public NES(CoreComm comm, GameInfo game, byte[] rom, byte[] fdsbios = null, Dictionary<string, string> boardProperties = null)
		{
			if (boardProperties != null)
			{
				InitialMapperRegisterValues.Set(boardProperties);
			}

			CoreComm = comm;
			CoreComm.CpuTraceAvailable = true;
			BootGodDB.Initialize();
			SetPalette(Palettes.FCEUX_Standard);
			videoProvider = new MyVideoProvider(this);
			Init(game, rom, fdsbios);
			ControllerDefinition = new ControllerDefinition(NESController);
			if (board is FDS)
			{
				var b = board as FDS;
				ControllerDefinition.BoolButtons.Add("FDS Eject");
				for (int i = 0; i < b.NumSides; i++)
					ControllerDefinition.BoolButtons.Add("FDS Insert " + i);

				CoreComm.UsesDriveLed = true;
				b.SetDriveLightCallback((val) => CoreComm.DriveLED = val);
			}
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

		MapperProperties InitialMapperRegisterValues = new MapperProperties();

		public class MapperProperties
		{
			private List<KeyValuePair<string, string>> _properties = new List<KeyValuePair<string, string>>();

			public string this[string key]
			{
				get
				{
					if(_properties.Any(x => x.Key == key))
					{
						return _properties.FirstOrDefault(x => x.Key == key).Value;
					}
					else
					{
						return null;
					}
				}
			}

			public void Set(Dictionary<string, string> values)
			{
				_properties.Clear();
				_properties.AddRange(values);
			}

			public void Add(string key, string value)
			{
				_properties.Add(new KeyValuePair<string, string>(key, value));
			}
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

			public void SetGameGenie(byte? compare, byte value)
			{
				flags |= EFlags.GameGenie;
				Compare = compare;
				Value = value;
				Sync();
			}

			public bool HasGameGenie
			{
				get
				{
					return (flags & EFlags.GameGenie) != 0;
				}
			}
			
			public byte ApplyGameGenie(byte curr)
			{
				if (!HasGameGenie)
				{
					return curr;
				}
				else if (curr == Compare || Compare == null)
				{
					Console.WriteLine("applied game genie");
					return (byte)Value;
				}
				else
				{
					return curr;
				}
			}

			public void RemoveGameGenie()
			{
				flags &= ~EFlags.GameGenie;
				Sync();
			}

			byte? Compare;
			byte Value;

			NESWatch[] watches;
		}

		public CoreComm CoreComm { get; private set; }

		public DisplayType DisplayType { get { return _display_type; } }

		class MyVideoProvider : IVideoProvider
		{
			public int ntsc_top = 8;
			public int ntsc_bottom = 231;
			public int pal_top = 0;
			public int pal_bottom = 239;
			public int left = 0;
			public int right = 255;
			
			NES emu;
			public MyVideoProvider(NES emu)
			{
				this.emu = emu;
			}

			int[] pixels = new int[256 * 240];
			public int[] GetVideoBuffer()
			{
				return pixels;
			}

			public void FillFrameBuffer()
			{
				int the_top;
				int the_bottom;
				if (emu.DisplayType == DisplayType.NTSC)
				{
					the_top = ntsc_top;
					the_bottom = ntsc_bottom;
				}
				else
				{
					the_top = pal_top;
					the_bottom = pal_bottom;
				}

				int backdrop = 0;
				if (emu.CoreComm != null)
					backdrop = emu.CoreComm.NES_BackdropColor;
				bool useBackdrop = (backdrop & 0xFF000000) != 0;

				//TODO - we could recalculate this on the fly (and invalidate/recalculate it when the palette is changed)
				int width = BufferWidth;
				for (int x = left; x <= right; x++)
				{
					for (int y = the_top; y <= the_bottom; y++)
					{
						short pixel = emu.ppu.xbuf[(y << 8) + x];
						if ((pixel & 0x8000) != 0 && useBackdrop)
						{
							pixels[((y - the_top) * width) + (x - left)] = backdrop;
						}
						else pixels[((y - the_top) * width) + (x - left)] = emu.palette_compiled[pixel & 0x7FFF];
					}
				}
			}
			public int VirtualWidth { get { return BufferWidth; } }
			public int BufferWidth { get { return right - left + 1; } }
			public int BackgroundColor { get { return 0; } }
			public int BufferHeight
			{
				get
				{
					if (emu.DisplayType == DisplayType.NTSC)
					{
						return ntsc_bottom - ntsc_top + 1;
					}
					else
					{
						return pal_bottom - pal_top + 1;
					}
				}
			}
			
		}

		public int NTSC_FirstDrawLine
		{
			get { return videoProvider.ntsc_top; }
			set { videoProvider.ntsc_top = value; CoreComm.ScreenLogicalOffsetY = videoProvider.ntsc_top; }
		}

		public int NTSC_LastDrawLine
		{
			get { return videoProvider.ntsc_bottom; }
			set { videoProvider.ntsc_bottom = value; }
		}

		public int PAL_FirstDrawLine
		{
			get { return videoProvider.pal_top; }
			set { videoProvider.pal_top = value; CoreComm.ScreenLogicalOffsetY = videoProvider.pal_top; }
		}

		public int PAL_LastDrawLine
		{
			get { return videoProvider.pal_bottom; }
			set { videoProvider.pal_bottom = value; }
		}

		public void SetClipLeftAndRight(bool clip)
		{
			if (clip)
			{
				videoProvider.left = 8;
				videoProvider.right = 248;
			}
			else
			{
				videoProvider.left = 0;
				videoProvider.right = 255;
			}
			
			CoreComm.ScreenLogicalOffsetX = videoProvider.left;
			videoProvider.FillFrameBuffer();
		}

		MyVideoProvider videoProvider;
		public IVideoProvider VideoProvider { get { return videoProvider; } }
		public ISoundProvider SoundProvider { get { return magicSoundProvider; } }
		public ISyncSoundProvider SyncSoundProvider { get { return magicSoundProvider; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public static readonly ControllerDefinition NESController =
			new ControllerDefinition
			{
				Name = "NES Controller",
				BoolButtons = {
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Start", "P1 Select", "P1 B", "P1 A", "Reset", "Power",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Start", "P2 Select", "P2 B", "P2 A"
				}
			};

		public ControllerDefinition ControllerDefinition { get; private set; }

		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value; }
		}

		interface IPortDevice
		{
			void Write(int value);
			byte Read(bool peek);
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
				foreach (
					string str in new string[] {
						"P" + (player + 1).ToString() + " Right", "P" + (player + 1).ToString() + " Left",
						"P" + (player + 1).ToString() +  " Down", "P" + (player + 1).ToString() +  " Up",
						"P" + (player + 1).ToString() +  " Start", "P" + (player + 1).ToString() +  " Select",
						"P" + (player + 1).ToString() +  " B", "P" + (player + 1).ToString() +  " A"
					}
				)
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
			public override byte Read(bool peek)
			{
				int ret = value & 1;
				if(!peek) value >>= 1;
				// more information is needed
				return (byte)(ret | (nes.DB & 0xe0));
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
			public virtual byte Read(bool peek)
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
			_lagcount = 0;
			islag = false;
		}

		public long Timestamp { get; private set; }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }

		public bool DeterministicEmulation { get { return true; } }



		public byte[] ReadSaveRam()
		{
			if (board is FDS)
				return (board as FDS).ReadSaveRam();

			if (board == null || board.SaveRam == null)
				return null;
			return (byte[])board.SaveRam.Clone();	
		}
		public void StoreSaveRam(byte[] data)
		{
			if (board is FDS)
			{
				(board as FDS).StoreSaveRam(data);
				return;
			}

			if (board == null || board.SaveRam == null)
				return;
			Array.Copy(data, board.SaveRam, data.Length);
		}

		public void ClearSaveRam()
		{
			if (board is FDS)
			{
				(board as FDS).ClearSaveRam();
				return;
			}

			if (board == null || board.SaveRam == null)
				return;
			for (int i = 0; i < board.SaveRam.Length; i++)
				board.SaveRam[i] = 0;
		}

		public bool SaveRamModified
		{
			get
			{
				if (board == null) return false;
				if (board is FDS) return true;
				if (board.SaveRam == null) return false;
				return true;
			}
			set { }
		}

		private IList<MemoryDomain> memoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();
			var RAM = new MemoryDomain("RAM", 0x800, Endian.Little,
				addr => ram[addr & 0x07FF], (addr, value) => ram[addr & 0x07FF] = value);
			var SystemBus = new MemoryDomain("System Bus", 0x10000, Endian.Little,
				addr => ReadMemory((ushort)addr), (addr, value) => ApplySystemBusPoke(addr, value)); //WriteMemory((ushort)addr, value));
			var PPUBus = new MemoryDomain("PPU Bus", 0x4000, Endian.Little,
				addr => ppu.ppubus_peek(addr), (addr, value) => ppu.ppubus_write(addr, value));
			var CIRAMdomain = new MemoryDomain("CIRAM (nametables)", 0x800, Endian.Little,
				addr => CIRAM[addr & 0x07FF], (addr, value) => CIRAM[addr & 0x07FF] = value);
			var OAMdoman = new MemoryDomain("OAM", 64 * 4, Endian.Unknown,
				addr => ppu.OAM[addr & (64 * 4 - 1)], (addr, value) => ppu.OAM[addr & (64 * 4 - 1)] = value);

			domains.Add(RAM);
			domains.Add(SystemBus);
			domains.Add(PPUBus);
			domains.Add(CIRAMdomain);
			domains.Add(OAMdoman);

			if (!(board is FDS) && board.SaveRam != null)
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

			// if there were more boards with special ram sets, we'd want to do something more general
			if (board is FDS)
				domains.Add((board as FDS).GetDiskPeeker());
			else if (board is ExROM)
				domains.Add((board as ExROM).GetExRAM());

			memoryDomains = domains.AsReadOnly();
		}

		public string SystemId { get { return "NES"; } }
		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public string GameName { get { return game_name; } }

		public enum EDetectionOrigin
		{
			None, BootGodDB, GameDB, INES, UNIF, FDS
		}

		StringWriter LoadReport;
		void LoadWriteLine(string format, params object[] arg)
		{
			Console.WriteLine(format, arg);
			LoadReport.WriteLine(format, arg);
		}
		void LoadWriteLine(object arg) { LoadWriteLine("{0}", arg); }

		class MyWriter : StringWriter
		{
			public MyWriter(TextWriter _loadReport)	
			{
				loadReport = _loadReport;
			}
			TextWriter loadReport;
			public override void WriteLine(string format, params object[] arg)
			{
				Console.WriteLine(format, arg);
				loadReport.WriteLine(format, arg);
			}
			public override void WriteLine(string value)
			{
				Console.WriteLine(value);
				loadReport.WriteLine(value);
			}
		}

		public unsafe void Init(GameInfo gameInfo, byte[] rom, byte[] fdsbios = null)
		{
			LoadReport = new StringWriter();
			LoadWriteLine("------");
			LoadWriteLine("BEGIN NES rom analysis:");
			byte[] file = rom;

			Type boardType = null;
			CartInfo choice = null;
			CartInfo iNesHeaderInfo = null;
			List<string> hash_sha1_several = new List<string>();
			string hash_sha1 = null, hash_md5 = null;
			Unif unif = null;

			origin = EDetectionOrigin.None;

			if (file.Length < 16) throw new Exception("Alleged NES rom too small to be anything useful");
			if (file.Take(4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("UNIF")))
			{
				LoadWriteLine("Found UNIF header:");
				LoadWriteLine("Since this is UNIF we can confidently parse PRG/CHR banks to hash.");
				unif = new Unif(new MemoryStream(file));
				hash_sha1 = unif.GetCartInfo().sha1;
				hash_sha1_several.Add(hash_sha1);
				LoadWriteLine("headerless rom hash: {0}", hash_sha1);
			}
			else if (file.Take(4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("FDS\x1A")))
			{
				// there's not much else to do with FDS images other than to feed them to the board
				origin = EDetectionOrigin.FDS;
				LoadWriteLine("Found FDS header.");
				if (fdsbios == null)
					throw new Exception("Missing FDS Bios!");
				cart = new CartInfo();
				var fdsboard = new FDS();
				fdsboard.biosrom = fdsbios;
				fdsboard.SetDiskImage(rom);
				fdsboard.Create(this);
				fdsboard.Configure(origin);

				board = fdsboard;

				//create the vram and wram if necessary
				if (cart.wram_size != 0)
					board.WRAM = new byte[cart.wram_size * 1024];
				if (cart.vram_size != 0)
					board.VRAM = new byte[cart.vram_size * 1024];

				board.PostConfigure();

				HardReset();
				return;
			}
			else
			{
				fixed (byte* bfile = &file[0])
				{
					var header = (iNES_HEADER*)bfile;
					if (!header->CheckID()) throw new InvalidOperationException("iNES header not found");
					header->Cleanup();

					//now that we know we have an iNES header, we can try to ignore it.

					hash_sha1 = "sha1:" + Util.Hash_SHA1(file, 16, file.Length - 16);
					hash_sha1_several.Add(hash_sha1);
					hash_md5 = "md5:" + Util.Hash_MD5(file, 16, file.Length - 16);

					LoadWriteLine("Found iNES header:");
					iNesHeaderInfo = header->Analyze(new MyWriter(LoadReport));
					LoadWriteLine("Since this is iNES we can (somewhat) confidently parse PRG/CHR banks to hash.");

					LoadWriteLine("headerless rom hash: {0}", hash_sha1);
					LoadWriteLine("headerless rom hash:  {0}", hash_md5);

					if (iNesHeaderInfo.prg_size == 16)
					{
						//8KB prg can't be stored in iNES format, which counts 16KB prg banks.
						//so a correct hash will include only 8KB.
						LoadWriteLine("Since this rom has a 16 KB PRG, we'll hash it as 8KB too for bootgod's DB:");
						var msTemp = new MemoryStream();
						msTemp.Write(file, 16, 8 * 1024); //add prg
						msTemp.Write(file, 16 + 16 * 1024, iNesHeaderInfo.chr_size * 1024); //add chr
						msTemp.Flush();
						var bytes = msTemp.ToArray();
						var hash = "sha1:" + Util.Hash_SHA1(bytes, 0, bytes.Length);
						LoadWriteLine("  PRG (8KB) + CHR hash: {0}", hash);
						hash_sha1_several.Add(hash);
						hash = "md5:" + Util.Hash_MD5(bytes, 0, bytes.Length);
						LoadWriteLine("  PRG (8KB) + CHR hash:  {0}", hash);
					}
				}
			}

			if (USE_DATABASE)
				choice = IdentifyFromBootGodDB(hash_sha1_several);
			if (choice == null)
			{
				LoadWriteLine("Could not locate game in nescartdb");
				if (USE_DATABASE)
				{
					if (hash_md5 != null) choice = IdentifyFromGameDB(hash_md5);
					if (choice == null)
					{
						choice = IdentifyFromGameDB(hash_sha1);
					}
				}
				if (choice == null)
				{
					LoadWriteLine("Could not locate game in bizhawk gamedb");
					if (unif != null)
					{
						LoadWriteLine("Using information from UNIF header");
						choice = unif.GetCartInfo();
						choice.game = new NESGameInfo();
						choice.game.name = gameInfo.Name;
						origin = EDetectionOrigin.UNIF;
					}
					if (iNesHeaderInfo != null)
					{
						LoadWriteLine("Attempting inference from iNES header");
						choice = iNesHeaderInfo;
						string iNES_board = iNESBoardDetector.Detect(choice);
						if (iNES_board == null)
							throw new Exception("couldnt identify NES rom");
						choice.board_type = iNES_board;

						//try spinning up a board with 8K wram and with 0K wram to see if one answers
						try
						{
							boardType = FindBoard(choice, origin, InitialMapperRegisterValues);
						}
						catch { }
						if (boardType == null)
						{
							if (choice.wram_size == 8) choice.wram_size = 0;
							else if (choice.wram_size == 0) choice.wram_size = 8;
							try
							{
								boardType = FindBoard(choice, origin, InitialMapperRegisterValues);
							}
							catch { }
							if (boardType != null)
								LoadWriteLine("Ambiguous iNES wram size resolved as {0}k", choice.wram_size);
						}

						LoadWriteLine("Chose board from iNES heuristics: " + iNES_board);
						choice.game.name = gameInfo.Name;
						origin = EDetectionOrigin.INES;
					}
				}
				else
				{
					origin = EDetectionOrigin.GameDB;
					LoadWriteLine("Chose board from bizhawk gamedb: " + choice.board_type);
					//gamedb entries that dont specify prg/chr sizes can infer it from the ines header
					if (iNesHeaderInfo != null)
					{
						if (choice.prg_size == -1) choice.prg_size = iNesHeaderInfo.prg_size;
						if (choice.chr_size == -1) choice.chr_size = iNesHeaderInfo.chr_size;
						if (choice.vram_size == -1) choice.vram_size = iNesHeaderInfo.vram_size;
						if (choice.wram_size == -1) choice.wram_size = iNesHeaderInfo.wram_size;
					}
					else if (unif != null)
					{
						if (choice.prg_size == -1) choice.prg_size = unif.GetCartInfo().prg_size;
						if (choice.chr_size == -1) choice.chr_size = unif.GetCartInfo().chr_size;
						// unif has no wram\vram sizes; hope the board impl can figure it out...
						if (choice.vram_size == -1) choice.vram_size = 0;
						if (choice.wram_size == -1) choice.wram_size = 0;
					}

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
			boardType = FindBoard(choice, origin, InitialMapperRegisterValues);
			if (boardType == null)
				throw new Exception("No class implements the necessary board type: " + choice.board_type);

			if (choice.DB_GameInfo != null)
				choice.bad = choice.DB_GameInfo.IsRomStatusBad();

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

			board = CreateBoardInstance(boardType);

			cart = choice;
			board.Create(this);
			board.InitialRegisterValues = InitialMapperRegisterValues;
			board.Configure(origin);

			if (origin == EDetectionOrigin.BootGodDB)
			{
				RomStatus = RomStatus.GoodDump;
				CoreComm.RomStatusAnnotation = "Identified from BootGod's database";
			}
			if (origin == EDetectionOrigin.UNIF)
			{
				RomStatus = RomStatus.NotInDatabase;
				CoreComm.RomStatusAnnotation = "Inferred from UNIF header; somewhat suspicious";
			}
			if (origin == EDetectionOrigin.INES)
			{
				RomStatus = RomStatus.NotInDatabase;
				CoreComm.RomStatusAnnotation = "Inferred from iNES header; potentially wrong";
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
			CoreComm.RomStatusDetails = LoadReport.ToString();

			//create the board's rom and vrom
			if (iNesHeaderInfo != null)
			{
				//pluck the necessary bytes out of the file
				board.ROM = new byte[choice.prg_size * 1024];
				Array.Copy(file, 16, board.ROM, 0, board.ROM.Length);
				if (choice.chr_size > 0)
				{
					board.VROM = new byte[choice.chr_size * 1024];
					int vrom_offset = iNesHeaderInfo.prg_size * 1024;
					Array.Copy(file, 16 + vrom_offset, board.VROM, 0, board.VROM.Length);
				}
			}
			else
			{
				board.ROM = unif.GetPRG();
				board.VROM = unif.GetCHR();
			}

			//create the vram and wram if necessary
			if (cart.wram_size != 0)
				board.WRAM = new byte[cart.wram_size * 1024];
			if (cart.vram_size != 0)
				board.VRAM = new byte[cart.vram_size * 1024];

			board.PostConfigure();

			HardReset();
		}

		void SyncState(Serializer ser)
		{
			int version = 2;
			ser.BeginSection("NES");
			ser.Sync("version", ref version);
			ser.Sync("Frame", ref _frame);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("IsLag", ref islag);
			cpu.SyncState(ser);
			ser.Sync("ram", ref ram, false);
			ser.Sync("CIRAM", ref CIRAM, false);
			ser.Sync("cpu_accumulate", ref cpu_accumulate);
			ser.Sync("_irq_apu", ref _irq_apu);
			ser.Sync("sprdma_countdown", ref sprdma_countdown);
			ser.Sync("cpu_step", ref cpu_step);
			ser.Sync("cpu_stepcounter", ref cpu_stepcounter);
			ser.Sync("cpu_deadcounter", ref cpu_deadcounter);
			ser.BeginSection("Board");
			board.SyncState(ser);
			if (board is NESBoardBase && !((NESBoardBase)board).SyncStateFlag)
				throw new InvalidOperationException("the current NES mapper didnt call base.SyncState");
			ser.EndSection();
			ppu.SyncState(ser);
			apu.SyncState(ser);

			if (version >= 2)
				ser.Sync("DB", ref DB);

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

		public bool BinarySaveStatesPreferred { get { return false; } }
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