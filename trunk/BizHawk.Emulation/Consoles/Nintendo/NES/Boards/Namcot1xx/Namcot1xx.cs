//see http://nesdev.parodius.com/bbs/viewtopic.php?t=5426&sid=e7472c15a758ebf05c588c8330c2187f
//and http://nesdev.parodius.com/bbs/viewtopic.php?t=311
//for some info on NAMCOT 108
//but mostly http://wiki.nesdev.com/w/index.php/INES_Mapper_206

using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//also, Namcot109, Namcot118, Namcot119 chips are this exact same thing
	public class Namcot108Chip : IDisposable
	{
		//state
		int reg_addr;
		ByteBuffer chr_regs_1k = new ByteBuffer(8);
		ByteBuffer prg_regs_8k = new ByteBuffer(4);

		NES.NESBoardBase board;
		public Namcot108Chip(NES.NESBoardBase board)
		{
			this.board = board;

			prg_regs_8k[0] = 0;
			prg_regs_8k[1] = 1;
			prg_regs_8k[2] = 0xFE; //constant
			prg_regs_8k[3] = 0xFF; //constant

			chr_regs_1k[0] = 0;
			chr_regs_1k[1] = 1;
			chr_regs_1k[2] = 2;
			chr_regs_1k[3] = 3;
			chr_regs_1k[4] = 4;
			chr_regs_1k[5] = 5;
			chr_regs_1k[6] = 6;
			chr_regs_1k[7] = 7;
		}

		public void Dispose()
		{
			chr_regs_1k.Dispose();
			prg_regs_8k.Dispose();
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.Sync("reg_addr", ref reg_addr);
			ser.Sync("chr_regs_1k", ref chr_regs_1k);
			ser.Sync("prg_regs_8k", ref prg_regs_8k);
		}

		public virtual void WritePRG(int addr, byte value)
		{
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					reg_addr = (value & 7);
					break;
				case 0x0001: //$8001
					switch (reg_addr)
					{
						//bottom bits of these chr regs are ignored
						case 0: 
							chr_regs_1k[0] = (byte)(value & ~1); 
							chr_regs_1k[1] = (byte)(value | 1); 
							break;
						case 1: 
							chr_regs_1k[2] = (byte)(value & ~1);
							chr_regs_1k[3] = (byte)(value | 1);
							break;

						case 2: chr_regs_1k[4] = value; break;
						case 3: chr_regs_1k[5] = value; break;
						case 4: chr_regs_1k[6] = value; break;
						case 5: chr_regs_1k[7] = value; break;
						case 6: prg_regs_8k[0] = value; break;
						case 7: prg_regs_8k[1] = value; break;
					}
					break;
			}
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
		protected int prg_mask, chr_mask;

		public override void Dispose()
		{
			mapper.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
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

		int MapCHR(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			bank_1k &= chr_mask;
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (VROM != null)
					return VROM[addr];
				else return VRAM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (VRAM == null) return;
				addr = MapCHR(addr);
				VRAM[addr] = value;
			}
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

		protected virtual void BaseSetup()
		{
			int num_prg_banks = Cart.prg_size / 8;
			prg_mask = num_prg_banks - 1;

			int num_chr_banks = (Cart.chr_size);
			chr_mask = num_chr_banks - 1;

			mapper = new Namcot108Chip(this);
			SetMirrorType(EMirrorType.Vertical);
		}

	}
}