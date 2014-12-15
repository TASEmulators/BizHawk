using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

using BizHawk.Emulation.Common;

//TODO - redo all timekeeping in terms of master clock
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[CoreAttributes(
		"NesHawk",
		"zeromus, natt, adelikat",
		isPorted: false,
		isReleased: true
		)]
	public partial class NES : IEmulator, IMemoryDomains, ISaveRam, IDebuggable, IStatable, IInputPollable,
		ISettable<NES.NESSettings, NES.NESSyncSettings>
	{
		static readonly bool USE_DATABASE = true;
		public RomStatus RomStatus;

		[CoreConstructor("NES")]
		public NES(CoreComm comm, GameInfo game, byte[] rom, object Settings, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			byte[] fdsbios = comm.CoreFileProvider.GetFirmware("NES", "Bios_FDS", false);
			if (fdsbios != null && fdsbios.Length == 40976)
			{
				comm.ShowMessage("Your FDS BIOS is a bad dump.  BizHawk will attempt to use it, but no guarantees!  You should find a new one.");
				var tmp = new byte[8192];
				Buffer.BlockCopy(fdsbios, 16 + 8192 * 3, tmp, 0, 8192);
				fdsbios = tmp;
			}

			this.SyncSettings = (NESSyncSettings)SyncSettings ?? new NESSyncSettings();
			this.ControllerSettings = this.SyncSettings.Controls;
			CoreComm = comm;
			Tracer = new TraceBuffer();
			MemoryCallbacks = new MemoryCallbackSystem();
			BootGodDB.Initialize();
			videoProvider = new MyVideoProvider(this);
			Init(game, rom, fdsbios);
			if (board is FDS)
			{
				DriveLightEnabled = true;
				(board as FDS).SetDriveLightCallback((val) => DriveLightOn = val);
			}
			PutSettings((NESSettings)Settings ?? new NESSettings());

			if (board is BANDAI_FCG_1)
			{
				var reader = (board as BANDAI_FCG_1).reader;
				// not all BANDAI FCG 1 boards have a barcode reader
				if (reader != null)
					(ServiceProvider as BasicServiceProvider).Register<DatachBarcode>(reader);
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		private NES()
		{
			BootGodDB.Initialize();
		}

		public bool DriveLightEnabled { get; private set; }
		public bool DriveLightOn { get; private set; }

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
			//public int ntsc_top = 8;
			//public int ntsc_bottom = 231;
			//public int pal_top = 0;
			//public int pal_bottom = 239;
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
					the_top = emu.Settings.NTSC_TopLine;
					the_bottom = emu.Settings.NTSC_BottomLine;
				}
				else
				{
					the_top = emu.Settings.PAL_TopLine;
					the_bottom = emu.Settings.PAL_BottomLine;
				}

				int backdrop = 0;
				backdrop = emu.Settings.BackgroundColor;
				bool useBackdrop = (backdrop & 0xFF000000) != 0;

				if (useBackdrop)
				{
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
				else
				{
					unsafe
					{
						fixed (int* dst_ = pixels)
						fixed (short* src_ = emu.ppu.xbuf)
						fixed (int* pal = emu.palette_compiled)
						{
							int* dst = dst_;
							short* src = src_ + 256 * the_top + left;
							int xcount = right - left + 1;
							int srcinc = 256 - xcount;
							int ycount = the_bottom - the_top + 1;
							xcount /= 16;
							for (int y = 0; y < ycount; y++)
							{
								for (int x = 0; x < xcount; x++)
								{
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
									*dst++ = pal[0x7fff & *src++];
								}
								src += srcinc;
							}
						}
					}
				}
			}
			public int VirtualWidth { get { return (int)(BufferWidth * 1.146); } }
			public int VirtualHeight { get { return BufferHeight; } }
			public int BufferWidth { get { return right - left + 1; } }
			public int BackgroundColor { get { return 0; } }
			public int BufferHeight
			{
				get
				{
					if (emu.DisplayType == DisplayType.NTSC)
					{
						return emu.Settings.NTSC_BottomLine - emu.Settings.NTSC_TopLine + 1;
					}
					else
					{
						return emu.Settings.PAL_BottomLine - emu.Settings.PAL_TopLine + 1;
					}
				}
			}

		}

		MyVideoProvider videoProvider;
		public IVideoProvider VideoProvider { get { return videoProvider; } }
		public ISoundProvider SoundProvider { get { return magicSoundProvider; } }
		public ISyncSoundProvider SyncSoundProvider { get { return magicSoundProvider; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		[Obsolete] // with the changes to both nes and quicknes cores, nothing uses this anymore
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

		int _frame;
		int _lagcount;
		bool lagged = true;
		bool islag = false;
		public int Frame { get { return _frame; } set { _frame = value; } }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			islag = false;
		}

		public long Timestamp { get; private set; }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		public bool DeterministicEmulation { get { return true; } }

		public byte[] CloneSaveRam()
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

		public bool SaveRamModified
		{
			get
			{
				if (board == null) return false;
				if (board is FDS) return true;
				if (board.SaveRam == null) return false;
				return true;
			}
		}

		private MemoryDomainList memoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();
			var RAM = new MemoryDomain("RAM", 0x800, MemoryDomain.Endian.Little,
				addr => ram[addr], (addr, value) => ram[addr] = value);
			var SystemBus = new MemoryDomain("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr => PeekMemory((ushort)addr), (addr, value) => ApplySystemBusPoke(addr, value));
			var PPUBus = new MemoryDomain("PPU Bus", 0x4000, MemoryDomain.Endian.Little,
				addr => ppu.ppubus_peek(addr), (addr, value) => ppu.ppubus_write(addr, value));
			var CIRAMdomain = new MemoryDomain("CIRAM (nametables)", 0x800, MemoryDomain.Endian.Little,
				addr => CIRAM[addr], (addr, value) => CIRAM[addr] = value);
			var OAMdoman = new MemoryDomain("OAM", 64 * 4, MemoryDomain.Endian.Unknown,
				addr => ppu.OAM[addr], (addr, value) => ppu.OAM[addr] = value);

			domains.Add(RAM);
			domains.Add(SystemBus);
			domains.Add(PPUBus);
			domains.Add(CIRAMdomain);
			domains.Add(OAMdoman);

			if (!(board is FDS) && board.SaveRam != null)
			{
				var BatteryRam = new MemoryDomain("Battery RAM", board.SaveRam.Length, MemoryDomain.Endian.Little,
					addr => board.SaveRam[addr], (addr, value) => board.SaveRam[addr] = value);
				domains.Add(BatteryRam);
			}

			var PRGROM = new MemoryDomain("PRG ROM", cart.prg_size * 1024, MemoryDomain.Endian.Little,
				addr => board.ROM[addr], (addr, value) => board.ROM[addr] = value);
			domains.Add(PRGROM);

			if (board.VROM != null)
			{
				var CHRROM = new MemoryDomain("CHR VROM", cart.chr_size * 1024, MemoryDomain.Endian.Little,
					addr => board.VROM[addr], (addr, value) => board.VROM[addr] = value);
				domains.Add(CHRROM);
			}

			if (board.VRAM != null)
			{
				var VRAM = new MemoryDomain("VRAM", board.VRAM.Length, MemoryDomain.Endian.Little,
					addr => board.VRAM[addr], (addr, value) => board.VRAM[addr] = value);
				domains.Add(VRAM);
			}

			if (board.WRAM != null)
			{
				var WRAM = new MemoryDomain("WRAM", board.WRAM.Length, MemoryDomain.Endian.Little,
					addr => board.WRAM[addr], (addr, value) => board.WRAM[addr] = value);
				domains.Add(WRAM);
			}

			// if there were more boards with special ram sets, we'd want to do something more general
			if (board is FDS)
				domains.Add((board as FDS).GetDiskPeeker());
			else if (board is ExROM)
				domains.Add((board as ExROM).GetExRAM());

			memoryDomains = new MemoryDomainList(domains);
		}

		public string SystemId { get { return "NES"; } }
		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }

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

		public void Init(GameInfo gameInfo, byte[] rom, byte[] fdsbios = null)
		{
			LoadReport = new StringWriter();
			LoadWriteLine("------");
			LoadWriteLine("BEGIN NES rom analysis:");
			byte[] file = rom;

			Type boardType = null;
			CartInfo choice = null;
			CartInfo iNesHeaderInfo = null;
			CartInfo iNesHeaderInfoV2 = null;
			List<string> hash_sha1_several = new List<string>();
			string hash_sha1 = null, hash_md5 = null;
			Unif unif = null;

			Dictionary<string, string> InitialMapperRegisterValues = new Dictionary<string, string>(SyncSettings.BoardProperties);

			origin = EDetectionOrigin.None;

			if (file.Length < 16) throw new Exception("Alleged NES rom too small to be anything useful");
			if (file.Take(4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("UNIF")))
			{
				unif = new Unif(new MemoryStream(file));
				LoadWriteLine("Found UNIF header:");
				LoadWriteLine(unif.CartInfo);
				LoadWriteLine("Since this is UNIF we can confidently parse PRG/CHR banks to hash.");
				hash_sha1 = unif.CartInfo.sha1;
				hash_sha1_several.Add(hash_sha1);
				LoadWriteLine("headerless rom hash: {0}", hash_sha1);
			}
			else if (file.Take(4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("FDS\x1A"))
				|| file.Take(4).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("\x01*NI")))
			{
				// danger!  this is a different codepath with an early return.  accordingly, some
				// code is duplicated twice...

				// FDS roms are just fed to the board, we don't do much else with them
				origin = EDetectionOrigin.FDS;
				LoadWriteLine("Found FDS header.");
				if (fdsbios == null)
					throw new MissingFirmwareException("Missing FDS Bios");
				cart = new CartInfo();
				var fdsboard = new FDS();
				fdsboard.biosrom = fdsbios;
				fdsboard.SetDiskImage(rom);
				fdsboard.Create(this);
				// at the moment, FDS doesn't use the IRVs, but it could at some point in the future
				fdsboard.InitialRegisterValues = InitialMapperRegisterValues;
				fdsboard.Configure(origin);

				board = fdsboard;

				//create the vram and wram if necessary
				if (cart.wram_size != 0)
					board.WRAM = new byte[cart.wram_size * 1024];
				if (cart.vram_size != 0)
					board.VRAM = new byte[cart.vram_size * 1024];

				board.PostConfigure();

				Console.WriteLine("Using NTSC display type for FDS disk image");
				_display_type = Common.DisplayType.NTSC;

				HardReset();

				return;
			}
			else
			{
				byte[] nesheader = new byte[16];
				Buffer.BlockCopy(file, 0, nesheader, 0, 16);

				if (!DetectFromINES(nesheader, out iNesHeaderInfo, out iNesHeaderInfoV2))
					throw new InvalidOperationException("iNES header not found");

				//now that we know we have an iNES header, we can try to ignore it.

				hash_sha1 = "sha1:" + file.HashSHA1(16, file.Length - 16);
				hash_sha1_several.Add(hash_sha1);
				hash_md5 = "md5:" + file.HashMD5(16, file.Length - 16);

				LoadWriteLine("Found iNES header:");
				LoadWriteLine(iNesHeaderInfo.ToString());
				if (iNesHeaderInfoV2 != null)
				{
					LoadWriteLine("Found iNES V2 header:");
					LoadWriteLine(iNesHeaderInfoV2);
				}
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
					var hash = "sha1:" + bytes.HashSHA1(0, bytes.Length);
					LoadWriteLine("  PRG (8KB) + CHR hash: {0}", hash);
					hash_sha1_several.Add(hash);
					hash = "md5:" + bytes.HashMD5(0, bytes.Length);
					LoadWriteLine("  PRG (8KB) + CHR hash:  {0}", hash);
				}
			}

			if (USE_DATABASE)
			{
				if (hash_md5 != null) choice = IdentifyFromGameDB(hash_md5);
				if (choice == null)
					choice = IdentifyFromGameDB(hash_sha1);
				if (choice == null)
					LoadWriteLine("Could not locate game in bizhawk gamedb");
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
						if (choice.prg_size == -1) choice.prg_size = unif.CartInfo.prg_size;
						if (choice.chr_size == -1) choice.chr_size = unif.CartInfo.chr_size;
						// unif has no wram\vram sizes; hope the board impl can figure it out...
						if (choice.vram_size == -1) choice.vram_size = 0;
						if (choice.wram_size == -1) choice.wram_size = 0;
					}
				}

				//if this is still null, we have to try it some other way. nescartdb perhaps?

				if (choice == null)
				{
					choice = IdentifyFromBootGodDB(hash_sha1_several);
					if (choice == null)
						LoadWriteLine("Could not locate game in nescartdb");
					else
					{
						LoadWriteLine("Chose board from nescartdb:");
						LoadWriteLine(choice);
						origin = EDetectionOrigin.BootGodDB;
					}
				}
			}

			//if choice is still null, try UNIF and iNES
			if (choice == null)
			{
				if (unif != null)
				{
					LoadWriteLine("Using information from UNIF header");
					choice = unif.CartInfo;
					//ok, i have this Q-Boy rom with no VROM and no VRAM.
					//looks like FCEUX policy is to allocate 8KB of chr ram no matter what UNLESS certain flags are set.
					//we'll let individual boards override that and set 8KB here
					choice.vram_size = 8;
					//(do we need to suppress this in case theres a CHR rom? probably not. nes board base will use ram if no rom is available)
					origin = EDetectionOrigin.UNIF;
				}
				if (iNesHeaderInfo != null)
				{
					LoadWriteLine("Attempting inference from iNES header");
					// try to spin up V2 header first, then V1 header
					if (iNesHeaderInfoV2 != null)
					{
						try
						{
							boardType = FindBoard(iNesHeaderInfoV2, origin, InitialMapperRegisterValues);
						}
						catch { }
						if (boardType == null)
							LoadWriteLine("Failed to load as iNES V2");
						else
							choice = iNesHeaderInfoV2;

						// V2 might fail but V1 might succeed because we don't have most V2 aliases setup; and there's
						// no reason to do so except when needed
					}
					if (boardType == null)
					{
						choice = iNesHeaderInfo; // we're out of options, really
						boardType = FindBoard(iNesHeaderInfo, origin, InitialMapperRegisterValues);
						if (boardType == null)
							LoadWriteLine("Failed to load as iNES V1");

						// do not further meddle in wram sizes.  a board that is being loaded from a "MAPPERxxx"
						// entry should know and handle the situation better for the individual board
					}

					LoadWriteLine("Chose board from iNES heuristics:");
					LoadWriteLine(choice);
					origin = EDetectionOrigin.INES;
				}
			}

			game_name = choice.name;

			//find a INESBoard to handle this
			if (choice != null)
				boardType = FindBoard(choice, origin, InitialMapperRegisterValues);
			else
				throw new Exception("Unable to detect ROM");
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

					// if file isn't long enough for VROM, truncate
					int vrom_copy_size = Math.Min(board.VROM.Length, file.Length - 16 - vrom_offset);
					Array.Copy(file, 16 + vrom_offset, board.VROM, 0, vrom_copy_size);
					if (vrom_copy_size < board.VROM.Length)
						LoadWriteLine("Less than the expected VROM was found in the file: {0} < {1}", vrom_copy_size, board.VROM.Length);
				}
				if (choice.prg_size != iNesHeaderInfo.prg_size || choice.chr_size != iNesHeaderInfo.chr_size)
					LoadWriteLine("Warning: Detected choice has different filesizes than the INES header!");
			}
			else
			{
				board.ROM = unif.PRG;
				board.VROM = unif.CHR;
			}

			LoadReport.Flush();
			CoreComm.RomStatusDetails = LoadReport.ToString();

			// IF YOU DO ANYTHING AT ALL BELOW THIS LINE, MAKE SURE THE APPROPRIATE CHANGE IS MADE TO FDS (if applicable)

			//create the vram and wram if necessary
			if (cart.wram_size != 0)
				board.WRAM = new byte[cart.wram_size * 1024];
			if (cart.vram_size != 0)
				board.VRAM = new byte[cart.vram_size * 1024];

			board.PostConfigure();

			// set up display type

			NESSyncSettings.Region fromrom = DetectRegion(cart.system);
			NESSyncSettings.Region fromsettings = SyncSettings.RegionOverride;

			if (fromsettings != NESSyncSettings.Region.Default)
			{
				Console.WriteLine("Using system region override");
				fromrom = fromsettings;
			}
			switch (fromrom)
			{
				case NESSyncSettings.Region.Dendy:
					_display_type = Common.DisplayType.DENDY;
					break;
				case NESSyncSettings.Region.NTSC:
					_display_type = Common.DisplayType.NTSC;
					break;
				case NESSyncSettings.Region.PAL:
					_display_type = Common.DisplayType.PAL;
					break;
				default:
					_display_type = Common.DisplayType.NTSC;
					break;
			}
			Console.WriteLine("Using NES system region of {0}", _display_type);

			HardReset();
		}

		static NESSyncSettings.Region DetectRegion(string system)
		{
			switch (system)
			{
				case "NES-PAL":
				case "NES-PAL-A":
				case "NES-PAL-B":
					return NESSyncSettings.Region.PAL;
				case "NES-NTSC":
				case "Famicom":
					return NESSyncSettings.Region.NTSC;
				// this is in bootgod, but not used at all
				case "Dendy":
					return NESSyncSettings.Region.Dendy;
				case null:
					Console.WriteLine("Rom is of unknown NES region!");
					return NESSyncSettings.Region.Default;
				default:
					Console.WriteLine("Unrecognized region {0}", system);
					return NESSyncSettings.Region.Default;
			}
		}

		void SyncState(Serializer ser)
		{
			int version = 4;
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
			{
				ser.Sync("DB", ref DB);
			}
			if (version >= 3)
			{
				ser.Sync("latched4016", ref latched4016);
				ser.BeginSection("ControllerDeck");
				ControllerDeck.SyncState(ser);
				ser.EndSection();
			}
			if (version >= 4)
			{
				ser.Sync("resetSignal", ref resetSignal);
				ser.Sync("hardResetSignal", ref hardResetSignal);
			}

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

		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", cpu.A },
				{ "X", cpu.X },
				{ "Y", cpu.Y },
				{ "S", cpu.S },
				{ "PC", cpu.PC },
				{ "Flag C", cpu.FlagC ? 1 : 0 },
				{ "Flag Z", cpu.FlagZ ? 1 : 0 },
				{ "Flag I", cpu.FlagI ? 1 : 0 },
				{ "Flag D", cpu.FlagD ? 1 : 0 },
				{ "Flag B", cpu.FlagB ? 1 : 0 },
				{ "Flag V", cpu.FlagV ? 1 : 0 },
				{ "Flag N", cpu.FlagN ? 1 : 0 },
				{ "Flag T", cpu.FlagT ? 1 : 0 }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					cpu.A = (byte)value;
					break;
				case "X":
					cpu.X = (byte)value;
					break;
				case "Y":
					cpu.Y = (byte)value;
					break;
				case "S":
					cpu.S = (byte)value;
					break;
				case "PC":
					cpu.PC = (ushort)value;
					break;
				case "Flag I":
					cpu.FlagI = value > 0;
					break;
			}
		}

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public ITracer Tracer { get; private set; }
		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		NESSettings Settings = new NESSettings();
		NESSyncSettings SyncSettings = new NESSyncSettings();

		public NESSettings GetSettings() { return Settings.Clone(); }
		public NESSyncSettings GetSyncSettings() { return SyncSettings.Clone(); }
		public bool PutSettings(NESSettings o)
		{
			Settings = o;
			if (Settings.ClipLeftAndRight)
			{
				videoProvider.left = 8;
				videoProvider.right = 247;
			}
			else
			{
				videoProvider.left = 0;
				videoProvider.right = 255;
			}
			CoreComm.ScreenLogicalOffsetX = videoProvider.left;
			CoreComm.ScreenLogicalOffsetY = DisplayType == DisplayType.NTSC ? Settings.NTSC_TopLine : Settings.PAL_TopLine;

			SetPalette(Settings.Palette);

			apu.Square1V = Settings.Square1;
			apu.Square2V = Settings.Square2;
			apu.TriangleV = Settings.Triangle;
			apu.NoiseV = Settings.Noise;
			apu.DMCV = Settings.DMC;

			return false;
		}
		public bool PutSyncSettings(NESSyncSettings o)
		{
			bool ret = NESSyncSettings.NeedsReboot(SyncSettings, o);
			SyncSettings = o;
			return ret;
		}

		public class NESSettings
		{
			public bool AllowMoreThanEightSprites = false;
			public bool ClipLeftAndRight = false;
			public bool DispBackground = true;
			public bool DispSprites = true;
			public int BackgroundColor = 0;

			public int NTSC_TopLine = 8;
			public int NTSC_BottomLine = 231;
			public int PAL_TopLine = 0;
			public int PAL_BottomLine = 239;

			public int[,] Palette;

			public int Square1 = 376;
			public int Square2 = 376;
			public int Triangle = 426;
			public int Noise = 247;
			public int DMC = 167;

			public NESSettings Clone()
			{
				var ret = (NESSettings)MemberwiseClone();
				ret.Palette = (int[,])ret.Palette.Clone();
				return ret;
			}

			public NESSettings()
			{
				Palette = (int[,])Palettes.QuickNESPalette.Clone();
			}

			[Newtonsoft.Json.JsonConstructor]
			public NESSettings(int[,] Palette)
			{
				if (Palette == null)
					// only needed for SVN purposes
					this.Palette = (int[,])Palettes.QuickNESPalette.Clone();
				else
					this.Palette = Palette;
			}
		}

		public class NESSyncSettings
		{
			public Dictionary<string, string> BoardProperties = new Dictionary<string, string>();

			public enum Region
			{
				Default,
				NTSC,
				PAL,
				Dendy
			};

			public Region RegionOverride = Region.Default;

			public NESControlSettings Controls = new NESControlSettings();

			public NESSyncSettings Clone()
			{
				var ret = (NESSyncSettings)MemberwiseClone();
				ret.BoardProperties = new Dictionary<string, string>(BoardProperties);
				ret.Controls = Controls.Clone();
				return ret;
			}

			public static bool NeedsReboot(NESSyncSettings x, NESSyncSettings y)
			{
				return !(Util.DictionaryEqual(x.BoardProperties, y.BoardProperties) &&
					x.RegionOverride == y.RegionOverride &&
					!NESControlSettings.NeedsReboot(x.Controls, y.Controls));
			}
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