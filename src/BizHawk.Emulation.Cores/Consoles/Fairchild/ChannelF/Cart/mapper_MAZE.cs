
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;
using System.Collections;

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

			RAM = new byte[400];
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
			var result = 0xFF;

			switch (addr)
			{
				case 0x24:
					if (m_read_write == 0)
					{
						m_addr = m_addr_latch;
						m_data0 = RAM[m_addr] & 1;
						return (byte)((m_latch[0] & 0x7f) | (m_data0 << 7));
					}

					return m_latch[0];

				case 0x25:
					return m_latch[1];
			}

			return (byte)result;
		}

		public override void WritePort(ushort addr, byte data)
		{
			switch (addr)
			{
				case 24:
					m_latch[0] = data;

					m_read_write = data.Bit(0) ? 1 : 0;// BIT(data, 0);

					//m_addr_latch = (m_addr_latch & 0x3f3) | (BIT(data, 2) << 2) | (BIT(data, 1) << 3);  // bits 2,3 come from this write!
					m_addr_latch = (ushort)((m_addr_latch & 0x3f3) | (data.Bit(2) ? 1 : 0 << 2) | (data.Bit(1) ? 1 : 0 << 3));  // bits 2,3 come from this write!

					m_addr = m_addr_latch;

					m_data0 = data.Bit(3) ? 1 : 0; // BIT(data, 3);

					if (m_read_write == 1)
						RAM[m_addr] = (byte)m_data0;
					break;

				case 25:
					m_latch[1] = data;
					// all bits but 2,3 come from this write, but they are shuffled
					// notice that data is 8bits, so when swapping bit8 & bit9 are always 0!
					//m_addr_latch = (m_addr_latch & 0x0c) | (bitswap < 16 > ((uint16_t)data, 15, 14, 13, 12, 11, 10, 7, 6, 5, 3, 2, 1, 9, 8, 4, 0));

					BitArray b = new BitArray(m_addr_latch);
					b[9] = m_addr_latch.Bit(7);
					b[8] = m_addr_latch.Bit(6);
					b[7] = m_addr_latch.Bit(5);
					b[6] = m_addr_latch.Bit(3);
					b[5] = m_addr_latch.Bit(2);
					b[4] = m_addr_latch.Bit(1);
					b[3] = m_addr_latch.Bit(3);
					b[2] = m_addr_latch.Bit(2);
					b[1] = m_addr_latch.Bit(4);
					b[0] = m_addr_latch.Bit(0);
					var resBytes = new byte[1];
					b.CopyTo(resBytes, 0);
					m_addr_latch = resBytes[0];

					break;
			}
		}
	}
}
