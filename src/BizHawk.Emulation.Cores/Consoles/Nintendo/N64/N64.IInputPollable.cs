using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : IInputPollable
	{
		public int Frame { get; private set; }
		public int LagCount { get; set; }

		public bool IsLagFrame
		{
			get
			{
				if (_settings.UseMupenStyleLag)
				{
					return !FrameFinished;
				}

				return !_inputProvider.LastFrameInputPolled;
			}

			set
			{
				if (_settings.UseMupenStyleLag)
				{
					FrameFinished = !value;
				}
				else
				{
					_inputProvider.LastFrameInputPolled = !value;
				}
			}
		}

		public bool FrameFinished
		{
			get => _videoProvider.FrameFinished;
			internal set => _videoProvider.FrameFinished = value;
		}

		// TODO: optimize managed to unmanaged using the ActiveChanged event
		public IInputCallbackSystem InputCallbacks { get; }
	}
}
