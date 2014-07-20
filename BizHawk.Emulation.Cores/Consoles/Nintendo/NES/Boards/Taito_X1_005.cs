using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Mapper 80.  TLSROM variant is Mapper 207

	/*
	 * Bakushou!! Jinsei Gekijou 2 爆笑！！人生劇場２
	 * Fudou Myouou Den 不動明王伝 <=== TLSROM VARIANT!!!
	 * Kyonshiizu 2 キョンシーズ２
	 * Kyuukyoku Harikiri Stadium 究極ハリキリスタジアム
	 * Minelvaton Saga: Ragon no Fukkatsu ミネルバトンサーガ ラゴンの復活
	 * Mirai Shinwa Jarvas 未来神話ジャーヴァス
	 * Taito Grand Prix: Eikou e no License タイトーグランプリ 栄光へのライセンス
	 * Yamamura Misa Suspense: Kyouto Ryuu no Tera Satsujin Jiken 山村美紗サスペンス 京都龍の寺殺人事件
	 */

	/*
	 * Registers should be masked with $ff7f.
	 * 
	 * $7e70: 2k chr @ PPU $0000 (lsb ignored)
	 * $7e71: 2k chr @ PPU $0800 (lsb ignored)
	 * $7e72: 1k chr @ PPU $1000
	 * $7e73: 1k chr @ PPU $1400
	 * $7e74: 1k chr @ PPU $1800
	 * $7e75: 1k chr @ PPU $1c00
	 * $7e76,$7e77: mirroring.  bit0: 0 = H, 1 = V
	 * $7e78,$7e79: prg wram protect.  must be $a3 to enable read+write
	 * $7e7a,$7e7b: 8k prg @ cpu $8000
	 * $7e7c,$7e7d: 8k prg @ cpu $a000
	 * $7e7e,$7e7f: 8k prg @ cpu $c000
	 * 
	 * $7f00:$7f7f: 128 bytes internal prg ram, mirrored on CPU A7
	 * 
	 * 8k prg @ cpu $e000 is fixed to last 8k of rom.
	 * in TLSROM-like mode (mapper 207), mirroring reg is ignored,
	 *   and top bit of CHR regs (normally CHRROM A17) is used as CIRAM A10
	 * 
	 */

	public sealed class TAITO_X1_005 : NES.NESBoardBase
	{
		// config
		int prg_bank_mask, chr_bank_mask;
		bool tlsrewire = false;
		// state
		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		ByteBuffer prg_regs_8k = new ByteBuffer(4);
		bool wramenable = false;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr_regs_1k", ref chr_regs_1k);
			ser.Sync("prg_regs_8k", ref prg_regs_8k);
			ser.Sync("wramenable", ref wramenable);
		}

		public override void Dispose()
		{
			base.Dispose();
			chr_regs_1k.Dispose();
			prg_regs_8k.Dispose();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER080":
					break;
				case "MAPPER207":
					tlsrewire = true;
					break;
				case "TAITO-X1-005":
					if (Cart.pcb == "アシユラー")
						tlsrewire = true;
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);
			chr_bank_mask = Cart.chr_size / 1 - 1;
			prg_bank_mask = Cart.prg_size / 8 - 1;
			
			// the chip has 128 bytes of WRAM built into it, which we have to instantiate ourselves
			Cart.wram_size = 0;

			prg_regs_8k[3] = 0xFF;
			return true;
		}

		public override void PostConfigure()
		{
			WRAM = new byte[128];
			base.PostConfigure();
		}

		public override void WriteWRAM(int addr, byte value)
		{
			addr &= 0x1f7f;

			if (addr >= 0x1f00)
			{
				if (wramenable)
					WRAM[addr & 0x7f] = value;
				return;
			}

			switch (addr)
			{
				case 0x1E76:
				case 0x1E77:
					if (value.Bit(0))
						SetMirrorType(EMirrorType.Vertical);
					else
						SetMirrorType(EMirrorType.Horizontal);
					break;

				case 0x1E70:
					chr_regs_1k[0] = (byte)(value & ~1);
					chr_regs_1k[1] = (byte)(value | 1);
					break;
				case 0x1E71:
					chr_regs_1k[2] = (byte)(value & ~1);
					chr_regs_1k[3] = (byte)(value | 1);
					break;

				case 0x1E72:
					chr_regs_1k[4] = value;
					break;
				case 0x1E73:
					chr_regs_1k[5] = value;
					break;
				case 0x1E74:
					chr_regs_1k[6] = value;
					break;
				case 0X1E75:
					chr_regs_1k[7] = value;
					break;

				case 0x1E78:
				case 0x1E79:
					wramenable = value == 0xa3;
					break;

				case 0x1E7A: //PRG Reg 0
				case 0x1E7B:
					prg_regs_8k[0] = value;
					break;
				case 0x1E7C: //PRG Reg 1
				case 0x1E7D:
					prg_regs_8k[1] = value;
					break;
				case 0x1E7E: //PRG Reg 2
				case 0x1E7F:
					prg_regs_8k[2] = value;
					break;
			}
		}

		public override byte ReadWRAM(int addr)
		{
			if (addr >= 0x1f00 && wramenable)
				return WRAM[addr & 0x7f];
			else
				return NES.DB;
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_regs_8k[bank_8k];
			bank_8k &= prg_bank_mask;
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_regs_1k[bank_1k];
				bank_1k &= chr_bank_mask;
				addr = (bank_1k << 10) | ofs;
				return VROM[addr];
			}
			else if (tlsrewire)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_regs_1k[bank_1k & 7]; // ignore PPU A13
				bank_1k >>= 7; // top bit is used
				addr = (bank_1k << 10) | ofs;
				return NES.CIRAM[addr];
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr >= 0x2000)
			{
				if (tlsrewire)
				{
					int bank_1k = addr >> 10;
					int ofs = addr & ((1 << 10) - 1);
					bank_1k = chr_regs_1k[bank_1k & 7]; // ignore PPU A13
					bank_1k >>= 7; // top bit is used
					addr = (bank_1k << 10) | ofs;
					NES.CIRAM[addr] = value;
				}
				else
				{
					base.WritePPU(addr, value);
				}
			}
		}
	}
}
