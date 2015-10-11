using BizHawk.Common;
namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_UNL_EDU2000 : NES.NESBoardBase
	{
		private int _reg;

		private int _prgMask32;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-EDU2000":
					break;
				default:
					return false;
			}

			_prgMask32 = Cart.prg_size / 32 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("reg", ref _reg);
		}

		public override void WritePRG(int addr, byte value)
		{
			_reg = value;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[((_reg & _prgMask32) * 0x8000) + (addr & 0x7FFF)];
		}
	}
}
