using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	[PortedCore(CoreNames.Cygne, "Dox, Mednafen Team", "1.24.3", "https://mednafen.github.io/releases/")]
	[ServiceNotApplicable(typeof(IRegionable))]
	public partial class WonderSwan : IEmulator, IVideoProvider, ISoundProvider,
		IInputPollable, IDebuggable
	{
		[CoreConstructor(VSystemID.Raw.WSWAN)]
		public WonderSwan(byte[] file, bool deterministic, WonderSwan.Settings settings, WonderSwan.SyncSettings syncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			_settings = settings ?? new Settings();
			_syncSettings = syncSettings ?? new SyncSettings();
			
			DeterministicEmulation = deterministic; // when true, remember to force the RTC flag!
			Core = BizSwan.bizswan_new();
			if (Core == IntPtr.Zero)
				throw new InvalidOperationException($"{nameof(BizSwan.bizswan_new)}() returned NULL!");
			try
			{
				var ss = _syncSettings.GetNativeSettings();
				if (deterministic)
					ss.userealtime = false;

				bool rotate = false;

				if (!BizSwan.bizswan_load(Core, file, file.Length, ref ss, ref rotate))
					throw new InvalidOperationException($"{nameof(BizSwan.bizswan_load)}() returned FALSE!");

				InitISaveRam();

				InitVideo(rotate);
				PutSettings(_settings);
				InitIMemoryDomains();

				InitIStatable();
				InitDebugCallbacks();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		public IEmulatorServiceProvider ServiceProvider { get; }

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				BizSwan.bizswan_delete(Core);
				Core = IntPtr.Zero;
			}
		}

		public bool FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			IsLagFrame = true;

			if (controller.IsPressed("Power"))
				BizSwan.bizswan_reset(Core);

			bool rotate = false;
			int soundbuffsize = sbuff.Length;
			IsLagFrame = BizSwan.bizswan_advance(Core, GetButtons(controller), !render, vbuff, sbuff, ref soundbuffsize, ref rotate);
			if (soundbuffsize == sbuff.Length)
				throw new Exception();
			sbuffcontains = soundbuffsize;
			InitVideo(rotate);

			if (IsLagFrame)
				LagCount++;

			Frame++;

			return true;
		}

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		private IntPtr Core;

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		public string SystemId => VSystemID.Raw.WSWAN;
		public bool DeterministicEmulation { get; }

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { "System Bus" }); // This isn't an actual memory domain in this core (yet), but there's nothing that enforces that it has to be
		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var ret = new Dictionary<string, RegisterValue>();
			for (int i = (int)BizSwan.NecRegsMin; i <= (int)BizSwan.NecRegsMax; i++)
			{
				BizSwan.NecRegs en = (BizSwan.NecRegs)i;
				uint val = BizSwan.bizswan_getnecreg(Core, en);
				ret[Enum.GetName(typeof(BizSwan.NecRegs), en)] = (ushort)val;
			}
			return ret;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		private BizSwan.MemoryCallback ReadCallbackD;
		private BizSwan.MemoryCallback WriteCallbackD;
		private BizSwan.MemoryCallback ExecCallbackD;
		private BizSwan.ButtonCallback ButtonCallbackD;

		private void ReadCallback(uint addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessRead;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void WriteCallback(uint addr)
		{
			if (MemoryCallbacks.HasWrites)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessWrite;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void ExecCallback(uint addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				uint flags = (uint)MemoryCallbackFlags.AccessExecute;
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void ButtonCallback()
		{
			InputCallbacks.Call();
		}

		private void InitDebugCallbacks()
		{
			ReadCallbackD = new BizSwan.MemoryCallback(ReadCallback);
			WriteCallbackD = new BizSwan.MemoryCallback(WriteCallback);
			ExecCallbackD = new BizSwan.MemoryCallback(ExecCallback);
			ButtonCallbackD = new BizSwan.ButtonCallback(ButtonCallback);
			_inputCallbacks.ActiveChanged += SetInputCallback;
			_memorycallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		private void SetInputCallback()
		{
			BizSwan.bizswan_setbuttoncallback(Core, InputCallbacks.Any() ? ButtonCallbackD : null);
		}

		private void SetMemoryCallbacks()
		{
			BizSwan.bizswan_setmemorycallbacks(Core,
				MemoryCallbacks.HasReads ? ReadCallbackD : null,
				MemoryCallbacks.HasWrites ? WriteCallbackD : null,
				MemoryCallbacks.HasExecutes ? ExecCallbackD : null);
		}

		private void InitVideo(bool rotate)
		{
			if (rotate)
			{
				BufferWidth = 144;
				BufferHeight = 224;
			}
			else
			{
				BufferWidth = 224;
				BufferHeight = 144;
			}
		}

		private readonly int[] vbuff = new int[224 * 144];

		public int[] GetVideoBuffer() => vbuff;

		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferHeight;
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator => 3072000; // master CPU clock, also pixel clock
		public int VsyncDenominator => (144 + 15) * (224 + 32); // 144 vislines, 15 vblank lines; 224 vispixels, 32 hblank pixels
	}
}
