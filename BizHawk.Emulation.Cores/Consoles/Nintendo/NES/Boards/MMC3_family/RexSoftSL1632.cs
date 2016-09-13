using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// p-p-p-p-pirate?
	// http://svn.opennestopia.staulkor.com/Nestopia/core/board/NstBoardRexSoftSl1632.cpp
	public class RexSoftSL1632 : MMC3Board_Base
	{
		// state
		byte exmode;
		int[] exprg = new int[4];
		int[] exchr = new int[8];
		byte exnmt;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-SL1632":
					break;
				default:
					return false;
			}
			BaseSetup();
			exprg[3] = prg_mask;
			exprg[2] = prg_mask - 1;
			return true;
		}

		public override void SyncState(BizHawk.Common.Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("exmode", ref exmode);
			ser.Sync("exprg", ref exprg, false);
			ser.Sync("exchr", ref exchr, false);
			ser.Sync("exnmt", ref exnmt);
		}

		public override byte ReadPRG(int addr)
		{
			if (exmode.Bit(1))
			{
				return base.ReadPRG(addr);
			}
			else
			{
				int b = addr >> 13;
				b = exprg[b];
				b &= prg_mask;
				return ROM[addr & 0x1fff | b << 13];
			}
		}

		void SinkMirror(bool flip)
		{
			if (flip)
				SetMirrorType(exnmt.Bit(0) ? EMirrorType.Vertical : EMirrorType.Horizontal);
			else
				SetMirrorType(!exnmt.Bit(0) ? EMirrorType.Vertical : EMirrorType.Horizontal);
		}

		readonly static byte[] modes = { 5, 5, 3, 1 };
		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank;
				if (exmode.Bit(1))
				{
					bank = Get_CHRBank_1K(addr);
					int tmp = mmc3.get_chr_mode ? 2 : 0;
					bank |= exmode << modes[addr >> 11 ^ tmp] & 0x100;
				}
				else
				{
					bank = exchr[addr >> 10];
				}
				bank &= chr_mask;
				return VROM[addr & 0x3ff | bank << 10];
			}
			else
			{
				return base.ReadPPU(addr);
			}
		}


		// this is stupid as hell
		public override void WritePRG(int addr, byte value)
		{
			//Console.WriteLine("{0:x4}:{1:x2}", addr, value);

			if ((addr & 0x2131) == 0x2131 && (exmode != value))
			{
				exmode = value;
				if (!exmode.Bit(1))
				{
					SinkMirror(false);
				}
			}

			if (exmode.Bit(1))
			{
				switch (addr & 0x6001)
				{
					case 0x0000: base.WritePRG(0x0000, value); break;
					case 0x0001: base.WritePRG(0x0001, value); break;
					case 0x2000: SinkMirror(true); break;
					case 0x2001: base.WritePRG(0x2001, value); break;
					case 0x4000: base.WritePRG(0x4000, value); break;
					case 0x4001: base.WritePRG(0x4001, value); break;
					case 0x6000: base.WritePRG(0x6000, value); break;
					case 0x6001: base.WritePRG(0x6001, value); break;

				}
			}
			else if (addr >= 0x3000 && addr <= 0x6003)
			{
				int offset = addr << 2 & 0x4;
				addr = ((((addr & 0x2) | addr >> 10) >> 1) + 2) & 0x7;
				exchr[addr] = (exchr[addr] & 0xf0 >> offset) | ((value & 0x0f) << offset);
				// sync chr
			}
			else
			{
				switch (addr & 0x7003)
				{
					case 0x0000: exprg[0] = value; break;
					case 0x1000: exnmt = value; SinkMirror(false); break;
					case 0x2000: exprg[1] = value; break;
				}
			}
		}



	}
}
