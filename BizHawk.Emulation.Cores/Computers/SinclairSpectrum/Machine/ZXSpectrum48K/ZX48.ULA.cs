
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    class ULA48 : ULABase
    {
        #region Construction

        public ULA48(SpectrumBase machine)
            : base(machine)
        {
            InterruptPeriod = 32;
            LongestOperationCycles = 64;
            FrameLength = 69888;
            ClockSpeed = 3500000;

            contentionTable = new byte[70930];
            floatingBusTable = new short[70930];
            for (int f = 0; f < 70930; f++)
                floatingBusTable[f] = -1;

            CharRows = 24;
            CharCols = 32;
            ScreenWidth = 256;
            ScreenHeight = 192;
            BorderTopHeight = 48;
            BorderBottomHeight = 56;
            BorderLeftWidth = 48;
            BorderRightWidth = 48;
            DisplayStart = 16384;
            DisplayLength = 6144;
            AttributeStart = 22528;
            AttributeLength = 768;
            borderColour = 7;
            ScanLineWidth = BorderLeftWidth + ScreenWidth + BorderRightWidth;

            TstatesPerScanline = 224;
            TstateAtTop = BorderTopHeight * TstatesPerScanline;
            TstateAtBottom = BorderBottomHeight * TstatesPerScanline;
            tstateToDisp = new short[FrameLength];

            ScreenBuffer = new int[ScanLineWidth * BorderTopHeight //48 lines of border
                                              + ScanLineWidth * ScreenHeight //border + main + border of 192 lines
                                              + ScanLineWidth * BorderBottomHeight]; //56 lines of border

            attr = new short[DisplayLength]; //6144 bytes of display memory will be mapped

            SetupScreenSize();

            Reset();
        }

        #endregion

        #region Misc Operations

        public override void Reset()
        {
            contentionStartPeriod = 14335; // + LateTiming;
            contentionEndPeriod = contentionStartPeriod + (ScreenHeight * TstatesPerScanline);
            screen = _machine.RAM0;
            screenByteCtr = DisplayStart;
            ULAByteCtr = 0;
            actualULAStart = 14340 - 24 - (TstatesPerScanline * BorderTopHeight);// + LateTiming;
            lastTState = actualULAStart;
            BuildAttributeMap();
            BuildContentionTable();
        }

        #endregion

        #region Contention Methods

        public override bool IsContended(int addr)
        {
            if ((addr & 49152) == 16384)
                return true;
            return false;
        }

        public override void BuildContentionTable()
        {
            int t = contentionStartPeriod;
            while (t < contentionEndPeriod)
            {
                //for 128 t-states
                for (int i = 0; i < 128; i += 8)
                {
                    contentionTable[t++] = 6;
                    contentionTable[t++] = 5;
                    contentionTable[t++] = 4;
                    contentionTable[t++] = 3;
                    contentionTable[t++] = 2;
                    contentionTable[t++] = 1;
                    contentionTable[t++] = 0;
                    contentionTable[t++] = 0;
                }
                t += (TstatesPerScanline - 128); //24 tstates of right border + left border + 48 tstates of retrace
            }

            //build top half of tstateToDisp table
            //vertical retrace period
            for (t = 0; t < actualULAStart; t++)
                tstateToDisp[t] = 0;

            //next 48 are actual border
            while (t < actualULAStart + (TstateAtTop))
            {
                //border(24t) + screen (128t) + border(24t) = 176 tstates
                for (int g = 0; g < 176; g++)
                    tstateToDisp[t++] = 1;

                //horizontal retrace
                for (int g = 176; g < TstatesPerScanline; g++)
                    tstateToDisp[t++] = 0;
            }

            //build middle half of display
            int _x = 0;
            int _y = 0;
            int scrval = 2;
            while (t < actualULAStart + (TstateAtTop) + (ScreenHeight * TstatesPerScanline))
            {
                //left border
                for (int g = 0; g < 24; g++)
                    tstateToDisp[t++] = 1;

                //screen
                for (int g = 24; g < 24 + 128; g++)
                {
                    //Map screenaddr to tstate
                    if (g % 4 == 0)
                    {
                        scrval = (((((_y & 0xc0) >> 3) | (_y & 0x07) | (0x40)) << 8)) | (((_x >> 3) & 0x1f) | ((_y & 0x38) << 2));
                        _x += 8;
                    }
                    tstateToDisp[t++] = (short)scrval;
                }
                _y++;

                //right border
                for (int g = 24 + 128; g < 24 + 128 + 24; g++)
                    tstateToDisp[t++] = 1;

                //horizontal retrace
                for (int g = 24 + 128 + 24; g < 24 + 128 + 24 + 48; g++)
                    tstateToDisp[t++] = 0;
            }

            int h = contentionStartPeriod + 3;
            while (h < contentionEndPeriod + 3)
            {
                for (int j = 0; j < 128; j += 8)
                {
                    floatingBusTable[h] = tstateToDisp[h + 2];                          //screen address
                    floatingBusTable[h + 1] = attr[(tstateToDisp[h + 2] - 16384)];      //attr address
                    floatingBusTable[h + 2] = tstateToDisp[h + 2 + 4];                  //screen address + 1
                    floatingBusTable[h + 3] = attr[(tstateToDisp[h + 2 + 4] - 16384)];  //attr address + 1
                    h += 8;
                }
                h += TstatesPerScanline - 128;
            }

            //build bottom border
            while (t < actualULAStart + (TstateAtTop) + (ScreenHeight * TstatesPerScanline) + (TstateAtBottom))
            {
                //border(24t) + screen (128t) + border(24t) = 176 tstates
                for (int g = 0; g < 176; g++)
                    tstateToDisp[t++] = 1;

                //horizontal retrace
                for (int g = 176; g < TstatesPerScanline; g++)
                    tstateToDisp[t++] = 0;
            }
        }

        #endregion


    }
}
