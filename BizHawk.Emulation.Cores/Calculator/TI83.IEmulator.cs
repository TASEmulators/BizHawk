using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => TI83Controller;

		public void FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;
			_lagged = true;

			if (_tracer.Enabled)
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}

			// I eyeballed this speed
			for (int i = 0; i < 5; i++)
			{
				_onPressed = controller.IsPressed("ON");

				// and this was derived from other emus
				for (int j = 0; j < 10000; j++)
				{
					_cpu.ExecuteOne();
				}
				
				_cpu.FlagI = true;
			}

			Frame++;

			if (_lagged)
			{
				_lagCount++;
			}

			_isLag = _lagged;
		}

		public int Frame
		{
			get { return _frame; }
			private set { _frame = value; }
		}

		public string SystemId => "TI83";

	    public bool DeterministicEmulation => true;

	    public void ResetCounters()
		{
			Frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
		}
	}
}
