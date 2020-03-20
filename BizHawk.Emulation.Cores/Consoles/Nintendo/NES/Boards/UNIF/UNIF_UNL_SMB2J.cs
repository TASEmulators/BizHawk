using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_SMB2J : NesBoardBase
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

		public override void WriteExp(int addr, byte value)
		{
			addr += 0x4000;

			switch (addr)
			{
				case 0x4022:
					if (Rom.Length > 0x10000) { prg = (value & 0x01) << 2; }					
					break;

				case 0x4122:
					irqenable = (value & 3) > 0;
					IrqSignal = false;
					irqcnt = 0;
					break;
			}
		}

		public override byte ReadExp(int addr)
		{
			if (addr > 0x1000)
			{
				return Rom[(addr - 0x1000) + (prg_count - 3) * 0x1000];
			}
			else return base.ReadExp(addr);
		}

		public override byte ReadWram(int addr)
		{
			return Rom[addr + (prg_count - 2) * 0x1000];
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[(addr + prg * 01000)];
		}

		public override void ClockCpu()
		{
			if (irqenable)
			{
				irqcnt++;

				if (irqcnt >= 4096)
				{
					irqenable = false;
					IrqSignal = true;
				}				
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
			ser.Sync(nameof(irqenable), ref irqenable);
			ser.Sync(nameof(irqcnt), ref irqcnt);
			ser.Sync(nameof(swap), ref swap);
		}
	}
}
