using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Mapper 83 seems to be a hacky mess that represents 3 different Cony cartridges
	// http://problemkaputt.de/everynes.htm#mapper83cony
	public class ConyA : NES.NESBoardBase
	{
		private ByteBuffer chr_regs = new ByteBuffer(8);
		private ByteBuffer prg_regs = new ByteBuffer(4);

		private int prg_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER083":
					if (Cart.prg_size == 128)
					{
						prg_mask_8k = Cart.prg_size / 8 - 1;

						prg_regs[0] = 0xC;
						prg_regs[1] = 0xB;
						prg_regs[2] = 0xE;
						prg_regs[3] = 0xF;
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
			ser.Sync("chr_regs", ref chr_regs);
			ser.Sync("prg_regs", ref prg_regs);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr == 0x100)
			{
				// TODO: irq
			}

			if (addr == 0x200)
			{
				// TODO: irq
			}

			if (addr == 0x201)
			{
				// TODO: irq
			}

			if (addr >= 0x300 && addr <= 0x302)
			{
				prg_regs[addr & 0x3] = value;
			}

			if (addr >= 0x310 && addr < 0x318)
			{
				chr_regs[addr & 0x7] = value;
			}

			/* TODO
			B000h Select 256K ROM/ VROM Windows(upper two address bits)
				Bit0 - 3  Unknown
				  Bit4,6  Bit0 of 256K Block Number
				  Bit5,7  Bit1 of 256K Block Number
				  Used values are 00h,50h,A0h,F0h.Other values could probably select
				 separate 256K banks for ROM / VROM.The ROM selection also affects
 
				 the "fixed" 16K at C000h - FFFFh(last bank in current 256K block).
			   B0FFh  Probably same as B000h
			B1FFh  Probably same as B000h
			*/
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int index = (addr >> 10) & 0x7;
				int bank = chr_regs[index];
				return VROM[(bank << 10) + (addr & 0x3FF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int index = (addr >> 13) & 0x3;
			int bank = prg_regs[index] & prg_mask_8k;
			return ROM[(bank << 13) + (addr & 0x1FFF)];
		}
	}

	public class ConyB : NES.NESBoardBase
	{
		private ByteBuffer prg_regs = new ByteBuffer(2);
		private ByteBuffer chr_regs = new ByteBuffer(4);

		private int prg_bank_mask_16k, chr_bank_mask_2k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER083":
					if (Cart.prg_size == 256)
					{
						prg_bank_mask_16k = Cart.prg_size / 16 - 1;
						chr_bank_mask_2k = Cart.prg_size / 2 - 1;

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
			ser.Sync("prg_regs", ref prg_regs);
			ser.Sync("chr_regs", ref chr_regs);
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0000:
					prg_regs[0] = (byte)(value & prg_bank_mask_16k);
					break;
				case 0x0310:
					chr_regs[0] = (byte)(value & chr_bank_mask_2k);
					break;
				case 0x0311:
					chr_regs[1] = (byte)(value & chr_bank_mask_2k);
					break;
				case 0x0316:
					chr_regs[2] = (byte)(value & chr_bank_mask_2k);
					break;
				case 0x0317:
					chr_regs[3] = (byte)(value & chr_bank_mask_2k);
					break;
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int index = (addr >> 11) & 0x3;
				int bank = chr_regs[index];
				return VROM[(bank << 11) + (addr & 0x7FF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[(prg_regs[0] << 14) + (addr & 0x3FFF)];
			}

			return ROM[(prg_regs[1] << 14) + (addr & 0x3FFF)];
		}
	}

	public class ConyC : NES.NESBoardBase
	{
		private ByteBuffer prg_regs = new ByteBuffer(2);
		private ByteBuffer chr_regs = new ByteBuffer(8);

		private int prg_bank_mask_16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER083":
					// We need one of the Cony boards to throw an error on an unexpected cart size, so we picked this one
					if (Cart.prg_size != 128 && Cart.prg_size != 256 && Cart.prg_size != 1024)
					{
						throw new InvalidOperationException("Unexpected prg size of " + Cart.prg_size + " for Mapper 83");
					}

					if (Cart.prg_size == 1024)
					{
						prg_bank_mask_16k = Cart.prg_size / 16 - 1;

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
			ser.Sync("chr_regs", ref chr_regs);
			ser.Sync("prg_regs", ref prg_regs);
		}

		public override void WritePRG(int addr, byte value)
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
				// TODO: irq
			}

			else if (addr == 0x201)
			{
				// TODO: irq
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int index = (addr >> 10) & 0x7;
				int bank = chr_regs[index];
				return VROM[(bank << 10) + (addr & 0x3FF)];
			}

			return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[(prg_regs[0] << 14) + (addr & 0x3FFF)];
			}

			return ROM[(prg_regs[1] << 14) + (addr & 0x3FFF)];
		}
	}
}
