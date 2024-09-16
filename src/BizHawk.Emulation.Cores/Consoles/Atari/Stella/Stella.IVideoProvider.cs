using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : IVideoProvider
	{
		public int[] GetVideoBuffer() => _vidBuff;

		public int VirtualWidth => Region == DisplayType.NTSC ? 275 : 320;

		public int VirtualHeight => BufferHeight;

		public int BufferWidth { get; private set; }

		public int BufferHeight { get; private set; }

		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

		private readonly int[] _vidPalette;
		private int[] _vidBuff = [ ];

		private unsafe void UpdateVideo()
		{
			using (_elf.EnterExit())
			{
				var src = IntPtr.Zero;
				Core.stella_get_video(out var width, out var height, out _, ref src);

				BufferWidth = width;
				BufferHeight = height;

				if (_vidBuff.Length < BufferWidth * BufferHeight)
				{
					_vidBuff = new int[BufferWidth * BufferHeight];
				}

				var buffer = (byte*)src.ToPointer();
				for (var i = 0; i < _vidBuff.Length; i++)
				{
					_vidBuff[i] = _vidPalette[buffer[i]];
				}
			}
		}
	}
}
