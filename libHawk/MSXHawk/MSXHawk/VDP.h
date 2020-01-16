#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace MSXHawk
{
	class VDP
	{
	public:
		#pragma region VDP

		// external pointers to CPU
		bool* INT_FLAG = nullptr;
		// external flags to display background or sprites
		bool SHOW_BG, SHOW_SPRITES;


		// VDP State
		uint8_t VRAM[0x4000]; //16kb video RAM
		uint8_t CRAM[64]; // SMS = 32 uint8_ts, GG = 64 uint8_ts CRAM
		uint8_t Registers[16] = { 0x06, 0x80, 0xFF, 0xFF, 0xFF, 0xFF, 0xFB, 0xF0, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00 };
		uint8_t Statusuint8_t;

		static void TO_REGS(uint8_t value) 
		{

		}

		const uint32_t Command_VramRead = 0x00;
		const uint32_t Command_VramWrite = 0x40;
		const uint32_t Command_RegisterWrite = 0x80;
		const uint32_t Command_CramWrite = 0xC0;

		const uint32_t MODE_SMS = 1;
		const uint32_t MODE_GG = 2;

		const uint32_t DISP_TYPE_NTSC = 1;
		const uint32_t DISP_TYPE_PAL = 2;

		bool VdpWaitingForLatchuint8_t = true;
		uint8_t VdpLatch;
		uint8_t VdpBuffer;
		uint16_t VdpAddress;
		uint8_t VdpCommand;
		uint8_t HCounter = 0x90;
		uint32_t TmsMode = 4;

		bool VIntPending;
		bool HIntPending;
		uint32_t lineIntLinesRemaining;

		uint32_t mode;
		uint32_t DisplayType;

		bool SpriteLimit;
		
		int32_t IPeriod = 228;
		int32_t FrameHeight = 192;
		uint32_t FrameBuffer[256 * 244];
		uint32_t GameGearFrameBuffer[160 * 144];
		int32_t OverscanFrameBuffer[1];
		int32_t ScanLine;

		inline bool Mode1Bit() { return (Registers[1] & 16) > 0; }
		inline bool Mode2Bit() {return (Registers[0] & 2) > 0; }
		inline bool Mode3Bit() {return (Registers[1] & 8) > 0; }
		inline bool Mode4Bit() {return (Registers[0] & 4) > 0; }
		inline bool ShiftSpritesLeft8Pixels() { return (Registers[0] & 8) > 0; }
		inline bool EnableLineInterrupts() { return (Registers[0] & 16) > 0; }
		inline bool LeftBlanking() { return (Registers[0] & 32) > 0; }
		inline bool HorizScrollLock() { return (Registers[0] & 64) > 0; }
		inline bool VerticalScrollLock() { return (Registers[0] & 128) > 0; }
		inline bool EnableDoubledSprites() { return (Registers[1] & 1) > 0; }
		inline bool EnableLargeSprites() { return (Registers[1] & 2) > 0; }
		inline bool EnableFrameInterrupts() { return (Registers[1] & 32) > 0; }
		inline bool DisplayOn() { return (Registers[1] & 64) > 0; }
		uint32_t SpriteAttributeTableBase() { return ((Registers[5] >> 1) << 8) & 0x3FFF; }
		uint32_t SpriteTileBase() { return (Registers[6] & 4) > 0 ? 256 : 0; }
		uint8_t BackdropColor() { return (uint8_t)(16 + (Registers[7] & 15)); }

		uint32_t NameTableBase;
		uint32_t ColorTableBase;
		uint32_t PatternGeneratorBase;
		uint32_t SpritePatternGeneratorBase;
		uint32_t TmsPatternNameTableBase;
		uint32_t TmsSpriteAttributeBase;

		// preprocessed state assist stuff.
		uint32_t Palette[32];
		uint8_t PatternBuffer[0x8000];

		uint8_t ScanlinePriorityBuffer[256];
		uint8_t SpriteCollisionBuffer[256];

		const uint8_t SMSPalXlatTable[4] = { 0, 85, 170, 255 };
		const uint8_t GGPalXlatTable[16] = { 0, 17, 34, 51, 68, 85, 102, 119, 136, 153, 170, 187, 204, 221, 238, 255 };

		VDP()
		{
			mode = MODE_GG;

			DisplayType = DISP_TYPE_NTSC;
			NameTableBase = CalcNameTableBase();
		}

		uint8_t ReadData()
		{
			VdpWaitingForLatchuint8_t = true;
			uint8_t value = VdpBuffer;
			VdpBuffer = VRAM[VdpAddress & 0x3FFF];
			VdpAddress++;
			return value;
		}

		uint8_t ReadVdpStatus()
		{
			VdpWaitingForLatchuint8_t = true;
			uint8_t returnValue = Statusuint8_t;
			Statusuint8_t &= 0x1F;
			HIntPending = false;
			VIntPending = false;
			INT_FLAG[0] = false;
			return returnValue;
		}

		uint8_t ReadVLineCounter()
		{
			if (DisplayType == DISP_TYPE_NTSC)
			{
				if (FrameHeight == 192)
					return VLineCounterTableNTSC192[ScanLine];
				if (FrameHeight == 224)
					return VLineCounterTableNTSC224[ScanLine];
				return VLineCounterTableNTSC240[ScanLine];
			}
			else
			{ // PAL
				if (FrameHeight == 192)
					return VLineCounterTablePAL192[ScanLine];
				if (FrameHeight == 224)
					return VLineCounterTablePAL224[ScanLine];
				return VLineCounterTablePAL240[ScanLine];
			}
		}

		uint8_t ReadHLineCounter()
		{
			return HCounter;
		}

		void WriteVdpControl(uint8_t value)
		{
			if (VdpWaitingForLatchuint8_t)
			{
				VdpLatch = value;
				VdpWaitingForLatchuint8_t = false;
				VdpAddress = (uint16_t)((VdpAddress & 0xFF00) | value);
				return;
			}

			VdpWaitingForLatchuint8_t = true;
			VdpAddress = (uint16_t)(((value & 63) << 8) | VdpLatch);
			switch (value & 0xC0)
			{
			case 0x00: // read VRAM
				VdpCommand = Command_VramRead;
				VdpBuffer = VRAM[VdpAddress & 0x3FFF];
				VdpAddress++;
				break;
			case 0x40: // write VRAM
				VdpCommand = Command_VramWrite;
				break;
			case 0x80: // VDP register write
				VdpCommand = Command_RegisterWrite;
				WriteRegister(value & 0x0F, VdpLatch);
				break;
			case 0xC0: // write CRAM / modify palette
				VdpCommand = Command_CramWrite;
				break;
			}
		}

		void WriteVdpData(uint8_t value)
		{
			VdpWaitingForLatchuint8_t = true;
			VdpBuffer = value;
			if (VdpCommand == Command_CramWrite)
			{
				// Write Palette / CRAM
				uint32_t mask = mode == MODE_SMS ? 0x1F : 0x3F;
				CRAM[VdpAddress & mask] = value;
				UpdatePrecomputedPalette();
			}
			else
			{
				// Write VRAM and update pre-computed pattern buffer. 
				UpdatePatternBuffer((uint16_t)(VdpAddress & 0x3FFF), value);
				VRAM[VdpAddress & 0x3FFF] = value;
			}
			VdpAddress++;
		}

		void UpdatePrecomputedPalette()
		{
			if (mode == MODE_SMS)
			{
				for (uint32_t i = 0; i < 32; i++)
				{
					uint8_t value = CRAM[i];
					uint8_t r = SMSPalXlatTable[(value & 0x03)];
					uint8_t g = SMSPalXlatTable[(value & 0x0C) >> 2];
					uint8_t b = SMSPalXlatTable[(value & 0x30) >> 4];
					Palette[i] = ARGB(r, g, b);
				}
			}
			else
			{ // GameGear
				for (uint32_t i = 0; i < 32; i++)
				{
					uint16_t value = (uint16_t)((CRAM[(i * 2) + 1] << 8) | CRAM[(i * 2) + 0]);
					uint8_t r = GGPalXlatTable[(value & 0x000F)];
					uint8_t g = GGPalXlatTable[(value & 0x00F0) >> 4];
					uint8_t b = GGPalXlatTable[(value & 0x0F00) >> 8];
					Palette[i] = ARGB(r, g, b);
				}
			}
		}

		uint32_t ARGB(uint8_t red, uint8_t green, uint8_t blue)
		{
			return (uint32_t)((red << 0x10) | (green << 8) | blue | (0xFF << 0x18));
		}

		uint32_t CalcNameTableBase()
		{
			if (FrameHeight == 192)
				return 1024 * (Registers[2] & 0x0E);
			return (1024 * (Registers[2] & 0x0C)) + 0x0700;
		}

		void CheckVideoMode()
		{
			if (Mode4Bit() == false) // check old TMS modes
			{
				if (Mode1Bit()) TmsMode = 1;
				else if (Mode2Bit()) TmsMode = 2;
				else if (Mode3Bit()) TmsMode = 3;
				else TmsMode = 0;
			}

			else if (Mode4Bit() && Mode2Bit()) // if Mode4 and Mode2 set, then check extension modes
			{
				TmsMode = 4;
				switch (Registers[1] & 0x18)
				{
				case 0x00:
				case 0x18: // 192-line mode
					if (FrameHeight != 192)
					{
						FrameHeight = 192;
						NameTableBase = CalcNameTableBase();
					}
					break;
				case 0x10: // 224-line mode
					if (FrameHeight != 224)
					{
						FrameHeight = 224;
						NameTableBase = CalcNameTableBase();
					}
					break;
				case 0x08: // 240-line mode
					if (FrameHeight != 240)
					{
						FrameHeight = 240;
						NameTableBase = CalcNameTableBase();
					}
					break;
				}
			}

			else
			{ // default to standard 192-line mode4
				TmsMode = 4;
				if (FrameHeight != 192)
				{
					FrameHeight = 192;
					NameTableBase = CalcNameTableBase();
				}
			}
		}

		void WriteRegister(uint32_t reg, uint8_t data)
		{
			Registers[reg] = data;

			switch (reg)
			{
			case 0: // Mode Control Register 1
				CheckVideoMode();
				INT_FLAG[0] = (EnableLineInterrupts() && HIntPending);
				INT_FLAG[0] |= (EnableFrameInterrupts() && VIntPending);
				break;
			case 1: // Mode Control Register 2
				CheckVideoMode();
				INT_FLAG[0] = (EnableFrameInterrupts() && VIntPending);
				INT_FLAG[0] |= (EnableLineInterrupts() && HIntPending);
				break;
			case 2: // Name Table Base Address
				NameTableBase = CalcNameTableBase();
				TmsPatternNameTableBase = (Registers[2] << 10) & 0x3C00;
				break;
			case 3: // Color Table Base Address
				ColorTableBase = (Registers[3] << 6) & 0x3FC0;
				break;
			case 4: // Pattern Generator Base Address
				PatternGeneratorBase = (Registers[4] << 11) & 0x3800;
				break;
			case 5: // Sprite Attribute Table Base Address
				// ??? should I move from my property to precalculated?
				TmsSpriteAttributeBase = (Registers[5] << 7) & 0x3F80;
				break;
			case 6: // Sprite Pattern Generator Base Adderss 
				SpritePatternGeneratorBase = (Registers[6] << 11) & 0x3800;
				break;
			}

		}

		const uint8_t pow2[8] = { 1, 2, 4, 8, 16, 32, 64, 128 };

		void UpdatePatternBuffer(uint16_t address, uint8_t value)
		{
			// writing one uint8_t affects 8 pixels due to stupid planar storage.
			for (uint32_t i = 0; i < 8; i++)
			{
				uint8_t colorBit = pow2[address % 4];
				uint8_t sourceBit = pow2[7 - i];
				uint16_t dest = (uint16_t)(((address & 0xFFFC) * 2) + i);
				if ((value & sourceBit) > 0) // setting bit
					PatternBuffer[dest] |= colorBit;
				else // clearing bit
					PatternBuffer[dest] &= (uint8_t)~colorBit;
			}
		}

		void ProcessFrameInterrupt()
		{
			if (ScanLine == FrameHeight + 1)
			{
				Statusuint8_t |= 0x80;
				VIntPending = true;
			}

			if (VIntPending && EnableFrameInterrupts())
			{
				INT_FLAG[0] = true;
			}

		}

		void ProcessLineInterrupt()
		{
			if (ScanLine <= FrameHeight)
			{
				if (lineIntLinesRemaining-- <= 0)
				{
					HIntPending = true;
					if (EnableLineInterrupts())
					{
						INT_FLAG[0] = true;
					}
					lineIntLinesRemaining = Registers[0x0A];
				}
				return;
			}
			// else we're outside the active display period
			lineIntLinesRemaining = Registers[0x0A];
		}

		void RenderCurrentScanline(bool render)
		{
			// only mode 4 supports frameskip. deal with it
			if (TmsMode == 4)
			{
				if (render)
					RenderBackgroundCurrentLine(SHOW_BG);

				if (EnableDoubledSprites())
					RenderSpritesCurrentLineDoubleSize(SHOW_SPRITES & render);
				else
					RenderSpritesCurrentLine(SHOW_SPRITES & render);

				RenderLineBlanking(render);
			}
			else if (TmsMode == 2)
			{
				RenderBackgroundM2(SHOW_BG);
				RenderTmsSprites(SHOW_SPRITES);
			}
			else if (TmsMode == 0)
			{
				RenderBackgroundM0(SHOW_BG);
				RenderTmsSprites(SHOW_SPRITES);
			}
		}
		/*
		void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(VDP));
			ser.Sync(nameof(Statusuint8_t), ref Statusuint8_t);
			ser.Sync("WaitingForLatchuint8_t", ref VdpWaitingForLatchuint8_t);
			ser.Sync("Latch", ref VdpLatch);
			ser.Sync("ReadBuffer", ref VdpBuffer);
			ser.Sync(nameof(VdpAddress), ref VdpAddress);
			ser.Sync("Command", ref VdpCommand);
			ser.Sync(nameof(HIntPending), ref HIntPending);
			ser.Sync(nameof(VIntPending), ref VIntPending);
			ser.Sync("LineIntLinesRemaining", ref lineIntLinesRemaining);
			ser.Sync(nameof(Registers), ref Registers, false);
			ser.Sync(nameof(CRAM), ref CRAM, false);
			ser.Sync(nameof(VRAM), ref VRAM, false);
			ser.Sync(nameof(HCounter), ref HCounter);
			ser.EndSection();

			if (ser.IsReader)
			{
				for (uint32_t i = 0; i < Registers.Length; i++)
					WriteRegister(i, Registers[i]);
				for (uint16_t i = 0; i < VRAM.Length; i++)
					UpdatePatternBuffer(i, VRAM[i]);
				UpdatePrecomputedPalette();
			}
		}
		*/

		uint32_t VirtualWidth = 160;

		uint32_t VirtualHeight = 160; // GameGear

		uint32_t BufferHeight = 144; // GameGear

		uint32_t BackgroundColor() { return Palette[BackdropColor()]; }

		uint32_t VsyncNumerator = 60;

		uint32_t VsyncDenominator = 1;
		#pragma endregion

		#pragma region Mode4
		
		void RenderBackgroundCurrentLine(bool show)
		{
			if (ScanLine >= FrameHeight)
				return;

			if (DisplayOn() == false)
			{
				for (uint32_t x = 0; x < 256; x++)
					FrameBuffer[(ScanLine * 256) + x] = Palette[BackdropColor()];
				return;
			}

			// Clear the priority buffer for this scanline
			for (uint32_t i = 0; i < 256; i++) 
			{
				ScanlinePriorityBuffer[i] = 0;
			}

			uint32_t mapBase = NameTableBase;

			uint32_t vertOffset = ScanLine + Registers[9];
			if (FrameHeight == 192)
			{
				if (vertOffset >= 224)
					vertOffset -= 224;
			}
			else
			{
				if (vertOffset >= 256)
					vertOffset -= 256;
			}
			uint8_t horzOffset = (HorizScrollLock() && ScanLine < 16) ? (uint8_t)0 : Registers[8];

			uint32_t yTile = vertOffset / 8;

			for (uint32_t xTile = 0; xTile < 32; xTile++)
			{
				if (xTile == 24 && VerticalScrollLock())
				{
					vertOffset = ScanLine;
					yTile = vertOffset / 8;
				}

				uint8_t PaletteBase = 0;
				uint32_t tileInfo = VRAM[mapBase + ((yTile * 32) + xTile) * 2] | (VRAM[mapBase + (((yTile * 32) + xTile) * 2) + 1] << 8);
				uint32_t tileNo = tileInfo & 0x01FF;
				if ((tileInfo & 0x800) != 0)
					PaletteBase = 16;
				bool Priority = (tileInfo & 0x1000) != 0;
				bool VFlip = (tileInfo & 0x400) != 0;
				bool HFlip = (tileInfo & 0x200) != 0;

				uint32_t yOfs = vertOffset & 7;
				if (VFlip)
					yOfs = 7 - yOfs;

				if (HFlip == false)
				{
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase] : Palette[BackdropColor()];

					if (Priority)
					{
						horzOffset -= 8;
						for (uint32_t k = 0; k < 8; k++)
						{
							if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
								ScanlinePriorityBuffer[horzOffset] = 1;
							horzOffset++;
						}
					}
				}
				else // Flipped Horizontally
				{
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase] : Palette[BackdropColor()];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase] : Palette[BackdropColor()];

					if (Priority)
					{
						horzOffset -= 8;
						for (int32_t k = 7; k >= 0; k--)
						{
							if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
								ScanlinePriorityBuffer[horzOffset] = 1;
							horzOffset++;
						}
					}
				}
			}
		}

		void RenderSpritesCurrentLine(bool show)
		{
			bool overflowHappens = true;
			bool collisionHappens = true;
			bool renderHappens = show;

			if (!DisplayOn())
			{
				renderHappens = false;
				collisionHappens = false;
			}
			if (ScanLine >= FrameHeight)
			{
				renderHappens = false;
				overflowHappens = false;
			}

			int32_t SpriteBase = SpriteAttributeTableBase();
			int32_t SpriteHeight = EnableLargeSprites() ? 16 : 8;

			// Clear the sprite collision buffer for this scanline
			for (int32_t i = 0; i < 256; i++)
			{
				SpriteCollisionBuffer[i] = 0;
			}

			// Loop through these sprites and render the current scanline
			int32_t SpritesDrawnThisScanline = 0;
			for (int32_t i = 0; i < 64; i++)
			{
				int32_t x = VRAM[SpriteBase + 0x80 + (i * 2)];
				if (ShiftSpritesLeft8Pixels())
					x -= 8;

				int32_t y = VRAM[SpriteBase + i] + 1;
				if (y == 209 && FrameHeight == 192)
					break; // 208 is special terminator sprite (in 192-line mode)
				if (y >= (EnableLargeSprites() ? 240 : 248))
					y -= 256;

				if (y + SpriteHeight <= ScanLine || y > ScanLine)
					continue;

				if (SpritesDrawnThisScanline >= 8)
				{
					collisionHappens = false; // technically the VDP stops processing sprite past this so we would never set the collision bit for sprites past this
					if (overflowHappens)
						Statusuint8_t |= 0x40; // Set Overflow bit
					if (SpriteLimit)
						renderHappens = false; // should be able to break/return, but to ensure this has no effect on sync we keep processing and disable rendering
				}

				int32_t tileNo = VRAM[SpriteBase + 0x80 + (i * 2) + 1];
				if (EnableLargeSprites())
					tileNo &= 0xFE;
				tileNo += SpriteTileBase();

				int32_t ys = ScanLine - y;

				for (int32_t xs = 0; xs < 8 && x + xs < 256; xs++)
				{
					uint8_t color = PatternBuffer[(tileNo * 64) + (ys * 8) + xs];
					if (color != 0 && x + xs >= 0)
					{
						if (SpriteCollisionBuffer[x + xs] != 0)
						{
							if (collisionHappens)
								Statusuint8_t |= 0x20; // Set Collision bit
						}
						else if (renderHappens && ScanlinePriorityBuffer[x + xs] == 0)
						{
							FrameBuffer[(ys + y) * 256 + x + xs] = Palette[(color + 16)];
						}
						SpriteCollisionBuffer[x + xs] = 1;
					}
				}
				SpritesDrawnThisScanline++;
			}
		}

		void RenderSpritesCurrentLineDoubleSize(bool show)
		{
			bool overflowHappens = true;
			bool collisionHappens = true;
			bool renderHappens = show;

			if (!DisplayOn())
			{
				renderHappens = false;
				collisionHappens = false;
			}
			if (ScanLine >= FrameHeight)
			{
				renderHappens = false;
				overflowHappens = false;
			}

			int32_t SpriteBase = SpriteAttributeTableBase();
			int32_t SpriteHeight = EnableLargeSprites() ? 16 : 8;

			// Clear the sprite collision buffer for this scanline
			for (uint32_t i = 0; i < 256; i++)
			{
				SpriteCollisionBuffer[i] = 0;
			}

			// Loop through these sprites and render the current scanline
			int32_t SpritesDrawnThisScanline = 0;
			for (int32_t i = 0; i < 64; i++)
			{
				int32_t x = VRAM[SpriteBase + 0x80 + (i * 2)];
				if (ShiftSpritesLeft8Pixels())
					x -= 8;

				int32_t y = VRAM[SpriteBase + i] + 1;
				if (y == 209 && FrameHeight == 192)
					break; // terminator sprite
				if (y >= (EnableLargeSprites() ? 240 : 248))
					y -= 256;

				if (y + (SpriteHeight * 2) <= ScanLine || y > ScanLine)
					continue;

				if (SpritesDrawnThisScanline >= 8)
				{
					collisionHappens = false; // technically the VDP stops processing sprite past this so we would never set the collision bit for sprites past this
					if (overflowHappens)
						Statusuint8_t |= 0x40; // Set Overflow bit
					if (SpriteLimit)
						renderHappens = false; // should be able to break/return, but to ensure this has no effect on sync we keep processing and disable rendering
				}

				int32_t tileNo = VRAM[SpriteBase + 0x80 + (i * 2) + 1];
				if (EnableLargeSprites())
					tileNo &= 0xFE;
				tileNo += SpriteTileBase();

				int32_t ys = ScanLine - y;

				for (int32_t xs = 0; xs < 16 && x + xs < 256; xs++)
				{
					uint8_t color = PatternBuffer[(tileNo * 64) + ((ys / 2) * 8) + (xs / 2)];
					if (color != 0 && x + xs >= 0)
					{
						if (SpriteCollisionBuffer[x + xs] != 0)
						{
							if (collisionHappens)
								Statusuint8_t |= 0x20; // Set Collision bit
						}
						else if (renderHappens && ScanlinePriorityBuffer[x + xs] == 0)
						{
							FrameBuffer[(ys + y) * 256 + x + xs] = Palette[(color + 16)];
						}
						SpriteCollisionBuffer[x + xs] = 1;
					}
				}
				SpritesDrawnThisScanline++;
			}
		}

		// Renders left-blanking. Should be done per scanline, not per-frame.
		void RenderLineBlanking(bool render)
		{
			if (!LeftBlanking() || ScanLine >= FrameHeight || !render)
				return;

			int32_t ofs = ScanLine * 256;
			for (int32_t x = 0; x < 8; x++)
				FrameBuffer[ofs++] = Palette[BackdropColor()];
		}

		int32_t OverscanFrameWidth, OverscanFrameHeight;
		int32_t overscanTop;
		int32_t overscanBottom;
		int32_t overscanLeft;
		int32_t overscanRight;

		/*
		void ProcessOverscan()
		{
			if (Sms.Settings.DisplayOverscan == false)
				return;

			if (OverscanFrameBuffer == null)
			{
				if (Sms.Region == Common.DisplayType.NTSC)
				{
					overscanLeft = 13;
					overscanRight = 15;
					overscanTop = 27;
					overscanBottom = 24;
				}
				else // PAL
				{
					overscanLeft = 13;
					overscanRight = 15;
					overscanTop = 48;
					overscanBottom = 48;
				}

				OverscanFrameWidth = overscanLeft + 256 + overscanRight;
				OverscanFrameHeight = overscanTop + 192 + overscanBottom;
				OverscanFrameBuffer = new int[OverscanFrameHeight * OverscanFrameWidth];
			}

			// Top overscan
			for (uint32_t y = 0; y < overscanTop; y++)
				for (uint32_t x = 0; x < OverscanFrameWidth; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = BackgroundColor();

			// Bottom overscan
			for (uint32_t y = overscanTop + 192; y < OverscanFrameHeight; y++)
				for (uint32_t x = 0; x < OverscanFrameWidth; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = BackgroundColor();

			// Left overscan
			for (uint32_t y = overscanTop; y < overscanTop + 192; y++)
				for (uint32_t x = 0; x < overscanLeft; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = BackgroundColor();

			// Right overscan
			for (uint32_t y = overscanTop; y < overscanTop + 192; y++)
				for (uint32_t x = overscanLeft + 256; x < OverscanFrameWidth; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = BackgroundColor();

			// Active display area
			for (uint32_t y = 0; y < 192; y++)
				for (uint32_t x = 0; x < 256; x++)
					OverscanFrameBuffer[((y + overscanTop) * OverscanFrameWidth) + overscanLeft + x] = FrameBuffer[y * 256 + x];
		}
		*/


		// Handles GG clipping or highlighting
		void ProcessGGScreen()
		{
			uint32_t yStart = (FrameHeight - 144) / 2;
			for (uint32_t y = 0; y < 144; y++)
				for (uint32_t x = 0; x < 160; x++)
					GameGearFrameBuffer[(y * 160) + x] = FrameBuffer[((y + yStart) * 256) + x + 48];
			/*
			if (Sms.Settings.HighlightActiveDisplayRegion && Sms.Settings.ShowClippedRegions)
			{
				// Top 24 scanlines
				for (uint32_t y = 0; y < 24; y++)
				{
					for (uint32_t x = 0; x < 256; x++)
					{
						uint32_t frameOffset = (y * 256) + x;
						uint32_t p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (uint32_t)(p | 0x80000000);
					}
				}

				// Bottom 24 scanlines
				for (uint32_t y = 168; y < 192; y++)
				{
					for (uint32_t x = 0; x < 256; x++)
					{
						uint32_t frameOffset = (y * 256) + x;
						uint32_t p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (uint32_t)(p | 0x80000000);
					}
				}

				// Left 48 pixels
				for (uint32_t y = 24; y < 168; y++)
				{
					for (uint32_t x = 0; x < 48; x++)
					{
						uint32_t frameOffset = (y * 256) + x;
						uint32_t p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (uint32_t)(p | 0x80000000);
					}
				}

				// Right 48 pixels
				for (uint32_t y = 24; y < 168; y++)
				{
					for (uint32_t x = 208; x < 256; x++)
					{
						uint32_t frameOffset = (y * 256) + x;
						uint32_t p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (uint32_t)(p | 0x80000000);
					}
				}
			}
			*/
		}
		#pragma endregion

		#pragma region ModeTMS

		uint32_t PaletteTMS9918[16]
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

		void RenderBackgroundM0(bool show)
		{
			if (ScanLine >= FrameHeight)
				return;

			if (DisplayOn() == false)
			{
				for (int32_t i = ScanLine * 256; i < (ScanLine * 256 + 256); i++)
				{
					FrameBuffer[i] = 0;
				}
				return;
			}

			int32_t yc = ScanLine / 8;
			int32_t yofs = ScanLine % 8;
			int32_t FrameBufferOffset = ScanLine * 256;
			int32_t PatternNameOffset = TmsPatternNameTableBase + (yc * 32);
			int32_t ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (int32_t xc = 0; xc < 32; xc++)
			{
				int32_t pn = VRAM[PatternNameOffset++];
				int32_t pv = VRAM[PatternGeneratorBase + (pn * 8) + yofs];
				int32_t colorEntry = VRAM[ColorTableBase + (pn / 8)];
				int32_t fgIndex = (colorEntry >> 4) & 0x0F;
				int32_t bgIndex = colorEntry & 0x0F;
				int32_t fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				int32_t bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x80) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x40) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x20) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x10) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x08) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x04) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x02) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x01) > 0) ? fgColor : bgColor) : 0;
			}
		}

		void RenderBackgroundM2(bool show)
		{
			if (ScanLine >= FrameHeight)
				return;

			if (DisplayOn() == false)
			{
				for (int32_t i = ScanLine * 256; i < (ScanLine * 256 + 256); i++)
				{
					FrameBuffer[i] = 0;
				}
				return;
			}

			int32_t yrow = ScanLine / 8;
			int32_t yofs = ScanLine % 8;
			int32_t FrameBufferOffset = ScanLine * 256;
			int32_t PatternNameOffset = TmsPatternNameTableBase + (yrow * 32);
			int32_t PatternGeneratorOffset = (((Registers[4] & 4) << 11) & 0x2000);// +((yrow / 8) * 0x100);
			int32_t ColorOffset = (ColorTableBase & 0x2000);// +((yrow / 8) * 0x100);
			int32_t ScreenBGColor = PaletteTMS9918[Registers[7] & 0x0F];

			for (int32_t xc = 0; xc < 32; xc++)
			{
				int32_t pn = VRAM[PatternNameOffset++] + ((yrow / 8) * 0x100);
				int32_t pv = VRAM[PatternGeneratorOffset + (pn * 8) + yofs];
				int32_t colorEntry = VRAM[ColorOffset + (pn * 8) + yofs];
				int32_t fgIndex = (colorEntry >> 4) & 0x0F;
				int32_t bgIndex = colorEntry & 0x0F;
				int32_t fgColor = fgIndex == 0 ? ScreenBGColor : PaletteTMS9918[fgIndex];
				int32_t bgColor = bgIndex == 0 ? ScreenBGColor : PaletteTMS9918[bgIndex];

				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x80) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x40) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x20) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x10) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x08) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x04) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x02) > 0) ? fgColor : bgColor) : 0;
				FrameBuffer[FrameBufferOffset++] = show ? (((pv & 0x01) > 0) ? fgColor : bgColor) : 0;
			}
		}

		void RenderTmsSprites(bool show)
		{
			if (ScanLine >= FrameHeight || DisplayOn() == false)
				return;

			if (EnableDoubledSprites() == false)
				RenderTmsSpritesStandard(show);
			else
				RenderTmsSpritesDouble(show);
		}

		void RenderTmsSpritesStandard(bool show)
		{
			for (uint32_t i = 0; i < 256; i++)
			{
				ScanlinePriorityBuffer[i] = 0;
			}
			for (uint32_t i = 0; i < 256; i++)
			{
				SpriteCollisionBuffer[i] = 0;
			}

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
				if (y > ScanLine || y + SpriteSize <= ScanLine) continue; // sprite is not on this scanline
				if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

				if (++NumSpritesOnScanline == 5)
				{
					Statusuint8_t &= 0xE0;    // Clear FS0-FS4 bits
					Statusuint8_t |= (int8_t)i; // set 5th sprite index
					Statusuint8_t |= 0x40;    // set overflow bit
					break;
				}

				if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-uint8_t alignment
				int32_t SpriteLine = ScanLine - y;

				// pv contains the VRAM uint8_t holding the pattern data for this character at this scanline.
				// each uint8_t contains the pattern data for each the 8 pixels on this line.
				// the bit-shift further down on PV pulls out the relevant horizontal pixel.

				uint8_t pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine];

				for (int32_t xp = 0; xp < SpriteSize && x + xp < 256; xp++)
				{
					if (x + xp < 0) continue;
					if (LargeSprites && xp == OneCellSize)
						pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine + 16];

					if (Color != 0 && (pv & (1 << (7 - (xp & 7)))) > 0)
					{
						if (SpriteCollisionBuffer[x + xp] != 0)
							Statusuint8_t |= 0x20; // Set sprite collision flag

						if (ScanlinePriorityBuffer[x + xp] == 0)
						{
							ScanlinePriorityBuffer[x + xp] = 1;
							SpriteCollisionBuffer[x + xp] = 1;
							if (show)
								FrameBuffer[(ScanLine * 256) + x + xp] = PaletteTMS9918[Color & 0x0F];
						}
					}
				}
			}
		}

		void RenderTmsSpritesDouble(bool show)
		{
			for (uint32_t i = 0; i < 256; i++)
			{
				ScanlinePriorityBuffer[i] = 0;
			}
			for (uint32_t i = 0; i < 256; i++)
			{
				SpriteCollisionBuffer[i] = 0;
			}

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
				if (y > ScanLine || y + SpriteSize <= ScanLine) continue; // sprite is not on this scanline
				if ((Color & 0x80) > 0) x -= 32; // Early Clock adjustment

				if (++NumSpritesOnScanline == 5)
				{
					Statusuint8_t &= 0xE0;    // Clear FS0-FS4 bits
					Statusuint8_t |= (uint8_t)i; // set 5th sprite index
					Statusuint8_t |= 0x40;    // set overflow bit
					break;
				}

				if (LargeSprites) Pattern &= 0xFC; // 16x16 sprites forced to 4-uint8_t alignment
				int32_t SpriteLine = ScanLine - y;
				SpriteLine /= 2; // because of sprite magnification

				uint8_t pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine];

				for (int32_t xp = 0; (xp < SpriteSize) && ((x + xp) < 256); xp++)
				{
					if (x + xp < 0) continue;
					if (LargeSprites && xp == OneCellSize)
						pv = VRAM[SpritePatternGeneratorBase + (Pattern * 8) + SpriteLine + 16];

					if (Color != 0 && (pv & (1 << (7 - ((xp / 2) & 7)))) > 0)  // xp/2 is due to sprite magnification
					{
						if (SpriteCollisionBuffer[x + xp] != 0)
							Statusuint8_t |= 0x20; // Set sprite collision flag

						if (ScanlinePriorityBuffer[x + xp] == 0)
						{
							ScanlinePriorityBuffer[x + xp] = 1;
							SpriteCollisionBuffer[x + xp] = 1;
							if (show)
								FrameBuffer[(ScanLine * 256) + x + xp] = PaletteTMS9918[Color & 0x0F];
						}
					}
				}
			}
		}
		#pragma endregion

		#pragma region Tables

		// TODO: HCounter
		uint8_t VLineCounterTableNTSC192[262] =
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
			0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
			0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
										  0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};

		uint8_t VLineCounterTableNTSC224[262] =
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
			0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
			0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,
										  0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};

		uint8_t VLineCounterTableNTSC240[262] =
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
			0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
			0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05
		};

		uint8_t VLineCounterTablePAL192[313] =
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
			0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
			0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2,
																		0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF
		};

		uint8_t VLineCounterTablePAL224[313] =
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
			0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
			0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
			0x00, 0x01, 0x02,
																		0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF
		};

		uint8_t VLineCounterTablePAL240[313] =
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
			0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
			0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A,
						0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF
		};
		#pragma endregion
	};
}
