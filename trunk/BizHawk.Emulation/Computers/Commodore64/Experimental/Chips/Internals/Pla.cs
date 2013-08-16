using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    public class Pla
    {
        public Func<int> InputAddress;
        public Func<bool> InputAEC;
        public Func<bool> InputBA;
        public Func<bool> InputCAS;
        public Func<bool> InputCharen;
        public Func<bool> InputExRom;
        public Func<bool> InputGame;
        public Func<bool> InputHiRam;
        public Func<bool> InputLoRam;
        public Func<bool> InputRead;
        public Func<int> InputVA;

        virtual public bool Basic { get { return true; } }
        virtual public bool CASRam { get { return true; } }
        virtual public bool CharRom { get { return true; } }
        virtual public bool GraphicsRead { get { return true; } }
        virtual public bool IO { get { return true; } }
        virtual public bool Kernal { get { return true; } }
        public bool OutputBasic() { return Basic; }
        public bool OutputCASRam() { return CASRam; }
        public bool OutputCharRom() { return CharRom; }
        public bool OutputGraphicsRead() { return GraphicsRead; }
        public bool OutputIO() { return IO; }
        public bool OutputKernal() { return Kernal; }
        public bool OutputRomHi() { return RomHi; }
        public bool OutputRomLo() { return RomLo; }
        virtual public void Precache() { }
        virtual public bool RomHi { get { return true; } }
        virtual public bool RomLo { get { return true; } }
        virtual public void SyncState(Serializer ser) { }
    }
}
