using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper006 : NesBoardBase
	{
		private int _reg;

		private bool _irqEnable;
		private bool _irqPending;
		private int _irqCount;
		private const int IRQDESTINATION = 0x10000;

		private int _prgMask16k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER006":
					Cart.VramSize = 32;
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.PadH, Cart.PadV);
			_prgMask16k = Cart.PrgSize / 16 - 1;

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

		public override void WriteExp(int addr, byte value)
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

		public override void WritePrg(int addr, byte value)
		{
			_reg = value;
		}

		public override byte ReadPrg(int addr)
		{
			int bank = addr < 0x4000
				? (_reg >> 2) & 0x3F
				: 7;
			bank &= _prgMask16k;

			return Rom[(bank * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vram[((_reg & 3) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				Vram[((_reg & 3) * 0x2000) + (addr & 0x1FFF)] = value;
			}
			else
			{
				base.WritePpu(addr, value);
			}
		}

		public override void ClockCpu()
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
