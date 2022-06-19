using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	[PortedCore(CoreNames.SubBsnes115, "")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public class SubBsnesCore
		: IEmulator, IDebuggable, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable,
			ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>, ICycleTiming
	{
		[CoreConstructor(VSystemID.Raw.SNES)]
		public SubBsnesCore(CoreLoadParameters<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings> loadParameters)
		{
			_bsnesCore = new BsnesCore(loadParameters, true);
			BasicServiceProvider ser = new(this);
			ser.Register(_bsnesCore.ServiceProvider.GetService<IDebuggable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IVideoProvider>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<ISaveRam>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IStatable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IInputPollable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IRegionable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<ICycleTiming>());
			ServiceProvider = ser;
		}

		private readonly BsnesCore _bsnesCore;
		
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _bsnesCore.ControllerDefinition;
		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			return _bsnesCore.FrameAdvance(controller, render, renderSound);
		}

		public int Frame => _bsnesCore.Frame;

		public string SystemId => _bsnesCore.SystemId;

		public bool DeterministicEmulation => _bsnesCore.DeterministicEmulation;

		public void ResetCounters()
		{
			_bsnesCore.ResetCounters();
		}

		public void Dispose()
		{
			_bsnesCore.Dispose();
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return _bsnesCore.GetCpuFlagsAndRegisters();
		}

		public void SetCpuRegister(string register, int value)
		{
			_bsnesCore.SetCpuRegister(register, value);
		}

		public IMemoryCallbackSystem MemoryCallbacks => _bsnesCore.MemoryCallbacks;

		public bool CanStep(StepType type)
		{
			return _bsnesCore.CanStep(type);
		}

		public void Step(StepType type)
		{
			_bsnesCore.Step(type);
		}

		public long TotalExecutedCycles => _bsnesCore.TotalExecutedCycles;

		public int[] GetVideoBuffer()
		{
			return _bsnesCore.GetVideoBuffer();
		}

		public int VirtualWidth => _bsnesCore.VirtualWidth;

		public int VirtualHeight => _bsnesCore.VirtualHeight;

		public int BufferWidth => _bsnesCore.BufferWidth;

		public int BufferHeight => _bsnesCore.BufferHeight;

		public int VsyncNumerator => _bsnesCore.VsyncNumerator;

		public int VsyncDenominator => _bsnesCore.VsyncDenominator;

		public int BackgroundColor => _bsnesCore.BackgroundColor;

		public byte[] CloneSaveRam()
		{
			return _bsnesCore.CloneSaveRam();
		}

		public void StoreSaveRam(byte[] data)
		{
			_bsnesCore.StoreSaveRam(data);
		}

		public bool SaveRamModified => _bsnesCore.SaveRamModified;

		public void SaveStateBinary(BinaryWriter writer)
		{
			_bsnesCore.SaveStateBinary(writer);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			_bsnesCore.LoadStateBinary(reader);
		}

		public int LagCount
		{
			get => _bsnesCore.LagCount;
			set => _bsnesCore.LagCount = value;
		}

		public bool IsLagFrame
		{
			get => _bsnesCore.IsLagFrame;
			set => _bsnesCore.IsLagFrame = value;
		}

		public IInputCallbackSystem InputCallbacks => _bsnesCore.InputCallbacks;

		public DisplayType Region => _bsnesCore.Region;

		public BsnesCore.SnesSettings GetSettings()
		{
			return _bsnesCore.GetSettings();
		}

		public BsnesCore.SnesSyncSettings GetSyncSettings()
		{
			return _bsnesCore.GetSyncSettings();
		}

		public PutSettingsDirtyBits PutSettings(BsnesCore.SnesSettings o)
		{
			return _bsnesCore.PutSettings(o);
		}

		public PutSettingsDirtyBits PutSyncSettings(BsnesCore.SnesSyncSettings o)
		{
			return _bsnesCore.PutSyncSettings(o);
		}

		public long CycleCount => _bsnesCore.CycleCount;

		public double ClockRate => _bsnesCore.ClockRate;
	}
}
