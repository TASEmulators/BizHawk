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
        static private C64Timing timing;

        public C64PAL() : base(timing)
        {
            this.basicRom = new Rom2364();
            this.cassette = new Cassette();
            this.characterRom = new Rom2332();
            this.cia1 = new Cia();
            this.cia2 = new Cia();
            this.colorRam = new Ram2114();
            this.cpu = new Cpu();
            this.expansion = new Expansion();
            this.joystickA = new Joystick();
            this.joystickB = new Joystick();
            this.kernalRom = new Rom2364();
            this.keyboard = new Keyboard();
            this.memory = new Ram4864();
            this.pla = new Pla();
            this.serial = new Serial();
            this.sid = new MOS6581();
            this.user = new Userport();
            this.vic = new MOS6569();
            InitializeConnections();
        }
    }
}
