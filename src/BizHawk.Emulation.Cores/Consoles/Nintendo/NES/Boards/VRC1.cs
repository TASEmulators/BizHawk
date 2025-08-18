﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//AKA mapper 75
	internal sealed class VRC1 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_8k;
		private int chr_bank_mask_4k;

		//state
		private int[] prg_banks_8k = new int[4];
		private int[] chr_banks_4k = new int[2];
		private readonly int[] chr_regs_4k = new int[2];

		//the VS actually does have 2 KB of nametable address space
		//let's make the extra space here, instead of in the main NES to avoid confusion
		private byte[] CIRAM_VS = new byte[0x800];

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_banks_8k), ref prg_banks_8k, false);
			ser.Sync(nameof(chr_banks_4k), ref chr_banks_4k, false);
			if (NES.IsVS)
			{
				ser.Sync("VS_CIRAM", ref CIRAM_VS, false);
			}

			for (int i = 0; i < 2; i++) ser.Sync("chr_regs_4k_" + i, ref chr_regs_4k[i]);

			if (ser.IsReader)
			{
				SyncCHR();
			}
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER075":
					break;
				case "MAPPER075VS":
					NES._isVS = true;
					//update the state of the dip switches
					//this is only done at power on
					NES.VS_dips[0] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_1 ? 1 : 0);
					NES.VS_dips[1] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_2 ? 1 : 0);
					NES.VS_dips[2] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_3 ? 1 : 0);
					NES.VS_dips[3] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_4 ? 1 : 0);
					NES.VS_dips[4] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_5 ? 1 : 0);
					NES.VS_dips[5] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_6 ? 1 : 0);
					NES.VS_dips[6] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_7 ? 1 : 0);
					NES.VS_dips[7] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_8 ? 1 : 0);
					break;
				case "KONAMI-VRC-1":
				case "JALECO-JF-20":
				case "JALECO-JF-22":
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.PrgSize / 8 - 1;
			chr_bank_mask_4k = Cart.ChrSize / 4 - 1;

			SetMirrorType(EMirrorType.Vertical);

			prg_banks_8k[3] = (byte)(0xFF & prg_bank_mask_8k);

			return true;
		}
		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return Rom[addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_4k = addr >> 12;
				int ofs = addr & ((1 << 12) - 1);
				bank_4k = chr_banks_4k[bank_4k];
				bank_4k &= chr_bank_mask_4k;
				addr = (bank_4k << 12) | ofs;
				return Vrom[addr];
			}
			else
			{
				if (NES._isVS)
				{
					addr -= 0x2000;
					if (addr < 0x800)
					{
						return NES.CIRAM[addr];
					}
					else
					{
						return CIRAM_VS[addr - 0x800];
					}
				}
				else
					return base.ReadPpu(addr);
			}
		}

		public override void WritePpu(int addr, byte value)
		{
			// The game VS Goonies apparently scans for more CIRAM then actually exists, so we have to mask out nonsensical values
			addr &= 0x2FFF;

			if (NES._isVS)
			{
				if (addr < 0x2000)
				{
					if (Vram != null)
						Vram[addr] = value;
				}
				else
				{
					addr -= 0x2000;
					if (addr < 0x800)
					{
						NES.CIRAM[addr] = value;
					}
					else
					{
						CIRAM_VS[addr - 0x800] = value;
					}
				}
			}
			else
				base.WritePpu(addr, value);
		}

		private void SyncCHR()
		{
			chr_banks_4k[0] = chr_regs_4k[0] & chr_bank_mask_4k;
			chr_banks_4k[1] = chr_regs_4k[1] & chr_bank_mask_4k;
		}

		public override void WritePrg(int addr, byte value)
		{
			switch (addr & 0xF000)
			{
				//0x0F mask on value was removed
				//technically its out of specs, but some hacks will be adding banks
				//however, if the game has normal hardware, it has a normal (16 and less) number of prg banks, and the prg_bank_mask_8k will take care of it
				case 0x0000: prg_banks_8k[0] = value & prg_bank_mask_8k; break;
				case 0x2000: prg_banks_8k[1] = value & prg_bank_mask_8k; break;
				case 0x4000: prg_banks_8k[2] = value & prg_bank_mask_8k; break;

				case 0x1000: //[.... .BAM]   Mirroring, CHR reg high bits
					if(value.Bit(0))
						SetMirrorType(EMirrorType.Horizontal);
					else
						SetMirrorType(EMirrorType.Vertical);
					chr_regs_4k[0] &= 0x0F;
					chr_regs_4k[1] &= 0x0F;
					if (value.Bit(1)) chr_regs_4k[0] |= 0x10;
					if (value.Bit(2)) chr_regs_4k[1] |= 0x10;
					SyncCHR();
					break;

				case 0x6000:
					chr_regs_4k[0] = (chr_regs_4k[0] & 0xF0) | (value & 0x0F);
					SyncCHR();
					break;
				case 0x7000:
					chr_regs_4k[1] = (chr_regs_4k[1] & 0xF0) | (value & 0x0F);
					SyncCHR();
					break;
			}
		}
	}
}
