using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public sealed class TMS9918A : IVideoProvider
	{
		public byte[] VRAM = new byte[0x4000];
		private byte[] Registers = new byte[8];
		private byte StatusByte;

		private bool VdpWaitingForLatchByte = true;
		private byte VdpLatch;
		private ushort VdpAddress;
		private byte VdpBuffer;
		private int TmsMode;

		private bool Mode1Bit => (Registers[1] & 16) > 0;
		private bool Mode2Bit => (Registers[0] & 2) > 0;
		private bool Mode3Bit => (Registers[1] & 8) > 0;
		private bool EnableDoubledSprites => (Registers[1] & 1) > 0;
		private bool EnableLargeSprites => (Registers[1] & 2) > 0;
		public bool EnableInterrupts => (Registers[1] & 32) > 0;
		private bool DisplayOn => (Registers[1] & 64) > 0;
		private bool Mode16k => (Registers[1] & 128) > 0;

		public bool InterruptPending
		{
			get => (StatusByte & 0x80) != 0;
			set => StatusByte = (byte)((StatusByte & ~0x02) | (value ? 0x80 : 0x00));
		}

		private int ColorTableBase;
		private int PatternGeneratorBase;
		private int SpritePatternGeneratorBase;
		private int TmsPatternNameTableBase;
		private int TmsSpriteAttributeBase;

		public void WriteVdpControl(byte value)
		{
			if (VdpWaitingForLatchByte)
			{
				VdpLatch = value;
				VdpWaitingForLatchByte = false;
				VdpAddress = (ushort)((VdpAddress & 0x3F00) | value);
				return;
			}

			VdpWaitingForLatchByte = true;
			VdpAddress = (ushort)(((value & 63) << 8) | VdpLatch);
			VdpAddress &= 0x3FFF;
			switch (value & 0xC0)
			{
				case 0x00: // read VRAM
					VdpBuffer = VRAM[VdpAddress];
					VdpAddress++;
					VdpAddress &= 0x3FFF;
					break;
				case 0x40: // write VRAM
					break;
				case 0x80: // VDP register write
					int reg = value & 0x0F;
					WriteRegister(reg, VdpLatch);
					break;
			}
		}

		public void WriteVdpData(byte value)
		{
			VdpWaitingForLatchByte = true;
			VdpBuffer = value;

			VRAM[VdpAddress] = value;
			//if (!Mode16k)
			//    Console.WriteLine("VRAM written while not in 16k addressing mode!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			VdpAddress++;
			VdpAddress &= 0x3FFF;
		}

		private void WriteRegister(int reg, byte data)
		{
			if (reg >= 8) return;

			Registers[reg] = data;
			switch (reg)
			{
				case 0: // Mode Control Register 1
					CheckVideoMode();
					break;
				case 1: // Mode Control Register 2
					CheckVideoMode();
					Cpu.NonMaskableInterrupt = (EnableInterrupts && InterruptPending);
					break;
				case 2: // Name Table Base Address
					TmsPatternNameTableBase = (Registers[2] << 10) & 0x3C00;
					break;
				case 3: // Color Table Base Address
					ColorTableBase = (Registers[3] << 6) & 0x3FC0;
					break;
				case 4: // Pattern Generator Base Address
					PatternGeneratorBase = (Registers[4] << 11) & 0x3800;
					break;
				case 5: // Sprite Attribute Table Base Address
					TmsSpriteAttributeBase = (Registers[5] << 7) & 0x3F80;
					break;
				case 6: // Sprite Pattern Generator Base Address
					SpritePatternGeneratorBase = (Registers[6] << 11) & 0x3800;
					break;
			}
		}

		public byte ReadVdpStatus()
		{
			VdpWaitingForLatchByte = true;
			byte returnValue = StatusByte;
			StatusByte &= 0x1F;
			Cpu.NonMaskableInterrupt = false;

			return returnValue;
		}

		public byte ReadData()
		{
			VdpWaitingForLatchByte = true;
			byte value = VdpBuffer;
			VdpBuffer = VRAM[VdpAddress];
			VdpAddress++;
			VdpAddress &= 0x3FFF;
			return value;
		}

		private void CheckVideoMode()
		{
			if (Mode1Bit) TmsMode = 1;
			else if (Mode2Bit) TmsMode = 2;
			else if (Mode3Bit) TmsMode = 3;
			else TmsMode = 0;
		}

		public void RenderScanline(int scanLine)
		{
			if (scanLine >= 192)
				return;

			if (TmsMode == 2)
			{
				RenderBackgroundM2(scanLine);
				RenderTmsSprites(scanLine);
			}
			else if (TmsMode == 0)
			{
				RenderBackgroundM0(scanLine);
				RenderTmsSprites(scanLine);
			}
			else if (TmsMode == 3)
			{
				RenderBackgroundM3(scanLine);
				RenderTmsSprites(scanLine);
			}
			else if (TmsMode == 1)
			{
				RenderBackgroundM1(scanLine);
				// no sprites (text mode)
			}
		}

		private void RenderBackgroundM0(int scanLine)
		{
			if (!DisplayOn)
			{
				Array.Clear(FrameBuffer, scanLine * 256, 256);
				return;
			}

			int yc = scanLine / 8;
			int yofs = scanLine % 8;
			int FrameBufferOffset = scanLine * 256;
			int PatternNameOffset = TmsPatternNameTableBase + (yc * 32);
			int ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (int xc = 0; xc < 32; xc++)
			{
				int pn = VRAM[PatternNameOffset++];
				int pv = VRAM[PatternGeneratorBase + (pn * 8) + yofs];
				int colorEntry = VRAM[ColorTableBase + (pn / 8)];
				int fgIndex = (colorEntry >> 4) & 0x0F;
				int bgIndex = colorEntry & 0x0F;
				int fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				int bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

				FrameBuffer[FrameBufferOffset++] = ((pv & 0x80) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x40) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x20) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x10) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x08) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x04) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x02) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x01) > 0) ? fgColor : bgColor;
			}
		}

		private void RenderBackgroundM1(int scanLine)
		{
			if (!DisplayOn)
			{
				Array.Clear(FrameBuffer, scanLine * 256, 256);
				return;
			}

			int yc = scanLine / 8;
			int yofs = scanLine % 8;
			int FrameBufferOffset = scanLine * 256;
			int PatternNameOffset = TmsPatternNameTableBase + (yc * 40);
			int ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (int xc = 0; xc < 40; xc++)
			{
				int pn = VRAM[PatternNameOffset++];
				int pv = VRAM[PatternGeneratorBase + (pn * 8) + yofs];
				int colorEntry = Registers[7];
				int fgIndex = (colorEntry >> 4) & 0x0F;
				int bgIndex = colorEntry & 0x0F;
				int fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				int bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

				FrameBuffer[FrameBufferOffset++] = ((pv & 0x80) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x40) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x20) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x10) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x08) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x04) > 0) ? fgColor : bgColor;
			}
		}

		private void RenderBackgroundM2(int scanLine)
		{
			if (!DisplayOn)
			{
				Array.Clear(FrameBuffer, scanLine * 256, 256);
				return;
			}

			int yrow = scanLine / 8;
			int yofs = scanLine % 8;
			int FrameBufferOffset = scanLine * 256;
			int PatternNameOffset = TmsPatternNameTableBase + (yrow * 32);
			int PatternGeneratorOffset = (((Registers[4] & 4) << 11) & 0x2000);
			int ColorOffset = (ColorTableBase & 0x2000);
			int ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (int xc = 0; xc < 32; xc++)
			{
				int pn = VRAM[PatternNameOffset++] + ((yrow / 8) * 0x100);
				int pv = VRAM[PatternGeneratorOffset + (pn * 8) + yofs];
				int colorEntry = VRAM[ColorOffset + (pn * 8) + yofs];
				int fgIndex = (colorEntry >> 4) & 0x0F;
				int bgIndex = colorEntry & 0x0F;
				int fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				int bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

				FrameBuffer[FrameBufferOffset++] = ((pv & 0x80) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x40) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x20) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x10) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x08) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x04) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x02) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x01) > 0) ? fgColor : bgColor;
			}
		}

		private void RenderBackgroundM3(int scanLine)
		{
			if (!DisplayOn)
			{
				Array.Clear(FrameBuffer, scanLine * 256, 256);
				return;
			}

			int yc = scanLine / 8;
			bool top = (scanLine & 4) == 0; // am I in the top 4 pixels of an 8-pixel character?
			int FrameBufferOffset = scanLine * 256;
			int PatternNameOffset = TmsPatternNameTableBase + (yc * 32);
			int ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (int xc = 0; xc < 32; xc++)
			{
				int pn = VRAM[PatternNameOffset++];
				int pv = VRAM[PatternGeneratorBase + (pn * 8) + ((yc & 3) * 2) + (top ? 0 : 1)];

				int lColorIndex = pv & 0xF;
				int rColorIndex = pv >> 4;
				int lColor = lColorIndex == 0 ? ScreenBGColor : PaletteTMS9918[lColorIndex];
				int rColor = rColorIndex == 0 ? ScreenBGColor : PaletteTMS9918[rColorIndex];

				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = rColor;
				FrameBuffer[FrameBufferOffset++] = rColor;
				FrameBuffer[FrameBufferOffset++] = rColor;
				FrameBuffer[FrameBufferOffset  ] = rColor;
			}
		}

		private readonly byte[] ScanlinePriorityBuffer = new byte[256];
		private readonly byte[] SpriteCollisionBuffer = new byte[256];

		private void RenderTmsSprites(int scanLine)
		{
			if (EnableDoubledSprites)
				RenderTmsSpritesDouble(scanLine);
			else
				RenderTmsSpritesStandard(scanLine);
		}

		private void RenderTmsSpritesStandard(int scanLine)
		{
			if (!DisplayOn) return;

			Array.Clear(ScanlinePriorityBuffer, 0, 256);
			Array.Clear(SpriteCollisionBuffer, 0, 256);

			bool LargeSprites = EnableLargeSprites;

			int SpriteSize = 8;
			if (LargeSprites) SpriteSize *= 2;
			const int OneCellSize = 8;

			int NumSpritesOnScanline = 0;
			for (int i = 0; i < 32; i++)
			{
				int SpriteBase = TmsSpriteAttributeBase + (i * 4);
				int y = VRAM[SpriteBase++];
				int x = VRAM[SpriteBase++];
				int Pattern = VRAM[SpriteBase++];
				int Color = VRAM[SpriteBase];

				if (y == 208) break; // terminator sprite
				if (y > 224) y -= 256; // sprite Y wrap
				y++; // inexplicably, sprites start on Y+1
				if (y > scanLine || y + SpriteSize <= scanLine) continue; // sprite is not on this scanline
				if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

				if (++NumSpritesOnScanline == 5)
				{
					StatusByte &= 0xE0;    // Clear FS0-FS4 bits
					StatusByte |= (byte)i; // set 5th sprite index
					StatusByte |= 0x40;    // set overflow bit
					break;
				}

				if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-byte alignment
				int SpriteLine = scanLine - y;

				// pv contains the VRAM byte holding the pattern data for this character at this scanline.
				// each byte contains the pattern data for each the 8 pixels on this line.
				// the bit-shift further down on PV pulls out the relevant horizontal pixel.

				byte pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine];

				for (int xp = 0; xp < SpriteSize && x + xp < 256; xp++)
				{
					if (x + xp < 0) continue;
					if (LargeSprites && xp == OneCellSize)
						pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine + 16];

					if (Color != 0 && (pv & (1 << (7 - (xp & 7)))) > 0)
					{
						if (SpriteCollisionBuffer[x + xp] != 0)
							StatusByte |= 0x20; // Set sprite collision flag

						if (ScanlinePriorityBuffer[x + xp] == 0)
						{
							ScanlinePriorityBuffer[x + xp] = 1;
							SpriteCollisionBuffer[x + xp] = 1;
							FrameBuffer[(scanLine * 256) + x + xp] = PaletteTMS9918[Color & 0x0F];
						}
					}
				}
			}
		}

		private void RenderTmsSpritesDouble(int scanLine)
		{
			if (!DisplayOn) return;

			Array.Clear(ScanlinePriorityBuffer, 0, 256);
			Array.Clear(SpriteCollisionBuffer, 0, 256);

			bool LargeSprites = EnableLargeSprites;

			int SpriteSize = 8;
			if (LargeSprites) SpriteSize *= 2;
			SpriteSize *= 2;  // because sprite magnification
			const int OneCellSize = 16; // once 8-pixel cell, doubled, will take 16 pixels

			int NumSpritesOnScanline = 0;
			for (int i = 0; i < 32; i++)
			{
				int SpriteBase = TmsSpriteAttributeBase + (i * 4);
				int y = VRAM[SpriteBase++];
				int x = VRAM[SpriteBase++];
				int Pattern = VRAM[SpriteBase++];
				int Color = VRAM[SpriteBase];

				if (y == 208) break; // terminator sprite
				if (y > 224) y -= 256; // sprite Y wrap
				y++; // inexplicably, sprites start on Y+1
				if (y > scanLine || y + SpriteSize <= scanLine) continue; // sprite is not on this scanline
				if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

				if (++NumSpritesOnScanline == 5)
				{
					StatusByte &= 0xE0;    // Clear FS0-FS4 bits
					StatusByte |= (byte)i; // set 5th sprite index
					StatusByte |= 0x40;    // set overflow bit
					break;
				}

				if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-byte alignment
				int SpriteLine = scanLine - y;
				SpriteLine /= 2; // because of sprite magnification

				byte pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine];

				for (int xp = 0; xp < SpriteSize && x + xp < 256; xp++)
				{
					if (x + xp < 0) continue;
					if (LargeSprites && xp == OneCellSize)
						pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine + 16];

					if (Color != 0 && (pv & (1 << (7 - ((xp / 2) & 7)))) > 0)  // xp/2 is due to sprite magnification
					{
						if (SpriteCollisionBuffer[x + xp] != 0)
							StatusByte |= 0x20; // Set sprite collision flag

						if (ScanlinePriorityBuffer[x + xp] == 0)
						{
							ScanlinePriorityBuffer[x + xp] = 1;
							SpriteCollisionBuffer[x + xp] = 1;
							FrameBuffer[(scanLine * 256) + x + xp] = PaletteTMS9918[Color & 0x0F];
						}
					}
				}
			}
		}

		private readonly Z80A<ColecoVision.CpuLink> Cpu;

		public TMS9918A(Z80A<ColecoVision.CpuLink> cpu)
		{
			Cpu = cpu;
		}

		public readonly int[] FrameBuffer = new int[256 * 192];
		public int[] GetVideoBuffer() => FrameBuffer;

		public int VirtualWidth => 293;
		public int VirtualHeight => 192;
		public int BufferWidth => 256;
		public int BufferHeight => 192;
		public int BackgroundColor => 0;

		public int VsyncNumerator
			=> NullVideo.DefaultVsyncNum; //TODO precise numbers or confirm the default is okay

		public int VsyncDenominator
			=> NullVideo.DefaultVsyncDen; //TODO precise numbers or confirm the default is okay

		private readonly int[] PaletteTMS9918 =
		{
			unchecked((int)0xFF000000),
			unchecked((int)0xFF000000),
			unchecked((int)0xFF47B73B),
			unchecked((int)0xFF7CCF6F),
			unchecked((int)0xFF5D4EFF),
			unchecked((int)0xFF8072FF),
			unchecked((int)0xFFB66247),
			unchecked((int)0xFF5DC8ED),
			unchecked((int)0xFFD76B48),
			unchecked((int)0xFFFB8F6C),
			unchecked((int)0xFFC3CD41),
			unchecked((int)0xFFD3DA76),
			unchecked((int)0xFF3E9F2F),
			unchecked((int)0xFFB664C7),
			unchecked((int)0xFFCCCCCC),
			unchecked((int)0xFFFFFFFF)
		};

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("VDP");
			ser.Sync(nameof(StatusByte), ref StatusByte);
			ser.Sync("WaitingForLatchByte", ref VdpWaitingForLatchByte);
			ser.Sync("Latch", ref VdpLatch);
			ser.Sync("ReadBuffer", ref VdpBuffer);
			ser.Sync(nameof(VdpAddress), ref VdpAddress);
			ser.Sync(nameof(Registers), ref Registers, false);
			ser.Sync(nameof(VRAM), ref VRAM, false);
			ser.EndSection();

			if (ser.IsReader)
			{
				for (int i = 0; i < Registers.Length; i++)
				{
					WriteRegister(i, Registers[i]);
				}
			}
		}
	}
}
