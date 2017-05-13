using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class C64 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ControllerDefinition ControllerDefinition => C64ControllerDefinition;

		public void FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_board.Controller = controller;
			do
			{
				DoCycle();
			}
			while (_frameCycles != 0);
		}

		public int Frame => _frame;

		public string SystemId { get { return "C64"; } }

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLagFrame = false;
			_frameCycles = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (_board != null)
			{
				if (_board.TapeDrive != null)
				{
					_board.TapeDrive.RemoveMedia();
				}
				if (_board.DiskDrive != null)
				{
					_board.DiskDrive.RemoveMedia();
				}
				_board = null;
			}
		}
	}
}
