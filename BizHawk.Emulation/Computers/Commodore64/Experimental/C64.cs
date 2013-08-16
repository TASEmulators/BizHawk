using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public abstract partial class C64 : IMotherboard
    {
        Rom basicRom;
        Cassette cassette;
        Rom characterRom;
        Ram colorRam;
        Cpu cpu;
        Expansion expansion;
        Joystick joystickA;
        Joystick joystickB;
        Rom kernalRom;
        Keyboard keyboard;
        Ram memory;
        Pla pla;
        Serial serial;
        Sid sid;
        Vic vic;

        public C64(C64Timing timing)
        {
        }

        public void ExecuteFrame()
        {
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
