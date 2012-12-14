using System;
using System.IO;
using System.Diagnostics;

//simplifications/approximations:
//* "Note that no commercial games rely on this mirroring -- therefore you can take the easy way out and simply give all MMC5 games 64k PRG-RAM."
//   (i.e. ignore chipselect/page select on prg-ram)
//* in general PPU state is peeked directly instead of figuring out how the mmc5 actually accounts for things.
//* Specifically, the tall sprite mode is peeked. this is annoying.. the mmc5 should not know about that until the first tall sprite appears and asks
//  for something from the right page. there should be a better way to determine this
//* Specifically, the dot number / BG/OBJ phase status is used instead of counting reads.
//* Specifically, the scanline number is used for IRQ instead of counting reads or whatever

//TODO - tweak nametable / chr viewer to be more useful

//FUTURE - we may need to split this into a separate MMC5 class. but for now it is just a pain.

namespace BizHawk.Emulation.Consoles.Nintendo
{
	[NES.INESBoardImplPriority]
	public class ExROM : NES.NESBoardBase
	{
		//configuraton
		int prg_bank_mask_8k, chr_bank_mask_1k; //board setup (to be isolated from mmc5 code later, when we need the separate mmc5 class)

		//state
		int irq_target, irq_counter;
		bool irq_enabled, irq_pending, in_frame;
		int exram_mode, chr_mode, prg_mode;
		int chr_reg_high;
		int ab_mode;
		IntBuffer regs_a = new IntBuffer(8);
		IntBuffer regs_b = new IntBuffer(4);
		IntBuffer regs_prg = new IntBuffer(4);
		IntBuffer nt_modes = new IntBuffer(4);
		byte nt_fill_tile, nt_fill_attrib;
		int wram_bank;
		byte[] EXRAM = new byte[1024];
		byte multiplicand, multiplier;
		Sound.MMC5Audio audio;
		//regeneratable state
		IntBuffer a_banks_1k = new IntBuffer(8);
		IntBuffer b_banks_1k = new IntBuffer(8);
		IntBuffer prg_banks_8k = new IntBuffer(4);
		byte product_low, product_high;
		int last_nt_read;
		bool irq_audio;

		public MemoryDomain GetExRAM()
		{
			return new MemoryDomain("ExRAM", EXRAM.Length, Endian.Little, (addr) => EXRAM[addr], (addr, val) => EXRAM[addr] = val);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("irq_target", ref irq_target);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("in_frame", ref in_frame);
			ser.Sync("exram_mode", ref exram_mode);
			ser.Sync("chr_mode", ref chr_mode);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("chr_reg_high", ref chr_reg_high);
			ser.Sync("ab_mode", ref ab_mode);
			ser.Sync("regs_a", ref regs_a);
			ser.Sync("regs_b", ref regs_b);
			ser.Sync("regs_prg", ref regs_prg);
			ser.Sync("nt_modes", ref nt_modes);
			ser.Sync("nt_fill_tile", ref nt_fill_tile);
			ser.Sync("nt_fill_attrib", ref nt_fill_attrib);
			ser.Sync("wram_bank", ref wram_bank);
			ser.Sync("last_nt_read", ref last_nt_read);
			ser.Sync("EXRAM", ref EXRAM, false);

			SyncPRGBanks();
			SyncCHRBanks();
			SyncMultiplier();
			SyncIRQ();
			audio.SyncState(ser);
		}

		public override void Dispose()
		{
			regs_a.Dispose();
			regs_b.Dispose();
			regs_prg.Dispose();
			a_banks_1k.Dispose();
			b_banks_1k.Dispose();
			prg_banks_8k.Dispose();
			nt_modes.Dispose();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER005":
					break;
				case "NES-ELROM": //Castlevania 3 - Dracula's Curse (U)
					AssertPrg(128,256); AssertChr(128);
					break;
				case "NES-EKROM": //Gemfire (U)
					AssertPrg(256); AssertChr(256);
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size/8-1;
			chr_bank_mask_1k = Cart.chr_size - 1;

			PoweronState();

			if (NES.apu != null)
				audio = new Sound.MMC5Audio(NES.apu.ExternalQueue, (e) => { irq_audio = e; SyncIRQ(); });

			return true;
		}

