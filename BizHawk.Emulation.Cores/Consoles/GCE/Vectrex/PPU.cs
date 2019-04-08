using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public class PPU
	{
		public VectrexHawk Core { get; set; }

		public byte ReadReg(int addr)
		{
			return 0;
		}

		public void WriteReg(int addr, byte value)
		{

		}

		public void tick()
		{

		}

		public virtual void latch_delay()
		{

		}

		public void render(int render_cycle)
		{

		}

		public void Reset()
		{

		}

		public void SyncState(Serializer ser)
		{

		}
	}
}
