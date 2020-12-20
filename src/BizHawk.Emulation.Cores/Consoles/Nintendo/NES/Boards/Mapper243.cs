using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper243 : NesBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_243

		private int reg_addr;
		private bool var_a;
		private byte[] regs = new byte[8];
		private int chr_bank_mask_8k, prg_bank_mask_32k;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER243":
					break;
				case "UNIF_UNL-Sachen-74LS374N":
					var_a = true;
					break;
				default:
					return false;
			}
			chr_bank_mask_8k = Cart.ChrSize / 8 - 1;
			prg_bank_mask_32k = Cart.PrgSize / 32 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg_addr), ref reg_addr);
			ser.Sync(nameof(regs), ref regs, false);
			base.SyncState(ser);
		}

		public override void WriteExp(int addr, byte value)
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

		public override byte ReadPpu(int addr)
		{
			if (var_a)
			{
				if (addr < 0x2000)
				{
					int chr_bank = regs[4] | (regs[6] << 1) | (regs[2] << 3);

					return Vrom[((chr_bank & chr_bank_mask_8k) * 0x2000) + addr];
				}

				return base.ReadPpu(addr);
			}

			if (addr < 0x2000)
			{
				int chr_bank = (regs[4] << 2) | (regs[6]) | (regs[2] << 3);
				return Vrom[((chr_bank & chr_bank_mask_8k) * 0x2000) + addr];
			}

			return base.ReadPpu(addr);
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[((regs[5] & prg_bank_mask_32k) * 0x8000) + addr];
		}
	}
}
