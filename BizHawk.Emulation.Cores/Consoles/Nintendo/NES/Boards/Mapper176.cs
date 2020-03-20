using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper176 : NesBoardBase
	{
		//configuration
		int prg_bank_mask_8k, chr_bank_mask_8k;

		//state
		int mirror;
		byte[] prg_banks_8k = new byte[4];
		byte[] chr_banks_8k = new byte[1];
		Bit sbw;

		public override bool Configure(EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				// http://wiki.nesdev.com/w/index.php/INES_Mapper_176
				// Mapper 176 was originally used for some Waixing boards, but goodNES 3.23 seems to go with CaH4e3's opinion that this mapper is FK23C
				// We will default 176 to FK23C, and use this board to support the tradional Waixing boards.  These ROMs will have to be specifically set to this board by the Game Database
				// case "MAPPER176":
				case "WAIXINGMAPPER176":
					break;
				default:
					return false;
			}
			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;
			chr_bank_mask_8k = (Cart.chr_size / 8) - 1;

			mirror = 0;
			SyncMirror();

			sbw = 0;
			prg_banks_8k[0] = 0;
			prg_banks_8k[1] = 1;
			prg_banks_8k[2] = 62;
			prg_banks_8k[3] = 63;
			ApplyMemoryMapMask(prg_bank_mask_8k,prg_banks_8k);

			chr_banks_8k[0] = 0;
			ApplyMemoryMapMask(chr_bank_mask_8k, chr_banks_8k);

			return true;
		}

		static readonly EMirrorType[] kMirrorTypes = {EMirrorType.Vertical,EMirrorType.Horizontal,EMirrorType.OneScreenA,EMirrorType.OneScreenB};
		void SyncMirror()
		{
			SetMirrorType(kMirrorTypes[mirror]);
		}

		public override byte ReadPrg(int addr)
		{
			addr = ApplyMemoryMap(13,prg_banks_8k,addr);
			return Rom[addr];
		}

		public override void WritePrg(int addr, byte value)
		{
			switch (addr)
			{
				case 0x2000: //0xA000
					mirror = value & 3;
					SyncMirror();
					break;
				case 0x2001: //0xA001
					//we_sram = data & 0x03;
					break;
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				addr = ApplyMemoryMap(13, chr_banks_8k, addr);
				return base.ReadPPUChr(addr);
			}
			else return base.ReadPpu(addr);
		}

		void SetPrg32k(int value)
		{
			for(int i=0;i<4;i++)
				prg_banks_8k[i] = (byte)(value * 4 + i);
			ApplyMemoryMapMask(prg_bank_mask_8k, prg_banks_8k);
		}

		public override void WriteExp(int addr, byte value)
		{
			switch (addr)
			{
				case 0x1000: //0x5000
					break;
				case 0x1001: //0x5001
					if (sbw) SetPrg32k(value);
					break;
				case 0x1010: //0x5010
					if (value == 0x24) sbw = 1;
					break;
				case 0x1011: //0x5011
					if (sbw) SetPrg32k(value >> 1);
					break;
				case 0x1FF1: //0x5FF1
					SetPrg32k(value>>1);
					break;
				case 0x1FF2: //0x5FF2
					chr_banks_8k[0] = (byte)value;
					ApplyMemoryMapMask(chr_bank_mask_8k, chr_banks_8k);
					break;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(mirror), ref mirror);
			ser.Sync(nameof(prg_banks_8k), ref prg_banks_8k, false);
			ser.Sync(nameof(chr_banks_8k), ref chr_banks_8k, false);
			ser.Sync(nameof(sbw), ref sbw);
		}
	}
}
