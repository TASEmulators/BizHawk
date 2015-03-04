using System;
using System.IO;
using Jellyfish.Virtu.Services;

namespace Jellyfish.Virtu
{
    [Flags]
    public enum ScannerOptions { None = 0x0, AppleII = 0x1, Pal = 0x2 } // defaults to AppleIIe, Ntsc

    public sealed partial class Video : MachineComponent
    {
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

        public override void Initialize()
        {
            _memory = Machine.Memory;
            _videoService = Machine.Services.GetService<VideoService>();

#if SILVERLIGHT || WPF
            _colorBlack = 0xFF000000; // BGRA
            _colorDarkBlue = 0xFF000099;
            _colorDarkGreen = 0xFF117722;
            _colorMediumBlue = 0xFF0000FF;
            _colorBrown = 0xFF885500;
            _colorLightGrey = 0xFF99AAAA;
            _colorGreen = 0xFF00EE11;
            _colorAquamarine = 0xFF55FFAA;
            _colorDeepRed = 0xFFFF1111;
            _colorPurple = 0xFFDD00DD;
            _colorDarkGrey = 0xFF445555;
            _colorLightBlue = 0xFF33AAFF;
            _colorOrange = 0xFFFF4411;
            _colorPink = 0xFFFF9988;
            _colorYellow = 0xFFFFFF11;
            _colorWhite = 0xFFFFFFFF;
            _colorMonochrome = 0xFF00AA00;
#else
            _colorBlack = 0xFF000000; // RGBA
            _colorDarkBlue = 0xFF990000;
            _colorDarkGreen = 0xFF227711;
            _colorMediumBlue = 0xFFFF0000;
            _colorBrown = 0xFF005588;
            _colorLightGrey = 0xFFAAAA99;
            _colorGreen = 0xFF11EE00;
            _colorAquamarine = 0xFFAAFF55;
            _colorDeepRed = 0xFF1111FF;
            _colorPurple = 0xFFDD00DD;
            _colorDarkGrey = 0xFF555544;
            _colorLightBlue = 0xFFFFAA33;
            _colorOrange = 0xFF1144FF;
            _colorPink = 0xFF8899FF;
            _colorYellow = 0xFF11FFFF;
            _colorWhite = 0xFFFFFFFF;
            _colorMonochrome = 0xFF00AA00;
#endif
            SetPalette();

            IsFullScreen = false;
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

        public override void LoadState(BinaryReader reader, Version version)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            _colorBlack = reader.ReadUInt32();
            _colorDarkBlue = reader.ReadUInt32();
            _colorDarkGreen = reader.ReadUInt32();
            _colorMediumBlue = reader.ReadUInt32();
            _colorBrown = reader.ReadUInt32();
            _colorLightGrey = reader.ReadUInt32();
            _colorGreen = reader.ReadUInt32();
            _colorAquamarine = reader.ReadUInt32();
            _colorDeepRed = reader.ReadUInt32();
            _colorPurple = reader.ReadUInt32();
            _colorDarkGrey = reader.ReadUInt32();
            _colorLightBlue = reader.ReadUInt32();
            _colorOrange = reader.ReadUInt32();
            _colorPink = reader.ReadUInt32();
            _colorYellow = reader.ReadUInt32();
            _colorWhite = reader.ReadUInt32();
            _colorMonochrome = reader.ReadUInt32();
            SetPalette();

            IsFullScreen = reader.ReadBoolean();
            IsMonochrome = reader.ReadBoolean();
            ScannerOptions = (ScannerOptions)reader.ReadInt32();

            SetCharSet();
            DirtyScreen();
            FlushScreen();
        }

