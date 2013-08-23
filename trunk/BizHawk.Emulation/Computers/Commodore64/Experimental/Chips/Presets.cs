using BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips
{
    static public class Presets
    {
        static public Cia Cia6526(bool todJumper) { return new Cia(Settings6526(todJumper)); }
        static public Cia Cia6526A(bool todJumper) { return new Cia(Settings6526A(todJumper)); }
        static public Cpu Cpu6510() { return new Cpu(); }
        static public Ram Ram2114() { return new Ram(0x1000, 0x0FFF, 0x0F); }
        static public Ram Ram4864() { return new Ram(0x10000, 0xFFFF, 0xFF); }
        static public Rom Rom2332(byte[] data) { return new Rom(0x1000, 0xFFF, data); }
        static public Rom Rom2364(byte[] data) { return new Rom(0x2000, 0x1FFF, data); }
        static public Sid Sid6581() { return new Sid(Settings6581()); }
        static public Sid Sid8580() { return new Sid(Settings8580()); }
        static public Vic Vic6567() { return new Vic(Settings6567()); }
        static public Vic Vic6569() { return new Vic(Settings6569()); }

        static private CiaSettings Settings6526(bool todJumper)
        {
            CiaSettings result = new CiaSettings();
            return result;
        }

        static private CiaSettings Settings6526A(bool todJumper)
        {
            CiaSettings result = new CiaSettings();
            return result;
        }

        static private VicSettings Settings6567()
        {
            VicSettings result = new VicSettings();
            return result;
        }

        static private VicSettings Settings6569()
        {
            VicSettings result = new VicSettings();

            VicTiming timing = new VicTiming();
            timing.CharacterBAStart = 0x1EC;
            timing.Frequency = 17734472 / 18;
            timing.HBlankDelay = 0;
            timing.HBlankEnd = 0x1E0;
            timing.HBlankStart = 0x17C;
            timing.HSize = 0x1F8;
            timing.LineStart = 0x194;
            timing.RefreshStart = 0x1E4;
            timing.SpriteBAStart = 0x14C;
            timing.VBlankEnd = 0x010;
            timing.VBlankStart = 0x12C;
            timing.VSize = 0x138;

            result.timing = timing;
            return result;
        }

        static private SidSettings Settings6581()
        {
            SidSettings result = new SidSettings();
            return result;
        }

        static private SidSettings Settings8580()
        {
            SidSettings result = new SidSettings();
            return result;
        }
    }
}
