using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ControllerDefinition ControllerDefinition
		{
			get { return TI83Controller; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			_lagged = true;

			Cpu.Debug = Tracer.Enabled;

			if (Cpu.Debug && Cpu.Logger == null) // TODO, lets not do this on each frame. But lets refactor CoreComm/CoreComm first
				Cpu.Logger = (s) => Tracer.Put(s);

			//I eyeballed this speed
			for (int i = 0; i < 5; i++)
			{
				_onPressed = Controller.IsPressed("ON");

				//and this was derived from other emus
				Cpu.ExecuteCycles(10000);
				Cpu.Interrupt = true;
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
			set { _frame = value; }
		}

		public string SystemId { get { return "TI83"; } }

		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose() { }
	}
}
