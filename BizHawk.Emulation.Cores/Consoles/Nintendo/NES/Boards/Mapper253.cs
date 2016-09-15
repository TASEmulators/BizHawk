using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper253 : NES.NESBoardBase
	{
		private ByteBuffer prg = new ByteBuffer(2);
		private ByteBuffer chrlo = new ByteBuffer(8);
		private ByteBuffer chrhi = new ByteBuffer(8);
		private bool vlock;

		private int prg_bank_mask_8k, chr_bank_mask_1k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER253":
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
			ser.Sync("preg", ref prg);
			ser.Sync("chrlo", ref chrlo);
			ser.Sync("chrhi", ref chrhi);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			if ((addr >= 0xB000) && (addr <= 0xE00C))
			{
				var ind = ((((addr & 8) | (addr >> 8)) >> 3) + 2) & 7;
				var sar = addr & 4;
				var clo = (chrlo[ind] & (0xF0 >> sar)) | ((value & 0x0F) << sar);
				chrlo[ind] = (byte)clo;
				if (ind == 0)
				{
					if (clo == 0xc8)
						vlock = false;
					else if (clo == 0x88)
						vlock = true;
				}
				if (sar > 0)
					chrhi[ind] = (byte)(value >> 4);
			}
			else
			{
				switch (addr)
				{
					case 0x8010: prg[0] = value; break;
					case 0xA010: prg[1] = value; break;
					case 0x9400: SetMirroring(value); break;

					// TODO: IRQ
					case 0xF000: break;
					case 0xF004: break;
					case 0xF008: break;
				}
			}
		}

		private void SetMirroring(int mirr)
		{
			switch(mirr & 3)
			{
				case 0: SetMirrorType(EMirrorType.Vertical); break;
				case 1: SetMirrorType(EMirrorType.Horizontal); break;
				case 2: SetMirrorType(EMirrorType.OneScreenA); break;
				case 3: SetMirrorType(EMirrorType.Vertical); break;
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
				bank = prg[0];
			}
			else if (addr < 0x4000)
			{
				bank = prg[1];
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
				var chr = chrlo[x] | (chrhi[x] << 8);
				int bank = (chr & chr_bank_mask_1k) << 10;
				return VROM[bank + (addr & 0x3FF)];
			}

			return base.ReadPPU(addr);
		}
	}
}