        public override void SaveState(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.Write(_colorBlack);
            writer.Write(_colorDarkBlue);
            writer.Write(_colorDarkGreen);
            writer.Write(_colorMediumBlue);
            writer.Write(_colorBrown);
            writer.Write(_colorLightGrey);
            writer.Write(_colorGreen);
            writer.Write(_colorAquamarine);
            writer.Write(_colorDeepRed);
            writer.Write(_colorPurple);
            writer.Write(_colorDarkGrey);
            writer.Write(_colorLightBlue);
            writer.Write(_colorOrange);
            writer.Write(_colorPink);
            writer.Write(_colorYellow);
            writer.Write(_colorWhite);
            writer.Write(_colorMonochrome);

            writer.Write(IsFullScreen);
            writer.Write(IsMonochrome);
            writer.Write((int)ScannerOptions);
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
            _videoService.SetPixel(x, 2 * y, _colorPalette[color]);
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

        public uint ColorBlack { get { return _colorBlack; } set { _colorBlack = value; SetPalette(); } }
        public uint ColorDarkBlue { get { return _colorDarkBlue; } set { _colorDarkBlue = value; SetPalette(); } }
        public uint ColorDarkGreen { get { return _colorDarkGreen; } set { _colorDarkGreen = value; SetPalette(); } }
        public uint ColorMediumBlue { get { return _colorMediumBlue; } set { _colorMediumBlue = value; SetPalette(); } }
        public uint ColorBrown { get { return _colorBrown; } set { _colorBrown = value; SetPalette(); } }
        public uint ColorLightGrey { get { return _colorLightGrey; } set { _colorLightGrey = value; SetPalette(); } }
        public uint ColorGreen { get { return _colorGreen; } set { _colorGreen = value; SetPalette(); } }
        public uint ColorAquamarine { get { return _colorAquamarine; } set { _colorAquamarine = value; SetPalette(); } }
        public uint ColorDeepRed { get { return _colorDeepRed; } set { _colorDeepRed = value; SetPalette(); } }
        public uint ColorPurple { get { return _colorPurple; } set { _colorPurple = value; SetPalette(); } }
        public uint ColorDarkGrey { get { return _colorDarkGrey; } set { _colorDarkGrey = value; SetPalette(); } }
        public uint ColorLightBlue { get { return _colorLightBlue; } set { _colorLightBlue = value; SetPalette(); } }
        public uint ColorOrange { get { return _colorOrange; } set { _colorOrange = value; SetPalette(); } }
        public uint ColorPink { get { return _colorPink; } set { _colorPink = value; SetPalette(); } }
        public uint ColorYellow { get { return _colorYellow; } set { _colorYellow = value; SetPalette(); } }
        public uint ColorWhite { get { return _colorWhite; } set { _colorWhite = value; SetPalette(); } }
        public uint ColorMonochrome { get { return _colorMonochrome; } set { _colorMonochrome = value; SetPalette(); } }

        public bool IsFullScreen { get { return _isFullScreen; } set { _isFullScreen = value; _videoService.SetFullScreen(_isFullScreen); } }
        public bool IsMonochrome { get { return _isMonochrome; } set { _isMonochrome = value; DirtyScreen(); } }
        public ScannerOptions ScannerOptions { get { return _scannerOptions; } set { _scannerOptions = value; SetScanner(); } }

        public bool IsVBlank { get; private set; }

        private Action _flushRowEvent;
        private Action _inverseTextEvent;
        private Action _leaveVBlankEvent;
        private Action _resetVSyncEvent;

        private Memory _memory;
        private VideoService _videoService;

        private uint _colorBlack;
        private uint _colorDarkBlue;
        private uint _colorDarkGreen;
        private uint _colorMediumBlue;
        private uint _colorBrown;
        private uint _colorLightGrey;
        private uint _colorGreen;
        private uint _colorAquamarine;
        private uint _colorDeepRed;
        private uint _colorPurple;
        private uint _colorDarkGrey;
        private uint _colorLightBlue;
        private uint _colorOrange;
        private uint _colorPink;
        private uint _colorYellow;
        private uint _colorWhite;
        private uint _colorMonochrome;
        private bool _isFullScreen;
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
        private uint[] _colorPalette = new uint[ColorPaletteCount];
        private bool[] _isCellDirty = new bool[Height * CellColumns + 1]; // includes sentinel
    }
}
