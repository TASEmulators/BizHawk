using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper057 : NES.NESBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_057

		bool prg_mode = false;
		int chr_reg_low_0, chr_reg_low_1, chr_reg;
		int prg_reg;

		[MapperProp]
		public int Mapper57_DipSwitch = 0;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER057":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Horizontal);

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
			ser.Sync("chr_reg_low_0", ref chr_reg);
			ser.Sync("chr_reg_low_1", ref chr_reg);
			ser.Sync("prg_mode", ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0x8800;
			if (addr == 0)
			{
				chr_reg_low_0 = value & 0x07;
				chr_reg &= 0x08;
				chr_reg |= (value & 0x40) >> 3;
			}
			else if(addr == 0x800)
			{
				prg_reg = (value >> 5) & 0x07;
				prg_mode = value.Bit(4);
				chr_reg_low_1 = (value & 0x07);

				if (value.Bit(3))
				{
					SetMirrorType(EMirrorType.Horizontal);
				}
				else
				{
					SetMirrorType(EMirrorType.Vertical);
				}
			}
			
			chr_reg &= ~0x07;
			chr_reg |= (chr_reg_low_0 | chr_reg_low_1);

			//Console.WriteLine("chr page = {0}", chr_reg);
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode)
			{
				return ROM[((prg_reg >> 1) * 0x8000) + addr];
			}
			else
			{
				return ROM[(prg_reg * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(chr_reg * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}

		public override byte ReadWRAM(int addr)
		{
			return (byte)(Mapper57_DipSwitch & 3);
		}
	}
}
