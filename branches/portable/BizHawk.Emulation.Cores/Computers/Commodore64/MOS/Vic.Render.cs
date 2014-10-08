using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	sealed public partial class Vic
	{
		private int delayC;
		private int ecmPixel;
		private int pixel;
		private int pixelCounter;
		private int pixelData;
		private int pixelOwner;
		private int sprData;
		private int sprIndex;
		private int sprPixel;
		private int srC = 0;
		private int srSync = 0;
		private int videoMode;

		private void Render()
		{
			if (hblankCheckEnableL)
			{
				if (rasterX == hblankEnd)
					hblank = false;
			}
			else if (hblankCheckEnableR)
			{
				if (rasterX == hblankStart)
					hblank = true;
			}

			renderEnabled = (!hblank && !vblank);
			pixelCounter = -1;
			while (pixelCounter++ < 3)
			{

				if (delayC > 0)
					delayC--;
				else
					displayC = (srC >> 12) & 0xFFF;


				#region PRE-RENDER BORDER
				if (borderCheckLEnable && (rasterX == borderL))
				{
					if (rasterLine == borderB)
						borderOnVertical = true;
					if (rasterLine == borderT && displayEnable)
						borderOnVertical = false;
					if (!borderOnVertical)
						borderOnMain = false;
				}
				#endregion

				#region CHARACTER GRAPHICS
				switch (videoMode)
				{
					case 0:
						pixelData = sr & srMask2;
						pixel = (pixelData != 0) ? (displayC >> 8) : backgroundColor0;
						break;
					case 1:
						if ((displayC & 0x800) != 0)
						{
							// multicolor 001
							if ((srSync & srMask2) != 0)
								pixelData = sr & srMask3;

							if (pixelData == 0)
								pixel = backgroundColor0;
							else if (pixelData == srMask1)
								pixel = backgroundColor1;
							else if (pixelData == srMask2)
								pixel = backgroundColor2;
							else
								pixel = (displayC & 0x700) >> 8;
						}
						else
						{
							// standard 001
							pixelData = sr & srMask2;
							pixel = (pixelData != 0) ? (displayC >> 8) : backgroundColor0;
						}
						break;
					case 2:
						pixelData = sr & srMask2;
						pixel = (pixelData != 0) ? (displayC >> 4) : (displayC);
						break;
					case 3:
						if ((srSync & srMask2) != 0)
							pixelData = sr & srMask3;

						if (pixelData == 0)
							pixel = backgroundColor0;
						else if (pixelData == srMask1)
							pixel = (displayC >> 4);
						else if (pixelData == srMask2)
							pixel = displayC;
						else
							pixel = (displayC >> 8);
						break;
					case 4:
						pixelData = sr & srMask2;
						if (pixelData != 0)
						{
							pixel = displayC >> 8;
						}
						else
						{
							ecmPixel = (displayC) & 0xC0;
							if (ecmPixel == 0x00)
								pixel = backgroundColor0;
							else if (ecmPixel == 0x40)
								pixel = backgroundColor1;
							else if (ecmPixel == 0x80)
								pixel = backgroundColor2;
							else
								pixel = backgroundColor3;
						}
						break;
					default:
						pixelData = 0;
						pixel = 0;
						break;
				}
				pixel &= 0xF;
				sr <<= 1;
				srSync <<= 1;
#endregion

				#region SPRITES
				// render sprites
				pixelOwner = -1;
				sprIndex = 0;
				foreach (var spr in sprites)
				{
					sprData = 0;
					sprPixel = pixel;

					if (spr.x == rasterX)
					{
						spr.shiftEnable = spr.display;
						spr.xCrunch = !spr.xExpand;
						spr.multicolorCrunch = false;
					}
					else
					{
						spr.xCrunch |= !spr.xExpand;
					}

					if (spr.shiftEnable) // sprite rule 6
					{
						if (spr.multicolor)
						{
							sprData = (spr.sr & srSpriteMaskMC);
							if (spr.multicolorCrunch && spr.xCrunch && !rasterXHold)
							{
								if (spr.loaded == 0)
								{
									spr.shiftEnable = false;
								}
								spr.sr <<= 2;
								spr.loaded >>= 2;
							}
							spr.multicolorCrunch ^= spr.xCrunch;
						}
						else
						{
							sprData = (spr.sr & srSpriteMask);
							if (spr.xCrunch && !rasterXHold)
							{
								if (spr.loaded == 0)
								{
									spr.shiftEnable = false;
								}
								spr.sr <<= 1;
								spr.loaded >>= 1;
							}
						}
						spr.xCrunch ^= spr.xExpand;

						if (sprData != 0)
						{
							// sprite-sprite collision
							if (pixelOwner < 0)
							{
								if (sprData == srSpriteMask1)
									sprPixel = spriteMulticolor0;
								else if (sprData == srSpriteMask2)
									sprPixel = spr.color;
								else if (sprData == srSpriteMask3)
									sprPixel = spriteMulticolor1;
								pixelOwner = sprIndex;
							}
							else
							{
								if (!borderOnVertical)
								{
									spr.collideSprite = true;
									sprites[pixelOwner].collideSprite = true;
								}
							}

							// sprite-data collision
							if (!borderOnVertical && (pixelData >= srMask2))
							{
								spr.collideData = true;
							}

							// sprite priority logic
							if (spr.priority)
							{
								pixel = (pixelData >= srMask2) ? pixel : sprPixel;
							}
							else
							{
								pixel = sprPixel;
							}
						}
					}

					sprIndex++;
				}

#endregion

				#region POST-RENDER BORDER
				if (borderCheckREnable && (rasterX == borderR))
					borderOnMain = true;

				// border doesn't work with the background buffer
				if (borderOnMain || borderOnVertical)
					pixel = borderColor;
				#endregion

				// plot pixel if within viewing area
				if (renderEnabled)
				{
					buf[bufOffset] = palette[pixBuffer[pixBufferIndex]];
					bufOffset++;
					if (bufOffset == bufLength)
						bufOffset = 0;
				}

				pixBuffer[pixBufferIndex] = pixel;
				pixBufferIndex++;

				if (!rasterXHold)
					rasterX++;
				bitmapColumn++;
			}

			if (pixBufferIndex >= pixBufferSize)
				pixBufferIndex = 0;
		}
	}
}
