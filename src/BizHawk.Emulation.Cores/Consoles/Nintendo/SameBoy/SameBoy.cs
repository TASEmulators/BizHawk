using System.IO;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Properties;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C libsameboy
	/// </summary>
	[PortedCore(CoreNames.Sameboy, "LIJI32", "0.16.3", "https://github.com/LIJI32/SameBoy")]
	public partial class Sameboy : ICycleTiming, IInputPollable, ILinkable, IRomInfo, IBoardInfo, IGameboyCommon
	{
		private static readonly LibSameboy LibSameboy;

		static Sameboy()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libsameboy.so" : "libsameboy.dll", hasLimitedLifetime: false);
			LibSameboy = BizInvoker.GetInvoker<LibSameboy>(resolver, CallingConventionAdapters.Native);
		}

		private readonly BasicServiceProvider _serviceProvider;

		private readonly Gameboy.GBDisassembler _disassembler;

		private readonly CoreComm Comm;

		private IntPtr SameboyState { get; set; } = IntPtr.Zero;

		public bool IsCgb { get; set; }

		public bool IsCGBMode
			=> IsCgb; //TODO inline

		public bool IsCGBDMGMode
			=> LibSameboy.sameboy_iscgbdmg(SameboyState);

		private readonly LibSameboy.InputCallback _inputcb;

		private readonly LibSameboy.RumbleCallback _rumblecb;

		public Sameboy(CoreComm comm, GameInfo game, byte[] gbs, SameboySettings settings, SameboySyncSettings syncSettings)
			: this(comm, game, null, settings, syncSettings, false)
		{
			var gbsInfo = new LibSameboy.GBSInfo
			{
				TrackCount = 0,
				FirstTrack = 0,
				Title = new byte[33],
				Author = new byte[33],
				Copyright = new byte[33],
			};

			if (!LibSameboy.sameboy_loadgbs(SameboyState, gbs, gbs.Length, ref gbsInfo))
			{
				throw new InvalidOperationException("Core rejected the GBS!");
			}

			RomDetails = $"Track Count: {gbsInfo.TrackCount}\r\n" +
				$"First Track: {gbsInfo.FirstTrack}\r\n" +
				$"Title: {Encoding.UTF8.GetString(gbsInfo.Title).Trim()}\r\n" +
				$"Author: {Encoding.UTF8.GetString(gbsInfo.Author).Trim()}\r\n" +
				$"Copyright: {Encoding.UTF8.GetString(gbsInfo.Copyright).Trim()}";

			_firstTrack = gbsInfo.FirstTrack;
			_lastTrack = gbsInfo.FirstTrack + gbsInfo.TrackCount - 1;

			_curTrack = _firstTrack;
			LibSameboy.sameboy_switchgbstrack(SameboyState, _curTrack);

			BoardName = "GBS";
			ControllerDefinition = new ControllerDefinition("GBS Controller")
			{
				BoolButtons = { "Previous Track", "Next Track" }
			}.MakeImmutable();

			Comm = comm;

			_stateBuf = new byte[LibSameboy.sameboy_statelen(SameboyState)];
		}

		[CoreConstructor(VSystemID.Raw.GB)]
		[CoreConstructor(VSystemID.Raw.GBC)]
		public Sameboy(CoreLoadParameters<SameboySettings, SameboySyncSettings> lp)
			: this(lp.Comm, lp.Game, lp.Roms[0].FileData, lp.Settings, lp.SyncSettings, lp.DeterministicEmulationRequested)
		{
			var file = lp.Roms[0].FileData;

			RomDetails = $"{lp.Game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(file)}\r\n{MD5Checksum.ComputePrefixedHex(file)}\r\n";

			BoardName = MapperName(file);
			_hasAcc = BoardName is "MBC7 ROM+ACCEL+EEPROM";
			ControllerDefinition = Gameboy.Gameboy.CreateControllerDefinition(sgb: false, sub: false, tilt: _hasAcc, rumble: true, remote: false);

			_stateBuf = new byte[LibSameboy.sameboy_statelen(SameboyState)];
		}

		private Sameboy(
			CoreComm comm,
			GameInfo game,
			byte[] file,
			SameboySettings settings,
			SameboySyncSettings syncSettings,
			bool deterministic)
		{
			_serviceProvider = new BasicServiceProvider(this);

			_settings = settings ?? new();
			_syncSettings = syncSettings ?? new();

			var model = _syncSettings.ConsoleMode;
			if (model is SameboySyncSettings.GBModel.Auto)
			{
				model = game.System == VSystemID.Raw.GBC
					? SameboySyncSettings.GBModel.GB_MODEL_CGB_E
					: SameboySyncSettings.GBModel.GB_MODEL_DMG_B;
			}

			IsCgb = model >= SameboySyncSettings.GBModel.GB_MODEL_CGB_0;

			byte[] bios;
			if (_syncSettings.EnableBIOS)
			{
				FirmwareID fwid = new(
					IsCgb ? "GBC" : "GB",
					_syncSettings.ConsoleMode >= SameboySyncSettings.GBModel.GB_MODEL_AGB
					? "AGB"
					: "World");
				bios = comm.CoreFileProvider.GetFirmwareOrThrow(fwid, "BIOS Not Found, Cannot Load.  Change SyncSettings to run without BIOS.");
			}
			else
			{
				bios = Zstd.DecompressZstdStream(new MemoryStream(IsCgb
					? _syncSettings.ConsoleMode >= SameboySyncSettings.GBModel.GB_MODEL_AGB ? Resources.SameboyAgbBoot.Value : Resources.SameboyCgbBoot.Value
					: Resources.SameboyDmgBoot.Value)).ToArray();
			}

			DeterministicEmulation = false;

			bool realtime = true;
			if (!_syncSettings.UseRealTime || deterministic)
			{
				realtime = false;
				DeterministicEmulation = true;
			}

			SameboyState = LibSameboy.sameboy_create(file, file?.Length ?? 0, bios, bios.Length, model, realtime, _syncSettings.NoJoypadBounce);

			InitMemoryDomains();
			InitMemoryCallbacks();

			_inputcb = InputCallback;
			_rumblecb = RumbleCallback;
			_tracecb = MakeTrace;

			LibSameboy.sameboy_setinputcallback(SameboyState, _inputcb);
			LibSameboy.sameboy_setrumblecallback(SameboyState, _rumblecb);

			LibSameboy.sameboy_settracecallback(SameboyState, null);
			LibSameboy.sameboy_setscanlinecallback(SameboyState, null, 0);
			LibSameboy.sameboy_setprintercallback(SameboyState, null);

			const string TRACE_HEADER = "SM83: PC, opcode, registers (A, F, B, C, D, E, H, L, SP, LY, CY)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register(Tracer);

			_disassembler = new Gameboy.GBDisassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			PutSettings(_settings);

			LibSameboy.sameboy_setrtcdivisoroffset(SameboyState, _syncSettings.RTCDivisorOffset);
			CycleCount = 0;
		}

		public double ClockRate => 2097152;

		public long CycleCount
		{
			get => LibSameboy.sameboy_getcyclecount(SameboyState);
			private set => LibSameboy.sameboy_setcyclecount(SameboyState, value);
		}

		public int LagCount { get; set; } = 0;

		public bool IsLagFrame { get; set; } = false;

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private readonly InputCallbackSystem _inputCallbacks = new();

		private void InputCallback()
		{
			IsLagFrame = false;
			_inputCallbacks.Call();
		}

		private void RumbleCallback(int amplitude)
		{
			_controller.SetHapticChannelStrength("Rumble", amplitude);
		}

		public bool LinkConnected
		{
			get => _printercb != null;
			set {}
		}

		public string RomDetails { get; }

		private static string MapperName(byte[] romdata)
		{
			return (romdata[0x147]) switch
			{
				0x00 => "Plain ROM",
				0x01 => "MBC1 ROM",
				0x02 => "MBC1 ROM+RAM",
				0x03 => "MBC1 ROM+RAM+BATTERY",
				0x05 => "MBC2 ROM",
				0x06 => "MBC2 ROM+BATTERY",
				0x08 => "Plain ROM+RAM",
				0x09 => "Plain ROM+RAM+BATTERY",
				0x0F => "MBC3 ROM+TIMER+BATTERY",
				0x10 => "MBC3 ROM+TIMER+RAM+BATTERY",
				0x11 => "MBC3 ROM",
				0x12 => "MBC3 ROM+RAM",
				0x13 => "MBC3 ROM+RAM+BATTERY",
				0x19 => "MBC5 ROM",
				0x1A => "MBC5 ROM+RAM",
				0x1B => "MBC5 ROM+RAM+BATTERY",
				0x1C => "MBC5 ROM+RUMBLE",
				0x1D => "MBC5 ROM+RUMBLE+RAM",
				0x1E => "MBC5 ROM+RUMBLE+RAM+BATTERY",
				0x22 => "MBC7 ROM+ACCEL+EEPROM",
				0xFC => "Pocket Camera ROM+RAM+BATTERY",
				0xFE => "HuC3 ROM+RAM+BATTERY",
				0xFF => "HuC1 ROM+RAM+BATTERY",
				_ => "UNKNOWN",
			};
		}

		public string BoardName { get; }

		public IGPUMemoryAreas LockGPU()
		{
			var _vram = IntPtr.Zero;
			var _bgpal = IntPtr.Zero;
			var _sppal = IntPtr.Zero;
			var _oam = IntPtr.Zero;
			long unused = 0;
			if (!LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.VRAM, ref _vram, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.BGPRGB, ref _bgpal, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.OBPRGB, ref _sppal, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.OAM, ref _oam, ref unused))
			{
				throw new InvalidOperationException("Unexpected error in sameboy_getmemoryarea");
			}

			return new GPUMemoryAreas()
			{
				Vram = _vram,
				Oam = _oam,
				Sppal = _sppal,
				Bgpal = _bgpal
			};
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			public IntPtr Vram { get; init; }

			public IntPtr Oam { get; init; }

			public IntPtr Sppal { get; init; }

			public IntPtr Bgpal { get; init; }

			public void Dispose() {}
		}

		private ScanlineCallback _scanlinecb;
		private int _scanlinecbline;

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			_scanlinecb = callback;
			_scanlinecbline = line;

			LibSameboy.sameboy_setscanlinecallback(SameboyState, _scanlinecbline >= 0 ? callback : null, line);

			if (_scanlinecbline == -2)
			{
				_scanlinecb(LibSameboy.sameboy_cpuread(SameboyState, 0xFF40));
			}
		}

		private PrinterCallback _printercb;

		public void SetPrinterCallback(PrinterCallback callback)
		{
			_printercb = callback;
			LibSameboy.sameboy_setprintercallback(SameboyState, _printercb);
		}
	}
}
