using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class DataPortSerialInputConverter : DataPortConverter
	{
		public override byte Convert(byte local, byte remote)
		{
			int via = (local & 0x70);
			via |= ((remote & 0x08) == 0) ? 0x80 : 0x00; //CIA ATNOUT - VIA ATNIN
			via |= ((remote & 0x10) == 0) ? 0x04 : 0x00; //CIA CLKOUT - VIA CLKIN
			via |= ((remote & 0x20) == 0) ? 0x01 : 0x00; //CIA DATOUT - VIA DATIN
			via |= ((remote & 0x40) == 0) ? 0x08 : 0x00; //CIA CLKIN - VIA CLKOUT
			via |= ((remote & 0x80) == 0) ? 0x02 : 0x00; //CIA DATIN - VIA DATOUT
			return (byte)via;
		}
	}

	public class DataPortSerialOutputConverter : DataPortConverter
	{
		public override byte Convert(byte local, byte remote)
		{
			int cia = (remote & 0x7);
			cia |= ((local & 0x01) == 0) ? 0x20 : 0x00; //VIA DATIN - CIA DATOUT
			cia |= ((local & 0x02) == 0) ? 0x80 : 0x00; //VIA DATOUT - CIA DATIN
			cia |= ((local & 0x04) == 0) ? 0x10 : 0x00; //VIA CLKIN - CIA CLKOUT
			cia |= ((local & 0x08) == 0) ? 0x40 : 0x00; //VIA CLKOUT - CIA CLKIN
			cia |= ((local & 0x80) == 0) ? 0x08 : 0x00; //VIA DATIN - CIA DATOUT
			return (byte)cia;
		}
	}
}
