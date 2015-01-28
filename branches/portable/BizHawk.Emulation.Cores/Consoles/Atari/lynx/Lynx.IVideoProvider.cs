using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
    public partial class Lynx : IVideoProvider
    {
        private const int WIDTH = 160;
        private const int HEIGHT = 102;

        private int[] videobuff = new int[WIDTH * HEIGHT];

        public int[] GetVideoBuffer()
        {
            return videobuff;
        }

        public int VirtualWidth
        {
            get { return BufferWidth; }
        }

        public int VirtualHeight
        {
            get { return BufferHeight; }
        }

        public int BufferWidth { get; private set; }

        public int BufferHeight { get; private set; }

        public int BackgroundColor
        {
            get { return unchecked((int)0xff000000); }
        }
    }
}
