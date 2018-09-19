using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Render pixels to the screen
    /// </summary>
    public class CRTDevice : IVideoProvider
    {
        #region Devices

        private CPCBase _machine;
        private CRCT_6845 CRCT => _machine.CRCT;
        private AmstradGateArray GateArray => _machine.GateArray;

        #endregion

        #region Construction

        public CRTDevice(CPCBase machine)
        {
            _machine = machine;
            CurrentLine = new ScanLine(this);

            CRCT.AttachHSYNCCallback(OnHSYNC);
            CRCT.AttachVSYNCCallback(OnVSYNC);
        }

        #endregion

        #region Palettes
        
        /// <summary>
        /// The standard CPC Pallete (ordered by firmware #)
        /// http://www.cpcwiki.eu/index.php/CPC_Palette
        /// </summary>
        public static readonly int[] CPCFirmwarePalette =
        {
            Colors.ARGB(0x00, 0x00, 0x00), // Black
            Colors.ARGB(0x00, 0x00, 0x80), // Blue
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue
            Colors.ARGB(0x80, 0x00, 0x00), // Red            
            Colors.ARGB(0x80, 0x00, 0x80), // Magenta
            Colors.ARGB(0x80, 0x00, 0xFF), // Mauve
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta
            Colors.ARGB(0x00, 0x80, 0x00), // Green
            Colors.ARGB(0x00, 0x80, 0x80), // Cyan
            Colors.ARGB(0x00, 0x80, 0xFF), // Sky Blue
            Colors.ARGB(0x80, 0x80, 0x00), // Yellow
            Colors.ARGB(0x80, 0x80, 0x80), // White
            Colors.ARGB(0x80, 0x80, 0xFF), // Pastel Blue
            Colors.ARGB(0xFF, 0x80, 0x00), // Orange
            Colors.ARGB(0xFF, 0x80, 0x80), // Pink
            Colors.ARGB(0xFF, 0x80, 0xFF), // Pastel Magenta
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan
            Colors.ARGB(0x80, 0xFF, 0x00), // Lime
            Colors.ARGB(0x80, 0xFF, 0x80), // Pastel Green
            Colors.ARGB(0x80, 0xFF, 0xFF), // Pastel Cyan
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White
        };

        /// <summary>
        /// The standard CPC Pallete (ordered by hardware #)
        /// http://www.cpcwiki.eu/index.php/CPC_Palette
        /// </summary>
        public static readonly int[] CPCHardwarePalette =
        {
            Colors.ARGB(0x80, 0x80, 0x80), // White
            Colors.ARGB(0x80, 0x80, 0x80), // White (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow
            Colors.ARGB(0x00, 0x00, 0x80), // Blue
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple
            Colors.ARGB(0x00, 0x80, 0x80), // Cyan
            Colors.ARGB(0xFF, 0x80, 0x80), // Pink
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple (duplicate)
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow (duplicate)
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta
            Colors.ARGB(0xFF, 0x80, 0x00), // Orange
            Colors.ARGB(0xFF, 0x80, 0xFF), // Pastel Magenta
            Colors.ARGB(0x00, 0x00, 0x80), // Blue (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan
            Colors.ARGB(0x00, 0x00, 0x00), // Black
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue
            Colors.ARGB(0x00, 0x80, 0x00), // Green
            Colors.ARGB(0x00, 0x80, 0xFF), // Sky Blue
            Colors.ARGB(0x80, 0x00, 0x80), // Magenta
            Colors.ARGB(0x80, 0xFF, 0x80), // Pastel Green
            Colors.ARGB(0x80, 0xFF, 0x00), // Lime
            Colors.ARGB(0x80, 0xFF, 0xFF), // Pastel Cyan
            Colors.ARGB(0x80, 0x00, 0x00), // Red     
            Colors.ARGB(0x80, 0x00, 0xFF), // Mauve
            Colors.ARGB(0x80, 0x80, 0x00), // Yellow            
            Colors.ARGB(0x80, 0x80, 0xFF), // Pastel Blue
        };
       

        #endregion

        #region Public Stuff        

        /// <summary>
        /// The current scanline that is being added to
        /// (will be processed and committed to the screen buffer every HSYNC)
        /// </summary>
        public ScanLine CurrentLine;

        /// <summary>
        /// The number of top border scanlines to ommit when rendering
        /// </summary>
        public int TopLinesToTrim = 20;

        /// <summary>
        /// Count of rendered scanlines this frame
        /// </summary>
        public int ScanlineCounter = 0;

        /// <summary>
        /// Video buffer processing
        /// </summary>
        public int[] ProcessVideoBuffer()
        {
            return ScreenBuffer;
        }

        /// <summary>
        /// Sets up buffers and the like at the start of a frame
        /// </summary>
        public void SetupVideo()
        {
            if (BufferHeight == 576)
                return;

            BufferWidth = 800;
            BufferHeight = 576;

            VirtualWidth = BufferWidth / 2;
            VirtualHeight = BufferHeight / 2;

            ScreenBuffer = new int[BufferWidth * BufferHeight];
        }

        /// <summary>
        /// Fired when the CRCT flags HSYNC
        /// </summary>
        public void OnHSYNC()
        {

        }

        /// <summary>
        /// Fired when the CRCT flags VSYNC
        /// </summary>
        public void OnVSYNC()
        {

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
            get { return CPCHardwarePalette[0]; }
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
            get { return GateArray.Z80ClockSpeed * 50; }
            set { }
        }

        public int VsyncDenominator
        {
            get { return GateArray.Z80ClockSpeed; }
        }

        public int[] GetVideoBuffer()
        {
            return ProcessVideoBuffer();
        }

        public void SetupScreenSize()
        {
            BufferWidth = 1024; // 512;
            BufferHeight = 768;
            VirtualHeight = BufferHeight;
            VirtualWidth = BufferWidth;
            ScreenBuffer = new int[BufferWidth * BufferHeight];
            croppedBuffer = ScreenBuffer;
        }

        protected int[] croppedBuffer;

        #endregion

        #region Serialization

        public void SyncState(Serializer ser)
        {
            ser.BeginSection("CRT");
            ser.Sync("BufferWidth", ref _bufferWidth);
            ser.Sync("BufferHeight", ref _bufferHeight);
            ser.Sync("VirtualHeight", ref _virtualHeight);
            ser.Sync("VirtualWidth", ref _virtualWidth);
            ser.Sync("ScreenBuffer", ref ScreenBuffer, false);
            ser.Sync("ScanlineCounter", ref ScanlineCounter);
            ser.EndSection();
        }

        #endregion
    }

    /// <summary>
    /// Represents a single scanline buffer
    /// </summary>
    public class ScanLine
    {
        /// <summary>
        /// Array of character information
        /// </summary>
        public Character[] Characters;

        /// <summary>
        /// The screenmode that was set at the start of this scanline
        /// </summary>
        public int ScreenMode = 1;

        /// <summary>
        /// The scanline number (0 based)
        /// </summary>
        public int LineIndex;

        /// <summary>
        /// The calling CRT device
        /// </summary>
        private CRTDevice CRT;

        public ScanLine(CRTDevice crt)
        {
            Reset();            
            CRT = crt;
        }

        // To be run after scanline has been fully processed
        public void InitScanline(int screenMode, int lineIndex)
        {
            Reset();
            ScreenMode = screenMode;
            LineIndex = lineIndex;
        }

        /// <summary>
        /// Adds a single scanline character into the matrix
        /// </summary>
        /// <param name="charIndex"></param>
        /// <param name="phase"></param>
        public void AddScanlineCharacter(int index, RenderPhase phase, byte vid1, byte vid2, int[] pens)
        {
            if (index >= 64)
            {
                return;
            }

            switch (phase)
            {
                case RenderPhase.BORDER:
                    AddBorderValue(index, CRTDevice.CPCHardwarePalette[pens[16]]);
                    break;
                case RenderPhase.DISPLAY:
                    AddDisplayValue(index, vid1, vid2, pens);
                    break;
                default:
                    AddSyncValue(index, phase);
                    break;
            }
        }

        /// <summary>
        /// Adds a HSYNC, VSYNC or HSYNC+VSYNC character into the scanline
        /// </summary>
        /// <param name="charIndex"></param>
        /// <param name="phase"></param>
        private void AddSyncValue(int charIndex, RenderPhase phase)
        {
            Characters[charIndex].Phase = phase;
            Characters[charIndex].Pixels = new int[0];
        }

        /// <summary>
        /// Adds a border character into the scanline
        /// </summary>
        /// <param name="index"></param>
        /// <param name="colourValue"></param>
        private void AddBorderValue(int charIndex, int colourValue)
        {
            Characters[charIndex].Phase = RenderPhase.BORDER;

            switch (ScreenMode)
            {
                case 0:
                    Characters[charIndex].Pixels = new int[4];
                    break;
                case 1:
                    Characters[charIndex].Pixels = new int[8];
                    break;
                case 2:
                    Characters[charIndex].Pixels = new int[16];
                    break;
                case 3:
                    Characters[charIndex].Pixels = new int[8];
                    break;
            }

            

            for (int i = 0; i < Characters[charIndex].Pixels.Length; i++)
            {
                Characters[charIndex].Pixels[i] = colourValue;
            }
        }

        /// <summary>
        /// Adds a display character into the scanline
        /// Pixel matrix is calculated based on the current ScreenMode
        /// </summary>
        /// <param name="charIndex"></param>
        /// <param name="vid1"></param>
        /// <param name="vid2"></param>
        public void AddDisplayValue(int charIndex, byte vid1, byte vid2, int[] pens)
        {
            Characters[charIndex].Phase = RenderPhase.DISPLAY;

            // generate pixels based on screen mode
            switch (ScreenMode)
            {
                // 4 bits per pixel - 2 bytes - 4 pixels (8 CRT pixels)
                // RECT
                case 0:
                    Characters[charIndex].Pixels = new int[16];

                    int m0Count = 0;

                    int pix = vid1 & 0xaa;
                    pix = ((pix & 0x80) >> 7) | ((pix & 0x08) >> 2) | ((pix & 0x20) >> 3) | ((pix & 0x02 << 2));
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    pix = vid1 & 0x55;
                    pix = (((pix & 0x40) >> 6) | ((pix & 0x04) >> 1) | ((pix & 0x10) >> 2) | ((pix & 0x01 << 3)));
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];

                    pix = vid2 & 0xaa;
                    pix = ((pix & 0x80) >> 7) | ((pix & 0x08) >> 2) | ((pix & 0x20) >> 3) | ((pix & 0x02 << 2));
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    pix = vid2 & 0x55;
                    pix = (((pix & 0x40) >> 6) | ((pix & 0x04) >> 1) | ((pix & 0x10) >> 2) | ((pix & 0x01 << 3)));
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[pix]];
                    /*
                    int m0B0P0i = vid1 & 0xaa;
                    int m0B0P0 = ((m0B0P0i & 0x80) >> 7) | ((m0B0P0i & 0x08) >> 2) | ((m0B0P0i & 0x20) >> 3) | ((m0B0P0i & 0x02 << 2));
                    int m0B0P1i = vid1 & 85;
                    int m0B0P1 = ((m0B0P1i & 0x40) >> 6) | ((m0B0P1i & 0x04) >> 1) | ((m0B0P1i & 0x10) >> 2) | ((m0B0P1i & 0x01 << 3));

                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B0P0]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B0P0]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B0P1]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B0P1]];

                    int m0B1P0i = vid2 & 170;
                    int m0B1P0 = ((m0B1P0i & 0x80) >> 7) | ((m0B1P0i & 0x08) >> 2) | ((m0B1P0i & 0x20) >> 3) | ((m0B1P0i & 0x02 << 2));
                    int m0B1P1i = vid2 & 85;
                    int m0B1P1 = ((m0B1P1i & 0x40) >> 6) | ((m0B1P1i & 0x04) >> 1) | ((m0B1P1i & 0x10) >> 2) | ((m0B1P1i & 0x01 << 3));

                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B1P0]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B1P0]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B1P1]];
                    Characters[charIndex].Pixels[m0Count++] = CRTDevice.CPCHardwarePalette[pens[m0B1P1]];
                    */
                    break;

                // 2 bits per pixel - 2 bytes - 8 pixels (16 CRT pixels)
                // SQUARE
                case 1:
                    Characters[charIndex].Pixels = new int[8];

                    int m1Count = 0;

                    int m1B0P0 = (((vid1 & 0x80) >> 7) | ((vid1 & 0x08) >> 2));
                    int m1B0P1 = (((vid1 & 0x40) >> 6) | ((vid1 & 0x04) >> 1));
                    int m1B0P2 = (((vid1 & 0x20) >> 5) | ((vid1 & 0x02)));
                    int m1B0P3 = (((vid1 & 0x10) >> 4) | ((vid1 & 0x01) << 1));

                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B0P0]];
                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B0P1]];
                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B0P2]];
                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B0P3]];

                    int m1B1P0 = (((vid2 & 0x80) >> 7) | ((vid2 & 0x08) >> 2));
                    int m1B1P1 = (((vid2 & 0x40) >> 6) | ((vid2 & 0x04) >> 1));
                    int m1B1P2 = (((vid2 & 0x20) >> 5) | ((vid2 & 0x02)));
                    int m1B1P3 = (((vid2 & 0x10) >> 4) | ((vid2 & 0x01) << 1));

                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B1P0]];
                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B1P1]];
                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B1P2]];
                    Characters[charIndex].Pixels[m1Count++] = CRTDevice.CPCHardwarePalette[pens[m1B1P3]];
                    break;

                // 1 bit per pixel - 2 bytes - 16 pixels (16 CRT pixels)
                // RECT
                case 2:
                    Characters[charIndex].Pixels = new int[16];

                    int m2Count = 0;

                    int[] pixBuff = new int[16];

                    for (int bit = 7; bit >= 0; bit--)
                    {
                        int val = vid1.Bit(bit) ? 1 : 0;
                        Characters[charIndex].Pixels[m2Count++] = CRTDevice.CPCHardwarePalette[pens[val]];

                    }
                    for (int bit = 7; bit >= 0; bit--)
                    {
                        int val = vid2.Bit(bit) ? 1 : 0;
                        Characters[charIndex].Pixels[m2Count++] = CRTDevice.CPCHardwarePalette[pens[val]];
                    }
                    break;

                // 4 bits per pixel - 2 bytes - 4 pixels (8 CRT pixels)
                // RECT
                case 3:
                    Characters[charIndex].Pixels = new int[4];

                    int m3Count = 0;

                    int m3B0P0i = vid1 & 170;
                    int m3B0P0 = ((m3B0P0i & 0x80) >> 7) | ((m3B0P0i & 0x08) >> 2) | ((m3B0P0i & 0x20) >> 3) | ((m3B0P0i & 0x02 << 2));
                    int m3B0P1i = vid1 & 85;
                    int m3B0P1 = ((m3B0P1i & 0x40) >> 6) | ((m3B0P1i & 0x04) >> 1) | ((m3B0P1i & 0x10) >> 2) | ((m3B0P1i & 0x01 << 3));

                    Characters[charIndex].Pixels[m3Count++] = CRTDevice.CPCHardwarePalette[pens[m3B0P0]];
                    Characters[charIndex].Pixels[m3Count++] = CRTDevice.CPCHardwarePalette[pens[m3B0P1]];

                    int m3B1P0i = vid1 & 170;
                    int m3B1P0 = ((m3B1P0i & 0x80) >> 7) | ((m3B1P0i & 0x08) >> 2) | ((m3B1P0i & 0x20) >> 3) | ((m3B1P0i & 0x02 << 2));
                    int m3B1P1i = vid1 & 85;
                    int m3B1P1 = ((m3B1P1i & 0x40) >> 6) | ((m3B1P1i & 0x04) >> 1) | ((m3B1P1i & 0x10) >> 2) | ((m3B1P1i & 0x01 << 3));

                    Characters[charIndex].Pixels[m3Count++] = CRTDevice.CPCHardwarePalette[pens[m3B1P0]];
                    Characters[charIndex].Pixels[m3Count++] = CRTDevice.CPCHardwarePalette[pens[m3B1P1]];
                    break;
            }
        }

        /// <summary>
        /// Returns the number of pixels decoded in this scanline (border and display)
        /// </summary>
        /// <returns></returns>
        private int GetPixelCount()
        {
            int cnt = 0;

            foreach (var c in Characters)
            {
                if (c.Pixels != null)
                    cnt += c.Pixels.Length;
            }

            return cnt;
        }

        /// <summary>
        /// Called at the start of HSYNC
        /// Processes and adds the scanline to the Screen Buffer
        /// </summary>
        public void CommitScanline()
        {            
            int hScale = 1;
            int vScale = 1;

            switch (ScreenMode)
            {
                case 0:
                    hScale = 1;
                    vScale = 2;
                    break;
                case 1:
                case 3:                    
                    hScale = 2;
                    vScale = 2;
                    break;

                case 2:
                    hScale = 1;
                    vScale = 2;
                    break;
            }

            int hPix = GetPixelCount() * hScale;
            //int hPix = GetPixelCount() * 2;
            int leftOver = CRT.BufferWidth - hPix;
            int lPad = leftOver / 2;
            int rPad = lPad;
            int rem = leftOver % 2;
            if (rem != 0)
                rPad += rem;

            if (LineIndex < CRT.TopLinesToTrim)
            {
                return;
            }

            // render out the scanline
            int pCount = (LineIndex - CRT.TopLinesToTrim) * vScale * CRT.BufferWidth;

            // vScale
            for (int s = 0; s < vScale; s++)
            {
                // left padding
                for (int lP = 0; lP < lPad; lP++)
                {
                    CRT.ScreenBuffer[pCount++] = 0;
                }

                // border and display
                foreach (var c in Characters)
                {
                    if (c.Pixels == null || c.Pixels.Length == 0)
                        continue;

                    for (int p = 0; p < c.Pixels.Length; p++)
                    {
                        // hScale
                        for (int h = 0; h < hScale; h++)
                        {
                            CRT.ScreenBuffer[pCount++] = c.Pixels[p];
                        }
                        
                        //CRT.ScreenBuffer[pCount++] = c.Pixels[p];
                    }
                }

                // right padding
                for (int rP = 0; rP < rPad; rP++)
                {
                    CRT.ScreenBuffer[pCount++] = 0;
                }

                if (pCount != hPix)
                {

                }

                CRT.ScanlineCounter++;
            }
        }

        public void Reset()
        {
            ScreenMode = 1;
            Characters = new Character[64];

            for (int i = 0; i < Characters.Length; i++)
            {
                Characters[i] = new Character();
            }
        }
    }

    /// <summary>
    /// Contains data relating to one character written on one scanline
    /// </summary>
    public class Character
    {
        /// <summary>
        /// Array of pixels generated for this character
        /// </summary>
        public int[] Pixels;

        /// <summary>
        /// The type (NONE/BORDER/DISPLAY/HSYNC/VSYNC/HSYNC+VSYNC
        /// </summary>
        public RenderPhase Phase = RenderPhase.NONE;

        public Character()
        {
            Pixels = new int[0];
        }
    }

    [Flags]
    public enum RenderPhase : int
    {
        /// <summary>
        /// Nothing
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Border is being rendered
        /// </summary>
        BORDER = 1,
        /// <summary>
        /// Display rendered from video RAM
        /// </summary>
        DISPLAY = 2,
        /// <summary>
        /// HSYNC in progress
        /// </summary>
        HSYNC = 3,
        /// <summary>
        /// VSYNC in process
        /// </summary>
        VSYNC = 4,
        /// <summary>
        /// HSYNC occurs within a VSYNC
        /// </summary>
        HSYNCandVSYNC = 5
    }
}
