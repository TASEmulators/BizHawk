using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips;
using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental
{
    public partial class C64PAL : C64
    {
        static private C64Timing timing = null;

        public C64PAL(byte[] basic, byte[] kernal, byte[] character) : base(timing)
        {
            this.basicRom = Presets.Rom2364(basic);
            this.cassette = new Cassette();
            this.characterRom = Presets.Rom2332(character);
            this.cia1 = Presets.Cia6526(true);
            this.cia2 = Presets.Cia6526(true);
            this.colorRam = Presets.Ram2114();
            this.cpu = new Cpu();
            this.expansion = new Expansion();
            this.joystickA = new Joystick();
            this.joystickB = new Joystick();
            this.kernalRom = Presets.Rom2364(kernal);
            this.keyboard = new Keyboard();
            this.memory = Presets.Ram4864();
            this.pla = new Pla();
            this.serial = new Serial();
            this.sid = Presets.Sid6581();
            this.user = new Userport();
            this.vic = Presets.Vic6569();
            InitializeConnections();
        }
    }
}
