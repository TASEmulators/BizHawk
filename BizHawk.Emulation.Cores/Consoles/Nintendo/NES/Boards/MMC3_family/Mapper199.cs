using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper199 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER199":
					break;
				default:
					return false;
			}

			exRegs[0] = (byte)(Cart.prg_size / 8 - 2);
			exRegs[1] = (byte)(Cart.prg_size / 8 - 1);
			exRegs[2] = 1;
			exRegs[3] = 3;

			BaseSetup();
			mmc3.MirrorMask = 3;
			return true;
		}

		public override void Dispose()
		{
			exRegs.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if (addr<0x1000)
				{
					if (addr<0x400)
					{
						if (mmc3.regs[0]<8)
						{
							return VRAM[(mmc3.regs[0] << 10) + (addr & 0x3FF)];
						} else
						{
							return VROM[(mmc3.regs[0] << 10) + (addr & 0x3FF)];
						}
					}
					else if (addr<0x800)
					{
						if (exRegs[2] < 8)
						{
							return VRAM[(exRegs[2] << 10) + (addr & 0x3FF)];
						}
						else
						{
							return VROM[(exRegs[2] << 10) + (addr & 0x3FF)];
						}
					}
					else if (addr < 0xC00)
					{
						if (mmc3.regs[1] < 8)
						{
							return VRAM[(mmc3.regs[1] << 10) + (addr & 0x3FF)];
						}
						else
						{
							return VROM[(mmc3.regs[1] << 10) + (addr & 0x3FF)];
						}
					}
					else
					{
						if (exRegs[3] < 8)
						{
							return VRAM[(exRegs[3] << 10) + (addr & 0x3FF)];
						}
						else
						{
							return VROM[(exRegs[3] << 10) + (addr & 0x3FF)];
						}
					}
				}
				else
				{
					int bank_1k = Get_CHRBank_1K(addr);
					if (bank_1k < 8)
					{
						return VRAM[(bank_1k << 10) + (addr & 0x3FF)];
					}
					else
					{
						return VROM[(bank_1k << 10) + (addr & 0x3FF)];
					}
				}
			}
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (addr < 0x1000)
				{
					if (addr < 0x400)
					{
						if (mmc3.regs[0] < 8)
						{
							VRAM[(mmc3.regs[0] << 10) + (addr & 0x3FF)]=value;
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
							VRAM[(exRegs[2] << 10) + (addr & 0x3FF)]=value;
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
							VRAM[(mmc3.regs[1] << 10) + (addr & 0x3FF)]=value;
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
							VRAM[(exRegs[3] << 10) + (addr & 0x3FF)]=value;
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
						VRAM[(bank_1k << 10) + (addr & 0x3FF)]=value;
					}
					else
					{
						// nothing
					}
				}
			}
			else
				base.WritePPU(addr, value);
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			if (addr >= 0x4000 && addr < 0x6000)
			{
				return exRegs[0];
			}
			else if (addr >= 0x6000)
			{
				return exRegs[1];
			}

			return base.Get_PRGBank_8K(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			if ((addr == 1) && ((mmc3.cmd & 0x8) > 0))
			{
				exRegs[mmc3.cmd & 3] = value;
			}
			else 
				base.WritePRG(addr, value);
		}
	}
}
