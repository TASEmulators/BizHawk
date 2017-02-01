using System;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.ARM;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	[CoreAttributes("VBA-Next", "many authors", true, true, "cd508312a29ed8c29dacac1b11c2dce56c338a54", "https://github.com/libretro/vba-next")]
	[ServiceNotApplicable(typeof(IDriveLight), typeof(IRegionable))]
	public partial class VBANext : IEmulator, IVideoProvider, ISoundProvider, IInputPollable,
		IGBAGPUViewable, ISaveRam, IStatable, IDebuggable, ISettable<object, VBANext.SyncSettings>
	{
		IntPtr Core;

		[CoreConstructor("GBA")]
		public VBANext(byte[] file, CoreComm comm, GameInfo game, bool deterministic, object syncsettings)
		{
			var ser = new BasicServiceProvider(this);
			ser.Register<IDisassemblable>(new ArmV4Disassembler());
			ServiceProvider = ser;

			CoreComm = comm;

			byte[] biosfile = CoreComm.CoreFileProvider.GetFirmware("GBA", "Bios", true, "GBA bios file is mandatory.");
			if (file.Length > 32 * 1024 * 1024)
				throw new ArgumentException("ROM is too big to be a GBA ROM!");
			if (biosfile.Length != 16 * 1024)
				throw new ArgumentException("BIOS file is not exactly 16K!");

			LibVBANext.FrontEndSettings FES = new LibVBANext.FrontEndSettings();
			FES.saveType = (LibVBANext.FrontEndSettings.SaveType)game.GetInt("saveType", 0);
			FES.flashSize = (LibVBANext.FrontEndSettings.FlashSize)game.GetInt("flashSize", 0x10000);
			FES.enableRtc = game.GetInt("rtcEnabled", 0) != 0;
			FES.mirroringEnable = game.GetInt("mirroringEnabled", 0) != 0;

			Console.WriteLine("GameDB loaded settings: saveType={0}, flashSize={1}, rtcEnabled={2}, mirroringEnabled={3}",
				FES.saveType, FES.flashSize, FES.enableRtc, FES.mirroringEnable);

			_syncSettings = (SyncSettings)syncsettings ?? new SyncSettings();
			DeterministicEmulation = deterministic;

			FES.skipBios = _syncSettings.SkipBios;
			FES.RTCUseRealTime = _syncSettings.RTCUseRealTime;
			FES.RTCwday = (int)_syncSettings.RTCInitialDay;
			FES.RTCyear = _syncSettings.RTCInitialTime.Year % 100;
			FES.RTCmonth = _syncSettings.RTCInitialTime.Month - 1;
			FES.RTCmday = _syncSettings.RTCInitialTime.Day;
			FES.RTChour = _syncSettings.RTCInitialTime.Hour;
			FES.RTCmin = _syncSettings.RTCInitialTime.Minute;
			FES.RTCsec = _syncSettings.RTCInitialTime.Second;
			if (DeterministicEmulation)
			{
				// FES.skipBios = false; // this is OK; it is deterministic and probably accurate
				FES.RTCUseRealTime = false;
			}

			Core = LibVBANext.Create();
			if (Core == IntPtr.Zero)
				throw new InvalidOperationException("Create() returned nullptr!");
			try
			{
				if (!LibVBANext.LoadRom(Core, file, (uint)file.Length, biosfile, (uint)biosfile.Length, FES))
					throw new InvalidOperationException("LoadRom() returned false!");

				Tracer = new TraceBuffer()
				{
					Header = "ARM7: PC, machine code, mnemonic, operands, registers (r0-r16)"
				};
				ser.Register<ITraceable>(Tracer);

				CoreComm.VsyncNum = 262144;
				CoreComm.VsyncDen = 4389;
				CoreComm.NominalWidth = 240;
				CoreComm.NominalHeight = 160;

				GameCode = Encoding.ASCII.GetString(file, 0xac, 4);
				Console.WriteLine("Game code \"{0}\"", GameCode);

				savebuff = new byte[LibVBANext.BinStateSize(Core)];
				savebuff2 = new byte[savebuff.Length + 13];
				InitMemoryDomains();
				InitRegisters();
				InitCallbacks();

				// todo: hook me up as a setting
				SetupColors();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;

			if (Controller.IsPressed("Power"))
				LibVBANext.Reset(Core);

			SyncTraceCallback();

			IsLagFrame = LibVBANext.FrameAdvance(Core, GetButtons(Controller), videobuff, soundbuff, out numsamp, videopalette);

			if (IsLagFrame)
				LagCount++;
		}

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		private ITraceable Tracer { get; set; }

		public string SystemId { get { return "GBA"; } }

		public bool DeterministicEmulation { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public string BoardName { get { return null; } }
		/// <summary>
		/// set in the ROM internal header
		/// </summary>
		public string GameCode { get; private set; }

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				LibVBANext.Destroy(Core);
				Core = IntPtr.Zero;
			}
		}

		#region Debugging

		LibVBANext.StandardCallback padcb;
		LibVBANext.AddressCallback fetchcb;
		LibVBANext.AddressCallback readcb;
		LibVBANext.AddressCallback writecb;
		LibVBANext.TraceCallback tracecb;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		TraceInfo Trace(uint addr, uint opcode)
		{
			return new TraceInfo
			{
				Disassembly = string.Format("{2:X8}:  {0:X8}  {1}", opcode, Darm.DisassembleStuff(addr, opcode), addr).PadRight(54),
				RegisterInfo = regs.TraceString()
			};
		}

		void InitCallbacks()
		{
			padcb = new LibVBANext.StandardCallback(() => InputCallbacks.Call());
			fetchcb = new LibVBANext.AddressCallback((addr) => MemoryCallbacks.CallExecutes(addr));
			readcb = new LibVBANext.AddressCallback((addr) => MemoryCallbacks.CallReads(addr));
			writecb = new LibVBANext.AddressCallback((addr) => MemoryCallbacks.CallWrites(addr));
			tracecb = new LibVBANext.TraceCallback((addr, opcode) => Tracer.Put(Trace(addr, opcode)));
			_inputCallbacks.ActiveChanged += SyncPadCallback;
			_memorycallbacks.ActiveChanged += SyncMemoryCallbacks;
		}

		void SyncPadCallback()
		{
			LibVBANext.SetPadCallback(Core, InputCallbacks.Any() ? padcb : null);
		}

		void SyncMemoryCallbacks()
		{
			LibVBANext.SetFetchCallback(Core, MemoryCallbacks.HasExecutes ? fetchcb : null);
			LibVBANext.SetReadCallback(Core, MemoryCallbacks.HasReads ? readcb : null);
			LibVBANext.SetWriteCallback(Core, MemoryCallbacks.HasWrites ? writecb : null);
		}

		void SyncTraceCallback()
		{
			LibVBANext.SetTraceCallback(Core, Tracer.Enabled ? tracecb : null);
		}

		VBARegisterHelper regs;

		void InitRegisters()
		{
			regs = new VBARegisterHelper(Core);
		}

		#endregion

		#region Controller

		public ControllerDefinition ControllerDefinition { get { return GBA.GBAController; } }
		public IController Controller { get; set; }

		public static LibVBANext.Buttons GetButtons(IController c)
		{
			LibVBANext.Buttons ret = 0;
			foreach (string s in Enum.GetNames(typeof(LibVBANext.Buttons)))
			{
				if (c.IsPressed(s))
				{
					ret |= (LibVBANext.Buttons)Enum.Parse(typeof(LibVBANext.Buttons), s);
				}
			}
			return ret;
		}

		#endregion
	}
}
