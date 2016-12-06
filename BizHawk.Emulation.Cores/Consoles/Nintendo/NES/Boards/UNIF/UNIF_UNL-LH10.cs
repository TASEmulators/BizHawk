using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class UNIF_UNL_LH10 : NES.NESBoardBase
	{
		private ByteBuffer reg = new ByteBuffer(8);
		private int cmd;

		private int prg_bank_mask_8;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "UNIF_UNL-LH10":
					//NES._isVS = true;
					break;
				default:
					return false;
			}

			prg_bank_mask_8 = Cart.prg_size / 8 - 1;

			//SetMirrorType(Cart.pad_h, Cart.pad_v);
			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void Dispose()
		{
			reg.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			ser.Sync("cmd", ref cmd);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr>=0x4000 && addr<0x6000)
			{
				WRAM[addr - 0x4000] = value;
				return;
			}


			addr += 0x8000;
			switch (addr & 0xE001)
			{
				case 0x8000: cmd = value & 7; break;
				case 0x8001: reg[cmd] = value; break;
			}


		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[ROM.Length - 0x4000 + addr];
		}

		public override byte ReadPRG(int addr)
		{
			int bank = 0;
			if (addr < 0x2000)
			{
				bank = reg[6];
			}
			else if (addr < 0x4000)
			{
				bank = reg[7];
			}
			else if (addr < 0x6000)
			{
				return WRAM[addr - 0x4000];
			}
			else
			{
				bank = 0xFF;
			}

			bank &= prg_bank_mask_8;
			return ROM[(bank << 13) + (addr & 0x1FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			return base.ReadPPU(addr);
		}
	}
}
