using System;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper090 : NES.NESBoardBase
	{
		ByteBuffer prg_regs = new ByteBuffer(4);
		IntBuffer chr_regs = new IntBuffer(8);
		IntBuffer nt_regs = new IntBuffer(4);

		IntBuffer prg_banks = new IntBuffer(4);
		IntBuffer chr_banks = new IntBuffer(8);
		IntBuffer chr_latches = new IntBuffer(2);

		ByteBuffer ram_bytes = new ByteBuffer(5);

		[MapperProp]
		public bool dipswitch_0;
		[MapperProp]
		public bool dipswitch_1;

		int prg_bank_mask_8k;
		int chr_bank_mask_1k;

		byte prg_mode_select = 0;
		byte chr_mode_select = 0;
		bool sram_prg = false;

		int ram_bank;

		bool mapper_090 = false;
		bool mapper_209 = false;
		bool mapper_211 = false;

		bool nt_advanced_control = false;
		bool nt_ram_disable = false;

		bool nt_ram_select = false;


		bool mirror_chr = false;
		bool chr_block_mode = true;
		int chr_block = 0;
		int prg_block = 0;

		int multiplicator = 0;
		int multiplicand = 0;
		int multiplication_result = 0;

		bool irq_enable = false;
		bool irq_pending = false;

		bool irq_count_down = false;
		bool irq_count_up = false;
		int irq_prescaler_size;
		byte irq_source = 0;

		byte prescaler;
		byte irq_counter;
		byte xor_reg;

		int a12_old;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER090":
				case "UNIF_UNL-TEK90":
					mapper_090 = true;
					nt_advanced_control = false;
					break;
				case "MAPPER209":
					mapper_209 = true;	
					break;
				case "MAPPER211":
					nt_advanced_control = true;
					mapper_211 = true;
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;

			// Junk support
			if (Cart.chr_size == 2040)
			{
				chr_bank_mask_1k = 2047;
			}

			InitValues();

			return true;
		}

		public override void NESSoftReset()
		{
			InitValues();

			base.NESSoftReset();
		}

		private void InitValues()
		{
			for (int i = 0; i < 4; i++)
			{
				prg_regs[i] = 0xFF;
				nt_regs[i] = 0;
			}
			for (int i = 0; i < 8; i++)
			{
				chr_regs[i] = 0xFFFF;
			}

			chr_latches[0] = 0;
			chr_latches[1] = 4;

			AutoMapperProps.Apply(this);

			Sync();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);

			ser.Sync("prg_regs", ref prg_regs);
			ser.Sync("chr_regs", ref chr_regs);
			ser.Sync("chr_latches", ref chr_latches);
			ser.Sync("nt_regs", ref nt_regs);

			ser.Sync("prg_banks", ref prg_banks);
			ser.Sync("chr_banks", ref chr_banks);
			ser.Sync("ram_bytes", ref ram_bytes);

			ser.Sync("dipswitch_0", ref dipswitch_0);
			ser.Sync("dipswitch_1", ref dipswitch_1);

			ser.Sync("prg_bank_mask_8k", ref prg_bank_mask_8k);
			ser.Sync("chr_bank_mask_1k", ref chr_bank_mask_1k);

			ser.Sync("prg_mode_select", ref prg_mode_select);
			ser.Sync("chr_mode_select", ref chr_mode_select);
			ser.Sync("sram_prg", ref sram_prg);
			ser.Sync("ram_bank", ref ram_bank);

			ser.Sync("mapper_090", ref mapper_090);
			ser.Sync("mapper_209", ref mapper_209);
			ser.Sync("mapper_211", ref mapper_211);

			ser.Sync("nt_advanced_control", ref nt_advanced_control);
			ser.Sync("nt_ram_disable", ref nt_ram_disable);
			ser.Sync("nt_ram_select", ref nt_ram_select);

			ser.Sync("mirror_chr", ref mirror_chr);
			ser.Sync("chr_block_mode", ref chr_block_mode);
			ser.Sync("chr_block", ref chr_block);
			ser.Sync("prg_block", ref prg_block);

			ser.Sync("multiplicator", ref multiplicator);
			ser.Sync("multiplicand", ref multiplicand);
			ser.Sync("multiplication_result", ref multiplication_result);

			ser.Sync("irq_enable", ref irq_enable);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_count_down", ref irq_count_down);
			ser.Sync("irq_count_up", ref irq_count_up);
			ser.Sync("irq_prescaler_size", ref irq_prescaler_size);
			ser.Sync("irq_source", ref irq_source);
			ser.Sync("prescaler", ref prescaler);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("xor_reg", ref xor_reg);
			ser.Sync("a12_old", ref a12_old);

			Sync();
		}

		public override void Dispose()
		{
			prg_regs.Dispose();
			chr_regs.Dispose();
			chr_latches.Dispose();
			nt_regs.Dispose();
			prg_banks.Dispose();
			chr_banks.Dispose();
			ram_bytes.Dispose();
			base.Dispose();
		}

		private void Sync()
		{
			SyncIRQ();
			SyncPRGBanks();
			SyncCHRBanks();
			SyncNametables();
		}

		private void SetBank(IntBuffer target, byte offset, byte size, int value)
		{
			value &= ~(size - 1);
			for (int i = 0; i < size; i++)
			{
				int index = i + offset;
				target[index] = value;
				value++;
			}
		}

		private byte BitRev6(int value)
		{
			int newvalue = 0;

			newvalue |= (value & 0x20) >> 5;
			newvalue |= (value & 0x10) >> 3;
			newvalue |= (value & 0x08) >> 1;
			newvalue |= (value & 0x04) << 1;
			newvalue |= (value & 0x02) << 3;
			newvalue |= (value & 0x01) << 5;

			return (byte)newvalue;
		}

		private void SyncPRGBanks()
		{
			int bankmode = prg_block << 6;

			switch(prg_mode_select)
			{
				case 0:
					SetBank(prg_banks, 0, 4, ( bankmode | (prg_bank_mask_8k & 0x3F) ));
					ram_bank = bankmode | (((prg_regs[3] << 2) + 3) & 0x3F);
					break;
				case 1:
					SetBank(prg_banks, 0, 2, ( bankmode | (prg_regs[1] & 0x1F) ));
					SetBank(prg_banks, 2, 2, ( bankmode | (prg_bank_mask_8k & 0x3F) ));
					ram_bank = bankmode | (((prg_regs[3] << 1) + 1) & 0x3F);
					break;
				case 2:
					SetBank(prg_banks, 0, 1, ( bankmode | prg_regs[0] ));
					SetBank(prg_banks, 1, 1, ( bankmode | prg_regs[1] ));
					SetBank(prg_banks, 2, 1, ( bankmode | prg_regs[2] ));
					SetBank(prg_banks, 3, 1, ( bankmode | (prg_bank_mask_8k & 0x3F) ));
					ram_bank = bankmode | prg_regs[3];
					break;
				case 3:
					SetBank(prg_banks, 0, 1, ( bankmode | BitRev6(prg_regs[0]) ));
					SetBank(prg_banks, 1, 1, ( bankmode | BitRev6(prg_regs[1]) ));
					SetBank(prg_banks, 2, 1, ( bankmode | BitRev6(prg_regs[2]) ));
					SetBank(prg_banks, 3, 1, ( bankmode | (prg_bank_mask_8k & 0x3F) ));
					ram_bank = bankmode | BitRev6(prg_regs[3]);
					break;
				case 4:
					SetBank(prg_banks, 0, 4, ( bankmode | (prg_regs[3] & 0x3F) ));
					ram_bank = bankmode | (((prg_regs[3] << 2) + 3) & 0x3F);
					break;
				case 5:					
					SetBank(prg_banks, 0, 2, ( bankmode | (prg_regs[1] & 0x1F) ));
					SetBank(prg_banks, 2, 2, ( bankmode | (prg_regs[3] & 0x1F) ));
					ram_bank = bankmode | (((prg_regs[3] << 1) + 1) & 0x3F);
					break;
				case 6:
					SetBank(prg_banks, 0, 1, ( bankmode | prg_regs[0] ));
					SetBank(prg_banks, 1, 1, ( bankmode | prg_regs[1] ));
					SetBank(prg_banks, 2, 1, ( bankmode | prg_regs[2] ));
					SetBank(prg_banks, 3, 1, ( bankmode | prg_regs[3] ));
					ram_bank = bankmode | prg_regs[3];
					break;
				case 7:
					SetBank(prg_banks, 0, 1, ( bankmode | BitRev6(prg_regs[0]) ));
					SetBank(prg_banks, 1, 1, ( bankmode | BitRev6(prg_regs[1]) ));
					SetBank(prg_banks, 2, 1, ( bankmode | BitRev6(prg_regs[2]) ));
					SetBank(prg_banks, 3, 1, ( bankmode | BitRev6(prg_regs[3]) ));
					ram_bank = bankmode | BitRev6(prg_regs[3]);
					break;
			}
		}

		private void SyncCHRBanks()
		{
			int mask = 0xFFFF;
			int block = 0;

			if (chr_block_mode)
			{
				mask = 0xFF >> (chr_mode_select ^ 3);
				block = chr_block << (chr_mode_select + 5);
			}

			int mirror_chr_9002 = mirror_chr ? 0 : 2;
			int mirror_chr_9003 = mirror_chr ? 1 : 3;

			switch (chr_mode_select)
			{
				case 0:
					SetBank(chr_banks, 0, 8, ((chr_regs[0] & mask) | block) << 3);
					break;
				case 1:
					var reg_0 = mapper_090 ? chr_regs[0] : chr_regs[chr_latches[0]];
					var reg_1 = mapper_090 ? chr_regs[4] : chr_regs[chr_latches[1]];

					SetBank(chr_banks, 0, 4, ((reg_0 & mask) | block) << 2);
					SetBank(chr_banks, 4, 4, ((reg_1 & mask) | block) << 2);
					break;
				case 2:
					SetBank(chr_banks, 0, 2, ((chr_regs[0] & mask) | block) << 1);
					SetBank(chr_banks, 2, 2, ((chr_regs[mirror_chr_9002] & mask) | block) << 1);
					SetBank(chr_banks, 4, 2, ((chr_regs[4] & mask) | block) << 1);
					SetBank(chr_banks, 6, 2, ((chr_regs[6] & mask) | block) << 1);
					break;
				case 3:
					SetBank(chr_banks, 0, 1, (chr_regs[0] & mask) | block);
					SetBank(chr_banks, 1, 1, (chr_regs[1] & mask) | block);
					SetBank(chr_banks, 2, 1, (chr_regs[mirror_chr_9002] & mask) | block);
					SetBank(chr_banks, 3, 1, (chr_regs[mirror_chr_9003] & mask) | block);
					SetBank(chr_banks, 4, 1, (chr_regs[4] & mask) | block);
					SetBank(chr_banks, 5, 1, (chr_regs[5] & mask) | block);
					SetBank(chr_banks, 6, 1, (chr_regs[6] & mask) | block);
					SetBank(chr_banks, 7, 1, (chr_regs[7] & mask) | block);
					break;
			}
		}

		private void SyncNametables()
		{
			if (nt_advanced_control)
			{
				int[] m = new int[4];
				for (var i = 0; i < 4; i++)
				{
					m[i] = nt_regs[i] & 0x01;
				}
				SetMirroring(m[0], m[1], m[2], m[3]);
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr & 0x7007)	
			{
				case 0x0000:	//0x8000: PRG ROM select
				case 0x0001:
				case 0x0002:
				case 0x0003:
				case 0x0004:
				case 0x0005:
				case 0x0006:
				case 0x0007:
					prg_regs[addr & 3] = (byte)(value & 0x3F);
					SyncPRGBanks();
					break;

				case 0x1000:	//0x9000: CHR ROM lower 8 bits select
				case 0x1001:
				case 0x1002:
				case 0x1003:
				case 0x1004:
				case 0x1005:
				case 0x1006:
				case 0x1007:
					chr_regs[addr & 7] &= 0xff00;
					chr_regs[addr & 7] |= value;
					SyncCHRBanks();
					break;

				case 0x2000:	//0xA000: CHR ROM upper 8 bits select
				case 0x2001:
				case 0x2002:
				case 0x2003:
				case 0x2004:
				case 0x2005:
				case 0x2006:
				case 0x2007:
					chr_regs[addr & 7] &= 0x00ff;
					chr_regs[addr & 7] |= (value << 8);
					SyncCHRBanks();
					break;

				case 0x3000:	//0xB000 Nametable Regs
				case 0x3001:
				case 0x3002:
				case 0x3003:
					nt_regs[addr & 3] &= 0xff00;
					nt_regs[addr & 3] |= value;
					SyncNametables();
					break;
					
				case 0x3004:
				case 0x3005:
				case 0x3006:
				case 0x3007:
					nt_regs[addr & 3] &= 0x00ff;
					nt_regs[addr & 3] |= (value << 8);
					SyncNametables();
					break; 

				case 0x4000:	//0xC000 IRQ operation
					if (value.Bit(0))
					{
						goto case 0x4003;
					}
					else
					{
						goto case 0x4002;						
					}
				case 0x4001:	//IRQ control
					irq_count_down = value.Bit(7);
					irq_count_up = value.Bit(6);
					//Bit 3 enables IRQ prescaler adjusting at 0xC007.
					irq_prescaler_size = value.Bit(2) ? 8 : 256;

					//TODO: Mode 4 (CPU reads) not implemented. No game actually seems to use it, however.
					irq_source = (byte)(value & 0x03);
					break;
				case 0x4002:	//IRQ acknowledge and disable
					irq_pending = false;
					irq_enable = false;
					SyncIRQ();
					break;
				case 0x4003:	//IRQ enable
					irq_enable = true;
					SyncIRQ();
					break;
				case 0x4004:	//Prescaler
					prescaler = (byte)(value ^ xor_reg);
					break;
				case 0x4005:	//IRQ_Counter
					irq_counter = (byte)(value ^ xor_reg);
					break;
				case 0x4006:	//XOR Reg
					xor_reg = value;
					break;
				case 0x4007:	//IRQ prescaler adjust
					//Poorly understood, and no game actually appears to use it.
					//We therefore forego emulating it.
					break;

				case 0x5000:	//0xD000 Mapper Banking Control and Mirroring
				case 0x5004:
					//Only Mapper 209 can set this. It is always clear for Mapper 90 and always set for Mapper 211
					if (mapper_209)
					{
						nt_advanced_control = value.Bit(5);
					}
					nt_ram_disable = value.Bit(6);
					prg_mode_select = (byte)(value & 0x07);
					chr_mode_select = (byte)((value >> 3) & 0x03);
					sram_prg = value.Bit(7);

					SyncPRGBanks();
					SyncCHRBanks();
					SyncNametables();
					break;
				case 0x5001:	//0xD001: Mirroring
				case 0x5005:
					switch (value & 0x3)
					{
						case 0:
							SetMirrorType(EMirrorType.Vertical);
							break;
						case 1:
							SetMirrorType(EMirrorType.Horizontal);
							break;
						case 2:
							SetMirrorType(EMirrorType.OneScreenA);
							break;
						case 3:
							SetMirrorType(EMirrorType.OneScreenB);
							break;
					}
					break;
				case 0x5002:
				case 0x5006:
					nt_ram_select = value.Bit(7);
					SyncNametables();
					break;
				case 0x5003:
				case 0x5007:
					mirror_chr = value.Bit(7);
					chr_block_mode = !value.Bit(5);
					chr_block = ((value & 0x18) >> 2) | (value & 0x1);
					prg_block = (value & 0x06) >> 1;
					SyncPRGBanks();
					SyncCHRBanks();
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int offset = addr & 0x1FFF;
			int bank = prg_banks[addr >> 13];
			bank &= prg_bank_mask_8k;
			return ROM[bank << 13 | offset];
		}

		public override byte ReadWRAM(int addr)
		{
			return sram_prg ? ROM[ram_bank << 13 | addr & 0x1FFF] : base.ReadWRAM(addr);
		}

		public override byte ReadEXP(int addr)
		{
			switch (addr & 0x1807)
			{
				case 0x1000:
					int value = dipswitch_0 ? 0x80 : 0x00;
					value = dipswitch_1 ? value | 0x40 : value;
					return (byte)(value | (NES.DB & 0x3F));
				case 0x1800:
					return (byte)multiplication_result;
				case 0x1801:
					return (byte)(multiplication_result >> 8);
				case 0x1803:
				case 0x1804:
				case 0x1805:
				case 0x1806:
				case 0x1807:
					return ram_bytes[addr - 0x1803];
				default:
					return base.ReadEXP(addr);
			}
		}

		public override void WriteEXP(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1800:
					multiplicator = value;
					multiplication_result = multiplicator * multiplicand;
					break;
				case 0x1801:
					multiplicand = value;
					multiplication_result = multiplicator * multiplicand;
					break;
				case 0x1803:	//It's not known if 0x1804 - 0x1807 are actually RAM. For safety, we'll assume it is.
				case 0x1804:
				case 0x1805:
				case 0x1806:
				case 0x1807:
					ram_bytes[addr - 0x1803] = value;
					break;
			}
		}

		public override void ClockCPU()
		{
			if (irq_source == 0)
			{
				ClockIRQ();
			}
		}

		public void ClockIRQ()
		{
			int mask = irq_prescaler_size - 1;

			if (irq_count_up && !irq_count_down)
			{
				prescaler++;
				if((prescaler & mask) == 0)
				{
					irq_counter++;
					if(irq_counter == 0)
					{
						irq_pending = irq_enable;
					}
				}
			}

			if (irq_count_down && !irq_count_up)
			{
				prescaler--;
				if((prescaler & mask) == mask)
				{
					irq_counter--;
					if (irq_counter == 0xFF)
					{
						irq_pending = irq_enable;
					}
				}
			}

			SyncIRQ();
		}

		public void SyncIRQ()
		{
			SyncIRQ(irq_pending);
		}

		public override void AddressPPU(int addr)
		{
			int a12 = (addr >> 12) & 1;
			bool rising_edge = (a12 == 1 && a12_old == 0);

			if (rising_edge && irq_source == 1)
			{
				ClockIRQ();
			}

			a12_old = a12;
		}

		public override byte PeekPPU(int addr)
		{
			if (addr < 0x2000)	//Read CHR
			{
				int bank = chr_banks[addr >> 10];
				bank &= chr_bank_mask_1k;
				int offset = addr & 0x3FF;

				return VROM[bank << 10 | offset];
			}

			if (nt_advanced_control) //Read from Nametables
			{
				addr -= 0x2000;
				int nt = nt_regs[addr >> 10];
				int offset = addr & 0x3FF;

				if (!nt_ram_disable)
				{
					if(nt.Bit(7) == nt_ram_select)
					{
						return nt.Bit(0) ? NES.CIRAM[0x400 | offset] : NES.CIRAM[offset];
					}
				}

				return VROM[nt << 10 | offset];
			}
			else
			{
				return base.PeekPPU(addr);
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (irq_source == 2)
			{
				ClockIRQ();	//No game ever should use this.
			}

			if (addr < 0x2000)	//Read CHR
			{
				int bank = chr_banks[addr >> 10];
				bank &= chr_bank_mask_1k;
				int offset = addr & 0x3FF;

				//Super Strange MMC2 logic
				int side = addr >> 12;
				int tile = addr & 0xFF8;

				switch (tile)
				{
					case 0xFD8:
					case 0xFE8:
						chr_latches[side] = (addr >> 4) & ((side << 2) | 0x2);
						SyncCHRBanks();
						break;
				}

				return VROM[bank << 10 | offset];
			}

			if (nt_advanced_control) //Read from Nametables
			{
				addr -= 0x2000;
				int nt = nt_regs[addr >> 10];
				int offset = addr & 0x3FF;

				if (!nt_ram_disable)
				{
					if(nt.Bit(7) == nt_ram_select)
					{
						return nt.Bit(0) ? NES.CIRAM[0x400 | offset] : NES.CIRAM[offset];
					}
				}

				return VROM[nt << 10 | offset];
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}
	}
}
