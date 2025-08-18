﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// Adapted from Nestopia src
	internal sealed class Mapper121 : MMC3Board_Base
	{
		private byte[] exRegs = new byte[3];

		private readonly byte[] lut = { 0x00, 0x83, 0x42, 0x00 };

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER121":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(exRegs), ref exRegs, false);
		}

		public override byte ReadExp(int addr)
		{
			if (addr >= 0x1000)
			{
				return exRegs[2];
			}
			else
			{
				return base.ReadExp(addr);
			}
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1000) // 0x5000-0x5FFF
			{
				exRegs[2] = lut[value & 0x3];
			}
		}

		public override byte ReadPrg(int addr)
		{
			int bank_8k = addr >> 13;

			if (bank_8k == 2 && exRegs[0] > 0)
			{
				bank_8k = exRegs[0] & prg_mask;
			}
			else if (bank_8k == 3 && exRegs[1] > 0)
			{
				bank_8k = exRegs[1] & prg_mask;
			}
			else
				bank_8k = base.Get_PRGBank_8K(addr);

			bank_8k &= prg_mask;
			addr = (bank_8k << 13) | (addr & 0x1FFF);
			return Rom[addr];
		}

		public override void WritePrg(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if ((addr & 3) == 3)
				{
					switch (value)
					{
						case 0x28: exRegs[0] = 0x0C; break;
						case 0x26: exRegs[1] = 0x08; break;
						case 0xAB: exRegs[1] = 0x07; break;
						case 0xEC: exRegs[1] = 0x0D; break;
						case 0xEF: exRegs[1] = 0x0D; break;
						case 0xFF: exRegs[1] = 0x09; break;

						case 0x20: exRegs[1] = 0x13; break;
						case 0x29: exRegs[1] = 0x1B; break;

						default: exRegs[0] = 0x0; exRegs[1] = 0x0; break;
					}
				}
				else if ((addr & 1)>0)
					base.WritePrg(addr, value);
				else //if (addr==0)
					base.WritePrg(0, value);
			}
			else
			{
				base.WritePrg(addr, value);
			}
		}
	}
}
