using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	[NES.INESBoardImplCancel]
	public class FDS : NES.NESBoardBase
	{
		/// <summary>
		/// fds bios image; should be 8192 bytes
		/// </summary>
		public byte[] biosrom;

		// as we have [INESBoardImplCancel], this will only be called with an fds disk image
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			if (biosrom == null || biosrom.Length != 8192)
				throw new Exception("FDS bios image needed!");

			Cart.vram_size = 8;
			Cart.wram_size = 32;
			Cart.wram_battery = false;
			Cart.system = "FDS";
			Cart.board_type = "FAMICOM_DISK_SYSTEM";
			// set mirroring

			return true;
		}


		public override byte ReadWRAM(int addr)
		{
			return WRAM[addr & 0x1fff];
		}

		public override void WriteWRAM(int addr, byte value)
		{
			WRAM[addr & 0x1fff] = value;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr >= 0x6000)
				return biosrom[addr & 0x1fff];
			else
				return WRAM[addr + 0x2000];
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr < 0x6000)
				WRAM[addr + 0x2000] = value;
		}

		public override void WriteEXP(int addr, byte value)
		{
		}

		public override byte ReadEXP(int addr)
		{
			return 0;
		}

	}
}
