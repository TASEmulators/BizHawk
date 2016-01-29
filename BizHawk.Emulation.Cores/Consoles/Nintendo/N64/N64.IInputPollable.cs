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
					return !IsVIFrame;
				}

				return !_inputProvider.LastFrameInputPolled;
			}

			set
			{
				if (_settings.UseMupenStyleLag)
				{
					IsVIFrame = !value;
				}
				else
				{
					_inputProvider.LastFrameInputPolled = !value;
				}
			}
		}

		public bool IsVIFrame
		{
			get
			{
				return _videoProvider.IsVIFrame;
			}

			internal set
			{
				_videoProvider.IsVIFrame = value;
			}
		}

		// TODO: optimize managed to unmanaged using the ActiveChanged event
		public IInputCallbackSystem InputCallbacks { [FeatureNotImplemented] get; private set; }
	}
}
