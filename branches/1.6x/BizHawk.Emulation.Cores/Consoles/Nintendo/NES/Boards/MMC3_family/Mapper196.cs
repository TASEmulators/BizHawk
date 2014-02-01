using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class Mapper196 : MMC3Board_Base
	{
		// pirate crap
		// behavior from fceumm
		// standard MMC3, plus scrambled address lines to access the MMC3, and a bit of extra hardware
		// adding a 32K prg banking mode

		// config
		int prg_bank_mask_32k;

		// state
		bool prgmode;
		int prgreg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER196":
					break;
				default:
					return false;
			}

			Cart.wram_size = 0;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.BeginSection("Mapper196");
			ser.Sync("prgmode", ref prgmode);
			ser.Sync("prgreg", ref prgreg);
			ser.EndSection();
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr < 0x1000)
			{
				if (!prgmode)
					Console.WriteLine("M196: 32K prg activated");
				prgmode = true; // no way to turn this off once activated?
				prgreg = value & 15 | value >> 4;
				prgreg &= prg_bank_mask_32k;
			}
		}

		public override void WritePRG(int addr, byte value)
		{
			// addresses are scrambled
			if (addr >= 0x4000)
			{
				addr = (addr & 0xFFFE) | ((addr >> 2) & 1) | ((addr >> 3) & 1);
			}
			else
			{
				addr = (addr & 0xFFFE) | ((addr >> 2) & 1) | ((addr >> 3) & 1) | ((addr >> 1) & 1);
			}
			base.WritePRG(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			if (prgmode)
			{
				return ROM[addr | prgreg << 15];
			}
			else
			{
				return base.ReadPRG(addr);
			}
		}

	}
}
