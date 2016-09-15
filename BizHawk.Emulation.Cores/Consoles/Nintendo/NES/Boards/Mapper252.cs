using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	public sealed class Mapper252 : NES.NESBoardBase
	{
		private ByteBuffer preg = new ByteBuffer(2);
		private ByteBuffer creg = new ByteBuffer(8);

		private int prg_bank_mask_8k, chr_bank_mask_1k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER252":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("preg", ref preg);
			ser.Sync("creg", ref creg);
		}

		public override void WritePRG(int addr, byte value)
		{
			WriteReg((addr + 0x8000), value);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			WriteReg((addr + 0x6000), value);
		}

		public void WriteReg(int addr, byte value)
		{
			if (addr >= 0xB000 && addr < 0xF000)
			{
				var ind = ((((addr & 8) | (addr >> 8)) >> 3) + 2) & 7;
				var sar = addr & 4;
				creg[ind] = (byte)((creg[ind] & (0xF0 >> sar)) | ((value & 0x0F) << sar));
			}
			else
			{
				switch (addr & 0xF00C)
				{
					case 0x8000:
					case 0x8004:
					case 0x8008:
					case 0x800C:
						preg[0] = value;
						break;

					case 0xA000:
					case 0xA004:
					case 0xA008:
					case 0xA00C:
						preg[1] = value;
						break;

					// TODO IRQ
					case 0xF000: break;
					case 0xF004: break;
					case 0xF008: break;
				}
			}
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[addr];
		}

		public override byte ReadPRG(int addr)
		{
			int bank;

			if (addr < 0x2000)
			{
				bank = preg[0] & prg_bank_mask_8k;
			}
			else if (addr < 0x4000)
			{
				bank = preg[1] & prg_bank_mask_8k;
			}
			else if (addr < 0x6000)
			{
				bank = prg_bank_mask_8k - 1;
			}
			else
			{
				bank = prg_bank_mask_8k;
			}


			return ROM[(bank << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int x = (addr >> 10) & 7;

				int bank;
				if (creg[x] == 6 || creg[x] == 7)
				{
					bank = creg[x] & 1;
				}
				else
				{
					bank = (creg[x] & chr_bank_mask_1k) << 10;
				}

				if (addr == 0x400)
				{
					int zzz = 0;
				}

				return VROM[bank + (addr & 0x3FF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
