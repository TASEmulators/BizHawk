using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//Mapper 77
	//Napoleon Senki

	//the 4screen implementation is a bit of a guess, but it seems to work

	internal sealed class IREM_74_161_161_21_138 : NesBoardBase
	{
		private int chr, prg;
		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.BoardType)
			{
				case "MAPPER077":
					Cart.VramSize = 8;
					break;
				case "IREM-74*161/161/21/138":
					AssertVram(8);
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Vertical);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(chr), ref chr);
			ser.Sync(nameof(prg), ref prg);
		}

		public override void WritePrg(int addr, byte value)
		{
			chr = (value >> 4) & 0x0F;
			prg = value & 0x0F;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x0800)
				return Vrom[addr + (chr * 0x0800)];
			else if (addr < 0x2000)
				return Vram[addr];
			else if (addr < 0x2800)
				return Vram[addr & 0x7ff];
			else return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x0800)
				return;
			else if (addr < 0x2000)
				Vram[addr] = value;
			else if (addr < 0x2800)
				Vram[addr & 0x7ff] = value;
			else base.WritePpu(addr, value);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x8000)
				return Rom[addr + (prg * 0x8000)];
			else
				return base.ReadPrg(addr); 
		}
	}
}
