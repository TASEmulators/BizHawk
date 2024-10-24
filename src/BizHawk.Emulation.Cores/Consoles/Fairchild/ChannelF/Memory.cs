namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Memory and related functions
	/// </summary>
	public partial class ChannelF
	{
		private readonly byte[] _bios01;
		private readonly byte[] _bios02;

		/// <summary>
		/// Simulates reading a byte of data from the address space
		/// </summary>
		public byte ReadBus(ushort addr)
		{
			if (addr < 0x400)
			{
				// BIOS ROM 1
				return _bios01[addr];
			}
			else if (addr < 0x800)
			{
				// BIOS ROM 2
				return _bios02[addr - 0x400];
			}
			else
			{
				// Cartridge Memory Space
				return _cartridge.ReadBus(addr);
			}
		}

		public CDLResult ReadCDL(ushort addr)
		{
			var result = new CDLResult();
			int divisor = addr / 0x400;
			result.Address = addr % 0x400;

			switch (divisor)
			{
				case 0:
					result.Type = CDLType.BIOS1;
					break;

				case 1:
					result.Type = CDLType.BIOS2;
					break;

				default:
					result.Type = CDLType.CARTROM;
					break;
			}

			return result;
		}

		/// <summary>
		/// Simulates writing a byte of data to the address space (in its default configuration, there is no writeable RAM in the
		/// Channel F addressable through the address space)
		/// </summary>
		public void WriteBus(ushort addr, byte value)
			=> _cartridge.WriteBus(addr, value);
	}
}
