using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//aka mapper 118
	//wires the mapper outputs to control the nametables
	public sealed class TLSROM : MMC3Board_Base
	{
		public int[] nametables = new int[4];

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-TLSROM": //pro sport hockey (U)
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "MAPPER118":
					AssertVram(0);
					break;
				case "HVC-TKSROM": //ys III: wanderers from ys (J)
					AssertPrg(256); AssertChr(128); AssertVram(0); AssertWram(8);
					AssertBattery(true);
					break;
				case "TENGEN-800037": //Alien Syndrome (U)
									  // this board is actually a RAMBO-1 (mapper064) with TLS-style rewiring
									  // but it seems to work fine here, so lets not worry about it
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				case "MAPPER158":
					// as above
					AssertVram(0); Cart.wram_size = 0;
					break;
				case "HVC-TLSROM":
					AssertPrg(256); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			//maybe need other initialization
			nametables[0] = 0;
			nametables[1] = 1;
			nametables[2] = 0;
			nametables[3] = 1;

			return true;
		}

		public override void WritePrg(int addr, byte value)
		{
			int nt = value >> 7;

			if ((addr & 0x6001) == 0x1)
			{
				if (!mmc3.chr_mode)
				{
					switch (mmc3.reg_addr)
					{
						case 0:
							nametables[0] = nt;
							nametables[1] = nt;
							break;
						case 1:
							nametables[2] = nt;
							nametables[3] = nt;
							break;
					}
				}
				else
				{
					switch (mmc3.reg_addr)
					{
						case 2:
							nametables[0] = nt;
							break;
						case 3:
							nametables[1] = nt;
							break;
						case 4:
							nametables[2] = nt;
							break;
						case 5:
							nametables[3] = nt;
							break;
					}
				}
			}

			if ((addr & 0x6001) != 0x2000)
				base.WritePrg(addr, value);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000) return base.ReadPpu(addr);
			else
			{
				int nt = ((addr - 0x2000) >> 10) & 0x3;
				addr = 0x2000 + (addr & 0x3FF) + (nametables[nt] << 10);
				return base.ReadPpu(addr);

			}
		}
		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000) base.WritePpu(addr, value);
			else
			{
				int nt = ((addr - 0x2000) >> 10) & 0x3;
				addr = 0x2000 + (addr & 0x3FF) + (nametables[nt] << 10);
				base.WritePpu(addr, value);
			}

		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(nametables), ref nametables, false);
		}
	}
}