		void PoweronState()
		{
			//set all prg regs to use ROM
			regs_prg[0] = 0x80;
			regs_prg[1] = 0x80;
			regs_prg[2] = 0x80;
			regs_prg[3] = 0xFF;
			prg_mode = 3;

			SyncPRGBanks();
			SyncCHRBanks();
			SetMirrorType(EMirrorType.Vertical);
		}

		int MapWRAM(int addr)
		{
			int bank_8k = wram_bank;
			int ofs = addr & ((1 << 13) - 1);
			addr = (bank_8k << 13) | ofs;
			return addr;
		}

		int MapPRG(int addr, out bool ram)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			ram = (bank_8k & 0x80) == 0;
			bank_8k &= ~0x80;
			if (!ram)
				bank_8k &= prg_bank_mask_8k;
			return (bank_8k << 13) | ofs;
		}

		//this could be handy, but probably not. I did it on accident.
		//TileCoord ComputeTXTYFromPPUTiming(int visible_scanline, int cycle)
		//{
		//  int py = visible_scanline;
		//  int px = cycle;
		//  if (cycle > 260)
		//  {
		//    py++;
		//    px -= 322;
		//  }
		//  else px += 16;
		//  int tx = px / 8;
		//  int ty = py / 8;
		//  return new TileCoord(tx, ty);
		//}

