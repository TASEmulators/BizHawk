using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public partial class Emu83 : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => TI83Controller;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;

			LibEmu83.TI83_SetTraceCallback(Context, Tracer.IsEnabled() ? _traceCallback : null);

			IsLagFrame = LibEmu83.TI83_Advance(Context, _controller.IsPressed("ON"), _controller.IsPressed("SEND"), render ? _videoBuffer : null, _settings.BGColor, _settings.ForeColor);

			Frame++;

			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		public int Frame { get; set; }

		public string SystemId => VSystemID.Raw.TI83;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (Context != IntPtr.Zero)
			{
				LibEmu83.TI83_DestroyContext(Context);
				Context = IntPtr.Zero;
			}
		}
	}
}
