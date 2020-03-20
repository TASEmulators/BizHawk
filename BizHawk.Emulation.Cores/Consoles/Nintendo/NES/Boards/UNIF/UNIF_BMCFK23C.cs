using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_BMC_FK23C : MMC3Board_Base
	{
		private byte[] exRegs = new byte[8];
		private int[] _chrRegs1K = new int[8];
		public int[] prg_regs_8k = new int[4];

		[MapperProp]
		public bool dip_switch;

		[MapperProp]
		public byte dip_switch_setting;

		private int unromChr;

		private int prg_mask_8k, chr_mask_1k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-FK23C":
				case "MAPPER176":
					// http://wiki.nesdev.com/w/index.php/INES_Mapper_176
					// Mapper 176 was originally used for some Waixing boards, but goodNES 3.23 seems to go with CaH4e3's opinion that this mapper is FK23C
					// We will default 176 to FK23C, and route traditional Waixing boards to WAIXINGMAPPER176 via the Game Database
					break;
				default:
					return false;
			}

			exRegs[0] = 0; // xFF;
			exRegs[1] = 0; // xFF;
			exRegs[2] = 0; // xFF;
			exRegs[3] = 0; // x FF;
			exRegs[4] = 0xFF;
			exRegs[5] = 0xFF;
			exRegs[6] = 0xFF;
			exRegs[7] = 0xFF;

			BaseSetup();

			prg_mask_8k = Cart.prg_size / 8 - 1;
			chr_mask_1k = Cart.chr_size - 1;

			prg_regs_8k[0] = 0;
			prg_regs_8k[1] = 1;
			prg_regs_8k[2] = (byte)(0xFE & prg_mask_8k);
			prg_regs_8k[3] = (byte)(0xFF & prg_mask_8k);

			byte r0_0 = (byte)(mmc3.regs[0] & ~1);
			byte r0_1 = (byte)(mmc3.regs[0] | 1);
			byte r1_0 = (byte)(mmc3.regs[1] & ~1);
			byte r1_1 = (byte)(mmc3.regs[1] | 1);

			_chrRegs1K[0] = r0_0;
			_chrRegs1K[1] = r0_1;
			_chrRegs1K[2] = r1_0;
			_chrRegs1K[3] = r1_1;
			_chrRegs1K[4] = mmc3.regs[2];
			_chrRegs1K[5] = mmc3.regs[3];
			_chrRegs1K[6] = mmc3.regs[4];
			_chrRegs1K[7] = mmc3.regs[5];

			UpdateChr_2();
			UpdatePrg_2();

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(exRegs), ref exRegs, false);
			ser.Sync(nameof(_chrRegs1K), ref _chrRegs1K, false);
			ser.Sync(nameof(prg_regs_8k), ref prg_regs_8k, false);
			ser.Sync(nameof(prg_mask), ref prg_mask_8k);
			ser.Sync(nameof(chr_mask), ref chr_mask_1k);
			ser.Sync(nameof(dip_switch), ref dip_switch);
			ser.Sync(nameof(dip_switch_setting), ref dip_switch_setting);
		}

		void UpdateChr() 
		{
			for (int i = 0; i < 8; i++)
			{
				int bank = mmc3.chr_regs_1k[i];
				if (((exRegs[0] & 0x40) == 0) && (((exRegs[3] & 0x2) == 0) || (i != 1 && i != 3)))
					_chrRegs1K[i] = ((exRegs[2] & 0x7F) << 3 | bank );
			}
		}

		void UpdateChr_2()
		{
			if ((exRegs[0] & 0x40) > 0)
			{
				int bank = (exRegs[2] | unromChr);
				_chrRegs1K[0] = bank;
				_chrRegs1K[1] = bank;//(bank + 1);
				_chrRegs1K[2] = bank;//(bank + 2);
				_chrRegs1K[3] = bank;//(bank + 3);
				_chrRegs1K[4] = bank;//(bank + 4);
				_chrRegs1K[5] = bank;//(bank + 5);
				_chrRegs1K[6] = bank;//(bank + 6);
				_chrRegs1K[7] = bank;//(bank + 7);
			}
			else
			{
				if ((exRegs[3] & 0x2) > 0)
				{
					int bank = (exRegs[2] & 0x7F) << 3;
					_chrRegs1K[1] = (bank | exRegs[6]);
					_chrRegs1K[3] = (bank | exRegs[7]);
				}
				UpdateChr();
			}
		}

		void UpdatePrg()
		{
			for (int i = 0; i < 4; i++)
			{
				int bank = mmc3.prg_regs_8k[i];

				uint check = (uint)(exRegs[0] & 0x7) - 3;

				if ((check > 1) && (((exRegs[3] & 0x2) == 0) || (i < 2)))
				{
					if ((exRegs[0] & 0x3) > 0)
						bank = (bank & (0x3F >> (exRegs[0] & 0x3))) | (exRegs[1] << 1);

					prg_regs_8k[i] = (byte)(bank & prg_mask_8k);
				}
			}
		}

		void UpdatePrg_2()
		{
			if ((exRegs[0] & 0x7) == 4)
			{
				int bank = exRegs[1]>>1;
				bank *= 4;
				bank &= prg_mask_8k;

				prg_regs_8k[0] = (byte)bank;
				prg_regs_8k[1] = (byte)(bank + 1);
				prg_regs_8k[2] = (byte)(bank + 2);
				prg_regs_8k[3] = (byte)(bank + 3);
			}
			else if ((exRegs[0] & 0x7) == 3)
			{
				int bank = exRegs[1];
				bank *= 2;
				bank &= prg_mask_8k;

				prg_regs_8k[0] = (byte)bank;
				prg_regs_8k[1] = (byte)(bank + 1);
				prg_regs_8k[2] = (byte)bank;
				prg_regs_8k[3] = (byte)(bank + 1);
			}
			else
			{
				if ((exRegs[3] & 0x2)>0)
				{
					prg_regs_8k[2] = (byte)(exRegs[4]);
					prg_regs_8k[3] = (byte)(exRegs[5]);
				}
				UpdatePrg();
			}
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr>0x1000)
			{
				addr += 0x4000;
				if ((addr & (1 << ((dip_switch ? dip_switch_setting : 0) + 4)))>0)
				{
					exRegs[addr & 0x3] = value;
					UpdateChr_2();
					UpdatePrg_2();
				}
			}
			else
				base.WriteExp(addr, value);
		}

		public override void WritePrg(int addr, byte value)
		{
			if ((exRegs[0] & 0x40)>0)
			{
				unromChr = ((exRegs[0] & 0x30)>0) ? 0x0 : (value & 0x3);
				UpdateChr_2();
			}
			else switch ((addr+0x8000) & 0xE001)
				{
					case 0x8000: base.WritePrg(addr,value); UpdatePrg(); UpdateChr(); break;
					case 0x8001:

						if (((exRegs[3] << 2) & (mmc3.cmd & 0x8))>0)
						{
							exRegs[4 | mmc3.cmd & 0x3] = value;

							UpdatePrg_2();
							UpdateChr_2();
						}
						else
						{
							base.WritePrg(addr, value);
							UpdatePrg();
							UpdateChr();
						}
						break;

					case 0xA000:
						if (value == 0)
						{
							SetMirrorType(EMirrorType.Vertical);
						}
						else
						{
							SetMirrorType(EMirrorType.Horizontal);
						}
						break;
					case 0xA001: base.WritePrg(addr, value); break;
					case 0xC000: base.WritePrg(addr, value); break;
					case 0xC001: base.WritePrg(addr, value); break;
					case 0xE000: base.WritePrg(addr, value); break;
					case 0xE001: base.WritePrg(addr, value); break;

				}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				bank_1k = _chrRegs1K[bank_1k];
				
				if ((exRegs[0] & 0x40) > 0)
					addr = (bank_1k << 13) | (addr & 0x1FFF);
				else
					addr = (bank_1k << 10) | (addr & 0x3FF);

				return Vrom[addr];
			}
			else return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			int bank = addr >> 13;
			bank = prg_regs_8k[bank];
			return Rom[(bank << 13) + (addr & 0x1FFF)];
		}
	}
}
