using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// behavior pulled from fceux:
	/*
	DIS23C01 DAOU ROM CONTROLLER, Korea
	* Metal Force (K)
	* Buzz and Waldog (K)
	* General's Son (K) 
	*/
	internal sealed class Mapper156 : NesBoardBase
	{
		private int prg_mask;
		private int chr_mask;
		private int prg;
		private readonly int[] chr = new int[8];

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER156":
					break;
				default:
					return false;
			}
			prg_mask = Cart.PrgSize / 16 - 1;
			chr_mask = Cart.ChrSize / 1 - 1;
			SetMirrorType(EMirrorType.OneScreenA);
			return true;
		}

		public override void NesSoftReset()
		{
			for (int i = 0; i < chr.Length; i++)
				chr[i] = 0;
			prg = 0;
			SetMirrorType(EMirrorType.OneScreenA);
		}

		public override byte ReadPrg(int addr)
		{
			if (addr < 0x4000)
				return Rom[addr + (prg << 14)];
			else
				return Rom[(addr & 0x3fff) + (prg_mask << 14)];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
				return Vrom[(addr & 0x3ff) + (chr[addr >> 10] << 10)];
			else
				return base.ReadPpu(addr);
		}

		public override void WritePrg(int addr, byte value)
		{
			switch (addr)
			{
				case 0x4000:
				case 0x4001:
				case 0x4002:
				case 0x4003:
					chr[addr & 3] &= 0xff00;
					chr[addr & 3] |= value;
					chr[addr & 3] &= chr_mask;
					break;
				case 0x4004:
				case 0x4005:
				case 0x4006:
				case 0x4007:
					chr[addr & 3] &= 0x00ff;
					chr[addr & 3] |= value << 8;
					chr[addr & 3] &= chr_mask;
					break;
				case 0x4008:
				case 0x4009:
				case 0x400a:
				case 0x400b:
					chr[(addr & 3) + 4] &= 0xff00;
					chr[(addr & 3) + 4] |= value;
					chr[(addr & 3) + 4] &= chr_mask;
					break;
				case 0x400c:
				case 0x400d:
				case 0x400e:
				case 0x400f:
					chr[(addr & 3) + 4] &= 0x00ff;
					chr[(addr & 3) + 4] |= value << 8;
					chr[(addr & 3) + 4] &= chr_mask;
					break;
				case 0x4010:
					prg = value & prg_mask;
					break;
				case 0x4014:
					// is this right??
					if ((value & 1) != 0)
						SetMirrorType(EMirrorType.Vertical);
					else
						SetMirrorType(EMirrorType.Horizontal);
					break;				
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			// do not need to serialize mirroring, as that's handled for us
			for (int i = 0; i < chr.Length; i++)
				ser.Sync("chr" + i, ref chr[i]);
			ser.Sync(nameof(prg), ref prg);
		}
	}
}
