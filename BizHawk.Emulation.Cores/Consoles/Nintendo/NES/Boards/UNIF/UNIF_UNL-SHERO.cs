using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class UNIF_UNL_SHERO : MMC3Board_Base
	{
		[MapperProp]
		public bool RegionAsia = false;

		private byte reg;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "UNIF_UNL-SHERO":
					break;
				default:
					return false;
			}

			BaseSetup();
			AutoMapperProps.Apply(this);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(reg), ref reg);
			ser.Sync(nameof(RegionAsia), ref RegionAsia);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr == 0x100)
			{
				reg = value;
			}

			base.WriteExp(addr, value);
		}

		public override byte ReadExp(int addr)
		{
			if (addr == 0x100)
			{
				return (byte)(RegionAsia ? 0xFF : 00);
			}

			return base.ReadExp(addr);
		}


		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				if ((reg & 0x40) > 0)
				{
					return Vram[addr];
				}
				else
				{
					int bank_1k = Get_CHRBank_1K(addr);
					addr = (bank_1k << 10) | (addr & 0x3FF);

					return Vrom[addr];
				}

			}
			else
				return Vram[addr];
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				if ((reg & 0x40) > 0)
				{
					Vram[addr] = value;
				}
				else
				{
					//nothing to write to VROM
				}

			}
			else
				Vram[addr] = value;	
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			if (addr < 0x800)
			{
				return base.Get_CHRBank_1K(addr) | ((reg & 8) << 5);
			}
			else if (addr < 0x1000)
			{
				return base.Get_CHRBank_1K(addr) | ((reg & 4) << 6);
			}
			else if (addr < 0x1800)
			{
				return base.Get_CHRBank_1K(addr) | ((reg & 1) << 8);
			}
			else
			{
				return base.Get_CHRBank_1K(addr) | ((reg & 2) << 7);
			}
		}
	}
}
