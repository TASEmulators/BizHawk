using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Cia
	{
		public StateParameters State
		{
			get
			{
				StateParameters result = new StateParameters();
				
				// registers
				result.Save("ALARM", regs.ALARM);
				result.Save("ALARM10", regs.ALARM10);
				result.Save("ALARMHR", regs.ALARMHR);
				result.Save("ALARMMIN", regs.ALARMMIN);
				result.Save("ALARMPM", regs.ALARMPM);
				result.Save("ALARMSEC", regs.ALARMSEC);
				result.Save("CNT", regs.CNT);
				result.Save("EIALARM", regs.EIALARM);
				result.Save("EIFLG", regs.EIFLG);
				result.Save("EISP", regs.EISP);
				result.Save("EIT0", regs.EIT[0]);
				result.Save("EIT1", regs.EIT[1]);
				result.Save("FLG", regs.FLG);
				result.Save("IALARM", regs.IALARM);
				result.Save("IFLG", regs.IFLG);
				result.Save("INMODE0", regs.INMODE[0]);
				result.Save("INMODE1", regs.INMODE[1]);
				result.Save("IRQ", regs.IRQ);
				result.Save("ISP", regs.ISP);
				result.Save("IT0", regs.IT[0]);
				result.Save("IT1", regs.IT[1]);
				result.Save("LOAD0", regs.LOAD[0]);
				result.Save("LOAD1", regs.LOAD[1]);
				result.Save("OUTMODE0", regs.OUTMODE[0]);
				result.Save("OUTMODE1", regs.OUTMODE[1]);
				result.Save("PBON0", regs.PBON[0]);
				result.Save("PBON1", regs.PBON[1]);
				result.Save("RUNMODE0", regs.RUNMODE[0]);
				result.Save("RUNMODE1", regs.RUNMODE[1]);
				result.Save("SDR", regs.SDR);
				result.Save("SDRCOUNT", regs.SDRCOUNT);
				result.Save("SPMODE", regs.SPMODE);
				result.Save("START0", regs.START[0]);
				result.Save("START1", regs.START[1]);
				result.Save("T0", regs.T[0]);
				result.Save("T1", regs.T[1]);
				result.Save("TICK0", regs.TICK[0]);
				result.Save("TICK1", regs.TICK[1]);
				result.Save("TLATCH0", regs.TLATCH[0]);
				result.Save("TLATCH1", regs.TLATCH[1]);
				result.Save("TOD10", regs.TOD10);
				result.Save("TODHR", regs.TODHR);
				result.Save("TODMIN", regs.TODMIN);
				result.Save("TODPM", regs.TODPM);
				result.Save("TODREADLATCH", regs.TODREADLATCH);
				result.Save("TODREADLATCH10", regs.TODREADLATCH10);
				result.Save("TODREADLATCHSEC", regs.TODREADLATCHSEC);
				result.Save("TODREADLATCHMIN", regs.TODREADLATCHMIN);
				result.Save("TODREADLATCHHR", regs.TODREADLATCHHR);
				result.Save("TODSEC", regs.TODSEC);

				// ports
				result.Save("DIR0", regs.connectors[0].Direction);
				result.Save("DIR1", regs.connectors[1].Direction);
				result.Save("PORT0", regs.connectors[0].Latch);
				result.Save("PORT1", regs.connectors[1].Latch);

				// state
				result.Save("INTMASK", intMask);
				result.Save("LASTCNT", lastCNT);
				result.Save("TODCOUNTER", todCounter);
				result.Save("TODFREQUENCY", todFrequency);
				result.Save("UNDERFLOW0", underflow[0]);
				result.Save("UNDERFLOW1", underflow[1]);

				return result;
			}
			set
			{
				StateParameters result = value;

				// registers
				result.Load("ALARM", out regs.ALARM);
				result.Load("ALARM10", out regs.ALARM10);
				result.Load("ALARMHR", out regs.ALARMHR);
				result.Load("ALARMMIN", out regs.ALARMMIN);
				result.Load("ALARMPM", out regs.ALARMPM);
				result.Load("ALARMSEC", out regs.ALARMSEC);
				result.Load("CNT", out regs.CNT);
				result.Load("EIALARM", out regs.EIALARM);
				result.Load("EIFLG", out regs.EIFLG);
				result.Load("EISP", out regs.EISP);
				result.Load("EIT0", out regs.EIT[0]);
				result.Load("EIT1", out regs.EIT[1]);
				result.Load("FLG", out regs.FLG);
				result.Load("IALARM", out regs.IALARM);
				result.Load("IFLG", out regs.IFLG);
				result.Load("INMODE0", out regs.INMODE[0]);
				result.Load("INMODE1", out regs.INMODE[1]);
				result.Load("IRQ", out regs.IRQ);
				result.Load("ISP", out regs.ISP);
				result.Load("IT0", out regs.IT[0]);
				result.Load("IT1", out regs.IT[1]);
				result.Load("LOAD0", out regs.LOAD[0]);
				result.Load("LOAD1", out regs.LOAD[1]);
				result.Load("OUTMODE0", out regs.OUTMODE[0]);
				result.Load("OUTMODE1", out regs.OUTMODE[1]);
				result.Load("PBON0", out regs.PBON[0]);
				result.Load("PBON1", out regs.PBON[1]);
				result.Load("RUNMODE0", out regs.RUNMODE[0]);
				result.Load("RUNMODE1", out regs.RUNMODE[1]);
				result.Load("SDR", out regs.SDR);
				result.Load("SDRCOUNT", out regs.SDRCOUNT);
				result.Load("SPMODE", out regs.SPMODE);
				result.Load("START0", out regs.START[0]);
				result.Load("START1", out regs.START[1]);
				result.Load("T0", out regs.T[0]);
				result.Load("T1", out regs.T[1]);
				result.Load("TICK0", out regs.TICK[0]);
				result.Load("TICK1", out regs.TICK[1]);
				result.Load("TLATCH0", out regs.TLATCH[0]);
				result.Load("TLATCH1", out regs.TLATCH[1]);
				result.Load("TOD10", out regs.TOD10);
				result.Load("TODHR", out regs.TODHR);
				result.Load("TODMIN", out regs.TODMIN);
				result.Load("TODPM", out regs.TODPM);
				result.Load("TODREADLATCH", out regs.TODREADLATCH);
				result.Load("TODREADLATCH10", out regs.TODREADLATCH10);
				result.Load("TODREADLATCHSEC", out regs.TODREADLATCHSEC);
				result.Load("TODREADLATCHMIN", out regs.TODREADLATCHMIN);
				result.Load("TODREADLATCHHR", out regs.TODREADLATCHHR);
				result.Load("TODSEC", out regs.TODSEC);

				// ports
				regs.connectors[0].Direction = (byte)result["DIR0"];
				regs.connectors[1].Direction = (byte)result["DIR1"];
				regs.connectors[0].Latch = (byte)result["LATCH0"];
				regs.connectors[1].Latch = (byte)result["LATCH1"];

				// state
				result.Load("INTMASK", out intMask);
				result.Load("LASTCNT", out lastCNT);
				result.Load("TODCOUNTER", out todCounter);
				result.Load("TODFREQUENCY", out todFrequency);
				result.Load("UNDERFLOW0", out underflow[0]);
				result.Load("UNDERFLOW1", out underflow[1]);
			}
		}
	}
}
