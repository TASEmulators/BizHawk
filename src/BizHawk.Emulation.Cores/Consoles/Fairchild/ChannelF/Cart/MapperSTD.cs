namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Standard ChannelF Cartridge
	/// 2KB ROM / NO RAM
	/// </summary>
	public class MapperSTD : VesCartBase
	{
		public override string BoardType => "STD";

		public MapperSTD(byte[] rom)
		{
			_rom = new byte[0x10000 - 0x800];
			Array.Copy(rom, _rom, rom.Length);
			_rom.AsSpan(rom.Length).Fill(0xFF);
			_ram = [ ];
		}

		public override byte ReadBus(ushort addr)
		{
			var off = addr - 0x800;
			return _rom[off];
		}

		public override void WriteBus(ushort addr, byte value)
		{
			// no writeable memory
		}

		public override byte ReadPort(ushort addr)
			=> 0xFF;

		public override void WritePort(ushort addr, byte data)
		{
			// no writeable hardware
		}
	}
}
