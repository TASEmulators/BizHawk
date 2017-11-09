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

			// NOTE: Need to research differences between reset and power cycle
			if (_controller.IsPressed("Power"))
			{
				HardReset();
			}

			if (_controller.IsPressed("Reset"))
			{
				SoftReset();
			}

			_frame++;
			_isLag = true;
			PSG.BeginFrame(_cpu.TotalExecutedCycles);

			if (_tracer.Enabled)
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}
			byte tempRet1 = ControllerDeck.ReadPort1(controller, true, true);
			byte tempRet2 = ControllerDeck.ReadPort2(controller, true, true);

			bool intPending = (!tempRet1.Bit(4)) | (!tempRet2.Bit(4));

			_vdp.ExecuteFrame(intPending);

			PSG.EndFrame(_cpu.TotalExecutedCycles);

			if (_isLag)
			{
				_lagCount++;
			}
		}

		public int Frame => _frame;

		public string SystemId => "Coleco";

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
