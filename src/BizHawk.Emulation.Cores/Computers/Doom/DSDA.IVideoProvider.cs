using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IVideoProvider
	{
		private int[] _palBuffer = [ ];
		private int[] _vidBuff = [ ];
		public int VirtualWidth => BufferWidth;
		public int VirtualHeight { get; private set; }
		public int PaletteSize { get; private set; }
		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }
		public int BackgroundColor => unchecked((int)0xff000000);
		public int VsyncNumerator { get; }
		public int VsyncDenominator { get; }
		public int[] GetVideoBuffer() => _vidBuff;

		private unsafe void UpdateVideo()
		{
			using (_elf.EnterExit())
			{
				var videoBufferSrc = IntPtr.Zero;
				var palletteBufferSrc = IntPtr.Zero;

				_core.dsda_get_video(out var width, out var height, out var pitch, ref videoBufferSrc, out var paletteSize, ref palletteBufferSrc);

				var videoBuffer = (byte*) videoBufferSrc.ToPointer();
				var paletteBuffer = (int*) palletteBufferSrc.ToPointer();
				PaletteSize = paletteSize;
				BufferWidth = width;
				BufferHeight = height;

				// Handling pallette buffer
				if (_palBuffer.Length < PaletteSize)
				{
					_palBuffer = new int[PaletteSize];
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
