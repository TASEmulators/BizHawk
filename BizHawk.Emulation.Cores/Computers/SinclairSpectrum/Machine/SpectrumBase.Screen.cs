using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /*
     *  Much of the SCREEN implementation has been taken from: https://github.com/Dotneteer/spectnetide
     *  
     *  MIT License

        Copyright (c) 2017 Istvan Novak

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.
     */

        /*

    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Screen *
    /// </summary>
    public abstract partial class SpectrumBase : IVideoProvider
    {
        #region State

        /// <summary>
        /// The main screen buffer
        /// </summary>
        protected int[] _frameBuffer;

        /// <summary>
        /// Pixel and attribute info stored while rendering the screen
        /// </summary>
        protected byte _pixelByte1;
        protected byte _pixelByte2;
        protected byte _attrByte1;
        protected byte _attrByte2;
        protected int _xPos;
        protected int _yPos;
        protected int[] _flashOffColors;
        protected int[] _flashOnColors;
        protected ScreenRenderingCycle[] _renderingCycleTable;
        protected bool _flashPhase;

        #endregion

        #region Statics

        /// <summary>
        /// The standard ULA palette
        /// </summary>
        private static readonly int[] ULAPalette =
        {
            Colors.ARGB(0x00, 0x00, 0x00), // Black
            Colors.ARGB(0x00, 0x00, 0xD7), // Blue
            Colors.ARGB(0xD7, 0x00, 0x00), // Red
            Colors.ARGB(0xD7, 0x00, 0xD7), // Magenta
            Colors.ARGB(0x00, 0xD7, 0x00), // Green
            Colors.ARGB(0x00, 0xD7, 0xD7), // Cyan
            Colors.ARGB(0xD7, 0xD7, 0x00), // Yellow
            Colors.ARGB(0xD7, 0xD7, 0xD7), // White
            Colors.ARGB(0x00, 0x00, 0x00), // Bright Black
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White
        };

        #endregion

        #region ScreenConfig

        /// <summary>
        /// The number of displayed pixels in a display row
        /// </summary>
        protected int DisplayWidth = 256;

        /// <summary>
        /// Number of display lines
        /// </summary>
        protected int DisplayLines = 192;

        /// <summary>
        /// The number of frames after the flash is toggled
        /// </summary>
        protected int FlashToggleFrames = 25;

        /// <summary>
        /// Number of lines used for vertical sync
        /// </summary>
        protected int VerticalSyncLines = 8;

        /// <summary>
        /// The number of top border lines that are not visible
        /// when rendering the screen
        /// </summary>
        protected int NonVisibleBorderTopLines = 8;

        /// <summary>
        /// The number of border lines before the display
        /// </summary>
        protected int BorderTopLines = 48;

        /// <summary>
        /// The number of border lines after the display
        /// </summary>
        protected int BorderBottomLines = 48;

        /// <summary>
        /// The number of bottom border lines that are not visible
        /// when rendering the screen
        /// </summary>
        protected int NonVisibleBorderBottomLines = 8;

        /// <summary>
        /// The total number of lines in the screen
        /// </summary>
        protected int ScreenLines;

        /// <summary>
        /// The first screen line that contains the top left display pixel
        /// </summary>
        protected int FirstDisplayLine;

        /// <summary>
        /// The last screen line that contains the bottom right display pixel
        /// </summary>
        protected int LastDisplayLine;

        /// <summary>
        /// The number of border pixels to the left of the display
        /// </summary>
        protected int BorderLeftPixels = 48;

        /// <summary>
        /// The number of border pixels to the right of the display
        /// </summary>
        protected int BorderRightPixels = 48;
        
        /// <summary>
        /// The total width of the screen in pixels
        /// </summary>
        protected int ScreenWidth;

        /// <summary>
        /// Horizontal blanking time (HSync+blanking).
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int HorizontalBlankingTime = 40;

        /// <summary>
        /// The time of displaying left part of the border.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int BorderLeftTime = 24;

        /// <summary>
        /// The time of displaying a pixel row.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int DisplayLineTime = 128;

        /// <summary>
        /// The time of displaying right part of the border.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int BorderRightTime  = 24;

        /// <summary>
        /// The time used to render the nonvisible right part of the border.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int NonVisibleBorderRightTime = 8;

        /// <summary>
        /// The time of displaying a full screen line.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int ScreenLineTime;

        /// <summary>
        /// The time the data of a particular pixel should be prefetched
        /// before displaying it.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int PixelDataPrefetchTime = 2;

        /// <summary>
        /// The time the data of a particular pixel attribute should be prefetched
        /// before displaying it.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int AttributeDataPrefetchTime = 1;

        /// <summary>
        /// The tact within the line that should display the first pixel.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int FirstPixelCycleInLine;

        /// <summary>
        /// The tact in which the top left pixel should be displayed.
        /// Given in Z80 clock cycles.
        /// </summary>
        protected int FirstDisplayPixelCycle;

        /// <summary>
        /// The tact in which the top left screen pixel (border) should be displayed
        /// </summary>
        protected int FirstScreenPixelCycle;

        /// <summary>
        /// Defines the number of Z80 clock cycles used for the full rendering
        /// of the screen.
        /// </summary>
        public int UlaFrameCycleCount;

		/// <summary>
        /// The last rendered ULA cycle
        /// </summary>
        public int LastRenderedULACycle;


        /// <summary>
        /// This structure defines information related to a particular T-State
        /// (cycle) of ULA screen rendering
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct ScreenRenderingCycle
        {
            /// <summary>
            /// Tha rendering phase to be applied for the particular tact
            /// </summary>
            [FieldOffset(0)]
            public ScreenRenderingPhase Phase;

            /// <summary>
            /// Display memory contention delay
            /// </summary>
            [FieldOffset(1)]
            public byte ContentionDelay;

            /// <summary>
            /// Display memory address used in the particular tact
            /// </summary>
            [FieldOffset(2)]
            public ushort PixelByteToFetchAddress;

            /// <summary>
            /// Display memory address used in the particular tact
            /// </summary>
            [FieldOffset(4)]
            public ushort AttributeToFetchAddress;

            /// <summary>
            /// Pixel X coordinate
            /// </summary>
            [FieldOffset(6)]
            public ushort XPos;

            /// <summary>
            /// Pixel Y coordinate
            /// </summary>
            [FieldOffset(8)]
            public ushort YPos;
        }

        /// <summary>
        /// This enumeration defines the particular phases of ULA rendering
        /// </summary>
        public enum ScreenRenderingPhase : byte
        {
            /// <summary>
            /// The ULA does not do any rendering
            /// </summary>
            None,

            /// <summary>
            /// The ULA simple sets the border color to display the current pixel.
            /// </summary>
            Border,

            /// <summary>
            /// The ULA sets the border color to display the current pixel. It
            /// prepares to display the fist pixel in the row with prefetching the
            /// corresponding byte from the display memory.
            /// </summary>
            BorderAndFetchPixelByte,

            /// <summary>
            /// The ULA sets the border color to display the current pixel. It has
            /// already fetched the 8 pixel bits to display. It carries on
            /// preparing to display the fist pixel in the row with prefetching the
            /// corresponding attribute byte from the display memory.
            /// </summary>
            BorderAndFetchPixelAttribute,

            /// <summary>
            /// The ULA displays the next two pixels of Byte1 sequentially during a
            /// single Z80 clock cycle.
            /// </summary>
            DisplayByte1,

            /// <summary>
            /// The ULA displays the next two pixels of Byte1 sequentially during a
            /// single Z80 clock cycle. It prepares to display the pixels of the next
            /// byte in the row with prefetching the corresponding byte from the
            /// display memory.
            /// </summary>
            DisplayByte1AndFetchByte2,

            /// <summary>
            /// The ULA displays the next two pixels of Byte1 sequentially during a
            /// single Z80 clock cycle. It prepares to display the pixels of the next
            /// byte in the row with prefetching the corresponding attribute from the
            /// display memory.
            /// </summary>
            DisplayByte1AndFetchAttribute2,

            /// <summary>
            /// The ULA displays the next two pixels of Byte2 sequentially during a
            /// single Z80 clock cycle.
            /// </summary>
            DisplayByte2,

            /// <summary>
            /// The ULA displays the next two pixels of Byte2 sequentially during a
            /// single Z80 clock cycle. It prepares to display the pixels of the next
            /// byte in the row with prefetching the corresponding byte from the
            /// display memory.
            /// </summary>
            DisplayByte2AndFetchByte1,

            /// <summary>
            /// The ULA displays the next two pixels of Byte2 sequentially during a
            /// single Z80 clock cycle. It prepares to display the pixels of the next
            /// byte in the row with prefetching the corresponding attribute from the
            /// display memory.
            /// </summary>
            DisplayByte2AndFetchAttribute1
        }

        #endregion

        #region Border

        private int _borderColour;

        /// <summary>
        /// Gets or sets the ULA border color
        /// </summary>
        public int BorderColour
        {
            get { return _borderColour; }
            set { _borderColour = value & 0x07; }
        }

		protected virtual void ResetBorder()
        {
            BorderColour = 0;
        }

        #endregion

        #region Screen Methods

        /// <summary>
        /// ULA renders the screen between two specified T-States (cycles)
        /// </summary>
        /// <param name="fromCycle"></param>
        /// <param name="toCycle"></param>
        public void RenderScreen(int fromCycle, int toCycle)
        {
            // Adjust cycle boundaries
            fromCycle = fromCycle % UlaFrameCycleCount;
            toCycle = toCycle % UlaFrameCycleCount;

            // Do rendering action for cycles based on the rendering phase
            for (int curr = fromCycle; curr <= toCycle; curr++)
            {
                var ulaCycle = _renderingCycleTable[curr];
                _xPos = ulaCycle.XPos;
                _yPos = ulaCycle.YPos;

                switch (ulaCycle.Phase)
                {
                    case ScreenRenderingPhase.None:
                        // --- Invisible screen area, nothing to do
                        break;

                    case ScreenRenderingPhase.Border:
                        // --- Fetch the border color from ULA and set the corresponding border pixels
                        SetPixels(BorderColour, BorderColour);
                        break;

                    case ScreenRenderingPhase.BorderAndFetchPixelByte:
                        // --- Fetch the border color from ULA and set the corresponding border pixels
                        SetPixels(BorderColour, BorderColour);
                        // --- Obtain the future pixel byte
                        _pixelByte1 = FetchScreenMemory(ulaCycle.PixelByteToFetchAddress);
                        break;

                    case ScreenRenderingPhase.BorderAndFetchPixelAttribute:
                        // --- Fetch the border color from ULA and set the corresponding border pixels
                        SetPixels(BorderColour, BorderColour);
                        // --- Obtain the future attribute byte
                        _attrByte1 = FetchScreenMemory(ulaCycle.AttributeToFetchAddress);
                        break;

                    case ScreenRenderingPhase.DisplayByte1:
                        // --- Display bit 7 and 6 according to the corresponding color
                        SetPixels(
                            GetColor(_pixelByte1 & 0x80, _attrByte1),
                            GetColor(_pixelByte1 & 0x40, _attrByte1));
                        // --- Shift in the subsequent bits
                        _pixelByte1 <<= 2;
                        break;

                    case ScreenRenderingPhase.DisplayByte1AndFetchByte2:
                        // --- Display bit 7 and 6 according to the corresponding color
                        SetPixels(
                            GetColor(_pixelByte1 & 0x80, _attrByte1),
                            GetColor(_pixelByte1 & 0x40, _attrByte1));
                        // --- Shift in the subsequent bits
                        _pixelByte1 <<= 2;
                        // --- Obtain the next pixel byte
                        _pixelByte2 = FetchScreenMemory(ulaCycle.PixelByteToFetchAddress);
                        break;

                    case ScreenRenderingPhase.DisplayByte1AndFetchAttribute2:
                        // --- Display bit 7 and 6 according to the corresponding color
                        SetPixels(
                            GetColor(_pixelByte1 & 0x80, _attrByte1),
                            GetColor(_pixelByte1 & 0x40, _attrByte1));
                        // --- Shift in the subsequent bits
                        _pixelByte1 <<= 2;
                        // --- Obtain the next attribute
                        _attrByte2 = FetchScreenMemory(ulaCycle.AttributeToFetchAddress);
                        break;

                    case ScreenRenderingPhase.DisplayByte2:
                        // --- Display bit 7 and 6 according to the corresponding color
                        SetPixels(
                            GetColor(_pixelByte2 & 0x80, _attrByte2),
                            GetColor(_pixelByte2 & 0x40, _attrByte2));
                        // --- Shift in the subsequent bits
                        _pixelByte2 <<= 2;
                        break;

                    case ScreenRenderingPhase.DisplayByte2AndFetchByte1:
                        // --- Display bit 7 and 6 according to the corresponding color
                        SetPixels(
                            GetColor(_pixelByte2 & 0x80, _attrByte2),
                            GetColor(_pixelByte2 & 0x40, _attrByte2));
                        // --- Shift in the subsequent bits
                        _pixelByte2 <<= 2;
                        // --- Obtain the next pixel byte
                        _pixelByte1 = FetchScreenMemory(ulaCycle.PixelByteToFetchAddress);
                        break;

                    case ScreenRenderingPhase.DisplayByte2AndFetchAttribute1:
                        // --- Display bit 7 and 6 according to the corresponding color
                        SetPixels(
                            GetColor(_pixelByte2 & 0x80, _attrByte2),
                            GetColor(_pixelByte2 & 0x40, _attrByte2));
                        // --- Shift in the subsequent bits
                        _pixelByte2 <<= 2;
                        // --- Obtain the next attribute
                        _attrByte1 = FetchScreenMemory(ulaCycle.AttributeToFetchAddress);
                        break;
                }
            }
        }

        /// <summary>
        /// Tests whether the specified cycle is in the visible area of the screen.
        /// </summary>
        /// <param name="line">Line index</param>
        /// <param name="cycleInLine">Tacts index within the line</param>
        /// <returns>
        /// True, if the tact is visible on the screen; otherwise, false
        /// </returns>
        public virtual bool IsCycleVisible(int line, int cycleInLine)
        {
            var firstVisibleLine = VerticalSyncLines + NonVisibleBorderTopLines;
            var lastVisibleLine = firstVisibleLine + BorderTopLines + DisplayLines + BorderBottomLines;
            return
                line >= firstVisibleLine
                && line < lastVisibleLine
                && cycleInLine >= HorizontalBlankingTime
                && cycleInLine < ScreenLineTime - NonVisibleBorderRightTime;
        }

        /// <summary>
        /// Tests whether the cycle is in the display area of the screen.
        /// </summary>
        /// <param name="line">Line index</param>
        /// <param name="cycleInLine">Tacts index within the line</param>
        /// <returns>
        /// True, if the tact is within the display area of the screen; otherwise, false.
        /// </returns>
        public virtual bool IsCycleInDisplayArea(int line, int cycleInLine)
        {
            return line >= FirstDisplayLine
                && line <= LastDisplayLine
                && cycleInLine >= FirstPixelCycleInLine
                && cycleInLine < FirstPixelCycleInLine + DisplayLineTime;
        }

        /// <summary>
        /// Sets the two adjacent screen pixels belonging to the specified cycle to the given
        /// color
        /// </summary>
        /// <param name="colorIndex1">Color index of the first pixel</param>
        /// <param name="colorIndex2">Color index of the second pixel</param>
        protected virtual void SetPixels(int colorIndex1, int colorIndex2)
        {
            var pos = _yPos * ScreenWidth + _xPos;
            _frameBuffer[pos++] = ULAPalette[colorIndex1];
            _frameBuffer[pos] = ULAPalette[colorIndex2];
        }

        /// <summary>
        /// Gets the color index for the specified pixel value according
        /// to the given color attribute
        /// </summary>
        /// <param name="pixelValue">0 for paper pixel, non-zero for ink pixel</param>
        /// <param name="attr">Color attribute</param>
        /// <returns></returns>
        protected virtual int GetColor(int pixelValue, byte attr)
        {
            var offset = (pixelValue == 0 ? 0 : 0x100) + attr;
            return _flashPhase
                ? _flashOnColors[offset]
                : _flashOffColors[offset];
        }

		/// <summary>
        /// Resets the ULA cycle to start screen rendering from the beginning
        /// </summary>
		protected virtual void ResetULACycle()
        {
            LastRenderedULACycle = -1;
        }

        /// <summary>
        /// Initializes the ULA cycle table
        /// </summary>
        protected virtual void InitULACycleTable()
        {
            _renderingCycleTable = new ScreenRenderingCycle[UlaFrameCycleCount];

            // loop through every cycle
            for (var cycle = 0; cycle < UlaFrameCycleCount; cycle++)
            {
                var line = cycle / ScreenLineTime;
                var cycleInLine = cycle % ScreenLineTime;

                var cycleItem = new ScreenRenderingCycle
                {
                    Phase = ScreenRenderingPhase.None,
                    ContentionDelay = 0
                };

                if (IsCycleVisible(line, cycleInLine))
                {
					// calculate pixel positions
                    cycleItem.XPos = (ushort)((cycleInLine - HorizontalBlankingTime) * 2);
                    cycleItem.YPos = (ushort)(line - VerticalSyncLines - NonVisibleBorderTopLines);

                    if (!IsCycleInDisplayArea(line, cycleInLine))
                    {
                        // we are in the border
                        cycleItem.Phase = ScreenRenderingPhase.Border;
                        // set the border colour
                        if (line >= FirstDisplayLine &&
                            line <= LastDisplayLine)
                        {
                            if (cycleInLine == FirstPixelCycleInLine - PixelDataPrefetchTime)
                            {
                                // left or right border beside the display area
                                // fetch the first pixel data byte of the current line (2 cycles away)
                                cycleItem.Phase = ScreenRenderingPhase.BorderAndFetchPixelByte;
                                cycleItem.PixelByteToFetchAddress = CalculatePixelByteAddress(line, cycleInLine + 2);
                                cycleItem.ContentionDelay = 6;
                            }
                            else if (cycleInLine == FirstPixelCycleInLine - AttributeDataPrefetchTime)
                            {
                                // fetch the first attribute data byte of the current line (1 cycle away)
                                cycleItem.Phase = ScreenRenderingPhase.BorderAndFetchPixelAttribute;
                                cycleItem.AttributeToFetchAddress = CalculateAttributeAddress(line, cycleInLine + 1);
                                cycleItem.ContentionDelay = 5;
                            }
                        }
                    }
                    else
                    {
                        var pixelCycle = cycleInLine - FirstPixelCycleInLine;
                        // the ULA will perform a different action based on the current cycle (T-State)
                        switch (pixelCycle & 7)
                        {
                            case 0:
                                // Display the current cycle pixels
                                cycleItem.Phase = ScreenRenderingPhase.DisplayByte1;
                                cycleItem.ContentionDelay = 4;
                                break;
                            case 1:
                                // Display the current cycle pixels
                                cycleItem.Phase = ScreenRenderingPhase.DisplayByte1;
                                cycleItem.ContentionDelay = 3;
                                break;
                            case 2:
                                // While displaying the current cycle pixels, we need to prefetch the
                                // pixel data byte 2 cycles away
                                cycleItem.Phase = ScreenRenderingPhase.DisplayByte1AndFetchByte2;
                                cycleItem.PixelByteToFetchAddress = CalculatePixelByteAddress(line, cycleInLine + 2);
                                cycleItem.ContentionDelay = 2;
                                break;
                            case 3:
                                // While displaying the current cycle pixels, we need to prefetch the
                                // attribute data byte 1 cycle away
                                cycleItem.Phase = ScreenRenderingPhase.DisplayByte1AndFetchAttribute2;
                                cycleItem.AttributeToFetchAddress = CalculateAttributeAddress(line, cycleInLine + 1);
                                cycleItem.ContentionDelay = 1;
                                break;
                            case 4:
                            case 5:
                                // Display the current cycle pixels
                                cycleItem.Phase = ScreenRenderingPhase.DisplayByte2;
                                break;
                            case 6:
                                if (cycleInLine < FirstPixelCycleInLine + DisplayLineTime - 2)
                                {
                                    // There are still more bytes to display in this line.
                                    // While displaying the current cycle pixels, we need to prefetch the
                                    // pixel data byte 2 cycles away
                                    cycleItem.Phase = ScreenRenderingPhase.DisplayByte2AndFetchByte1;
                                    cycleItem.PixelByteToFetchAddress = CalculatePixelByteAddress(line, cycleInLine + 2);
                                    cycleItem.ContentionDelay = 6;
                                }
                                else
                                {
                                    // Last byte in this line.
                                    // Display the current cycle pixels
                                    cycleItem.Phase = ScreenRenderingPhase.DisplayByte2;
                                }
                                break;
                            case 7:
                                if (cycleInLine < FirstPixelCycleInLine + DisplayLineTime - 1)
                                {
                                    // There are still more bytes to display in this line.
                                    // While displaying the current cycle pixels, we need to prefetch the
                                    // attribute data byte 1 cycle away
                                    cycleItem.Phase = ScreenRenderingPhase.DisplayByte2AndFetchAttribute1;
                                    cycleItem.AttributeToFetchAddress = CalculateAttributeAddress(line, cycleInLine + 1);
                                    cycleItem.ContentionDelay = 5;
                                }
                                else
                                {
                                    // Last byte in this line.
                                    // Display the current cycle pixels
                                    cycleItem.Phase = ScreenRenderingPhase.DisplayByte2;
                                }
                                break;
                        }
                    }
                }

                // Store the calulated cycle item
                _renderingCycleTable[cycle] = cycleItem;
            }
        }

        /// <summary>
        /// Calculates the pixel address for the specified line and tact within 
        /// the line
        /// </summary>
        /// <param name="line">Line index</param>
        /// <param name="tactInLine">Tacts index within the line</param>
        /// <returns>ZX spectrum screen memory address</returns>
        /// <remarks>
        /// Memory address bits: 
        /// C0-C2: pixel count within a byte -- not used in address calculation
        /// C3-C7: pixel byte within a line
        /// V0-V7: pixel line address
        /// 
        /// Direct Pixel Address (da)
        /// =================================================================
        /// |A15|A14|A13|A12|A11|A10|A9 |A8 |A7 |A6 |A5 |A4 |A3 |A2 |A1 |A0 |
        /// =================================================================
        /// | 0 | 0 | 0 |V7 |V6 |V5 |V4 |V3 |V2 |V1 |V0 |C7 |C6 |C5 |C4 |C3 |
        /// =================================================================
        /// | 1 | 1 | 1 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 1 | 1 | 1 | 0xF81F
        /// =================================================================
        /// | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0x0700
        /// =================================================================
        /// | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0x00E0
        /// =================================================================
        /// 
        /// Spectrum Pixel Address
        /// =================================================================
        /// |A15|A14|A13|A12|A11|A10|A9 |A8 |A7 |A6 |A5 |A4 |A3 |A2 |A1 |A0 |
        /// =================================================================
        /// | 0 | 0 | 0 |V7 |V6 |V2 |V1 |V0 |V5 |V4 |V3 |C7 |C6 |C5 |C4 |C3 |
        /// =================================================================
        /// </remarks>
        protected virtual ushort CalculatePixelByteAddress(int line, int cycleInLine)
        {
            var row = line - FirstDisplayLine;
            var column = 2 * (cycleInLine - (HorizontalBlankingTime + BorderLeftTime));
            var da = 0x4000 | (column >> 3) | (row << 5);
            return (ushort)((da & 0xF81F) // --- Reset V5, V4, V3, V2, V1
                | ((da & 0x0700) >> 3)    // --- Keep V5, V4, V3 only
                | ((da & 0x00E0) << 3));  // --- Exchange the V2, V1, V0 bit 
                                          // --- group with V5, V4, V3
        }

        /// <summary>
        /// Calculates the pixel attribute address for the specified line and 
        /// tact within the line
        /// </summary>
        /// <param name="line">Line index</param>
        /// <param name="tactInLine">Tacts index within the line</param>
        /// <returns>ZX spectrum screen memory address</returns>
        /// <remarks>
        /// Memory address bits: 
        /// C0-C2: pixel count within a byte -- not used in address calculation
        /// C3-C7: pixel byte within a line
        /// V0-V7: pixel line address
        /// 
        /// Spectrum Attribute Address
        /// =================================================================
        /// |A15|A14|A13|A12|A11|A10|A9 |A8 |A7 |A6 |A5 |A4 |A3 |A2 |A1 |A0 |
        /// =================================================================
        /// | 0 | 1 | 0 | 1 | 1 | 0 |V7 |V6 |V5 |V4 |V3 |C7 |C6 |C5 |C4 |C3 |
        /// =================================================================
        /// </remarks>
        protected virtual ushort CalculateAttributeAddress(int line, int cycleInLine)
        {
            var row = line - FirstDisplayLine;
            var column = 2 * (cycleInLine - (HorizontalBlankingTime + BorderLeftTime));
            var da = (column >> 3) | ((row >> 3) << 5);
            return (ushort)(0x5800 + da);
        }

        #endregion

        #region Initialisation

        /// <summary>
        /// Initialises the screen configuration calculations
        /// </summary>
        public virtual void InitScreenConfig(ZXSpectrum.BorderType border_type)
        {
            switch (border_type)
            {
                case ZXSpectrum.BorderType.Full:
                    BorderTopLines = 48;
                    BorderBottomLines = 48;
                    NonVisibleBorderTopLines = 8;
                    NonVisibleBorderBottomLines = 8;
                    break;

                case ZXSpectrum.BorderType.Widescreen:
                    BorderTopLines = 0;
                    BorderBottomLines = 0;
                    NonVisibleBorderTopLines = 8 + 48;
                    NonVisibleBorderBottomLines = 8 + 48;
                    break;
            }

            ScreenLines = BorderTopLines + DisplayLines + BorderBottomLines;
            FirstDisplayLine = VerticalSyncLines + NonVisibleBorderTopLines + BorderTopLines;
            LastDisplayLine = FirstDisplayLine + DisplayLines - 1;
            ScreenWidth = BorderLeftPixels + DisplayWidth + BorderRightPixels;
            FirstPixelCycleInLine = HorizontalBlankingTime + BorderLeftTime;
            ScreenLineTime = FirstPixelCycleInLine + DisplayLineTime + BorderRightTime + NonVisibleBorderRightTime;
            UlaFrameCycleCount = (FirstDisplayLine + DisplayLines + BorderBottomLines + NonVisibleBorderTopLines) * ScreenLineTime;
            FirstScreenPixelCycle = (VerticalSyncLines + NonVisibleBorderTopLines) * ScreenLineTime + HorizontalBlankingTime;
        }

        /// <summary>
        /// Inits the screen
        /// </summary>
        protected virtual void InitScreen()
        {
            //BorderDevice.Reset();
            _flashPhase = false;

            _frameBuffer = new int[ScreenWidth * ScreenLines];

            InitULACycleTable();

            // --- Calculate color conversion table
            _flashOffColors = new int[0x200];
            _flashOnColors = new int[0x200];

            for (var attr = 0; attr < 0x100; attr++)
            {
                var ink = (attr & 0x07) | ((attr & 0x40) >> 3);
                var paper = ((attr & 0x38) >> 3) | ((attr & 0x40) >> 3);
                _flashOffColors[attr] = paper;
                _flashOffColors[0x100 + attr] = ink;
                _flashOnColors[attr] = (attr & 0x80) != 0 ? ink : paper;
                _flashOnColors[0x100 + attr] = (attr & 0x80) != 0 ? paper : ink;
            }

            FrameCount = 0;
        }

        #endregion

        #region VBLANK Interrupt

		/// <summary>
        /// The longest instruction cycle count
        /// </summary>
        protected const int LONGEST_OP_CYCLES = 23;

        /// <summary>
        /// The ULA cycle to raise the interrupt at
        /// </summary>
        protected int InterruptCycle = 32;

        /// <summary>
        /// Signs that an interrupt has been raised in this frame.
        /// </summary>
        protected bool InterruptRaised;

        /// <summary>
        /// Signs that the interrupt signal has been revoked
        /// </summary>
        protected bool InterruptRevoked;

		/// <summary>
        /// Resets the interrupt - this should happen every frame in order to raise
        /// the VBLANK interrupt in the proceding frame
        /// </summary>
		public virtual void ResetInterrupt()
        {
            InterruptRaised = false;
            InterruptRevoked = false;
        }

		/// <summary>
        /// Generates an interrupt in the current phase if needed
        /// </summary>
        /// <param name="currentCycle"></param>
		protected virtual void CheckForInterrupt(int currentCycle)
        {
            if (InterruptRevoked)
            {
                // interrupt has already been handled
                return;
            }

			if (currentCycle < InterruptCycle)
            {
                // interrupt does not need to be raised yet
                return;
            }

			if (currentCycle > InterruptCycle + LONGEST_OP_CYCLES)
            {
                // interrupt should have already been raised and the cpu may or
                // may not have caught it. The time has passed so revoke the signal
                InterruptRevoked = true;
                //CPU.IFF1 = true;
                CPU.FlagI = false;
                //CPU.NonMaskableInterruptPending = true;
            }

			if (InterruptRaised)
            {
                // INT is raised but not yet revoked
                // CPU has NOT handled it yet
                return;
            }

            // if CPU is masking the interrupt do not raise it
            //if (!CPU.IFF1)
                //return;

            // Raise the interrupt
            InterruptRaised = true;
            //CPU.IFF1 = false;
            //CPU.IFF2 = false;
            CPU.FlagI = true;
            FrameCount++;			
        }

        #endregion

        #region IVideoProvider

        public int VirtualWidth => ScreenWidth;
        public int VirtualHeight => ScreenLines;
        public int BufferWidth => ScreenWidth;
        public int BufferHeight => ScreenLines;
        public int BackgroundColor => ULAPalette[BorderColour];
        
        public int VsyncNumerator
        {
            get { return 3500000; }
            set { }
        }

        public int VsyncDenominator
        {
            get { return UlaFrameCycleCount; }
        }
        /*
        public int VsyncNumerator => NullVideo.DefaultVsyncNum;
        public int VsyncDenominator => NullVideo.DefaultVsyncDen;
		
        public int[] GetVideoBuffer()
        {
            /*
            switch(Spectrum.SyncSettings.BorderType)
            {
                case ZXSpectrum.BorderType.Full:                
                    return _frameBuffer;

                case ZXSpectrum.BorderType.Small:
                    // leave only 10 border units all around
                    int[] smlBuff = new int[(ScreenWidth - 30) * (DisplayLines - 30)];
                    int index = 0;
                    int brdCount = 0;
                    // skip top and bottom
                    for (int i = ScreenWidth * 30; i < smlBuff.Length - ScreenWidth * 30; i++)
                    {
                        if (brdCount < 30)
                        {
                            brdCount++;
                            continue;
                        }
                        if (brdCount > ScreenWidth - 30)
                        {
                            brdCount++;
                            continue;
                        }

                        smlBuff[index] = _frameBuffer[i];
                        index++;
                        brdCount++;
                    }

                    return smlBuff;

                case ZXSpectrum.BorderType.Medium:
                    break;
            }
           
            return _frameBuffer;
            
        }

        #endregion

    }
    */
}
