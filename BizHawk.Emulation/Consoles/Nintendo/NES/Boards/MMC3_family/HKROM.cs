using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//aka MMC6 aka StarTropics and StarTropics 2
	//for simplicity's sake, the behaviour of mmc6 is wrapped up into this board since it isnt used anywhere else
	[NES.INESBoardImplPriority]
	public class HKROM : MMC3Board_Base
	{
		//configuration

		//state
		bool wram_enabled;
		bool wram_h_enabled, wram_l_enabled;
		bool wram_h_enabled_write, wram_l_enabled_write;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-HKROM":
					AssertPrg(256); AssertChr(256); AssertVram(0); AssertWram(0,1);
					Cart.wram_size = 1; //1K of wram is in the mmc6
					Cart.wram_battery = true; //and its battery backed.
					break;
				case "MAPPER004_MMC6":
					Cart.wram_size = 1; //1K of wram is in the mmc6
					Cart.wram_battery = true; //and its battery backed.
					break;
				default:
					return false;
			}

			BaseSetup();
			mmc3.MMC3Type = MMC3.EMMC3Type.MMC6;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("wram_enabled", ref wram_enabled);
			ser.Sync("wram_h_enabled", ref wram_h_enabled);
			ser.Sync("wram_l_enabled", ref wram_l_enabled);
			ser.Sync("wram_h_enabled_write", ref wram_h_enabled_write);
			ser.Sync("wram_l_enabled_write", ref wram_l_enabled_write);
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr & 0x6001)
			{
				case 0x0000: //$8000
					//an extra wram enabled bit is here
					wram_enabled = value.Bit(5);
					if (!wram_enabled)
					{
						wram_h_enabled = wram_l_enabled = false;
						wram_h_enabled_write = wram_l_enabled_write = false;
					}
					break;
				
				case 0x2001: //$A001
					//a whole host of wram configurations
					if (wram_enabled)
					{
						wram_h_enabled = value.Bit(7);
						wram_l_enabled = value.Bit(5);
						wram_h_enabled_write = value.Bit(6);
						wram_l_enabled_write = value.Bit(4);
					}
					break;
			}
			base.WritePRG(addr, value);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (addr < 0x1000)
				return;

			//probably wrong:
			//if (!wram_enabled) return;

			addr &= (1 << 10) - 1;
			int block = addr >> 9;
			bool block_enabled = (block == 1) ? wram_h_enabled : wram_l_enabled;
			bool write_enabled = (block == 1) ? wram_h_enabled_write : wram_l_enabled_write;

			if (write_enabled && block_enabled)
				base.WriteWRAM(addr, value);
		}

		public override byte ReadWRAM(int addr)
		{
			byte open_bus = 0xFF; //open bus
			if (addr < 0x1000)
				return open_bus;

			//probably wrong:
			//if (!wram_enabled) return open_bus;

			addr &= (1 << 10) - 1;
			int block = addr >> 9;
			bool block_enabled = (block == 1) ? wram_h_enabled : wram_l_enabled;

			if (!wram_h_enabled && !wram_l_enabled)
				return open_bus;

			if (block_enabled)
				return base.ReadWRAM(addr);
			else return 0;
		}

	}
}