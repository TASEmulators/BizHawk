using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// AKA mapper 19
	// the similar mapper 210 has no sound, no irq, and a different nametable setup
	// as of Jan 2014, bootgod has the 210 separated from the 19 correctly; but "in the wild"
	// most things are labeled 19.

	// to further complicate matters, some 210 roms write to both sets of NT regs so that they
	// will work emulated either way.  but some don't, and must be emulated differently

	// what we have here should work for everything that's actually a 129 or 163,
	// and some of the 175/340 (mapper 210)
	[NES.INESBoardImplPriority]
	public sealed class Namcot129_163 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k;
		int chr_bank_mask_1k;

		//state
		IntBuffer prg_banks_8k = new IntBuffer(4);
		IntBuffer chr_banks_1k = new IntBuffer(12);
		bool[] vram_enable = new bool[3];

		int irq_counter;
		bool irq_enabled;
		bool irq_pending;
		bool audio_disable = true;

		Namco163Audio audio;
		int audio_cycles;

		byte prgram_write = 0;

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("chr_banks_1k", ref chr_banks_1k);
			for (int i = 0; i < vram_enable.Length; i++)
				ser.Sync("vram_enable_" + i, ref vram_enable[i]);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_pending", ref irq_pending);
			SyncIRQ();
			ser.Sync("audio_cycles", ref audio_cycles);
			audio.SyncState(ser);
			ser.Sync("audio_disable", ref audio_disable);
			ser.Sync("prgram_write", ref prgram_write);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER019":
					AssertVram(0);
					break;

				case "NAMCOT-129": // star wars
				// no known differences between 129 and 163
				case "NAMCOT-163":
					//final lap
					//battle fleet
					//dragon ninja
					//famista '90
					//hydelide 3 - this is a good test of more advanced features
					AssertPrg(128, 256); AssertChr(128, 256); AssertVram(0); AssertWram(0, 8);
					break;

				default:
					return false;
			}

			if (NES.apu != null)
				audio = new Namco163Audio(NES.apu.ExternalQueue);

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size / 1 - 1;

			prg_banks_8k[3] = (byte)(0xFF & prg_bank_mask_8k);
			prg_banks_8k[2] = (byte)(0xFF & prg_bank_mask_8k)-1;
			chr_banks_1k[8] = chr_banks_1k[10] = 0xFE;
			chr_banks_1k[9] = chr_banks_1k[11] = 0xFF;

			vram_enable[2] = true;

			return true;
		}

		public override byte ReadEXP(int addr)
		{
			addr &= 0xF800;
			switch (addr)
			{
				case 0x0800:
					return audio.ReadData();
				case 0x1000:
					return (byte)(irq_counter & 0xFF);
				case 0x1800:
					return (byte)((irq_counter >> 8) | (irq_enabled ? 0x8000 : 0));
			}
			return base.ReadEXP(addr);
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr &= 0xF800;
			switch (addr)
			{
				case 0x0800:
					audio.WriteData(value);
					break;
				case 0x1000:
					irq_counter = (irq_counter & 0xFF00) | value;
					irq_pending = false;
					SyncIRQ();
					break;
				case 0x1800:
					{
						irq_counter = (irq_counter & 0x00FF) | (((value & 0x7F) << 8));
						bool last_enabled = irq_enabled;
						irq_enabled = value.Bit(7);
						irq_pending = false;
						SyncIRQ();
						break;
					}
			}
		}


		public override void WritePRG(int addr, byte value)
		{
			addr &= 0xF800;
			switch (addr)
			{
				case 0x0000: chr_banks_1k[0] = value; break;
				case 0x0800: chr_banks_1k[1] = value; break;
				case 0x1000: chr_banks_1k[2] = value; break;
				case 0x1800: chr_banks_1k[3] = value; break;
				case 0x2000: chr_banks_1k[4] = value; break;
				case 0x2800: chr_banks_1k[5] = value; break;
				case 0x3000: chr_banks_1k[6] = value; break;
				case 0x3800: chr_banks_1k[7] = value; break;
				case 0x4000: chr_banks_1k[8] = value; break;
				case 0x4800: chr_banks_1k[9] = value; break;
				case 0x5000: chr_banks_1k[10] = value; break;
				case 0x5800: chr_banks_1k[11] = value; break;

				case 0x6000: //$E000
					prg_banks_8k[0] = (value & 0x3F) & prg_bank_mask_8k;
					audio_disable = value.Bit(6);
					break;
				case 0x6800: //$E800
					prg_banks_8k[1] = (value & 0x3F) & prg_bank_mask_8k;
					vram_enable[0] = !value.Bit(6);
					vram_enable[1] = !value.Bit(7);
					break;
				case 0x7000: //$F000
					prg_banks_8k[2] = (value & 0x3F) & prg_bank_mask_8k;
					break;
				case 0x7800: //$F800
					audio.WriteAddr(value);
					prgram_write = value; // yes, same port
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}


		public override void WritePPU(int addr, byte value)
		{
			int bank_1k = addr >> 10;
			if (bank_1k >= 12)
				bank_1k -= 4; // mirror 3000:3fff to 2000:2fff
			int ofs = addr & ((1 << 10) - 1);
			bool useciram = vram_enable[bank_1k >> 2];
			bank_1k = chr_banks_1k[bank_1k];

			if (useciram && bank_1k >= 0xe0)
			{
				bank_1k &= 1;
				NES.CIRAM[bank_1k << 10 | ofs] = value;
			}
			else
			{
				// mapped to VROM; nothing to do
			}
		}
		public override byte ReadPPU(int addr)
		{
			int bank_1k = addr >> 10;
			if (bank_1k >= 12)
				bank_1k -= 4; // mirror 3000:3fff to 2000:2fff
			int ofs = addr & ((1 << 10) - 1);
			bool useciram = vram_enable[bank_1k >> 2];
			bank_1k = chr_banks_1k[bank_1k];

			if (useciram && bank_1k >= 0xe0)
			{
				bank_1k &= 1;
				return NES.CIRAM[bank_1k << 10 | ofs];
			}
			else
			{
				bank_1k &= chr_bank_mask_1k;
				return VROM[bank_1k << 10 | ofs];
			}
		}

		void SyncIRQ()
		{
			IRQSignal = (irq_pending && irq_enabled);
		}

		void TriggerIRQ()
		{
			//NES.LogLine("trigger irq");
			irq_pending = true;
			SyncIRQ();
		}

		void ClockIRQ()
		{
			if (irq_counter == 0x7FFF)
			{
				//irq_counter = 0;
				TriggerIRQ();
				//irq_enabled = false;
			}
			else irq_counter++;
		}

		public override void ClockCPU()
		{
			if (irq_enabled)
			{
				ClockIRQ();
			}
			if (!audio_disable)
			{
				audio_cycles++;
				if (audio_cycles == 15)
				{
					audio_cycles = 0;
					audio.Clock();
				}
			}
		}

		public override byte[] SaveRam
		{
			get
			{
				if (Cart.wram_battery)
				{
					if (WRAM != null)
						return WRAM;
					else
						return audio.GetSaveRam();
				}
				else
				{
					return null;
				}
			}
		}

		public override void WriteWRAM(int addr, byte value)
		{
			// top 4 bits must be in this arrangement to write at all
			if ((prgram_write & 0xf0) == 0x40)
			{
				// then the bit corresponding to the 2K subsection must be 0
				if (!prgram_write.Bit(addr >> 11))
				{
					base.WriteWRAM(addr, value);
				}
			}
		}
	}
}
