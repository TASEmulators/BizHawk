using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => ControllerDeck.Definition;

		public void FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;
			Cpu.Debug = Tracer.Enabled;
			frame++;
			_isLag = true;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);

			if (Cpu.Debug && Cpu.Logger == null) // TODO, lets not do this on each frame. But lets refactor CoreComm/CoreComm first
			{
				Cpu.Logger = (s) => Tracer.Put(s);
			}

			byte tempRet1 = ControllerDeck.ReadPort1(controller, true, true);
			byte tempRet2 = ControllerDeck.ReadPort2(controller, true, true);

			bool intPending = (!tempRet1.Bit(4)) | (!tempRet2.Bit(4));

			VDP.ExecuteFrame(intPending);

			PSG.EndFrame(Cpu.TotalExecutedCycles);

			if (_isLag)
			{
				_lagCount++;
			}
		}

		public int Frame => frame;

		public string SystemId => "Coleco";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
		}
	}
}
