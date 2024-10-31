
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

			var divider = address / 0x2000;

			switch (divider)
			{
				case 0:
					// WRAM
					result = WRAM[address];
					break;

				case 1:
					// IO address space
					break;

				case 2:
					// VRAM
					result = VRAM[address - 0x4000];
					break;

				case 3:
					// nothing here
					break;

				case 4:
					// cartridge rom banking
					// 0x8000 - 0xBFFF is selectable using the 3 bits from the SystemControl register
					switch (BankSelect)
					{
						// first 16k
						case 0:
							result = _cartridge.ReadByte((ushort) (address % 0x2000));
							break;

						// second 16k
						case 1:
							result = _cartridge.ReadByte((ushort) ((address % 0x2000) + 0x2000));
							break;

						// third 16k
						case 2:
							result = _cartridge.ReadByte((ushort) ((address % 0x2000) + 0x4000));
							break;
					}
					break;

				case 5:
					// fixed to the last 16K in the cart address space
					result = _cartridge.ReadByte(address);
					break;
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
