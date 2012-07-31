using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper090 : NES.NESBoardBase
	{
		ByteBuffer prg_banks = new ByteBuffer(4);
		IntBuffer chr_banks = new IntBuffer(8);
		int prg_bank_mask_8k;
		int prg_bank_mask_16k;
		int prg_bank_mask_32k;

		int chr_bank_mask_1k;
		int chr_bank_mask_2k;
		int chr_bank_mask_4k;
		int chr_bank_mask_8k;

		int prg_mode_select = 0;
		bool sram_prg = false;
		
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER090":
					break;
				case "MAPPER209":
					//TODO: Set some flag for 209 mirroring
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;

			chr_bank_mask_1k = Cart.chr_size / 1 - 1;
			chr_bank_mask_2k = Cart.chr_size / 2 - 1;
			chr_bank_mask_4k = Cart.chr_size / 4 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_regs_8k", ref prg_banks);
			ser.Sync("prg_mode_select", ref prg_mode_select);
			ser.Sync("sram_prg", ref sram_prg);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0000:
				case 0x0004:
					prg_banks[0] = (byte)(value & 0x7F);
					break;
				case 0x0001:
				case 0x0005:
					prg_banks[1] = (byte)(value & 0x7F);
					break;
				case 0x0002:
				case 0x0006:
					prg_banks[2] = (byte)(value & 0x7F);
					break;
				case 0x0003:
				case 0x0007:
					prg_banks[3] = (byte)(value & 0x7F);
					break;

				case 0x1000:
					break;
				case 0x1001:
					break;
				case 0x1002:
					break;
				case 0x1003:
					break;
				case 0x1004:
					break;
				case 0x1005:
					break;
				case 0x1006:
					break;
				case 0x1007:
					break;

				case 0x5000:
					prg_mode_select = (byte)(value & 0x07);
					sram_prg = value.Bit(7);
					break;
				case 0x5001: //TODO: mapper 90 flag
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
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank = 0;
			switch (prg_mode_select)
			{
				case 0:
					bank = 0xFF & prg_bank_mask_32k;
					return ROM[(bank * 0x8000) + (addr & 0x7FFF)];
				case 1:
					if (addr < 0x4000)
					{
						bank = prg_banks[0] & prg_bank_mask_16k;
					}
					else
					{
						bank = 0xFF & prg_bank_mask_16k;
						
					}
					return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
				case 2:
				case 3:
					if (addr < 0x2000)
					{
						bank = prg_banks[0] & prg_bank_mask_8k;
					}
					else if (addr < 0x4000)
					{
						bank = prg_banks[1] & prg_bank_mask_8k;
					}
					else if (addr < 0x6000)
					{
						bank = prg_banks[2] & prg_bank_mask_8k;
					}
					else
					{
						bank = 0xFF & prg_bank_mask_8k;
					}
					return ROM[(bank * 0x2000) + (addr & 0x1FFF)];
				case 4:
					bank = prg_banks[3] & prg_bank_mask_32k;
					return ROM[(bank * 0x8000) + (addr & 0x7FFF)];
				case 5:
					if (addr < 0x4000)
					{
						bank = prg_banks[0] & prg_bank_mask_16k;
					}
					else
					{
						bank = prg_banks[1] & prg_bank_mask_16k;
						
					}
					return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
				case 6:
				case 7:
					if (addr < 0x2000)
					{
						bank = prg_banks[0] & prg_bank_mask_8k;
					}
					else if (addr < 0x4000)
					{
						bank = prg_banks[1] & prg_bank_mask_8k;
					}
					else if (addr < 0x6000)
					{
						bank = prg_banks[2] & prg_bank_mask_8k;
					}
					else
					{
						bank = prg_banks[3] & prg_bank_mask_8k;
					}
					return ROM[(bank * 0x2000) + (addr & 0x1FFF)];
			}
			
			bank = prg_banks[0];
			bank &= prg_bank_mask_8k;
			return ROM[(bank * 0x2000) + (addr & 0x1FFF)];
		}

		public override byte ReadWRAM(int addr)
		{
			if (sram_prg)
			{
				int bank = 0;
				switch (prg_mode_select)
				{
					case 0:
					case 4:
						bank = (prg_banks[3] << 2) + 3;
						break;
					case 1:
					case 5:
						bank = (prg_banks[3] << 1) + 3;
						break;
					case 2:
					case 3:
					case 6:
					case 7:
						bank = prg_banks[3];
						break;
				}
				return ROM[(bank * 0x2000) + (addr + 0x1FFF)];
			}
			else
			{
				return base.ReadWRAM(addr);
			}
		}
	}
}
