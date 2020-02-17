namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Memory and related functions
	/// </summary>
	public partial class ChannelF
	{
		public byte[] BIOS01 = new byte[1024];
		public byte[] BIOS02 = new byte[1024];

		/// <summary>
		/// Simulates reading a byte of data from the address space
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		public byte ReadBus(ushort addr)
		{
			if (addr < 0x400)
			{
				// Rom0
				return BIOS01[addr];
			}
			else if (addr < 0x800)
			{
				// Rom1
				return BIOS02[addr - 0x400];
			}
			else if (addr < 0x2000)
			{
				// Cart
				//return 0;
				return Rom[addr - 0x800];
			}

			return 0xFF;
		}

		/// <summary>
		/// Simulates writing a byte of data to the address space (in its default configuration, there is no writeable RAM in the
		/// Channel F addressable through the address space)
		/// </summary>
		/// <param name="addr"></param>
		/// <returns></returns>
		public void WriteBus(ushort addr, byte value)
		{

		}
	}
}
