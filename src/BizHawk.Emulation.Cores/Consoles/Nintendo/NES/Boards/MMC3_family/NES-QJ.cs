using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class NES_QJ : MMC3Board_Base
	{
		//state
		private int block;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(block), ref block);
		}

		public override void WritePrg(int addr, byte value)
		{
			base.WritePrg(addr, value);
			SetMirrorType(mmc3.MirrorType);  //often redundant, but gets the job done
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER047": // Junk roms
					break;
				case "NES-QJ": //super spike v'ball / nintendo world cup
					AssertPrg(256); AssertChr(256); AssertVram(0); AssertWram(0);
					AssertBattery(false);
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			//base logic will return the mmc reg, which needs to be masked without awareness of the extra block
			return (base.Get_PRGBank_8K(addr) & 0xF) + block * 16;
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			//base logic will return the mmc reg, which needs to be masked without awareness of the extra block
			return (base.Get_CHRBank_1K(addr) & 0x7F) + block * 128;
		}

		public override byte ReadWram(int addr)
		{
			return (byte)block;
		}
		public override void WriteWram(int addr, byte value)
		{
			if (mmc3.wram_enable && !mmc3.wram_write_protect)
			{
				block = value & 1;
			}
		}
	}
}