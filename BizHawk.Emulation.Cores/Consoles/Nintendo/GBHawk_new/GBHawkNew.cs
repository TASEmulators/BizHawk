using System;
using System.Text;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkNew
{
	[Core(
		"GBHawkNew",
		"",
		isPorted: false,
		isReleased: true)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class GBHawkNew : IEmulator, ISaveRam, IDebuggable, IInputPollable, IRegionable, IGameboyCommon,
	ISettable<GBHawkNew.GBSettings, GBHawkNew.GBSyncSettings>
	{
		public IntPtr GB_Pntr { get; set; } = IntPtr.Zero;
		byte[] GB_core = new byte[0x80000];

		private int _frame = 0;
		public int _lagCount = 0;
		public bool is_GBC = false;

		public byte[] _bios;
		public readonly byte[] _rom;		

		[CoreConstructor(new[] { "GB", "GBC" })]
		public GBHawkNew(CoreComm comm, GameInfo game, byte[] rom, /*string gameDbFn,*/ object settings, object syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			_settings = (GBSettings)settings ?? new GBSettings();
			_syncSettings = (GBSyncSettings)syncSettings ?? new GBSyncSettings();
			_controllerDeck = new GBHawkNewControllerDeck(_syncSettings.Port1);

			byte[] Bios = null;

			// Load up a BIOS and initialize the correct PPU
			if (_syncSettings.ConsoleMode == GBSyncSettings.ConsoleModeType.Auto)
			{
				if (game.System == "GB")
				{
					Bios = comm.CoreFileProvider.GetFirmware("GB", "World", true, "BIOS Not Found, Cannot Load");
				}
				else
				{
					Bios = comm.CoreFileProvider.GetFirmware("GBC", "World", true, "BIOS Not Found, Cannot Load");
					is_GBC = true;
				}
				
			}
			else if (_syncSettings.ConsoleMode == GBSyncSettings.ConsoleModeType.GB)
			{
				Bios = comm.CoreFileProvider.GetFirmware("GB", "World", true, "BIOS Not Found, Cannot Load");
			}
			else
			{
				Bios = comm.CoreFileProvider.GetFirmware("GBC", "World", true, "BIOS Not Found, Cannot Load");
				is_GBC = true;
			}			

			if (Bios == null)
			{
				throw new MissingFirmwareException("Missing Gamboy Bios");
			}

			_bios = Bios;
			_rom = rom;

			GB_Pntr = LibGBHawk.GB_create();

			char[] MD5_temp = rom.HashMD5(0, rom.Length).ToCharArray();

			LibGBHawk.GB_load_bios(GB_Pntr, _bios, is_GBC, _syncSettings.GBACGB);
			LibGBHawk.GB_load(GB_Pntr, rom, (uint)rom.Length, MD5_temp, (uint)_syncSettings.RTCInitialTime, (uint)_syncSettings.RTCOffset);

			blip_L.SetRates(4194304, 44100);
			blip_R.SetRates(4194304, 44100);

			(ServiceProvider as BasicServiceProvider).Register<ISoundProvider>(this);

			SetupMemoryDomains();

			Header_Length = LibGBHawk.GB_getheaderlength(GB_Pntr);
			Disasm_Length = LibGBHawk.GB_getdisasmlength(GB_Pntr);
			Reg_String_Length = LibGBHawk.GB_getregstringlength(GB_Pntr);

			var newHeader = new StringBuilder(Header_Length);
			LibGBHawk.GB_getheader(GB_Pntr, newHeader, Header_Length);

			Console.WriteLine(Header_Length + " " + Disasm_Length + " " + Reg_String_Length);

			Tracer = new TraceBuffer { Header = newHeader.ToString() };

			var serviceProvider = ServiceProvider as BasicServiceProvider;
			serviceProvider.Register<ITraceable>(Tracer);
			serviceProvider.Register<IStatable>(new StateSerializer(SyncState));

			Console.WriteLine("MD5: " + rom.HashMD5(0, rom.Length));
			Console.WriteLine("SHA1: " + rom.HashSHA1(0, rom.Length));

			HardReset();

			iptr0 = LibGBHawk.GB_get_ppu_pntrs(GB_Pntr, 0);
			iptr1 = LibGBHawk.GB_get_ppu_pntrs(GB_Pntr, 1);
			iptr2 = LibGBHawk.GB_get_ppu_pntrs(GB_Pntr, 2);
			iptr3 = LibGBHawk.GB_get_ppu_pntrs(GB_Pntr, 3);

			_scanlineCallback = null;
		}

		#region GPUViewer

		public bool IsCGBMode() => is_GBC;

		public IntPtr iptr0 = IntPtr.Zero;
		public IntPtr iptr1 = IntPtr.Zero;
		public IntPtr iptr2 = IntPtr.Zero;
		public IntPtr iptr3 = IntPtr.Zero;

		private GPUMemoryAreas _gpuMemory
		{
			get
			{				
				return new GPUMemoryAreas(iptr0, iptr1, iptr2, iptr3);
			}
		}

		public LibGBHawk.ScanlineCallback _scanlineCallback;

		public GPUMemoryAreas GetGPU() => _gpuMemory;

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			if ((callback == null) || (line == -2))
			{
				_scanlineCallback = null;
				LibGBHawk.GB_setscanlinecallback(GB_Pntr, null, 0);
			}
			else
			{
				_scanlineCallback = delegate
				{
					callback(LibGBHawk.GB_get_LCDC(GB_Pntr));
				};
				LibGBHawk.GB_setscanlinecallback(GB_Pntr, _scanlineCallback, line);
			}

			if (line == -2)
			{
				callback(LibGBHawk.GB_get_LCDC(GB_Pntr));
			}
		}

		private PrinterCallback _printerCallback = null;

		public void SetPrinterCallback(PrinterCallback callback)
		{
			_printerCallback = null;
		}

		#endregion

		public DisplayType Region => DisplayType.NTSC;

		private readonly GBHawkNewControllerDeck _controllerDeck;

		public void HardReset()
		{
			LibGBHawk.GB_Reset(GB_Pntr);

			frame_buffer = new int[VirtualWidth * VirtualHeight];
		}

		private void ExecFetch(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}

		#region Trace Logger
		public ITraceable Tracer;

		public LibGBHawk.TraceCallback tracecb;

		// these will be constant values assigned during core construction
		public int Header_Length;
		public int Disasm_Length;
		public int Reg_String_Length;

		public void MakeTrace(int t)
		{
			StringBuilder new_d = new StringBuilder(Disasm_Length);
			StringBuilder new_r = new StringBuilder(Reg_String_Length);

			LibGBHawk.GB_getdisassembly(GB_Pntr, new_d, t, Disasm_Length);
			LibGBHawk.GB_getregisterstate(GB_Pntr, new_r, t, Reg_String_Length);

			Tracer.Put(new TraceInfo
			{
				Disassembly = new_d.ToString().PadRight(36),
				RegisterInfo = new_r.ToString()
			});
		}

		#endregion
	}
}
