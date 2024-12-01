// Contains rendering functions for TMS9918 Mode 4.

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class VDP
	{
		internal void RenderBackgroundCurrentLine(bool show)
		{
			if (ScanLine >= FrameHeight)
				return;
			
			if (!DisplayOn)
			{
				for (int x = 0; x < 256; x++)
					FrameBuffer[(ScanLine * 256) + x] = Palette[BackdropColor];
				return;
			}

			// Clear the priority buffer for this scanline
			Array.Clear(ScanlinePriorityBuffer, 0, 256);

			int mapBase = NameTableBase;

			int vertOffset = ScanLine + Registers[9];
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
			byte horzOffset = (HorizScrollLock && ScanLine < 16) ? (byte)0 : Registers[8];

			int yTile = vertOffset / 8;

			for (int xTile = 0; xTile < 32; xTile++)
			{
				if (xTile == lock_tile_start && VerticalScrollLock)
				{
					vertOffset = ScanLine;
					yTile = vertOffset / 8;
				}

				byte PaletteBase = 0;

				int VRAM_addr = mapBase + ((yTile * 32) + xTile) * 2;
				if (JPN_Compat) { VRAM_addr &= NameTableMaskBit; }

				int tileInfo = VRAM[VRAM_addr] | (VRAM[VRAM_addr + 1] << 8);

				int tileNo = tileInfo & 0x01FF;
				if ((tileInfo & 0x800) != 0)
					PaletteBase = 16;
				bool Priority = (tileInfo & 0x1000) != 0;
				bool VFlip = (tileInfo & 0x400) != 0;
				bool HFlip = (tileInfo & 0x200) != 0;

				int yOfs = vertOffset & 7;
				if (VFlip)
					yOfs = 7 - yOfs;

				if (!HFlip)
				{
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase] : Palette[BackdropColor];

					if (Priority)
					{
						horzOffset -= 8;
						for (int k = 0; k < 8; k++)
						{
							if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
								ScanlinePriorityBuffer[horzOffset] = 1;
							horzOffset++;
						}
					}
				}
				else // Flipped Horizontally
				{
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 7] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 6] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 5] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 4] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 3] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 2] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 1] + PaletteBase] : Palette[BackdropColor];
					FrameBuffer[(ScanLine * 256) + horzOffset++] = show ? Palette[PatternBuffer[(tileNo * 64) + (yOfs * 8) + 0] + PaletteBase] : Palette[BackdropColor];

					if (Priority)
					{
						horzOffset -= 8;
						for (int k = 7; k >= 0; k--)
						{
							if (PatternBuffer[(tileNo * 64) + (yOfs * 8) + k] != 0)
								ScanlinePriorityBuffer[horzOffset] = 1;
							horzOffset++;
						}
					}
				}
			}
		}

		internal void RenderSpritesCurrentLine(bool show)
		{
			bool overflowHappens = true;
			bool collisionHappens = true;
			bool renderHappens = show;

			if (!DisplayOn)
			{
				renderHappens = false;
				collisionHappens = false;
			}
			if (ScanLine >= FrameHeight)
			{
				renderHappens = false;
				overflowHappens = false;
				collisionHappens = false;
			}

			int SpriteBase = SpriteAttributeTableBase;
			int SpriteHeight = EnableLargeSprites ? 16 : 8;

			// Clear the sprite collision buffer for this scanline
			Array.Clear(SpriteCollisionBuffer, 0, 256);

			// Loop through these sprites and render the current scanline
			int SpritesDrawnThisScanline = 0;
			for (int i = 0; i < 64; i++)
			{
				int x = VRAM[SpriteBase + 0x80 + (i * 2)];
				if (ShiftSpritesLeft8Pixels)
					x -= 8;

				int y = VRAM[SpriteBase + i] + 1;
				if (y == 209 && FrameHeight == 192) 
					break; // 208 is special terminator sprite (in 192-line mode)
				if (y >= (EnableLargeSprites ? 240 : 248)) 
					y -= 256;

				if (y + SpriteHeight <= ScanLine || y > ScanLine)
					continue;

				if (SpritesDrawnThisScanline >= 8)
				{
					collisionHappens = false; // technically the VDP stops processing sprite past this so we would never set the collision bit for sprites past this
					if (overflowHappens)
						StatusByte |= 0x40; // Set Overflow bit
					if (SpriteLimit)
						renderHappens = false; // should be able to break/return, but to ensure this has no effect on sync we keep processing and disable rendering
				}

				int tileNo = VRAM[SpriteBase + 0x80 + (i * 2) + 1];
				if (EnableLargeSprites)
					tileNo &= 0xFE;
				tileNo += SpriteTileBase;

				int ys = ScanLine - y;

				for (int xs = 0; xs < 8 && x + xs < 256; xs++)
				{
					byte color = PatternBuffer[(tileNo * 64) + (ys * 8) + xs];
					if (color != 0 && x + xs >= 0)
					{
						if (SpriteCollisionBuffer[x + xs] != 0)
						{
							if (collisionHappens)
								StatusByte |= 0x20; // Set Collision bit	
						}
						else if (renderHappens && ScanlinePriorityBuffer[x + xs] == 0)
						{
							FrameBuffer[(ys + y) * 256 + x + xs] = Palette[(color + 16)];
						}
						if (collisionHappens)
							SpriteCollisionBuffer[x + xs] = (byte)i;
					}
				}
				SpritesDrawnThisScanline++;
			}
		}

		internal void RenderSpritesCurrentLineDoubleSize(bool show)
		{
			bool overflowHappens = true;
			bool collisionHappens = true;
			bool renderHappens = show;

			if (!DisplayOn)
			{
				renderHappens = false;
				collisionHappens = false;
			}
			if (ScanLine >= FrameHeight)
			{
				renderHappens = false;
				overflowHappens = false;
				collisionHappens = false;
			}

			int SpriteBase = SpriteAttributeTableBase;
			int SpriteHeight = EnableLargeSprites ? 16 : 8;

			// Clear the sprite collision buffer for this scanline
			Array.Clear(SpriteCollisionBuffer, 0, 256);

			// Loop through these sprites and render the current scanline
			int SpritesDrawnThisScanline = 0;
			for (int i = 0; i < 64; i++)
			{
				int x = VRAM[SpriteBase + 0x80 + (i * 2)];
				if (ShiftSpritesLeft8Pixels)
					x -= 8;

				int y = VRAM[SpriteBase + i] + 1;
				if (y == 209 && FrameHeight == 192) 
					break; // terminator sprite
				if (y >= (EnableLargeSprites ? 240 : 248)) 
					y -= 256;

				if (y + (SpriteHeight * 2) <= ScanLine || y > ScanLine)
					continue;

				if (SpritesDrawnThisScanline >= 8)
				{
					collisionHappens = false; // technically the VDP stops processing sprite past this so we would never set the collision bit for sprites past this
					if (overflowHappens)
						StatusByte |= 0x40; // Set Overflow bit
					if (SpriteLimit)
						renderHappens = false; // should be able to break/return, but to ensure this has no effect on sync we keep processing and disable rendering
				}

				int tileNo = VRAM[SpriteBase + 0x80 + (i * 2) + 1];
				if (EnableLargeSprites)
					tileNo &= 0xFE;
				tileNo += SpriteTileBase;

				int ys = ScanLine - y;

				for (int xs = 0; xs < 16 && x + xs < 256; xs++)
				{
					byte color = PatternBuffer[(tileNo * 64) + ((ys / 2) * 8) + (xs / 2)];
					if (color != 0 && x + xs >= 0)
					{
						if (SpriteCollisionBuffer[x + xs] != 0)
						{
							if (collisionHappens)
								StatusByte |= 0x20; // Set Collision bit
						}
						else if (renderHappens && ScanlinePriorityBuffer[x + xs] == 0)
						{
							FrameBuffer[(ys + y) * 256 + x + xs] = Palette[(color + 16)];
						}
						if (collisionHappens)
							SpriteCollisionBuffer[x + xs] = 1;
					}
				}
				SpritesDrawnThisScanline++;
			}
		}

		// Renders left-blanking. Should be done per scanline, not per-frame.
		internal void RenderLineBlanking(bool render)
		{
			if (!LeftBlanking || ScanLine >= FrameHeight || !render) 
				return;

			int ofs = ScanLine * 256;
			for (int x = 0; x < 8; x++)
				FrameBuffer[ofs++] = Palette[BackdropColor];
		}

		internal int OverscanFrameWidth, OverscanFrameHeight;
		private int overscanTop;
		private int overscanBottom;
		private int overscanLeft;
		private int overscanRight;

		internal void ProcessOverscan()
		{
			if (!Sms.Settings.DisplayOverscan)
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
			for (int y=0; y<overscanTop; y++)
				for (int x = 0; x < OverscanFrameWidth; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = Backdrop_SL[0];
			
			// Bottom overscan
			for (int y = overscanTop + 192; y < OverscanFrameHeight; y++)
				for (int x = 0; x < OverscanFrameWidth; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = Backdrop_SL[FrameHeight-1];

			// Left overscan
			for (int y = overscanTop; y < overscanTop + 192; y++)
				for (int x = 0; x < overscanLeft; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = Backdrop_SL[y - overscanTop];

			// Right overscan
			for (int y = overscanTop; y < overscanTop + 192; y++)
				for (int x = overscanLeft + 256; x < OverscanFrameWidth; x++)
					OverscanFrameBuffer[(y * OverscanFrameWidth) + x] = Backdrop_SL[y - overscanTop];

			// Active display area
			for (int y = 0; y < 192; y++)
				for (int x = 0; x < 256; x++)
					OverscanFrameBuffer[((y + overscanTop) * OverscanFrameWidth) + overscanLeft + x] = FrameBuffer[y * 256 + x];
		}


		// Handles GG clipping or highlighting
		internal void ProcessGGScreen()
		{
			if (mode != VdpMode.GameGear)
				return;

			if (!Sms.Settings.ShowClippedRegions)
			{
				int yStart = (FrameHeight - 144) / 2;
				for (int y = 0; y < 144; y++)
					for (int x = 0; x < 160; x++)
						GameGearFrameBuffer[(y * 160) + x] = FrameBuffer[((y + yStart) * 256) + x + 48];
			}

			if (Sms.Settings.HighlightActiveDisplayRegion && Sms.Settings.ShowClippedRegions)
			{
				// Top 24 scanlines
				for (int y = 0; y < 24; y++)
				{
					for (int x = 0; x < 256; x++)
					{
						int frameOffset = (y * 256) + x;
						int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
					}
				}

				// Bottom 24 scanlines
				for (int y = 168; y < 192; y++)
				{
					for (int x = 0; x < 256; x++)
					{
						int frameOffset = (y * 256) + x;
						int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
					}
				}

				// Left 48 pixels
				for (int y = 24; y < 168; y++)
				{
					for (int x = 0; x < 48; x++)
					{
						int frameOffset = (y * 256) + x;
						int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
					}
				}

				// Right 48 pixels
				for (int y = 24; y < 168; y++)
				{
					for (int x = 208; x < 256; x++)
					{
						int frameOffset = (y * 256) + x;
						int p = (FrameBuffer[frameOffset] >> 1) & 0x7F7F7F7F;
						FrameBuffer[frameOffset] = (int)((uint)p | 0x80000000);
					}
				}
			}
		}
	}
}
