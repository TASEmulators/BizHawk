using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public abstract partial class Vic : IVideoProvider
	{
		private int[] buf;
		private int bufHeight;
		private uint bufLength;
		private uint bufOffset;
		private Point bufPoint;
		private Rectangle bufRect;
		private int bufWidth;

		// palette
		private int[] palette =
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

		public int BackgroundColor
		{
			get { return Colors.ARGB(0, 0, 0); }
		}

		public int BufferHeight
		{
			get { return bufRect.Height; }
		}

		public int BufferWidth
		{
			get { return bufRect.Width; }
		}

		public int[] GetVideoBuffer()
		{
			return buf;
		}

		public int VirtualWidth
		{
			get { return bufRect.Width; }
		}
	}
}
