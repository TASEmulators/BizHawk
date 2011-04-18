using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
/*
Registers:
---------------------------
I do not know whether or not this mapper suffers from bus conflicts.  Use caution!

  $8000-FFFF:  [PPPP PPP.]
               [CCCC CCCC]
    P = Selects 32k PRG @ $8000
    C = Selects 8k CHR @ $0000

This is very strange.  Bits 1-7 seem to be used by both CHR and PRG.
 
Games:
    Magic Dragon
*/

    class Mapper107 : NES.NESBoardBase
    {
        int prg, chr;
        public override bool Configure(NES.EDetectionOrigin origin)
        {
            //configure
			switch (Cart.board_type)
			{
				case "Mapper107":
					break;
				default:
					return false;
			}
            return true;
        }

        public override byte ReadPPU(int addr)
        {
            return VRAM[addr + (chr * 0x2000)];
        }

        public override byte ReadPRG(int addr)
        {
            return VROM[addr + (prg * 0x8000)];
        }

        public override void WriteWRAM(int addr, byte value)
        {
            chr = value;
            prg = addr >> 1;
            base.WriteWRAM(addr, value);
        }

		public override void SyncState(Serializer ser)
        {
			base.SyncState(ser);
            ser.Sync("prg", ref prg);
            ser.Sync("chr", ref chr);
        }
    }
}
