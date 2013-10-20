namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class Mapper193 : NES.NESBoardBase 
	{
		/*
		* ========================
		=  Mapper 193          =
		========================


		Example Game:
		--------------------------
		Fighting Hero (Unl)



		Registers:
		---------------------------
		Regs at $6000-7FFF = no SRAM

		Range,Mask:   $6000-7FFF, $6003


		$6000:  CHR Reg 0
		$6001:  CHR Reg 1
		$6002:  CHR Reg 2
		$6003:  PRG Reg


		CHR Setup:
		---------------------------

		$0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
		+-------------------------------+---------------+---------------+
		|           <<$6000>>           |    <$6001>    |    <$6002>    |
		+-------------------------------+---------------+---------------+

		PRG Setup:
		---------------------------

		$8000   $A000   $C000   $E000  
		+-------+-------+-------+-------+
		| $6003 | { -3} | { -2} | { -1} |
		+-------+-------+-------+-------+
		*/

		int prg_bank_mask_8k;
		ByteBuffer prg_banks_8k = new ByteBuffer(4);

		int chr_bank_mask_2k;
		ByteBuffer chr_banks_2k = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER193":
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;
			prg_banks_8k[1] = 0xFD;
			prg_banks_8k[2] = 0xFE;
			prg_banks_8k[3] = 0xFF;

			chr_bank_mask_2k = (Cart.chr_size / 2) - 1;

			SetMirrorType(EMirrorType.Vertical);
			SyncMap();
			return true;
		}

		void SyncMap()
		{
			ApplyMemoryMapMask(prg_bank_mask_8k, prg_banks_8k);
			ApplyMemoryMapMask(chr_bank_mask_2k, chr_banks_2k);
		}

		public override void Dispose()
		{
			prg_banks_8k.Dispose();
			chr_banks_2k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("chr_banks_2k", ref chr_banks_2k);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			addr &= 0x6003;
			switch (addr)
			{
				case 0:
					chr_banks_2k[0] = (byte)((value & ~3) >> 1);
					chr_banks_2k[1] = (byte)(((value & ~3) >> 1) + 1); 
					break;
				case 1:
					chr_banks_2k[2] = (byte)((value & ~1) >> 1);
					break;
				case 2:
					chr_banks_2k[3] = (byte)((value & ~1) >> 1); 
					break;
				case 3:
					prg_banks_8k[0] = value;
					break;
			}
			SyncMap();
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = ApplyMemoryMap(11, chr_banks_2k, addr);
				return base.ReadPPUChr(addr);
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}

		public override byte ReadPRG(int addr)
		{
			addr = ApplyMemoryMap(13, prg_banks_8k, addr);
			return ROM[addr];
		}
	}
}
