using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IVideoProvider
	{
		private int[] _vidBuff = [ ];
		public int VirtualWidth => BufferWidth;
		public int VirtualHeight { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator { get; }
		public int VsyncDenominator { get; }
		public int[] GetVideoBuffer() => _vidBuff;

		private unsafe void UpdateVideo(int gamma = -1)
		{
			using (_elf.EnterExit())
			{
				_core.dsda_get_video(gamma, out var vi);

				int[] _palBuffer = [ ];
				var videoBuffer = (byte*)vi.VideoBuffer.ToPointer();
				var paletteBuffer = (int*)vi.PaletteBuffer.ToPointer();
				BufferWidth = vi.Width;
				BufferHeight = vi.Height;

				// Handling pallette buffer
				if (_palBuffer.Length < vi.PaletteSize)
				{
					_palBuffer = new int[vi.PaletteSize];
				}
				for (var i = 0; i < _palBuffer.Length; i++)
				{
					_palBuffer[i] = paletteBuffer[i];
				}

				// Handling video buffer
				if (_vidBuff.Length < BufferWidth * BufferHeight)
				{
					_vidBuff = new int[BufferWidth * BufferHeight];
				}
				for (var i = 0; i < _vidBuff.Length; i++)
				{
					_vidBuff[i] = _palBuffer[videoBuffer[i]];
				}
			}
		}
	}
}
