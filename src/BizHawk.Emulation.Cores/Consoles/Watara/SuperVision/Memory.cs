
namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision
	{
		/// <summary>
		/// 8K of WRAM which is connected to the CPU
		/// </summary>
		private byte[] WRAM = new byte[0x2000];

		/// <summary>
		/// 8K of VRAM which the CPU can access with 0 wait states
		/// </summary>
		public byte[] VRAM = new byte[0x2000];

		

		/// <summary>
		/// Bank select index
		/// </summary>
		public int BankSelect;

		/// <summary>
		/// True when the CPU is accessing memory
		/// </summary>
		private bool _cpuMemoryAccess;

		public byte ReadMemory(ushort address)
		{
			_cpuMemoryAccess = true;
			byte result = 0xFF;

			if (address < 0x2000)
			{
				// RAM
				return WRAM[address];
			}

			if (address < 0x4000)
			{
				// port access
				return ReadHardware(address);
			}

			if (address < 0x6000)
			{
				// VRAM
				return VRAM[address - 0x4000];
			}

			if (address < 0x8000)
			{
				// nothing here
			}

			if (address < 0xC000)
			{
				// cartridge rom banking
				// 0x8000 - 0xBFFF is selectable using the 3 bits from the SystemControl register
				switch (BankSelect)
				{
					// first 16k
					case 0:
						return _cartridge.ReadByte((ushort)(address % 0x2000));

					// second 16k
					case 1:
						return _cartridge.ReadByte((ushort)((address % 0x2000) + 0x2000));

					// third 16k
					case 2:
						return _cartridge.ReadByte((ushort)((address % 0x2000) + 0x4000));
				}
			}

			if (address < 0xFFFF)
			{
				// fixed to the last 16K in the cart address space
				return _cartridge.ReadByte((ushort)((address % 0x2000) + 0x6000));
			}

			return result;
		}

		public void WriteMemory(ushort address, byte value)
		{
			if (address < 0x2000)
			{
				// RAM
				WRAM[address] = value;
			}
			else if (address < 0x4000)
			{
				// port access
				WriteHardware(address, value);
			}
			else if (address < 0x6000)
			{
				// VRAM
				VRAM[address - 0x4000] = value;
			}
			else if (address < 0x8000)
			{
				// nothing here
			}
			else if (address < 0xC000)
			{
				// cartridge rom banking
				// 0x8000 - 0xBFFF is selectable using the 3 bits from the SystemControl register
				switch (BankSelect)
				{
					// first 16k
					case 0:
						_cartridge.WriteByte((ushort) (address % 0x2000), value);
						break;

					// second 16k
					case 1:
						_cartridge.WriteByte((ushort) ((address % 0x2000) + 0x2000), value);
						break;

					// third 16k
					case 2:
						_cartridge.WriteByte((ushort) ((address % 0x2000) + 0x4000), value);
						break;
				}
			}

			if (address < 0xFFFF)
			{
				// fixed to the last 16K in the cart address space
				_cartridge.WriteByte((ushort) ((address % 0x2000) + 0x6000), value);
			}
		}
	}
}
