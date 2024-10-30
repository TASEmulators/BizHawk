
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public class SVCart
	{
		/// <summary>
		/// Max ROM size is 128KB
		/// </summary>
		private byte[] _rom = new byte[0x20000];

		public SVCart(byte[] rom)
		{
			if (_rom.Length != rom.Length)
			{
				_rom = new byte[rom.Length];
			}

			rom.CopyTo(_rom, 0);
		}

		public byte ReadByte(ushort address)
		{
			return _rom[address];
		}

		public void WriteByte(ushort address, byte value)
		{
			// In a standard cart, the WR pin is not connected
		}

		public static SVCart Configure(GameInfo gi, byte[] rom)
		{
			// Standard ROM
			return new SVCart(rom);
		}

		public void SyncByteArrayDomain(SuperVision sys)
		{
			sys.SyncByteArrayDomain("ROM", _rom);
		}
	}
}
