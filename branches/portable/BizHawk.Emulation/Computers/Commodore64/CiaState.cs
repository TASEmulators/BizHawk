using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Cia
	{
		public void SyncState(Serializer ser)
		{
			ser.Sync("ALARM", ref regs.ALARM);
			ser.Sync("ALARM10", ref regs.ALARM10);
			ser.Sync("ALARMHR", ref regs.ALARMHR);
			ser.Sync("ALARMMIN", ref regs.ALARMMIN);
			ser.Sync("ALARMPM", ref regs.ALARMPM);
			ser.Sync("ALARMSEC", ref regs.ALARMSEC);
			ser.Sync("CNT", ref regs.CNT);
			ser.Sync("EIALARM", ref regs.EIALARM);
			ser.Sync("EIFLG", ref regs.EIFLG);
			ser.Sync("EISP", ref regs.EISP);
			ser.Sync("EIT0", ref regs.EIT[0]);
			ser.Sync("EIT1", ref regs.EIT[1]);
			ser.Sync("FLG", ref regs.FLG);
			ser.Sync("IALARM", ref regs.IALARM);
			ser.Sync("IFLG", ref regs.IFLG);
			ser.Sync("INMODE0", ref regs.INMODE[0]);
			ser.Sync("INMODE1", ref regs.INMODE[1]);
			ser.Sync("IRQ", ref regs.IRQ);
			ser.Sync("ISP", ref regs.ISP);
			ser.Sync("IT0", ref regs.IT[0]);
			ser.Sync("IT1", ref regs.IT[1]);
			ser.Sync("LOAD0", ref regs.LOAD[0]);
			ser.Sync("LOAD1", ref regs.LOAD[1]);
			ser.Sync("OUTMODE0", ref regs.OUTMODE[0]);
			ser.Sync("OUTMODE1", ref regs.OUTMODE[1]);
			ser.Sync("PBON0", ref regs.PBON[0]);
			ser.Sync("PBON1", ref regs.PBON[1]);
			ser.Sync("RUNMODE0", ref regs.RUNMODE[0]);
			ser.Sync("RUNMODE1", ref regs.RUNMODE[1]);
			ser.Sync("SDR", ref regs.SDR);
			ser.Sync("SDRCOUNT", ref regs.SDRCOUNT);
			ser.Sync("SPMODE", ref regs.SPMODE);
			ser.Sync("START0", ref regs.START[0]);
			ser.Sync("START1", ref regs.START[1]);
			ser.Sync("T0", ref regs.T[0]);
			ser.Sync("T1", ref regs.T[1]);
			ser.Sync("TICK0", ref regs.TICK[0]);
			ser.Sync("TICK1", ref regs.TICK[1]);
			ser.Sync("TLATCH0", ref regs.TLATCH[0]);
			ser.Sync("TLATCH1", ref regs.TLATCH[1]);
			ser.Sync("TOD10", ref regs.TOD10);
			ser.Sync("TODHR", ref regs.TODHR);
			ser.Sync("TODMIN", ref regs.TODMIN);
			ser.Sync("TODPM", ref regs.TODPM);
			ser.Sync("TODREADLATCH", ref regs.TODREADLATCH);
			ser.Sync("TODREADLATCH10", ref regs.TODREADLATCH10);
			ser.Sync("TODREADLATCHSEC", ref regs.TODREADLATCHSEC);
			ser.Sync("TODREADLATCHMIN", ref regs.TODREADLATCHMIN);
			ser.Sync("TODREADLATCHHR", ref regs.TODREADLATCHHR);
			ser.Sync("TODSEC", ref regs.TODSEC);

			// ports
			byte dir0 = regs.connectors[0].Direction;
			byte dir1 = regs.connectors[1].Direction;
			byte latch0 = regs.connectors[0].Latch;
			byte latch1 = regs.connectors[0].Latch;
			ser.Sync("DIR0", ref dir0);
			ser.Sync("DIR1", ref dir1);
			ser.Sync("PORT0", ref latch0);
			ser.Sync("PORT1", ref latch1);
			if (ser.IsReader)
			{
				regs.connectors[0].Direction = dir0;
				regs.connectors[0].Latch = latch0;
				regs.connectors[1].Direction = dir1;
				regs.connectors[1].Latch = latch1;
			}

			// state
			ser.Sync("INTMASK", ref intMask);
			ser.Sync("LASTCNT", ref lastCNT);
			ser.Sync("TODCOUNTER", ref todCounter);
			ser.Sync("TODFREQUENCY", ref todFrequency);
			ser.Sync("UNDERFLOW0", ref underflow[0]);
			ser.Sync("UNDERFLOW1", ref underflow[1]);
		}
	}
}
