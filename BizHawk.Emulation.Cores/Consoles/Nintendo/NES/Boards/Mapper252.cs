using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from FCEUX src
	public sealed class Mapper252 : NES.NESBoardBase
	{
		private ByteBuffer preg = new ByteBuffer(2);
		private ByteBuffer creg = new ByteBuffer(8);

		private int prg_bank_mask_8k, chr_bank_mask_1k;
		private int IRQLatch, IRQClock, IRQCount;
		private bool IRQa;

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
			AssertPrg(256);
			AssertChr(128);
			AssertVram(2);
			AssertWram(8);
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void Dispose()
		{
			preg.Dispose();
			creg.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("preg", ref preg);
			ser.Sync("creg", ref creg);
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
					if (IRQCount==0x100)
					{
						IRQSignal=true;
						IRQCount = IRQLatch;
					}					
				}
			}
		}


		public override void WritePRG(int addr, byte value)
		{
			WriteReg((addr + 0x8000), value);
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

					case 0xF000: IRQSignal = false; IRQLatch &= 0xF0; IRQLatch |= value & 0xF; break;
					case 0xF004: IRQSignal = false; IRQLatch &= 0x0F; IRQLatch |= value << 4; break;
					case 0xF008: IRQSignal = false; IRQClock = 0; IRQCount = IRQLatch; IRQa = value.Bit(1); break;

				}
			}
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
					return VRAM[(bank << 10) + (addr & 0x3FF)];
				}
				else
				{
					bank = (creg[x] & chr_bank_mask_1k) << 10;
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
					VRAM[addr&0x7FF] = value;
			}
			else
			{
				NES.CIRAM[ApplyMirroring(addr)] = value;
			}
		}
	}
}
