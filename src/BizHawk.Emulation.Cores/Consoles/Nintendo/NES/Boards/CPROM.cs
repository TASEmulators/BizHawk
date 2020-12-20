using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class CPROM : NesBoardBase
	{
		//generally mapper 13

		//state
		private int chr;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER013":
					AssertPrg(32);
					AssertChr(0);
					Cart.VramSize = 16;
					Cart.WramSize = 0;
					break;

				case "NES-CPROM": //videomation
					AssertPrg(32); AssertChr(0); AssertVram(16); AssertWram(0);
					break;
				
				default:
					return false;
			}

			//TODO - assert that mirror type is vertical?
			//set it in the cart?

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}
		
		public override void WritePrg(int addr, byte value)
		{
			value = HandleNormalPRGConflict(addr,value);
			chr = value&3;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x1000)
				return Vram[addr];
			else if(addr<0x2000)
				return Vram[addr - 0x1000 + (chr << 12)];
			else return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x1000)
				Vram[addr] = value;
			else if (addr < 0x2000)
				Vram[addr - 0x1000 + (chr << 12)] = value;
			else base.WritePpu(addr,value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
		}
	}
}