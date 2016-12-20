using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Subor : NES.NESBoardBase
	{
		private ByteBuffer regs = new ByteBuffer(4);
		private bool is167;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER166":
					break;
				case "MAPPER167":
					is167 = true;
					break;
				default:
					return false;
			}

			return true;
		}

		public override void Dispose()
		{
			regs.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("regs", ref regs);
			ser.Sync("is167", ref is167);
		}

		public override void WritePRG(int addr, byte value)
		{
			regs[(addr >> 13) & 0x03] = value;
		}

		public override byte ReadPRG(int addr)
		{
			int basea, bank;
			basea = ((regs[0] ^ regs[1]) & 0x10) << 1;
			bank = (regs[2] ^ regs[3]) & 0x1f;

			if ((regs[1] & 0x08) > 0)
			{
				bank &= 0xFE;
				if (is167)
				{
					if (addr < 0x4000)
					{
						return ROM[((basea + bank + 1) * 0x4000) + (addr & 0x3FFF)];
					}
					else
					{
						return ROM[((basea + bank + 0) * 0x4000) + (addr & 0x3FFF)];
					}
				}
				else
				{
					if (addr < 0x4000)
					{
						return ROM[((basea + bank + 0) * 0x4000) + (addr & 0x3FFF)];
					}
					else
					{
						return ROM[((basea + bank + 1) * 0x4000) + (addr & 0x3FFF)];
					}
				}
			}
			else
			{
				if ((regs[1] & 0x04) > 0)
				{
					if (addr < 0x4000)
					{
						return ROM[(0x1F * 0x4000) + (addr & 0x3FFF)];
					}
					else
					{
						return ROM[((basea + bank) * 0x4000) + (addr & 0x3FFF)];
					}
				}
				else
				{
					if (addr < 0x4000)
					{
						return ROM[((basea + bank) * 0x4000) + (addr & 0x3FFF)];
					}
					else
					{
						if (is167)
						{
							return ROM[(0x20 * 0x4000) + (addr & 0x3FFF)];
						}
						else
						{
							return ROM[(0x07 * 0x4000) + (addr & 0x3FFF)];
						}
					}
				}
			}
		}
	}
}
