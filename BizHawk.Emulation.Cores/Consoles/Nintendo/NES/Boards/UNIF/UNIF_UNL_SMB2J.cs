using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_SMB2J : NES.NESBoardBase
	{
		int prg = 0;
		int prg_count;
		int irqcnt = 0;
		bool irqenable = false;
		bool swap;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-SMB2J":
					break;
				default:
					return false;
			}

			prg_count = Cart.prg_size/4;

			Cart.wram_size = 0;
			// not sure on initial mirroring
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr += 0x4000;

			switch (addr)
			{
				case 0x4022:
					if (ROM.Length > 0x10000) { prg = (value & 0x01) << 2; }					
					break;

				case 0x4122:
					irqenable = (value & 3) > 0;
					IRQSignal = false;
					irqcnt = 0;
					break;
			}
		}

		public override byte ReadEXP(int addr)
		{
			if (addr > 0x1000)
			{
				return ROM[(addr - 0x1000) + (prg_count - 3) * 0x1000];
			}
			else return base.ReadEXP(addr);
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[addr + (prg_count - 2) * 0x1000];
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(addr + prg * 01000)];
		}

		public override void ClockCPU()
		{
			if (irqenable)
			{
				irqcnt++;

				if (irqcnt >= 4096)
				{
					irqenable = false;
					IRQSignal = true;
				}				
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("irqenable", ref irqenable);
			ser.Sync("irqcnt", ref irqcnt);
			ser.Sync("swap", ref swap);
		}
	}
}
