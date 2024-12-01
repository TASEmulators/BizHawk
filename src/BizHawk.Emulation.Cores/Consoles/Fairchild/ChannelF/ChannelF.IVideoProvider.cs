using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IVideoProvider, IRegionable
	{
		/// <summary>
		/// 128x64 pixels - 8192x2bits (2 KB)
		/// For the purposes of this core we will use 8192 bytes and just mask 0x03
		/// </summary>
		private byte[] _vram = new byte[128 * 64];

		public static readonly int[] FPalette =
		[
			// 0x101010, 0xFDFDFD, 0x5331FF, 0x5DCC02, 0xF33F4B, 0xE0E0E0, 0xA6FF91, 0xD0CEFF

			Colors.ARGB(0x10, 0x10, 0x10), // Black
			Colors.ARGB(0xFD, 0xFD, 0xFD), // White
			Colors.ARGB(0xFF, 0x31, 0x53), // Red
			Colors.ARGB(0x02, 0xCC, 0x5D), // Green
			Colors.ARGB(0x4B, 0x3F, 0xF3), // Blue
			Colors.ARGB(0xE0, 0xE0, 0xE0), // Gray
			Colors.ARGB(0x91, 0xFF, 0xA6), // BGreen
			Colors.ARGB(0xCE, 0xD0, 0xFF), // BBlue
		];

		public static readonly int[] CMap =
		[
			0, 1, 1, 1,
			7, 4, 2, 3,
			5, 4, 2, 3,
			6, 4, 2, 3,
		];

		private int _latchColour = 2;
		private int _latchX;
		private int _latchY;
		private int[] _videoBuffer;
		private double _pixelClockCounter;
		private double _pixelClocksRemaining;

		private int ScanlineRepeats;
		private int PixelWidth;
		private int HTotal;
		private int HBlankOff;
		private int HBlankOn;
		private int VTotal;
		private int VBlankOff;
		private int VBlankOn;
		private double PixelClocksPerCpuClock;
		private double PixelClocksPerFrame;

		private int HDisplayable => HBlankOn - HBlankOff - TrimLeft - TrimRight;
		private int VDisplayable => VBlankOn - VBlankOff - TrimTop - TrimBottom;

		private int TrimLeft;
		private int TrimRight;
		private int TrimTop;
		private int TrimBottom;

		private void SetupVideo()
		{
			_videoBuffer = new int[HTotal * VTotal];

			switch (_syncSettings.Viewport)
			{
				case ViewPort.AllVisible:
					TrimLeft = 0;
					TrimRight = 0;
					TrimTop = 0;
					TrimBottom = 0;
					break;

				case ViewPort.Trimmed:
					// https://channelf.se/veswiki/index.php?title=VRAM
					TrimLeft = 5;
					TrimRight = 5;
					TrimTop = 5;
					TrimBottom = 5;
					break;
			}
		}

		/// <summary>
		/// Called after every CPU clock
		/// </summary>
		private void ClockVideo()
		{
			while (_pixelClocksRemaining > 1)
			{
				var currScanline = (int)(_pixelClockCounter / HTotal);
				var currPixelInLine = (int)(_pixelClockCounter - currScanline * HTotal);
				var currRowInVram = currScanline / ScanlineRepeats;
				var currColInVram = currPixelInLine / PixelWidth;

				if (currScanline < VBlankOff || currScanline >= VBlankOn)
				{
					// vertical flyback
				}
				else if (currPixelInLine < HBlankOff || currPixelInLine >= HBlankOn)
				{
					// horizontal flyback
				}
				else
				{
					// active display
					if (currRowInVram < 64)
					{
						var p1 = _vram[(currRowInVram * 0x80) + 125] & 0x03;
						var p2 = _vram[(currRowInVram * 0x80) + 126] & 0x03;
						var pOffset = ((p2 & 0x02) | (p1 >> 1)) << 2;

						var colourIndex = pOffset + (_vram[currColInVram | (currRowInVram << 7)] & 0x03);
						_videoBuffer[(currScanline * HTotal) + currPixelInLine] = FPalette[CMap[colourIndex]];
					}
				}

				_pixelClockCounter++;
				_pixelClocksRemaining -= 1;
			}

			_pixelClocksRemaining += PixelClocksPerCpuClock;
			while (_pixelClockCounter >= PixelClocksPerFrame)
			{
				_pixelClockCounter -= PixelClocksPerFrame;
			}
		}		

		private static int[] ClampBuffer(int[] buffer, int originalWidth, int originalHeight, int trimLeft, int trimTop, int trimRight, int trimBottom)
		{
			var newWidth = originalWidth - trimLeft - trimRight;
			var newHeight = originalHeight - trimTop - trimBottom;
			var newBuffer = new int[newWidth * newHeight];

			for (var y = 0; y < newHeight; y++)
			{
				for (var x = 0; x < newWidth; x++)
				{
					var originalIndex = (y + trimTop) * originalWidth + (x + trimLeft);
					var newIndex = y * newWidth + x;
					newBuffer[newIndex] = buffer[originalIndex];
				}
			}

			return newBuffer;
		}

		private static double GetVerticalModifier(int bufferWidth, int bufferHeight, double targetAspectRatio)
		{
			var currentAspectRatio = (double)bufferWidth / bufferHeight;
			var verticalModifier = currentAspectRatio / targetAspectRatio;
			return verticalModifier;
		}

		public int VirtualWidth => HDisplayable * 2;
		public int VirtualHeight => (int)(VDisplayable * GetVerticalModifier(HDisplayable, VDisplayable, 4.0/3.0)) * 2;
		public int BufferWidth => HDisplayable;
		public int BufferHeight => VDisplayable;
		public int BackgroundColor => Colors.ARGB(0xFF, 0xFF, 0xFF);
		public int VsyncNumerator { get; private set; }
		public int VsyncDenominator { get; private set; }


		// https://channelf.se/veswiki/index.php?title=VRAM
		// 'The emulator MESS uses a fixed 102x58 resolution starting at (4,4) but the safe area for a real system is about 95x58 pixels'
		// 'Note that the pixel aspect is a lot closer to widescreen (16:9) than standard definition (4:3). On a TV from the 70's or 80's pixels are rectangular, standing up. In widescreen mode they are close to perfect squares'
		// https://channelf.se/veswiki/index.php?title=Resolution
		// 'Even though PAL televisions system has more lines vertically, the Channel F displays about the same as on the original NTSC video system'
		//
		// Right now we are just trimming based on the HBLANK and VBLANK values (we might need to go further like the other emulators)
		// VirtualWidth is being used to force the aspect ratio into 4:3
		// On real hardware it looks like this (so we are close): https://www.youtube.com/watch?v=ZvQA9tiEIuQ
		public int[] GetVideoBuffer()
			=> ClampBuffer(_videoBuffer, HTotal, VTotal, HBlankOff + TrimLeft, VBlankOff + TrimTop, HTotal - HBlankOn + TrimRight, VTotal - VBlankOn + TrimBottom);

		public DisplayType Region => _region == RegionType.NTSC ? DisplayType.NTSC : DisplayType.PAL;
	}
}
