
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
				// 0x0000 - 0x1FFF
				// WRAM - 8K
				case 0:					
					result = WRAM[address];
					break;

				// 0x2000 - 0x3FFF
				// IO address space - 8K
				case 1:
					result = ReadHardware(address);
					break;

				// 0x4000 - 0x5FFF
				// VRAM - 8K
				case 2:
					result = VRAM[address % 0x2000];
					break;

				// 0x6000 - 0x7FFF
				case 3:
					// nothing here
					break;

				// 0x8000 - 0xBFFF
				// Cart bank 1 - 16K
				case 4:
				case 5:
					// 0x8000 - 0xBFFF is selectable using the 3 bits from the SystemControl register
					// 0: first 16k
					// 1: 2nd 16k
					// 2: 3rd 16k
					result = _cartridge.ReadByte((ushort) ((address % 0x4000) + (BankSelect * 0x4000)));
					break;

				// 0xC000 - 0xFFFF
				// Cart bank 2 - 16K
				case 6:
				case 7:
					// fixed to the last 16K in the cart address space
					result = _cartridge.ReadByte((ushort) ((address % 0x4000) + (3 * 0x4000)));
					break;
			}

			return result;
		}

		public void WriteMemory(ushort address, byte value)
		{
			var divider = address / 0x2000;

			switch (divider)
			{
				// 0x0000 - 0x1FFF
				// WRAM - 8K
				case 0:
					WRAM[address] = value;
					break;

				// 0x2000 - 0x3FFF
				// IO address space - 8K
				case 1:
					WriteHardware(address, value);
					break;

				// 0x4000 - 0x5FFF
				// VRAM - 8K
				case 2:
					VRAM[address % 0x2000] = value;
					break;

				// 0x6000 - 0x7FFF
				case 3:
					// nothing here
					break;

				// 0x8000 - 0xBFFF
				// Cart bank 1 - 16K
				case 4:
				case 5:
					// 0x8000 - 0xBFFF is selectable using the 3 bits from the SystemControl register
					// 0: first 16k
					// 1: 2nd 16k
					// 2: 3rd 16k
					_cartridge.WriteByte((ushort) ((address % 0x4000) + (BankSelect * 0x4000)), value);
					break;

				// 0xC000 - 0xFFFF
				// Cart bank 2 - 16K
				case 6: 
				case 7:
					// fixed to the last 16K in the cart address space
					_cartridge.WriteByte((ushort) ((address % 0x4000) + (3 * 0x4000)), value);
					break;
			}			
		}
	}
}
