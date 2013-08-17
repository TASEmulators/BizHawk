using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public abstract partial class C64 : IMotherboard
    {
        protected Rom basicRom;
        protected Cassette cassette;
        protected Rom characterRom;
        protected Cia cia1;
        protected Cia cia2;
        protected Ram colorRam;
        protected Cpu cpu;
        protected Expansion expansion;
        protected Joystick joystickA;
        protected Joystick joystickB;
        protected Rom kernalRom;
        protected Keyboard keyboard;
        protected Ram memory;
        protected Pla pla;
        protected Serial serial;
        protected Sid sid;
        protected Userport user;
        protected Vic vic;

        public C64(C64Timing timing)
        {
        }

        public void ExecuteFrame()
        {
            vic.Clock();
            vic.Clock();
            vic.Clock();
            vic.Clock();
            vic.Precache();
            cpu.Clock();
            cpu.Precache();
        }

        public byte PeekBasicRom(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekCartridge(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekCharRom(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekCpu(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekKernalRom(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekRam(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekSerial(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekSid(int addr)
        {
            throw new NotImplementedException();
        }

        public byte PeekVic(int addr)
        {
            throw new NotImplementedException();
        }

        public void PokeBasicRom(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeCartridge(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeCharRom(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeCpu(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeKernalRom(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeRam(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeSerial(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeSid(int addr, byte val)
        {
            throw new NotImplementedException();
        }

        public void PokeVic(int addr, byte val)
        {
            throw new NotImplementedException();
        }
    }

    public class C64Timing
    {
    }
}
