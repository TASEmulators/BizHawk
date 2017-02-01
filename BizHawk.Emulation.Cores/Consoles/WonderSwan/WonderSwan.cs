using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	[CoreAttributes("Cygne/Mednafen", "Dox", true, true, "0.9.36.5", "http://mednafen.sourceforge.net/")]
	[ServiceNotApplicable(typeof(IDriveLight), typeof(IRegionable))]
	public partial class WonderSwan : IEmulator, IVideoProvider, ISoundProvider,
		IInputPollable, IDebuggable
	{
		[CoreConstructor("WSWAN")]
		public WonderSwan(CoreComm comm, byte[] file, bool deterministic, object Settings, object SyncSettings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
			_Settings = (Settings)Settings ?? new Settings();
			_SyncSettings = (SyncSettings)SyncSettings ?? new SyncSettings();
			
			DeterministicEmulation = deterministic; // when true, remember to force the RTC flag!
			Core = BizSwan.bizswan_new();
			if (Core == IntPtr.Zero)
				throw new InvalidOperationException("bizswan_new() returned NULL!");
			try
			{
				var ss = _SyncSettings.GetNativeSettings();
				if (deterministic)
					ss.userealtime = false;

				bool rotate = false;

				if (!BizSwan.bizswan_load(Core, file, file.Length, ref ss, ref rotate))
					throw new InvalidOperationException("bizswan_load() returned FALSE!");

				CoreComm.VsyncNum = 3072000; // master CPU clock, also pixel clock
				CoreComm.VsyncDen = (144 + 15) * (224 + 32); // 144 vislines, 15 vblank lines; 224 vispixels, 32 hblank pixels

				InitISaveRam();

				InitVideo(rotate);
				PutSettings(_Settings);
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

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public void Dispose()
		{
			if (Core != IntPtr.Zero)
			{
				BizSwan.bizswan_delete(Core);
				Core = IntPtr.Zero;
			}
		}

		public void FrameAdvance(bool render, bool rendersound = true)
		{
			Frame++;
			IsLagFrame = true;

			if (Controller.IsPressed("Power"))
				BizSwan.bizswan_reset(Core);

			bool rotate = false;
			int soundbuffsize = sbuff.Length;
			IsLagFrame = BizSwan.bizswan_advance(Core, GetButtons(), !render, vbuff, sbuff, ref soundbuffsize, ref rotate);
			if (soundbuffsize == sbuff.Length)
				throw new Exception();
			sbuffcontains = soundbuffsize;
			InitVideo(rotate);

			if (IsLagFrame)
				LagCount++;
		}

		public CoreComm CoreComm { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		IntPtr Core;

		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }

		public string SystemId { get { return "WSWAN"; } }
		public bool DeterministicEmulation { get; private set; }
		public string BoardName { get { return null; } }

		#region Debugging

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();
		public IInputCallbackSystem InputCallbacks { get { return _inputCallbacks; } }

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem();
		public IMemoryCallbackSystem MemoryCallbacks { get { return _memorycallbacks; } }

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

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public int TotalExecutedCycles {  get { throw new NotImplementedException(); } }

		BizSwan.MemoryCallback ReadCallbackD;
		BizSwan.MemoryCallback WriteCallbackD;
		BizSwan.MemoryCallback ExecCallbackD;
		BizSwan.ButtonCallback ButtonCallbackD;

		void ReadCallback(uint addr)
		{
			MemoryCallbacks.CallReads(addr);
		}
		void WriteCallback(uint addr)
		{
			MemoryCallbacks.CallWrites(addr);
		}
		void ExecCallback(uint addr)
		{
			MemoryCallbacks.CallExecutes(addr);
		}
		void ButtonCallback()
		{
			InputCallbacks.Call();
		}

		void InitDebugCallbacks()
		{
			ReadCallbackD = new BizSwan.MemoryCallback(ReadCallback);
			WriteCallbackD = new BizSwan.MemoryCallback(WriteCallback);
			ExecCallbackD = new BizSwan.MemoryCallback(ExecCallback);
			ButtonCallbackD = new BizSwan.ButtonCallback(ButtonCallback);
			_inputCallbacks.ActiveChanged += SetInputCallback;
			_memorycallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		void SetInputCallback()
		{
			BizSwan.bizswan_setbuttoncallback(Core, InputCallbacks.Any() ? ButtonCallbackD : null);
		}

		void SetMemoryCallbacks()
		{
			BizSwan.bizswan_setmemorycallbacks(Core,
				MemoryCallbacks.HasReads ? ReadCallbackD : null,
				MemoryCallbacks.HasWrites ? WriteCallbackD : null,
				MemoryCallbacks.HasExecutes ? ExecCallbackD : null);
		}

		#endregion

		#region IVideoProvider

		void InitVideo(bool rotate)
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

		private int[] vbuff = new int[224 * 144];

		public int[] GetVideoBuffer()
		{
			return vbuff;
		}

		public int VirtualWidth { get { return BufferWidth; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		#endregion
	}
}
