using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Ram : Rom
    {
        public Func<bool> InputRead;

        public Ram(int size, int addressMask, int dataMask)
            : base(size, addressMask, dataMask)
        {
        }

        virtual public void Execute()
        {
            if (!InputRead())
                memory[InputAddress() & addressMask] = InputData() & dataMask;
        }
    }
}
