using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper217 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(4);

		public ByteBuffer prg_regs_8k = new ByteBuffer(4);
		private int prg_mask_8k, chr_mask_1k;
		private byte[] regs_sec = { 0, 6, 3, 7, 5, 2, 4, 1 };


		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER217":
					break;
				default:
					return false;
			}

			BaseSetup();

			exRegs[0] = 0x00;
			exRegs[1] = 0xFF;
			exRegs[2] = 0x03;
			exRegs[3] = 0x00;

			prg_mask_8k = Cart.prg_size / 8 - 1;
			chr_mask_1k = Cart.chr_size - 1;

			prg_regs_8k[0] = 0;
			prg_regs_8k[1] = 1;
			prg_regs_8k[2] = (byte)(0xFE & prg_mask_8k);
			prg_regs_8k[3] = (byte)(0xFF & prg_mask_8k);

			return true;
		}

		public override void Dispose()
		{
			exRegs.Dispose();
			prg_regs_8k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
			ser.Sync("ptg_regs_8k", ref prg_regs_8k);
			ser.Sync("prg_mask", ref prg_mask_8k);
			ser.Sync("chr_mask", ref chr_mask_1k);
		}

		public void sync_prg()
		{
			int temp = 0;
			for (int i=0;i<4;i++)
			{
			temp = mmc3.prg_regs_8k[i];

			if ((exRegs[1] & 0x8) > 0)
				temp = temp & 0x1F;
			else
				temp = ((temp & 0x0F) | (exRegs[1] & 0x10));

				temp |= (exRegs[1] << 5 & 0x60);
			prg_regs_8k[i] = (byte)(temp & prg_mask_8k);
			}
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x1000)
			{
				exRegs[0] = value;
				if ((value & 0x80)>0)
				{
					int bank = (byte)((value & 0x0F) | (exRegs[1] << 4 & 0x30));

					bank *= 2;
					bank &= prg_mask_8k;

					prg_regs_8k[0] = (byte)bank;
					prg_regs_8k[1] = (byte)(bank + 1);
					prg_regs_8k[2] = (byte)bank;
					prg_regs_8k[3] = (byte)(bank + 1);
				}
				else
				{
					sync_prg();
				}
			}

			else if (addr == 0x1001)
			{
				exRegs[1] = value;
				sync_prg();
			}

			else if (addr == 0x1007)
			{
				exRegs[2] = value;
			}

			base.WriteEXP(addr, value);
		}

		public override void WritePRG(int addr, byte value)
		{
			switch ((addr + 0x8000) & 0xE001)
			{
				case 0x8000:
					if (exRegs[2] > 0)
					{
						base.WritePRG(0x4000, value);
					}
					else
					{
						base.WritePRG(0x0000, value);
						sync_prg();
					}
					break;
				case 0x8001:
					if (exRegs[2] > 0)
					{
						value = (byte)((value & 0xC0) | regs_sec[value & 0x07]);
						exRegs[3] = 1;

						base.WritePRG(0x0000, value);
						sync_prg();
					}
					else
					{
						base.WritePRG(0x0001, value);
						sync_prg();
					}
					break;
				case 0xA000:
					if (exRegs[2] > 0)
					{
						if ((exRegs[3] > 0) && ((exRegs[0] & 0x80) == 0 || (mmc3.reg_addr & 0x7) < 6))
						{
							exRegs[3] = 0;
							base.WritePRG(0x0001, value);
							sync_prg();
						}
					}
					else
					{
						if (value == 0)
						{
							SetMirrorType(EMirrorType.Vertical);
						}
						else
						{
							SetMirrorType(EMirrorType.Horizontal);
						}
					}
					break;
				case 0xA001:
					if (exRegs[2] > 0)
					{
						if (value == 0)
						{
							SetMirrorType(EMirrorType.Vertical);
						}
						else
						{
							SetMirrorType(EMirrorType.Horizontal);
						}
					} else
					{
						base.WritePRG(0x2001, value);
					}
					break;
			}

			if (addr>=0x4000) 
				base.WritePRG(addr, value);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = base.Get_CHRBank_1K(addr);

				if ((exRegs[1] & 0x8) == 0)
					bank_1k = (bank_1k & 0x7F) | (exRegs[1] << 3 & 0x80);

				bank_1k |= (exRegs[1] << 8 & 0x300);
				bank_1k &= chr_mask_1k;
				addr = (bank_1k << 10) | (addr & 0x3FF);
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}


		public override byte ReadPRG(int addr)
		{
			int bank = addr >> 13;
			bank = prg_regs_8k[bank];
			return ROM[(bank << 13) + (addr & 0x1FFF)];
		}
	}
}
