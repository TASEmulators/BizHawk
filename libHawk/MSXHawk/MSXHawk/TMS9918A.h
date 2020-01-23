#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace MSXHawk
{
	class TMS9918A
	{
	public:
		#pragma region VDP

		TMS9918A()
		{

		}

		// external pointers to CPU
		bool* INT_FLAG = nullptr;
		// external flags to display background or sprites
		bool SHOW_BG, SHOW_SPRITES;
		bool SpriteLimit;

		// VDP State
		bool VdpWaitingForLatchInt = true;
		bool VdpWaitingForLatchByte = true;
		bool VIntPending;
		bool HIntPending;
			
		uint8_t StatusByte;		
		uint8_t VdpLatch;	
		uint8_t VdpBuffer;
		uint8_t Registers[8] = {};
		uint8_t VRAM[0x4000]; //16kb video RAM

		int32_t ScanLine;
		uint32_t IPeriod = 228;
		uint32_t VdpAddress;
		uint32_t TmsMode;
		uint32_t ColorTableBase;
		uint32_t PatternGeneratorBase;
		uint32_t SpritePatternGeneratorBase;
		uint32_t TmsPatternNameTableBase;
		uint32_t TmsSpriteAttributeBase;

		uint32_t FrameBuffer[192 * 256] = {};

		uint32_t BackgroundColor = 0;

		uint32_t PaletteTMS9918[16] =
		{
			0xFF000000,
			0xFF000000,
			0xFF47B73B,
			0xFF7CCF6F,
			0xFF5D4EFF,
			0xFF8072FF,
			0xFFB66247,
			0xFF5DC8ED,
			0xFFD76B48,
			0xFFFB8F6C,
			0xFFC3CD41,
			0xFFD3DA76,
			0xFF3E9F2F,
			0xFFB664C7,
			0xFFCCCCCC,
			0xFFFFFFFF
		};

		bool Mode1Bit() { return (Registers[1] & 16) > 0; }
		bool Mode2Bit() { return (Registers[0] & 2) > 0; }
		bool Mode3Bit() { return (Registers[1] & 8) > 0; }
		bool EnableDoubledSprites() { return (Registers[1] & 1) > 0; }
		bool EnableLargeSprites() { return (Registers[1] & 2) > 0; }
		bool EnableInterrupts() { return (Registers[1] & 32) > 0; }
		bool DisplayOn() { return (Registers[1] & 64) > 0; }
		bool Mode16k() { return (Registers[1] & 128) > 0; }

		bool InterruptPendingGet() { return (StatusByte & 0x80) != 0; }
		void InterruptPendingSet(bool value) { StatusByte = (uint8_t)((StatusByte & ~0x02) | (value ? 0x80 : 0x00)); }

		void WriteVdpControl(uint8_t value)
		{
			if (VdpWaitingForLatchByte)
			{
				VdpLatch = value;
				VdpWaitingForLatchByte = false;
				VdpAddress = (uint32_t)((VdpAddress & 0x3F00) | value);
				return;
			}

			VdpWaitingForLatchByte = true;
			VdpAddress = (uint32_t)(((value & 63) << 8) | VdpLatch);
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
				uint32_t reg = value & 0x0F;
				WriteRegister(reg, VdpLatch);
				break;
			}
		}

		void WriteVdpData(uint8_t value)
		{
			VdpWaitingForLatchByte = true;
			VdpBuffer = value;

			VRAM[VdpAddress] = value;
			//if (!Mode16k)
			//    Console.WriteLine("VRAM written while not in 16k addressing mode!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			VdpAddress++;
			VdpAddress &= 0x3FFF;
		}

		void WriteRegister(uint32_t reg, uint8_t data)
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
				INT_FLAG[0] = (EnableInterrupts() && InterruptPendingGet());
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
			case 6: // Sprite Pattern Generator Base Adderss 
				SpritePatternGeneratorBase = (Registers[6] << 11) & 0x3800;
				break;
			}
		}

		uint8_t ReadVdpStatus()
		{
			VdpWaitingForLatchByte = true;
			uint8_t returnValue = StatusByte;
			StatusByte &= 0x1F;
			INT_FLAG[0] = false;

			return returnValue;
		}

		uint8_t ReadData()
		{
			VdpWaitingForLatchByte = true;
			uint8_t value = VdpBuffer;
			VdpBuffer = VRAM[VdpAddress];
			VdpAddress++;
			VdpAddress &= 0x3FFF;
			return value;
		}

		void CheckVideoMode()
		{
			if (Mode1Bit()) TmsMode = 1;
			else if (Mode2Bit()) TmsMode = 2;
			else if (Mode3Bit()) TmsMode = 3;
			else TmsMode = 0;
		}

		void RenderScanline(int32_t scanLine)
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

		void RenderBackgroundM0(uint32_t scanLine)
		{
			if (DisplayOn() == false)
			{
				for (int i = 0; i < 256; i++) { FrameBuffer[scanLine * 256 + i] = 0; };
				return;
			}

			uint32_t yc = scanLine / 8;
			uint32_t yofs = scanLine % 8;
			uint32_t FrameBufferOffset = scanLine * 256;
			uint32_t PatternNameOffset = TmsPatternNameTableBase + (yc * 32);
			uint32_t ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (uint32_t xc = 0; xc < 32; xc++)
			{
				uint32_t pn = VRAM[PatternNameOffset++];
				uint32_t pv = VRAM[PatternGeneratorBase + (pn * 8) + yofs];
				uint32_t colorEntry = VRAM[ColorTableBase + (pn / 8)];
				uint32_t fgIndex = (colorEntry >> 4) & 0x0F;
				uint32_t bgIndex = colorEntry & 0x0F;
				uint32_t fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				uint32_t bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

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

		void RenderBackgroundM1(uint32_t scanLine)
		{
			if (DisplayOn() == false)
			{
				for (int i = 0; i < 256; i++) { FrameBuffer[scanLine * 256 + i] = 0; };
				return;
			}

			uint32_t yc = scanLine / 8;
			uint32_t yofs = scanLine % 8;
			uint32_t FrameBufferOffset = scanLine * 256;
			uint32_t PatternNameOffset = TmsPatternNameTableBase + (yc * 40);
			uint32_t ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (uint32_t xc = 0; xc < 40; xc++)
			{
				uint32_t pn = VRAM[PatternNameOffset++];
				uint32_t pv = VRAM[PatternGeneratorBase + (pn * 8) + yofs];
				uint32_t colorEntry = Registers[7];
				uint32_t fgIndex = (colorEntry >> 4) & 0x0F;
				uint32_t bgIndex = colorEntry & 0x0F;
				uint32_t fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				uint32_t bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

				FrameBuffer[FrameBufferOffset++] = ((pv & 0x80) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x40) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x20) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x10) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x08) > 0) ? fgColor : bgColor;
				FrameBuffer[FrameBufferOffset++] = ((pv & 0x04) > 0) ? fgColor : bgColor;
			}
		}

		void RenderBackgroundM2(uint32_t scanLine)
		{
			if (DisplayOn() == false)
			{
				for (int i = 0; i < 256; i++) { FrameBuffer[scanLine * 256 + i] = 0; };
				return;
			}

			uint32_t yrow = scanLine / 8;
			uint32_t yofs = scanLine % 8;
			uint32_t FrameBufferOffset = scanLine * 256;
			uint32_t PatternNameOffset = TmsPatternNameTableBase + (yrow * 32);
			uint32_t PatternGeneratorOffset = (((Registers[4] & 4) << 11) & 0x2000);
			uint32_t ColorOffset = (ColorTableBase & 0x2000);
			uint32_t ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (uint32_t xc = 0; xc < 32; xc++)
			{
				uint32_t pn = VRAM[PatternNameOffset++] + ((yrow / 8) * 0x100);
				uint32_t pv = VRAM[PatternGeneratorOffset + (pn * 8) + yofs];
				uint32_t colorEntry = VRAM[ColorOffset + (pn * 8) + yofs];
				uint32_t fgIndex = (colorEntry >> 4) & 0x0F;
				uint32_t bgIndex = colorEntry & 0x0F;
				uint32_t fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				uint32_t bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

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

		void RenderBackgroundM3(uint32_t scanLine)
		{
			if (DisplayOn() == false)
			{
				for (int i = 0; i < 256; i++) { FrameBuffer[scanLine * 256 + i] = 0; };
				return;
			}

			uint32_t yc = scanLine / 8;
			bool top = (scanLine & 4) == 0; // am I in the top 4 pixels of an 8-pixel character?
			uint32_t FrameBufferOffset = scanLine * 256;
			uint32_t PatternNameOffset = TmsPatternNameTableBase + (yc * 32);
			uint32_t ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (uint32_t xc = 0; xc < 32; xc++)
			{
				uint32_t pn = VRAM[PatternNameOffset++];
				uint32_t pv = VRAM[PatternGeneratorBase + (pn * 8) + ((yc & 3) * 2) + (top ? 0 : 1)];

				uint32_t lColorIndex = pv & 0xF;
				uint32_t rColorIndex = pv >> 4;
				uint32_t lColor = lColorIndex == 0 ? ScreenBGColor : PaletteTMS9918[lColorIndex];
				uint32_t rColor = rColorIndex == 0 ? ScreenBGColor : PaletteTMS9918[rColorIndex];

				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = lColor;
				FrameBuffer[FrameBufferOffset++] = rColor;
				FrameBuffer[FrameBufferOffset++] = rColor;
				FrameBuffer[FrameBufferOffset++] = rColor;
				FrameBuffer[FrameBufferOffset] = rColor;
			}
		}

		uint8_t ScanlinePriorityBuffer[256] = {};
		uint8_t SpriteCollisionBuffer[256] = {};

		void RenderTmsSprites(int32_t scanLine)
		{
			if (EnableDoubledSprites() == false)
			{
				RenderTmsSpritesStandard(scanLine);
			}
			else
			{
				RenderTmsSpritesDouble(scanLine);
			}
		}

		void RenderTmsSpritesStandard(int32_t scanLine)
		{
			if (DisplayOn() == false) return;

			for (uint32_t i = 0; i < 256; i++) 
			{ 
				ScanlinePriorityBuffer[i] = 0; 
				SpriteCollisionBuffer[i] = 0;
			};

			bool LargeSprites = EnableLargeSprites();

			int32_t SpriteSize = 8;
			if (LargeSprites) SpriteSize *= 2;
			const int32_t OneCellSize = 8;

			int32_t NumSpritesOnScanline = 0;
			for (int32_t i = 0; i < 32; i++)
			{
				int32_t SpriteBase = TmsSpriteAttributeBase + (i * 4);
				int32_t y = VRAM[SpriteBase++];
				int32_t x = VRAM[SpriteBase++];
				int32_t Pattern = VRAM[SpriteBase++];
				int32_t Color = VRAM[SpriteBase];

				if (y == 208) break; // terminator sprite
				if (y > 224) y -= 256; // sprite Y wrap
				y++; // inexplicably, sprites start on Y+1
				if (y > scanLine || y + SpriteSize <= scanLine) continue; // sprite is not on this scanline
				if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

				if (++NumSpritesOnScanline == 5)
				{
					StatusByte &= 0xE0;    // Clear FS0-FS4 bits
					StatusByte |= (uint8_t)i; // set 5th sprite index
					StatusByte |= 0x40;    // set overflow bit
					break;
				}

				if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-uint8_t alignment
				int32_t SpriteLine = scanLine - y;

				// pv contains the VRAM uint8_t holding the pattern data for this character at this scanline.
				// each uint8_t contains the pattern data for each the 8 pixels on this line.
				// the bit-shift further down on PV pulls out the relevant horizontal pixel.

				int8_t pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine];

				for (int32_t xp = 0; xp < SpriteSize && x + xp < 256; xp++)
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

		void RenderTmsSpritesDouble(int32_t scanLine)
		{
			if (DisplayOn() == false) return;

			for (uint32_t i = 0; i < 256; i++)
			{
				ScanlinePriorityBuffer[i] = 0;
				SpriteCollisionBuffer[i] = 0;
			};

			bool LargeSprites = EnableLargeSprites();

			int32_t SpriteSize = 8;
			if (LargeSprites) SpriteSize *= 2;
			SpriteSize *= 2;  // because sprite magnification
			const int32_t OneCellSize = 16; // once 8-pixel cell, doubled, will take 16 pixels

			int32_t NumSpritesOnScanline = 0;
			for (int32_t i = 0; i < 32; i++)
			{
				int32_t SpriteBase = TmsSpriteAttributeBase + (i * 4);
				int32_t y = VRAM[SpriteBase++];
				int32_t x = VRAM[SpriteBase++];
				int32_t Pattern = VRAM[SpriteBase++];
				int32_t Color = VRAM[SpriteBase];

				if (y == 208) break; // terminator sprite
				if (y > 224) y -= 256; // sprite Y wrap
				y++; // inexplicably, sprites start on Y+1
				if (y > scanLine || y + SpriteSize <= scanLine) continue; // sprite is not on this scanline
				if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

				if (++NumSpritesOnScanline == 5)
				{
					StatusByte &= 0xE0;    // Clear FS0-FS4 bits
					StatusByte |= (uint8_t)i; // set 5th sprite index
					StatusByte |= 0x40;    // set overflow bit
					break;
				}

				if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-byte alignment
				int32_t SpriteLine = scanLine - y;
				SpriteLine /= 2; // because of sprite magnification

				int8_t pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine];

				for (int32_t xp = 0; xp < SpriteSize && x + xp < 256; xp++)
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

		#pragma endregion

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{

			return loader;
		}

		#pragma endregion
	};
}
