using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    public abstract partial class Vic
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
        private bool debugScreen;
        private bool displayEnable;
        private int displayIndex;
        private int displayC;
        private bool enableIntLightPen;
        private bool enableIntRaster;
        private bool enableIntSpriteCollision;
        private bool enableIntSpriteDataCollision;
        private bool extraColorMode;
        private bool idle;
        private bool intLightPen;
        private bool intRaster;
        private bool intSpriteCollision;
        private bool intSpriteDataCollision;
        private int lastRasterLine;
        private int lightPenX;
        private int lightPenY;
        private bool multicolorMode;
        private bool pinAEC = true;
        private bool pinBA = true;
        private bool pinIRQ = true;
        private int[] pixelDataBuffer;
        private int pointerCB;
        private int pointerVM;
        private int rasterInterruptLine;
        private int rasterLine;
        private int rasterX;
        private int rc;
        private int refreshCounter;
        private bool renderEnabled;
        private bool rowSelect;
        private int spriteMulticolor0;
        private int spriteMulticolor1;
        private Sprite[] sprites;
        private int sr;
        private int vc;
        private int vcbase;
        private int vmli;
        private int xOffset;
        private int xScroll;
        private int yScroll;

        public void HardReset()
        {
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
            displayIndex = 0;
            enableIntLightPen = false;
            enableIntRaster = false;
            enableIntSpriteCollision = false;
            enableIntSpriteDataCollision = false;
            extraColorMode = false;
            idle = true;
            intLightPen = false;
            intRaster = false;
            intSpriteCollision = false;
            intSpriteDataCollision = false;
            lastRasterLine = 0;
            lightPenX = 0;
            lightPenY = 0;
            multicolorMode = false;
            pixelBufferIndex = 0;
            pixelBackgroundBufferIndex = 0;
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
            vc = 0;
            vcbase = 0;
            vmli = 0;
            xOffset = 0;
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

            // clear pixel buffer
            for (int i = 0; i < pixelBufferDelay; i++)
            {
                pixelBuffer[i] = 0;
                pixelDataBuffer[i] = 0;
            }
            for (int i = 0; i < pixelBackgroundBufferDelay; i++)
                pixelBackgroundBuffer[i] = 0;

            UpdateBorder();
        }

        public void SyncState(Serializer ser)
        {
        }
    }
}
