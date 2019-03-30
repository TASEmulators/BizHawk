using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	// Here is where you will write the renderer for the core.
	// You will probably spend just about all of your time writing this part.
	// Plan ahead on what types of memory structures you want, and understand how the physical display unit sees data
	// if you get stuck, probably GBHawk has the cleanest implementation to use for reference

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

		// you should be able to run the PPU one step at a time through this method.
		public void tick()
		{

		}

		// if some values aren't latched immediately, you might need this function to delay their latching
		public virtual void latch_delay()
		{

		}

		public void render(int render_cycle)
		{

		}

		// Reset all values here, should be called along with other reset methods
		public void Reset()
		{

		}

		public void SyncState(Serializer ser)
		{

		}
	}
}
