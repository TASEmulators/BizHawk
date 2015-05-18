using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IVideoProvider
	{
		private Jellyfish.Virtu.Services.VideoService _V
		{ get { return _machine.Video.VideoService; } }

		int[] IVideoProvider.GetVideoBuffer() { return _V.fb; }

		// put together, these describe a metric on the screen
		// they should define the smallest size that the buffer can be placed inside such that:
		// 1. no actual pixel data is lost
		// 2. aspect ratio is accurate
		int IVideoProvider.VirtualWidth { get { return 560; } }
		int IVideoProvider.VirtualHeight { get { return 384; } }

		int IVideoProvider.BufferWidth { get { return 560; } }
		int IVideoProvider.BufferHeight { get { return 384; } }
		int IVideoProvider.BackgroundColor { get { return 0; } }

	}
}
