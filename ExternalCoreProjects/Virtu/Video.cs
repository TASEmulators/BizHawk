using System;
using System.IO;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    [Flags]
    public enum ScannerOptions { None = 0x0, AppleII = 0x1, Pal = 0x2 } // defaults to AppleIIe, Ntsc

    public sealed partial class Video : MachineComponent
    {
		public Video() { }
        public Video(Machine machine) : 
            base(machine)
        {
            _flushRowEvent = FlushRowEvent; // cache delegates; avoids garbage
            _inverseTextEvent = InverseTextEvent;
            _leaveVBlankEvent = LeaveVBlankEvent;
            _resetVSyncEvent = ResetVSyncEvent;

            FlushRowMode = new Action<int>[ModeCount]
            {
                FlushRowMode0, FlushRowMode1, FlushRowMode2, FlushRowMode3, FlushRowMode4, FlushRowMode5, FlushRowMode6, FlushRowMode7, 
                FlushRowMode8, FlushRowMode9, FlushRowModeA, FlushRowModeB, FlushRowModeC, FlushRowModeD, FlushRowModeE, FlushRowModeF
            };
        }

		[System.Runtime.Serialization.OnDeserialized]
		public void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
		{
			// the videoservice forgets all of its information on loadstate
			DirtyScreen();
		}

        public override void Initialize()
        {
            _memory = Machine.Memory;
			VideoService = new Services.VideoService();

//#if SILVERLIGHT || WPF
			unchecked
			{
				_colorBlack = (int)0xFF000000; // BGRA
				_colorDarkBlue = (int)0xFF000099;
				_colorDarkGreen = (int)0xFF117722;
				_colorMediumBlue = (int)0xFF0000FF;
				_colorBrown = (int)0xFF885500;
				_colorLightGrey = (int)0xFF99AAAA;
				_colorGreen = (int)0xFF00EE11;
				_colorAquamarine = (int)0xFF55FFAA;
				_colorDeepRed = (int)0xFFFF1111;
				_colorPurple = (int)0xFFDD00DD;
				_colorDarkGrey = (int)0xFF445555;
				_colorLightBlue = (int)0xFF33AAFF;
				_colorOrange = (int)0xFFFF4411;
				_colorPink = (int)0xFFFF9988;
				_colorYellow = (int)0xFFFFFF11;
				_colorWhite = (int)0xFFFFFFFF;
				_colorMonochrome = (int)0xFF00AA00;
			}
//#else
//			_colorBlack = 0xFF000000; // RGBA
//			_colorDarkBlue = 0xFF990000;
//			_colorDarkGreen = 0xFF227711;
//			_colorMediumBlue = 0xFFFF0000;
//			_colorBrown = 0xFF005588;
//			_colorLightGrey = 0xFFAAAA99;
//			_colorGreen = 0xFF11EE00;
//			_colorAquamarine = 0xFFAAFF55;
//			_colorDeepRed = 0xFF1111FF;
//			_colorPurple = 0xFFDD00DD;
//			_colorDarkGrey = 0xFF555544;
//			_colorLightBlue = 0xFFFFAA33;
//			_colorOrange = 0xFF1144FF;
//			_colorPink = 0xFF8899FF;
//			_colorYellow = 0xFF11FFFF;
//			_colorWhite = 0xFFFFFFFF;
//			_colorMonochrome = 0xFF00AA00;
//#endif
            SetPalette();

            IsMonochrome = false;
            ScannerOptions = ScannerOptions.None;

            IsVBlank = true;

            Machine.Events.AddEvent(_cyclesPerVBlankPreset, _leaveVBlankEvent); // align flush events with scanner; assumes vcount preset at start of frame [3-15, 3-16]
            Machine.Events.AddEvent(_cyclesPerVSync, _resetVSyncEvent);
            Machine.Events.AddEvent(_cyclesPerFlash, _inverseTextEvent);
        }

        public override void Reset()
        {
            SetCharSet();
            DirtyScreen();
            FlushScreen();
        }

        public void DirtyCell(int addressOffset)
        {
            _isCellDirty[CellIndex[addressOffset]] = true;
        }

        public void DirtyCellMixed(int addressOffset)
        {
            int cellIndex = CellIndex[addressOffset];
            if (cellIndex < MixedCellIndex)
            {
                _isCellDirty[cellIndex] = true;
            }
        }

        public void DirtyCellMixedText(int addressOffset)
        {
            int cellIndex = CellIndex[addressOffset];
            if (cellIndex >= MixedCellIndex)
            {
                _isCellDirty[cellIndex] = true;
            }
        }

        public void DirtyScreen()
        {
            for (int i = 0; i < Height * CellColumns; i++)
            {
                _isCellDirty[i] = true;
            }
        }

        public void DirtyScreenText()
        {
            if (_memory.IsText)
            {
                for (int i = 0; i < MixedHeight * CellColumns; i++)
                {
                    _isCellDirty[i] = true;
                }
            }
            if (_memory.IsText || _memory.IsMixed)
            {
                for (int i = MixedHeight * CellColumns; i < Height * CellColumns; i++)
                {
                    _isCellDirty[i] = true;
                }
            }
        }

        public int ReadFloatingBus() // [5-40]
        {
            // derive scanner counters from current cycles into frame; assumes hcount and vcount preset at start of frame [3-13, 3-15, 3-16]
            int cycles = _cyclesPerVSync - Machine.Events.FindEvent(_resetVSyncEvent);
            int hClock = cycles % CyclesPerHSync;
            int hCount = (hClock != 0) ? HCountPreset + hClock - 1 : 0;
            int vLine = cycles / CyclesPerHSync;
            int vCount = _vCountPreset + vLine;

            // derive scanner address [5-8]
            int address = ((vCount << 4) & 0x0380) | ((0x0068 + (hCount & 0x0038) + (((vCount >> 1) & 0x0060) | ((vCount >> 3) & 0x0018))) & 0x0078) | (hCount & 0x0007);
            if (_memory.IsHires && !(_memory.IsMixed && ((vCount & 0xA0) == 0xA0))) // hires but not actively mixed [5-13, 5-19]
            {
                address |= (_memory.IsVideoPage2 ? 0x4000 : 0x2000) | ((vCount << 10) & 0x1C00);
            }
            else
            {
                address |= _memory.IsVideoPage2 ? 0x0800 : 0x0400;
                if (((_scannerOptions & ScannerOptions.AppleII) != 0) && (hCount < HCountLeaveHBlank))
                {
                    address |= 0x1000;
                }
            }

            return _memory.Read(address);
        }

        public void SetCharSet()
        {
            _charSet = !_memory.IsCharSetAlternate ? CharSetPrimary : (_memory.Monitor == MonitorType.Standard) ? CharSetSecondaryStandard : CharSetSecondaryEnhanced;
            DirtyScreenText();
        }

        #region Draw Methods
        private void DrawText40(int data, int x, int y)
        {
            int color = IsMonochrome ? ColorMono00 : ColorWhite00;
            int index = _charSet[data] * CharBitmapBytes;
            int inverseMask = (_isTextInversed && !_memory.IsCharSetAlternate && (0x40 <= data) && (data <= 0x7F)) ? 0x7F : 0x00;
            for (int i = 0; i < TextHeight; i++, y++)
            {
                data = CharBitmap[index + i] ^ inverseMask;
                SetPixel(x + 0, y, color | (data & 0x01));
                SetPixel(x + 1, y, color | (data & 0x01));
                SetPixel(x + 2, y, color | (data & 0x02));
                SetPixel(x + 3, y, color | (data & 0x02));
                SetPixel(x + 4, y, color | (data & 0x04));
                SetPixel(x + 5, y, color | (data & 0x04));
                SetPixel(x + 6, y, color | (data & 0x08));
                SetPixel(x + 7, y, color | (data & 0x08));
                SetPixel(x + 8, y, color | (data & 0x10));
                SetPixel(x + 9, y, color | (data & 0x10));
                SetPixel(x + 10, y, color | (data & 0x20));
                SetPixel(x + 11, y, color | (data & 0x20));
                SetPixel(x + 12, y, color | (data & 0x40));
                SetPixel(x + 13, y, color | (data & 0x40));
            }
        }

        private void DrawText80(int data, int x, int y)
        {
            int color = IsMonochrome ? ColorMono00 : ColorWhite00;
            int index = _charSet[data] * CharBitmapBytes;
            int mask = (_isTextInversed && !_memory.IsCharSetAlternate && (0x40 <= data) && (data <= 0x7F)) ? 0x7F : 0x00;
            for (int i = 0; i < TextHeight; i++, y++)
            {
                data = CharBitmap[index + i] ^ mask;
                SetPixel(x + 0, y, color | (data & 0x01));
                SetPixel(x + 1, y, color | (data & 0x02));
                SetPixel(x + 2, y, color | (data & 0x04));
                SetPixel(x + 3, y, color | (data & 0x08));
                SetPixel(x + 4, y, color | (data & 0x10));
                SetPixel(x + 5, y, color | (data & 0x20));
                SetPixel(x + 6, y, color | (data & 0x40));
            }
        }

        private void DrawLores(int data, int x, int y)
        {
            if (IsMonochrome)
            {
                if ((x & 0x02) == 0x02) // odd cell
                {
                    data = ((data << 2) & 0xCC) | ((data >> 2) & 0x33);
                }
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, data & 0x01);
                    SetPixel(x + 1, y, data & 0x02);
                    SetPixel(x + 2, y, data & 0x04);
                    SetPixel(x + 3, y, data & 0x08);
                    SetPixel(x + 4, y, data & 0x01);
                    SetPixel(x + 5, y, data & 0x02);
                    SetPixel(x + 6, y, data & 0x04);
                    SetPixel(x + 7, y, data & 0x08);
                    SetPixel(x + 8, y, data & 0x01);
                    SetPixel(x + 9, y, data & 0x02);
                    SetPixel(x + 10, y, data & 0x04);
                    SetPixel(x + 11, y, data & 0x08);
                    SetPixel(x + 12, y, data & 0x01);
                    SetPixel(x + 13, y, data & 0x02);
                }
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, data & 0x10);
                    SetPixel(x + 1, y, data & 0x20);
                    SetPixel(x + 2, y, data & 0x40);
                    SetPixel(x + 3, y, data & 0x80);
                    SetPixel(x + 4, y, data & 0x10);
                    SetPixel(x + 5, y, data & 0x20);
                    SetPixel(x + 6, y, data & 0x40);
                    SetPixel(x + 7, y, data & 0x80);
                    SetPixel(x + 8, y, data & 0x10);
                    SetPixel(x + 9, y, data & 0x20);
                    SetPixel(x + 10, y, data & 0x40);
                    SetPixel(x + 11, y, data & 0x80);
                    SetPixel(x + 12, y, data & 0x10);
                    SetPixel(x + 13, y, data & 0x20);
                }
            }
            else
            {
                int color = ColorLores[data & 0x0F];
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, color);
                    SetPixel(x + 1, y, color);
                    SetPixel(x + 2, y, color);
                    SetPixel(x + 3, y, color);
                    SetPixel(x + 4, y, color);
                    SetPixel(x + 5, y, color);
                    SetPixel(x + 6, y, color);
                    SetPixel(x + 7, y, color);
                    SetPixel(x + 8, y, color);
                    SetPixel(x + 9, y, color);
                    SetPixel(x + 10, y, color);
                    SetPixel(x + 11, y, color);
                    SetPixel(x + 12, y, color);
                    SetPixel(x + 13, y, color);
                }
                color = ColorLores[data >> 4];
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, color);
                    SetPixel(x + 1, y, color);
                    SetPixel(x + 2, y, color);
                    SetPixel(x + 3, y, color);
                    SetPixel(x + 4, y, color);
                    SetPixel(x + 5, y, color);
                    SetPixel(x + 6, y, color);
                    SetPixel(x + 7, y, color);
                    SetPixel(x + 8, y, color);
                    SetPixel(x + 9, y, color);
                    SetPixel(x + 10, y, color);
                    SetPixel(x + 11, y, color);
                    SetPixel(x + 12, y, color);
                    SetPixel(x + 13, y, color);
                }
            }
        }

        private void Draw7MLores(int data, int x, int y)
        {
            if (IsMonochrome)
            {
                if ((x & 0x02) == 0x02) // odd cell
                {
                    data = ((data << 2) & 0xCC) | ((data >> 2) & 0x33);
                }
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, data & 0x01);
                    SetPixel(x + 1, y, data & 0x01);
                    SetPixel(x + 2, y, data & 0x02);
                    SetPixel(x + 3, y, data & 0x02);
                    SetPixel(x + 4, y, data & 0x04);
                    SetPixel(x + 5, y, data & 0x04);
                    SetPixel(x + 6, y, data & 0x08);
                    SetPixel(x + 7, y, data & 0x08);
                    SetPixel(x + 8, y, data & 0x01);
                    SetPixel(x + 9, y, data & 0x01);
                    SetPixel(x + 10, y, data & 0x02);
                    SetPixel(x + 11, y, data & 0x02);
                    SetPixel(x + 12, y, data & 0x04);
                    SetPixel(x + 13, y, data & 0x04);
                }
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, data & 0x10);
                    SetPixel(x + 1, y, data & 0x10);
                    SetPixel(x + 2, y, data & 0x20);
                    SetPixel(x + 3, y, data & 0x20);
                    SetPixel(x + 4, y, data & 0x40);
                    SetPixel(x + 5, y, data & 0x40);
                    SetPixel(x + 6, y, data & 0x80);
                    SetPixel(x + 7, y, data & 0x80);
                    SetPixel(x + 8, y, data & 0x10);
                    SetPixel(x + 9, y, data & 0x10);
                    SetPixel(x + 10, y, data & 0x20);
                    SetPixel(x + 11, y, data & 0x20);
                    SetPixel(x + 12, y, data & 0x40);
                    SetPixel(x + 13, y, data & 0x40);
                }
            }
            else
            {
                int color = Color7MLores[((x & 0x02) << 3) | (data & 0x0F)];
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, color);
                    SetPixel(x + 1, y, color);
                    SetPixel(x + 2, y, color);
                    SetPixel(x + 3, y, color);
                    SetPixel(x + 4, y, color);
                    SetPixel(x + 5, y, color);
                    SetPixel(x + 6, y, color);
                    SetPixel(x + 7, y, color);
                    SetPixel(x + 8, y, color);
                    SetPixel(x + 9, y, color);
                    SetPixel(x + 10, y, color);
                    SetPixel(x + 11, y, color);
                    SetPixel(x + 12, y, color);
                    SetPixel(x + 13, y, color);
                }
                color = Color7MLores[((x & 0x02) << 3) | (data >> 4)];
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, color);
                    SetPixel(x + 1, y, color);
                    SetPixel(x + 2, y, color);
                    SetPixel(x + 3, y, color);
                    SetPixel(x + 4, y, color);
                    SetPixel(x + 5, y, color);
                    SetPixel(x + 6, y, color);
                    SetPixel(x + 7, y, color);
                    SetPixel(x + 8, y, color);
                    SetPixel(x + 9, y, color);
                    SetPixel(x + 10, y, color);
                    SetPixel(x + 11, y, color);
                    SetPixel(x + 12, y, color);
                    SetPixel(x + 13, y, color);
                }
            }
        }

        private void DrawDLores(int data, int x, int y)
        {
            if (IsMonochrome)
            {
                if ((x & 0x01) == 0x00) // even half cell
                {
                    data = ((data << 1) & 0xEE) | ((data >> 3) & 0x11);
                }
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, data & 0x01);
                    SetPixel(x + 1, y, data & 0x02);
                    SetPixel(x + 2, y, data & 0x04);
                    SetPixel(x + 3, y, data & 0x08);
                    SetPixel(x + 4, y, data & 0x01);
                    SetPixel(x + 5, y, data & 0x02);
                    SetPixel(x + 6, y, data & 0x04);
                }
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, data & 0x10);
                    SetPixel(x + 1, y, data & 0x20);
                    SetPixel(x + 2, y, data & 0x40);
                    SetPixel(x + 3, y, data & 0x80);
                    SetPixel(x + 4, y, data & 0x10);
                    SetPixel(x + 5, y, data & 0x20);
                    SetPixel(x + 6, y, data & 0x40);
                }
            }
            else
            {
                int color = ColorDLores[((x & 0x01) << 4) | (data & 0x0F)];
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, color);
                    SetPixel(x + 1, y, color);
                    SetPixel(x + 2, y, color);
                    SetPixel(x + 3, y, color);
                    SetPixel(x + 4, y, color);
                    SetPixel(x + 5, y, color);
                    SetPixel(x + 6, y, color);
                }
                color = ColorDLores[((x & 0x01) << 4) | (data >> 4)];
                for (int i = 0; i < LoresHeight; i++, y++)
                {
                    SetPixel(x + 0, y, color);
                    SetPixel(x + 1, y, color);
                    SetPixel(x + 2, y, color);
                    SetPixel(x + 3, y, color);
                    SetPixel(x + 4, y, color);
                    SetPixel(x + 5, y, color);
                    SetPixel(x + 6, y, color);
                }
            }
        }

        private void DrawHires(int address, int x, int y)
        {
            if (IsMonochrome)
            {
                int data = _memory.ReadRamMainRegion02BF(address);
                SetPixel(x + 0, y, data & 0x01);
                SetPixel(x + 1, y, data & 0x01);
                SetPixel(x + 2, y, data & 0x02);
                SetPixel(x + 3, y, data & 0x02);
                SetPixel(x + 4, y, data & 0x04);
                SetPixel(x + 5, y, data & 0x04);
                SetPixel(x + 6, y, data & 0x08);
                SetPixel(x + 7, y, data & 0x08);
                SetPixel(x + 8, y, data & 0x10);
                SetPixel(x + 9, y, data & 0x10);
                SetPixel(x + 10, y, data & 0x20);
                SetPixel(x + 11, y, data & 0x20);
                SetPixel(x + 12, y, data & 0x40);
                SetPixel(x + 13, y, data & 0x40);
            }
            else
            {
                //   3                   2                   1                   0
                // 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 
                //
                //                 - - - - - - - - 0 0 0 0 0 0 0 0 + + + + + + + +
                //                 H           1 0 H 6 5 4 3 2 1 0 H           1 0

                int data = _memory.ReadRamMainRegion02BF(address) << 8;
                if (x < Width - CellWidth)
                {
                    data |= _memory.ReadRamMainRegion02BF(address + 1);
                    SetPixel(x + 14, y, ColorHires[((~x & 0x02) << 3) | ((data >> 4) & 0x08) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
                    SetPixel(x + 15, y, ColorHires[((~x & 0x02) << 3) | ((data >> 4) & 0x08) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
                }
                if (x > 0)
                {
                    data |= _memory.ReadRamMainRegion02BF(address - 1) << 16;
                    SetPixel(x - 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 20) & 0x08) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
                    SetPixel(x - 1, y, ColorHires[((~x & 0x02) << 3) | ((data >> 20) & 0x08) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
                }
                SetPixel(x + 0, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
                SetPixel(x + 1, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
                SetPixel(x + 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 8) & 0x07)]);
                SetPixel(x + 3, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 8) & 0x07)]);
                SetPixel(x + 4, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 9) & 0x07)]);
                SetPixel(x + 5, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 9) & 0x07)]);
                SetPixel(x + 6, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 10) & 0x07)]);
                SetPixel(x + 7, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 10) & 0x07)]);
                SetPixel(x + 8, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 11) & 0x07)]);
                SetPixel(x + 9, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data >> 11) & 0x07)]);
                SetPixel(x + 10, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x0F)]);
                SetPixel(x + 11, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x0F)]);
                SetPixel(x + 12, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
                SetPixel(x + 13, y, ColorHires[((x & 0x02) << 3) | ((data >> 12) & 0x08) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
            }
        }

        private void DrawNDHires(int address, int x, int y)
        {
            if (IsMonochrome)
            {
                int data = _memory.ReadRamMainRegion02BF(address);
                SetPixel(x + 0, y, data & 0x01);
                SetPixel(x + 1, y, data & 0x01);
                SetPixel(x + 2, y, data & 0x02);
                SetPixel(x + 3, y, data & 0x02);
                SetPixel(x + 4, y, data & 0x04);
                SetPixel(x + 5, y, data & 0x04);
                SetPixel(x + 6, y, data & 0x08);
                SetPixel(x + 7, y, data & 0x08);
                SetPixel(x + 8, y, data & 0x10);
                SetPixel(x + 9, y, data & 0x10);
                SetPixel(x + 10, y, data & 0x20);
                SetPixel(x + 11, y, data & 0x20);
                SetPixel(x + 12, y, data & 0x40);
                SetPixel(x + 13, y, data & 0x40);
            }
            else
            {
                //   3                   2                   1                   0
                // 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 9 8 7 6 5 4 3 2 1 0 
                //
                //                 - - - - - - - - 0 0 0 0 0 0 0 0 + + + + + + + +
                //                 X           1 0 X 6 5 4 3 2 1 0 X           1 0

                int data = _memory.ReadRamMainRegion02BF(address) << 8;
                if (x < Width - CellWidth)
                {
                    data |= _memory.ReadRamMainRegion02BF(address + 1);
                    SetPixel(x + 14, y, ColorHires[((~x & 0x02) << 3) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
                    SetPixel(x + 15, y, ColorHires[((~x & 0x02) << 3) | ((data << 1) & 0x06) | ((data >> 14) & 0x01)]);
                }
                if (x > 0)
                {
                    data |= _memory.ReadRamMainRegion02BF(address - 1) << 16;
                    SetPixel(x - 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
                    SetPixel(x - 1, y, ColorHires[((~x & 0x02) << 3) | ((data >> 6) & 0x04) | ((data >> 21) & 0x03)]);
                }
                SetPixel(x + 0, y, ColorHires[((x & 0x02) << 3) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
                SetPixel(x + 1, y, ColorHires[((x & 0x02) << 3) | ((data >> 7) & 0x06) | ((data >> 22) & 0x01)]);
                SetPixel(x + 2, y, ColorHires[((~x & 0x02) << 3) | ((data >> 8) & 0x07)]);
                SetPixel(x + 3, y, ColorHires[((~x & 0x02) << 3) | ((data >> 8) & 0x07)]);
                SetPixel(x + 4, y, ColorHires[((x & 0x02) << 3) | ((data >> 9) & 0x07)]);
                SetPixel(x + 5, y, ColorHires[((x & 0x02) << 3) | ((data >> 9) & 0x07)]);
                SetPixel(x + 6, y, ColorHires[((~x & 0x02) << 3) | ((data >> 10) & 0x07)]);
                SetPixel(x + 7, y, ColorHires[((~x & 0x02) << 3) | ((data >> 10) & 0x07)]);
                SetPixel(x + 8, y, ColorHires[((x & 0x02) << 3) | ((data >> 11) & 0x07)]);
                SetPixel(x + 9, y, ColorHires[((x & 0x02) << 3) | ((data >> 11) & 0x07)]);
                SetPixel(x + 10, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x07)]);
                SetPixel(x + 11, y, ColorHires[((~x & 0x02) << 3) | ((data >> 12) & 0x07)]);
                SetPixel(x + 12, y, ColorHires[((x & 0x02) << 3) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
                SetPixel(x + 13, y, ColorHires[((x & 0x02) << 3) | ((data << 2) & 0x04) | ((data >> 13) & 0x03)]);
            }
        }

        private void DrawDHiresA(int address, int x, int y)
        {
            if (IsMonochrome)
            {
                if ((x & 0x2) == 0x00) // even cell
                {
                    int data = ((_memory.ReadRamMainRegion02BF(address) << 7) & 0x80) | (_memory.ReadRamAuxRegion02BF(address) & 0x7F);
                    SetPixel(x + 0, y, data & 0x01);
                    SetPixel(x + 1, y, data & 0x02);
                    SetPixel(x + 2, y, data & 0x04);
                    SetPixel(x + 3, y, data & 0x08);
                    SetPixel(x + 4, y, data & 0x10);
                    SetPixel(x + 5, y, data & 0x20);
                    SetPixel(x + 6, y, data & 0x40);
                    SetPixel(x + 7, y, data & 0x80);
                }
                else
                {
                    int data = ((_memory.ReadRamMainRegion02BF(address) << 9) & 0xE00) | ((_memory.ReadRamAuxRegion02BF(address) << 2) & 0x1FC) |
                        ((_memory.ReadRamMainRegion02BF(address - 1) >> 5) & 0x003);
                    SetPixel(x - 2, y, data & 0x01);
                    SetPixel(x - 1, y, data & 0x02);
                    SetPixel(x + 0, y, data & 0x04);
                    SetPixel(x + 1, y, data & 0x08);
                    SetPixel(x + 2, y, data & 0x10);
                    SetPixel(x + 3, y, data & 0x20);
                    SetPixel(x + 4, y, data & 0x40);
                    SetPixel(x + 5, y, data & 0x80);
                    SetPixel(x + 6, y, (data >> 8) & 0x01);
                    SetPixel(x + 7, y, (data >> 8) & 0x02);
                    SetPixel(x + 8, y, (data >> 8) & 0x04);
                    SetPixel(x + 9, y, (data >> 8) & 0x08);
                }
            }
            else
            {
                if ((x & 0x2) == 0x00) // even cell
                {
                    int data = ((_memory.ReadRamMainRegion02BF(address) << 7) & 0x80) | (_memory.ReadRamAuxRegion02BF(address) & 0x7F);
                    SetPixel(x + 0, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 1, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 2, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 3, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 4, y, ColorDHires0 | (data >> 4));
                    SetPixel(x + 5, y, ColorDHires0 | (data >> 4));
                    SetPixel(x + 6, y, ColorDHires0 | (data >> 4));
                    SetPixel(x + 7, y, ColorDHires0 | (data >> 4));
                }
                else
                {
                    int data = ((_memory.ReadRamMainRegion02BF(address) << 9) & 0xE00) | ((_memory.ReadRamAuxRegion02BF(address) << 2) & 0x1FC) | 
                        ((_memory.ReadRamMainRegion02BF(address - 1) >> 5) & 0x003);
                    SetPixel(x - 2, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x - 1, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 0, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 1, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 2, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 3, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 4, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 5, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 6, y, ColorDHires0 | (data >> 8));
                    SetPixel(x + 7, y, ColorDHires0 | (data >> 8));
                    SetPixel(x + 8, y, ColorDHires0 | (data >> 8));
                    SetPixel(x + 9, y, ColorDHires0 | (data >> 8));
                }
            }
        }

        private void DrawDHiresM(int address, int x, int y)
        {
            if (IsMonochrome)
            {
                if ((x & 0x2) == 0x02) // odd cell
                {
                    int data = ((_memory.ReadRamMainRegion02BF(address) << 1) & 0xFE) | ((_memory.ReadRamAuxRegion02BF(address) >> 6) & 0x01);
                    SetPixel(x + 6, y, data & 0x01);
                    SetPixel(x + 7, y, data & 0x02);
                    SetPixel(x + 8, y, data & 0x04);
                    SetPixel(x + 9, y, data & 0x08);
                    SetPixel(x + 10, y, data & 0x10);
                    SetPixel(x + 11, y, data & 0x20);
                    SetPixel(x + 12, y, data & 0x40);
                    SetPixel(x + 13, y, data & 0x80);
                }
                else
                {
                    int data = ((_memory.ReadRamAuxRegion02BF(address + 1) << 10) & 0xC00) | ((_memory.ReadRamMainRegion02BF(address) << 3) & 0x3F8) |
                        ((_memory.ReadRamAuxRegion02BF(address) >> 4) & 0x007);
                    SetPixel(x + 4, y, data & 0x01);
                    SetPixel(x + 5, y, data & 0x02);
                    SetPixel(x + 6, y, data & 0x04);
                    SetPixel(x + 7, y, data & 0x08);
                    SetPixel(x + 8, y, data & 0x10);
                    SetPixel(x + 9, y, data & 0x20);
                    SetPixel(x + 10, y, data & 0x40);
                    SetPixel(x + 11, y, data & 0x80);
                    SetPixel(x + 12, y, (data >> 8) & 0x01);
                    SetPixel(x + 13, y, (data >> 8) & 0x02);
                    SetPixel(x + 14, y, (data >> 8) & 0x04);
                    SetPixel(x + 15, y, (data >> 8) & 0x08);
                }
            }
            else
            {
                if ((x & 0x2) == 0x02) // odd cell
                {
                    int data = ((_memory.ReadRamMainRegion02BF(address) << 1) & 0xFE) | ((_memory.ReadRamAuxRegion02BF(address) >> 6) & 0x01);
                    SetPixel(x + 6, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 7, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 8, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 9, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 10, y, ColorDHires0 | (data >> 4));
                    SetPixel(x + 11, y, ColorDHires0 | (data >> 4));
                    SetPixel(x + 12, y, ColorDHires0 | (data >> 4));
                    SetPixel(x + 13, y, ColorDHires0 | (data >> 4));
                }
                else
                {
                    int data = ((_memory.ReadRamAuxRegion02BF(address + 1) << 10) & 0xC00) | ((_memory.ReadRamMainRegion02BF(address) << 3) & 0x3F8) |
                        ((_memory.ReadRamAuxRegion02BF(address) >> 4) & 0x007);
                    SetPixel(x + 4, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 5, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 6, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 7, y, ColorDHires0 | (data & 0x0F));
                    SetPixel(x + 8, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 9, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 10, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 11, y, ColorDHires0 | ((data >> 4) & 0x0F));
                    SetPixel(x + 12, y, ColorDHires0 | (data >> 8));
                    SetPixel(x + 13, y, ColorDHires0 | (data >> 8));
                    SetPixel(x + 14, y, ColorDHires0 | (data >> 8));
                    SetPixel(x + 15, y, ColorDHires0 | (data >> 8));
                }
            }
        }
        #endregion

        #region Flush Methods
        private void FlushRowMode0(int y)
        {
            int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
            for (int x = 0; x < CellColumns; x++)
            {
                if (_isCellDirty[CellColumns * y + x])
                {
                    _isCellDirty[CellColumns * y + x] = false;
                    DrawLores(_memory.ReadRamMainRegion02BF(address + x), CellWidth * x, y); // lores
                }
            }
        }

        private void FlushRowMode1(int y)
        {
            int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
            for (int x = 0; x < CellColumns; x++)
            {
                if (_isCellDirty[CellColumns * y + x])
                {
                    _isCellDirty[CellColumns * y + x] = false;
                    DrawText40(_memory.ReadRamMainRegion02BF(address + x), CellWidth * x, y); // text40
                }
            }
        }

        private void FlushRowMode2(int y)
        {
            int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
            for (int x = 0; x < 2 * CellColumns; x += 2)
            {
                if (_isCellDirty[CellColumns * y + x / 2])
                {
                    _isCellDirty[CellColumns * y + x / 2] = false;
                    DrawText80(_memory.ReadRamAuxRegion02BF(address + x / 2), CellWidth / 2 * (x + 0), y); // text80
                    DrawText80(_memory.ReadRamMainRegion02BF(address + x / 2), CellWidth / 2 * (x + 1), y);
                }
            }
        }

        private void FlushRowMode3(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowMode0(y); // lores
            }
            else
            {
                FlushRowMode1(y); // text40
            }
        }

        private void FlushRowMode4(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowMode0(y); // lores
            }
            else
            {
                FlushRowMode2(y); // text80
            }
        }

        private void FlushRowMode5(int y)
        {
            int address = _memory.IsVideoPage2 ? 0x4000 : 0x2000;
            for (int i = 0; i < CellHeight; i++, y++)
            {
                for (int x = 0; x < CellColumns; x++)
                {
                    if (_isCellDirty[CellColumns * y + x])
                    {
                        _isCellDirty[CellColumns * y + x] = false;
                        DrawHires(address + AddressOffset[y] + x, CellWidth * x, y); // hires
                    }
                }
            }
        }

        private void FlushRowMode6(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowMode5(y); // hires
            }
            else
            {
                FlushRowMode1(y); // text40
            }
        }

        private void FlushRowMode7(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowMode5(y); // hires
            }
            else
            {
                FlushRowMode2(y); // text80
            }
        }

        private void FlushRowMode8(int y)
        {
            int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
            for (int x = 0; x < CellColumns; x++)
            {
                if (_isCellDirty[CellColumns * y + x])
                {
                    _isCellDirty[CellColumns * y + x] = false;
                    Draw7MLores(_memory.ReadRamMainRegion02BF(address + x), CellWidth * x, y); // 7mlores
                }
            }
        }

        private void FlushRowMode9(int y)
        {
            int address = (_memory.IsVideoPage2 ? 0x0800 : 0x0400) + AddressOffset[y];
            for (int x = 0; x < 2 * CellColumns; x += 2)
            {
                if (_isCellDirty[CellColumns * y + x / 2])
                {
                    _isCellDirty[CellColumns * y + x / 2] = false;
                    DrawDLores(_memory.ReadRamAuxRegion02BF(address + x / 2), CellWidth / 2 * (x + 0), y); // dlores
                    DrawDLores(_memory.ReadRamMainRegion02BF(address + x / 2), CellWidth / 2 * (x + 1), y);
                }
            }
        }

        private void FlushRowModeA(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowMode8(y); // 7mlores
            }
            else
            {
                FlushRowMode1(y); // text40
            }
        }

        private void FlushRowModeB(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowMode9(y); // dlores
            }
            else
            {
                FlushRowMode2(y); // text80
            }
        }

        private void FlushRowModeC(int y)
        {
            int address = _memory.IsVideoPage2 ? 0x4000 : 0x2000;
            for (int i = 0; i < CellHeight; i++, y++)
            {
                for (int x = 0; x < CellColumns; x++)
                {
                    if (_isCellDirty[CellColumns * y + x])
                    {
                        _isCellDirty[CellColumns * y + x] = false;
                        DrawNDHires(address + AddressOffset[y] + x, CellWidth * x, y); // ndhires
                    }
                }
            }
        }

        private void FlushRowModeD(int y)
        {
            int address = _memory.IsVideoPage2 ? 0x4000 : 0x2000;
            for (int i = 0; i < CellHeight; i++, y++)
            {
                for (int x = 0; x < CellColumns; x++)
                {
                    if (_isCellDirty[CellColumns * y + x])
                    {
                        _isCellDirty[CellColumns * y + x] = false;
                        DrawDHiresA(address + AddressOffset[y] + x, CellWidth * x, y); // dhires
                        DrawDHiresM(address + AddressOffset[y] + x, CellWidth * x, y);
                    }
                }
            }
        }

        private void FlushRowModeE(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowModeC(y); // ndhires
            }
            else
            {
                FlushRowMode1(y); // text40
            }
        }

        private void FlushRowModeF(int y)
        {
            if (y < MixedHeight)
            {
                FlushRowModeD(y); // dhires
            }
            else
            {
                FlushRowMode2(y); // text80
            }
        }
        #endregion

        private void FlushRowEvent()
        {
            int y = (_cyclesPerVSync - _cyclesPerVBlankPreset - Machine.Events.FindEvent(_resetVSyncEvent)) / CyclesPerHSync;

            FlushRowMode[_memory.VideoMode](y - CellHeight); // in arrears

            if (y < Height)
            {
                Machine.Events.AddEvent(CyclesPerFlush, _flushRowEvent);
            }
            else
            {
                IsVBlank = true;

                Machine.Events.AddEvent(_cyclesPerVBlank, _leaveVBlankEvent);
            }
        }

        private void FlushScreen()
        {
            var flushRowMode = FlushRowMode[_memory.VideoMode];

            for (int y = 0; y < Height; y += CellHeight)
            {
                flushRowMode(y);
            }
        }

        private void InverseTextEvent()
        {
            _isTextInversed = !_isTextInversed;

            DirtyScreenText();

            Machine.Events.AddEvent(_cyclesPerFlash, _inverseTextEvent);
        }

        private void LeaveVBlankEvent()
        {
            IsVBlank = false;

            Machine.Events.AddEvent(CyclesPerFlush, _flushRowEvent);
        }

        private void ResetVSyncEvent()
        {
            Machine.Events.AddEvent(_cyclesPerVSync, _resetVSyncEvent);
        }

        private void SetPalette()
        {
            _colorPalette[ColorMono00] = _colorBlack;
            _colorPalette[ColorMono01] = _colorMonochrome;
            _colorPalette[ColorMono02] = _colorMonochrome;
            _colorPalette[ColorMono04] = _colorMonochrome;
            _colorPalette[ColorMono08] = _colorMonochrome;
            _colorPalette[ColorMono10] = _colorMonochrome;
            _colorPalette[ColorMono20] = _colorMonochrome;
            _colorPalette[ColorMono40] = _colorMonochrome;
            _colorPalette[ColorMono80] = _colorMonochrome;

            _colorPalette[ColorWhite00] = _colorBlack;
            _colorPalette[ColorWhite01] = _colorWhite;
            _colorPalette[ColorWhite02] = _colorWhite;
            _colorPalette[ColorWhite04] = _colorWhite;
            _colorPalette[ColorWhite08] = _colorWhite;
            _colorPalette[ColorWhite10] = _colorWhite;
            _colorPalette[ColorWhite20] = _colorWhite;
            _colorPalette[ColorWhite40] = _colorWhite;
            _colorPalette[ColorWhite80] = _colorWhite;

            _colorPalette[ColorDHires0] = _colorBlack;
            _colorPalette[ColorDHires1] = _colorDarkBlue;
            _colorPalette[ColorDHires2] = _colorDarkGreen;
            _colorPalette[ColorDHires3] = _colorMediumBlue;
            _colorPalette[ColorDHires4] = _colorBrown;
            _colorPalette[ColorDHires5] = _colorLightGrey;
            _colorPalette[ColorDHires6] = _colorGreen;
            _colorPalette[ColorDHires7] = _colorAquamarine;
            _colorPalette[ColorDHires8] = _colorDeepRed;
            _colorPalette[ColorDHires9] = _colorPurple;
            _colorPalette[ColorDHiresA] = _colorDarkGrey;
            _colorPalette[ColorDHiresB] = _colorLightBlue;
            _colorPalette[ColorDHiresC] = _colorOrange;
            _colorPalette[ColorDHiresD] = _colorPink;
            _colorPalette[ColorDHiresE] = _colorYellow;
            _colorPalette[ColorDHiresF] = _colorWhite;

            DirtyScreen();
        }

        private void SetPixel(int x, int y, int color)
        {
            VideoService.SetPixel(x, 2 * y, _colorPalette[color]);
        }

        private void SetScanner()
        {
            if ((_scannerOptions & ScannerOptions.Pal) != 0)
            {
                _vCountPreset = VCountPresetPal;
                _vLineLeaveVBlank = VLineLeaveVBlankPal;
            }
            else
            {
                _vCountPreset = VCountPresetNtsc;
                _vLineLeaveVBlank = VLineLeaveVBlankNtsc;
            }

            _cyclesPerVBlank = (_vLineLeaveVBlank - VLineEnterVBlank) * CyclesPerHSync;
            _cyclesPerVBlankPreset = (_vLineLeaveVBlank - VLineTriggerPreset) * CyclesPerHSync; // cycles during vblank after vcount preset [3-15, 3-16]
            _cyclesPerVSync = _vLineLeaveVBlank * CyclesPerHSync;
            _cyclesPerFlash = VSyncsPerFlash * _cyclesPerVSync;
        }

		[Newtonsoft.Json.JsonIgnore]
		public int ColorBlack { get { return _colorBlack; } set { _colorBlack = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorDarkBlue { get { return _colorDarkBlue; } set { _colorDarkBlue = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorDarkGreen { get { return _colorDarkGreen; } set { _colorDarkGreen = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorMediumBlue { get { return _colorMediumBlue; } set { _colorMediumBlue = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorBrown { get { return _colorBrown; } set { _colorBrown = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorLightGrey { get { return _colorLightGrey; } set { _colorLightGrey = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorGreen { get { return _colorGreen; } set { _colorGreen = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorAquamarine { get { return _colorAquamarine; } set { _colorAquamarine = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorDeepRed { get { return _colorDeepRed; } set { _colorDeepRed = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorPurple { get { return _colorPurple; } set { _colorPurple = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorDarkGrey { get { return _colorDarkGrey; } set { _colorDarkGrey = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorLightBlue { get { return _colorLightBlue; } set { _colorLightBlue = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorOrange { get { return _colorOrange; } set { _colorOrange = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorPink { get { return _colorPink; } set { _colorPink = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorYellow { get { return _colorYellow; } set { _colorYellow = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorWhite { get { return _colorWhite; } set { _colorWhite = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
		public int ColorMonochrome { get { return _colorMonochrome; } set { _colorMonochrome = value; SetPalette(); } }
		[Newtonsoft.Json.JsonIgnore]
        public bool IsMonochrome { get { return _isMonochrome; } set { _isMonochrome = value; DirtyScreen(); } }
        public ScannerOptions ScannerOptions { get { return _scannerOptions; } set { _scannerOptions = value; SetScanner(); } }

        public bool IsVBlank { get; private set; }

        private Action _flushRowEvent;
        private Action _inverseTextEvent;
        private Action _leaveVBlankEvent;
        private Action _resetVSyncEvent;

        private Memory _memory;
		public VideoService VideoService { get; private set; }

        private int _colorBlack;
        private int _colorDarkBlue;
        private int _colorDarkGreen;
        private int _colorMediumBlue;
        private int _colorBrown;
        private int _colorLightGrey;
        private int _colorGreen;
        private int _colorAquamarine;
        private int _colorDeepRed;
        private int _colorPurple;
        private int _colorDarkGrey;
        private int _colorLightBlue;
        private int _colorOrange;
        private int _colorPink;
        private int _colorYellow;
        private int _colorWhite;
        private int _colorMonochrome;
		[Newtonsoft.Json.JsonIgnore] // not sync relevant
        private bool _isMonochrome;
        private bool _isTextInversed;
        private ScannerOptions _scannerOptions;
        private int _cyclesPerVBlank;
        private int _cyclesPerVBlankPreset;
        private int _cyclesPerVSync;
        private int _cyclesPerFlash;
        private int _vCountPreset;
        private int _vLineLeaveVBlank;

        private ushort[] _charSet;
        private int[] _colorPalette = new int[ColorPaletteCount];

		[Newtonsoft.Json.JsonIgnore] // everything is automatically dirtied on load, so no need to save
        private bool[] _isCellDirty = new bool[Height * CellColumns + 1]; // includes sentinel
    }
}
