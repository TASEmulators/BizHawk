using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper253 : NesBoardBase
	{
		private byte[] _prg = new byte[2];
		private byte[] chrlo = new byte[8];
		private byte[] chrhi = new byte[8];
		private bool _vLock;
		private int _irqLatch, _irqClock, _irqCount;
		private bool _irqA;

		private int _prgBankMask8K, _chrBankMask1k;

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER253":
					break;
				default:
					return false;
			}

			_prgBankMask8K = Cart.PrgSize / 8 - 1;
			_chrBankMask1k = Cart.ChrSize - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("preg", ref _prg, false);
			ser.Sync(nameof(chrlo), ref chrlo, false);
			ser.Sync(nameof(chrhi), ref chrhi, false);
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
					if (_irqCount == 0x100)
					{
						IrqSignal = true;
						_irqCount = _irqLatch;
					}
				}
			}
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			if ((addr >= 0xB000) && (addr <= 0xE00C))
			{
				var ind = ((((addr & 8) | (addr >> 8)) >> 3) + 2) & 7;
				var sar = addr & 4;
				var clo = (chrlo[ind] & (0xF0 >> sar)) | ((value & 0x0F) << sar);
				chrlo[ind] = (byte)clo;
				if (ind == 0)
				{
					if (clo == 0xc8)
						_vLock = false;
					else if (clo == 0x88)
						_vLock = true;
				}
				if (sar > 0)
					chrhi[ind] = (byte)(value >> 4);
			}
			else
			{
				switch (addr)
				{
					case 0x8010: _prg[0] = value; break;
					case 0xA010: _prg[1] = value; break;
					case 0x9400: SetMirroring(value); break;

					case 0xF000: IrqSignal = false; _irqLatch &= 0xF0; _irqLatch |= value & 0xF; break;
					case 0xF004: IrqSignal = false; _irqLatch &= 0x0F; _irqLatch |= value << 4; break;
					case 0xF008: IrqSignal = false; _irqClock = 0; _irqCount = _irqLatch; _irqA = value.Bit(1); break;
				}
			}
		}

		private void SetMirroring(int mirr)
		{
			switch(mirr & 3)
			{
				case 0: SetMirrorType(EMirrorType.Vertical); break;
				case 1: SetMirrorType(EMirrorType.Horizontal); break;
				case 2: SetMirrorType(EMirrorType.OneScreenA); break;
				case 3: SetMirrorType(EMirrorType.OneScreenB); break;
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank;

			if (addr < 0x2000)
			{
				bank = _prg[0] & _prgBankMask8K;
			}
			else if (addr < 0x4000)
			{
				bank = _prg[1] & _prgBankMask8K;
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
				var chr = chrlo[x] | (chrhi[x] << 8);
				int bank = (chr & _chrBankMask1k) << 10;

				if ((chrlo[x] == 4 || chrlo[x] == 5) && !_vLock)
				{
					bank = chr & 1;
					return Vram[(bank << 10) + (addr & 0x3FF)];
				}
				else
				{
					return Vrom[bank + (addr & 0x3FF)];
				}

			}

			return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (Vram != null)
				{ 
					int x = (addr >> 10) & 7;
					var chr = chrlo[x] | (chrhi[x] << 8);
					int bank = (chr & _chrBankMask1k) << 10;

					if ((chrlo[x] == 4 || chrlo[x] == 5) && !_vLock)
					{
						bank = chr & 1;
						Vram[(bank << 10) + (addr & 0x3FF)]=value;
					}
				}
			}
			else
			{
				NES.CIRAM[ApplyMirroring(addr)] = value;
			}
		}
	}
}
