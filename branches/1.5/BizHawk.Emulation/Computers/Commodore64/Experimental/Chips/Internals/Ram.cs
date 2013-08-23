using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public class Ram
    {
        protected int addressMask;
        protected int dataMask;
        protected int[] memory;

        public Ram(int size, int addressMask, int dataMask)
        {
            this.addressMask = addressMask;
            this.dataMask = dataMask;
            this.memory = new int[size];
        }

        public int Peek(int addr)
        {
            return memory[addr & addressMask];
        }

        public void Poke(int addr, int val)
        {
            memory[addr & addressMask] = val;
        }

        public int Read(int addr)
        {
            return memory[addr & addressMask];
        }

        public void Write(int addr, int val)
        {
            memory[addr & addressMask] = val & dataMask;
        }

        public void SyncState(Serializer ser) { Sync.SyncObject(ser, this); }
    }
}
