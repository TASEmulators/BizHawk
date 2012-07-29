using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class JALECO_SS8806 : NES.NESBoardBase
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_018

		ByteBuffer prg_banks_8k = new ByteBuffer(4);
		ByteBuffer chr_banks_1k = new ByteBuffer(8);
		int chr_bank_mask_1k, prg_bank_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER018":
				case "JALECO-JF-24": //TODO: there will be many boards to list here
					break;
				default:
					return false;
			}

			chr_bank_mask_1k = Cart.chr_size / 1 - 1;
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			prg_banks_8k[3] = 0xFF;

			SetMirrorType(EMirrorType.Horizontal);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			base.SyncState(ser);
		}

		public override void Dispose()
		{
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
			base.Dispose();
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000; //temporary

			addr &= 0xF003;
			switch (addr)
			{
				case 0x8000:
					prg_banks_8k[0] |= (byte)(value & 0x0F);
					break;
				case 0x8001:
					prg_banks_8k[0] |= (byte)((value & 0x0F) << 4);
					break;
				case 0x8002:
					prg_banks_8k[1] |= (byte)(value & 0x0F);
					break;
				case 0x8003:
					prg_banks_8k[1] |= (byte)((value & 0x0F) << 4);
					break;
				case 0x9000:
					prg_banks_8k[2] |= (byte)(value & 0x0F);
					break;
				case 0x9001:
					prg_banks_8k[2] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xA000:
					chr_banks_1k[0] |= (byte)(value & 0x0F);
					break;
				case 0xA001:
					chr_banks_1k[0] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xA002:
					chr_banks_1k[1] |= (byte)(value & 0x0F);
					break;
				case 0xA003:
					chr_banks_1k[1] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xB000:
					chr_banks_1k[2] |= (byte)(value & 0x0F);
					break;
				case 0xB001:
					chr_banks_1k[2] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xB002:
					chr_banks_1k[3] |= (byte)(value & 0x0F);
					break;
				case 0xB003:
					chr_banks_1k[3] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xC000:
					chr_banks_1k[4] |= (byte)(value & 0x0F);
					break;
				case 0xC001:
					chr_banks_1k[4] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xC002:
					chr_banks_1k[5] |= (byte)(value & 0x0F);
					break;
				case 0xC003:
					chr_banks_1k[5] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xD000:
					chr_banks_1k[6] |= (byte)(value & 0x0F);
					break;
				case 0xD001:
					chr_banks_1k[6] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xD002:
					chr_banks_1k[7] |= (byte)(value & 0x0F);
					break;
				case 0xD003:
					chr_banks_1k[7] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xF002:
					switch (value & 0x03)
					{
						case 0:
							SetMirrorType(EMirrorType.Horizontal);
							break;
						case 1:
							SetMirrorType(EMirrorType.Vertical);
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
			int bank_8k = addr >> 13;
			bank_8k = prg_banks_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			return ROM[(bank_8k * 0x2000) + (addr & 0x1FFF)];
		}

		private int MapCHR(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = chr_banks_1k[bank_1k];
			bank_1k &= chr_bank_mask_1k;
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}


		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (VROM != null)
					return VROM[addr];
				else return VRAM[addr];
			}
			else return base.ReadPPU(addr);
		}
	}
}
