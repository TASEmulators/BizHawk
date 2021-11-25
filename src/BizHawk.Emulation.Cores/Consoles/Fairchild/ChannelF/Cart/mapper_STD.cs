
using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Standard ChannelF Cartridge
	/// 2KB ROM / NO RAM
	/// </summary>
	public class mapper_STD : VesCartBase
	{
		public override string BoardType => "STD";

		public mapper_STD(byte[] rom)
		{
			ROM = new byte[0xFFFF - 0x800];
			for (int i = 0; i < rom.Length; i++)
			{
				ROM[i] = rom[i];
			}

			RAM = new byte[0];
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
			return 0xFF;
		}

		public override void WritePort(ushort addr, byte data)
		{
			// no writeable hardware
		}

		public override void SyncState(Serializer ser)
		{
			ser.BeginSection("Cart");
			ser.EndSection();
		}
	}
}
