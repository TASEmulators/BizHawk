namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper219 : MMC3Board_Base
	{
		public byte[] exregs = new byte[3];
		public byte[] prgregs = new byte[4];
		public byte[] chrregs = new byte[8];

		public byte bits_rev, reg_value;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER219":
					break;
				default:
					return false;
			}

			BaseSetup();

			prgregs[1] = 1;
			prgregs[2] = 2;
			prgregs[3] = 3;

			byte r0_0 = (byte)(0 & ~1);
			byte r0_1 = (byte)(0 | 1);
			byte r1_0 = (byte)(2 & ~1);
			byte r1_1 = (byte)(2 | 1);


			chrregs[0] = 4;
			chrregs[1] = 5;
			chrregs[2] = 6;
			chrregs[3] = 7;
			chrregs[4] = r0_0;
			chrregs[5] = r0_1;
			chrregs[6] = r1_0;
			chrregs[7] = r1_1;
			/*
			chr_regs_1k[0] = r0_0;
			chr_regs_1k[1] = r0_1;
			chr_regs_1k[2] = r1_0;
			chr_regs_1k[3] = r1_1;
			chr_regs_1k[4] = regs[2];
			chr_regs_1k[5] = regs[3];
			chr_regs_1k[6] = regs[4];
			chr_regs_1k[7] = regs[5];*/

			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr<0x2000)
			{
				switch ((addr + 0x8000) & 0xE003)
				{
					case 0x8000:
						exregs[1] = value;
						exregs[0] = 0;
						break;
					case 0x8002:
						exregs[0] = value;
						exregs[1] = 0;
						break;
					case 0x8001:
						reg_value = value;
						bits_rev = (byte)(((value & 0x20) >> 5) | ((value & 0x10) >> 3) | ((value & 0x08) >> 1) | ((value & 0x04) << 1));
						switch (exregs[0])
						{
							case 0x26: prgregs[0] = bits_rev; break;
							case 0x25: prgregs[1] = bits_rev; break;
							case 0x24: prgregs[2] = bits_rev; break;
							case 0x23: prgregs[3] = bits_rev; break;
						}
						switch (exregs[1])
						{
							case 0x08:
							case 0x0A:
							case 0x0E:
							case 0x12:
							case 0x16:
							case 0x1A:
							case 0x1E: exregs[2] = (byte)(value << 4); break;
							case 0x09: chrregs[0] = (byte)(exregs[2] | (value >> 1 & 0xE)); break;
							case 0x0b: chrregs[1] = (byte)(exregs[2] | (value >> 1) | 1); break;
							case 0x0c:
							case 0x0d: chrregs[2] = (byte)(exregs[2] | (value >> 1 & 0xE)); break;
							case 0x0f: chrregs[3] = (byte)(exregs[2] | (value >> 1) | 1); break;
							case 0x10:
							case 0x11: chrregs[4] = (byte)(exregs[2] | (value >> 1)); break;
							case 0x14:
							case 0x15: chrregs[5] = (byte)(exregs[2] | (value >> 1)); break;
							case 0x18:
							case 0x19: chrregs[6] = (byte)(exregs[2] | (value >> 1)); break;
							case 0x1c:
							case 0x1d: chrregs[7] = (byte)(exregs[2] | (value >> 1)); break;
						}
						break;
				}
			}		
			else 
				base.WritePRG(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_prg = addr >> 13;
			bank_prg = prgregs[bank_prg];
			return ROM[((bank_prg << 13) + (addr & 0x1FFF))];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr<0x2000)
			{
				int bank_chr = addr >> 10;
				bank_chr = chrregs[bank_chr];
				return VRAM[((bank_chr << 10) + (addr & 0x3FF))];
			}
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_chr = addr >> 10;
				bank_chr = chrregs[bank_chr];
				VRAM[((bank_chr << 10) + (addr & 0x3FF))]=value;
			}
			else
				base.WritePPU(addr, value);
		}
	}
}
