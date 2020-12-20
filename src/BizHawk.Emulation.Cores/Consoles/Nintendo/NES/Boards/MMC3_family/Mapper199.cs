using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper199 : MMC3Board_Base
	{
		private byte[] exRegs = new byte[4];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER199":
					break;
				default:
					return false;
			}

			exRegs[0] = (byte)(Cart.PrgSize / 8 - 2);
			exRegs[1] = (byte)(Cart.PrgSize / 8 - 1);
			exRegs[2] = 1;
			exRegs[3] = 3;

			BaseSetup();
			mmc3.MirrorMask = 3;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(exRegs), ref exRegs, false);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (addr<0x1000)
				{
					if (addr<0x400)
					{
						if (mmc3.regs[0]<8)
						{
							return Vram[(mmc3.regs[0] << 10) + (addr & 0x3FF)];
						}

						return Vrom[(mmc3.regs[0] << 10) + (addr & 0x3FF)];
					}

					if (addr<0x800)
					{
						if (exRegs[2] < 8)
						{
							return Vram[(exRegs[2] << 10) + (addr & 0x3FF)];
						}

						return Vrom[(exRegs[2] << 10) + (addr & 0x3FF)];
					}

					if (addr < 0xC00)
					{
						if (mmc3.regs[1] < 8)
						{
							return Vram[(mmc3.regs[1] << 10) + (addr & 0x3FF)];
						}

						return Vrom[(mmc3.regs[1] << 10) + (addr & 0x3FF)];
					}

					if (exRegs[3] < 8)
					{
						return Vram[(exRegs[3] << 10) + (addr & 0x3FF)];
					}

					return Vrom[(exRegs[3] << 10) + (addr & 0x3FF)];
				}

				int bank_1k = Get_CHRBank_1K(addr);
				if (bank_1k < 8)
				{
					return Vram[(bank_1k << 10) + (addr & 0x3FF)];
				}

				return Vrom[(bank_1k << 10) + (addr & 0x3FF)];
			}

			return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (addr < 0x1000)
				{
					if (addr < 0x400)
					{
						if (mmc3.regs[0] < 8)
						{
							Vram[(mmc3.regs[0] << 10) + (addr & 0x3FF)]=value;
						}
						else
						{
							// nothing
						}
					}
					else if (addr < 0x800)
					{
						if (exRegs[2] < 8)
						{
							Vram[(exRegs[2] << 10) + (addr & 0x3FF)]=value;
						}
						else
						{
							// nothing
						}
					}
					else if (addr < 0xC00)
					{
						if (mmc3.regs[1] < 8)
						{
							Vram[(mmc3.regs[1] << 10) + (addr & 0x3FF)]=value;
						}
						else
						{
							//nothing
						}
					}
					else
					{
						if (exRegs[3] < 8)
						{
							Vram[(exRegs[3] << 10) + (addr & 0x3FF)]=value;
						}
						else
						{
							// nothing
						}
					}
				}
				else
				{
					int bank_1k = Get_CHRBank_1K(addr);
					if (bank_1k < 8)
					{
						Vram[(bank_1k << 10) + (addr & 0x3FF)]=value;
					}
					else
					{
						// nothing
					}
				}
			}
			else
				base.WritePpu(addr, value);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			if (addr >= 0x4000 && addr < 0x6000)
			{
				return exRegs[0];
			}

			if (addr >= 0x6000)
			{
				return exRegs[1];
			}

			return base.Get_PRGBank_8K(addr);
		}

		public override void WritePrg(int addr, byte value)
		{
			if ((addr == 1) && ((mmc3.cmd & 0x8) > 0))
			{
				exRegs[mmc3.cmd & 3] = value;
			}
			else 
				base.WritePrg(addr, value);
		}
	}
}
