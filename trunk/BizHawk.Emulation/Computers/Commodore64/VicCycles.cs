using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII
	{
		private CycleFlags[] cycleActions;

		private struct CycleFlags
		{
			public bool baFetch;
			public uint baspr0;
			public bool baspr0e;
			public uint baspr1;
			public bool baspr1e;
			public uint baspr2;
			public bool baspr2e;
			public bool chkBrdL0;
			public bool chkBrdL1;
			public bool chkBrdR0;
			public bool chkBrdR1;
			public bool chkSprCrunch;
			public bool chkSprDisp;
			public bool chkSprDma;
			public bool chkSprExp;
			public uint cycle;
			public bool fetchC;
			public bool fetchG;
			public bool fetchIdle;
			public bool fetchRefresh;
			public uint phase;
			public uint sprDma0;
			public bool sprDma0e;
			public uint sprDma1;
			public bool sprDma1e;
			public uint sprDma2;
			public bool sprDma2e;
			public uint sprPtr;
			public bool sprPtre;
			public bool updateMcBase;
			public bool updateRc;
			public bool updateVc;
			public uint vis;
			public bool vise;
			public uint x;
		}

		static private uint[,] CycleTableNTSC =
		{
			{ 1, 0, 0x19C, 0x40, 0x13, 0x345, 0 },
			{ 1, 1, 0x1A0, 0x40, 0x13, 0x345, 0 },
			{ 2, 0, 0x1A4, 0x40, 0x13, 0x345, 0 },
			{ 2, 1, 0x1A8, 0x40, 0x13, 0x345, 0 },
		};

	}

}
