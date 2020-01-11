using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class C64 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => C64ControllerDefinition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_board.Controller = controller;

			if (_tracer.Enabled)
			{
				_board.Cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_board.Cpu.TraceCallback = null;
			}

			if (controller.IsPressed("Power"))
			{
				if (!_powerPressed)
				{
					_powerPressed = true;
					HardReset();
				}
			}
			else
			{
				_powerPressed = false;
			}

			if (controller.IsPressed("Reset"))
			{
				if (!_resetPressed)
				{
					_resetPressed = true;
					SoftReset();
				}
			}
			else
			{
				_resetPressed = false;
			}

			if (controller.IsPressed("Next Disk") && !_nextPressed)
			{
				_nextPressed = true;
				IncrementDisk();
			}
			else if (controller.IsPressed("Previous Disk") && !_prevPressed)
			{
				_prevPressed = true;
				DecrementDisk();
			}

			if (!controller.IsPressed("Next Disk"))
			{
				_nextPressed = false;
			}

			if (!controller.IsPressed("Previous Disk"))
			{
				_prevPressed = false;
			}

			do
			{
				DoCycle();
			}
			while (_frameCycles != 0);

			return true;
		}

		public int Frame => _frame;

		public string SystemId => "C64";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLagFrame = false;
			_frameCycles = 0;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			if (_board != null)
			{
				_board.TapeDrive?.RemoveMedia();
				_board.DiskDrive?.RemoveMedia();
				_board = null;
			}
		}
	}
}
