using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper142 : NesBoardBase
	{
		private byte[] _reg = new byte[8];
		private byte _cmd;
		private int _lastBank;

		private bool _isIrqUsed;
		private byte _irqA;
		private int _irqCount;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER142":
				case "UNIF_UNL-KS7032":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Horizontal);
			_lastBank = Cart.PrgSize / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(_reg), ref _reg, false);
			ser.Sync(nameof(_cmd), ref _cmd);
			ser.Sync(nameof(_lastBank), ref _lastBank);
			ser.Sync(nameof(_isIrqUsed), ref _isIrqUsed);
			ser.Sync(nameof(_irqA), ref _irqA);
			ser.Sync(nameof(_irqCount), ref _irqCount);
		}

		public override byte ReadWram(int addr)
		{
			return Rom[(_reg[4] << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPrg(int addr)
		{

			if (addr < 0x2000) { return Rom[(_reg[1] << 13) + (addr & 0x1FFF)]; }
			if (addr < 0x4000) { return Rom[(_reg[2] << 13) + (addr & 0x1FFF)]; }
			if (addr < 0x6000) { return Rom[(_reg[3] << 13) + (addr & 0x1FFF)]; }

			return Rom[(_lastBank << 13) + (addr & 0x1FFF)];
		}

		public override void WriteExp(int addr, byte value)
		{
			Write(addr + 0x4000, value);
		}

		public override void WriteWram(int addr, byte value)
		{
			Write(addr + 0x6000, value);
		}

		public override void WritePrg(int addr, byte value)
		{
			Write(addr + 0x8000, value);
		}

		private void IRQHook(int a)
		{
			if (_irqA > 0)
			{
				_irqCount += a;
				if (_irqCount >= 0xFFFF)
				{
					_irqA = 0;
					_irqCount = 0;

					IrqSignal = true;
				}
			}
		}

		public override void ClockPpu()
		{
			IRQHook(1);
		}

		private void Write(int addr, byte value)
		{
			switch (addr & 0xF000)
			{
				case 0x8000:
					IrqSignal = false;
					_irqCount = (_irqCount & 0x000F) | (value & 0x0F);
					_isIrqUsed = true;
					break;
				case 0x9000:
					IrqSignal = false;
					_irqCount = (_irqCount & 0x00F0) | ((value & 0x0F) << 4);
					_isIrqUsed = true;
					break;
				case 0xA000:
					IrqSignal = false;
					_irqCount = (_irqCount & 0x0F00) | ((value & 0x0F) << 8);
					_isIrqUsed = true;
					break;
				case 0xB000:
					IrqSignal = false;
					_irqCount = (_irqCount & 0xF000) | (value << 12);
					_isIrqUsed = true;
					break;
				case 0xC000:
					if (_isIrqUsed)
					{
						IrqSignal = false;
						_irqA = 1;
					}
					break;

				case 0xE000:
					_cmd = (byte)(value & 7);
					break;
				case 0xF000:
					_reg[_cmd] = value;
					break;
			}
		}
	}
}
