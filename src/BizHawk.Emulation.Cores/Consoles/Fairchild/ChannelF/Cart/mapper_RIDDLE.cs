namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Sean Riddle's modified SCHACH cart mapper (multi-cart) (WIP)
	/// </summary>
	public class mapper_RIDDLE : VesCartBase
	{
		public override string BoardType => "RIDDLE";

		public mapper_RIDDLE(byte[] rom)
		{
			ROM = new byte[rom.Length];
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
					result = ROM[off + (MultiBank * 0x2000) + (MultiHalfBank * 0x1000)];
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
			else if (addr == 0x3000)
			{
				// bank switching
				MultiBank = value & 0x1F;
				MultiHalfBank = (value & 0x20) >> 5;
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

		public override void Reset()
		{
			base.Reset();
			MultiBank = 0;
			MultiHalfBank = 0;
		}
	}
}
