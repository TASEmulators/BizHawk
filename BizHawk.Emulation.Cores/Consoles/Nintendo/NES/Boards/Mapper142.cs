using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper142 : NES.NESBoardBase
	{
		private ByteBuffer reg = new ByteBuffer(8);
		private byte cmd;
		private int lastBank;

		private bool isirqused = false;
		private byte IRQa = 0;
		private int IRQCount = 0;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER142":
				case "UNIF_UNL-KS7032":
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Horizontal);
			lastBank = Cart.prg_size / 8 - 1;

			return true;
		}

		public override void Dispose()
		{
			reg.Dispose();
			base.Dispose();
		}

		public override byte ReadWRAM(int addr)
		{
			return ROM[(reg[4] << 13) + (addr & 0x1FFF)];		
		}

		public override byte ReadPRG(int addr)
		{

			if (addr < 0x2000) { return ROM[(reg[1] << 13) + (addr & 0x1FFF)]; }
			if (addr < 0x4000) { return ROM[(reg[2] << 13) + (addr & 0x1FFF)]; }
			if (addr < 0x6000) { return ROM[(reg[3] << 13) + (addr & 0x1FFF)]; }

			return ROM[(lastBank << 13) + (addr & 0x1FFF)];		
		}

		public override void WriteEXP(int addr, byte value)
		{
			Write(addr + 0x4000, value);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			Write(addr + 0x6000, value);
		}

		public override void WritePRG(int addr, byte value)
		{
			Write(addr + 0x8000, value);
		}

		private void IRQHook(int a)
		{
			if (IRQa > 0)
			{
				IRQCount += a;
				if (IRQCount >= 0xFFFF)
				{
					IRQa = 0;
					IRQCount = 0;

					IRQSignal = true;
				}
			}
		}

		public override void ClockPPU()
		{
			IRQHook(1);
		}

		private void Write(int addr, byte value)
		{
			switch (addr & 0xF000)
			{
				case 0x8000:
					IRQSignal = false;
					IRQCount = (IRQCount & 0x000F) | (value & 0x0F);
					isirqused = true;
					break;
				case 0x9000:
					IRQSignal = false;
					IRQCount = (IRQCount & 0x00F0) | ((value & 0x0F) << 4);
					isirqused = true;
					break;
				case 0xA000:
					IRQSignal = false;
					IRQCount = (IRQCount & 0x0F00) | ((value & 0x0F) << 8);
					isirqused = true;
					break;
				case 0xB000:
					IRQSignal = false;
					IRQCount = (IRQCount & 0xF000) | (value << 12);
					isirqused = true;
					break;
				case 0xC000:
					if (isirqused)
					{
						IRQSignal = false;
						IRQa = 1;
					}
					break;

				case 0xE000:
					cmd = (byte)(value & 7);
					break;
				case 0xF000:
					reg[cmd] = value;
					break;
			}
		}
	}
}
