using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	sealed public partial class Vic
	{
		private int backgroundColor0;
		private int backgroundColor1;
		private int backgroundColor2;
		private int backgroundColor3;
		private int baCount;
		private bool badline;
		private bool badlineEnable;
		private int bitmapColumn;
		private bool bitmapMode;
		private int borderB;
		private bool borderCheckLEnable;
		private bool borderCheckREnable;
		private int borderColor;
		private int borderL;
		private bool borderOnMain;
		private bool borderOnVertical;
		private int borderR;
		private int borderT;
		private int[] bufferC;
		private int[] bufferG;
		private int cycle;
		private int cycleIndex;
		private bool columnSelect;
		private int dataC;
		private int dataG;
		private bool displayEnable;
		private int displayC;
		private bool enableIntLightPen;
		private bool enableIntRaster;
		private bool enableIntSpriteCollision;
		private bool enableIntSpriteDataCollision;
		private bool extraColorMode;
		private bool hblank;
		private bool idle;
		private bool intLightPen;
		private bool intRaster;
		private bool intSpriteCollision;
		private bool intSpriteDataCollision;
		private int hblankEnd;
		private int hblankStart;
		private bool hblankCheckEnableL;
		private bool hblankCheckEnableR;
		private int lastRasterLine;
		private int lightPenX;
		private int lightPenY;
		private bool multicolorMode;
		private bool pinAEC = true;
		private bool pinBA = true;
		private bool pinIRQ = true;
		private int pointerCB;
		private int pointerVM;
		private int rasterInterruptLine;
		private int rasterLine;
		private int rasterX;
		private bool rasterXHold;
		private int rc;
		private int refreshCounter;
		private bool renderEnabled;
		private bool rowSelect;
		private int spriteMulticolor0;
		private int spriteMulticolor1;
		private Sprite[] sprites;
		private int sr;
		private int srMask;
		private int srMask1;
		private int srMask2;
		private int srMask3;
		private int srMaskMC;
		private int srSpriteMask;
		private int srSpriteMask1;
		private int srSpriteMask2;
		private int srSpriteMask3;
		private int srSpriteMaskMC;
		private bool vblank;
		private int vblankEnd;
		private int vblankStart;
		private int vc;
		private int vcbase;
		private int vmli;
		private int xScroll;
		private int yScroll;

		public void HardReset()
		{
			// *** SHIFT REGISTER BITMASKS ***
			srMask1 = 0x20000;
			srMask2 = srMask1 << 1;
			srMask3 = srMask1 | srMask2;
			srMask = srMask2;
			srMaskMC = srMask3;
			srSpriteMask1 = 0x400000;
			srSpriteMask2 = srSpriteMask1 << 1;
			srSpriteMask3 = srSpriteMask1 | srSpriteMask2;
			srSpriteMask = srSpriteMask2;
			srSpriteMaskMC = srSpriteMask3;

			pinAEC = true;
			pinBA = true;
			pinIRQ = true;

			bufOffset = 0;

			backgroundColor0 = 0;
			backgroundColor1 = 0;
			backgroundColor2 = 0;
			backgroundColor3 = 0;
			baCount = baResetCounter;
			badline = false;
			badlineEnable = false;
			bitmapMode = false;
			borderCheckLEnable = false;
			borderCheckREnable = false;
			borderColor = 0;
			borderOnMain = true;
			borderOnVertical = true;
			columnSelect = false;
			displayEnable = false;
			enableIntLightPen = false;
			enableIntRaster = false;
			enableIntSpriteCollision = false;
			enableIntSpriteDataCollision = false;
			extraColorMode = false;
			hblank = true;
			idle = true;
			intLightPen = false;
			intRaster = false;
			intSpriteCollision = false;
			intSpriteDataCollision = false;
			lastRasterLine = 0;
			lightPenX = 0;
			lightPenY = 0;
			multicolorMode = false;
			pointerCB = 0;
			pointerVM = 0;
			rasterInterruptLine = 0;
			rasterLine = 0;
			rasterX = 0;
			rc = 7;
			refreshCounter = 0xFF;
			rowSelect = false;
			spriteMulticolor0 = 0;
			spriteMulticolor1 = 0;
			sr = 0;
			vblank = true;
			vc = 0;
			vcbase = 0;
			vmli = 0;
			xScroll = 0;
			yScroll = 0;

			// reset sprites
			for (int i = 0; i < 8; i++)
				sprites[i].HardReset();

			// clear C buffer
			for (int i = 0; i < 40; i++)
			{
				bufferC[i] = 0;
				bufferG[i] = 0;
			}

			pixBuffer = new int[pixBufferSize];
			UpdateBorder();
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
			for (int i = 0; i < 8; i++)
			{
				ser.BeginSection("sprite" + i.ToString());
				SaveState.SyncObject(ser, sprites[i]);
				ser.EndSection();
			}
		}
	}
}
