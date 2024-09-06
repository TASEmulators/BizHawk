namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Saba Schach Mapper
	/// Any size ROM / 2KB RAM mapped at 0x2800 - 0x2FFF
	/// Info here: http://www.seanriddle.com/chanfmulti.html
	/// </summary>
	public class MapperSCHACH : VesCartBase
	{
		public override string BoardType => "SCHACH";
		public override bool HasActivityLED => true;
		public override string ActivityLEDDescription => "Chess Brain Thinking Activity";

		public MapperSCHACH(byte[] rom)
		{
			_rom = new byte[0x10000 - 0x800];
			Array.Copy(rom, _rom, rom.Length);
			_rom.AsSpan(rom.Length).Fill(0xFF);
			_ram = new byte[0x800];
		}

		public override byte ReadBus(ushort addr)
		{
			byte result;
			if (addr is >= 0x2800 and < 0x3000)
			{
				// 2KB RAM
				result = _ram[addr - 0x2800];
			}
			else
			{
				result = _rom[addr - 0x800];
			}

			return result;
		}

		public override void WriteBus(ushort addr, byte value)
		{
			// 2KB writeable memory at 0x2800;
			if (addr is >= 0x2800 and < 0x3000)
			{
				_ram[addr - 0x2800] = value;
			}
			else if (addr == 0x3800)
			{
				ActivityLED = false;
			}
			else if (addr == 0x8000)
			{
				ActivityLED = true;
			}
		}

		public override byte ReadPort(ushort addr)
			=> 0xFF;

		public override void WritePort(ushort addr, byte data)
		{
			// no writeable hardware
		}
	}
}
