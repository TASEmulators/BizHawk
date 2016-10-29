using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//VS System Mapper 99

	[NES.INESBoardImplPriority]
	public sealed class MAPPER99 : NES.NESBoardBase
	{
		//configuration
		int prg_byte_mask, chr_mask;

		//state
		int chr;

		[MapperProp]
		public byte Dip_Switch_1;
		[MapperProp]
		public byte Dip_Switch_2;
		[MapperProp]
		public byte Dip_Switch_3;
		[MapperProp]
		public byte Dip_Switch_4;
		[MapperProp]
		public byte Dip_Switch_5;
		[MapperProp]
		public byte Dip_Switch_6;
		[MapperProp]
		public byte Dip_Switch_7;
		[MapperProp]
		public byte Dip_Switch_8;

		//the VS actually does have 2 KB of nametable address space
		//let's make the extra space here, instead of in the main NES to avoid confusion
		byte[] CIRAM_VS = new byte[0x800];

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER099":
					NES._isVS = true;
					break;
				default:
					return false;
			}

			if (Cart.DB_GameInfo.Hash == "D232F7BE509E3B745D9E9803DA945C3FABA37A70") // ninja kun
				NES._isVS2c05 = 1;
			if (Cart.DB_GameInfo.Hash == "CAE9CB4C0452C56BED58AEACCEACE8A3107F843A") // mighty bomb jack
				NES._isVS2c05 = 2;
			if (Cart.DB_GameInfo.Hash == "21674A6571F0D4C812B9C30092C0C5ABED0C92E1") // Gumshoe
				NES._isVS2c05 = 3;
			

			prg_byte_mask = Cart.prg_size * 1024 - 1;
			chr_mask = (Cart.chr_size / 8) - 1;

			AutoMapperProps.Apply(this);


			//update the state of the dip switches
			//this is only done at power on
			NES.VS_dips[0] = (byte)(Dip_Switch_1 & 1);
			NES.VS_dips[1] = (byte)(Dip_Switch_2 & 1);
			NES.VS_dips[2] = (byte)(Dip_Switch_3 & 1);
			NES.VS_dips[3] = (byte)(Dip_Switch_4 & 1);
			NES.VS_dips[4] = (byte)(Dip_Switch_5 & 1);
			NES.VS_dips[5] = (byte)(Dip_Switch_6 & 1);
			NES.VS_dips[6] = (byte)(Dip_Switch_7 & 1);
			NES.VS_dips[7] = (byte)(Dip_Switch_8 & 1);

			return true;
		}

		// this now tracks coins
		public override void WriteEXP(int addr, byte value)
		{
			//but we don't actually need to do anything yet
		}

		public override byte ReadEXP(int addr)
		{
			//what are we reading?
			return 0;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(addr & 0x1FFF) + (NES.VS_chr_reg << 13)];
			}
			else
			{
				addr = addr - 0x2000;
				if (addr<0x800)
				{
					return NES.CIRAM[addr];
				}
				else
				{
					return CIRAM_VS[addr-0x800];
				}
				
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (VRAM != null)
					VRAM[addr] = value;
			}
			else
			{
				addr = addr - 0x2000;
				if (addr < 0x800)
				{
					NES.CIRAM[addr] = value;
				}
				else
				{
					CIRAM_VS[addr-0x800] = value;
				}
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("VS_CIRAM", ref CIRAM_VS, false);
			ser.Sync("Dip_1", ref Dip_Switch_1);
			ser.Sync("Dip_2", ref Dip_Switch_2);
			ser.Sync("Dip_3", ref Dip_Switch_3);
			ser.Sync("Dip_4", ref Dip_Switch_4);
			ser.Sync("Dip_5", ref Dip_Switch_5);
			ser.Sync("Dip_6", ref Dip_Switch_6);
			ser.Sync("Dip_7", ref Dip_Switch_7);
			ser.Sync("Dip_8", ref Dip_Switch_8);
		}

		public override byte ReadPRG(int addr)
		{
			if (Cart.prg_size==48)
			{
				if (addr<0x2000)
				{
					return ROM[(addr & 0x1FFF) + ((NES.VS_prg_reg*4) << 13)];
				} else
					return ROM[addr];	
			}
			else
			{
				return ROM[addr];
			}
			
		}
	}
}
