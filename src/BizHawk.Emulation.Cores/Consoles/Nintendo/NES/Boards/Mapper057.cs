using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper057 : NesBoardBase
	{
		// http://wiki.nesdev.com/w/index.php/INES_Mapper_057

		private bool prg_mode = false;
		private int chr_reg_low_0, chr_reg_low_1, chr_reg;
		private int prg_reg;

		[MapperProp]
		public int Mapper57_DipSwitch = 0;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
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
			ser.Sync(nameof(prg_reg), ref prg_reg);
			ser.Sync(nameof(chr_reg), ref chr_reg);
			ser.Sync(nameof(chr_reg_low_0), ref chr_reg);
			ser.Sync(nameof(chr_reg_low_1), ref chr_reg);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePrg(int addr, byte value)
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

		public override byte ReadPrg(int addr)
		{
			if (prg_mode)
			{
				return Rom[((prg_reg >> 1) * 0x8000) + addr];
			}
			else
			{
				return Rom[(prg_reg * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				return Vrom[(chr_reg * 0x2000) + addr];
			}
			return base.ReadPpu(addr);
		}

		public override byte ReadWram(int addr)
		{
			return (byte)(Mapper57_DipSwitch & 3);
		}
	}
}
