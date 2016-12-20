using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

using BizHawk.Emulation.Common;

//TODO - redo all timekeeping in terms of master clock
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[CoreAttributes(
		"NesHawk",
		"zeromus, natt, alyosha, adelikat",
		isPorted: false,
		isReleased: true
		)]
	public partial class NES : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
		ISettable<NES.NESSettings, NES.NESSyncSettings>
	{
		static readonly bool USE_DATABASE = true;
		public RomStatus RomStatus;

		[CoreConstructor("NES")]
		public NES(CoreComm comm, GameInfo game, byte[] rom, object Settings, object SyncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

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
			
			MemoryCallbacks = new MemoryCallbackSystem();
			BootGodDB.Initialize();
			videoProvider = new MyVideoProvider(this);
			Init(game, rom, fdsbios);
			if (Board is FDS)
			{
				DriveLightEnabled = true;
				(Board as FDS).SetDriveLightCallback((val) => DriveLightOn = val);
				// bit of a hack: we don't have a private gamedb for FDS, but the frontend
				// expects this to be set.
				RomStatus = game.Status;
			}
			PutSettings((NESSettings)Settings ?? new NESSettings());

			// we need to put this here because the line directly above will overwrite palette intialization anywhere else
			// TODO: What if settings are later loaded?
			if (_isVS)
			{
				PickVSPalette(cart);
			}

			
			ser.Register<IDisassemblable>(cpu);

			Tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register<ITraceable>(Tracer);
			ser.Register<IVideoProvider>(videoProvider);
			ser.Register<ISoundProvider>(magicSoundProvider);
			
			if (Board is BANDAI_FCG_1)
			{
				var reader = (Board as BANDAI_FCG_1).reader;
				// not all BANDAI FCG 1 boards have a barcode reader
				if (reader != null)
					ser.Register<DatachBarcode>(reader);
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

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

		public bool HasMapperProperties
		{
			get
			{
				var fields = Board.GetType().GetFields();
				foreach (var field in fields)
				{
					var attrib = field.GetCustomAttributes(typeof(MapperPropAttribute), false).OfType<MapperPropAttribute>().SingleOrDefault();
					if (attrib != null)
					{
						return true;
					}
				}

				return false;
			}
		}

		public bool IsVS
		{
			get { return _isVS; }
		}

		public bool IsFDS
		{
			get { return Board is FDS; }
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

		public DisplayType Region { get { return _display_type; } }

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
				if (emu.Region == DisplayType.NTSC)
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
					if (emu.Region == DisplayType.NTSC)
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

		public int Frame { get { return _frame; } set { _frame = value; } }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			islag = false;
		}

		public long Timestamp { get; private set; }

		public bool DeterministicEmulation { get { return true; } }

		public string SystemId { get { return "NES"; } }

		public string GameName { get { return game_name; } }

		public enum EDetectionOrigin
		{
			None, BootGodDB, GameDB, INES, UNIF, FDS, NSF
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
			else if(file.Take(5).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("NESM\x1A")))
			{
				origin = EDetectionOrigin.NSF;
				LoadWriteLine("Loading as NSF");
				var nsf = new NSFFormat();
				nsf.WrapByteArray(file);
				
				cart = new CartInfo();
				var nsfboard = new NSFBoard();
				nsfboard.Create(this);
				nsfboard.ROM = rom;
				nsfboard.InitNSF( nsf);
				nsfboard.InitialRegisterValues = InitialMapperRegisterValues;
				nsfboard.Configure(origin);
				nsfboard.WRAM = new byte[cart.wram_size * 1024];
				Board = nsfboard;
				Board.PostConfigure();
				AutoMapperProps.Populate(Board, SyncSettings);

				Console.WriteLine("Using NTSC display type for NSF for now");
				_display_type = Common.DisplayType.NTSC;

				HardReset();

				return;
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

				Board = fdsboard;

				//create the vram and wram if necessary
				if (cart.wram_size != 0)
					Board.WRAM = new byte[cart.wram_size * 1024];
				if (cart.vram_size != 0)
					Board.VRAM = new byte[cart.vram_size * 1024];

				Board.PostConfigure();
				AutoMapperProps.Populate(Board, SyncSettings);

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
					//we also certainly have games with VROM and no VRAM.
					//looks like FCEUX policy is to allocate 8KB of chr ram no matter what UNLESS certain flags are set. but what's the justification for this? please leave a note if you go debugging in it again.
					//well, we know we can't have much of a NES game if there's no VROM unless there's VRAM instead.
					//so if the VRAM isn't set, choose 8 for it.
					//TODO - unif loading code may need to use VROR flag to transform chr_size=8 to vram_size=8 (need example)
					if (choice.chr_size == 0 && choice.vram_size == 0)
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

			Board = CreateBoardInstance(boardType);

			cart = choice;
			Board.Create(this);
			Board.InitialRegisterValues = InitialMapperRegisterValues;
			Board.Configure(origin);

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

			byte[] trainer = null;

			//create the board's rom and vrom
			if (iNesHeaderInfo != null)
			{
				var ms = new MemoryStream(file, false);
				ms.Seek(16, SeekOrigin.Begin); // ines header
				//pluck the necessary bytes out of the file
				if (iNesHeaderInfo.trainer_size != 0)
				{
					trainer = new byte[512];
					ms.Read(trainer, 0, 512);
				}

				Board.ROM = new byte[choice.prg_size * 1024];
				ms.Read(Board.ROM, 0, Board.ROM.Length);

				if (choice.chr_size > 0)
				{
					Board.VROM = new byte[choice.chr_size * 1024];
					int vrom_copy_size = ms.Read(Board.VROM, 0, Board.VROM.Length);

					if (vrom_copy_size < Board.VROM.Length)
						LoadWriteLine("Less than the expected VROM was found in the file: {0} < {1}", vrom_copy_size, Board.VROM.Length);
				}
				if (choice.prg_size != iNesHeaderInfo.prg_size || choice.chr_size != iNesHeaderInfo.chr_size)
					LoadWriteLine("Warning: Detected choice has different filesizes than the INES header!");
			}
			else
			{
				Board.ROM = unif.PRG;
				Board.VROM = unif.CHR;
			}

			LoadReport.Flush();
			CoreComm.RomStatusDetails = LoadReport.ToString();

			// IF YOU DO ANYTHING AT ALL BELOW THIS LINE, MAKE SURE THE APPROPRIATE CHANGE IS MADE TO FDS (if applicable)

			//create the vram and wram if necessary
			if (cart.wram_size != 0)
				Board.WRAM = new byte[cart.wram_size * 1024];
			if (cart.vram_size != 0)
				Board.VRAM = new byte[cart.vram_size * 1024];

			Board.PostConfigure();
			AutoMapperProps.Populate(Board, SyncSettings);

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

			if (trainer != null)
			{
				Console.WriteLine("Applying trainer");
				for (int i = 0; i < 512; i++)
					WriteMemory((ushort)(0x7000 + i), trainer[i]);
			}
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

		private ITraceable Tracer { get; set; }
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
