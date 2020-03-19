using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IVideoProvider
	{
		public int[] GetVideoBuffer() => _machine.Video.GetVideoBuffer();

		// put together, these describe a metric on the screen
		// they should define the smallest size that the buffer can be placed inside such that:
		// 1. no actual pixel data is lost
		// 2. aspect ratio is accurate
		public int VirtualWidth => 560;
		public int VirtualHeight => 384;
		public int BufferWidth => 560;
		public int BufferHeight => 384;
		public int BackgroundColor => 0;

		public int VsyncNumerator
		{
			[FeatureNotImplemented] // TODO: precise numbers or confirm the default is okay
			get => NullVideo.DefaultVsyncNum;
		}

		public int VsyncDenominator
		{
			[FeatureNotImplemented] // TODO: precise numbers or confirm the default is okay
			get => NullVideo.DefaultVsyncDen;
		}
	}
}
