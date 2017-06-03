using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition
		{
			get
			{
				if (IsGameGear)
				{
					return GGController;
				}

				return SmsController;
			}
		}

		public void FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;
			_lagged = true;
			_frame++;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);
			Cpu.Debug = Tracer.Enabled;
			if (!IsGameGear)
			{
				PSG.StereoPanning = Settings.ForceStereoSeparation ? ForceStereoByte : (byte)0xFF;
			}

			if (Cpu.Debug && Cpu.Logger == null) // TODO, lets not do this on each frame. But lets refactor CoreComm/CoreComm first
			{
				Cpu.Logger = s => Tracer.Put(s);
			}

			if (IsGameGear == false)
			{
				Cpu.NonMaskableInterrupt = controller.IsPressed("Pause");
			}

			if (IsGame3D && Settings.Fix3D)
			{
				Vdp.ExecFrame((Frame & 1) == 0);
			}
			else
			{
				Vdp.ExecFrame(render);
			}

			PSG.EndFrame(Cpu.TotalExecutedCycles);
			if (_lagged)
			{
				_lagCount++;
				_isLag = true;
			}
			else
			{
				_isLag = false;
			}
		}

	    public int Frame => _frame;

		public string SystemId => "SMS";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
		}
	}
}
