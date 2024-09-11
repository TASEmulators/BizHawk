namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Sean Riddle's modified SCHACH cart mapper (multi-cart)
	/// </summary>
	public class MapperRIDDLE : VesCartBase
	{
		public override string BoardType => "RIDDLE";

		public MapperRIDDLE(byte[] rom)
		{
			_rom = new byte[rom.Length];
			Array.Copy(rom, _rom, rom.Length);
			_ram = new byte[0x800];
		}

		public override byte ReadBus(ushort addr)
		{
			var result = 0xFF;
			var off = addr - 0x800;

			if (addr is >= 0x2800 and < 0x3000)
			{
				// 2KB RAM
				result = _ram[addr - 0x2800];
			}
			else
			{
				if (off < _rom.Length)
					result = _rom[off + (MultiBank * 0x2000) + (MultiHalfBank * 0x1000)];
			}

			return (byte)result;
		}

		public override void WriteBus(ushort addr, byte value)
		{
			// 2KB writeable memory at 0x2800;
			if (addr is >= 0x2800 and < 0x3000)
			{
				_ram[addr - 0x2800] = value;
			}
			else if (addr == 0x3000)
			{
				// bank switching
				MultiBank = value & 0x1F;
				MultiHalfBank = (value & 0x20) >> 5;
			}
		}

		public override byte ReadPort(ushort addr)
			=> 0xFF;

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
