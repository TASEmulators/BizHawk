using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public interface IMotherboard
    {
        void ExecuteFrame();

        byte PeekBasicRom(int addr);
        byte PeekCartridge(int addr);
        byte PeekCharRom(int addr);
        byte PeekCpu(int addr);
        byte PeekKernalRom(int addr);
        byte PeekRam(int addr);
        byte PeekSerial(int addr);
        byte PeekSid(int addr);
        byte PeekVic(int addr);

        void PokeBasicRom(int addr, byte val);
        void PokeCartridge(int addr, byte val);
        void PokeCharRom(int addr, byte val);
        void PokeCpu(int addr, byte val);
        void PokeKernalRom(int addr, byte val);
        void PokeRam(int addr, byte val);
        void PokeSerial(int addr, byte val);
        void PokeSid(int addr, byte val);
        void PokeVic(int addr, byte val);
    }
}
