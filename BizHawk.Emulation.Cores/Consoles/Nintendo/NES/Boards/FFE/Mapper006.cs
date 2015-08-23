using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper006 : NES.NESBoardBase
	{
		private int _reg;
		private int _mirr;

		private int IRQa, mirr;
		private int IRQCount, IRQLatch;

		private int _prgMask16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER006":
					Cart.vram_size = 32;
					break;
				default:
					return false;
			}

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			_prgMask16k = Cart.prg_size / 16 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
		}

		public override void WriteEXP(int addr, byte value)
		{
			// Mirroring
			if (addr == 0x2FE || addr == 0x2FF)
			{
				_mirr = ((addr << 1) & 2) | ((addr >> 4) & 1);
				Sync();
			}

			// IRQ
			else if (addr >= 0x500 && addr <= 0x503)
			{
				switch (addr)
				{
					case 0x501:
						int zzz = 0;
						break;
					case 0x502:
						break;
					case 0x503:
						break;

				}
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = value;
			Sync();
		}

		public override byte ReadPRG(int addr)
		{
			int bank = addr < 0x4000
				? (_reg >> 2) & 0x3F
				: 7;
			bank &= _prgMask16k;

			return ROM[(bank * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VRAM[((_reg & 3) * 0x2000) + (addr & 0x1FFF)];
			}

			return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[((_reg & 3) * 0x2000) + (addr & 0x1FFF)] = value;
			}
			else
			{
				base.WritePPU(addr, value);
			}
		}

		private void Sync()
		{
			switch (_mirr)
			{
				case 0:
					SetMirrorType(EMirrorType.OneScreenA);
					break;
				case 1:
					SetMirrorType(EMirrorType.OneScreenB);
					break;
				case 2:
					SetMirrorType(EMirrorType.Vertical);
					break;
				case 3:
					SetMirrorType(EMirrorType.Vertical);
					break;
			}
		}
	}
}
