using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Properties;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C libsameboy
	/// </summary>
	[PortedCore(CoreNames.Sameboy, "LIJI32", "0.14.7", "https://github.com/LIJI32/SameBoy", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class Sameboy : ICycleTiming, IInputPollable, IGameboyCommon
	{
		private readonly BasicServiceProvider _serviceProvider;

		private readonly Gameboy.GBDisassembler _disassembler;

		private IntPtr SameboyState { get; set; } = IntPtr.Zero;

		public bool IsCgb { get; set; }

		public bool IsCGBMode() => IsCgb;

		private readonly LibSameboy.SampleCallback _samplecb;
		private readonly LibSameboy.InputCallback _inputcb;

		[CoreConstructor(VSystemID.Raw.GB)]
		[CoreConstructor(VSystemID.Raw.GBC)]
		public Sameboy(CoreComm comm, GameInfo game, byte[] file, SameboySyncSettings syncSettings, bool deterministic)
		{
			_serviceProvider = new BasicServiceProvider(this);

			_syncSettings = syncSettings ?? new SameboySyncSettings();

			LibSameboy.LoadFlags flags = _syncSettings.ConsoleMode switch
			{
				SameboySyncSettings.ConsoleModeType.GB => LibSameboy.LoadFlags.IS_DMG,
				SameboySyncSettings.ConsoleModeType.GBC => LibSameboy.LoadFlags.IS_CGB,
				SameboySyncSettings.ConsoleModeType.GBA => LibSameboy.LoadFlags.IS_CGB | LibSameboy.LoadFlags.IS_AGB,
				_ => game.System == VSystemID.Raw.GBC ? LibSameboy.LoadFlags.IS_CGB : LibSameboy.LoadFlags.IS_DMG
			};

			IsCgb = (flags & LibSameboy.LoadFlags.IS_CGB) == LibSameboy.LoadFlags.IS_CGB;

			byte[] bios = null;
			if (_syncSettings.EnableBIOS)
			{
				FirmwareID fwid = new(
					IsCgb ? "GBC" : "GB",
					_syncSettings.ConsoleMode is SameboySyncSettings.ConsoleModeType.GBA
					? "AGB"
					: "World");
				bios = comm.CoreFileProvider.GetFirmwareOrThrow(fwid, "BIOS Not Found, Cannot Load.  Change SyncSettings to run without BIOS.");
			}
			else
			{
				bios = Util.DecompressGzipFile(new MemoryStream(IsCgb
					? _syncSettings.ConsoleMode is SameboySyncSettings.ConsoleModeType.GBA ? Resources.SameboyAgbBoot.Value : Resources.SameboyCgbBoot.Value
					: Resources.SameboyDmgBoot.Value));
			}

			SameboyState = LibSameboy.sameboy_create(file, file.Length, bios, bios.Length, flags);

			InitMemoryDomains();
			InitMemoryCallbacks();

			_samplecb = QueueSample;
			LibSameboy.sameboy_setsamplecallback(SameboyState, _samplecb);
			_inputcb = InputCallback;
			LibSameboy.sameboy_setinputcallback(SameboyState, _inputcb);
			_tracecb = MakeTrace;
			LibSameboy.sameboy_settracecallback(SameboyState, null);

			const string TRACE_HEADER = "SM83: PC, opcode, registers (A, F, B, C, D, E, H, L, SP, LY, CY)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register<ITraceable>(Tracer);

			_disassembler = new Gameboy.GBDisassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			_stateBuf = new byte[LibSameboy.sameboy_statelen(SameboyState)];
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

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		private void InputCallback()
		{
			IsLagFrame = false;
			_inputCallbacks.Call();
		}

		// getmemoryarea will return the raw palette buffer, but we want the rgb32 palette, so convert it
		private unsafe uint[] SynthesizeFrontendPal(IntPtr _pal)
		{
			var buf = new uint[8];
			var pal = (short*)_pal;
			for (int i = 0; i < 8; i++)
			{
				byte r = (byte)(pal[i]       & 0x1F);
				byte g = (byte)(pal[i] >> 5  & 0x1F);
				byte b = (byte)(pal[i] >> 10 & 0x1F);
				buf[i] = (uint)((0xFF << 24) | (r << 19) | (g << 11) | (b << 3));
			}
			return buf;
		}

		public IGPUMemoryAreas LockGPU()
		{
			var _vram = IntPtr.Zero;
			var _bgpal = IntPtr.Zero;
			var _sppal = IntPtr.Zero;
			var _oam = IntPtr.Zero;
			int unused = 0;
			if (!LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.VRAM, ref _vram, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.BGP, ref _bgpal, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.OBP, ref _sppal, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.OAM, ref _oam, ref unused))
			{
				throw new InvalidOperationException("Unexpected error in sameboy_getmemoryarea");
			}
			return new GPUMemoryAreas(_vram, _oam, SynthesizeFrontendPal(_sppal), SynthesizeFrontendPal(_bgpal));
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			public IntPtr Vram { get; }

			public IntPtr Oam { get; }

			public IntPtr Sppal { get; }

			public IntPtr Bgpal { get; }

			private readonly List<GCHandle> _handles = new List<GCHandle>();

			public GPUMemoryAreas(IntPtr vram, IntPtr oam, uint[] sppal, uint[] bgpal)
			{
				Vram = vram;
				Oam = oam;
				Sppal = AddHandle(sppal);
				Bgpal = AddHandle(bgpal);
			}

			private IntPtr AddHandle(object target)
			{
				var handle = GCHandle.Alloc(target, GCHandleType.Pinned);
				_handles.Add(handle);
				return handle.AddrOfPinnedObject();
			}

			public void Dispose()
			{
				foreach (var h in _handles)
					h.Free();
				_handles.Clear();
			}
		}

		private ScanlineCallback _scanlinecb;
		private int _scanlineline = 0;

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			_scanlinecb = callback;
			_scanlineline = line;

			if (_scanlineline == -2)
			{
				_scanlinecb(LibSameboy.sameboy_cpuread(SameboyState, 0xFF40));
			}
		}

		PrinterCallback _printercb = null;

		public void SetPrinterCallback(PrinterCallback callback)
		{
			_printercb = null;
		}
	}
}
