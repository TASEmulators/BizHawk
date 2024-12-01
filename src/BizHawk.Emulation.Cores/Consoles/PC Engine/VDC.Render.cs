using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	// This rendering code is only used for TurboGrafx/TurboCD Mode.
	// In SuperGrafx mode, separate rendering functions in the VPC class are used.
	public partial class VDC
	{
		/* There are many line-counters here. Here is a breakdown of what they each are:

		 + ScanLine is the current NTSC scanline. It has a range from 0 to 262.
		 + ActiveLine is the current offset into the framebuffer. 0 is the first
		   line of active display, and the last value will be BufferHeight-1.
		 + BackgroundY is the current offset into the scroll plane. It is set with BYR
		   register at certain sync points and incremented every scanline. 
		   Its values range from 0 - $1FF.
		 + RCRCounter is set to $40 at the first line of active display, and incremented each
		   scanline thereafter.
		*/
		public int ScanLine;
		public int BackgroundY;
		public int RCRCounter;
		public int ActiveLine;

		public int HBlankCycles = 79;
		public bool PerformSpriteLimit;

		private readonly byte[] PriorityBuffer = new byte[512];
		private readonly byte[] InterSpritePriorityBuffer = new byte[512];

		public void ExecFrame(bool render)
		{
			if (MultiResHack > 0 && render)
				Array.Clear(FrameBuffer, 0, FrameBuffer.Length);

			int ActiveDisplayStartLine = DisplayStartLine;

			while (true)
			{
				int VBlankLine = ActiveDisplayStartLine + Registers[VDW] + 1;
				if (VBlankLine > 261)
					VBlankLine = 261;
				ActiveLine = ScanLine - ActiveDisplayStartLine;
				bool InActiveDisplay = (ScanLine >= ActiveDisplayStartLine) && (ScanLine < VBlankLine);

				if (ScanLine == ActiveDisplayStartLine)
					RCRCounter = 0x40;

				if (ScanLine == VBlankLine)
					UpdateSpriteAttributeTable();

				if (RCRCounter == (Registers[RCR] & 0x3FF))
				{
					if (RasterCompareInterruptEnabled)
					{
						StatusByte |= StatusRasterCompare;
						cpu.IRQ1Assert = true;
					}
				}

				cpu.Execute(HBlankCycles);

				if (InActiveDisplay)
				{
					if (ScanLine == ActiveDisplayStartLine)
						BackgroundY = Registers[BYR];
					else
					{
						BackgroundY++;
						BackgroundY &= 0x01FF;
					}

					if (render) RenderScanLine();
				}

				if (ScanLine == VBlankLine && VBlankInterruptEnabled)
					StatusByte |= StatusVerticalBlanking;

				if (ScanLine == VBlankLine + 4 && SatDmaPerformed)
				{
					SatDmaPerformed = false;
					if ((Registers[DCR] & 1) > 0)
						StatusByte |= StatusVramSatDmaComplete;
				}

				cpu.Execute(2);

				if ((StatusByte & (StatusVerticalBlanking | StatusVramSatDmaComplete)) != 0)
					cpu.IRQ1Assert = true;

				cpu.Execute(455 - HBlankCycles - 2);

				if (!InActiveDisplay && DmaRequested)
					RunDmaForScanline();

				ScanLine++;
				RCRCounter++;

				if (ScanLine == vce.NumberOfScanlines)
				{
					ScanLine = 0;
					break;
				}
			}
		}

		public void RenderScanLine()
		{
			if (pce.Settings.BottomLine <= ActiveLine + ViewStartLine
				|| ActiveLine + ViewStartLine < pce.Settings.TopLine)
			{
				return;
			}
			RenderBackgroundScanline(pce.Settings.ShowBG1);
			RenderSpritesScanline(pce.Settings.ShowOBJ1);
		}

		private readonly Action<bool> RenderBackgroundScanline;

		private unsafe void RenderBackgroundScanlineUnsafe(bool show)
		{
			Array.Clear(PriorityBuffer, 0, FrameWidth);

			if (!BackgroundEnabled)
			{
				int p = vce.Palette[256];
				fixed (int* FBptr = FrameBuffer)
				{
					int* dst = FBptr + (ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch;
					for (int i = 0; i < FrameWidth; i++)
						*dst++ = p;
				}

				return;
			}

			// per-line parameters
			int vertLine = BackgroundY;
			vertLine %= BatHeight * 8;
			int yTile = (vertLine / 8);
			int yOfs = vertLine % 8;
			int xScroll = Registers[BXR] & 0x3FF;
			int BatRowMask = BatWidth - 1;

			fixed (ushort* VRAMptr = VRAM)
			fixed (int* PALptr = vce.Palette)
			fixed (byte* Patternptr = PatternBuffer)
			fixed (int* FBptr = FrameBuffer)
			fixed (byte* Priortyptr = PriorityBuffer)
			{
				// pointer to the BAT and the framebuffer for this line
				ushort* BatRow = VRAMptr + yTile * BatWidth;
				int* dst = FBptr + (ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch;

				// parameters that change per tile
				ushort BatEnt;
				int tileNo, paletteNo, paletteBase;
				byte* src;

				// calculate tile number and offset for first tile
				int xTile = (xScroll >> 3) & BatRowMask;
				int xOfs = xScroll & 7;

				// update per-tile parameters for first tile
				BatEnt = BatRow[xTile];
				tileNo = BatEnt & 2047;
				paletteNo = BatEnt >> 12;
				paletteBase = paletteNo * 16;
				src = Patternptr + (tileNo << 6 | yOfs << 3 | xOfs);

				for (int x = 0; x < FrameWidth; x++)
				{
					byte c = *src++;
					if (c == 0)
						dst[x] = PALptr[0];
					else
					{
						dst[x] = show ? PALptr[paletteBase + c] : PALptr[0];
						Priortyptr[x] = 1;
					}

					xOfs++;
					if (xOfs == 8)
					{
						// update tile number
						xOfs = 0;
						xTile++;
						xTile &= BatRowMask;
						// update per-tile parameters
						BatEnt = BatRow[xTile];
						tileNo = BatEnt & 2047;
						paletteNo = BatEnt >> 12;
						paletteBase = paletteNo * 16;
						src = Patternptr + (tileNo << 6 | yOfs << 3 | xOfs);
					}
				}
			}
		}

		private void RenderBackgroundScanlineSafe(bool show)
		{
			Array.Clear(PriorityBuffer, 0, FrameWidth);

			if (!BackgroundEnabled)
			{
				for (int i = 0; i < FrameWidth; i++)
					FrameBuffer[((ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch) + i] = vce.Palette[256];
				return;
			}

			int batHeight = BatHeight * 8;
			int batWidth = BatWidth * 8;

			int vertLine = BackgroundY;
			vertLine %= batHeight;
			int yTile = (vertLine / 8);
			int yOfs = vertLine % 8;

			// This is not optimized. But it seems likely to remain that way.
			int xScroll = Registers[BXR] & 0x3FF;
			for (int x = 0; x < FrameWidth; x++)
			{
				int xTile = ((x + xScroll) / 8) % BatWidth;
				int xOfs = (x + xScroll) & 7;
				int tileNo = VRAM[(ushort)(((yTile * BatWidth) + xTile))] & 2047;
				int paletteNo = VRAM[(ushort)(((yTile * BatWidth) + xTile))] >> 12;
				int paletteBase = paletteNo * 16;

				byte c = PatternBuffer[(tileNo * 64) + (yOfs * 8) + xOfs];
				if (c == 0)
					FrameBuffer[((ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch) + x] = vce.Palette[0];
				else
				{
					FrameBuffer[((ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch) + x] = show ? vce.Palette[paletteBase + c] : vce.Palette[0];
					PriorityBuffer[x] = 1;
				}
			}
		}

		private readonly byte[] heightTable = { 16, 32, 64, 64 };

		public void RenderSpritesScanline(bool show)
		{
			if (!SpritesEnabled)
			{
				return;
			}

			Array.Clear(InterSpritePriorityBuffer, 0, FrameWidth);
			bool Sprite4ColorMode = Sprite4ColorModeEnabled;
			int activeSprites = 0;

			for (int i = 0; i < 64; i++)
			{
				if (activeSprites >= 16 && PerformSpriteLimit)
					break;

				int y = (SpriteAttributeTable[(i * 4) + 0] & 1023) - 64;
				int x = (SpriteAttributeTable[(i * 4) + 1] & 1023) - 32;
				ushort flags = SpriteAttributeTable[(i * 4) + 3];
				int height = heightTable[(flags >> 12) & 3];
				int width = (flags & 0x100) == 0 ? 16 : 32;

				if (y + height <= ActiveLine || y > ActiveLine)
					continue;

				activeSprites += width == 16 ? 1 : 2;

				int patternNo = (((SpriteAttributeTable[(i * 4) + 2]) >> 1) & 0x1FF);
				int paletteBase = 256 + ((flags & 15) * 16);
				bool priority = (flags & 0x80) != 0;
				bool hflip = (flags & 0x0800) != 0;
				bool vflip = (flags & 0x8000) != 0;

				int colorMask = 0xFF;
				if (Sprite4ColorMode)
				{
					if ((SpriteAttributeTable[(i * 4) + 2] & 1) == 0)
						colorMask = 0x03;
					else
						colorMask = 0x0C;
				}

				if (width == 32)
					patternNo &= 0x1FE;

				int yofs = 0;
				if (!vflip)
				{
					yofs = (ActiveLine - y) & 15;
					if (height == 32)
					{
						patternNo &= 0x1FD;
						if (ActiveLine - y >= 16)
						{
							y += 16;
							patternNo += 2;
						}
					}
					else if (height == 64)
					{
						patternNo &= 0x1F9;
						if (ActiveLine - y >= 48)
						{
							y += 48;
							patternNo += 6;
						}
						else if (ActiveLine - y >= 32)
						{
							y += 32;
							patternNo += 4;
						}
						else if (ActiveLine - y >= 16)
						{
							y += 16;
							patternNo += 2;
						}
					}
				}
				else // vflip == true
				{
					yofs = 15 - ((ActiveLine - y) & 15);
					if (height == 32)
					{
						patternNo &= 0x1FD;
						if (ActiveLine - y < 16)
						{
							y += 16;
							patternNo += 2;
						}
					}
					else if (height == 64)
					{
						patternNo &= 0x1F9;
						if (ActiveLine - y < 16)
						{
							y += 48;
							patternNo += 6;
						}
						else if (ActiveLine - y < 32)
						{
							y += 32;
							patternNo += 4;
						}
						else if (ActiveLine - y < 48)
						{
							y += 16;
							patternNo += 2;
						}
					}
				}

				if (!hflip)
				{
					if (x + width > 0 && y + height > 0)
					{
						for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
						{
							byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)] & colorMask);
							if (colorMask == 0x0C)
								pixel >>= 2;
							if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
							{
								InterSpritePriorityBuffer[xs] = 1;
								if ((priority || PriorityBuffer[xs] == 0) && show)
									FrameBuffer[((ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
							}
						}
					}
					if (width == 32)
					{
						patternNo++;
						x += 16;
						for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
						{
							byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + (xs - x)] & colorMask);
							if (colorMask == 0x0C)
								pixel >>= 2;
							if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
							{
								InterSpritePriorityBuffer[xs] = 1;
								if ((priority || PriorityBuffer[xs] == 0) && show)
									FrameBuffer[((ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
							}

						}
					}
				}
				else
				{ // hflip = true
					if (x + width > 0 && y + height > 0)
					{
						if (width == 32)
							patternNo++;
						for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
						{
							byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)] & colorMask);
							if (colorMask == 0x0C)
								pixel >>= 2;
							if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
							{
								InterSpritePriorityBuffer[xs] = 1;
								if ((priority || PriorityBuffer[xs] == 0) && show)
									FrameBuffer[((ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
							}
						}
						if (width == 32)
						{
							patternNo--;
							x += 16;
							for (int xs = x >= 0 ? x : 0; xs < x + 16 && xs >= 0 && xs < FrameWidth; xs++)
							{
								byte pixel = (byte)(SpriteBuffer[(patternNo * 256) + (yofs * 16) + 15 - (xs - x)] & colorMask);
								if (colorMask == 0x0C)
									pixel >>= 2;
								if (pixel != 0 && InterSpritePriorityBuffer[xs] == 0)
								{
									InterSpritePriorityBuffer[xs] = 1;
									if ((priority || PriorityBuffer[xs] == 0) && show)
										FrameBuffer[((ActiveLine + ViewStartLine - pce.Settings.TopLine) * FramePitch) + xs] = vce.Palette[paletteBase + pixel];
								}
							}
						}
					}
				}
			}
		}

		private int FramePitch = 320;
		private int FrameWidth = 320;
		private int[] FrameBuffer = new int[320 * 262];

		public void Resize_Frame_Buffer_MultiResHack()
		{
			FrameBuffer = new int[MultiResHack * 262];
		}

		// IVideoProvider implementation
		public int[] GetVideoBuffer() => FrameBuffer;

		public int VirtualWidth => FramePitch;
		public int VirtualHeight => BufferHeight;
		public int BufferWidth => FramePitch;
		public int BufferHeight => (pce.Settings.BottomLine - pce.Settings.TopLine);
		public int BackgroundColor => vce.Palette[256];

		public int VsyncNumerator
			=> NullVideo.DefaultVsyncNum; //TODO precise numbers or confirm the default is okay

		public int VsyncDenominator
			=> NullVideo.DefaultVsyncDen; //TODO precise numbers or confirm the default is okay
	}
}
