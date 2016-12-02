using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper243 : NES.NESBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_243

		int reg_addr;
		bool var_a;
		ByteBuffer regs = new ByteBuffer(8);
		int chr_bank_mask_8k, prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER243":
					break;
				case "UNIF_UNL-Sachen-74LS374N":
					var_a = true;
					break;
				default:
					return false;
			}
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			return true;
		}

		public override void Dispose()
		{
			regs.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg_addr", ref reg_addr);
			ser.Sync("regs", ref regs);
			base.SyncState(ser);
		}

		public override void WriteEXP(int addr, byte value)
		{
			switch (addr & 0x01)
			{
				case 0x0000:
					reg_addr = value & 0x07;
					break;
				case 0x0001:
					if (var_a)
					{
						switch (reg_addr)
						{
							case 0:
								// set prg bank to 0
								regs[5] = 0;
								// set chr bank to 3
								regs[2] = 0;
								regs[4] = 3;
								regs[6] = 0;
								break;
							case 2:
								regs[2] = (byte)(value & 0x01);
								break;
							case 4:
								regs[4] = (byte)(value & 0x01);
								break;
							case 5:
								regs[5] = (byte)(value & 0x07);
								break;
							case 6:
								regs[6] = (byte)(value & 0x03);
								break;
							case 7:
								int mirror = value & 1;
								switch (mirror)
								{
									case 0:
										SetMirrorType(EMirrorType.Horizontal);
										break;
									case 1:
										SetMirrorType(EMirrorType.Vertical);
										break;
								}
								break;
						}
						break;
					}
					else
					{
						switch (reg_addr)
						{
							case 2:
								regs[2] = (byte)(value & 0x01);
								regs[5] = (byte)(value & 0x01);
								break;
							case 4:
								regs[4] = (byte)(value & 0x01);
								break;
							case 5:
								regs[5] = (byte)(value & 0x07);
								break;
							case 6:
								regs[6] = (byte)(value & 0x03);
								break;
							case 7:
								int mirror = (value >> 1) & 0x03;
								switch (mirror)
								{
									case 0:
										SetMirrorType(EMirrorType.Horizontal);
										break;
									case 1:
										SetMirrorType(EMirrorType.Vertical);
										break;
									case 2:
										SetMirroring(0, 1, 1, 1);
										break;
									case 3:
										SetMirrorType(EMirrorType.OneScreenA);
										break;
								}
								break;
						}
						break;
					}
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (var_a)
			{
				if (addr < 0x2000)
				{
					int chr_bank = regs[4] | (regs[6] << 1) | (regs[2] << 3);

					return VROM[((chr_bank & chr_bank_mask_8k) * 0x2000) + addr];
				}
				else
				{
					return base.ReadPPU(addr);
				}
			}
			else
			{
				if (addr < 0x2000)
				{
					int chr_bank = (regs[4] << 2) | (regs[6]) | (regs[2] << 3);

					return VROM[((chr_bank & chr_bank_mask_8k) * 0x2000) + addr];
				}
				else
				{
					return base.ReadPPU(addr);
				}
			}		
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[((regs[5] & prg_bank_mask_32k) * 0x8000) + addr];
		}
	}
}
