using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : IEmulator
	{
		private readonly BasicServiceProvider _serviceProvider;

		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => _3DSController;

		public int Frame { get; set; }

		public string SystemId => VSystemID.Raw.N3DS;

		public bool DeterministicEmulation { get; }

		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			_controller = controller;
			IsLagFrame = true;

			if (_controller.IsPressed("Reset"))
			{
				_core.PPSSPP_Reset(_context);
				// memory domain pointers are no longer valid, reset them
				WireMemoryDomains();
			}

			IsLagFrame = _core.PPSSPP_RunFrame(_context);

			OnVideoRefresh();

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

		private void OnVideoRefresh()
		{
			_core.PPSSPP_GetVideoBufferDimensions(_context, out _ppssppVideoProvider.BW, out _ppssppVideoProvider.BH);
			_ppssppVideoProvider.VideoDirty = true;

			_core.PPSSPP_GetTouchScreenLayout(_context, out var x, out var y, out var width, out var height, out var rotated, out var enabled);
			TouchScreenRectangle = new(x, y, width, height);
			TouchScreenRotated = rotated;
			TouchScreenEnabled = enabled;
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

			_core.PPSSPP_DestroyContext(_context);

			CurrentCore = null;
			_disposed = true;
		}
	}
}
