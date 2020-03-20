using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class UNIF_BMC_Super24in1SC03 : MMC3Board_Base
	{
		private byte[] exRegs = new byte[3];
		private readonly int[] masko8 = { 63, 31, 15, 1, 3, 0, 0, 0 };
		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_BMC-Super24in1SC03":
					break;
				default:
					return false;
			}

			BaseSetup();

			exRegs[0] = 0x24;
			exRegs[1] = 159;
			exRegs[2] = 0;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(exRegs), ref exRegs, false);
		}

		public override void WriteExp(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1FF0:
					exRegs[0] = value; break;
				case 0x1FF1:
					exRegs[1] = value; break;
				case 0x1FF2:
					exRegs[2] = value; break;
			}

			base.WriteExp(addr, value);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = mmc3.prg_regs_8k[bank_8k];

			int NV= bank_8k & masko8[exRegs[0] & 7];
			NV |= (exRegs[1] << 1);

			return Rom[(NV << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if ((exRegs[0] & 0x20)>0)
				{
					return Vram[(bank_1k << 10) + (addr & 0x3FF)];
				}
				else
				{
					bank_1k = bank_1k | (exRegs[2] << 3);
					return Vrom[(bank_1k << 10) + (addr & 0x3FF)];
				}
			}
			else
				return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if ((exRegs[0] & 0x20) > 0)
				{
					Vram[(bank_1k << 10) + (addr & 0x3FF)]=value;
				}
				else
				{
					// don't write to VROM
				}
			}
			else
				base.WritePpu(addr, value);
		}
	}
}