		int MapCHR(int addr)
		{
			int bank_1k = addr >> 10;
			int ofs = addr & ((1 << 10) - 1);

			if (exram_mode == 1 && NES.ppu.ppuphase == Nintendo.NES.PPU.PPUPHASE.BG)
			{
				int exram_addr = last_nt_read;
				int bank_4k = EXRAM[exram_addr] & 0x3F;
				
				bank_1k = bank_4k * 4;
				bank_1k += chr_reg_high<<2;
				ofs = addr & (4 * 1024 - 1);
				goto MAPPED;
			}

			//wish this logic could be smaller..
			//how does this KNOW that its in 8x16 sprites? the pattern of reads... emulate it that way..
			if (NES.ppu.reg_2000.obj_size_16)
			{
				if (NES.ppu.ppuphase == NES.PPU.PPUPHASE.OBJ)
					bank_1k = a_banks_1k[bank_1k];
				else
					bank_1k = b_banks_1k[bank_1k];
			}
			else
				if (ab_mode == 0)
					bank_1k = a_banks_1k[bank_1k];
				else
					bank_1k = b_banks_1k[bank_1k];
		
			MAPPED:
			bank_1k &= chr_bank_mask_1k;
			addr = (bank_1k<<10)|ofs;
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				return VROM[addr];
			}
			else
			{
				addr -= 0x2000;
				int nt_entry = addr & 0x3FF;
				if (nt_entry < 0x3C0)
				{
					//track the last nametable entry read so that subsequent pattern and attribute reads will know which exram address to use
					last_nt_read = nt_entry;
				}
				else
				{
					//attribute table
					if (exram_mode == 1)
					{
						//attribute will be in the top 2 bits of the exram byte
						int exram_addr = last_nt_read;
						int attribute = EXRAM[exram_addr] >> 6;
						//calculate tile address by getting x/y from last nametable
						int tx = last_nt_read & 0x1F;
						int ty = last_nt_read / 32;
						//attribute table address is just these coords shifted
						int atx = tx >> 1;
						int aty = ty >> 1;
						//figure out how we need to shift the attribute to fake out the ppu
						int at_shift = ((aty & 1) << 1) + (atx & 1);
						at_shift <<= 1;
						attribute <<= at_shift;
						return (byte)attribute;
					}
				}
				int nt = addr >> 10;
				int offset = addr & ((1<<10)-1);
				nt = nt_modes[nt];
				switch (nt)
				{
					case 0: //NES internal NTA
						return base.ReadPPU(0x2000 + offset);
					case 1: //NES internal NTB
						return base.ReadPPU(0x2400 + offset);
					case 2: //use ExRAM as NT
						//TODO - additional r/w security
						if (exram_mode >= 2) return 0;
						else return EXRAM[offset];
					case 3: //Fill Mode
						return 0xFF; //TODO
					default: throw new Exception();
				}
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				throw new InvalidOperationException();
			}
			else
			{
				addr -= 0x2000;
				int nt = addr >> 10;
				int offset = addr & ((1 << 10) - 1);
				nt = nt_modes[nt];
				switch (nt)
				{
					case 0: //NES internal NTA
						base.WritePPU(0x2000 + offset, value);
						break;
					case 1: //NES internal NTB
						base.WritePPU(0x2400 + offset, value);
						break;
					case 2: //use ExRAM as NT
						//TODO - additional r/w security
						EXRAM[offset] = value;
						break;
					case 3: //Fill Mode
						//what to do?
						break;
					default: throw new Exception();
				}
			}
		}

		public override void WriteWRAM(int addr, byte value)
		{
			addr = MapWRAM(addr);
			WRAM[addr] = value;
		}

		public override byte ReadWRAM(int addr)
		{
			addr = MapWRAM(addr);
			return WRAM[addr];
		}

		public override byte ReadPRG(int addr)
		{
			bool ram;
			addr = MapPRG(addr, out ram);
			if (ram) return WRAM[addr];
			else return ROM[addr];
		}

		public override void WritePRG(int addr, byte value)
		{
			bool ram;
			addr = MapPRG(addr, out ram);
			if (ram) WRAM[addr] = value;
		}

		public override void WriteEXP(int addr, byte value)
		{
			//NES.LogLine("MMC5 WriteEXP: ${0:x4} = ${1:x2}", addr, value);
			if (addr >= 0x1000 && addr <= 0x1015)
			{
				audio.WriteExp(addr + 0x4000, value);
				return;
			}
			switch (addr)
			{
				case 0x1100: //$5100:  [.... ..PP]    PRG Mode Select:
					prg_mode = value & 3;
					SyncPRGBanks();
					break;

				case 0x1101: //$5101:  [.... ..CC]
					chr_mode = value & 3;
					SyncCHRBanks();
					break;

				case 0x1102: //$5102:  [.... ..AA]    PRG-RAM Protect A
				case 0x1103: //$5103:  [.... ..BB]    PRG-RAM Protect B
					break;

				case 0x1104: //$5104:  [.... ..XX]    ExRAM mode
					exram_mode = value & 3;
					//NES.LogLine("exram mode set to: {0}", exram_mode);
					break;

				case 0x1105: //$5105:  [DDCC BBAA] (nametable config)
					nt_modes[0] = (value >> 0) & 3;
					nt_modes[1] = (value >> 2) & 3;
					nt_modes[2] = (value >> 4) & 3;
					nt_modes[3] = (value >> 6) & 3;
					//NES.LogLine("nt_modes set to {0},{1},{2},{3}", nt_modes[0], nt_modes[1], nt_modes[2], nt_modes[3]);
					break;
				case 0x1106: //$5106:  [TTTT TTTT]     Fill Tile
					nt_fill_tile = value;
					break;
				case 0x1107: //$5107:  [.... ..AA]     Fill Attribute bits
					nt_fill_attrib = value;
					break;
				

				case 0x1113: //$5113:  [.... .PPP]        (simplified, but technically inaccurate -- see below)
					wram_bank = value & 7;
					break;

				//$5114-5117:  [RPPP PPPP] PRG select
				case 0x1114: case 0x1115: case 0x1116: case 0x1117:
					if (addr == 0x1117) value |= 0x80;
					regs_prg[addr - 0x1114] = value;
					SyncPRGBanks();
					break;

				//$5120 - $5127 'A' Regs:
				case 0x1120: case 0x1121: case 0x1122: case 0x1123: 
				case 0x1124: case 0x1125: case 0x1126: case 0x1127:
					ab_mode = 0;
					regs_a[addr - 0x1120] = value | (chr_reg_high<<8);
					//NES.LogLine("set bank A {0:x4} to {1:x2}", addr+0x4000, value);
					SyncCHRBanks();
					break;

				//$5128 - $512B 'B' Regs:
				case 0x1128: case 0x1129: case 0x112A: case 0x112B:
					ab_mode = 1;
					regs_b[addr - 0x1128] = value | (chr_reg_high<<8);
					//NES.LogLine("set bank B {0:x4} to {1:x2}", addr + 0x4000, value);
					SyncCHRBanks();
					break;

				case 0x1130: //$5130  [.... ..HH]  'High' CHR Reg:
					chr_reg_high = value & 3;
					break;

				case 0x1203: //$5203:  [IIII IIII]    IRQ Target
					irq_target = value;
					SyncIRQ();
					break;

				case 0x1204: //$5204:  [E... ....]    IRQ Enable (0=disabled, 1=enabled)
					irq_enabled = (value & 0x80) != 0;
					SyncIRQ();
					break;

				case 0x1205: //$5205:  multiplicand
					multiplicand = value;
					SyncMultiplier();
					break;
				case 0x1206: //$5206:  multiplier
					multiplier = value;
					SyncMultiplier();
					break;
			}

			//TODO - additional r/w timing security
			if (addr >= 0x1C00)
			{
				if(exram_mode != 3)
					EXRAM[addr - 0x1C00] = value;
			}
		}

		void SyncMultiplier()
		{
			int result = multiplicand*multiplier;
			product_low = (byte)(result&0xFF);
			product_high = (byte)((result>>8) & 0xFF);
		}

		public override byte ReadEXP(int addr)
		{
			byte ret = 0xFF;
			switch (addr)
			{
				case 0x1204: //$5204:  [E... ....]    IRQ Enable (0=disabled, 1=enabled)
					ret = (byte)((irq_pending ? 0x80 : 0) | (in_frame ? 0x40 : 0));
					irq_pending = false;
					SyncIRQ();
					break;

				case 0x1205: //$5205:  low 8 bits of product
					ret = product_low; 
					break;
				case 0x1206: //$5206:  high 8 bits of product
					ret = product_high;
					break;

				case 0x1015: // $5015: apu status
					ret = audio.Read5015();
					break;

				case 0x1010: // $5010: apu PCM
					ret = audio.Read5010();
					break;
			}

			//TODO - additional r/w timing security
			if (addr >= 0x1C00)
			{
				if (exram_mode < 2)
					ret = 0xFF;
				else ret = EXRAM[addr - 0x1C00];
			}

			return ret; ;
		}

		void SyncIRQ()
		{
			IRQSignal = (irq_pending && irq_enabled) || irq_audio;
		}

		public override void ClockPPU()
		{
			if (NES.ppu.ppur.status.cycle != 336)
				return;
			if (!NES.ppu.reg_2001.PPUON)
				return;

			int sl = NES.ppu.ppur.status.sl + 1;

			//not a visible scanline
			if (sl >= 241)
			{
				in_frame = false;
				return;
			}
	
			if (!in_frame)
			{
				in_frame = true;
				irq_counter = 0;
				irq_pending = false;
				SyncIRQ();
			}
			else
			{
				irq_counter++;
				if (irq_counter == irq_target)
				{
					irq_pending = true;
					SyncIRQ();
				}
			}

		}

		public override void ClockCPU()
		{
			audio.Clock();
		}

		void SetBank(IntBuffer target, int offset, int size, int value)
		{
			value &= ~(size-1);
			for (int i = 0; i < size; i++)
			{
				int index = i+offset;
				target[index] = value;
				value++;
			}
		}

		void SyncPRGBanks()
		{
			switch (prg_mode)
			{
				case 0:
					SetBank(prg_banks_8k, 0, 4, regs_prg[3]&~3);
					break;
				case 1:
					SetBank(prg_banks_8k, 0, 2, regs_prg[1] & ~1);
					SetBank(prg_banks_8k, 2, 2, regs_prg[3] & ~1);
					break;
				case 2:
					SetBank(prg_banks_8k, 0, 2, regs_prg[1] & ~1);
					SetBank(prg_banks_8k, 2, 1, regs_prg[2]);
					SetBank(prg_banks_8k, 3, 1, regs_prg[3]);
					break;
				case 3:
					SetBank(prg_banks_8k, 0, 1, regs_prg[0]);
					SetBank(prg_banks_8k, 1, 1, regs_prg[1]);
					SetBank(prg_banks_8k, 2, 1, regs_prg[2]);
					SetBank(prg_banks_8k, 3, 1, regs_prg[3]);
					break;
			}
		}

		void SyncCHRBanks()
		{
			//MASTER LOGIC: something like this this might be enough to work, but i'll play with it later
			//bank_1k >> (3 - chr_mode) << chr_mode | bank_1k & ( etc.etc.

			//TODO - do these need to have the last arguments multiplied by 8,4,2 to map to the right banks?
			switch (chr_mode)
			{
				case 0:
					SetBank(a_banks_1k, 0, 8, regs_a[7] * 8);
					SetBank(b_banks_1k, 0, 8, regs_b[3] * 8);
					break;
				case 1:
					SetBank(a_banks_1k, 0, 4, regs_a[3] * 4);
					SetBank(a_banks_1k, 4, 4, regs_a[7] * 4);
					SetBank(b_banks_1k, 0, 4, regs_b[3] * 4);
					SetBank(b_banks_1k, 4, 4, regs_b[3] * 4);
					break;
				case 2:
					SetBank(a_banks_1k, 0, 2, regs_a[1] * 2);
					SetBank(a_banks_1k, 2, 2, regs_a[3] * 2);
					SetBank(a_banks_1k, 4, 2, regs_a[5] * 2);
					SetBank(a_banks_1k, 6, 2, regs_a[7] * 2);
					SetBank(b_banks_1k, 0, 2, regs_b[1] * 2);
					SetBank(b_banks_1k, 2, 2, regs_b[3] * 2);
					SetBank(b_banks_1k, 4, 2, regs_b[1] * 2);
					SetBank(b_banks_1k, 6, 2, regs_b[3] * 2);
					break;
				case 3:
					SetBank(a_banks_1k, 0, 1, regs_a[0]);
					SetBank(a_banks_1k, 1, 1, regs_a[1]);
					SetBank(a_banks_1k, 2, 1, regs_a[2]);
					SetBank(a_banks_1k, 3, 1, regs_a[3]);
					SetBank(a_banks_1k, 4, 1, regs_a[4]);
					SetBank(a_banks_1k, 5, 1, regs_a[5]);
					SetBank(a_banks_1k, 6, 1, regs_a[6]);
					SetBank(a_banks_1k, 7, 1, regs_a[7]);
					SetBank(b_banks_1k, 0, 1, regs_b[0]);
					SetBank(b_banks_1k, 1, 1, regs_b[1]);
					SetBank(b_banks_1k, 2, 1, regs_b[2]);
					SetBank(b_banks_1k, 3, 1, regs_b[3]);
					SetBank(b_banks_1k, 4, 1, regs_b[0]);
					SetBank(b_banks_1k, 5, 1, regs_b[1]);
					SetBank(b_banks_1k, 6, 1, regs_b[2]);
					SetBank(b_banks_1k, 7, 1, regs_b[3]);
					break;
			}


		}

	}
}
