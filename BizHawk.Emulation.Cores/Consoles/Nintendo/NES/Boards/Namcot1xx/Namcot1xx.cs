//see http://nesdev.parodius.com/bbs/viewtopic.php?t=5426&sid=e7472c15a758ebf05c588c8330c2187f
//and http://nesdev.parodius.com/bbs/viewtopic.php?t=311
//for some info on NAMCOT 108
//but mostly http://wiki.nesdev.com/w/index.php/INES_Mapper_206

//TODO - prg is 4 bits, chr is 6 bits

using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//also, Namcot109, Namcot118, Namcot119 chips are this exact same thing
	public class Namcot108Chip : IDisposable
	{
		//state
		int reg_addr;
		ByteBuffer regs = new ByteBuffer(8);

		//volatile state
		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		ByteBuffer prg_regs_8k = new ByteBuffer(4);

		NES.NESBoardBase board;
		public Namcot108Chip(NES.NESBoardBase board)
		{
			this.board = board;

			Sync();
		}

		public void Dispose()
		{
			regs.Dispose();
			chr_regs_1k.Dispose();
			prg_regs_8k.Dispose();
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.Sync("reg_addr", ref reg_addr);
			ser.Sync("regs", ref regs);
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

		void Sync()
		{
			prg_regs_8k[0] = regs[6];
			prg_regs_8k[1] = regs[7];
			prg_regs_8k[2] = 0xFE;
			prg_regs_8k[3] = 0xFF;

			byte r0_0 = (byte)(regs[0] & ~1);
			byte r0_1 = (byte)(regs[0] | 1);
			byte r1_0 = (byte)(regs[1] & ~1);
			byte r1_1 = (byte)(regs[1] | 1);

			chr_regs_1k[0] = r0_0;
			chr_regs_1k[1] = r0_1;
			chr_regs_1k[2] = r1_0;
			chr_regs_1k[3] = r1_1;
			chr_regs_1k[4] = regs[2];
			chr_regs_1k[5] = regs[3];
			chr_regs_1k[6] = regs[4];
			chr_regs_1k[7] = regs[5];
		}

		public int Get_PRGBank_8K(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = prg_regs_8k[bank_8k];
			return bank_8k;
		}

		public int Get_CHRBank_1K(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = chr_regs_1k[bank_1k];
			return bank_1k;
		}
	}


	public abstract class Namcot108Board_Base : NES.NESBoardBase
	{
		//state
		protected Namcot108Chip mapper;

		//configuration
		protected int prg_mask, chr_byte_mask;

		//the VS actually does have 2 KB of nametable address space
		//let's make the extra space here, instead of in the main NES to avoid confusion
		byte[] CIRAM_VS = new byte[0x800];

		// security reading index for tko boxing
		int tko_security = 0;
		static byte[] TKO = new byte[] { 0xFF, 0xBF, 0xB7, 0x97, 0x97, 0x17, 0x57, 0x4F, 0x6F, 0x6B, 0xEB, 0xA9, 0xB1, 0x90, 0x94, 0x14,
										 0x56, 0x4E, 0x6F, 0x6B, 0xEB, 0xA9, 0xB1, 0x90, 0xD4, 0x5C, 0x3E, 0x26, 0x87, 0x83, 0x13, 0x51};

		public override void Dispose()
		{
			if(mapper != null)
				mapper.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			if (NES._isVS)
				ser.Sync("CIRAM_VS", ref CIRAM_VS, false);

			ser.Sync("tko_security", ref tko_security);
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

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (VROM != null)
				{
					addr &= chr_byte_mask;
					return VROM[addr];
				}
				else return VRAM[addr];
			}
			else
			{
				if (NES._isVS)
				{
					addr = addr - 0x2000;
					if (addr < 0x800)
					{
						return NES.CIRAM[addr];
					}
					else
					{
						return CIRAM_VS[addr - 0x800];
					}
				}
				else
					return base.ReadPPU(addr);
			}
			}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (VRAM == null) return;
				addr = MapCHR(addr);
				VRAM[addr] = value;
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
				base.WritePPU(addr, value);
		}


		public override void WritePRG(int addr, byte value)
		{
			mapper.WritePRG(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = Get_PRGBank_8K(addr);
			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return ROM[addr];
		}

		// there are 3 namco games which each use their own ICs for security in different ways
		// for convenience they are assigned in the VS_security setting
		// 16 = Super Xevious
		// 32 = TKO Boxing
		// 48 = RBI Baseball
		public override byte ReadEXP(int addr)
		{
			if (!NES._isVS)
				return base.ReadEXP(addr);
			else
			{

				if (Cart.vs_security == 16)
				{
					addr += 0x4000;
					if (addr == 0x54FF)
						return 0x05;
					else if (addr == 0x5678)
					{
						if (NES.cpu.X == 0x0C)
							return 0;
						else
							return 1;
					}

					else if (addr == 0x578F)
					{
						if (NES.cpu.X == 0x0C)
							return 0xD1;
						else
							return 0x89;
					}
					else if (addr == 0x5567)
					{
						if (NES.cpu.X == 0x0C)
							return 0x3E;
						else
							return 0x37;
					}

					else
						return base.ReadEXP(addr - 0x4000);
				}
				else if (Cart.vs_security==32)
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
				else if (Cart.vs_security == 48)
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
				else
				{
					return NES.DB;
				}
			}
		}

		protected virtual void BaseSetup()
		{
			int num_prg_banks = Cart.prg_size / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.chr_size);
			chr_byte_mask = (num_chr_banks*1024) - 1;

			mapper = new Namcot108Chip(this);
			if (!NES._isVS)
				SetMirrorType(EMirrorType.Vertical);
		}

	}
}