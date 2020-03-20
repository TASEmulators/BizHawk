using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// basic on FCEUX src
	public sealed class Mapper150 : NesBoardBase
	{
		private byte[] latch = new byte[8];
		private int cmd;
		private int chr_mask;
		private int prg_mask;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER150":
					break;
				default:
					return false;
			}

			chr_mask = (Cart.chr_size / 8) - 1;
			prg_mask = (Cart.prg_size / 32) - 1;

			latch[1] = 3;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(latch), ref latch, false);
			ser.Sync(nameof(cmd), ref cmd);
		}

		public override void WriteExp(int addr, byte value)
		{
			addr += 0x4000;
			Write(addr, value);
			SetMirroring(latch[2]);
		}

		public override void WriteWram(int addr, byte value)
		{
			addr += 0x6000;
			Write(addr, value);
			SetMirroring(latch[2]);
		}

		private void Write(int addr, byte value)
		{
			addr &= 0x4101;
			if (addr == 0x4100)
			{
				cmd = value & 7;
			}
			else
			{
				switch (cmd)
				{
					case 2:
						latch[0] = (byte)(value & 1);
						latch[3] = (byte)((value & 1) << 3);
						break;
					case 4:
						latch[4] = (byte)((value & 1) << 2);
						break;
					case 5:
						latch[0] = (byte)(value & 7);
						break;
					case 6:
						latch[1] = (byte)(value & 3);
						break;
					case 7:
						latch[2] = (byte)(value >> 1);
						break;
				}
			}
		}

		private void SetMirroring(byte val)
		{
			switch (val & 3)
			{
				case 0:
					SetMirrorType(EMirrorType.Horizontal);
					break;
				case 1:
					SetMirrorType(EMirrorType.Vertical);
					break;
				case 2:
					SetMirroring(0, 1, 1, 1);
					break;
				case 3:
					SetMirrorType(EMirrorType.OneScreenB);
					break;
			}
		}

		public override byte ReadExp(int addr)
		{
			byte ret;
			addr += 0x4000;
			if ((addr & 0x4100) == 0x4100)
			{
				ret = (byte)((~cmd) & 0x3F);
			}
			else
			{
				ret = NES.DB;
			}

			return ret;
		}

		public override byte ReadPrg(int addr)
		{
			return Rom[((latch[0] & prg_mask) << 15) + addr];
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = latch[1] | latch[3] | latch[4];
				bank &= chr_mask;
				return Vrom[(bank << 13) + addr];
			}

			return base.ReadPpu(addr);
		}
	}
}
