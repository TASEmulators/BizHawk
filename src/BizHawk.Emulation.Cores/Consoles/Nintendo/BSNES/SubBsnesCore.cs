using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	[PortedCore(CoreNames.SubBsnes115, "")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public class SubBsnesCore : IEmulator, ICycleTiming
	{
		[CoreConstructor(VSystemID.Raw.SGB)]
		[CoreConstructor(VSystemID.Raw.SNES)]
		public SubBsnesCore(CoreLoadParameters<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings> loadParameters)
		{
			_bsnesCore = new BsnesCore(loadParameters, true);
			if (_bsnesCore.Region == DisplayType.NTSC)
				ClockRate = 315.0 / 88.0 * 1000000.0 * 6.0;
			else
				ClockRate = (283.75 * 15625.0 + 25.0) * 4.8;

			BasicServiceProvider ser = new(this);
			ser.Register(_bsnesCore.ServiceProvider.GetService<IDebuggable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IVideoProvider>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<ISaveRam>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IStatable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IInputPollable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IRegionable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<ISoundProvider>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IMemoryDomains>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<IDisassemblable>());
			ser.Register(_bsnesCore.ServiceProvider.GetService<ITraceable>());
			if (IsSGB)
			{
				// board info is only set in SGB mode
				ser.Register(_bsnesCore.ServiceProvider.GetService<IBoardInfo>());
			}
			ServiceProvider = ser;
		}

		private readonly BsnesCore _bsnesCore;

		public bool IsSGB => _bsnesCore.IsSGB;

		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _bsnesCore.ControllerDefinition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			_bsnesCore.FrameAdvancePre(controller, render, renderSound);

			_bsnesCore.IsLagFrame = true;
			bool framePassed = false;

			bool resetSignal = controller.IsPressed("Reset");
			bool powerSignal = controller.IsPressed("Power");

			if (resetSignal || powerSignal)
			{
				int resetInstruction = controller.AxisValue("Reset Instruction");
				for (int i = 0; i < resetInstruction; i++)
				{
					framePassed = _bsnesCore.Api.core.snes_cpu_step();
					if (framePassed) break;
				}

				if (resetSignal)
				{
					_bsnesCore.Api.core.snes_reset();
				}

				if (powerSignal)
				{
					_bsnesCore.Api.core.snes_power();
				}
			}
			else
			{
				// run the core for one (sub-)frame
				bool subFrameRequested = controller.IsPressed("Subframe");
				framePassed = _bsnesCore.Api.core.snes_run(subFrameRequested);
			}

			_bsnesCore.Frame++;

			if (framePassed && _bsnesCore.IsLagFrame)
				_bsnesCore.LagCount++;
			else
				_bsnesCore.IsLagFrame = false;

			return true;
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

		public long CycleCount => _bsnesCore.Api.core.snes_get_executed_cycles();

		public double ClockRate { get; }
	}
}
