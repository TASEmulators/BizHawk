using BizHawk.Common;

// see http://nesdev.parodius.com/bbs/viewtopic.php?t=5426&sid=e7472c15a758ebf05c588c8330c2187f
// and http://nesdev.parodius.com/bbs/viewtopic.php?t=311
// for some info on NAMCOT 108
// but mostly http://wiki.nesdev.com/w/index.php/INES_Mapper_206
//TODO - prg is 4 bits, chr is 6 bits
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// also, Namcot109, Namcot118, Namcot119 chips are this exact same thing
	internal class Namcot108Chip
	{
		private int reg_addr;
		private byte[] regs = new byte[8];

		private byte[] _chrRegs1K = new byte[8];
		private byte[] _prgRegs8K = new byte[4];

		public Namcot108Chip(NesBoardBase board)
		{
			Sync();
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.Sync(nameof(reg_addr), ref reg_addr);
			ser.Sync(nameof(regs), ref regs, false);
			ser.Sync(nameof(_chrRegs1K), ref _chrRegs1K, false);
			ser.Sync(nameof(_prgRegs8K), ref _prgRegs8K, false);
			Sync();
		}

		public virtual void WritePRG(int addr, byte value)
		{
			//($8001-$9FFF, odd)
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					reg_addr = (value & 7);
					break;
				case 0x0001: //$8001
					regs[reg_addr] = value;
					Sync();
					break;
			}
		}

		private void Sync()
		{
			_prgRegs8K[0] = regs[6];
			_prgRegs8K[1] = regs[7];
			_prgRegs8K[2] = 0xFE;
			_prgRegs8K[3] = 0xFF;

			byte r0_0 = (byte)(regs[0] & ~1);
			byte r0_1 = (byte)(regs[0] | 1);
			byte r1_0 = (byte)(regs[1] & ~1);
			byte r1_1 = (byte)(regs[1] | 1);

			_chrRegs1K[0] = r0_0;
			_chrRegs1K[1] = r0_1;
			_chrRegs1K[2] = r1_0;
			_chrRegs1K[3] = r1_1;
			_chrRegs1K[4] = regs[2];
			_chrRegs1K[5] = regs[3];
			_chrRegs1K[6] = regs[4];
			_chrRegs1K[7] = regs[5];
		}

		public int Get_PRGBank_8K(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = _prgRegs8K[bank_8k];
			return bank_8k;
		}

		public int Get_CHRBank_1K(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = _chrRegs1K[bank_1k];
			return bank_1k;
		}
	}

	internal abstract class Namcot108Board_Base : NesBoardBase
	{
		//state
		protected Namcot108Chip mapper;

		//configuration
		protected int prg_mask, chr_byte_mask;

		//the VS actually does have 2 KB of nametable address space
		//let's make the extra space here, instead of in the main NES to avoid confusion
		private byte[] CIRAM_VS = new byte[0x800];

		// security reading index for tko boxing
		private int tko_security = 0;

		private static readonly byte[] TKO = { 0xFF, 0xBF, 0xB7, 0x97, 0x97, 0x17, 0x57, 0x4F, 0x6F, 0x6B, 0xEB, 0xA9, 0xB1, 0x90, 0x94, 0x14,
										 0x56, 0x4E, 0x6F, 0x6B, 0xEB, 0xA9, 0xB1, 0x90, 0xD4, 0x5C, 0x3E, 0x26, 0x87, 0x83, 0x13, 0x51};

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			if (NES._isVS)
				ser.Sync(nameof(CIRAM_VS), ref CIRAM_VS, false);

			ser.Sync(nameof(tko_security), ref tko_security);
			mapper.SyncState(ser);
		}

		public int Get_CHRBank_1K(int addr)
		{
			return mapper.Get_CHRBank_1K(addr);
		}

		public int Get_PRGBank_8K(int addr)
		{
			return mapper.Get_PRGBank_8K(addr);
		}

		protected int MapCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (Vrom != null)
				{
					addr &= chr_byte_mask;
					return Vrom[addr];
				}

				return Vram[addr];
			}

			if (NES._isVS)
			{
				addr = addr - 0x2000;
				if (addr < 0x800)
				{
					return NES.CIRAM[addr];
				}

				return CIRAM_VS[addr - 0x800];
			}

			return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (Vram == null) return;
				addr = MapCHR(addr);
				Vram[addr] = value;
			}
			else if (NES._isVS)
			{
				// The game VS Castlevania apparently scans for more CIRAM then actually exists, so we have to mask out nonsensical values 
				addr &= 0x2FFF;


				addr = addr - 0x2000;
				if (addr < 0x800)
				{
					NES.CIRAM[addr] = value;
				}
				else
				{
					CIRAM_VS[addr - 0x800] = value;
				}
			}
			else
				base.WritePpu(addr, value);
		}

		public override void WritePrg(int addr, byte value)
		{
			mapper.WritePRG(addr, value);
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return Rom[addr];
		}

		// there are 3 namco games which each use their own ICs for security in different ways
		// for convenience they are assigned in the VS_security setting
		// 16 = Super Xevious
		// 32 = TKO Boxing
		// 48 = RBI Baseball
		public override byte ReadExp(int addr)
		{
			if (!NES._isVS)
				return base.ReadExp(addr);

			if (Cart.VsSecurity == 16)
			{
				addr += 0x4000;
				if (addr == 0x54FF)
				{
					return 0x05;
				}

				if (addr == 0x5678)
				{
					return NES.cpu.X == 0x0C ? (byte)0 : (byte)1;
				}

				if (addr == 0x578F)
				{
					return NES.cpu.X == 0x0C ? (byte)0xD1 : (byte)0x89;
				}

				if (addr == 0x5567)
				{
					return NES.cpu.X == 0x0C ? (byte)0x3E : (byte)0x37;
				}

				return base.ReadExp(addr - 0x4000);
			}

			if (Cart.VsSecurity==32)
			{
				if (addr==0x1E00)
				{
					tko_security = 0;
					return 0xAA; //not used??
				}

				if (addr == 0x1E01)
				{
					tko_security++;
					return TKO[tko_security - 1];
				}

				return NES.DB;
			}

			if (Cart.VsSecurity == 48)
			{
				if (addr == 0x1E00)
				{
					tko_security = 0;
					return 0xAA; //not used??
				}
				if (addr == 0x1E01)
				{
					if (tko_security==4)
					{
						return 0xB4;
					}
					if (tko_security == 9)
					{
						return 0x6F;
					}
					tko_security++;
					return NES.DB;
				}
				return NES.DB;
			}

			return NES.DB;
		}

		protected virtual void BaseSetup()
		{
			int num_prg_banks = Cart.PrgSize / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.ChrSize);
			chr_byte_mask = (num_chr_banks*1024) - 1;

			mapper = new Namcot108Chip(this);
			if (!NES._isVS)
				SetMirrorType(EMirrorType.Vertical);
		}
	}
}