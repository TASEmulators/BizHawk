using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Cartridge : IMedia
	{
		public int bank;

		private byte Read0000(ushort addr)
		{
			// standard cart, no banking
			CartridgeChip currentChip = chips[0];
			return currentChip.data[addr & currentChip.romMask];
		}

		private byte Read0001(ushort addr)
		{
			return 0;
		}

		private byte Read0002(ushort addr)
		{
			return 0;
		}

		private byte Read0003(ushort addr)
		{
			return 0;
		}

		private byte Read0004(ushort addr)
		{
			return 0;
		}

		private byte Read0005(ushort addr)
		{
			return 0;
		}

		private byte Read0006(ushort addr)
		{
			return 0;
		}

		private byte Read0007(ushort addr)
		{
			return 0;
		}

		private byte Read0008(ushort addr)
		{
			return 0;
		}

		private byte Read0009(ushort addr)
		{
			return 0;
		}

		private byte Read000A(ushort addr)
		{
			return 0;
		}

		private byte Read000B(ushort addr)
		{
			return 0;
		}

		private byte Read000C(ushort addr)
		{
			return 0;
		}

		private byte Read000D(ushort addr)
		{
			return 0;
		}

		private byte Read000E(ushort addr)
		{
			return 0;
		}

		private byte Read000F(ushort addr)
		{
			return 0;
		}

		private byte Read0010(ushort addr)
		{
			return 0;
		}

		private byte Read0011(ushort addr)
		{
			return 0;
		}

		private byte Read0012(ushort addr)
		{
			return 0;
		}

		private byte Read0013(ushort addr)
		{

			return 0;
		}

		private byte Read0014(ushort addr)
		{
			return 0;
		}

		private byte Read0015(ushort addr)
		{
			return 0;
		}

		private byte Read0016(ushort addr)
		{
			return 0;
		}

		private byte Read0017(ushort addr)
		{
			return 0;
		}

		private byte Read0018(ushort addr)
		{
			return 0;
		}

		private byte Read0019(ushort addr)
		{
			return 0;
		}

		private byte Read001A(ushort addr)
		{
			return 0;
		}

		private byte Read001B(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0000(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0001(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0002(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0003(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0004(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0005(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0006(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0007(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0008(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0009(ushort addr)
		{
			return 0;
		}

		private byte ReadPort000A(ushort addr)
		{
			return 0;
		}

		private byte ReadPort000B(ushort addr)
		{
			return 0;
		}

		private byte ReadPort000C(ushort addr)
		{
			return 0;
		}

		private byte ReadPort000D(ushort addr)
		{
			return 0;
		}

		private byte ReadPort000E(ushort addr)
		{
			return 0;
		}

		private byte ReadPort000F(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0010(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0011(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0012(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0013(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0014(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0015(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0016(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0017(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0018(ushort addr)
		{
			return 0;
		}

		private byte ReadPort0019(ushort addr)
		{
			return 0;
		}

		private byte ReadPort001A(ushort addr)
		{
			return 0;
		}

		private byte ReadPort001B(ushort addr)
		{
			return 0;
		}

		private void WritePort0000(ushort addr, byte val)
		{
		}

		private void WritePort0001(ushort addr, byte val)
		{
		}

		private void WritePort0002(ushort addr, byte val)
		{
		}

		private void WritePort0003(ushort addr, byte val)
		{
		}

		private void WritePort0004(ushort addr, byte val)
		{
		}

		private void WritePort0005(ushort addr, byte val)
		{
		}

		private void WritePort0006(ushort addr, byte val)
		{
		}

		private void WritePort0007(ushort addr, byte val)
		{
		}

		private void WritePort0008(ushort addr, byte val)
		{
		}

		private void WritePort0009(ushort addr, byte val)
		{
		}

		private void WritePort000A(ushort addr, byte val)
		{
		}

		private void WritePort000B(ushort addr, byte val)
		{
		}

		private void WritePort000C(ushort addr, byte val)
		{
		}

		private void WritePort000D(ushort addr, byte val)
		{
		}

		private void WritePort000E(ushort addr, byte val)
		{
		}

		private void WritePort000F(ushort addr, byte val)
		{
		}

		private void WritePort0010(ushort addr, byte val)
		{
		}

		private void WritePort0011(ushort addr, byte val)
		{
		}

		private void WritePort0012(ushort addr, byte val)
		{
		}

		private void WritePort0013(ushort addr, byte val)
		{
			bank = (val & 0x7F) % chips.Count;
			if ((bank & 0x80) != 0x00)
			{
				exRomPin = false;
				gamePin = false;
			}
			else
			{
				exRomPin = true;
				gamePin = false;
			}
			UpdateRomPins();
		}

		private void WritePort0014(ushort addr, byte val)
		{
		}

		private void WritePort0015(ushort addr, byte val)
		{
		}

		private void WritePort0016(ushort addr, byte val)
		{
		}

		private void WritePort0017(ushort addr, byte val)
		{
		}

		private void WritePort0018(ushort addr, byte val)
		{
		}

		private void WritePort0019(ushort addr, byte val)
		{
		}

		private void WritePort001A(ushort addr, byte val)
		{
		}

		private void WritePort001B(ushort addr, byte val)
		{
		}
	}
}
