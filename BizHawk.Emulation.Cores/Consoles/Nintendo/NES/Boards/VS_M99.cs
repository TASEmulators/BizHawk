using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//VS System Mapper 99

	[NesBoardImplPriority]
	public sealed class MAPPER99 : NesBoardBase
	{
		//configuration
		int prg_byte_mask, chr_mask;

		//state
		int chr;

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

			prg_byte_mask = Cart.prg_size * 1024 - 1;
			chr_mask = (Cart.chr_size / 8) - 1;

			//update the state of the dip switches
			//this is only done at power on
			NES.VS_dips[0] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_1 ? 1 : 0);
			NES.VS_dips[1] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_2 ? 1 : 0);
			NES.VS_dips[2] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_3 ? 1 : 0);
			NES.VS_dips[3] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_4 ? 1 : 0);
			NES.VS_dips[4] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_5 ? 1 : 0);
			NES.VS_dips[5] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_6 ? 1 : 0);
			NES.VS_dips[6] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_7 ? 1 : 0);
			NES.VS_dips[7] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_8 ? 1 : 0);

			return true;
		}

		// this now tracks coins
		public override void WriteExp(int addr, byte value)
		{
			//but we don't actually need to do anything yet
		}

		public override byte ReadExp(int addr)
		{
			//what are we reading?
			return 0;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(addr & 0x1FFF) + ((NES.VS_chr_reg & chr_mask) << 13)];
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

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if (Vram != null)
					Vram[addr] = value;
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
			ser.Sync(nameof(chr), ref chr);
			ser.Sync("VS_CIRAM", ref CIRAM_VS, false);
		}

		public override byte ReadPrg(int addr)
		{
			if (Cart.prg_size==48)
			{
				if (addr<0x2000)
				{
					return Rom[(addr & 0x1FFF) + ((NES.VS_prg_reg*4) << 13)];
				} else
					return Rom[addr];	
			}
			else
			{
				return Rom[addr];
			}
			
		}
	}
}
