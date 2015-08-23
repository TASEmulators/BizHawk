using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper006 : NES.NESBoardBase
	{
		private int _reg;

		private bool _irqEnable;
		private bool _irqPending;
		private int _irqCount;
		private const int IRQDESTINATION = 0x10000;

		private int _prgMask16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER006":
					Cart.vram_size = 32;
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);
			_prgMask16k = Cart.prg_size / 16 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);

			ser.Sync("irqEnable", ref _irqEnable);
			ser.Sync("irqPending", ref _irqPending);
			ser.Sync("irqCount", ref _irqCount);
		}

		public override void WriteEXP(int addr, byte value)
		{
			// Mirroring
			if (addr == 0x2FE || addr == 0x2FF)
			{
				int mirr = ((addr << 1) & 2) | ((value >> 4) & 1);
				switch (mirr)
				{
					case 0:
						SetMirrorType(EMirrorType.OneScreenA);
						break;
					case 1:
						SetMirrorType(EMirrorType.OneScreenB);
						break;
					case 2:
						SetMirrorType(EMirrorType.Vertical);
						break;
					case 3:
						SetMirrorType(EMirrorType.Horizontal);
						break;
				}
			}

			// IRQ
			else if (addr >= 0x500 && addr <= 0x503)
			{
				switch (addr)
				{
					case 0x501:
						_irqEnable = false;
						break;
					case 0x502:
						_irqCount &= 0xFF00;
						_irqCount |= value;
						break;
					case 0x503:
						_irqCount &= 0x00FF;
						_irqCount |= value << 8;
						_irqEnable = true;
						break;

				}

				SyncIRQ();
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = value;
		}

		public override byte ReadPRG(int addr)
		{
			int bank = addr < 0x4000
				? (_reg >> 2) & 0x3F
				: 7;
			bank &= _prgMask16k;

			return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VRAM[((_reg & 3) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[((_reg & 3) * 0x2000) + (addr & 0x1FFF)] = value;
			}
			else
			{
				base.WritePPU(addr, value);
			}
		}

		public override void ClockCPU()
		{
			if (_irqEnable)
			{
				ClockIRQ();
			}
		}

		private void ClockIRQ()
		{
			_irqCount++;
			if (_irqCount >= IRQDESTINATION)
			{
				_irqEnable = false;
				_irqPending = true;
			}

			SyncIRQ();
		}

		private void SyncIRQ()
		{
			SyncIRQ(_irqPending);
		}
	}
}
