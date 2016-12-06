using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper253 : NES.NESBoardBase
	{
		private ByteBuffer prg = new ByteBuffer(2);
		private ByteBuffer chrlo = new ByteBuffer(8);
		private ByteBuffer chrhi = new ByteBuffer(8);
		private bool vlock;
		private int IRQLatch, IRQClock, IRQCount;
		private bool IRQa;

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

		public override void Dispose()
		{
			prg.Dispose();
			chrlo.Dispose();
			chrhi.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("preg", ref prg);
			ser.Sync("chrlo", ref chrlo);
			ser.Sync("chrhi", ref chrhi);
		}

		public override void ClockCPU()
		{
			if (IRQa)
			{
				IRQClock += 3;
				if (IRQClock >= 341)
				{
					IRQClock -= 341;
					IRQCount++;
					if (IRQCount == 0x100)
					{
						IRQSignal = true;
						IRQCount = IRQLatch;
					}
				}
			}
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

					case 0xF000: IRQSignal = false; IRQLatch &= 0xF0; IRQLatch |= value & 0xF; break;
					case 0xF004: IRQSignal = false; IRQLatch &= 0x0F; IRQLatch |= value << 4; break;
					case 0xF008: IRQSignal = false; IRQClock = 0; IRQCount = IRQLatch; IRQa = value.Bit(1); break;
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
				case 3: SetMirrorType(EMirrorType.OneScreenB); break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank;

			if (addr < 0x2000)
			{
				bank = prg[0] & prg_bank_mask_8k;
			}
			else if (addr < 0x4000)
			{
				bank = prg[1] & prg_bank_mask_8k;
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

				if ((chrlo[x] == 4 || chrlo[x] == 5) && !vlock)
				{
					bank = chr & 1;
					return VRAM[(bank << 10) + (addr & 0x3FF)];
				}
				else
				{
					return VROM[bank + (addr & 0x3FF)];
				}

			}

			return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (VRAM != null)
				{ 
					int x = (addr >> 10) & 7;
					var chr = chrlo[x] | (chrhi[x] << 8);
					int bank = (chr & chr_bank_mask_1k) << 10;

					if ((chrlo[x] == 4 || chrlo[x] == 5) && !vlock)
					{
						bank = chr & 1;
						VRAM[(bank << 10) + (addr & 0x3FF)]=value;
					}
				}
			}
			else
			{
				NES.CIRAM[ApplyMirroring(addr)] = value;
			}
		}
	}
}
