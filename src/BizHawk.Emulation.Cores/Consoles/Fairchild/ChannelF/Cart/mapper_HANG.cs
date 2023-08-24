namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Hangman ChannelF Cartridge
	/// 2KB ROM / NO RAM
	/// </summary>
	public class mapper_HANG : VesCartBase
	{
		public override string BoardType => "HANG";

		public mapper_HANG(byte[] rom)
		{
			ROM = new byte[0xFFFF - 0x800];
			for (int i = 0; i < rom.Length; i++)
			{
				ROM[i] = rom[i];
				if (i > 3000)
				{
					var test = rom[i];
				}
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
			// no writeable memory
		}

		public override byte ReadPort(ushort addr)
		{
			var index = addr - 0x20;
			return SRAM2102_Read(index);
		}

		public override void WritePort(ushort addr, byte data)
		{
			var index = addr - 0x20;
			SRAM2102_Write(index, data);
		}

	}
}
