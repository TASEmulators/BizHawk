using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper236 : NesBoardBase
	{
		[MapperProp]
		public byte CartSwitch_70in1 = 0xd;

		[MapperProp]
		public byte CartSwitch_800in1 = 06;

		private bool isLargeBanks = false;
		private byte large_bank;
		private byte prg_bank;
		private byte chr_bank;
		private byte bank_mode;

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				
				case "UNIF_BMC-70in1":
					isLargeBanks = false;
					break;
				case "MAPPER236":
				case "UNIF_BMC-70in1B":
					isLargeBanks = true;
					break;
				default:
					return false;
			}

			AutoMapperProps.Apply(this);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(isLargeBanks), ref isLargeBanks);
			ser.Sync(nameof(large_bank), ref large_bank);
			ser.Sync(nameof(prg_bank), ref prg_bank);
			ser.Sync(nameof(chr_bank), ref chr_bank);
			ser.Sync(nameof(bank_mode), ref bank_mode);
			ser.Sync("Cart_Switch", ref CartSwitch_70in1);
			ser.Sync("Cart_Switch", ref CartSwitch_800in1);
		}

		public override void WritePrg(int addr, byte value)
		{
			addr += 0x8000;
			if ((addr & 0x4000) > 0)
			{
				bank_mode = (byte)(addr & 0x30);
				prg_bank = (byte)(addr & 7);
			}
			else
			{
				var mirroring = ((addr & 0x20) >> 5) ^ 1;
				SetMirrorType(mirroring > 0 ? EMirrorType.Vertical : EMirrorType.Horizontal);

				if (isLargeBanks)
				{
					large_bank = (byte)((addr & 3) << 3);
				}
				else
				{
					chr_bank = (byte)(addr & 7);
				}
			}
		}

		public override byte ReadPrg(int addr)
		{
			switch (bank_mode)
			{
				default:
					throw new InvalidOperationException("Unexpected bank_mode value");
				case 0x00:
				case 0x10:
					int bank;
					if (addr < 0x4000)
					{
						bank = (large_bank | prg_bank);
					}
					else
					{
						bank = (large_bank | 7);
					}

					if (bank_mode == 0x10)
					{
						addr = (addr & 0x7FF0) | ((isLargeBanks ? CartSwitch_800in1 : CartSwitch_70in1) & 0xf);
					}
					return Rom[(bank << 14) + (addr & 0x3FFF)];
				case 0x20:
					return Rom[(((large_bank | prg_bank) >> 1) << 15) + addr];
				case 0x30:
					return Rom[((large_bank | prg_bank) << 14) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000 && Vrom != null)
			{
				return Vrom[(chr_bank << 13) + (addr & 0x1FFF)];
			}

			return base.ReadPpu(addr);
		}
	}
}
