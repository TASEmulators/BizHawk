using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Mapper 83 seems to be a hacky mess that represents 3 different Cony cartridges
	// http://problemkaputt.de/everynes.htm#mapper83cony
	internal sealed class ConyA : NesBoardBase
	{
		private byte[] prg_regs = new byte[4];
		private byte[] _low = new byte[4]; // some kind of security feature?
		private byte[] chr_regs = new byte[8];

		private int prg_bank_mask_16k, prg_bank_mask_8k, chr_bank_mask_2k;
		private int IRQCount;
		private bool IRQa;
		private byte bank, mode;
		private bool is_2k_bank, is_not_2k_bank;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER083":
					if (Cart.PrgSize == 128)
					{
						prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
						prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
						chr_bank_mask_2k = 127;
						//prg_regs[0] = 0xC;
						//prg_regs[1] = 0xB;
						//prg_regs[2] = 0xE;
						//prg_regs[3] = 0xF;
						return true;
					}
					return false;
				default:
					return false;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_regs), ref prg_regs, false);
			ser.Sync(nameof(chr_regs), ref chr_regs, false);
			ser.Sync(nameof(IRQCount), ref IRQCount);
			ser.Sync(nameof(IRQa), ref IRQa);
			ser.Sync(nameof(bank), ref bank);
			ser.Sync(nameof(mode), ref mode);
			ser.Sync(nameof(is_2k_bank), ref is_2k_bank);
			ser.Sync(nameof(is_not_2k_bank), ref is_not_2k_bank);
			ser.Sync(nameof(_low), ref _low, false);
		}

		public void Mirroring()
		{
			switch (mode & 3)
			{
				case 0: SetMirrorType(EMirrorType.Vertical); break;
				case 1: SetMirrorType(EMirrorType.Horizontal); break;
				case 2: SetMirrorType(EMirrorType.OneScreenA); break;
				case 3: SetMirrorType(EMirrorType.OneScreenB); break;
			}
		}
		public override void WritePrg(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0000: is_2k_bank = true; bank = value; mode |= 0x40; break;
				case 0x3000:
				case 0x30FF:
				case 0x31FF:
					bank = value;
					mode |= 0x40;
					break;

				case 0x0100: mode = (byte)(value | (mode & 0x40)); break;

				case 0x0300: prg_regs[0] = value; mode &= 0xBF; break;
				case 0x0301: prg_regs[1] = value; mode &= 0xBF; break;
				case 0x0302: prg_regs[2] = value; mode &= 0xBF; break;

				// used in 1k CHR bank switching
				case 0x0312: chr_regs[2] = value; is_not_2k_bank = true; break;
				case 0x0313: chr_regs[3] = value; is_not_2k_bank = true; break;
				case 0x0314: chr_regs[4] = value; is_not_2k_bank = true; break;
				case 0x0315: chr_regs[5] = value; is_not_2k_bank = true; break;

				// used in 1k and 2k CHR bank switching
				case 0x0310: chr_regs[0] = value; break;
				case 0x0311: chr_regs[1] = value; break;
				case 0x0316: chr_regs[6] = value; break;
				case 0x0317: chr_regs[7] = value; break;

				case 0x0200:
					IRQCount &= 0xFF00; IRQCount |= value;
					IrqSignal = false;
					break;
				case 0x0201:
					IRQCount &= 0xFF;
					IRQCount |= value << 8;
					IRQa = mode.Bit(7);
					break;
			}

			Mirroring();
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (is_2k_bank && !is_not_2k_bank)
				{
					int index = (addr >> 11) & 0x3;
					int bank = chr_regs[index];

					// indexes are numbered oddly for different bank switching schemes
					if (index == 2)
						bank = chr_regs[6];
					if (index == 3)
						bank = chr_regs[7];

					bank &= chr_bank_mask_2k;
					return Vrom[(bank << 11) + (addr & 0x7FF)];
				}
				else
				{
					int index = (addr >> 10) & 0x7;
					int bank = chr_regs[index];
					bank |= ((bank & 0x30) << 4);
					bank &= 0xFF;
					return Vrom[(bank << 10) + (addr & 0x3FF)];
				}

			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			if ((mode & 0x40) > 0)
			{
				if (addr < 0x4000)
				{
					return Rom[(((bank & 0x7) & 0x3F) << 14) + (addr & 0x3FFF)];
				}
				return Rom[((((bank & 0x7) & 0x30) | 0x7) << 14) + (addr & 0x3FFF)];
			}
			else
			{
				int index = (addr >> 13) & 0x3;
				int bank = prg_regs[index];
				// last bank is fixed
				if (index == 3)
					bank = prg_bank_mask_8k;

				return Rom[(bank << 13) + (addr & 0x1FFF)];
			}
		}

		public override void ClockCpu()
		{
			if (IRQa)
			{
				IRQCount--;
				if (IRQCount == 0)
				{
					IRQCount = 0xFFFF;
					IrqSignal = true;
					IRQa = false;
				}
			}
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1100 && addr <= 0x1103)
				_low[addr & 0x3] = value;
			else
				base.WriteExp(addr, value);
		}

		public override byte ReadExp(int addr)
		{
			if (addr == 0x1000)
				return (byte)((NES.DB & 0xFC) | 0);
			else if (addr >= 0x1100 && addr <= 0x1103)
				return _low[addr & 0x3];
			else
				return base.ReadExp(addr);

		}
	}

	internal sealed class ConyB : NesBoardBase
	{
		private byte[] prg_regs = new byte[4];
		private byte[] _low = new byte[4]; // some kind of security feature?
		private byte[] chr_regs = new byte[8];

		private int prg_bank_mask_16k, prg_bank_mask_8k, chr_bank_mask_2k;
		private int IRQCount;
		private bool IRQa;
		private byte bank, mode;
		private bool is_2k_bank, is_not_2k_bank;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER083":
					if (Cart.PrgSize == 256)
					{
						prg_bank_mask_16k = Cart.PrgSize / 16 - 1;
						prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
						chr_bank_mask_2k = Cart.PrgSize / 2 - 1;

						//prg_regs[1] = (byte)prg_bank_mask_16k;
						//is_2k_bank = true;
						return true;
					}
					return false;
				default:
					return false;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_regs), ref prg_regs, false);
			ser.Sync(nameof(chr_regs), ref chr_regs, false);
			ser.Sync(nameof(IRQCount), ref IRQCount);
			ser.Sync(nameof(IRQa), ref IRQa);
			ser.Sync(nameof(bank), ref bank);
			ser.Sync(nameof(mode), ref mode);
			ser.Sync(nameof(is_2k_bank), ref is_2k_bank);
			ser.Sync(nameof(is_not_2k_bank), ref is_not_2k_bank);
			ser.Sync(nameof(_low), ref _low, false);
		}

		public void Mirroring()
		{
			switch (mode & 3)
			{
				case 0: SetMirrorType(EMirrorType.Vertical); break;
				case 1: SetMirrorType(EMirrorType.Horizontal); break;
				case 2: SetMirrorType(EMirrorType.OneScreenA); break;
				case 3: SetMirrorType(EMirrorType.OneScreenB); break;
			}
		}
		public override void WritePrg(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0000: is_2k_bank = true; bank = value; mode |= 0x40; break;
				case 0x3000:  
				case 0x30FF: 
				case 0x31FF:
					bank = value;
					mode |= 0x40;
					break;

				case 0x0100: mode = (byte)(value | (mode & 0x40)); break;

				case 0x0300: prg_regs[0] = value; mode &= 0xBF; break;
				case 0x0301: prg_regs[1] = value; mode &= 0xBF; break;
				case 0x0302: prg_regs[2] = value; mode &= 0xBF; break;

				// used in 1k CHR bank switching
				case 0x0312: chr_regs[2] = value; is_not_2k_bank = true; break;
				case 0x0313: chr_regs[3] = value; is_not_2k_bank = true; break;
				case 0x0314: chr_regs[4] = value; is_not_2k_bank = true; break;
				case 0x0315: chr_regs[5] = value; is_not_2k_bank = true; break;

				// used in 1k and 2k CHR bank switching
				case 0x0310: chr_regs[0] = value; break;
				case 0x0311: chr_regs[1] = value; break;
				case 0x0316: chr_regs[6] = value; break;
				case 0x0317: chr_regs[7] = value; break;

				case 0x0200:
					IRQCount &= 0xFF00; IRQCount |= value; 
					IrqSignal = false;
					break;
				case 0x0201:
					IRQCount &= 0xFF;
					IRQCount |= value << 8;
					IRQa = mode.Bit(7);
					break;
			}

			Mirroring();
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if (is_2k_bank && !is_not_2k_bank)
				{
					int index = (addr >> 11) & 0x3;
					int bank = chr_regs[index];

					// indexes are numbered oddly for different bank switching schemes
					if (index == 2)
						bank = chr_regs[6];
					if (index == 3)
						bank = chr_regs[7];

					return Vrom[(bank << 11) + (addr & 0x7FF)];
				} else
				{
					int index = (addr >> 10) & 0x7;
					int bank = chr_regs[index];
					bank |= ((bank & 0x30) << 4);
					return Vrom[(bank << 10) + (addr & 0x3FF)];
				}
				
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			if ((mode & 0x40)>0)
			{
				if (addr < 0x4000)
				{
					return Rom[((bank&0x3F) << 14) + (addr & 0x3FFF)];
				}

				return Rom[(((bank & 0x30) | 0xF) << 14) + (addr & 0x3FFF)];
			} else
			{
				int index = (addr >> 13) & 0x3;
				int bank = prg_regs[index];

				// last bank is fixed
				if (index == 3)
					bank = prg_bank_mask_8k;

				return Rom[(bank << 13) + (addr & 0x1FFF)];


			}
			
		}

		public override void ClockCpu()
		{
			if (IRQa)
			{
				IRQCount--;
				if (IRQCount==0)
				{
					IRQCount = 0xFFFF;
					IrqSignal = true;
					IRQa = false;
				}
			}
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1100 && addr <= 0x1103)
				_low[addr & 0x3] = value;
			else
				base.WriteExp(addr, value);
		}

		public override byte ReadExp(int addr)
		{
			if (addr == 0x1000)
				return (byte)((NES.DB & 0xFC) | 0);
			else if (addr >= 0x1100 && addr <= 0x1103)
				return _low[addr & 0x3];
			else
				return base.ReadExp(addr);

		}
	}

	internal sealed class ConyC : NesBoardBase
	{
		private byte[] prg_regs = new byte[2];
		private byte[] chr_regs = new byte[8];

		private int prg_bank_mask_16k;
		private int _irqCount;
		private bool _irqA, _irqEnable;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER083":
					// We need one of the Cony boards to throw an error on an unexpected cart size, so we picked this one
					if (Cart.PrgSize != 128 && Cart.PrgSize != 256 && Cart.PrgSize != 1024)
					{
						throw new InvalidOperationException("Unexpected prg size of " + Cart.PrgSize + " for Mapper 83");
					}

					if (Cart.PrgSize == 1024)
					{
						prg_bank_mask_16k = Cart.PrgSize / 16 - 1;

						prg_regs[1] = (byte)prg_bank_mask_16k;
						return true;
					}
					return false;
				default:
					return false;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr_regs), ref chr_regs, false);
			ser.Sync(nameof(prg_regs), ref prg_regs, false);
			ser.Sync(nameof(_irqCount), ref _irqCount);
			ser.Sync(nameof(_irqA), ref _irqA);
			ser.Sync(nameof(_irqEnable), ref _irqEnable);
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr == 0)
			{
				prg_regs[0] = (byte)(value & prg_bank_mask_16k);
			}

			else if (addr >= 0x310 && addr < 0x318)
			{
				chr_regs[addr & 0x7] = value;
			}

			else if (addr == 0x200)
			{
				_irqCount &= 0xFF00; _irqCount |= value;
				IrqSignal = false;
			}

			else if (addr == 0x201)
			{
				_irqCount &= 0xFF;
				_irqCount |= value << 8;
				_irqA = true;
			}
			else if (addr == 0x0100)
			{
				_irqEnable = value.Bit(7);
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int index = (addr >> 10) & 0x7;
				int bank = chr_regs[index];
				return Vrom[(bank << 10) + (addr & 0x3FF)];
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x4000)
			{
				return Rom[(prg_regs[0] << 14) + (addr & 0x3FFF)];
			}

			return Rom[(prg_regs[1] << 14) + (addr & 0x3FFF)];
		}

		public override void ClockCpu()
		{
			if (_irqA)
			{
				_irqCount--;
				if (_irqCount == 0)
				{
					_irqCount = 0xFFFF;
					IrqSignal = _irqEnable;
				}
			}
		}
	}
}
