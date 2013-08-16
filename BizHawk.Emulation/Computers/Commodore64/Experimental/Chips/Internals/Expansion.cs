using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Expansion
    {
        public Func<int> InputAddress;
        public Func<bool> InputBA;
        public Func<int> InputData;
        public Func<bool> InputDotClock;
        public Func<bool> InputHiExpansion;
        public Func<bool> InputHiRom;
        public Func<bool> InputIRQ;
        public Func<bool> InputLoExpansion;
        public Func<bool> InputLoRom;
        public Func<bool> InputNMI;
        public Func<bool> InputRead;
        public Func<bool> InputReset;

        virtual public int Address { get { return 0xFFFF; } }
        virtual public int Data { get { return 0xFF; } }
        virtual public bool ExRom { get { return true; } }
        virtual public bool Game { get { return true; } }
        virtual public bool IRQ { get { return true; } }
        virtual public bool NMI { get { return true; } }
        public int OutputAddress() { return Address; }
        public int OutputData() { return Data; }
        public bool OutputExRom() { return ExRom; }
        public bool OutputGame() { return Game; }
        public bool OutputIRQ() { return IRQ; }
        public bool OutputNMI() { return NMI; }
        public bool OutputRead() { return Read; }
        public bool OutputReset() { return Reset; }
        virtual public void Precache() { }
        virtual public bool Read { get { return true; } }
        virtual public bool Reset { get { return true; } }
        virtual public void SyncState(Serializer ser) { }
    }
}
