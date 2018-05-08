
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// ULA (Uncommitted Logic Array) implementation
    /// </summary>
    public abstract class ULABase : IVideoProvider
    {
        #region General

        /// <summary>
        /// Length of the frame in T-States
        /// </summary>
        public int FrameLength;
        
        /// <summary>
        /// Emulated clock speed
        /// </summary>
        public int ClockSpeed;

        /// <summary>
        /// Whether machine is late or early timing model
        /// </summary>
        public bool LateTiming; //currently not implemented

        /// <summary>
        /// The current cycle within the current frame
        /// </summary>
        public int CurrentTStateInFrame;


        protected SpectrumBase _machine;

        #endregion

        #region Palettes

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

        #region Contention

        /// <summary>
        /// T-State at which to start applying contention
        /// </summary>
        public int contentionStartPeriod;
        /// <summary>
        /// T-State at which to end applying contention
        /// </summary>
        public int contentionEndPeriod;
        /// <summary>
        /// T-State memory contention delay mapping
        /// </summary>
        public byte[] contentionTable;

        #endregion

        #region Screen Rendering

        /// <summary>
        /// Video output buffer
        /// </summary>
        public int[] ScreenBuffer;
        /// <summary>
        /// Display memory
        /// </summary>
        protected byte[] screen;
        /// <summary>
        /// Attribute memory lookup (mapped 1:1 to screen for convenience)
        /// </summary>
        protected short[] attr;
        /// <summary>
        /// T-State display mapping
        /// </summary>
        protected short[] tstateToDisp;
        /// <summary>
        /// Table that stores T-State to screen/attribute address values
        /// </summary>
        public short[] floatingBusTable;
        /// <summary>
        /// Cycle at which the last render update took place
        /// </summary>
        protected long lastTState;
        /// <summary>
        /// T-States elapsed since last render update
        /// </summary>
        protected long elapsedTStates;
        /// <summary>
        /// T-State of top left raster pixel
        /// </summary>
        protected int actualULAStart;
        /// <summary>
        /// Offset into display memory based on current T-State
        /// </summary>
        protected int screenByteCtr;
        /// <summary>
        /// Offset into current pixel of rasterizer
        /// </summary>
        protected int ULAByteCtr;
        /// <summary>
        /// The current border colour
        /// </summary>
        public int borderColour;
        /// <summary>
        /// Signs whether the colour flash is ON or OFF
        /// </summary>
        protected bool flashOn = false;


        protected int flashCounter;
        public int FlashCounter
        {
            get { return flashCounter; }
            set
            {
                flashCounter = value;
            }
        }


        /// <summary>
        /// Internal frame counter used for flasher operations
        /// </summary>
        protected int frameCounter = 0;

        /// <summary>
        /// Last 8-bit bitmap read from display memory
        /// (Floating bus implementation)
        /// </summary>
        protected int lastPixelValue;
        /// <summary>
        /// Last 8-bit attr val read from attribute memory
        /// (Floating bus implementation)
        /// </summary>
        protected int lastAttrValue;
        /// <summary>
        /// Last 8-bit bitmap read from display memory+1
        /// (Floating bus implementation)
        /// </summary>
        protected int lastPixelValuePlusOne; 
        /// <summary>
        /// Last 8-bit attr val read from attribute memory+1
        /// (Floating bus implementation)
        /// </summary>
        protected int lastAttrValuePlusOne;

        /// <summary>
        /// Used to create the non-border display area
        /// </summary>
        protected int TtateAtLeft;
        protected int TstateWidth;
        protected int TstateAtTop;
        protected int TstateHeight;
        protected int TstateAtRight;
        protected int TstateAtBottom;

        /// <summary>
        /// Total T-States in one scanline
        /// </summary>
        protected int TstatesPerScanline;
        /// <summary>
        /// Total pixels in one scanline
        /// </summary>
        protected int ScanLineWidth;
        /// <summary>
        /// Total chars in one PRINT row
        /// </summary>
        protected int CharRows;
        /// <summary>
        /// Total chars in one PRINT column
        /// </summary>
        protected int CharCols;
        /// <summary>
        /// Total pixels in one display row
        /// </summary>
        protected int ScreenWidth;
        /// <summary>
        /// Total pixels in one display column
        /// </summary>
        protected int ScreenHeight;
        /// <summary>
        /// Total pixels in top border
        /// </summary>
        protected int BorderTopHeight;
        /// <summary>
        /// Total pixels in bottom border
        /// </summary>
        protected int BorderBottomHeight;
        /// <summary>
        /// Total pixels in left border width
        /// </summary>
        protected int BorderLeftWidth;
        /// <summary>
        /// Total pixels in right border width
        /// </summary>
        protected int BorderRightWidth;
        /// <summary>
        /// Memory address of display start
        /// </summary>
        protected int DisplayStart;
        /// <summary>
        /// Total number of bytes of display memory
        /// </summary>
        protected int DisplayLength;
        /// <summary>
        /// Memory address of attribute start
        /// </summary>
        protected int AttributeStart;
        /// <summary>
        /// Total number of bytes of attribute memory
        /// </summary>
        protected int AttributeLength;

        /// <summary>
        /// Raised when ULA has finished painting the entire screen
        /// </summary>
        public bool needsPaint = false;

        #endregion

        #region Interrupt

        /// <summary>
        /// The number of T-States that the INT pin is simulated to be held low
        /// </summary>
        public int InterruptPeriod;

        /// <summary>
        /// The longest instruction cycle count
        /// </summary>
        protected int LongestOperationCycles = 23;

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
        public virtual void CheckForInterrupt(long currentCycle)
        {
            if (InterruptRevoked)
            {
                // interrupt has already been handled
                return;
            }

            if (currentCycle < LongestOperationCycles)// InterruptPeriod)
            {
                // interrupt does not need to be raised yet
                return;
            }

            if (currentCycle >= InterruptPeriod + LongestOperationCycles)
            {
                // interrupt should have already been raised and the cpu may or
                // may not have caught it. The time has passed so revoke the signal
                InterruptRevoked = true;
                _machine.CPU.FlagI = false;
                return;
            }

            if (InterruptRaised)
            {
                // INT is raised but not yet revoked
                // CPU has NOT handled it yet
                return;
            }

            // Raise the interrupt
            InterruptRaised = true;
            _machine.CPU.FlagI = true;

            // Signal the start of ULA processing
            if (_machine._render)
                ULAUpdateStart();

            CalcFlashCounter();
        }

        #endregion        

        #region Construction & Initialisation

        public ULABase(SpectrumBase machine)
        {
            _machine = machine;
            borderType = _machine.Spectrum.SyncSettings.BorderType;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resets the ULA chip
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Builds the contention table for the emulated model
        /// </summary>
        public abstract void BuildContentionTable();

        /// <summary>
        /// Returns true if the given memory address should be contended
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public abstract bool IsContended(int addr);

        /// <summary>
        /// Contends the machine for a given address
        /// </summary>
        /// <param name="addr"></param>
        public virtual void Contend(ushort addr)
        {
            if (IsContended(addr) && !(_machine is ZX128Plus3))
            {
                _machine.CPU.TotalExecutedCycles += contentionTable[CurrentTStateInFrame];
            }
        }

        public virtual void Contend(int addr, int time, int count)
        {
            if (IsContended(addr) && !(_machine is ZX128Plus3))
            {
                for (int f = 0; f < count; f++)
                {
                    _machine.CPU.TotalExecutedCycles += contentionTable[CurrentTStateInFrame] + time;
                }
            }
            else
                _machine.CPU.TotalExecutedCycles += count * time;
        }

        /// <summary>
        /// Resets render state once interrupt is generated
        /// </summary>
        public void ULAUpdateStart()
        {
            ULAByteCtr = 0;
            screenByteCtr = DisplayStart;
            lastTState = actualULAStart;
            needsPaint = true;
        }

        /// <summary>
        /// Flash processing
        /// </summary>
        public void CalcFlashCounter()
        {
            flashCounter++;

            if (flashCounter > 15)
            {
                flashOn = !flashOn;
                flashCounter = 0;
            }
        }

        /// <summary>
        /// Builds the T-State to attribute map used with the floating bus
        /// </summary>
        public void BuildAttributeMap()
        {
            int start = DisplayStart;

            for (int f = 0; f < DisplayLength; f++, start++)
            {
                int addrH = start >> 8; //div by 256
                int addrL = start % 256;

                int pixelY = (addrH & 0x07);
                pixelY |= (addrL & (0xE0)) >> 2;
                pixelY |= (addrH & (0x18)) << 3;

                int attrIndex_Y = AttributeStart + ((pixelY >> 3) << 5);// pixel/8 * 32

                addrL = start % 256;
                int pixelX = addrL & (0x1F);

                attr[f] = (short)(attrIndex_Y + pixelX);
            }
        }

        /// <summary>
        /// Updates the screen buffer based on the number of T-States supplied
        /// </summary>
        /// <param name="_tstates"></param>
        public virtual void UpdateScreenBuffer(long _tstates)
        {
            if (_tstates < actualULAStart)
            {
                return;
            }
            else if (_tstates >= FrameLength)
            {
                _tstates = FrameLength - 1;

                needsPaint = true;
            }

            //the additional 1 tstate is required to get correct number of bytes to output in ircontention.sna
            elapsedTStates = (_tstates + 1 - lastTState) - 1;

            //It takes 4 tstates to write 1 byte. Or, 2 pixels per t-state.

            long numBytes = (elapsedTStates >> 2) + ((elapsedTStates % 4) > 0 ? 1 : 0);

            int pixelData;
            int pixel2Data = 0xff;
            int attrData;
            int attr2Data;
            int bright;
            int ink;
            int paper;
            int flash;

            for (int i = 0; i < numBytes; i++)
            {
                if (tstateToDisp[lastTState] > 1)
                {
                    screenByteCtr = tstateToDisp[lastTState] - 16384; //adjust for actual screen offset

                    pixelData = _machine.FetchScreenMemory((ushort)screenByteCtr); //screen[screenByteCtr];
                    attrData = _machine.FetchScreenMemory((ushort)(attr[screenByteCtr] - 16384)); //screen[attr[screenByteCtr] - 16384];

                    lastPixelValue = pixelData;
                    lastAttrValue = attrData;
                    
                    bright = (attrData & 0x40) >> 3;
                    flash = (attrData & 0x80) >> 7;
                    ink = (attrData & 0x07);
                    paper = ((attrData >> 3) & 0x7);
                    int paletteInk = ULAPalette[ink + bright];
                    int palettePaper = ULAPalette[paper + bright];

                    if (flashOn && (flash != 0)) //swap paper and ink when flash is on
                    {
                        int temp = paletteInk;
                        paletteInk = palettePaper;
                        palettePaper = temp;
                    }
                    
                    for (int a = 0; a < 8; ++a)
                    {
                        if ((pixelData & 0x80) != 0)
                        {
                            ScreenBuffer[ULAByteCtr++] = paletteInk;
                            lastAttrValue = ink;
                            //pixelIsPaper = false;
                        }
                        else
                        {
                            ScreenBuffer[ULAByteCtr++] = palettePaper;
                            lastAttrValue = paper;
                        }
                        pixelData <<= 1;
                    }
                }
                else if (tstateToDisp[lastTState] == 1)
                {
                    int bor = ULAPalette[borderColour];
                    
                    for (int g = 0; g < 8; g++)
                        ScreenBuffer[ULAByteCtr++] = bor;
                }
                lastTState += 4;
            }
        }

        #endregion

        #region IVideoProvider

        private int _virtualWidth;
        private int _virtualHeight;
        private int _bufferWidth;
        private int _bufferHeight;

        public int BackgroundColor
        {
            get { return ULAPalette[7]; } //ULAPalette[borderColour]; }
        }

        public int VirtualWidth
        {
            get { return _virtualWidth; }
            set { _virtualWidth = value; }
        }

        public int VirtualHeight
        {
            get { return _virtualHeight; }
            set { _virtualHeight = value; }
        }

        public int BufferWidth
        {
            get { return _bufferWidth; }
            set { _bufferWidth = value; }
        }

        public int BufferHeight
        {
            get { return _bufferHeight; }
            set { _bufferHeight = value; }
        }

        public int VsyncNumerator
        {
            get { return ClockSpeed * 50; }// ClockSpeed; }
            set { }
        }

        public int VsyncDenominator
        {
            get { return ClockSpeed; }//FrameLength; }
        }

        public int[] GetVideoBuffer()
        {
            switch (borderType)
            {
                // Full side borders, no top or bottom border (giving *almost* 16:9 output)
                case ZXSpectrum.BorderType.Widescreen:
                    // we are cropping out the top and bottom borders
                    var startPixelsToCrop = ScanLineWidth * BorderTopHeight;
                    var endPixelsToCrop = ScanLineWidth * BorderBottomHeight;
                    int index = 0;
                    for (int i = startPixelsToCrop; i < ScreenBuffer.Length - endPixelsToCrop; i++)
                    {
                        croppedBuffer[index++] = ScreenBuffer[i];
                    }
                    return croppedBuffer;

                // The full spectrum border
                case ZXSpectrum.BorderType.Full:
                    return ScreenBuffer;

                case ZXSpectrum.BorderType.Medium:
                    // all border sizes now 24
                    var lR = BorderLeftWidth - 24;
                    var rR = BorderRightWidth - 24;
                    var tR = BorderTopHeight - 24;
                    var bR = BorderBottomHeight - 24;
                    var startP = ScanLineWidth * tR;
                    var endP = ScanLineWidth * bR;

                    int index2 = 0;
                    // line by line
                    for (int i = startP; i < ScreenBuffer.Length - endP; i += ScreenWidth + BorderLeftWidth + BorderRightWidth)
                    {
                        // each pixel in each line
                        for (int p = lR; p < ScreenWidth + BorderLeftWidth + BorderRightWidth - rR; p++)
                        {
                            if (index2 == croppedBuffer.Length)
                                break;
                            croppedBuffer[index2++] = ScreenBuffer[i + p];
                        }
                    }

                    return croppedBuffer;

                case ZXSpectrum.BorderType.Small:
                    // all border sizes now 24
                    var lR_ = BorderLeftWidth - 10;
                    var rR_ = BorderRightWidth - 10;
                    var tR_ = BorderTopHeight - 10;
                    var bR_ = BorderBottomHeight - 10;
                    var startP_ = ScanLineWidth * tR_;
                    var endP_ = ScanLineWidth * bR_;

                    int index2_ = 0;
                    // line by line
                    for (int i = startP_; i < ScreenBuffer.Length - endP_; i += ScreenWidth + BorderLeftWidth + BorderRightWidth)
                    {
                        // each pixel in each line
                        for (int p = lR_; p < ScreenWidth + BorderLeftWidth + BorderRightWidth - rR_; p++)
                        {
                            if (index2_ == croppedBuffer.Length)
                                break;
                            croppedBuffer[index2_++] = ScreenBuffer[i + p];
                        }
                    }

                    return croppedBuffer;

                case ZXSpectrum.BorderType.None:
                    // all border sizes now 24
                    var lR__ = BorderLeftWidth;
                    var rR__ = BorderRightWidth;
                    var tR__ = BorderTopHeight;
                    var bR__ = BorderBottomHeight;
                    var startP__ = ScanLineWidth * tR__;
                    var endP__ = ScanLineWidth * bR__;

                    int index2__ = 0;
                    // line by line
                    for (int i = startP__; i < ScreenBuffer.Length - endP__; i += ScreenWidth + BorderLeftWidth + BorderRightWidth)
                    {
                        // each pixel in each line
                        for (int p = lR__; p < ScreenWidth + BorderLeftWidth + BorderRightWidth - rR__; p++)
                        {
                            if (index2__ == croppedBuffer.Length)
                                break;
                            croppedBuffer[index2__++] = ScreenBuffer[i + p];
                        }
                    }

                    return croppedBuffer;
            }

            return ScreenBuffer;
        }

        protected void SetupScreenSize()
        {
            switch (borderType)
            {
                case ZXSpectrum.BorderType.Full:
                    BufferWidth = ScreenWidth + BorderLeftWidth + BorderRightWidth;
                    BufferHeight = ScreenHeight + BorderTopHeight + BorderBottomHeight;
                    VirtualHeight = BufferHeight;
                    VirtualWidth = BufferWidth;
                    ScreenBuffer = new int[BufferWidth * BufferHeight];
                    break;

                case ZXSpectrum.BorderType.Widescreen:
                    BufferWidth = ScreenWidth + BorderLeftWidth + BorderRightWidth;
                    BufferHeight = ScreenHeight;
                    VirtualHeight = BufferHeight;
                    VirtualWidth = BufferWidth;
                    croppedBuffer = new int[BufferWidth * BufferHeight];
                    break;

                case ZXSpectrum.BorderType.Medium:
                    BufferWidth = ScreenWidth + (24) + (24);
                    BufferHeight = ScreenHeight + (24) + (24);
                    VirtualHeight = BufferHeight;
                    VirtualWidth = BufferWidth;
                    croppedBuffer = new int[BufferWidth * BufferHeight];
                    break;

                case ZXSpectrum.BorderType.Small:
                    BufferWidth = ScreenWidth + (10) + (10);
                    BufferHeight = ScreenHeight + (10) + (10);
                    VirtualHeight = BufferHeight;
                    VirtualWidth = BufferWidth;
                    croppedBuffer = new int[BufferWidth * BufferHeight];
                    break;

                case ZXSpectrum.BorderType.None:
                    BufferWidth = ScreenWidth;
                    BufferHeight = ScreenHeight;
                    VirtualHeight = BufferHeight;
                    VirtualWidth = BufferWidth;
                    croppedBuffer = new int[BufferWidth * BufferHeight];
                    break;
            }
        }

        protected int[] croppedBuffer;

        private ZXSpectrum.BorderType _borderType;

        public ZXSpectrum.BorderType borderType
        {
            get { return _borderType; }
            set { _borderType = value; }
        }



        #endregion

        #region IStatable

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("ULA");
            ser.Sync("ScreenBuffer", ref ScreenBuffer, false);
            ser.Sync("FrameLength", ref FrameLength);
            ser.Sync("ClockSpeed", ref ClockSpeed);
            ser.Sync("LateTiming", ref LateTiming);
            ser.Sync("borderColour", ref borderColour);
            ser.EndSection();
        }

        #endregion

        #region Attribution

        /*
         * Based on code from ArjunNair's Zero emulator (MIT Licensed)
         * https://github.com/ArjunNair/Zero-Emulator
        
        The MIT License (MIT)

        Copyright (c) 2009 Arjun Nair

        Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
        documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
        the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
        and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
        WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
        COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, 
        ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

        */
        #endregion
    }
}
