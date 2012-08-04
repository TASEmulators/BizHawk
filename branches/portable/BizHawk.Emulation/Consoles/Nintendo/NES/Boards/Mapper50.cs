using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper50 : NES.NESBoardBase
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_050

		byte prg_bank;
		int prg_bank_mask_8k;
		bool irq_enable;
		ushort irq_counter = 0;
		bool irq_ready = false;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER050":
					break;
				default:
					return false;
			}
			prg_bank = 0;
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_bank", ref prg_bank);
			ser.Sync("irq_enable", ref irq_enable);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_ready", ref irq_ready);
			base.SyncState(ser);
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr &= 0x0120;
			if (addr == 0x0020)
			{
				prg_bank = (byte)(((value & 1) << 2) | ((value & 2) >> 1) | ((value & 4) >> 1) | (value & 8));
			}
			else if (addr == 0x0120)
			{
				irq_enable = value.Bit(0);
				if (!irq_enable)
				{
					SyncIRQ(irq_enable);
					irq_counter = 0;
				}
			}
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x2000)
			{
				return ROM[(0x08 * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0x4000)
			{
				return ROM[(0x09 * 0x2000) + (addr & 0x1FFF)];
			}
			else if (addr < 0x6000)
			{
				int bank = (prg_bank & prg_bank_mask_8k);
				return ROM[(bank * 0x2000) + (addr & 0x1FFF)];
			}
			else
			{
				return ROM[(0x0B * 0x2000) + (addr & 0x1FFF)];
			}
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[(0x0F * 0x2000) + (addr & 0x1FFF)];
		}

		private void IRQ_Ready()
		{
			base.SyncIRQ(irq_ready);
		}

		private void IRQ_Tick()
		{
			if (irq_enable)
			{
				if (irq_counter < 0x0FFF)
				{
					irq_counter += 1;
					irq_ready = false;
				}
				else
				{
					irq_counter = 0;
					irq_ready = true;
					IRQ_Ready();
				}
			}
			
		}

		public override void ClockPPU()
		{
			IRQ_Tick();
			base.ClockPPU();
		}
	}
}
