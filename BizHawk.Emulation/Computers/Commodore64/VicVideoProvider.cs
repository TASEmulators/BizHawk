using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII : IVideoProvider
	{
		private static int[] palette =
		{
			Colors.ARGB(0x00, 0x00, 0x00),
			Colors.ARGB(0xFF, 0xFF, 0xFF),
			Colors.ARGB(0x68, 0x37, 0x2B),
			Colors.ARGB(0x70, 0xA4, 0xB2),
			Colors.ARGB(0x6F, 0x3D, 0x86),
			Colors.ARGB(0x58, 0x8D, 0x43),
			Colors.ARGB(0x35, 0x28, 0x79),
			Colors.ARGB(0xB8, 0xC7, 0x6F),
			Colors.ARGB(0x6F, 0x4F, 0x25),
			Colors.ARGB(0x43, 0x39, 0x00),
			Colors.ARGB(0x9A, 0x67, 0x59),
			Colors.ARGB(0x44, 0x44, 0x44),
			Colors.ARGB(0x6C, 0x6C, 0x6C),
			Colors.ARGB(0x9A, 0xD2, 0x84),
			Colors.ARGB(0x6C, 0x5E, 0xB5),
			Colors.ARGB(0x95, 0x95, 0x95)
		};

		private int[] screenBuffer;
		private uint screenHeight;
		private uint screenOffset;
		private uint screenOffsetX;
		private uint screenOffsetY;
		private bool screenOffsetVisibleX;
		private bool screenOffsetVisibleY;
		private uint screenWidth;
		private uint visibleBottom;
		private uint visibleHeight;
		private uint visibleLeft;
		private uint visibleRight;
		private uint visibleTop;
		private uint visibleWidth;

		public int BackgroundColor
		{
			get { return Colors.ARGB(0, 0, 0); }
		}

		public int BufferHeight
		{
			get { return (int)visibleHeight; }
		}

		public int BufferWidth
		{
			get { return (int)visibleWidth; }
		}

		public int[] GetVideoBuffer()
		{
			return screenBuffer;
		}

		public int VirtualWidth
		{
			get { return BufferWidth; }
		}

		private void WritePixel(uint pixel)
		{
			if (screenOffsetX == visibleLeft)
				screenOffsetVisibleX = true;
			else if (screenOffsetX == visibleRight)
				screenOffsetVisibleX = false;

			if (screenOffsetY == visibleTop)
				screenOffsetVisibleY = true;
			else if (screenOffsetY == visibleBottom)
				screenOffsetVisibleY = false;

			if (screenOffsetVisibleX && screenOffsetVisibleY)
				screenBuffer[screenOffset] = palette[pixel];

			screenOffset++;
			screenOffsetX++;
			if (screenOffsetX == screenWidth)
			{
				screenOffsetX = 0;
				screenOffsetY++;
				if (screenOffsetY == screenHeight)
				{
					screenOffsetY = 0;
					screenOffset = 0;
				}
			}
		}
	}
}
