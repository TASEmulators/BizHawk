using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo.Boards
{
	//AKA MMC1
	//http://wiki.nesdev.com/w/index.php/SxROM

	//consult nestopia as well.
	//the initial conditions for MMC1 games are known to be different. this may have to do with which MMC1 rev it is.
	//but how do we know which revision a game is? i don't know which revision is on which board
	//check UNIF for more information.. it may specify board and MMC1 rev independently because boards may have any MMC1 rev
	//in that case, we need to capture MMC1 rev in the game database (maybe add a new `chip` parameter)

	//Final Fantasy
	//Mega Man 2
	//Blaster Master
	//Metroid
	//Kid Icarus
	//Zelda
	//Zelda 2
	//Castlevania 2

	class MMC1
	{
		public MMC1()
		{
			//collect data about whether this is required here:
			//kid icarus requires it
			//zelda doesnt; nor megaman2; nor blastermaster; nor metroid
			StandardReset();
			//well, lets leave it.
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			bw.Write(shift_count);
			bw.Write(shift_val);
			bw.Write(chr_mode);
			bw.Write(prg_mode);
			bw.Write(prg_slot);
			bw.Write((int)mirror);
			bw.Write(chr_0);
			bw.Write(chr_1);
			bw.Write(wram_disable);
			bw.Write(prg);
		}
		public void LoadStateBinary(BinaryReader br)
		{
			shift_count = br.ReadInt32();
			shift_val = br.ReadInt32();
			chr_mode = br.ReadInt32();
			prg_mode = br.ReadInt32();
			prg_slot = br.ReadInt32();
			mirror = (NES.EMirrorType)br.ReadInt32();
			chr_0 = br.ReadInt32();
			chr_1 = br.ReadInt32();
			wram_disable = br.ReadInt32();
			prg = br.ReadInt32();
		}

		public enum Rev
		{
			A, B1, B2, B3
		}

		//shift register
		int shift_count, shift_val;

		//register 0:
		public int chr_mode;
		public int prg_mode;
		public int prg_slot; //complicated
		public NES.EMirrorType mirror;
		static NES.EMirrorType[] _mirrorTypes = new NES.EMirrorType[] { NES.EMirrorType.OneScreenA, NES.EMirrorType.OneScreenB, NES.EMirrorType.Vertical, NES.EMirrorType.Horizontal };

		//register 1,2:
		int chr_0, chr_1;

		//register 3:
		int wram_disable;
		int prg;

		void StandardReset()
		{
			prg_mode = 1;
			prg_slot = 1;
		}

		public void Write(int addr, byte value)
		{
			int data = value & 1;
			int reset = (value >> 7) & 1;
			if (reset == 1)
			{
				shift_count = 0;
				shift_val = 0;
				StandardReset();
			}
			else
			{
				shift_val >>= 1;
				shift_val |= (data<<4);
				shift_count++;
				if (shift_count == 5)
				{
					WriteRegister(addr >> 13, shift_val);
					shift_count = 0;
					shift_val = 0;
				}
			}
		}

		void WriteRegister(int addr, int value)
		{
			switch (addr)
			{
				case 0: //8000-9FFF
					mirror = _mirrorTypes[value & 3];
					prg_slot = ((value >> 2) & 1);
					prg_mode = ((value >> 3) & 1);
					chr_mode = ((value >> 4) & 1);
					break;
				case 1: //A000-BFFF
					chr_0 = value & 0x1F;
					break;
				case 2: //C000-DFFF
					chr_1 = value & 0x1F;
					break;
				case 3: //E000-FFFF
					prg = value & 0xF;
					wram_disable = (value >> 4) & 1;
					break;
			}
			//Console.WriteLine("mapping.. chr_mode={0}, chr={1},{2}", chr_mode, chr_0, chr_1);
			//Console.WriteLine("mapping.. prg_mode={0}, prg_slot{1}, prg={2}", prg_mode, prg_slot, prg);
		}

		public int Get_PRGBank(int addr)
		{
			int PRG_A14 = (addr >> 14) & 1;
			if (prg_mode == 0)
				if (PRG_A14 == 0)
					return prg;
				else
				{
					//"not tested very well yet! had to guess!
					return (prg+1) & 0xF;
				}
			else if (prg_slot == 0)
				if (PRG_A14 == 0)
					return 0;
				else return prg;
			else
				if (PRG_A14 == 0)
					return prg;
				else return 0xF;
		}

		public int Get_CHRBank_4K(int addr)
		{
			int CHR_A12 = (addr >> 12) & 1;
			if (chr_mode == 0)
				return chr_0;
			else if (CHR_A12 == 0)
				return chr_0;
			else return chr_1;
		}
	}

	public class SxROM : NES.NESBoardBase
	{
		//configuration
		int prg_mask, chr_mask;
		int cram_mask, pram_mask;

		//state
		byte[] cram, pram;
		MMC1 mmc1;

		public override void WritePRG(int addr, byte value)
		{
			mmc1.Write(addr, value);
			SetMirrorType(mmc1.mirror); //often redundant, but gets the job done
		}

		public override byte ReadPRG(int addr)
		{
			int bank = mmc1.Get_PRGBank(addr) & prg_mask;
			addr = (bank << 14) | (addr & 0x3FFF);
			return ROM[addr];
		}

		int Gen_CHR_Address(int addr)
		{
			int bank = mmc1.Get_CHRBank_4K(addr);
			addr = ((bank & chr_mask) << 12) | (addr & 0x0FFF);
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				if (BoardInfo.CRAM_Size != 0)
					return cram[addr & cram_mask];
				else return VROM[Gen_CHR_Address(addr)];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (BoardInfo.CRAM_Size != 0)
					cram[addr & cram_mask] = value;
			}
			else base.WritePPU(addr, value);
		}

		public override byte ReadPRAM(int addr)
		{
			if (BoardInfo.PRAM_Size != 0)
				return pram[addr & pram_mask];
			else return 0xFF;
		}

		public override void WritePRAM(int addr, byte value)
		{
			if (BoardInfo.PRAM_Size != 0)
				pram[addr & pram_mask] = value;
		}

		public override byte[] SaveRam
		{
			get
			{
				if (!BoardInfo.Battery) return null;
				return pram;
				//some boards have a pram that is backed-up or not backed-up. need to handle that somehow
				//(nestopia splits it into NVWRAM and WRAM but i didnt like that at first.. but it may player better with this architecture)
			}
		}

        public override byte[] PRam
        {
            get
            {
                if (BoardInfo.PRAM_Size > 0)
                    return pram;
                else
                    return null;
            }
        }

        public override byte[] CRam
        {
            get
            {
				if (BoardInfo.CRAM_Size > 0)
                    return cram;
                else
                    return null;
            }
        }

		public override void SaveStateBinary(BinaryWriter bw)
		{
			base.SaveStateBinary(bw);
			mmc1.SaveStateBinary(bw);
			Util.WriteByteBuffer(bw, pram);
			Util.WriteByteBuffer(bw, cram);
		}
		public override void LoadStateBinary(BinaryReader br)
		{
			base.LoadStateBinary(br);
			mmc1.LoadStateBinary(br);
			pram = Util.ReadByteBuffer(br, false);
			cram = Util.ReadByteBuffer(br, false);
		}


		public override bool Configure(NES.BootGodDB.Cart cart)
		{
			//analyze board type
			switch (cart.board_type)
			{
				case "NES-SAROM":
					Assert(cart.chr_size == 16 || cart.chr_size == 32 || cart.chr_size == 64);
					BoardInfo.PRG_Size = 64;
					BoardInfo.CHR_Size = cart.chr_size;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 8;
					break;
				case "NES-SBROM":
					Assert(cart.chr_size == 16 || cart.chr_size == 32 || cart.chr_size == 64);
					BoardInfo.PRG_Size = 64;
					BoardInfo.CHR_Size = cart.chr_size;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SCROM":
				case "NES-SC1ROM":
					BoardInfo.PRG_Size = 64;
					BoardInfo.CHR_Size = 128;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SEROM":
					BoardInfo.PRG_Size = 32;
					BoardInfo.CHR_Size = 32;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SFROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256);
					Assert(cart.chr_size == 16 || cart.chr_size == 32 || cart.chr_size == 64);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = cart.chr_size;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SGROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = 0;
					BoardInfo.CRAM_Size = 8;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SHROM":
				case "NES-SH1ROM":
					BoardInfo.PRG_Size = 32;
					BoardInfo.CHR_Size = 128;
					BoardInfo.PRAM_Size = 0;
					BoardInfo.CRAM_Size = 0;
					break;
				case "HVC-SIROM":
					Assert(cart.chr_size == 16 || cart.chr_size == 32 || cart.chr_size == 64);
					BoardInfo.PRG_Size = 32;
					BoardInfo.CHR_Size = cart.chr_size;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SJROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256);
					Assert(cart.chr_size == 16 || cart.chr_size == 32 || cart.chr_size == 64);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = cart.chr_size;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 8;
					break;
				case "NES-SKROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = cart.chr_size;
					BoardInfo.PRAM_Size = 8;
					BoardInfo.CRAM_Size = 0;
					break;
				case "NES-SLROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = 128;
					BoardInfo.PRAM_Size = 0;
					BoardInfo.CRAM_Size = 0;
					break;
				case "NES-SL1ROM":
					Assert(cart.prg_size == 64 || cart.prg_size == 128 || cart.prg_size == 256);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = 128;
					BoardInfo.PRAM_Size = 0;
					BoardInfo.CRAM_Size = 0;
					break;
				case "NES-SL2ROM":
					Assert(cart.prg_size == 128);
					Assert(cart.chr_size == 128);
					BoardInfo.PRG_Size = 128;
					BoardInfo.CHR_Size = 128;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SL3ROM":
					Assert(cart.prg_size == 256);
					Assert(cart.chr_size == 128);
					BoardInfo.PRG_Size = 256;
					BoardInfo.CHR_Size = 128;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "NES-SLRROM":
					Assert(cart.prg_size == 128);
					Assert(cart.chr_size == 128);
					BoardInfo.PRG_Size = 128;
					BoardInfo.CHR_Size = 128;
					BoardInfo.CRAM_Size = 0;
					BoardInfo.PRAM_Size = 0;
					break;
				case "HVC-SMROM":
					BoardInfo.PRG_Size = 256;
					BoardInfo.CHR_Size = 0;
					BoardInfo.PRAM_Size = 0;
					BoardInfo.CRAM_Size = 8;
					break;
				case "NES-SNROM":
					Assert(cart.prg_size == 16 || cart.prg_size == 128 || cart.prg_size == 256);
					//16 is unexpected but blargg's tests use it
					//TODO - consider making a unique board type for homebrew, as i discover how more of them are working
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = 0;
					BoardInfo.CRAM_Size = 8;
					BoardInfo.PRAM_Size = 8;
					break;
				case "NES-SOROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = 0;
					BoardInfo.CRAM_Size = 8;
					BoardInfo.PRAM_Size = 16;
					break;
				case "NES-SUROM":
					BoardInfo.PRG_Size = 512;
					BoardInfo.CHR_Size = 0;
					BoardInfo.PRAM_Size = 8;
					BoardInfo.CRAM_Size = 8;
					break;
				case "HVC-SXROM":
					Assert(cart.prg_size == 128 || cart.prg_size == 256 || cart.prg_size == 512);
					BoardInfo.PRG_Size = cart.prg_size;
					BoardInfo.CHR_Size = 0;
					BoardInfo.PRAM_Size = 32;
					BoardInfo.CRAM_Size = 8;
					break;
				default:
					return false;
			}

			//validate and setup the basics
			Assert(cart.prg_size == BoardInfo.PRG_Size);
			Assert(cart.chr_size == BoardInfo.CHR_Size);
			mmc1 = new MMC1();
			prg_mask = (BoardInfo.PRG_Size / 16) - 1;
			chr_mask = (BoardInfo.CHR_Size / 8) - 1;

			//boards that don't contain CHR rom will contain CRAM. only one size is supported; set it up if it is there.
			Debug.Assert(BoardInfo.CRAM_Size == 0 || BoardInfo.CRAM_Size == 8);
			if (BoardInfo.CRAM_Size != 0)
			{
				cram = new byte[BoardInfo.CRAM_Size * 1024];
				cram_mask = cram.Length - 1;
			}
			else cram = new byte[0];

			//some boards contain PRAM. we only understand one size right now. set it up if it is there.
			Debug.Assert(BoardInfo.PRAM_Size == 0 || BoardInfo.PRAM_Size == 8 || BoardInfo.PRAM_Size == 16 || BoardInfo.PRAM_Size == 32);
			if (BoardInfo.PRAM_Size != 0)
			{
				pram = new byte[BoardInfo.PRAM_Size * 1024];
				pram_mask = pram.Length - 1;
			}
			else pram = new byte[0];

			//some boards contain CHR roms, so set that up here.
			if (BoardInfo.CHR_Size != 0)
			{
				Debug.Assert(BoardInfo.CHR_Size == 16 || BoardInfo.CHR_Size == 32 || BoardInfo.CHR_Size == 128);
				chr_mask = (BoardInfo.CHR_Size / 8 * 2) - 1;
			}

			SetMirrorType(mmc1.mirror);

			return true;
		}

	}
}
