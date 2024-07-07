namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// ChannelF Cartridge that utilises 2102 SRAM over IO
	/// </summary>
	public class mapper_MAZE : VesCartBase
	{
		public override string BoardType => "MAZE";

		public mapper_MAZE(byte[] rom)
		{
			ROM = new byte[0xFFFF - 0x800];
			for (int i = 0; i < rom.Length; i++)
			{
				ROM[i] = rom[i];
			}

			RAM = new byte[0x400];
		}

		public override byte ReadBus(ushort addr)
		{
			var off = addr - 0x800;
			return ROM[off];
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
