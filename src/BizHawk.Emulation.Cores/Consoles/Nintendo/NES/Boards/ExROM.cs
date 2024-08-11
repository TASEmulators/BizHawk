using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components;

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
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[NesBoardImplPriority]
	internal sealed class ExROM : NesBoardBase
	{
		private int prg_bank_mask_8k, chr_bank_mask_1k; //board setup (to be isolated from mmc5 code later, when we need the separate mmc5 class)

		private int irq_target, irq_counter;
		private bool irq_enabled, irq_pending, in_frame;
		private int exram_mode, chr_mode, prg_mode;
		private int chr_reg_high;
		private int ab_mode;
		private int[] regs_a = new int[8];
		private int[] regs_b = new int[4];
		private int[] regs_prg = new int[4];
		private int[] nt_modes = new int[4];
		private byte nt_fill_tile, nt_fill_attrib;
		private int wram_bank;
		private byte[] EXRAM = new byte[1024];
		private byte multiplicand, multiplier;
		private MMC5Audio audio;

		private int[] _aBanks1K = new int[8];
		private int[] _bBanks1K = new int[8];
		private int[] _prgBanks8K = new int[4];
		private byte product_low, product_high;
		private int last_nt_read;
		private bool irq_audio;

		public MemoryDomain GetExRAM()
		{
			return new MemoryDomainByteArray("ExRAM", MemoryDomain.Endian.Little, EXRAM, true, 1);
		}

		/// <summary>
		/// use with caution
		/// </summary>
		public byte[] GetExRAMArray() => EXRAM;

		public bool ExAttrActive => exram_mode == 1;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(_aBanks1K), ref _aBanks1K, false);
			ser.Sync(nameof(_bBanks1K), ref _bBanks1K, false);
			ser.Sync(nameof(_prgBanks8K), ref _prgBanks8K, false);
			ser.Sync(nameof(irq_target), ref irq_target);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_enabled), ref irq_enabled);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(in_frame), ref in_frame);
			ser.Sync(nameof(exram_mode), ref exram_mode);
			ser.Sync(nameof(chr_mode), ref chr_mode);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			ser.Sync(nameof(chr_reg_high), ref chr_reg_high);
			ser.Sync(nameof(ab_mode), ref ab_mode);
			ser.Sync(nameof(regs_a), ref regs_a, false);
			ser.Sync(nameof(regs_b), ref regs_b, false);
			ser.Sync(nameof(regs_prg), ref regs_prg, false);
			ser.Sync(nameof(nt_modes), ref nt_modes, false);
			ser.Sync(nameof(nt_fill_tile), ref nt_fill_tile);
			ser.Sync(nameof(nt_fill_attrib), ref nt_fill_attrib);
			ser.Sync(nameof(wram_bank), ref wram_bank);
			ser.Sync(nameof(last_nt_read), ref last_nt_read);
			ser.Sync(nameof(EXRAM), ref EXRAM, false);

			SyncPRGBanks();
			SyncCHRBanks();
			SyncMultiplier();
			SyncIRQ();
			audio.SyncState(ser);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER005":
					Cart.WramSize = 64;
					break;
				case "NES-ELROM": //Castlevania 3 - Dracula's Curse (U)
				case "HVC-ELROM":
					AssertPrg(128, 256); AssertChr(128);
					break;
				case "NES-EKROM": //Gemfire (U)
					AssertPrg(256); AssertChr(256);
					break;
				case "HVC-EKROM":
					break;
				case "NES-ETROM":
				case "HVC-ETROM":
					break;
				case "NES-EWROM":
				case "HVC-EWROM":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;

			if (Cart.ChrSize > 0)
				chr_bank_mask_1k = Cart.ChrSize - 1;
			else
				chr_bank_mask_1k = Cart.VramSize - 1;

			PoweronState();

			if (NES.apu != null)
				audio = new MMC5Audio(NES.apu.ExternalQueue, e => { irq_audio = e; SyncIRQ(); });

			return true;
		}

		private void PoweronState()
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

		private int PRGGetBank(int addr, out bool ram)
		{
			int bank_8k = addr >> 13;
			bank_8k = _prgBanks8K[bank_8k];
			ram = (bank_8k & 0x80) == 0;
			if (!ram)
				bank_8k &= prg_bank_mask_8k;
			return bank_8k;
		}

		// wram:
		// [.... .CBB]
		// C = chip select
		// B = bank select (8K banks)
		// the following configurations are known:
		// 1) no wram
		// 2) 8K wram: 1x 8K
		// 3) 16K wram: 2x 8K
		// 4) 32K wram: 1x 32K
		//
		// for iNES, we assume 64K wram
		private int? MaskWRAM(int bank)
		{
			bank &= 7;
			switch (Cart.WramSize)
			{
				case 0:
					return null;
				case 8:
					if (bank >= 4)
						return null;
					else
						return 0;
				case 16:
					return bank >> 2;
				case 32:
					if (bank >= 4)
						return null;
					else
						return bank & 3;
				case 64:
					return bank;
				default:
					throw new Exception();
			}
		}

		private void WriteWRAMActual(int bank, int offs, byte value)
		{
			int? bbank = MaskWRAM(bank);
			if (bbank.HasValue)
				Wram[(int)bbank << 13 | offs] = value;
		}

		private byte ReadWRAMActual(int bank, int offs)
		{
			int? bbank = MaskWRAM(bank);
			return bbank.HasValue
				? Wram[(int)bbank << 13 | offs]
				: NES.DB;
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

		private int MapCHR(int addr)
		{
			int bank_1k = addr >> 10;
			int ofs = addr & ((1 << 10) - 1);

			if (exram_mode == 1 && NES.ppu.ppuphase == PPU.PPU_PHASE_BG)
			{
				int exram_addr = last_nt_read;
				int bank_4k = EXRAM[exram_addr] & 0x3F;

				bank_1k = bank_4k * 4;
				// low 12 bits of address come from PPU
				// next 6 bits of address come from exram table
				// top 2 bits of address come from chr_reg_high
				bank_1k += chr_reg_high << 8;
				ofs = addr & (4 * 1024 - 1);

				bank_1k &= chr_bank_mask_1k;
				addr = (bank_1k << 10) | ofs;
				return addr;
			}

			if (NES.ppu.reg_2000.obj_size_16)
			{
				bool isPattern = NES.ppu.PPUON;
				if (NES.ppu.ppuphase == PPU.PPU_PHASE_OBJ && isPattern)
					bank_1k = _aBanks1K[bank_1k];
				else if (NES.ppu.ppuphase == PPU.PPU_PHASE_BG && isPattern)
					bank_1k = _bBanks1K[bank_1k];
				else
				{
					bank_1k = ab_mode == 0
						? _aBanks1K[bank_1k]
						: _bBanks1K[bank_1k];
				}
			}
			else
			{
				bank_1k = _aBanks1K[bank_1k];
			}
		
			bank_1k &= chr_bank_mask_1k;
			addr = (bank_1k<<10)|ofs;
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				return (Vrom ?? Vram)[addr];
			}

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
			int nt = (addr >> 10) & 3; // &3 to read from the NT mirrors at 3xxx
			int offset = addr & ((1 << 10) - 1);
			nt = nt_modes[nt];
			switch (nt)
			{
				case 0: //NES internal NTA
					return NES.CIRAM[offset];
				case 1: //NES internal NTB
					return NES.CIRAM[0x400 | offset];
				case 2: //use ExRAM as NT
					//TODO - additional r/w security
					if (exram_mode >= 2)
						return 0;
					else
						return EXRAM[offset];
				case 3: // Fill Mode
					if (offset >= 0x3c0)
						return nt_fill_attrib;
					else
						return nt_fill_tile;
				default: throw new Exception();
			}
		}

		public override byte PeekPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				return (Vrom ?? Vram)[addr];
			}

			addr -= 0x2000;
			int nt_entry = addr & 0x3FF;
			if (nt_entry < 0x3C0)
			{
				//track the last nametable entry read so that subsequent pattern and attribute reads will know which exram address to use
				//last_nt_read = nt_entry;
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
			int nt = (addr >> 10) & 3; // &3 to read from the NT mirrors at 3xxx
			int offset = addr & ((1 << 10) - 1);
			nt = nt_modes[nt];
			switch (nt)
			{
				case 0: //NES internal NTA
					return NES.CIRAM[offset];
				case 1: //NES internal NTB
					return NES.CIRAM[0x400 | offset];
				case 2: //use ExRAM as NT
					//TODO - additional r/w security
					if (exram_mode >= 2)
						return 0;
					else
						return EXRAM[offset];
				case 3: // Fill Mode
					if (offset >= 0x3c0)
						return nt_fill_attrib;
					else
						return nt_fill_tile;
				default: throw new Exception();
			}
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (Vram != null)
					Vram[MapCHR(addr)] = value;
			}
			else
			{
				addr -= 0x2000;
				int nt = (addr >> 10) & 3; // &3 to read from the NT mirrors at 3xxx
				int offset = addr & ((1 << 10) - 1);
				nt = nt_modes[nt];
				switch (nt)
				{
					case 0: //NES internal NTA
						NES.CIRAM[offset] = value;
						break;
					case 1: //NES internal NTB
						NES.CIRAM[0x400 | offset] = value;
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

		public override void WriteWram(int addr, byte value) => WriteWRAMActual(wram_bank, addr & 0x1fff, value);

		public override byte ReadWram(int addr) => ReadWRAMActual(wram_bank, addr & 0x1fff);

		public override byte ReadPrg(int addr)
		{
			byte ret;
			int offs = addr & 0x1fff;
			int bank = PRGGetBank(addr, out var ram);

			if (ram)
				ret = ReadWRAMActual(bank, offs);
			else
				ret = Rom[bank << 13 | offs];
			if (addr < 0x4000)
				audio.ReadROMTrigger(ret);
			return ret;
		}

		public override byte PeekCart(int addr)
		{
			if (addr >= 0x8000)
				return PeekPRG(addr - 0x8000);
			if (addr >= 0x6000)
				return ReadWram(addr - 0x6000);
			return PeekEXP(addr - 0x4000);
		}

		public byte PeekPRG(int addr)
		{
			byte ret;
			int offs = addr & 0x1fff;
			int bank = PRGGetBank(addr, out var ram);

			if (ram)
				ret = ReadWRAMActual(bank, offs);
			else
				ret = Rom[bank << 13 | offs];
			//if (addr < 0x4000)
			//	audio.ReadROMTrigger(ret);
			return ret;
		}

		public override void WritePrg(int addr, byte value)
		{
			int bank = PRGGetBank(addr, out var ram);
			if (ram)
				WriteWRAMActual(bank, addr & 0x1fff, value);
		}

		public override void WriteExp(int addr, byte value)
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
					nt_fill_attrib = (byte)(value & 3);
					// extend out to fill all 4 positions
					nt_fill_attrib |= (byte)(nt_fill_attrib << 2);
					nt_fill_attrib |= (byte)(nt_fill_attrib << 4);
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

		private void SyncMultiplier()
		{
			int result = multiplicand*multiplier;
			product_low = (byte)(result&0xFF);
			product_high = (byte)((result>>8) & 0xFF);
		}

		public override byte ReadExp(int addr)
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

			return ret;
		}

		public byte PeekEXP(int addr)
		{
			byte ret = 0xFF;
			switch (addr)
			{
				case 0x1204: //$5204:  [E... ....]    IRQ Enable (0=disabled, 1=enabled)
					ret = (byte)((irq_pending ? 0x80 : 0) | (in_frame ? 0x40 : 0));
					//irq_pending = false;
					//SyncIRQ();
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
					ret = audio.Peek5010();
					break;
			}

			//TODO - additional r/w timing security
			if (addr >= 0x1C00)
			{
				if (exram_mode < 2)
					ret = 0xFF;
				else ret = EXRAM[addr - 0x1C00];
			}

			return ret;
		}

		private void SyncIRQ()
		{
			IrqSignal = (irq_pending && irq_enabled) || irq_audio;
		}

		public override void ClockPpu()
		{
			if (NES.ppu.ppur.status.cycle != 3)
				return;

			int sl = NES.ppu.ppur.status.sl;

			if (!NES.ppu.PPUON || sl >= 241)
			{
				// whenever rendering is off for any reason (vblank or forced disable
				// the irq counter resets, as well as the inframe flag (easily verifiable from software)
				in_frame = false;
				irq_counter = 0;
				irq_pending = false;
				SyncIRQ();
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
				if (irq_counter == (irq_target + 1))
				{
					irq_pending = true;
					SyncIRQ();
				}
			}

		}

		public override void ClockCpu()
		{
			audio.Clock();
		}

		private void SetBank(int[] target, int offset, int size, int value)
		{
			value &= ~(size-1);
			for (int i = 0; i < size; i++)
			{
				int index = i+offset;
				target[index] = value;
				value++;
			}
		}

		private void SyncPRGBanks()
		{
			switch (prg_mode)
			{
				case 0:
					SetBank(_prgBanks8K, 0, 4, regs_prg[3]&~3);
					break;
				case 1:
					SetBank(_prgBanks8K, 0, 2, regs_prg[1] & ~1);
					SetBank(_prgBanks8K, 2, 2, regs_prg[3] & ~1);
					break;
				case 2:
					SetBank(_prgBanks8K, 0, 2, regs_prg[1] & ~1);
					SetBank(_prgBanks8K, 2, 1, regs_prg[2]);
					SetBank(_prgBanks8K, 3, 1, regs_prg[3]);
					break;
				case 3:
					SetBank(_prgBanks8K, 0, 1, regs_prg[0]);
					SetBank(_prgBanks8K, 1, 1, regs_prg[1]);
					SetBank(_prgBanks8K, 2, 1, regs_prg[2]);
					SetBank(_prgBanks8K, 3, 1, regs_prg[3]);
					break;
			}
		}

		private void SyncCHRBanks()
		{
			//MASTER LOGIC: something like this this might be enough to work, but i'll play with it later
			//bank_1k >> (3 - chr_mode) << chr_mode | bank_1k & ( etc.etc.

			//TODO - do these need to have the last arguments multiplied by 8,4,2 to map to the right banks?
			switch (chr_mode)
			{
				case 0:
					SetBank(_aBanks1K, 0, 8, regs_a[7] * 8);
					SetBank(_bBanks1K, 0, 8, regs_b[3] * 8);
					break;
				case 1:
					SetBank(_aBanks1K, 0, 4, regs_a[3] * 4);
					SetBank(_aBanks1K, 4, 4, regs_a[7] * 4);
					SetBank(_bBanks1K, 0, 4, regs_b[3] * 4);
					SetBank(_bBanks1K, 4, 4, regs_b[3] * 4);
					break;
				case 2:
					SetBank(_aBanks1K, 0, 2, regs_a[1] * 2);
					SetBank(_aBanks1K, 2, 2, regs_a[3] * 2);
					SetBank(_aBanks1K, 4, 2, regs_a[5] * 2);
					SetBank(_aBanks1K, 6, 2, regs_a[7] * 2);
					SetBank(_bBanks1K, 0, 2, regs_b[1] * 2);
					SetBank(_bBanks1K, 2, 2, regs_b[3] * 2);
					SetBank(_bBanks1K, 4, 2, regs_b[1] * 2);
					SetBank(_bBanks1K, 6, 2, regs_b[3] * 2);
					break;
				case 3:
					SetBank(_aBanks1K, 0, 1, regs_a[0]);
					SetBank(_aBanks1K, 1, 1, regs_a[1]);
					SetBank(_aBanks1K, 2, 1, regs_a[2]);
					SetBank(_aBanks1K, 3, 1, regs_a[3]);
					SetBank(_aBanks1K, 4, 1, regs_a[4]);
					SetBank(_aBanks1K, 5, 1, regs_a[5]);
					SetBank(_aBanks1K, 6, 1, regs_a[6]);
					SetBank(_aBanks1K, 7, 1, regs_a[7]);
					SetBank(_bBanks1K, 0, 1, regs_b[0]);
					SetBank(_bBanks1K, 1, 1, regs_b[1]);
					SetBank(_bBanks1K, 2, 1, regs_b[2]);
					SetBank(_bBanks1K, 3, 1, regs_b[3]);
					SetBank(_bBanks1K, 4, 1, regs_b[0]);
					SetBank(_bBanks1K, 5, 1, regs_b[1]);
					SetBank(_bBanks1K, 6, 1, regs_b[2]);
					SetBank(_bBanks1K, 7, 1, regs_b[3]);
					break;
			}
		}
	}
}
