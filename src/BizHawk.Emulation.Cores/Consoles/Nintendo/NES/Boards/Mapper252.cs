using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	internal sealed class Mapper252 : NesBoardBase
	{
		private byte[] preg = new byte[2];
		private byte[] creg = new byte[8];

		private int _prgBankMask8K, _chrBankMask1K;
		private int _irqLatch, _irqClock, _irqCount;
		private bool _irqA;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER252":
					break;
				default:
					return false;
			}

			AssertPrg(256);
			AssertChr(128);
			AssertVram(2);
			AssertWram(8);
			_prgBankMask8K = Cart.PrgSize / 8 - 1;
			_chrBankMask1K = Cart.ChrSize - 1;
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(preg), ref preg, false);
			ser.Sync(nameof(creg), ref creg, false);
			ser.Sync(nameof(_irqLatch), ref _irqLatch);
			ser.Sync(nameof(_irqClock), ref _irqClock);
			ser.Sync(nameof(_irqCount), ref _irqCount);
			ser.Sync(nameof(_irqA), ref _irqA);
		}

		public override void ClockCpu()
		{
			if (_irqA)
			{
				_irqClock += 3;
				if (_irqClock >= 341)
				{
					_irqClock -= 341;
					_irqCount++;
					if (_irqCount==0x100)
					{
						IrqSignal=true;
						_irqCount = _irqLatch;
					}
				}
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			WriteReg((addr + 0x8000), value);
		}

		public void WriteReg(int addr, byte value)
		{
			if (addr >= 0xB000 && addr < 0xF000)
			{
				var ind = ((((addr & 8) | (addr >> 8)) >> 3) + 2) & 7;
				var sar = addr & 4;
				creg[ind] = (byte)((creg[ind] & (0xF0 >> sar)) | ((value & 0x0F) << sar));
			}
			else
			{
				switch (addr & 0xF00C)
				{
					case 0x8000:
					case 0x8004:
					case 0x8008:
					case 0x800C:
						preg[0] = value;
						break;

					case 0xA000:
					case 0xA004:
					case 0xA008:
					case 0xA00C:
						preg[1] = value;
						break;

					case 0xF000: IrqSignal = false; _irqLatch &= 0xF0; _irqLatch |= value & 0xF; break;
					case 0xF004: IrqSignal = false; _irqLatch &= 0x0F; _irqLatch |= value << 4; break;
					case 0xF008: IrqSignal = false; _irqClock = 0; _irqCount = _irqLatch; _irqA = value.Bit(1); break;

				}
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank;

			if (addr < 0x2000)
			{
				bank = preg[0] & _prgBankMask8K;
			}
			else if (addr < 0x4000)
			{
				bank = preg[1] & _prgBankMask8K;
			}
			else if (addr < 0x6000)
			{
				bank = _prgBankMask8K - 1;
			}
			else
			{
				bank = _prgBankMask8K;
			}

			return Rom[(bank << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int x = (addr >> 10) & 7;

				int bank;
				if (creg[x] == 6 || creg[x] == 7)
				{
					bank = creg[x] & 1;
					return Vram[(bank << 10) + (addr & 0x3FF)];
				}

				bank = (creg[x] & _chrBankMask1K) << 10;
				return Vrom[bank + (addr & 0x3FF)];
			}

			return base.ReadPpu(addr);
		}
		
		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (Vram != null)
					Vram[addr&0x7FF] = value;
			}
			else
			{
				NES.CIRAM[ApplyMirroring(addr)] = value;
			}
		}
	}
}
