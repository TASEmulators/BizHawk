using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_BMC_Super24in1SC03 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(3);
		private readonly int[] masko8 = { 63, 31, 15, 1, 3, 0, 0, 0 };
		public override bool Configure(NES.EDetectionOrigin origin)
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

		public override void WriteEXP(int addr, byte value)
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

			base.WriteEXP(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = mmc3.prg_regs_8k[bank_8k];

			int NV= bank_8k & masko8[exRegs[0] & 7];
			NV |= (exRegs[1] << 1);

			return ROM[(NV << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if ((exRegs[0] & 0x20)>0)
				{
					return VRAM[(bank_1k << 10) + (addr & 0x3FF)];
				}
				else
				{
					bank_1k = bank_1k | (exRegs[2] << 3);
					return VROM[(bank_1k << 10) + (addr & 0x3FF)];
				}
			}
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if ((exRegs[0] & 0x20) > 0)
				{
					VRAM[(bank_1k << 10) + (addr & 0x3FF)]=value;
				}
				else
				{
					// dont write to VROM
				}
			}
			else
				base.WritePPU(addr, value);
		}
	}
}
