
using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Saba Schach Mapper
	/// Any size ROM / 2KB RAM mapped at 0x2800 - 0x2FFF
	/// Info here: http://www.seanriddle.com/chanfmulti.html
	/// </summary>
	public class mapper_SCHACH : VesCartBase
	{
		public override string BoardType => "SCHACH";

		public mapper_SCHACH(byte[] rom)
		{
			ROM = new byte[0xFFFF - 0x800];
			for (int i = 0; i < rom.Length; i++)
			{
				ROM[i] = rom[i];
			}

			RAM = new byte[0x800];
		}

		public override byte ReadBus(ushort addr)
		{
			var result = 0xFF;
			var off = addr - 0x800;

			if (addr >= 0x2800 && addr < 0x3000)
			{
				// 2KB RAM
				result = RAM[addr - 0x2800];
			}
			else
			{
				if (off < ROM.Length)
					result = ROM[off];
			}

			return (byte)result;
		}

		public override void WriteBus(ushort addr, byte value)
		{
			// 2KB writeable memory at 0x2800;
			if (addr >= 0x2800 && addr < 0x3000)
			{
				RAM[addr - 0x2800] = value;
			}
			else if (addr == 0x3800)
			{
				// activity LED
				ActivityLED = !ActivityLED;
			}
			else
			{

			}
		}

		public override byte ReadPort(ushort addr)
		{
			return 0xFF;
		}

		public override void WritePort(ushort addr, byte data)
		{
			// no writeable hardware
		}
	}
}
