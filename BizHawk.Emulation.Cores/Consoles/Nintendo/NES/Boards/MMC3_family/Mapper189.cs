using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper189 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER189":
					break;
				case "TXC-PT8154":
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		int prg;

		protected override int Get_PRGBank_8K(int addr)
		{
			int block_8k = addr >> 13;
			return (prg * 4) + block_8k;
		}

		public override void WriteWram(int addr, byte value)
		{
			base.WriteWram(addr, value);
			int prg_a = value & 0xF;
			int prg_b = (value>>4)&0xF;
			prg = prg_a | prg_b;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg), ref prg);
		}

		public override void WriteExp(int addr, byte value)
		{
			WriteWram(addr, value);
		}

	}
}