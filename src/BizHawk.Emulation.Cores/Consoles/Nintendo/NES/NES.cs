using System.Linq;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

using BoardInitCB = System.Func<
	BizHawk.Emulation.Cores.Nintendo.NES.NES,
	byte[],
	(BizHawk.Emulation.Cores.Nintendo.NES.INesBoard Board, string RegionAssumedWarning)>;

//TODO - redo all timekeeping in terms of master clock
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[Core(CoreNames.NesHawk, "zeromus, natt, alyosha, adelikat")]
	public sealed partial class NES : IEmulator, ISaveRam, IDebuggable, IInputPollable, IRegionable, IVideoLogicalOffsets,
		IBoardInfo, IRomInfo, ISettable<NES.NESSettings, NES.NESSyncSettings>, ICodeDataLogger
	{
		public sealed class HeaderlessRomException() : InvalidOperationException("iNES header not found and no gamedb entry");

		public readonly struct InitResult
		{
			internal readonly Type/*?*/ BoardType;

			internal readonly CartInfo/*?*/ Choice;

			internal readonly CartInfo/*?*/ FromINes;

			internal readonly Unif/*?*/ FromUnif;

			internal readonly BoardInitCB/*?*/ InitCallback;

			internal InitResult(BoardInitCB initCallback)
			{
				BoardType = default;
				Choice = default;
				FromINes = default;
				FromUnif = default;
				InitCallback = initCallback;
			}

			internal InitResult(CartInfo choice, Type boardType, CartInfo fromINes, Unif fromUnif)
			{
				BoardType = boardType;
				Choice = choice;
				FromINes = fromINes;
				FromUnif = fromUnif;
				InitCallback = null;
			}

			internal void Deconstruct(out CartInfo choice, out Type boardType, out CartInfo fromINes, out Unif fromUnif)
			{
				boardType = BoardType;
				choice = Choice;
				fromINes = FromINes;
				fromUnif = FromUnif;
			}
		}

		[CoreConstructor(VSystemID.Raw.NES)]
		public NES(CoreComm comm, GameInfo game, byte[] rom, NESSettings settings, NESSyncSettings syncSettings, bool subframe = false)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			var fdsBios = comm.CoreFileProvider.GetFirmware(new("NES", "Bios_FDS"));
			if (fdsBios != null && fdsBios.Length == 40976)
			{
				comm.ShowMessage("Your FDS BIOS is a bad dump.  BizHawk will attempt to use it, but no guarantees!  You should find a new one.");
				var tmp = new byte[8192];
				Buffer.BlockCopy(fdsBios, 16 + 8192 * 3, tmp, 0, 8192);
				fdsBios = tmp;
			}

			SyncSettings = syncSettings ?? new NESSyncSettings();
			ControllerSettings = SyncSettings.Controls;

			videoProvider = new MyVideoProvider(this);
			Init(game, rom, fdsBios);
			if (Board is FDS fds)
			{
				DriveLightEnabled = true;
				fds.SetDriveLightCallback(val => DriveLightOn = val);
				// bit of a hack: we don't have a private gamedb for FDS, but the frontend
				// expects this to be set.
				RomStatus = game.Status;
			}
			PutSettings(settings ?? new NESSettings());

			// we need to put this here because the line directly above will overwrite palette intialization anywhere else
			// TODO: What if settings are later loaded?
			if (_isVS)
			{
				PickVSPalette(cart);
			}

			ser.Register<IDisassemblable>(cpu);

			Tracer = new TraceBuffer(cpu.TraceHeader);
			ser.Register<ITraceable>(Tracer);
			ser.Register<IVideoProvider>(videoProvider);
			ser.Register<ISoundProvider>(this);
			ser.Register<IStatable>(new StateSerializer(SyncState)
			{
				LoadStateCallback = SetupMemoryDomains
			});

			if (Board is BANDAI_FCG_1 bandai)
			{
				var reader = bandai.reader;
				// not all BANDAI FCG 1 boards have a barcode reader
				if (reader != null)
				{
					ser.Register(reader);
				}
			}

			// only the subframe core should have ICycleTiming registered
			if (!subframe)
			{
				ser.Unregister<ICycleTiming>();
			}

			ResetControllerDefinition(subframe);
		}

		private static readonly bool USE_DATABASE = true;
		public RomStatus RomStatus;

		public string RomDetails { get; private set; }

		public IEmulatorServiceProvider ServiceProvider { get; }

		private NES()
		{
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
			=> Board.MapperProps().Any();

		public bool IsVS => _isVS;

		public bool IsFDS => Board is FDS;

		public DisplayType Region => _display_type;

		int IVideoLogicalOffsets.ScreenX => Settings.ClipLeftAndRight
			? 8
			: 0;

		int IVideoLogicalOffsets.ScreenY => Region == DisplayType.NTSC
			? Settings.NTSC_TopLine
			: Settings.PAL_TopLine;

		public class MyVideoProvider : IVideoProvider
		{
			//public int ntsc_top = 8;
			//public int ntsc_bottom = 231;
			//public int pal_top = 0;
			//public int pal_bottom = 239;
			public int left = 0;
			public int right = 255;
			private readonly NES emu;
			public MyVideoProvider(NES emu)
			{
				this.emu = emu;
			}

			private readonly int[] pixels = new int[256 * 240];
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
			public int VirtualWidth => (int)(BufferWidth * 1.146);
			public int VirtualHeight => BufferHeight;
			public int BufferWidth => right - left + 1;
			public int BackgroundColor => 0;

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

			public int VsyncNumerator => emu.VsyncNum;
			public int VsyncDenominator => emu.VsyncDen;
		}

		public MyVideoProvider videoProvider;

		[Obsolete] // with the changes to both nes and quicknes cores, nothing uses this anymore
		public static readonly ControllerDefinition NESController =
			new ControllerDefinition("NES Controller")
			{
				BoolButtons = {
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Start", "P1 Select", "P1 B", "P1 A", "Reset", "Power",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Start", "P2 Select", "P2 B", "P2 A"
				}
			}.MakeImmutable();

		public ControllerDefinition ControllerDefinition { get; private set; }

		private int _frame;
		public int Frame
		{
			get => _frame;
			set => _frame = value;
		}

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			islag = false;
		}

		public long Timestamp { get; private set; }

		public bool DeterministicEmulation => true;

		public string SystemId => VSystemID.Raw.NES;

		public string GameName => game_name;

		public static InitResult InitInner(
			byte[] rom,
			Dictionary<string, string> InitialMapperRegisterValues,
			StringWriter log,
			out EDetectionOrigin origin)
		{
			log.WriteLine("------");
			log.WriteLine("BEGIN NES rom analysis:");
			byte[] file = rom;

			Type boardType = null;
			CartInfo choice = null;
			CartInfo iNesHeaderInfo = null;
			CartInfo iNesHeaderInfoV2 = null;
			List<string> hash_sha1_several = new List<string>();
			string hash_sha1 = null, hash_md5 = null;
			Unif unif = null;

			origin = EDetectionOrigin.None;

			if (file.Length < 16) throw new Exception("Alleged NES rom too small to be anything useful");
			if (file.AsSpan(start: 0, length: 4).SequenceEqual("UNIF"u8))
			{
				unif = new Unif(new MemoryStream(file));
				log.WriteLine("Found UNIF header:");
				log.WriteLine(unif.Cart);
				log.WriteLine("Since this is UNIF we can confidently parse PRG/CHR banks to hash.");
				hash_sha1 = unif.Cart.Sha1;
				hash_sha1_several.Add(hash_sha1);
				log.WriteLine("headerless rom hash: {0}", hash_sha1);
			}
			else if(file.AsSpan(0, 5).SequenceEqual("NESM\x1A"u8))
			{
				origin = EDetectionOrigin.NSF;
				log.WriteLine("Loading as NSF");
			return new((core, _) =>
			{
				var nsf = new NSFFormat();
				nsf.WrapByteArray(file);

				core.cart = new();
				var nsfboard = new NSFBoard();
				nsfboard.Create(core);
				nsfboard.Rom = rom;
				nsfboard.InitNSF( nsf);
				nsfboard.InitialRegisterValues = InitialMapperRegisterValues;
				nsfboard.Configure(core.origin);
				nsfboard.Wram = new byte[core.cart.WramSize * 1024];

				return (nsfboard, "Using NTSC display type for NSF for now");
			});
			}
			else if (file.AsSpan(start: 0, length: 4).SequenceEqual("FDS\x1A"u8)
				|| file.AsSpan(start: 0, length: 4).SequenceEqual("\x01*NI"u8))
			{
				// danger!  this is a different codepath with an early return.  accordingly, some
				// code is duplicated twice...

				// FDS roms are just fed to the board, we don't do much else with them
				origin = EDetectionOrigin.FDS;
				log.WriteLine("Found FDS header.");
			return new((core, fdsbios) =>
			{
				if (fdsbios == null)
					throw new MissingFirmwareException("Missing FDS Bios");
				core.cart = new();
				var fdsboard = new FDS();
				fdsboard.biosrom = fdsbios;
				fdsboard.SetDiskImage(rom);
				fdsboard.Create(core);
				// at the moment, FDS doesn't use the IRVs, but it could at some point in the future
				fdsboard.InitialRegisterValues = InitialMapperRegisterValues;
				fdsboard.Configure(core.origin);

				//create the vram and wram if necessary
				if (core.cart.WramSize is not 0) fdsboard.Wram = new byte[core.cart.WramSize * 1024];
				if (core.cart.VramSize is not 0) fdsboard.Vram = new byte[core.cart.VramSize * 1024];

				return (fdsboard, "Using NTSC display type for FDS disk image");
			});
			}
			else
			{
				bool exists = true;

				if (!DetectFromINES(file.AsSpan(start: 0, length: 16), out iNesHeaderInfo, out iNesHeaderInfoV2))
				{
					// we don't have an ines header, check if the game hash is in the game db
					exists = false;
					Console.WriteLine("headerless ROM, using Game DB");
					hash_md5 = MD5Checksum.ComputePrefixedHex(file);
					hash_sha1 = SHA1Checksum.ComputePrefixedHex(file);
					choice = IdentifyFromGameDB(hash_md5) ?? IdentifyFromGameDB(hash_sha1);
					if (choice==null)
					{
						hash_sha1_several.Add(hash_sha1);
						choice = IdentifyFromBootGodDB(hash_sha1_several);
						if (choice == null)
							log.WriteLine("Could not locate game in nescartdb");
						else
						{
							log.WriteLine("Chose board from nescartdb:");
							log.WriteLine(choice);
							origin = EDetectionOrigin.BootGodDB;
						}
					}
					if (choice == null)
						throw new HeaderlessRomException();
				}

				if (exists)
				{
					//now that we know we have an iNES header, we can try to ignore it.

					var trimmed = file.AsSpan(start: 16, length: file.Length - 16);
					hash_sha1 = SHA1Checksum.ComputePrefixedHex(trimmed);
					hash_sha1_several.Add(hash_sha1);
					hash_md5 = MD5Checksum.ComputePrefixedHex(trimmed);

					log.WriteLine("Found iNES header:");
					log.WriteLine(iNesHeaderInfo.ToString());
					if (iNesHeaderInfoV2 != null)
					{
						log.WriteLine("Found iNES V2 header:");
						log.WriteLine(iNesHeaderInfoV2);
					}
					log.WriteLine("Since this is iNES we can (somewhat) confidently parse PRG/CHR banks to hash.");

					log.WriteLine("headerless rom hash: {0}", hash_sha1);
					log.WriteLine("headerless rom hash:  {0}", hash_md5);

					if (iNesHeaderInfo.PrgSize == 16)
					{
						//8KB prg can't be stored in iNES format, which counts 16KB prg banks.
						//so a correct hash will include only 8KB.
						log.WriteLine("Since this rom has a 16 KB PRG, we'll hash it as 8KB too for bootgod's DB:");
						var msTemp = new MemoryStream();
						msTemp.Write(file, 16, 8 * 1024); //add prg
						if (file.Length >= (16 * 1024 + iNesHeaderInfo.ChrSize * 1024 + 16))
						{
							// This assumes that even though the PRG is only 8k the CHR is still written
							// 16k into the file, which is not always the case (e.x. Galaxian RevA)
							msTemp.Write(file, 16 + 16 * 1024, iNesHeaderInfo.ChrSize * 1024); //add chr
						}
						else if (file.Length >= (8 * 1024 + iNesHeaderInfo.ChrSize * 1024 + 16))
						{
							// maybe the PRG is only 8k
							msTemp.Write(file, 16 + 8 * 1024, iNesHeaderInfo.ChrSize * 1024); //add chr
						}
						else
						{
							// we failed somehow
							// most likely the header is wrong
							Console.WriteLine("WARNING: 16kb PRG iNES header but unable to parse");
						}
						msTemp.Flush();
						var bytes = msTemp.ToArray();
						var hash = SHA1Checksum.ComputePrefixedHex(bytes);
						log.WriteLine("  PRG (8KB) + CHR hash: {0}", hash);
						hash_sha1_several.Add(hash);
						hash = MD5Checksum.ComputePrefixedHex(bytes);
						log.WriteLine("  PRG (8KB) + CHR hash:  {0}", hash);
					}
				}
			}

			if (USE_DATABASE)
			{
				if (hash_md5 != null) choice = IdentifyFromGameDB(hash_md5);
				if (choice == null)
					choice = IdentifyFromGameDB(hash_sha1);
				if (choice == null)
					log.WriteLine("Could not locate game in bizhawk gamedb");
				else
				{
					origin = EDetectionOrigin.GameDB;
					log.WriteLine("Chose board from bizhawk gamedb: " + choice.BoardType);
					//gamedb entries that don't specify prg/chr sizes can infer it from the ines header
					if (iNesHeaderInfo != null)
					{
						if (choice.PrgSize == -1) choice.PrgSize = iNesHeaderInfo.PrgSize;
						if (choice.ChrSize == -1) choice.ChrSize = iNesHeaderInfo.ChrSize;
						if (choice.VramSize == -1) choice.VramSize = iNesHeaderInfo.VramSize;
						if (choice.WramSize == -1) choice.WramSize = iNesHeaderInfo.WramSize;
					}
					else if (unif != null)
					{
						if (choice.PrgSize == -1) choice.PrgSize = unif.Cart.PrgSize;
						if (choice.ChrSize == -1) choice.ChrSize = unif.Cart.ChrSize;
						// unif has no wram\vram sizes; hope the board impl can figure it out...
						if (choice.VramSize == -1) choice.VramSize = 0;
						if (choice.WramSize == -1) choice.WramSize = 0;
					}
				}

				//if this is still null, we have to try it some other way. nescartdb perhaps?
				if (choice == null)
				{
					choice = IdentifyFromBootGodDB(hash_sha1_several);
					if (choice == null)
						log.WriteLine("Could not locate game in nescartdb");
					else
					{
						log.WriteLine("Chose board from nescartdb:");
						log.WriteLine(choice);
						origin = EDetectionOrigin.BootGodDB;
					}
				}
			}

			//if choice is still null, try UNIF and iNES
			if (choice == null)
			{
				if (unif != null)
				{
					log.WriteLine("Using information from UNIF header");
					choice = unif.Cart;
					//ok, i have this Q-Boy rom with no VROM and no VRAM.
					//we also certainly have games with VROM and no VRAM.
					//looks like FCEUX policy is to allocate 8KB of chr ram no matter what UNLESS certain flags are set. but what's the justification for this? please leave a note if you go debugging in it again.
					//well, we know we can't have much of a NES game if there's no VROM unless there's VRAM instead.
					//so if the VRAM isn't set, choose 8 for it.
					//TODO - unif loading code may need to use VROR flag to transform chr_size=8 to vram_size=8 (need example)
					if (choice.ChrSize == 0 && choice.VramSize == 0)
						choice.VramSize = 8;
					//(do we need to suppress this in case theres a CHR rom? probably not. nes board base will use ram if no rom is available)
					origin = EDetectionOrigin.UNIF;
				}
				if (iNesHeaderInfo != null)
				{
					log.WriteLine("Attempting inference from iNES header");
					// try to spin up V2 header first, then V1 header
					if (iNesHeaderInfoV2 != null)
					{
						try
						{
							boardType = FindBoard(iNesHeaderInfoV2, origin, InitialMapperRegisterValues);
						}
						catch { }
						if (boardType == null)
							log.WriteLine("Failed to load as iNES V2");
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
							log.WriteLine("Failed to load as iNES V1");

						// do not further meddle in wram sizes.  a board that is being loaded from a "MAPPERxxx"
						// entry should know and handle the situation better for the individual board
					}

					log.WriteLine("Chose board from iNES heuristics:");
					log.WriteLine(choice);
					origin = EDetectionOrigin.INES;
				}
			}

			//find a INESBoard to handle this
			if (choice != null)
				boardType = FindBoard(choice, origin, InitialMapperRegisterValues);
			else
				throw new Exception("Unable to detect ROM");
			if (boardType == null)
				throw new Exception("No class implements the necessary board type: " + choice.BoardType);

			if (choice.GameInfo != null)
				choice.Bad = choice.GameInfo.IsRomStatusBad();

			log.WriteLine("Final game detection results:");
			log.WriteLine(choice);
			log.WriteLine("\"{0}\"", choice.Name);
			log.WriteLine("Implemented by: class " + boardType.Name);
			if (choice.Bad)
			{
				log.WriteLine("~~ ONE WAY OR ANOTHER, THIS DUMP IS KNOWN TO BE *BAD* ~~");
				log.WriteLine("~~ YOU SHOULD FIND A BETTER FILE ~~");
			}

			log.WriteLine("END NES rom analysis");
			log.WriteLine("------");

			return new(choice, boardType, iNesHeaderInfo, unif);
		}

		public void Init(GameInfo gameInfo, byte[] rom, byte[] fdsbios = null)
		{
			Dictionary<string, string> InitialMapperRegisterValues = new(SyncSettings.BoardProperties);

			StringWriter LoadReport = new();
			InitResult result;
			try
			{
				result = InitInner(rom, InitialMapperRegisterValues, LoadReport, out origin);
			}
			catch (HeaderlessRomException) when (gameInfo.Status is RomStatus.BadDump)
			{
				_ = DetectFromINES("NES\x1A\x02\x04\x10\x00\x00\x00\x00\x00\x00\x00\x00\x00"u8, out var cartInfo, out _);
//				cartInfo.BoardType = "MAPPER001";
				result = new(cartInfo, FindBoard(cartInfo, origin, InitialMapperRegisterValues), null, null);
			}
			finally
			{
				LoadReport.Flush();
				Console.Write(LoadReport);
			}
			void LoadWriteLine(string format, params object[] arg)
			{
				Console.WriteLine(format, arg);
				LoadReport.WriteLine(format, arg);
			}

			if (result.InitCallback is not null)
			{
				(Board, var regionAssumedWarning) = result.InitCallback(this, fdsbios);
				Board.PostConfigure();
				AutoMapperProps.Populate(Board, SyncSettings);
				Console.WriteLine(regionAssumedWarning);
				_display_type = DisplayType.NTSC;
				HardReset();
				return;
			}
			// else reached end
			var file = rom;
			var (choice, boardType, iNesHeaderInfo, unif) = result;
			game_name = choice.Name;

			Board = CreateBoardInstance(boardType);

			cart = choice;
			Board.Create(this);
			Board.InitialRegisterValues = InitialMapperRegisterValues;
			Board.Configure(origin);

			string romDetailsHeader = "";
			if (origin == EDetectionOrigin.BootGodDB)
			{
				RomStatus = RomStatus.GoodDump;
				romDetailsHeader = "Identified from BootGod's database";
			}
			if (origin == EDetectionOrigin.UNIF)
			{
				RomStatus = RomStatus.NotInDatabase;
				romDetailsHeader = "Inferred from UNIF header; somewhat suspicious";
			}
			if (origin == EDetectionOrigin.INES)
			{
				RomStatus = RomStatus.NotInDatabase;
				romDetailsHeader = "Inferred from iNES header; potentially wrong";
			}

			if (origin == EDetectionOrigin.GameDB)
			{
				RomStatus = choice.Bad
					? RomStatus.BadDump
					: choice.GameInfo.Status;
			}

			byte[] trainer = null;

			//create the board's rom and vrom
			if (iNesHeaderInfo != null)
			{
				using var ms = new MemoryStream(file, false);
				ms.Seek(16, SeekOrigin.Begin); // ines header
				//pluck the necessary bytes out of the file
				if (iNesHeaderInfo.TrainerSize != 0)
				{
					trainer = new byte[512];
					ms.Read(trainer, 0, 512);
				}

				Board.Rom = new byte[choice.PrgSize * 1024];
				ms.Read(Board.Rom, 0, Board.Rom.Length);

				if (choice.ChrSize > 0)
				{
					Board.Vrom = new byte[choice.ChrSize * 1024];
					int vrom_copy_size = ms.Read(Board.Vrom, 0, Board.Vrom.Length);

					if (vrom_copy_size < Board.Vrom.Length)
						LoadWriteLine("Less than the expected VROM was found in the file: {0} < {1}", vrom_copy_size, Board.Vrom.Length);
				}
				if (choice.PrgSize != iNesHeaderInfo.PrgSize || choice.ChrSize != iNesHeaderInfo.ChrSize)
					LoadWriteLine("Warning: Detected choice has different filesizes than the INES header!");
			}
			else if (unif != null)
			{
				Board.Rom = unif.Prg;
				Board.Vrom = unif.Chr;
			}
			else
			{
				// we should only get here for boards with no header
				var ms = new MemoryStream(file, false);
				ms.Seek(0, SeekOrigin.Begin);

				Board.Rom = new byte[choice.PrgSize * 1024];
				ms.Read(Board.Rom, 0, Board.Rom.Length);

				if (choice.ChrSize > 0)
				{
					Board.Vrom = new byte[choice.ChrSize * 1024];
					int vrom_copy_size = ms.Read(Board.Vrom, 0, Board.Vrom.Length);

					if (vrom_copy_size < Board.Vrom.Length)
						LoadWriteLine("Less than the expected VROM was found in the file: {0} < {1}", vrom_copy_size, Board.Vrom.Length);
				}
			}

			LoadReport.Flush();
			RomDetails = romDetailsHeader + "\n\n" + LoadReport;

			// IF YOU DO ANYTHING AT ALL BELOW THIS LINE, MAKE SURE THE APPROPRIATE CHANGE IS MADE TO FDS (if applicable)

			//create the vram and wram if necessary
			if (cart.WramSize != 0)
				Board.Wram = new byte[cart.WramSize * 1024];
			if (cart.VramSize != 0)
				Board.Vram = new byte[cart.VramSize * 1024];

			Board.PostConfigure();
			AutoMapperProps.Populate(Board, SyncSettings);

			// set up display type

			NESSyncSettings.Region fromrom = DetectRegion(cart.System);
			NESSyncSettings.Region fromsettings = SyncSettings.RegionOverride;

			if (fromsettings != NESSyncSettings.Region.Default)
			{
				Console.WriteLine("Using system region override");
				fromrom = fromsettings;
			}

			_display_type = fromrom switch
			{
				NESSyncSettings.Region.Dendy => DisplayType.Dendy,
				NESSyncSettings.Region.NTSC => DisplayType.NTSC,
				NESSyncSettings.Region.PAL => DisplayType.PAL,
				_ => DisplayType.NTSC
			};
			Console.WriteLine("Using NES system region of {0}", _display_type);

			HardReset();

			if (trainer != null)
			{
				Console.WriteLine("Applying trainer");
				for (int i = 0; i < 512; i++)
					WriteMemory((ushort)(0x7000 + i), trainer[i]);
			}
		}

		private static NESSyncSettings.Region DetectRegion(string system)
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

		private ITraceable Tracer { get; }
	}
}

//todo
//http://blog.ntrq.net/?p=428

//A VERY NICE board assignments list
//http://personales.epsg.upv.es/~jogilmo1/nes/TEXTOS/ARXIUS/BOARDTABLE.TXT

//why not make boards communicate over the actual board pinouts
//http://wiki.nesdev.com/w/index.php/Cartridge_connector

//a mappers list
//http://tuxnes.sourceforge.net/nesmapper.txt
