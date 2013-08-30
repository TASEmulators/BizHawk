using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips;
using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    static public partial class C64ChipPresets
    {
        static public C64 PAL(byte[] basic, byte[] kernal, byte[] character)
        {
            C64 result = new C64(PALTiming());
            result.basicRom = ChipPresets.Rom2364(basic);
            result.cassette = new Cassette();
            result.characterRom = ChipPresets.Rom2332(character);
            result.cia1 = ChipPresets.Cia6526(true);
            result.cia2 = ChipPresets.Cia6526(true);
            result.colorRam = ChipPresets.Ram2114();
            result.cpu = new Cpu();
            result.expansion = new Expansion();
            result.joystickA = new Joystick();
            result.joystickB = new Joystick();
            result.kernalRom = ChipPresets.Rom2364(kernal);
            result.keyboard = new Keyboard();
            result.memory = ChipPresets.Ram4864();
            result.pla = new Pla();
            result.serial = new Serial();
            result.sid = ChipPresets.Sid6581();
            result.user = new Userport();
            result.vic = ChipPresets.Vic6569();
            result.InitializeConnections();
            return result;
        }

        static public C64Timing PALTiming()
        {
            return new C64Timing();
        }
    }
}
