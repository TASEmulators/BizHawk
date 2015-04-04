using BizHawk.Emulation.Common;
using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII
	{
		public class BizVideoService : Jellyfish.Virtu.Services.VideoService, IVideoProvider
		{
			public int[] fb;

			int[] IVideoProvider.GetVideoBuffer() { return fb; }

			// put together, these describe a metric on the screen
			// they should define the smallest size that the buffer can be placed inside such that:
			// 1. no actual pixel data is lost
			// 2. aspect ratio is accurate
			int IVideoProvider.VirtualWidth { get { return 560; } }
			int IVideoProvider.VirtualHeight { get { return 384; } }

			int IVideoProvider.BufferWidth { get { return 560; } }
			int IVideoProvider.BufferHeight { get { return 384; } }
			int IVideoProvider.BackgroundColor { get { return 0; } }

			public BizVideoService(Machine machine) :
				base(machine)
			{
				fb = new int[560 * 384];
			}

			public override void SetFullScreen(bool isFullScreen)
			{

			}

			public override void SetPixel(int x, int y, uint color)
			{
				int i = 560 * y + x;
				fb[i] = fb[i + 560] = (int)color;
			}
			public override void Update()
			{

			}
		}
	}
}
