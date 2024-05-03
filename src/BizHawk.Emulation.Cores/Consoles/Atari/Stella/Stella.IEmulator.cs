using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			Core.stella_frame_advance(render);

			return true;
		}

		public int _frame;

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.A26;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_islag = false;
		}

		public void Dispose()
		{
		}
	}
}
