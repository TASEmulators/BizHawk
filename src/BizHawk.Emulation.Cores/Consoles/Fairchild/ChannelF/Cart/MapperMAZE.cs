namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Maze ChannelF Cartridge
	/// Utilises 2102 SRAM over IO
	/// </summary>
	public class MapperMAZE : VesCartBase
	{
		public override string BoardType => "MAZE";

		public MapperMAZE(byte[] rom)
		{
			_rom = new byte[0x10000 - 0x800];
			Array.Copy(rom, _rom, rom.Length);
			_rom.AsSpan(rom.Length).Fill(0xFF);
			_ram = new byte[0x400];
		}

		public override byte ReadBus(ushort addr)
		{
			var off = addr - 0x800;
			return _rom[off];
		}

		public override void WriteBus(ushort addr, byte value)
		{
			// no directly writeable memory
		}

		public override byte ReadPort(ushort addr)
		{
			var index = addr - 0x24;
			return SRAM2102_Read(index);
		}

		public override void WritePort(ushort addr, byte data)
		{
			var index = addr - 0x24;
			SRAM2102_Write(index, data);
		}
	}
}
