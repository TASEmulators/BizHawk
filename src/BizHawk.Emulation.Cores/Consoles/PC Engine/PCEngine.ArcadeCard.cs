using BizHawk.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	partial class PCEngine
	{
		private bool ArcadeCard, ArcadeCardRewindHack;
		private int ShiftRegister;
		private byte ShiftAmount;
		private byte RotateAmount;

		private readonly ArcadeCardPage[] ArcadePage = new ArcadeCardPage[4];

		private class ArcadeCardPage
		{
			public byte Control;
			public int Base;
			public ushort Offset;
			public ushort IncrementValue;

			public void Increment()
			{
				if ((Control & 1) == 0)
					return;

				if ((Control & 0x10) != 0)
				{
					Base += IncrementValue;
					Base &= 0xFFFFFF;
				}
				else
					Offset += IncrementValue;
			}

			public int EffectiveAddress
			{
				get
				{
					int address = Base;
					if ((Control & 2) != 0)
						address += Offset;
					if ((Control & 8) != 0)
						address += 0xFF0000;
					return address & 0x1FFFFF;
				}
			}
		}

		private void WriteArcadeCard(int addr, byte value)
		{
			if (!ArcadeCard)
			{
				return;
			}

			var page = ArcadePage[(addr >> 4) & 3];
			switch (addr & 0x0F)
			{
				case 0:
				case 1:
					ArcadeRam[page.EffectiveAddress] = value;
					page.Increment();
					break;
				case 2:
					page.Base &= ~0xFF;
					page.Base |= value;
					break;
				case 3:
					page.Base &= ~0xFF00;
					page.Base |= (value << 8);
					break;
				case 4:
					page.Base &= ~0xFF0000;
					page.Base |= (value << 16);
					break;
				case 5:
					page.Offset &= 0xFF00;
					page.Offset |= value;
					break;
				case 6:
					page.Offset &= 0x00FF;
					page.Offset |= (ushort)(value << 8);
					if ((page.Control & 0x60) == 0x40)
					{
						page.Base += page.Offset + (((page.Control & 0x08) == 0) ? 0 : 0xFF00000);
						page.Base &= 0xFFFFFF;
					}
					break;
				case 7:
					page.IncrementValue &= 0xFF00;
					page.IncrementValue |= value;
					break;
				case 8:
					page.IncrementValue &= 0x00FF;
					page.IncrementValue |= (ushort)(value << 8);
					break;
				case 9:
					page.Control = (byte)(value & 0x7F);
					break;
				case 10:
					if ((page.Control & 0x60) == 0x60)
					{
						page.Base += page.Offset;
						page.Base &= 0xFFFFFF;
						if ((page.Control & 8) != 0)
						{
							page.Base += 0xFF0000;
							page.Base &= 0xFFFFFF;
						}
					}
					break;
			}
		}

		private byte ReadArcadeCard(int addr)
		{
			if (!ArcadeCard) return 0xFF;
			var page = ArcadePage[(addr >> 4) & 3];
			switch (addr & 0x0F)
			{
				case 0:
				case 1:
					byte value = ArcadeRam[page.EffectiveAddress];
					page.Increment();
					return value;
				case 2: return (byte)(page.Base >> 0);
				case 3: return (byte)(page.Base >> 8);
				case 4: return (byte)(page.Base >> 16);
				case 5: return (byte)(page.Offset >> 0);
				case 6: return (byte)(page.Offset >> 8);
				case 7: return (byte)(page.IncrementValue >> 0);
				case 8: return (byte)(page.IncrementValue >> 8);
				case 9: return (byte)(page.Control >> 0);
				case 10: return 0;
			}

			return 0xFF;
		}

		public void ArcadeCardSyncState(Serializer ser)
		{
			ser.BeginSection(nameof(ArcadeCard));
			ser.Sync(nameof(ShiftRegister), ref ShiftRegister);
			ser.Sync(nameof(ShiftAmount), ref ShiftAmount);
			ser.Sync(nameof(RotateAmount), ref RotateAmount);

			if (!ArcadeCardRewindHack || ser.IsText)
			{
				ser.Sync("ArcadeRAM", ref ArcadeRam, false);
			}

			for (int i = 0; i < 4; i++)
			{
				ser.BeginSection("Page" + i);

				ser.Sync(nameof(ArcadeCardPage.Control), ref ArcadePage[i].Control);
				ser.Sync(nameof(ArcadeCardPage.Base), ref ArcadePage[i].Base);
				ser.Sync(nameof(ArcadeCardPage.Offset), ref ArcadePage[i].Offset);
				ser.Sync(nameof(ArcadeCardPage.IncrementValue), ref ArcadePage[i].IncrementValue);
				ser.EndSection();
			}

			ser.EndSection();
		}
	}
}
