using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    public abstract partial class Vic
    {
        protected int backgroundColor0;
        protected int backgroundColor1;
        protected int backgroundColor2;
        protected int backgroundColor3;
        protected int baCount;
        protected bool badline;
        protected bool badlineEnable;
        protected int bitmapColumn;
        protected bool bitmapMode;
        protected int borderB;
        protected bool borderCheckLEnable;
        protected bool borderCheckREnable;
        protected int borderColor;
        protected int borderL;
        protected bool borderOnMain;
        protected bool borderOnVertical;
        protected int borderR;
        protected int borderT;
        protected int[] bufferC;
        protected int[] bufferG;
        protected int cycle;
        protected int cycleIndex;
        protected bool columnSelect;
        protected int dataC;
        protected int dataG;
        protected bool debugScreen;
        protected bool displayEnable;
        protected int displayIndex;
        protected int displayC;
        protected bool enableIntLightPen;
        protected bool enableIntRaster;
        protected bool enableIntSpriteCollision;
        protected bool enableIntSpriteDataCollision;
        protected bool extraColorMode;
        protected bool idle;
        protected bool intLightPen;
        protected bool intRaster;
        protected bool intSpriteCollision;
        protected bool intSpriteDataCollision;
        protected int lastRasterLine;
        protected int lightPenX;
        protected int lightPenY;
        protected bool multicolorMode;
        protected bool pinAEC = true;
        protected bool pinBA = true;
        protected bool pinIRQ = true;
        protected int[] pixelDataBuffer;
        protected int pointerCB;
        protected int pointerVM;
        protected int rasterInterruptLine;
        protected int rasterLine;
        protected int rasterX;
        protected int rc;
        protected int refreshCounter;
        protected bool renderEnabled;
        protected bool rowSelect;
        protected int spriteMulticolor0;
        protected int spriteMulticolor1;
        protected Sprite[] sprites;
        protected int sr;
        protected int vc;
        protected int vcbase;
        protected int vmli;
        protected int xOffset;
        protected int xScroll;
        protected int yScroll;

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
            Sync.SyncObject(ser, this);
            for (int i = 0; i < 8; i++)
            {
                ser.BeginSection("sprite" + i.ToString());
                Sync.SyncObject(ser, sprites[i]);
                ser.EndSection();
            }
        }
    }
}
