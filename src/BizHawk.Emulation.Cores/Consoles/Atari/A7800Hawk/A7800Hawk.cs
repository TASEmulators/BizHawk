using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	[Core(CoreNames.A7800Hawk, "")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(ISettable<,>) })]
	public partial class A7800Hawk : IEmulator, ISaveRam, IDebuggable, IInputPollable,
		IRegionable, IBoardInfo, ISettable<A7800Hawk.A7800Settings, A7800Hawk.A7800SyncSettings>
	{
		internal static class RomChecksums
		{
			public const string KaratekaPAL = "MD5:5E0A1E832BBCEA6FACB832FDE23A440A";

			public const string Serpentine = "MD5:9BD70C06D3386F76F8162881699A777A";
		}

		// this register selects between 2600 and 7800 mode in the A7800
		// however, we already have a 2600 emulator so this core will only be loading A7800 games
		// furthermore, the location of the register is in the same place as TIA registers (0x0-0x1F)
		// any writes to this location before the register is 'locked' will go to the register and not the TIA
		public byte A7800_control_register;

		// memory domains
		public byte[] Maria_regs = new byte[0x20];
		public byte[] RAM = new byte[0x1000];
		public byte[] RAM_6532 = new byte[0x80];
		public byte[] hs_bios_mem = new byte[0x800];
		public byte[] _hsram = new byte[2048];

		public readonly byte[] _rom;
		public readonly byte[] _hsbios;
		public readonly byte[] _bios;

		private int _frame = 0;

		public string s_mapper;
		public MapperBase mapper;
		public bool small_flag = false;
		public bool PAL_Kara = false;
		public int cart_RAM = 0;
		public bool is_pokey = false;
		public bool is_pokey_450 = false;

		private readonly ITraceable _tracer;

		public MOS6502X<CpuLink> cpu;
		public Maria maria;
		public bool _isPAL;
		public M6532 m6532;
		public TIA tia;
		public Pokey pokey;

		public struct CpuLink : IMOS6502XLink
		{
			private readonly A7800Hawk _a7800;

			public CpuLink(A7800Hawk a7800)
			{
				_a7800 = a7800;
			}

			public byte DummyReadMemory(ushort address) => _a7800.ReadMemory(address);

			public void OnExecFetch(ushort address) => _a7800.ExecFetch(address);

			public byte PeekMemory(ushort address) => _a7800.ReadMemory(address);

			public byte ReadMemory(ushort address) => _a7800.ReadMemory(address);

			public void WriteMemory(ushort address, byte value) => _a7800.WriteMemory(address, value);
		}

		[CoreConstructor(VSystemID.Raw.A78)]
		public A7800Hawk(CoreComm comm, byte[] rom, A7800Hawk.A7800Settings settings, A7800Hawk.A7800SyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			maria = new Maria();
			tia = new TIA();
			m6532 = new M6532();
			pokey = new Pokey();

			cpu = new MOS6502X<CpuLink>(new CpuLink(this));

			maria = new Maria
			{
				ReadMemory = ReadMemory
			};

			_blip.SetRates(1789773, 44100);

			_settings = (A7800Settings)settings ?? new A7800Settings();
			_syncSettings = (A7800SyncSettings)syncSettings ?? new A7800SyncSettings();
			_controllerDeck = new A7800HawkControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			var highscoreBios = comm.CoreFileProvider.GetFirmware(new("A78", "Bios_HSC"), "Some functions may not work without the high score BIOS.");
			var palBios = comm.CoreFileProvider.GetFirmware(new("A78", "Bios_PAL"), "The game will not run if the correct region BIOS is not available.");
			var ntscBios = comm.CoreFileProvider.GetFirmware(new("A78", "Bios_NTSC"), "The game will not run if the correct region BIOS is not available.");

			byte[] header = new byte[128];
			bool is_header = false;

			if (rom.Length % 1024 == 128)
			{
				Console.WriteLine("128 byte header detected");
				byte[] newrom = new byte[rom.Length - 128];
				is_header = true;
				Buffer.BlockCopy(rom, 0, header, 0, 128);
				Buffer.BlockCopy(rom, 128, newrom, 0, newrom.Length);
				rom = newrom;
			}

			_isPAL = false;

			// look up hash in gamedb to see what mapper to use
			// if none found default is zero
			// also check for PAL region
			s_mapper = null;
			var hash_md5 = MD5Checksum.ComputePrefixedHex(rom);

			var gi = Database.CheckDatabase(hash_md5);

			if (gi != null)
			{
				var dict = gi.GetOptions();
				if (dict.ContainsKey("PAL"))
				{
					_isPAL = true;
				}

				if (!dict.TryGetValue("board", out s_mapper)) throw new Exception("No Board selected for this game");

				// check if the game uses pokey or RAM
				if (dict.TryGetValue("RAM", out var cartRAMStr))
				{
					int.TryParse(cartRAMStr, out cart_RAM);
				}

				if (dict.TryGetValue("Pokey", out var pokeyStr))
				{
					bool.TryParse(pokeyStr, out is_pokey);
				}

				if (dict.TryGetValue("Pokey_450", out var pokey450Str))
				{
					bool.TryParse(pokey450Str, out is_pokey_450);
				}

				// some games will not function with the high score bios
				// if such a game is being played, tell the user and disable it
				if (dict.TryGetValue("No_HS", out var noHSStr))
				{
					bool.TryParse(noHSStr, out var no_hs);

					if (no_hs)
					{
						Console.WriteLine("This game is incompatible with the High Score BIOS, disabling it");
						highscoreBios = null;
					}
				}
			}
			else if (is_header)
			{
				Console.WriteLine("ROM not in DB, inferring mapper info from header");

				byte cart_1 = header[0x35];
				byte cart_2 = header[0x36];

				_isPAL = (header[0x39] > 0);

				if (cart_2.Bit(1))
				{
					if (cart_2.Bit(3))
					{
						s_mapper = "2";
					}
					else
					{
						s_mapper = "1";
					}
					
					if (cart_2.Bit(2))
					{
						cart_RAM = 8;

						// the homebrew game serpentine requires extra RAM, but in the alternative style
						if (hash_md5 == RomChecksums.Serpentine)
						{
							cart_RAM = 16;
						}
					}
				}
				else
				{
					s_mapper = "0";
				}

				if (cart_2.Bit(0)) { is_pokey = true; }

				// the homebrew game serpentine requires the pokey chip to be available at the alternative location 0x450
				if (cart_2.Bit(6)) { is_pokey_450 = true; }
			}
			else
			{
				throw new Exception("ROM not in gamedb and has no header");
			}

			// some games that use the Super Game mapper only have 4 banks, so let's set a flag to limit bank size
			if (rom.Length < 0x14000)
			{
				small_flag = true;

				// additionally, PAL Karateka  has bank 6 (actually 2) at 0x4000
				if (hash_md5 == RomChecksums.KaratekaPAL)
				{
					PAL_Kara = true;
				}
			}

			_rom = rom;

			Reset_Mapper(s_mapper);
			
			_hsbios = highscoreBios;
			_bios = _isPAL ? palBios : ntscBios;

			if (_bios == null)
			{
				throw new MissingFirmwareException("The BIOS corresponding to the region of the game you loaded is required to run Atari 7800 games.");
			}

			// set up palette and frame rate
			if (_isPAL)
			{
				_frameHz = 50;
				_screen_width = 320;
				_screen_height = 313;
				_vblanklines = 20;
				maria._palette = PALPalette;
			}
			else
			{
				_frameHz = 60;
				_screen_width = 320;
				_screen_height = 263;
				_vblanklines = 20;
				maria._palette = NTSCPalette;
			}

			maria.Core = this;
			m6532.Core = this;
			tia.Core = this;
			pokey.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(this);
			ServiceProvider = ser;

			_tracer = new TraceBuffer(cpu.TraceHeader);
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
			ser.Register<IDisassemblable>(cpu);
			HardReset();
		}

		public string BoardName => mapper.GetType().Name.Replace("Mapper", "");

		public DisplayType Region => _isPAL ? DisplayType.PAL : DisplayType.NTSC;

		private readonly A7800HawkControllerDeck _controllerDeck;

		private void HardReset()
		{
			A7800_control_register = 0;

			tia.Reset();
			cpu.Reset();

			maria.Reset();
			m6532.Reset();
			pokey.Reset();
			
			Maria_regs = new byte[0x20];
			RAM = new byte[0x1000];

			cpu_cycle = 0;

			_vidbuffer = new int[VirtualWidth * VirtualHeight];

			master_audio_clock = 0;
			samp_c = samp_l = 0;
		}

		private void ExecFetch(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)(MemoryCallbackFlags.AccessExecute);
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void Reset_Mapper(string m)
		{
			if (m == "0")
			{
				mapper = new MapperDefault();
			}
			if (m == "1")
			{
				mapper = new MapperSG();
			}
			if (m == "2")
			{
				mapper = new MapperSGE();
			}
			if (m == "3")
			{
				mapper = new MapperF18();
			}
			if (m == "4")
			{
				mapper = new MapperRampage();
			}
			if (m == "5")
			{
				mapper = new MapperFractalus();
			}
			if (m == "6")
			{
				mapper = new MapperFractalus();
			}

			mapper.Core = this;
			mapper.mask = _rom.Length / 0x4000 - 1;
		}

		/*
		 * MariaTables.cs
		 *
		 * Palette tables for the Maria class.
		 * PAL derived from Dan Boris' 7800/MAME code.
		 *
		 * Region tables sourced from Trebor's palette research work: https://forums.atariage.com/topic/322259-trebors-pro-palettes-and-colors-guide-with-conversions/?do=findComment&comment=5024097
		 *
		 * CRT_V3_EVN-WARM leveraged for both regions.
		 * 
		 * 
		 */

		public static readonly int[] NTSCPalette =
		{
			0x000000, 0x0d0d0d, 0x282828, 0x3e3e3e,
			0x525252, 0x656565, 0x777777, 0x888888,
			0x989898, 0xa8a8a8, 0xb7b7b7, 0xc6c6c6,
			0xd5d5d5, 0xe3e3e3, 0xf1f1f1, 0xffffff,

			0x441100, 0x572a00, 0x693f00, 0x795200,
			0x896400, 0x997500, 0xa88500, 0xb79500,
			0xc5a400, 0xd3b300, 0xe1c100, 0xeecf00,
			0xfbdd0f, 0xffea29, 0xfff83e, 0xffff51,

			0x700000, 0x810d00, 0x902700, 0xa03c00,
			0xae5000, 0xbd6200, 0xcb7300, 0xd98300,
			0xe79300, 0xf4a200, 0xffb119, 0xffbf30,
			0xffcd45, 0xffdb57, 0xffe869, 0xfff679,

			0x860000, 0x960000, 0xa50d00, 0xb42700,
			0xc23d00, 0xd05003, 0xde6221, 0xeb7337,
			0xf9834b, 0xff935d, 0xffa26e, 0xffb17f,
			0xffbf8e, 0xffcd9e, 0xffdbad, 0xffe9bb,

			0x860010, 0x950029, 0xa4003f, 0xb31852,
			0xc13063, 0xcf4474, 0xdd5785, 0xeb6894,
			0xf879a3, 0xff89b2, 0xff98c1, 0xffa8cf,
			0xffb6dc, 0xffc4ea, 0xffd2f7, 0xffe0ff,

			0x6f0079, 0x7f0089, 0x8f0099, 0x9e14a8,
			0xad2db6, 0xbc41c5, 0xca54d3, 0xd866e0,
			0xe577ee, 0xf387fb, 0xff96ff, 0xffa6ff,
			0xffb4ff, 0xffc3ff, 0xffd1ff, 0xffdeff,

			0x4200b7, 0x5500c6, 0x6600d4, 0x771ee1,
			0x8735ef, 0x9749fc, 0xa65bff, 0xb46cff,
			0xc37dff, 0xd18dff, 0xdf9cff, 0xecabff,
			0xf9baff, 0xffc8ff, 0xffd6ff, 0xffe4ff,

			0x0000d4, 0x1400e1, 0x2c1aef, 0x4131fc,
			0x5445ff, 0x6658ff, 0x7669ff, 0x877aff,
			0x968aff, 0xa59aff, 0xb4a9ff, 0xc2b7ff,
			0xd0c6ff, 0xded3ff, 0xece1ff, 0xf9efff,

			0x0000cd, 0x001cda, 0x0033e8, 0x0047f5,
			0x155aff, 0x2e6bff, 0x427cff, 0x558cff,
			0x679bff, 0x77aaff, 0x88b8ff, 0x97c7ff,
			0xa6d5ff, 0xb5e2ff, 0xc3f0ff, 0xd1fdff,

			0x001fa3, 0x0035b1, 0x0049c0, 0x005cce,
			0x006ddc, 0x007de9, 0x0f8df7, 0x299dff,
			0x3eacff, 0x51baff, 0x63c8ff, 0x74d6ff,
			0x84e4ff, 0x94f1ff, 0xa3ffff, 0xb2ffff,

			0x003456, 0x004868, 0x005a79, 0x006c89,
			0x007c98, 0x008ca7, 0x009bb6, 0x0faac4,
			0x28b9d2, 0x3ec7e0, 0x51d5ed, 0x63e3fb,
			0x74f0ff, 0x84fdff, 0x94ffff, 0xa3ffff,

			0x003e00, 0x005100, 0x00630a, 0x007425,
			0x00843a, 0x00944e, 0x00a360, 0x1ab271,
			0x31c081, 0x45ce91, 0x58dca0, 0x69eaaf,
			0x7af7be, 0x8affcc, 0x9affda, 0xa9ffe7,

			0x003e00, 0x005100, 0x006300, 0x007400,
			0x008400, 0x129400, 0x2ba300, 0x40b206,
			0x53c023, 0x65ce39, 0x76dc4c, 0x86ea5e,
			0x95f770, 0xa4ff80, 0xb3ff90, 0xc2ff9f,

			0x003400, 0x004800, 0x085a00, 0x246c00,
			0x397c00, 0x4d8c00, 0x5f9c00, 0x70ab00,
			0x81b900, 0x90c700, 0xa0d500, 0xafe313,
			0xbdf02b, 0xcbfe40, 0xd9ff53, 0xe7ff65,

			0x251f00, 0x3b3600, 0x4e4a00, 0x605c00,
			0x716d00, 0x827e00, 0x928e00, 0xa19d00,
			0xb0ac00, 0xbeba00, 0xccc900, 0xdad700,
			0xe8e406, 0xf5f223, 0xffff39, 0xffff4c,

			0x5d0000, 0x6e1c00, 0x7f3300, 0x8f4700,
			0x9e5a00, 0xad6b00, 0xbb7c00, 0xc98c00,
			0xd79b00, 0xe5aa00, 0xf2b900, 0xffc70b,
			0xffd526, 0xffe33b, 0xfff04f, 0xfffd61
		};

		public static readonly int[] PALPalette =
		{
			0x000000, 0x0d0d0d, 0x282828, 0x3e3e3e,
			0x525252, 0x656565, 0x777777, 0x888888,
			0x989898, 0xa8a8a8, 0xb7b7b7, 0xc6c6c6,
			0xd5d5d5, 0xe3e3e3, 0xf1f1f1, 0xffffff,

			0x002c00, 0x144100, 0x2c5400, 0x416500,
			0x547600, 0x668600, 0x769600, 0x87a500,
			0x96b400, 0xa5c200, 0xb4d000, 0xc2de00,
			0xd0eb15, 0xdef92d, 0xecff42, 0xf9ff55,

			0x421200, 0x552b00, 0x664000, 0x775300,
			0x876500, 0x977600, 0xa68600, 0xb59500,
			0xc3a500, 0xd1b300, 0xdfc200, 0xecd000,
			0xf9dd0d, 0xffeb27, 0xfff83d, 0xffff50,

			0x6f0000, 0x7f0e00, 0x8f2800, 0x9e3d00,
			0xad5100, 0xbc6200, 0xca7300, 0xd88400,
			0xe59300, 0xf3a300, 0xffb114, 0xffc02d,
			0xffce42, 0xffdc54, 0xffe966, 0xfff677,

			0x860000, 0x950000, 0xa40e00, 0xb32800,
			0xc13d00, 0xd05100, 0xdd621b, 0xeb7332,
			0xf88447, 0xff9359, 0xffa36b, 0xffb17b,
			0xffc08b, 0xffce9b, 0xffdcaa, 0xffe9b8,

			0x860009, 0x960024, 0xa5003a, 0xb3184e,
			0xc23060, 0xd04471, 0xde5781, 0xeb6991,
			0xf879a0, 0xff89af, 0xff99bd, 0xffa8cc,
			0xffb6d9, 0xffc5e7, 0xffd3f4, 0xffe0ff,

			0x700076, 0x800086, 0x900096, 0x9f14a5,
			0xae2cb4, 0xbd41c2, 0xcb54d0, 0xd966de,
			0xe677eb, 0xf487f9, 0xff96ff, 0xffa5ff,
			0xffb4ff, 0xffc2ff, 0xffd1ff, 0xffdeff,

			0x4400b6, 0x5600c4, 0x6800d2, 0x791ee0,
			0x8934ed, 0x9848fb, 0xa75bff, 0xb66cff,
			0xc47dff, 0xd28dff, 0xe09cff, 0xedabff,
			0xfbb9ff, 0xffc8ff, 0xffd6ff, 0xffe3ff,

			0x0000d3, 0x1600e1, 0x2e19ee, 0x4330fc,
			0x5645ff, 0x6757ff, 0x7869ff, 0x887aff,
			0x988aff, 0xa799ff, 0xb5a8ff, 0xc4b7ff,
			0xd2c5ff, 0xdfd3ff, 0xede1ff, 0xfaeeff,

			0x0000cd, 0x001bdb, 0x0032e9, 0x0046f6,
			0x1859ff, 0x2f6aff, 0x447bff, 0x578bff,
			0x689aff, 0x79a9ff, 0x89b8ff, 0x98c6ff,
			0xa7d4ff, 0xb6e2ff, 0xc4efff, 0xd2fdff,

			0x001ea4, 0x0035b3, 0x0049c1, 0x005bcf,
			0x006cdd, 0x007deb, 0x118df8, 0x2a9cff,
			0x3fabff, 0x52baff, 0x64c8ff, 0x75d6ff,
			0x85e4ff, 0x95f1ff, 0xa4feff, 0xb2ffff,

			0x003359, 0x00476a, 0x005a7b, 0x006b8b,
			0x007c9a, 0x008ca9, 0x009bb8, 0x0faac6,
			0x29b9d4, 0x3ec7e2, 0x51d5ef, 0x63e3fc,
			0x74f0ff, 0x84fdff, 0x94ffff, 0xa3ffff,

			0x003e00, 0x005100, 0x00630e, 0x007428,
			0x00843d, 0x009450, 0x00a362, 0x19b273,
			0x31c083, 0x45ce93, 0x58dca2, 0x69eab1,
			0x7af7bf, 0x8affce, 0x99ffdb, 0xa8ffe9,

			0x003e00, 0x005100, 0x006300, 0x007400,
			0x008400, 0x119400, 0x2aa300, 0x3fb20a,
			0x52c025, 0x64cf3b, 0x75dc4e, 0x85ea60,
			0x95f771, 0xa4ff82, 0xb2ff91, 0xc1ffa1,

			0x003400, 0x004800, 0x065b00, 0x226c00,
			0x387c00, 0x4c8c00, 0x5e9c00, 0x6fab00,
			0x80b900, 0x90c800, 0x9fd500, 0xaee314,
			0xbcf12d, 0xcafe41, 0xd8ff54, 0xe6ff66,

			0x242000, 0x3a3600, 0x4d4a00, 0x605c00,
			0x716e00, 0x817e00, 0x918e00, 0xa09d00,
			0xafac00, 0xbdbb00, 0xccc900, 0xd9d700,
			0xe7e406, 0xf4f223, 0xffff39, 0xffff4c
		};
	}
}
