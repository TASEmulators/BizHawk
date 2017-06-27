using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public void FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;

			StartFrameCond();
			while (_tia.LineCount < _tia.NominalNumScanlines)
			{
				Cycle();
			}

			if (rendersound == false)
			{
				_tia.AudioClocks = 0; // we need this here since the async sound provider won't check in this case
			}

			FinishFrameCond();
		}

		public int Frame => _frame;

		public string SystemId => "A26";

		public bool DeterministicEmulation => true;

		public CoreComm CoreComm { get; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public void Dispose()
		{
		}
	}
}
