using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public partial class Vic : IVideoProvider
    {
        protected int[] videoBuffer;

        public int[] GetVideoBuffer()
        {
            throw new NotImplementedException();
        }

        public int VirtualWidth
        {
            get { throw new NotImplementedException(); }
        }

        public int BufferWidth
        {
            get { throw new NotImplementedException(); }
        }

        public int BufferHeight
        {
            get { throw new NotImplementedException(); }
        }

        public int BackgroundColor
        {
            get { throw new NotImplementedException(); }
        }
    }
}
