using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Rom
    {
        public Func<int> InputAddress;
        public Func<int> InputData;
        public Func<bool> InputSelect;

        protected int addressMask;
        protected int dataMask;
        protected int[] memory;

        public Rom(int size, int addressMask, int dataMask)
        {
            this.addressMask = addressMask;
            this.dataMask = dataMask;
            this.memory = new int[size];
        }

        virtual public int Data
        {
            get
            {
                if (InputSelect())
                    return memory[InputAddress() & addressMask] & dataMask;
                return 0xFF;
            }
        }

        public int OutputData() { return Data; }
        virtual public void Precache() { }
        virtual public void SyncState(Serializer ser) { }
    }
}
