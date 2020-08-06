using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.LR35902;

using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using System.Runtime.InteropServices;

// TODO: mode1_disableint_gbc.gbc behaves differently between GBC and GBA, why?

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	[Core(
		CoreNames.GbHawk,
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class GBHawk : IEmulator, ISaveRam, IDebuggable, IInputPollable, IRegionable, IGameboyCommon,
	ISettable<GBHawk.GBSettings, GBHawk.GBSyncSettings>
	{
		// this register controls whether or not the GB BIOS is mapped into memory
		public byte GB_bios_register;

		public byte input_register;

		// The unused bits in this register are still read/writable
		public byte REG_FFFF;
		// The unused bits in this register (interrupt flags) are always set
		public byte REG_FF0F = 0xE0;
		// Updating reg FF0F seemsto be delayed by one cycle
		// tests 
		public byte REG_FF0F_OLD = 0xE0;

		// memory domains
		public byte[] RAM = new byte[0x8000]; // only 0x2000 available to GB
		public byte[] ZP_RAM = new byte[0x80];
		/* 
		 * VRAM is arranged as: 
		 * 0x1800 Tiles
		 * 0x400 BG Map 1
		 * 0x400 BG Map 2
		 * 0x1800 Tiles
		 * 0x400 CA Map 1
		 * 0x400 CA Map 2
		 * Only the top set is available in GB (i.e. VRAM_Bank = 0)
		 */
		public byte[] VRAM = new byte[0x4000];
		public byte[] OAM = new byte[0xA0];

		// vblank memory domains
		public byte[] RAM_vbls = new byte[0x8000];
		public byte[] ZP_RAM_vbls = new byte[0x80];
		public byte[] VRAM_vbls = new byte[0x4000];
		public byte[] OAM_vbls = new byte[0xA0];

		public int RAM_Bank;
		public byte VRAM_Bank;
		internal bool is_GBC;
		public bool GBC_compat; // compatibility mode for GB games played on GBC
		public bool double_speed;
		public bool speed_switch;
		public bool HDMA_transfer; // stalls CPU when in progress
		public byte IR_reg, IR_mask, IR_signal, IR_receive, IR_self;
		public int IR_write;

		// several undocumented GBC Registers
		public byte undoc_6C, undoc_72, undoc_73, undoc_74, undoc_75, undoc_76, undoc_77;

		public byte[] _bios;
		public readonly byte[] _rom;		
		public readonly byte[] header = new byte[0x50];

		public byte[] cart_RAM;
		public byte[] cart_RAM_vbls;
		public bool has_bat;

		private int _frame = 0;

		public bool Use_MT;
		public ushort addr_access;

		public MapperBase mapper;

		private readonly ITraceable _tracer;

		public LR35902 cpu;
		public PPU ppu;
		public Timer timer;
		public Audio audio;
		public SerialPort serialport;

		private static byte[] GBA_override = { 0xFF, 0x00, 0xCD, 0x03, 0x35, 0xAA, 0x31, 0x90, 0x94, 0x00, 0x00, 0x00, 0x00 };

		[CoreConstructor("GB")]
		[CoreConstructor("GBC")]
		public GBHawk(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ GBSettings settings, GBSyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);

			cpu = new LR35902
			{
				ReadMemory = ReadMemory,
				WriteMemory = WriteMemory,
				PeekMemory = PeekMemory,
				DummyReadMemory = ReadMemory,
				OnExecFetch = ExecFetch,
				SpeedFunc = SpeedFunc,
				GetIntRegs = GetIntRegs,
				SetIntRegs = SetIntRegs
			};
			
			timer = new Timer();
			audio = new Audio();
			serialport = new SerialPort();

			_settings = (GBSettings)settings ?? new GBSettings();
			_syncSettings = (GBSyncSettings)syncSettings ?? new GBSyncSettings();
			_controllerDeck = new GBHawkControllerDeck(_syncSettings.Port1);

			byte[] Bios = null;

			// Load up a BIOS and initialize the correct PPU
			if (_syncSettings.ConsoleMode == GBSyncSettings.ConsoleModeType.Auto)
			{
				if (game.System == "GB")
				{
					Bios = comm.CoreFileProvider.GetFirmware("GB", "World", true, "BIOS Not Found, Cannot Load");
					ppu = new GB_PPU();
				}
				else
				{
					Bios = comm.CoreFileProvider.GetFirmware("GBC", "World", true, "BIOS Not Found, Cannot Load");
					ppu = new GBC_PPU();
					is_GBC = true;
				}
				
			}
			else if (_syncSettings.ConsoleMode == GBSyncSettings.ConsoleModeType.GB)
			{
				Bios = comm.CoreFileProvider.GetFirmware("GB", "World", true, "BIOS Not Found, Cannot Load");
				ppu = new GB_PPU();
			}
			else
			{
				Bios = comm.CoreFileProvider.GetFirmware("GBC", "World", true, "BIOS Not Found, Cannot Load");
				ppu = new GBC_PPU();
				is_GBC = true;
			}			

			if (Bios == null)
			{
				throw new MissingFirmwareException("Missing Gamboy Bios");
			}

			_bios = Bios;

			// set up IR register to off state
			if (is_GBC) { IR_mask = 0; IR_reg = 0x3E; IR_receive = 2; IR_self = 2; IR_signal = 2; }

			// Here we modify the BIOS if GBA mode is set (credit to ExtraTricky)
			if (is_GBC && _syncSettings.GBACGB)
			{
				for (int i = 0; i < 13; i++)
				{
					_bios[i + 0xF3] = (byte)((GBA_override[i] + _bios[i + 0xF3]) & 0xFF);
				}
				IR_mask = 2;
			}

			// CPU needs to know about GBC status too
			cpu.is_GBC = is_GBC;

			Buffer.BlockCopy(rom, 0x100, header, 0, 0x50);

			if (is_GBC && ((header[0x43] != 0x80) && (header[0x43] != 0xC0)))
			{
				ppu = new GBC_GB_PPU();
			}

			Console.WriteLine("MD5: " + rom.HashMD5(0, rom.Length));
			Console.WriteLine("SHA1: " + rom.HashSHA1(0, rom.Length));
			_rom = rom;
			Setup_Mapper();
			if (cart_RAM != null) { cart_RAM_vbls = new byte[cart_RAM.Length]; }

			timer.Core = this;
			audio.Core = this;
			ppu.Core = this;
			serialport.Core = this;

			ser.Register<IVideoProvider>(this);
			ser.Register<ISoundProvider>(audio);
			ServiceProvider = ser;

			_settings = (GBSettings)settings ?? new GBSettings();
			_syncSettings = (GBSyncSettings)syncSettings ?? new GBSyncSettings();

			_tracer = new TraceBuffer { Header = cpu.TraceHeader };
			ser.Register<ITraceable>(_tracer);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			SetupMemoryDomains();
			cpu.SetCallbacks(ReadMemory, PeekMemory, PeekMemory, WriteMemory);
			HardReset();

			iptr0 = Marshal.AllocHGlobal(VRAM.Length + 1);
			iptr1 = Marshal.AllocHGlobal(OAM.Length + 1);
			iptr2 = Marshal.AllocHGlobal(ppu.color_palette.Length * 8 * 8 + 1);
			iptr3 = Marshal.AllocHGlobal(ppu.color_palette.Length * 8 * 8 + 1);

			_scanlineCallback = null;
		}

		public bool IsCGBMode() => is_GBC;

		public IntPtr iptr0 = IntPtr.Zero;
		public IntPtr iptr1 = IntPtr.Zero;
		public IntPtr iptr2 = IntPtr.Zero;
		public IntPtr iptr3 = IntPtr.Zero;

		private GPUMemoryAreas _gpuMemory
		{
			get
			{
				Marshal.Copy(VRAM, 0, iptr0, VRAM.Length);
				Marshal.Copy(OAM, 0, iptr1, OAM.Length);

				if (is_GBC)
				{
					int[] cp2 = new int[32];
					int[] cp = new int[32];
					for (int i = 0; i < 32; i++)
					{
						cp2[i] = (int)ppu.OBJ_palette[i];
						cp[i] = (int)ppu.BG_palette[i];
					}

					Marshal.Copy(cp2, 0, iptr2, ppu.OBJ_palette.Length);
					Marshal.Copy(cp, 0, iptr3, ppu.BG_palette.Length);
				}
				else
				{
					int[] cp2 = new int[8];
					for (int i = 0; i < 4; i++)
					{
						cp2[i] = (int)ppu.color_palette[(ppu.obj_pal_0 >> (i * 2)) & 3];
						cp2[i + 4] = (int)ppu.color_palette[(ppu.obj_pal_1 >> (i * 2)) & 3];
					}
					Marshal.Copy(cp2, 0, iptr2, cp2.Length);

					int[] cp = new int[4];
					for (int i = 0; i < 4; i++)
					{
						cp[i] = (int)ppu.color_palette[(ppu.BGP >> (i * 2)) & 3];
					}
					Marshal.Copy(cp, 0, iptr3, cp.Length);
				}

				return new GPUMemoryAreas(iptr0, iptr1, iptr2, iptr3);
			}
		} 

		public GPUMemoryAreas GetGPU() => _gpuMemory;

		public ScanlineCallback _scanlineCallback;
		public int _scanlineCallbackLine = 0;

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			_scanlineCallback = callback;
			_scanlineCallbackLine = line;

			if (line == -2)
			{
				GetGPU();
				_scanlineCallback(ppu.LCDC);
			}
		}

		private PrinterCallback _printerCallback = null;

		public void SetPrinterCallback(PrinterCallback callback)
		{
			_printerCallback = null;
		}

		public DisplayType Region => DisplayType.NTSC;

		private readonly GBHawkControllerDeck _controllerDeck;

		public void HardReset()
		{
			GB_bios_register = 0; // bios enable
			GBC_compat = is_GBC;
			in_vblank = true; // we start off in vblank since the LCD is off
			in_vblank_old = true;
			double_speed = false;
			VRAM_Bank = 0;
			RAM_Bank = 1; // RAM bank always starts as 1 (even writing zero still sets 1)
			delays_to_process = false;
			controller_delay_cd = 0;

			Register_Reset();
			timer.Reset();
			ppu.Reset();
			audio.Reset();
			serialport.Reset();
			mapper.Reset();
			cpu.Reset();
			
			vid_buffer = new uint[VirtualWidth * VirtualHeight];
			frame_buffer = new int[VirtualWidth * VirtualHeight];
		}

		// TODO: move callbacks to cpu to avoid having to make a non-inlinable 
		private void ExecFetch(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void Setup_Mapper()
		{
			// setup up mapper based on header entry
			string mppr;

			switch (header[0x47])
			{
				case 0x0: mapper = new MapperDefault();		mppr = "NROM";							break;
				case 0x1: mapper = new MapperMBC1();		mppr = "MBC1";							break;
				case 0x2: mapper = new MapperMBC1();		mppr = "MBC1";							break;
				case 0x3: mapper = new MapperMBC1();		mppr = "MBC1";		has_bat = true;		break;
				case 0x5: mapper = new MapperMBC2();		mppr = "MBC2";							break;
				case 0x6: mapper = new MapperMBC2();		mppr = "MBC2";		has_bat = true;		break;
				case 0x8: mapper = new MapperDefault();		mppr = "NROM";							break;
				case 0x9: mapper = new MapperDefault();		mppr = "NROM";		has_bat = true;		break;
				case 0xB: mapper = new MapperMMM01();		mppr = "MMM01";							break;
				case 0xC: mapper = new MapperMMM01();		mppr = "MMM01";							break;
				case 0xD: mapper = new MapperMMM01();		mppr = "MMM01";		has_bat = true;		break;
				case 0xF: mapper = new MapperMBC3();		mppr = "MBC3";		has_bat = true;		break;
				case 0x10: mapper = new MapperMBC3();		mppr = "MBC3";		has_bat = true;		break;
				case 0x11: mapper = new MapperMBC3();		mppr = "MBC3";							break;
				case 0x12: mapper = new MapperMBC3();		mppr = "MBC3";							break;
				case 0x13: mapper = new MapperMBC3();		mppr = "MBC3";		has_bat = true;		break;
				case 0x19: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1A: mapper = new MapperMBC5();		mppr = "MBC5";		has_bat = true;		break;
				case 0x1B: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1C: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1D: mapper = new MapperMBC5();		mppr = "MBC5";							break;
				case 0x1E: mapper = new MapperMBC5();		mppr = "MBC5";		has_bat = true;		break;
				case 0x20: mapper = new MapperMBC6();		mppr = "MBC6";							break;
				case 0x22: mapper = new MapperMBC7();		mppr = "MBC7";		has_bat = true;		break;
				case 0xFC: mapper = new MapperCamera();		mppr = "CAM";		has_bat = true;		break;
				case 0xFD: mapper = new MapperTAMA5();		mppr = "TAMA5";		has_bat = true;		break;
				case 0xFE: mapper = new MapperHuC3();		mppr = "HuC3";		has_bat = true;		break;
				case 0xFF: mapper = new MapperHuC1();		mppr = "HuC1";							break;

				// Bootleg mappers
				// NOTE: Sachen mapper selection does not account for scrambling, so if another bootleg mapper
				// identifies itself as 0x31, this will need to be modified
				case 0x31: mapper = new MapperSachen2();	mppr = "Schn2";							break;

				case 0x4:
				case 0x7:
				case 0xA:
				case 0xE:
				case 0x14:
				case 0x15:
				case 0x16:
				case 0x17:
				case 0x18:
				case 0x1F:
				case 0x21:
				default:
					// mapper not implemented
					Console.WriteLine(header[0x47]);
					throw new Exception("Mapper not implemented");
			}

			// special case for multi cart mappers
			if ((_rom.HashMD5(0, _rom.Length) == "97122B9B183AAB4079C8D36A4CE6E9C1") ||
				(_rom.HashMD5(0, _rom.Length) == "9FB9C42CF52DCFDCFBAD5E61AE1B5777") ||
				(_rom.HashMD5(0, _rom.Length) == "CF1F58AB72112716D3C615A553B2F481")				
				)
			{
				Console.WriteLine("Using Multi-Cart Mapper");
				mapper = new MapperMBC1Multi();
			}
			
			// Wisdom Tree does not identify their mapper, so use hash instead
			if ((_rom.HashMD5(0, _rom.Length) == "2C07CAEE51A1F0C91C72C7C6F380B0F6") || // Joshua
				(_rom.HashMD5(0, _rom.Length) == "37E017C8D1A45BAB609FB5B43FB64337") || // Spiritual Warfare
				(_rom.HashMD5(0, _rom.Length) == "AB1FA0ED0207B1D0D5F401F0CD17BEBF") || // Exodus
				(_rom.HashMD5(0, _rom.Length) == "BA2AC3587B3E1B36DE52E740274071B0") || // Bible - KJV
				(_rom.HashMD5(0, _rom.Length) == "8CDDB8B2DCD3EC1A3FDD770DF8BDA07C")    // Bible - NIV
				)
			{
				Console.WriteLine("Using Wisdom Tree Mapper");
				mapper = new MapperWT();
				mppr = "Wtree";
			}

			// special case for bootlegs
			if ((_rom.HashMD5(0, _rom.Length) == "CAE0998A899DF2EE6ABA8E7695C2A096"))
			{
				Console.WriteLine("Using RockMan 8 (Unlicensed) Mapper");
				mapper = new MapperRM8();
			}
			if ((_rom.HashMD5(0, _rom.Length) == "D3C1924D847BC5D125BF54C2076BE27A"))
			{
				Console.WriteLine("Using Sachen 1 (Unlicensed) Mapper");
				mapper = new MapperSachen1();
				mppr = "Schn1";
			}

			Console.Write("Mapper: ");
			Console.WriteLine(mppr);

			cart_RAM = null;

			switch (header[0x49])
			{
				case 1:
					cart_RAM = new byte[0x800];
					break;
				case 2:
					cart_RAM = new byte[0x2000];
					break;
				case 3:
					cart_RAM = new byte[0x8000];
					break;
				case 4:
					cart_RAM = new byte[0x20000];
					break;
				case 5:
					cart_RAM = new byte[0x10000];
					break;
				case 0:
					Console.WriteLine("Mapper Number indicates Battery Backed RAM but none present.");
					Console.WriteLine("Disabling Battery Setting.");
					has_bat = false;
					break;
			}

			// Sachen maper not known to have RAM
			if ((mppr == "Schn1") || (mppr == "Schn2"))
			{
				cart_RAM = null;
				Use_MT = true;
			}

			// mbc2 carts have built in RAM
			if (mppr == "MBC2")
			{
				cart_RAM = new byte[0x200];
			}

			// mbc7 has 256 bytes of RAM, regardless of any header info
			if (mppr == "MBC7")
			{
				cart_RAM = new byte[0x100];
				has_bat = true;
			}

			// TAMA5 has 0x1000 bytes of RAM, regardless of any header info
			if (mppr == "TAMA5")
			{
				cart_RAM = new byte[0x20];
				has_bat = true;
			}

			mapper.Core = this;

			if (cart_RAM != null && (mppr != "MBC7"))
			{
				Console.Write("RAM: "); Console.WriteLine(cart_RAM.Length);

				for (int i = 0; i < cart_RAM.Length; i++)
				{
					cart_RAM[i] = 0xFF;
				}
			}
			
			// Extra RTC initialization for mbc3, HuC3, and TAMA5
			if (mppr == "MBC3")
			{
				Use_MT = true;

				mapper.RTC_Get(_syncSettings.RTCOffset, 5);

				int days = (int)Math.Floor(_syncSettings.RTCInitialTime / 86400.0);

				int days_upper = ((days & 0x100) >> 8) | ((days & 0x200) >> 2);

				mapper.RTC_Get(days_upper, 4);
				mapper.RTC_Get(days & 0xFF, 3);

				int remaining = _syncSettings.RTCInitialTime - (days * 86400);

				int hours = (int)Math.Floor(remaining / 3600.0);

				mapper.RTC_Get(hours & 0xFF, 2);

				remaining = remaining - (hours * 3600);

				int minutes = (int)Math.Floor(remaining / 60.0);

				mapper.RTC_Get(minutes & 0xFF, 1);

				remaining = remaining - (minutes * 60);

				mapper.RTC_Get(remaining & 0xFF, 0);
			}

			if (mppr == "HuC3")
			{
				Use_MT = true;

				int years = (int)Math.Floor(_syncSettings.RTCInitialTime / 31536000.0);

				mapper.RTC_Get(years, 24);

				int remaining = _syncSettings.RTCInitialTime - (years * 31536000);

				int days = (int)Math.Floor(remaining / 86400.0);
				int days_upper = (days >> 8) & 0xF;

				mapper.RTC_Get(days_upper, 20);
				mapper.RTC_Get(days & 0xFF, 12);

				remaining = remaining - (days * 86400);

				int minutes = (int)Math.Floor(remaining / 60.0);
				int minutes_upper = (minutes >> 8) & 0xF;

				mapper.RTC_Get(minutes_upper, 8);
				mapper.RTC_Get(remaining & 0xFF, 0);
			}

			if (mppr == "TAMA5")
			{
				Use_MT = true;

				// currently no date / time input for TAMA5

			}
		}
	}
}
