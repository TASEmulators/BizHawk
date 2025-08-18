﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper045 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_045

		private int chr_bank_mask_2k;
		private int prg_bank_mask_8k;
		private int cur_reg = 0;
		private byte[] regs = new byte[4];
		private bool lock_regs = false;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER045":
				case "UNIF_BMC-SuperHIK8in1":
					break;
				default:
					return false;
			}

			chr_bank_mask_2k = Cart.ChrSize / 2 - 1;
			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(regs), ref regs, false);
			ser.Sync(nameof(lock_regs), ref lock_regs);
			base.SyncState(ser);
		}

		public override void WriteWram(int addr, byte value)
		{
			if (lock_regs)
			{
				base.WriteWram(addr, value);
			}
			else
			{
				regs[cur_reg] = value;
				IncrementCounter();
				Sync45();
			}
		}

		private void Sync45()
		{
			lock_regs = regs[3].Bit(6);
		}

		private void IncrementCounter()
		{
			if (cur_reg == 3)
			{
				cur_reg = 0;
			}
			else
			{
				cur_reg++;
			}
		}

		public override void NesSoftReset()
		{
			lock_regs = false;
			cur_reg = 0;
			regs = new byte[4];
			base.NesSoftReset();
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= (regs[3] & 0x3F) ^ 0x3F;
			bank_8k |= regs[1];
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return Rom[addr];
		}

		private int CHR_AND()
		{

			if (regs[2]==0)
			{
				return 0xFF;
			}
			return (0xFF >> ~((regs[2] & 0x0F)|0xF0));
		}

		private int CHR_OR()
		{
			int temp = regs[2] >> 4;
			temp <<= 8;

			int x = regs[0] | (temp);
			return x;
		}

		protected override int MapCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);

			bank_1k &= CHR_AND();
			bank_1k |= CHR_OR();
			bank_1k &= chr_mask;
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}
	}
}
