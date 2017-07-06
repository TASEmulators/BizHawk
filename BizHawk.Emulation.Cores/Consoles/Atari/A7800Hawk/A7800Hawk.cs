using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	[CoreAttributes(
		"A7800Hawk",
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(typeof(ISettable<,>), typeof(IDriveLight))]
	public partial class A7800Hawk : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable,
	ISettable<A7800Hawk.A7800Settings, A7800Hawk.A7800SyncSettings>
	{
		// this register selects between 2600 and 7800 mode in the A7800
		// however, we already have a 2600 emulator so this core will only be loading A7800 games
		// furthermore, the location of the register is in the same place as TIA registers (0x0-0x1F)
		// any writes to this location before the register is 'locked' will go to the register and not the TIA
		public byte A7800_control_register;

		// memory domains
		public byte[] TIA_regs = new byte[0x20];
		public byte[] Maria_regs = new byte[0x20];
		public byte[] RAM = new byte[0x1000];
		public byte[] RAM_6532 = new byte[0x80];
		public byte[] hs_bios_mem = new byte[0x800];

		public readonly byte[] _rom;
		public readonly byte[] _hsbios;
		public readonly byte[] _bios;
		public readonly byte[] _hsram = new byte[2048];

		private int _frame = 0;

		public string s_mapper;
		public MapperBase mapper;

		private readonly ITraceable _tracer;

		public MOS6502X cpu;
		public Maria maria;
		private bool _isPAL;
		public M6532 m6532;
		public TIA tia;

		public A7800Hawk(CoreComm comm, GameInfo game, byte[] rom, string gameDbFn)
		{
			var ser = new BasicServiceProvider(this);

			maria = new Maria();
			tia = new TIA();
			m6532 = new M6532();

			cpu = new MOS6502X
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				PeekMemory = ReadMemory,
				DummyReadMemory = ReadMemory,
				OnExecFetch = ExecFetch
			};

			maria = new Maria
			{
				ReadMemory = ReadMemory
			};

			CoreComm = comm;

			_controllerDeck = new A7800HawkControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

			byte[] highscoreBios = comm.CoreFileProvider.GetFirmware("A78", "Bios_HSC", false, "Some functions may not work without the high score BIOS.");
			byte[] palBios = comm.CoreFileProvider.GetFirmware("A78", "Bios_PAL", false, "The game will not run if the correct region BIOS is not available.");
			byte[] ntscBios = comm.CoreFileProvider.GetFirmware("A78", "Bios_NTSC", false, "The game will not run if the correct region BIOS is not available.");

			if (rom.Length % 1024 == 128)
			{
				Console.WriteLine("Trimming 128 byte .a78 header...");
				byte[] newrom = new byte[rom.Length - 128];
				Buffer.BlockCopy(rom, 128, newrom, 0, newrom.Length);
				rom = newrom;
			}

			_isPAL = false;

			// look up hash in gamedb to see what mapper to use
			// if none found default is zero
			// also check for PAL region
			string hash_md5 = null;
			s_mapper = null;
			hash_md5 = "md5:" + rom.HashMD5(0, rom.Length);

			var gi = Database.CheckDatabase(hash_md5);

			if (gi != null)
			{
				var dict = gi.GetOptionsDict();
				if (dict.ContainsKey("PAL"))
				{
					_isPAL = true;
				}
				if (dict.ContainsKey("board"))
				{
					s_mapper = dict["board"];
				}
				else
					throw new Exception("No Board selected for this game");
			}
			else
			{
				throw new Exception("ROM not in gamedb");
			}

			Reset_Mapper(s_mapper);

			_rom = rom;
			_hsbios = highscoreBios;
			_bios = _isPAL ? palBios : ntscBios;

			if (_bios == null)
			{
				throw new MissingFirmwareException("The BIOS corresponding to the region of the game you loaded is required to run Atari 7800 games.");
			}

			// set up palette and frame rate
			if (_isPAL)
			{
				maria._frameHz = 50;
				maria._screen_width = 320;
				maria._screen_height = 313;
				maria._palette = PALPalette;
			}
			else
			{
				maria._frameHz = 60;
				maria._screen_width = 320;
				maria._screen_height = 263;
				maria._palette = NTSCPalette;
			}

			maria.Core = this;
			m6532.Core = this;
			tia.Core = this;

			ser.Register<IVideoProvider>(maria);
			ser.Register<ISoundProvider>(tia);
			ServiceProvider = ser;

			_tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);

			SetupMemoryDomains();
			HardReset();
		}

		public DisplayType Region => _isPAL ? DisplayType.PAL : DisplayType.NTSC;

		private readonly A7800HawkControllerDeck _controllerDeck;

		private void HardReset()
		{
			A7800_control_register = 0;

			tia.Reset();
			cpu.Reset();
			cpu.SetCallbacks(ReadMemory, ReadMemory, ReadMemory, WriteMemory);

			maria.Reset();
			m6532.Reset();
			
			TIA_regs = new byte[0x20];
			Maria_regs = new byte[0x20];
			RAM = new byte[0x1000];

			cpu_cycle = 0;
		}

		private void ExecFetch(ushort addr)
		{
			MemoryCallbacks.CallExecutes(addr);
		}

		private void Reset_Mapper(string m)
		{
			if (m=="0")
			{
				mapper = new MapperDefault();
			}

			mapper.Core = this;
		}

		/*
		 * MariaTables.cs
		 *
		 * Palette tables for the Maria class.
		 * All derived from Dan Boris' 7800/MAME code.
		 *
		 * Copyright © 2004 Mike Murphy
		 *
		 */

		public static readonly int[] NTSCPalette =
		{
			0x000000, 0x1c1c1c, 0x393939, 0x595959,  // Grey
            0x797979, 0x929292, 0xababab, 0xbcbcbc,
			0xcdcdcd, 0xd9d9d9, 0xe6e6e6, 0xececec,
			0xf2f2f2, 0xf8f8f8, 0xffffff, 0xffffff,

			0x391701, 0x5e2304, 0x833008, 0xa54716,  // Gold
            0xc85f24, 0xe37820, 0xff911d, 0xffab1d,
			0xffc51d, 0xffce34, 0xffd84c, 0xffe651,
			0xfff456, 0xfff977, 0xffff98, 0xffff98,

			0x451904, 0x721e11, 0x9f241e, 0xb33a20,  // Orange
            0xc85122, 0xe36920, 0xff811e, 0xff8c25,
			0xff982c, 0xffae38, 0xffc545, 0xffc559,
			0xffc66d, 0xffd587, 0xffe4a1, 0xffe4a1,

			0x4a1704, 0x7e1a0d, 0xb21d17, 0xc82119,  // Red Orange
            0xdf251c, 0xec3b38, 0xfa5255, 0xfc6161,
			0xff706e, 0xff7f7e, 0xff8f8f, 0xff9d9e,
			0xffabad, 0xffb9bd, 0xffc7ce, 0xffc7ce,

			0x050568, 0x3b136d, 0x712272, 0x8b2a8c,  // Pink
            0xa532a6, 0xb938ba, 0xcd3ecf, 0xdb47dd,
			0xea51eb, 0xf45ff5, 0xfe6dff, 0xfe7afd,
			0xff87fb, 0xff95fd, 0xffa4ff, 0xffa4ff,

			0x280479, 0x400984, 0x590f90, 0x70249d,  // Purple
            0x8839aa, 0xa441c3, 0xc04adc, 0xd054ed,
			0xe05eff, 0xe96dff, 0xf27cff, 0xf88aff,
			0xff98ff, 0xfea1ff, 0xfeabff, 0xfeabff,

			0x35088a, 0x420aad, 0x500cd0, 0x6428d0,  // Purple Blue
            0x7945d0, 0x8d4bd4, 0xa251d9, 0xb058ec,
			0xbe60ff, 0xc56bff, 0xcc77ff, 0xd183ff,
			0xd790ff, 0xdb9dff, 0xdfaaff, 0xdfaaff,

			0x051e81, 0x0626a5, 0x082fca, 0x263dd4,  // Blue1
            0x444cde, 0x4f5aee, 0x5a68ff, 0x6575ff,
			0x7183ff, 0x8091ff, 0x90a0ff, 0x97a9ff,
			0x9fb2ff, 0xafbeff, 0xc0cbff, 0xc0cbff,

			0x0c048b, 0x2218a0, 0x382db5, 0x483ec7,  // Blue2
            0x584fda, 0x6159ec, 0x6b64ff, 0x7a74ff,
			0x8a84ff, 0x918eff, 0x9998ff, 0xa5a3ff,
			0xb1aeff, 0xb8b8ff, 0xc0c2ff, 0xc0c2ff,

			0x1d295a, 0x1d3876, 0x1d4892, 0x1c5cac,  // Light Blue
            0x1c71c6, 0x3286cf, 0x489bd9, 0x4ea8ec,
			0x55b6ff, 0x70c7ff, 0x8cd8ff, 0x93dbff,
			0x9bdfff, 0xafe4ff, 0xc3e9ff, 0xc3e9ff,

			0x2f4302, 0x395202, 0x446103, 0x417a12,  // Turquoise
            0x3e9421, 0x4a9f2e, 0x57ab3b, 0x5cbd55,
			0x61d070, 0x69e27a, 0x72f584, 0x7cfa8d,
			0x87ff97, 0x9affa6, 0xadffb6, 0xadffb6,

			0x0a4108, 0x0d540a, 0x10680d, 0x137d0f,  // Green Blue
            0x169212, 0x19a514, 0x1cb917, 0x1ec919,
			0x21d91b, 0x47e42d, 0x6ef040, 0x78f74d,
			0x83ff5b, 0x9aff7a, 0xb2ff9a, 0xb2ff9a,

			0x04410b, 0x05530e, 0x066611, 0x077714,  // Green
            0x088817, 0x099b1a, 0x0baf1d, 0x48c41f,
			0x86d922, 0x8fe924, 0x99f927, 0xa8fc41,
			0xb7ff5b, 0xc9ff6e, 0xdcff81, 0xdcff81,

			0x02350f, 0x073f15, 0x0c4a1c, 0x2d5f1e,  // Yellow Green
            0x4f7420, 0x598324, 0x649228, 0x82a12e,
			0xa1b034, 0xa9c13a, 0xb2d241, 0xc4d945,
			0xd6e149, 0xe4f04e, 0xf2ff53, 0xf2ff53,

			0x263001, 0x243803, 0x234005, 0x51541b,  // Orange Green
            0x806931, 0x978135, 0xaf993a, 0xc2a73e,
			0xd5b543, 0xdbc03d, 0xe1cb38, 0xe2d836,
			0xe3e534, 0xeff258, 0xfbff7d, 0xfbff7d,

			0x401a02, 0x581f05, 0x702408, 0x8d3a13,  // Light Orange
            0xab511f, 0xb56427, 0xbf7730, 0xd0853a,
			0xe19344, 0xeda04e, 0xf9ad58, 0xfcb75c,
			0xffc160, 0xffc671, 0xffcb83, 0xffcb83
		};

		public static readonly int[] PALPalette =
		{
			0x000000, 0x1c1c1c, 0x393939, 0x595959,  // Grey
            0x797979, 0x929292, 0xababab, 0xbcbcbc,
			0xcdcdcd, 0xd9d9d9, 0xe6e6e6, 0xececec,
			0xf2f2f2, 0xf8f8f8, 0xffffff, 0xffffff,

			0x263001, 0x243803, 0x234005, 0x51541b,  // Orange Green
            0x806931, 0x978135, 0xaf993a, 0xc2a73e,
			0xd5b543, 0xdbc03d, 0xe1cb38, 0xe2d836,
			0xe3e534, 0xeff258, 0xfbff7d, 0xfbff7d,

			0x263001, 0x243803, 0x234005, 0x51541b,  // Orange Green
            0x806931, 0x978135, 0xaf993a, 0xc2a73e,
			0xd5b543, 0xdbc03d, 0xe1cb38, 0xe2d836,
			0xe3e534, 0xeff258, 0xfbff7d, 0xfbff7d,

			0x401a02, 0x581f05, 0x702408, 0x8d3a13,  // Light Orange
            0xab511f, 0xb56427, 0xbf7730, 0xd0853a,
			0xe19344, 0xeda04e, 0xf9ad58, 0xfcb75c,
			0xffc160, 0xffc671, 0xffcb83, 0xffcb83,

			0x391701, 0x5e2304, 0x833008, 0xa54716,  // Gold
            0xc85f24, 0xe37820, 0xff911d, 0xffab1d,
			0xffc51d, 0xffce34, 0xffd84c, 0xffe651,
			0xfff456, 0xfff977, 0xffff98, 0xffff98,

			0x451904, 0x721e11, 0x9f241e, 0xb33a20,  // Orange
            0xc85122, 0xe36920, 0xff811e, 0xff8c25,
			0xff982c, 0xffae38, 0xffc545, 0xffc559,
			0xffc66d, 0xffd587, 0xffe4a1, 0xffe4a1,

			0x4a1704, 0x7e1a0d, 0xb21d17, 0xc82119,  // Red Orange
            0xdf251c, 0xec3b38, 0xfa5255, 0xfc6161,
			0xff706e, 0xff7f7e, 0xff8f8f, 0xff9d9e,
			0xffabad, 0xffb9bd, 0xffc7ce, 0xffc7ce,

			0x050568, 0x3b136d, 0x712272, 0x8b2a8c,  // Pink
            0xa532a6, 0xb938ba, 0xcd3ecf, 0xdb47dd,
			0xea51eb, 0xf45ff5, 0xfe6dff, 0xfe7afd,
			0xff87fb, 0xff95fd, 0xffa4ff, 0xffa4ff,

			0x280479, 0x400984, 0x590f90, 0x70249d,  // Purple
            0x8839aa, 0xa441c3, 0xc04adc, 0xd054ed,
			0xe05eff, 0xe96dff, 0xf27cff, 0xf88aff,
			0xff98ff, 0xfea1ff, 0xfeabff, 0xfeabff,

			0x051e81, 0x0626a5, 0x082fca, 0x263dd4,  // Blue1
            0x444cde, 0x4f5aee, 0x5a68ff, 0x6575ff,
			0x7183ff, 0x8091ff, 0x90a0ff, 0x97a9ff,
			0x9fb2ff, 0xafbeff, 0xc0cbff, 0xc0cbff,

			0x0c048b, 0x2218a0, 0x382db5, 0x483ec7,  // Blue2
            0x584fda, 0x6159ec, 0x6b64ff, 0x7a74ff,
			0x8a84ff, 0x918eff, 0x9998ff, 0xa5a3ff,
			0xb1aeff, 0xb8b8ff, 0xc0c2ff, 0xc0c2ff,

			0x1d295a, 0x1d3876, 0x1d4892, 0x1c5cac,  // Light Blue
            0x1c71c6, 0x3286cf, 0x489bd9, 0x4ea8ec,
			0x55b6ff, 0x70c7ff, 0x8cd8ff, 0x93dbff,
			0x9bdfff, 0xafe4ff, 0xc3e9ff, 0xc3e9ff,

			0x2f4302, 0x395202, 0x446103, 0x417a12,  // Turquoise
            0x3e9421, 0x4a9f2e, 0x57ab3b, 0x5cbd55,
			0x61d070, 0x69e27a, 0x72f584, 0x7cfa8d,
			0x87ff97, 0x9affa6, 0xadffb6, 0xadffb6,

			0x0a4108, 0x0d540a, 0x10680d, 0x137d0f,  // Green Blue
            0x169212, 0x19a514, 0x1cb917, 0x1ec919,
			0x21d91b, 0x47e42d, 0x6ef040, 0x78f74d,
			0x83ff5b, 0x9aff7a, 0xb2ff9a, 0xb2ff9a,

			0x04410b, 0x05530e, 0x066611, 0x077714,  // Green
            0x088817, 0x099b1a, 0x0baf1d, 0x48c41f,
			0x86d922, 0x8fe924, 0x99f927, 0xa8fc41,
			0xb7ff5b, 0xc9ff6e, 0xdcff81, 0xdcff81,

			0x02350f, 0x073f15, 0x0c4a1c, 0x2d5f1e,  // Yellow Green
            0x4f7420, 0x598324, 0x649228, 0x82a12e,
			0xa1b034, 0xa9c13a, 0xb2d241, 0xc4d945,
			0xd6e149, 0xe4f04e, 0xf2ff53, 0xf2ff53
		};
	}
}
