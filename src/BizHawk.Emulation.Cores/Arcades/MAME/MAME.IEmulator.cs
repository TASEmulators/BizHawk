using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public partial class MAME : IEmulator
	{
		public string SystemId => VSystemID.Raw.Arcade;
		public bool DeterministicEmulation { get; }
		public int Frame { get; private set; }

		private BasicServiceProvider _serviceProvider;
		public IEmulatorServiceProvider ServiceProvider => _serviceProvider;

		public ControllerDefinition ControllerDefinition => MAMEController;

		/// <summary>
		/// MAME fires the periodic callback on every video and debugger update,
		/// which happens every VBlank and also repeatedly at certain time
		/// intervals while paused. In our implementation, MAME's emulation
		/// runs in a separate co-thread, which we swap over with mame_coswitch
		/// On a periodic callback, control will be switched back to the host
		/// co-thread. If MAME is internally unpaused, then the next periodic
		/// callback will occur once a frame is done, making mame_coswitch
		/// act like a frame advance.
		/// </summary>
		public bool FrameAdvance(IController controller, bool render, bool renderSound = true)
		{
			using (_exe.EnterExit())
			{
				SendInput(controller);
				IsLagFrame = _core.mame_coswitch();
				UpdateSound();
				if (render)
				{
					UpdateVideo();
				}
			}

			if (!renderSound)
			{
				DiscardSamples();
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

		private bool _disposed = false;

		public void Dispose()
		{
			if (_disposed) return;
			_exe.Dispose();
			_disposed = true;
		}
	}
}