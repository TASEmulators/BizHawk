using BizHawk.Common;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Uncommitted logic array implementation (ULA)
    /// </summary>
    public abstract class ULA : IVideoProvider
    {
        #region Other Devices

        /// <summary>
        /// The emulated spectrum
        /// </summary>
        protected SpectrumBase _machine;

        /// <summary>
        /// The CPU monitor class
        /// </summary>
        protected CPUMonitor CPUMon;

        #endregion

        #region Construction & Initialisation

        public ULA (SpectrumBase machine)
        {
            _machine = machine;
            CPUMon = _machine.CPUMon;
            borderType = _machine.Spectrum.SyncSettings.BorderType;
        }

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

        #region Timing

        /// <summary>
        /// The CPU speed
        /// </summary>
        public int ClockSpeed;

        /// <summary>
        /// Length of frame in T-State cycles
        /// </summary>
        public int FrameCycleLength;

        /// <summary>
        /// The T-State at which the interrupt should be raised within the frame
        /// </summary>
        public int InterruptStartTime;

        /// <summary>
        /// The period for which the interrupt should he held
        /// (simulated /INT pin held low)
        /// </summary>
        public int InterruptLength;

        /// <summary>
        /// Contention offset
        /// </summary> 
        public int ContentionOffset;

        /// <summary>
        /// Arbitrary offset for render table generation
        /// </summary>
        public int RenderTableOffset;

        /// <summary>
        /// The offset when return floating bus bytes
        /// </summary>
        public int FloatingBusOffset;

        /// <summary>
        /// The time in T-States for one scanline to complete
        /// </summary>
        public int ScanlineTime;

        /// <summary>
        /// T-States at the left border
        /// </summary>
        public int BorderLeftTime;

        /// <summary>
        /// T-States at the right border
        /// </summary>
        public int BorderRightTime;

        public int FirstPaperLine;
        public int FirstPaperTState;
        public bool Border4T;
        public int Border4TStage;

        #endregion

        #region Interrupt Generation

        /// <summary>
        /// Signs that an interrupt has been raised in this frame.
        /// </summary>
        protected bool InterruptRaised;

        public long ULACycleCounter;
        public long LastULATick;
        public bool FrameEnd;

        /// <summary>
        /// Cycles the ULA clock
        /// Handles interrupt generation
        /// </summary>
        /// <param name="currentCycle"></param>
        public virtual void CycleClock(long totalCycles)
        {
            // render the screen
            if (_machine._render)
                RenderScreen((int)_machine.CurrentFrameCycle);

            // has more than one cycle past since this last ran
            // (this can be true if contention has taken place)
            var ticksToProcess = totalCycles - LastULATick;

            // store the current cycle
            LastULATick = totalCycles;
            
            // process the cycles past as well as the upcoming one
            for (int i = 0; i < ticksToProcess; i++)
            {
                ULACycleCounter++;

                if (InterruptRaised)
                {
                    // /INT pin is currently being held low
                    if (ULACycleCounter < InterruptLength + InterruptStartTime)
                    {
                        // ULA should still hold the /INT pin low
                        _machine.CPU.FlagI = true;
                    }
                    else
                    {
                        // its time (or past time) to stop holding the /INT pin low
                        _machine.CPU.FlagI = false;
                        InterruptRaised = false;
                    }
                }
                else
                {
                    // interrupt is currently not raised
                    if (ULACycleCounter == FrameLength + InterruptStartTime)
                    {
                        // time to raise the interrupt
                        InterruptRaised = true;
						_machine.CPU.FlagI = true;
                        FrameEnd = true;
                        ULACycleCounter = InterruptStartTime;
                        CalcFlashCounter();
                    }
                }
            }            
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

        #endregion

        #region Screen Layout

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
        /// Total pixels in one scanline
        /// </summary>
        protected int ScanLineWidth;

        #endregion

        #region State

        /// <summary>
        /// The last T-State cycle at which the screen was rendered
        /// </summary>
        public int LastTState;

        /// <summary>
        /// Flash state
        /// </summary>
        public bool flashOn;

        private int flashCounter;

        protected byte fetchB1;
        protected byte fetchA1;
        protected byte fetchB2;
        protected byte fetchA2;
        protected int ink;
        protected int paper;
        protected int fetchBorder;
        protected int bright;
        protected int flash;

        public int palPaper;
        public int palInk;

        public int BorderColor = 7;

        #endregion

        #region Conversions

        public int FrameLength => FrameCycleLength;

        #endregion

        #region Rendering Configuration

        /// <summary>
        /// Holds all information regarding rendering the screen based on the current T-State
        /// </summary>
        public RenderTable RenderingTable;

        /// <summary>
        /// Holds all information regarding rendering the screen based on the current T-State
        /// </summary>
        public class RenderTable
        {
            /// <summary>
            /// The ULA device
            /// </summary>
            private ULA _ula;

            /// <summary>
            /// Array of rendercycle entries
            /// Starting from the interrupt
            /// </summary>
            public RenderCycle[] Renderer;

            /// <summary>
            /// The emulated machine
            /// </summary>
            public MachineType _machineType;

            public int Offset;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="contPattern"></param>
            public RenderTable(ULA ula, MachineType machineType)
            {
                _ula = ula;
                _machineType = machineType;
                Renderer = new RenderCycle[_ula.FrameCycleLength];
                InitRenderer(machineType);
            }

            /// <summary>
            /// Initializes the renderer
            /// </summary>
            /// <param name="machineType"></param>
            private void InitRenderer(MachineType machineType)
            {
                for (var t = 0; t < _ula.FrameCycleLength; t++)
                {
                    var tStateScreen = t + _ula.RenderTableOffset;// + _ula.InterruptStartTime;

                    if (tStateScreen < 0)
                        tStateScreen += _ula.FrameCycleLength;
                    else if (tStateScreen >= _ula.FrameCycleLength)
                        tStateScreen -= _ula.FrameCycleLength;

                    CalculateRenderItem(t, tStateScreen / _ula.ScanlineTime, tStateScreen % _ula.ScanlineTime);
                }

                CreateContention(machineType);
            }

            private void CalculateRenderItem(int item, int line, int pix)
            {
                Renderer[item] = new RenderCycle();

                Renderer[item].RAction = RenderAction.None;
                int pitchWidth = _ula.ScreenWidth + _ula.BorderRightWidth + _ula.BorderLeftWidth;

                int scrPix = pix - _ula.FirstPaperTState;
                int scrLin = line - _ula.FirstPaperLine;

                if ((line >= (_ula.FirstPaperLine - _ula.BorderTopHeight)) && (line < (_ula.FirstPaperLine + 192 + _ula.BorderBottomHeight)) &&
                    (pix >= (_ula.FirstPaperTState - _ula.BorderLeftTime)) && (pix < (_ula.FirstPaperTState + 128 + _ula.BorderRightTime)))
                {
                    // visibleArea (vertical)
                    if ((line >= _ula.FirstPaperLine) && (line < (_ula.FirstPaperLine + 192)) &&
                        (pix >= _ula.FirstPaperTState) && (pix < (_ula.FirstPaperTState + 128)))
                    {
                        // pixel area
                        switch (scrPix & 7)
                        {
                            case 0:
                                Renderer[item].RAction = RenderAction.Shift1AndFetchByte2;   // shift 1 + fetch B2
                                                                                       // +4 = prefetch!
                                Renderer[item].ByteAddress = CalculateByteAddress(scrPix + 4, scrLin);
                                break;
                            case 1:
                                Renderer[item].RAction = RenderAction.Shift1AndFetchAttribute2;   // shift 1 + fetch A2
                                                                                       // +3 = prefetch!
                                Renderer[item].AttributeAddress = CalculateAttributeAddress(scrPix + 3, scrLin);
                                break;
                            case 2:
                                Renderer[item].RAction = RenderAction.Shift1;   // shift 1
                                break;
                            case 3:
                                Renderer[item].RAction = RenderAction.Shift1Last;   // shift 1 (last)
                                break;
                            case 4:
                                Renderer[item].RAction = RenderAction.Shift2;   // shift 2
                                break;
                            case 5:
                                Renderer[item].RAction = RenderAction.Shift2;   // shift 2
                                break;
                            case 6:
                                if (pix < (_ula.FirstPaperTState + 128 - 2))
                                {
                                    Renderer[item].RAction = RenderAction.Shift2AndFetchByte1;   // shift 2 + fetch B2
                                }
                                else
                                {
                                    Renderer[item].RAction = RenderAction.Shift2;             // shift 2
                                }

                                // +2 = prefetch!
                                Renderer[item].ByteAddress = CalculateByteAddress(scrPix + 2, scrLin);
                                break;
                            case 7:
                                if (pix < (_ula.FirstPaperTState + 128 - 2))
                                {
                                    //???
                                    Renderer[item].RAction = RenderAction.Shift2AndFetchAttribute1;   // shift 2 + fetch A2
                                }
                                else
                                {
                                    Renderer[item].RAction = RenderAction.Shift2;             // shift 2
                                }

                                // +1 = prefetch!
                                Renderer[item].AttributeAddress = CalculateAttributeAddress(scrPix + 1, scrLin);
                                break;
                        }
                    }
                    else if ((line >= _ula.FirstPaperLine) && (line < (_ula.FirstPaperLine + 192)) &&
                             (pix == (_ula.FirstPaperTState - 2)))  // border & fetch B1
                    {
                        Renderer[item].RAction = RenderAction.BorderAndFetchByte1; // border & fetch B1
                                                                             // +2 = prefetch!
                        Renderer[item].ByteAddress = CalculateByteAddress(scrPix + 2, scrLin);
                    }
                    else if ((line >= _ula.FirstPaperLine) && (line < (_ula.FirstPaperLine + 192)) &&
                             (pix == (_ula.FirstPaperTState - 1)))  // border & fetch A1
                    {
                        Renderer[item].RAction = RenderAction.BorderAndFetchAttribute1; // border & fetch A1
                                                                             // +1 = prefetch!
                        Renderer[item].AttributeAddress = CalculateAttributeAddress(scrPix + 1, scrLin);
                    }
                    else
                    {
                        Renderer[item].RAction = RenderAction.Border; // border
                    }

                    int wy = line - (_ula.FirstPaperLine - _ula.BorderTopHeight);
                    int wx = (pix - (_ula.FirstPaperTState - _ula.BorderLeftTime)) * 2;
                    Renderer[item].LineOffset = wy * pitchWidth + wx;
                }
            }

            private void CreateContention(MachineType machineType)
            {
                int[] conPattern = new int[8];

                switch (machineType)
                {
                    case MachineType.ZXSpectrum16:
                    case MachineType.ZXSpectrum48:
                    case MachineType.ZXSpectrum128:
                    case MachineType.ZXSpectrum128Plus2:
                        conPattern = new int[] { 6, 5, 4, 3, 2, 1, 0, 0 };
                        break;

                    case MachineType.ZXSpectrum128Plus2a:
                    case MachineType.ZXSpectrum128Plus3:
                        conPattern = new int[] { 1, 0, 7, 6, 5, 4, 3, 2 };
                        break;
                }

                // calculate contention values
                for (int t = 0; t < _ula.FrameCycleLength; t++)
                {
                    int shifted = t + _ula.RenderTableOffset + _ula.ContentionOffset; // _ula.InterruptStartTime;
                    if (shifted < 0)
                        shifted += _ula.FrameCycleLength;
                    shifted %= _ula.FrameCycleLength;

                    Renderer[t].ContentionValue = 0;

                    int line = shifted / _ula.ScanlineTime;
                    int pix = shifted % _ula.ScanlineTime;
                    if (line < _ula.FirstPaperLine || line >= (_ula.FirstPaperLine + 192))
                    {
                        Renderer[t].ContentionValue = 0;
                        continue;
                    }
                    int scrPix = pix - _ula.FirstPaperTState;
                    if (scrPix < 0 || scrPix >= 128)
                    {
                        Renderer[t].ContentionValue = 0;
                        continue;
                    }
                    int pixByte = scrPix % 8;

                    Renderer[t].ContentionValue = conPattern[pixByte];
                }
            }

            private ushort CalculateByteAddress(int x, int y)
            {
                x >>= 2;
                var vp = x | (y << 5);
                return (ushort)((vp & 0x181F) | ((vp & 0x0700) >> 3) | ((vp & 0x00E0) << 3));
            }

            private ushort CalculateAttributeAddress(int x, int y)
            {
                x >>= 2;
                var ap = x | ((y >> 3) << 5);
                return (ushort)(6144 + ap);
            }

            /// <summary>
            /// Render/contention information for a single T-State
            /// </summary>
            public class RenderCycle
            {
                /// <summary>
                /// The ULA render action at this T-State
                /// </summary>
                public RenderAction RAction;
                /// <summary>
                /// The contention value at this T-State
                /// </summary>
                public int ContentionValue;
                /// <summary>
                /// The screen byte address at this T-State
                /// </summary>
                public ushort ByteAddress;
                /// <summary>
                /// The screen attribute address at this T-State
                /// </summary>
                public ushort AttributeAddress;
                /// <summary>
                /// The byte address returned by the floating bus at this T-State
                /// </summary>
                public ushort FloatingBusAddress;
                /// <summary>
                /// The offset
                /// </summary>
                public int LineOffset;
            }

            public enum RenderAction
            {
                None,
                Border,
                BorderAndFetchByte1,
                BorderAndFetchAttribute1,
                Shift1AndFetchByte2,
                Shift1AndFetchAttribute2,
                Shift1,
                Shift1Last,
                Shift2,
                Shift2Last,
                Shift2AndFetchByte1,
                Shift2AndFetchAttribute1
            }
        }

        #endregion

        #region Render Methods

        /// <summary>
        /// Renders to the screen buffer based on the current cycle
        /// </summary>
        /// <param name="toCycle"></param>
        public void RenderScreen(int toCycle)
        {
            // check boundaries
            if (toCycle > FrameCycleLength)
                toCycle = FrameCycleLength;

            // render the required number of cycles
            for (int t = LastTState; t < toCycle; t++)
            {
                if (!Border4T || (t & 3) == Border4TStage)
                {
                    fetchBorder = BorderColor;
                }
                else
                {

                }

                //fetchBorder = BorderColor;

                // get the table entry
                var item = RenderingTable.Renderer[t];

                switch (item.RAction)
                {
                    case RenderTable.RenderAction.None:
                        break;

                    case RenderTable.RenderAction.Border:
                        ScreenBuffer[item.LineOffset] = ULAPalette[fetchBorder];
                        ScreenBuffer[item.LineOffset + 1] = ULAPalette[fetchBorder];
                        break;

                    case RenderTable.RenderAction.BorderAndFetchByte1:
                        ScreenBuffer[item.LineOffset] = ULAPalette[fetchBorder];
                        ScreenBuffer[item.LineOffset + 1] = ULAPalette[fetchBorder];
                        fetchB1 = _machine.FetchScreenMemory(item.ByteAddress);
                        break;

                    case RenderTable.RenderAction.BorderAndFetchAttribute1:
                        ScreenBuffer[item.LineOffset] = ULAPalette[fetchBorder];
                        ScreenBuffer[item.LineOffset + 1] = ULAPalette[fetchBorder];
                        fetchA1 = _machine.FetchScreenMemory(item.AttributeAddress);
                        ProcessInkPaper(fetchA1);
                        break;

                    case RenderTable.RenderAction.Shift1AndFetchByte2:
                        ScreenBuffer[item.LineOffset] = ((fetchB1 & 0x80) != 0) ? palInk : palPaper;
                        ScreenBuffer[item.LineOffset + 1] = ((fetchB1 & 0x40) != 0) ? palInk : palPaper;
                        fetchB1 <<= 2;
                        fetchB2 = _machine.FetchScreenMemory(item.ByteAddress);
                        break;

                    case RenderTable.RenderAction.Shift1AndFetchAttribute2:
                        ScreenBuffer[item.LineOffset] = ((fetchB1 & 0x80) != 0) ? palInk : palPaper;
                        ScreenBuffer[item.LineOffset + 1] = ((fetchB1 & 0x40) != 0) ? palInk : palPaper;
                        fetchB1 <<= 2;
                        fetchA2 = _machine.FetchScreenMemory(item.AttributeAddress);
                        break;

                    case RenderTable.RenderAction.Shift1:
                        ScreenBuffer[item.LineOffset] = ((fetchB1 & 0x80) != 0) ? palInk : palPaper;
                        ScreenBuffer[item.LineOffset + 1] = ((fetchB1 & 0x40) != 0) ? palInk : palPaper;
                        fetchB1 <<= 2;
                        break;

                    case RenderTable.RenderAction.Shift1Last:
                        ScreenBuffer[item.LineOffset] = ((fetchB1 & 0x80) != 0) ? palInk : palPaper;
                        ScreenBuffer[item.LineOffset + 1] = ((fetchB1 & 0x40) != 0) ? palInk : palPaper;
                        fetchB1 <<= 2;
                        ProcessInkPaper(fetchA2);
                        break;

                    case RenderTable.RenderAction.Shift2:
                        ScreenBuffer[item.LineOffset] = ((fetchB2 & 0x80) != 0) ? palInk : palPaper;
                        ScreenBuffer[item.LineOffset + 1] = ((fetchB2 & 0x40) != 0) ? palInk : palPaper;
                        fetchB2 <<= 2;
                        break;

                    case RenderTable.RenderAction.Shift2AndFetchByte1:
                        ScreenBuffer[item.LineOffset] = ((fetchB2 & 0x80) != 0) ? palInk : palPaper;
                        ScreenBuffer[item.LineOffset + 1] = ((fetchB2 & 0x40) != 0) ? palInk : palPaper;
                        fetchB2 <<= 2;
                        fetchB1 = _machine.FetchScreenMemory(item.ByteAddress);
                        break;

                    case RenderTable.RenderAction.Shift2AndFetchAttribute1:
                        ScreenBuffer[item.LineOffset] = ((fetchB2 & 0x80) != 0) ? palInk : palPaper;
                        ScreenBuffer[item.LineOffset + 1] = ((fetchB2 & 0x40) != 0) ? palInk : palPaper;
                        fetchB2 <<= 2;
                        fetchA1 = _machine.FetchScreenMemory(item.AttributeAddress);
                        ProcessInkPaper(fetchA1);
                        break;
                }
            }

            LastTState = toCycle;
        }

        private void ProcessInkPaper(byte attrData)
        {
            bright = (attrData & 0x40) >> 3;
            flash = (attrData & 0x80) >> 7;
            ink = (attrData & 0x07);
            paper = ((attrData >> 3) & 0x7);

            palInk = ULAPalette[ink + bright];
            palPaper = ULAPalette[paper + bright];

            // swap paper and ink when flash is on
            if (flashOn && (flash != 0)) 
            {
                int temp = palInk;
                palInk = palPaper;
                palPaper = temp;
            }
        }

        /// <summary>
        /// Generates the port lookup table for +2a/+3 allowed floating bus ports
        /// </summary>
        public void GenerateP3PortTable()
        {
            List<ushort> table = new List<ushort>();
            for (int i = 0; i < 0x1000; i++)
            {
                ushort r = (ushort)(1 + (4 * i));
                if (r > 4093)
                    break;
                table.Add(r);
            }

            Plus3FBPortTable = table.ToArray();
        }

        private ushort[] Plus3FBPortTable = new ushort[1];

        /// <summary>
        /// Returns floating bus value (if available)
        /// </summary>
        /// <param name="tstate"></param>
        /// <returns></returns>
        public void ReadFloatingBus(int tstate, ref int result, ushort port)
        {
            tstate += FloatingBusOffset;
            if (tstate >= RenderingTable.Renderer.Length)
                tstate -= RenderingTable.Renderer.Length;
            if (tstate < 0)
                tstate += RenderingTable.Renderer.Length;

            var item = RenderingTable.Renderer[tstate];

            switch (RenderingTable._machineType)
            {
                case MachineType.ZXSpectrum16:
                case MachineType.ZXSpectrum48:
                case MachineType.ZXSpectrum128:
                case MachineType.ZXSpectrum128Plus2:

                    switch (item.RAction)
                    {
                        case RenderTable.RenderAction.BorderAndFetchByte1:
                        case RenderTable.RenderAction.Shift1AndFetchByte2:
                        case RenderTable.RenderAction.Shift2AndFetchByte1:
                            result = _machine.FetchScreenMemory(item.ByteAddress);
                            break;
                        case RenderTable.RenderAction.BorderAndFetchAttribute1:
                        case RenderTable.RenderAction.Shift1AndFetchAttribute2:
                        case RenderTable.RenderAction.Shift2AndFetchAttribute1:
                            result = _machine.FetchScreenMemory(item.AttributeAddress);
                            break;
                        default:
                            break;
                    }
                    break;

                case MachineType.ZXSpectrum128Plus2a:
                case MachineType.ZXSpectrum128Plus3:
                    
                    // http://sky.relative-path.com/zx/floating_bus.html
                    if (_machine.PagingDisabled)
                    {
                        result = 0xff;
                        break;
                    }

                    // check whether fb is found on this port
                    ushort pLook = Array.Find(Plus3FBPortTable, s => s == port);
                    if (pLook == 0)
                    {
                        result = 0xff;
                        break;
                    }  

                    // floating bus on +2a/+3 always returns a byte with Bit0 set
                    switch (item.RAction)
                    {
                        case RenderTable.RenderAction.BorderAndFetchByte1:
                        case RenderTable.RenderAction.Shift1AndFetchByte2:
                        case RenderTable.RenderAction.Shift2AndFetchByte1:
                            result = (byte)(_machine.FetchScreenMemory(item.ByteAddress) | 0x01);
                            break;
                        case RenderTable.RenderAction.BorderAndFetchAttribute1:
                        case RenderTable.RenderAction.Shift1AndFetchAttribute2:
                        case RenderTable.RenderAction.Shift2AndFetchAttribute1:
                            result = (byte)(_machine.FetchScreenMemory(item.AttributeAddress) | 0x01);
                            break;
                        default:
                            result = (byte)(_machine.LastContendedReadByte | 0x01);
                            break;
                    }

                    break;
            }
        }

        #endregion

        #region Contention

        /// <summary>
        /// Returns the contention value for the current t-state
        /// </summary>
        /// <returns></returns>
        public int GetContentionValue()
        {
            return GetContentionValue((int)_machine.CurrentFrameCycle);
        }

        /// <summary>
        /// Returns the contention value for the supplied t-state
        /// </summary>
        /// <returns></returns>
        public int GetContentionValue(int tstate)
        {
            //tstate += MemoryContentionOffset;
            if (tstate >= FrameCycleLength)
                tstate -= FrameCycleLength;

            if (tstate < 0)
                tstate += FrameCycleLength;

            return RenderingTable.Renderer[tstate].ContentionValue;
        }

        /// <summary>
        /// Returns the contention value for the supplied t-state
        /// </summary>
        /// <returns></returns>
        public int GetPortContentionValue(int tstate)
        {
            //tstate +=  PortContentionOffset;
            if (tstate >= FrameCycleLength)
                tstate -= FrameCycleLength;

            if (tstate < 0)
                tstate += FrameCycleLength;

            return RenderingTable.Renderer[tstate].ContentionValue;
        }

        #endregion

        #region IVideoProvider

        /// <summary>
        /// Video output buffer
        /// </summary>
        public int[] ScreenBuffer;

        private int _virtualWidth;
        private int _virtualHeight;
        private int _bufferWidth;
        private int _bufferHeight;

        public int BackgroundColor
        {
            get
            {
                var settings = _machine.Spectrum.GetSettings();
                var color = settings.BackgroundColor;
                if (!settings.UseCoreBorderForBackground)
                    return color;
                else
                    return ULAPalette[fetchBorder];
            }
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
            BufferWidth = ScreenWidth + BorderLeftWidth + BorderRightWidth;
            BufferHeight = ScreenHeight + BorderTopHeight + BorderBottomHeight;
            VirtualHeight = BufferHeight;
            VirtualWidth = BufferWidth;
            ScreenBuffer = new int[BufferWidth * BufferHeight];

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

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("ULA");
            if (ScreenBuffer != null)
                ser.Sync("ScreenBuffer", ref ScreenBuffer, false);
            ser.Sync("BorderColor", ref BorderColor);
            ser.Sync("LastTState", ref LastTState);
            ser.Sync("flashOn", ref flashOn);
            ser.Sync("fetchB1", ref fetchB1);
            ser.Sync("fetchA1", ref fetchA1);
            ser.Sync("fetchB2", ref fetchB2);
            ser.Sync("fetchA2", ref fetchA2);
            ser.Sync("ink", ref ink);
            ser.Sync("paper", ref paper);
            ser.Sync("fetchBorder", ref fetchBorder);
            ser.Sync("bright", ref bright);
            ser.Sync("flash", ref flash);
            ser.Sync("palPaper", ref palPaper);
            ser.Sync("palInk", ref palInk);

            ser.Sync("LastULATick", ref LastULATick);
            ser.Sync("ULACycleCounter", ref ULACycleCounter);
            ser.Sync("FrameEnd", ref FrameEnd);

            ser.Sync("InterruptRaised", ref InterruptRaised);
            ser.EndSection();
        }

        #endregion
    }
}
