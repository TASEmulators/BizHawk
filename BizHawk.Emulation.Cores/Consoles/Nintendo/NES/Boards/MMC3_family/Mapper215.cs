using BizHawk.Common;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper215 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(4);

		public ByteBuffer prg_regs_8k = new ByteBuffer(4);

		private bool is_mk3;

		private int prg_mask_8k, chr_mask_1k;

		private byte[] regs_sec = { 0, 2, 5, 3, 6, 1, 7, 4 }; 

		/*
		 *  I'm not sure where these matrices originated from, but they don't seem to be needed
		 *  so let's leave them as commented out in case a need arises
		private readonly byte[,] regperm = new byte[,]
		{
			{ 0, 1, 2, 3, 4, 5, 6, 7 },
			{ 0, 2, 6, 1, 7, 3, 4, 5 },
			{ 0, 5, 4, 1, 7, 2, 6, 3 },   // unused
			{ 0, 6, 3, 7, 5, 2, 4, 1 },
			{ 0, 2, 5, 3, 6, 1, 7, 4 },   // only one actually used?
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
		};

		private readonly byte[,] adrperm = new byte[,]
		{
			{ 0, 1, 2, 3, 4, 5, 6, 7 },
			{ 3, 2, 0, 4, 1, 5, 6, 7 },
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // unused
			{ 5, 0, 1, 2, 3, 7, 6, 4 },
			{ 3, 1, 0, 5, 2, 4, 6, 7 },	  // only one actully used?
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
		};
		*/

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER215":
					break;
				case "MK3E60":
					is_mk3 = true;
					break;
				default:
					return false;
			}

			BaseSetup();
			exRegs[0] = 0;
			exRegs[1] = 0xFF;
			exRegs[2] = 4;
			exRegs[3] = 0;

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
			ser.Sync("is_mk3", ref is_mk3);
			ser.Sync("ptg_regs_8k", ref prg_regs_8k);
			ser.Sync("prg_mask", ref prg_mask_8k);
			ser.Sync("chr_mask", ref chr_mask_1k);
		}

		public void sync_prg(int i)
		{
			if ((exRegs[0] & 0x80) == 0)
			{
				int temp = 0;
				//for (int i=0;i<4;i++)
				//{
					temp = mmc3.prg_regs_8k[i];

					if ((exRegs[1] & 0x8) > 0)
						temp = (temp & 0x1F) | 0x20;
					else
						temp = ((temp & 0x0F) | (exRegs[1] & 0x10));

					prg_regs_8k[i] = (byte)(temp & prg_mask_8k);
				//}
			}
		}

		public void sync_prg_2()
		{
			if ((exRegs[0] & 0x80) > 0)
			{
				int bank = (exRegs[0] & 0x0F) | (exRegs[1] & 0x10);
				bank *= 2;
				bank &= prg_mask_8k;

				prg_regs_8k[0] = (byte)bank;
				prg_regs_8k[1] = (byte)(bank+1);
				prg_regs_8k[2] = (byte)bank;
				prg_regs_8k[3] = (byte)(bank+1);
			}
			else
			{
				sync_prg(0);
				sync_prg(1);
				sync_prg(2);
				sync_prg(3);
			}
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x1000) { exRegs[0] = value; sync_prg_2(); }
			if (addr == 0x1001) { exRegs[1] = value; }
			if (addr == 0x1007) { exRegs[2] = value; mmc3.reg_addr = 0; sync_prg_2(); }

			base.WriteEXP(addr, value);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (!is_mk3)
			{
				if (addr == 0x0000) { exRegs[0] = value; sync_prg_2(); }
				if (addr == 0x0001) { exRegs[1] = value; }
				if (addr == 0x0007) { exRegs[2] = value; mmc3.reg_addr = 0; sync_prg_2(); }
			}
			
			base.WriteWRAM(addr, value);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			switch (addr &= 0xE001)
			{
				case 0x8000:
					if (exRegs[2]==0)
					{
						base.WritePRG(0x0000, value);
						sync_prg(0);
						sync_prg(2);
					}
					break;
				case 0x8001:
					if (exRegs[2]>0)
					{
						if ((exRegs[3]>0) && ((exRegs[0] & 0x80) == 0 || (mmc3.reg_addr & 0x7) < 6))
						{
							exRegs[3] = 0;
							base.WritePRG(0x0001, value);
							if (mmc3.reg_addr==7)
								sync_prg(1);
							else if ((mmc3.reg_addr==6) && mmc3.prg_mode)
								sync_prg(2);
							else if ((mmc3.reg_addr == 6) && !mmc3.prg_mode)
								sync_prg(0);
						}
					}
					else
					{
						base.WritePRG(0x0001, value);
						if (mmc3.reg_addr == 7)
							sync_prg(1);
						else if ((mmc3.reg_addr == 6) && mmc3.prg_mode)
							sync_prg(2);
						else if ((mmc3.reg_addr == 6) && !mmc3.prg_mode)
							sync_prg(0);
					}
					break;
				case 0xA000:
					if (exRegs[2]>0)
					{
						value = (byte)((value & 0xC0) | regs_sec[value & 0x07]);
						exRegs[3] = 1;

						base.WritePRG(0x0000, value);
						sync_prg(0);
						sync_prg(2);
					}
					else
					{
						if (value==0)
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
					base.WritePRG(0x4001, value);
					break;
				case 0xC000:
					if (exRegs[2]>0)
						if ((value >> 7 | value) == 0)
						{
							SetMirrorType(EMirrorType.Vertical);
						}
						else
						{
							SetMirrorType(EMirrorType.Horizontal);
						}
					else
						base.WritePRG(0x4000, value);
					break;
				case 0xC001:
					if (exRegs[2]>0)
						base.WritePRG(0x6001, value);
					else
						base.WritePRG(0x4001, value);
					break;
				case 0xE000:
					base.WritePRG(0x6000, value);
					break;
				case 0xE001:
					if (exRegs[2]>0)
					{
						base.WritePRG(0x4000, value);
						base.WritePRG(0x4001, value);
					}
					else
					{
						base.WritePRG(0x6001, value);
					}
					break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = base.Get_CHRBank_1K(addr);

				if ((exRegs[1] & 0x4) > 0)
					bank_1k = (bank_1k | 0x100);
				else
					bank_1k = (bank_1k & 0x7F) | (exRegs[1] << 3 & 0x80);

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
