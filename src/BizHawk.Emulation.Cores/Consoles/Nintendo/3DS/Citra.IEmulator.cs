using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo._3DS
{
	public partial class Citra : IEmulator
	{
		private readonly BasicServiceProvider _serviceProvider;

		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => _3DSController;

		public int Frame { get; set; }

		public string SystemId => VSystemID.Raw._3DS;

		public bool DeterministicEmulation { get; }

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			_controller = controller;
			IsLagFrame = true;

			_motionEmu.Update(
				controller.IsPressed("Tilt"),
				controller.AxisValue("Tilt X"),
				controller.AxisValue("Tilt Y"));

			if (_controller.IsPressed("Power"))
			{
				_core.Citra_Reset(_context);
			}

			_core.Citra_RunFrame(_context);

			_core.Citra_GetVideoDimensions(_context, out _citraVideoProvider.Width, out _citraVideoProvider.Height);
			_citraVideoProvider.VideoDirty = true;

			if (renderSound)
			{
				ProcessSound();
			}

			Frame++;
			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		private bool _disposed;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_core.Citra_DestroyContext(_context);

			foreach (var glContext in _glContexts)
			{
				_openGLProvider.ReleaseGLContext(glContext);
			}

			_glContexts.Clear();

			CurrentCore = null;
			_disposed = true;
		}
	}
}
