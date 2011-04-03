using System;

namespace BizHawk.Emulation.CPUs.ARM
{

	partial class ARM
	{
		public ARM.CP15 cp15;

		public uint cp15_MRC(uint opc1, uint t, uint crn, uint crm, uint opc2)
		{
			if (t == 15) unpredictable = true;
			switch (crn)
			{
				case 13:
					switch (opc2)
					{
						case 0: return cp15.FCSEIDR;
						case 1: return cp15.CONTEXTIDR;
						case 2: return cp15.TPIDRURW;
						case 3: return cp15.TPIDRURO;
						case 4: return cp15.TPIDRPRW;
					}
					break;
			}

			//unhandled...
			unpredictable = true;
			return 0;
		}

		public string cp15_Describe(uint opc1, uint t, uint crn, uint crm, uint opc2)
		{
			switch (crn)
			{
				case 13:
					switch (opc2)
					{
						case 0: return "CP15.FCSEIDR: FCSE PID";
						case 1: return "CP15.CONTEXTIDR: Context ID";
						case 2: return "CP15.TPIDRURW: User RW Thread ID";
						case 3: return "CP15.TPIDRURW: User RO Thread ID";
						case 4: return "CP15.TPIDRURW: Priv. Only Thread ID";
					}
					break;
			}

			return "unknown";
		}

		public class CP15
		{
			//c13
			public uint FCSEIDR;
			public uint CONTEXTIDR;
			public uint TPIDRURW, TPIDRURO, TPIDRPRW;
		}
	}

}