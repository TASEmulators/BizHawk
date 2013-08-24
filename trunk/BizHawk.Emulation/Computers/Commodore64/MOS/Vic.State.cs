using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Vic
    {
        int backgroundColor0;
        int backgroundColor1;
        int backgroundColor2;
        int backgroundColor3;
        int baCount;
        bool badline;
        bool badlineEnable;
        int bitmapColumn;
        bool bitmapMode;
        int borderB;
        bool borderCheckLEnable;
        bool borderCheckREnable;
        int borderColor;
        int borderL;
        bool borderOnMain;
        bool borderOnVertical;
        int borderR;
        int borderT;
        int[] bufferC;
        int[] bufferG;
        int cycle;
        int cycleIndex;
        bool columnSelect;
        int dataC;
        int dataG;
        bool debugScreen;
        bool displayEnable;
        int displayIndex;
        int displayC;
        bool enableIntLightPen;
        bool enableIntRaster;
        bool enableIntSpriteCollision;
        bool enableIntSpriteDataCollision;
        bool extraColorMode;
        bool hblank;
        bool idle;
        bool intLightPen;
        bool intRaster;
        bool intSpriteCollision;
        bool intSpriteDataCollision;
        int hblankEnd;
        int hblankStart;
        bool hblankCheckEnableL;
        bool hblankCheckEnableR;
        int lastRasterLine;
        int lightPenX;
        int lightPenY;
        bool multicolorMode;
        bool pinAEC = true;
        bool pinBA = true;
        bool pinIRQ = true;
        int[] pixelDataBuffer;
        int pointerCB;
        int pointerVM;
        int rasterInterruptLine;
        int rasterLine;
        int rasterX;
        bool rasterXHold;
        int rc;
        int refreshCounter;
        bool renderEnabled;
        bool rowSelect;
        int spriteMulticolor0;
        int spriteMulticolor1;
        Sprite[] sprites;
        int sr;
        bool vblank;
        int vblankEnd;
        int vblankStart;
        int vc;
        int vcbase;
        int vmli;
        int xOffset;
        int xScroll;
        int yScroll;

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
            vblank = true;
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
