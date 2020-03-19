using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

// http://wiki.nesdev.com/w/index.php/INES_Mapper_062
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper62 : NES.NESBoardBase
	{
		bool prg_mode = false;
		int chr_reg;
		int prg_reg; 

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER062":
					break;
				default:
					return false;
			}
			
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(prg_reg), ref prg_reg);
			ser.Sync(nameof(chr_reg), ref chr_reg);
			ser.Sync(nameof(prg_mode), ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_mode = addr.Bit(5);
			if (addr.Bit(7))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			prg_reg = (addr & 0x40) | ((addr >> 8) & 0x3F);
			chr_reg = ((addr & 0x1F) << 2) | (value & 0x03); 
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
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
	}
}
