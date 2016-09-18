using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper215 : MMC3Board_Base
	{
		private ByteBuffer exRegs = new ByteBuffer(4);
		private byte cmd;

		private int prg_mask_32k;

		private readonly byte[,] regperm = new byte[,]
		{
			{ 0, 1, 2, 3, 4, 5, 6, 7 },
			{ 0, 2, 6, 1, 7, 3, 4, 5 },
			{ 0, 5, 4, 1, 7, 2, 6, 3 },   // unused
			{ 0, 6, 3, 7, 5, 2, 4, 1 },
			{ 0, 2, 5, 3, 6, 1, 7, 4 },
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
		};

		private readonly byte[,] adrperm = new byte[,]
		{
			{ 0, 1, 2, 3, 4, 5, 6, 7 },
			{ 3, 2, 0, 4, 1, 5, 6, 7 },
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // unused
			{ 5, 0, 1, 2, 3, 7, 6, 4 },
			{ 3, 1, 0, 5, 2, 4, 6, 7 },
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
			{ 0, 1, 2, 3, 4, 5, 6, 7 },   // empty
		};

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER215":
					break;
				default:
					return false;
			}

			BaseSetup();
			exRegs[1] = 3;

			prg_mask_32k = Cart.prg_size / 32 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exRegs", ref exRegs);
			ser.Sync("cmd", ref cmd);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr == 0x1000) { exRegs[0] = value; }
			if (addr == 0x1001) { exRegs[1] = value; }
			if (addr == 0x1007) { exRegs[2] = value; }

			base.WriteEXP(addr, value);
		}

		//public override void WritePRG(int addr, byte value)
		//{
		//	cmd = value;
		//	int addr2 = addr + 0x8000;
		//	byte dat = value;

		//	var b = exRegs[2];

		//	byte adr = (byte)( adrperm[exRegs[2], ((addr2 >> 12) & 6) | (addr2 & 1)]);
		//	short addr3 = (short)((adr & 1) | ((adr & 6) << 12) | 0x8000);
		//	if (adr < 4)
		//	{
		//		if (adr == 0)
		//		{
		//			dat = (byte)((dat & 0xC0) | (regperm[exRegs[2], dat & 7]));
		//		}
				
		//	}

		//	base.WritePRG(addr, dat);
		//}

		public override byte ReadPRG(int addr)
		{
			if ((exRegs[0] & 0x40) > 0)
			{
				byte sbank = (byte)(exRegs[1] & 0x10);
				if ((exRegs[0] & 0x80) > 0)
				{
					byte bank =(byte)(((exRegs[1] & 3) << 4) | (exRegs[0] & 0x7) | (sbank >> 1));
					if ((exRegs[0] & 0x20) > 0)
					{
						return ROM[(bank << 15) + addr];
					}
					else
					{
						return ROM[(bank << 14) + (addr & 0x3FFF)];
					}
				}
				else
				{
					return ROM[(((exRegs[1] & 3) << 5) | (cmd & 0x0F) | sbank) + addr];
				}
			}
			else
			{
				if ((exRegs[0] & 0x80) > 0)
				{
					byte bank = (byte)(((exRegs[1] & 3) << 4) | (exRegs[0] & 0xF));
					if ((exRegs[0] & 0x20) > 0)
					{
						return ROM[((bank >> 1) << 15) + addr];
					}
					else
					{
						return ROM[(bank << 14) + (addr & 0x3FFF)];
					}
				}
				else
				{
					int bank32k = (((exRegs[1] & 3) << 5) | (cmd & 0x1F));
					bank32k &= prg_mask_32k;
					return ROM[(bank32k << 15) + addr];
				}
			}
		}
	}
}
