/*
 * Maria.cs
 *
 * The Maria display device.
 * 
 * Derived from much of Dan Boris' work with 7800 emulation
 * within the MESS emulator.
 *
 * Thanks to Matthias Luedtke <matthias@atari8bit.de> for correcting
 * the BuildLineRAM320B() method to correspond closer to real hardware.
 * (Matthias credited an insightful response by Eckhard Stolberg on a forum on
 * Atari Age circa June 2005.)
 * 
 * Copyright © 2004-2012 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public sealed class Maria : IDevice
    {
        #region Constants

        const int
            INPTCTRL= 0x01, // Write: input port control (VBLANK in TIA)
            INPT0   = 0x08, // Read pot port: D7
            INPT1   = 0x09, // Read pot port: D7
            INPT2   = 0x0a, // Read pot port: D7
            INPT3   = 0x0b, // Read pot port: D7
            INPT4   = 0x0c, // Read P1 joystick trigger: D7
            INPT5   = 0x0d, // Read P2 joystick trigger: D7
            AUDC0   = 0x15, // Write: audio control 0 (D3-0)
            AUDC1   = 0x16, // Write: audio control 1 (D4-0)
            AUDF0   = 0x17, // Write: audio frequency 0 (D4-0)
            AUDF1   = 0x18, // Write: audio frequency 1 (D3-0)
            AUDV0   = 0x19, // Write: audio volume 0 (D3-0)
            AUDV1   = 0x1a, // Write: audio volume 1 (D3-0)

            BACKGRND= 0x20, // Background color
            P0C1    = 0x21, // Palette 0 - color 1
            P0C2    = 0x22, // Palette 0 - color 2
            P0C3    = 0x23, // Palette 0 - color 3
            WSYNC   = 0x24, // Wait for sync
            P1C1    = 0x25, // Palette 1 - color 1
            P1C2    = 0x26, // Palette 1 - color 2
            P1C3    = 0x27, // Palette 1 - color 3
            MSTAT   = 0x28, // Maria status
            P2C1    = 0x29, // Palette 2 - color 1
            P2C2    = 0x2a, // Palette 2 - color 2
            P2C3    = 0x2b, // Palette 2 - color 3
            DPPH    = 0x2c, // Display list list point high
            P3C1    = 0x2d, // Palette 3 - color 1
            P3C2    = 0x2e, // Palette 3 - color 2
            P3C3    = 0x2f, // Palette 3 - color 3
            DPPL    = 0x30, // Display list list point low
            P4C1    = 0x31, // Palette 4 - color 1
            P4C2    = 0x32, // Palette 4 - color 2
            P4C3    = 0x33, // Palette 4 - color 3
            CHARBASE= 0x34, // Character base address
            P5C1    = 0x35, // Palette 5 - color 1
            P5C2    = 0x36, // Palette 5 - color 2
            P5C3    = 0x37, // Palette 5 - color 3
            OFFSET  = 0x38, // Future expansion (store zero here)
            P6C1    = 0x39, // Palette 6 - color 1
            P6C2    = 0x3a, // Palette 6 - color 2
            P6C3    = 0x3b, // Palette 6 - color 3
            CTRL    = 0x3c, // Maria control register
            P7C1    = 0x3d, // Palette 7 - color 1
            P7C2    = 0x3e, // Palette 7 - color 2
            P7C3    = 0x3f; // Palette 7 - color 3

        const int CPU_TICKS_PER_AUDIO_SAMPLE = 57;

        #endregion

        #region Fields

        readonly byte[] LineRAM = new byte[0x200];
        readonly byte[] Registers = new byte[0x40];

        readonly Machine7800 M;
        readonly TIASound TIASound;

        ulong _startOfFrameCpuClock;
        int Scanline { get { return (int)(M.CPU.Clock - _startOfFrameCpuClock) / 114; } }
        int HPos { get { return (int)(M.CPU.Clock - _startOfFrameCpuClock) % 114; } }

        int FirstVisibleScanline, LastVisibleScanline;
        int _dmaClocks;
        bool _isPal;

        // For lightgun emulation.
        // Transient state, serialization unnecessary.
        ulong _lightgunFirstSampleCpuClock;
        int _lightgunFrameSamples, _lightgunSampledScanline, _lightgunSampledVisibleHpos;

        bool WM;
        ushort DLL;
        ushort DL;
        int Offset;
        int Holey;
        int Width;
        byte HPOS;
        int PaletteNo;
        bool INDMode;

        bool CtrlLock;

        // MARIA CNTL
        bool DMAEnabled;
        bool ColorKill;
        bool CWidth;
        bool BCntl;
        bool Kangaroo;
        byte RM;

        #endregion

        #region Public Members

        public void Reset()
        {
            CtrlLock = false;

            DMAEnabled = false;
            ColorKill = false;
            CWidth = false;
            BCntl = false;
            Kangaroo = false;
            RM = 0;

            TIASound.Reset();

            Log("{0} reset", this);
        }

        public byte this[ushort addr]
        {
            get { return peek(addr); }
            set { poke(addr, value); }
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        public void StartFrame()
        {
            _startOfFrameCpuClock = M.CPU.Clock + (ulong)(M.CPU.RunClocks / M.CPU.RunClocksMultiple);
            _lightgunFirstSampleCpuClock = 0;

            AssertDebug(M.CPU.RunClocks <= 0 && (M.CPU.RunClocks % M.CPU.RunClocksMultiple) == 0);
            AssertDebug((_startOfFrameCpuClock % (114 * (ulong)M.FrameBuffer.Scanlines)) == 0);

            TIASound.StartFrame();
        }

        public int DoDMAProcessing()
        {
            OutputLineRAM();

            var sl = Scanline;

            if (!DMAEnabled || sl < FirstVisibleScanline || sl >= LastVisibleScanline)
                return 0;

            _dmaClocks = 0;

            if (DMAEnabled && sl == FirstVisibleScanline)
            {
                // DMA TIMING: End of VBLANK: DMA Startup + long shutdown
                _dmaClocks += 15;

                DLL = WORD(Registers[DPPL], Registers[DPPH]);

                ConsumeNextDLLEntry();
            }

            // DMA TIMING: DMA Startup, 5-9 cycles
            _dmaClocks += 5;

            BuildLineRAM();

            if (--Offset < 0)
            {
                ConsumeNextDLLEntry();

                // DMA TIMING: DMA Shutdown: Last line of zone, 10-13 cycles
                _dmaClocks += 10;
            }
            else
            {
                // DMA TIMING: DMA Shutdown: Other line of zone, 4-7 cycles
                _dmaClocks += 4;
            }

            return _dmaClocks;
        }

        public void EndFrame()
        {
            TIASound.EndFrame();
        }

        #endregion

        #region Constructors

        private Maria()
        {
        }

        public Maria(Machine7800 m, int scanlines)
        {
            if (m == null)
                throw new ArgumentNullException("m");

            M = m;
            InitializeVisibleScanlineValues(scanlines);
            TIASound = new TIASound(M, CPU_TICKS_PER_AUDIO_SAMPLE);
        }

        #endregion

        #region Scanline Builders

        void BuildLineRAM()
        {
            var dl = DL;

            // Iterate through Display List (DL)
            while (true)
            {
                var modeByte = DmaRead(dl + 1);
                if ((modeByte & 0x5f) == 0)
                    break;

                INDMode = false;
                ushort graphaddr;

                if ((modeByte & 0x1f) == 0)
                {
                    // Extended DL header
                    var dl0 = DmaRead(dl++); // low address
                    var dl1 = DmaRead(dl++); // mode
                    var dl2 = DmaRead(dl++); // high address
                    var dl3 = DmaRead(dl++); // palette(7-5)/width(4-0)
                    var dl4 = DmaRead(dl++); // horizontal position

                    graphaddr = WORD(dl0, dl2);
                    WM = (dl1 & 0x80) != 0;
                    INDMode = (dl1 & 0x20) != 0;
                    PaletteNo = (dl3 & 0xe0) >> 3;
                    Width = (~dl3 & 0x1f) + 1;
                    HPOS = dl4;

                    // DMA TIMING: DL 5 byte header
                    _dmaClocks += 10;
                }
                else
                {
                    // Normal DL header
                    var dl0 = DmaRead(dl++); // low address
                    var dl1 = DmaRead(dl++); // palette(7-5)/width(4-0)
                    var dl2 = DmaRead(dl++); // high address
                    var dl3 = DmaRead(dl++); // horizontal position

                    graphaddr = WORD(dl0, dl2);
                    PaletteNo = (dl1 & 0xe0) >> 3;
                    Width = (~dl1 & 0x1f) + 1;
                    HPOS = dl3;

                    // DMA TIMING: DL 4 byte header
                    _dmaClocks += 8;
                }

                // DMA TIMING: Graphic reads
                if (RM != 1)
                    _dmaClocks += (Width * (INDMode ? (CWidth ? 9 : 6) : 3));

                switch (RM)
                {
                    case 0:
                        if (WM) BuildLineRAM160B(graphaddr); else BuildLineRAM160A(graphaddr);
                        break;
                    case 1:
                        continue;
                    case 2:
                        if (WM) BuildLineRAM320B(graphaddr); else BuildLineRAM320D(graphaddr);
                        break;
                    case 3:
                        if (WM) BuildLineRAM320C(graphaddr); else BuildLineRAM320A(graphaddr);
                        break;
                }
            }
        }

        void BuildLineRAM160A(ushort graphaddr)
        {
            var indbytes = (INDMode && CWidth) ? 2 : 1;
            var hpos = HPOS << 1;
            var dataaddr = (ushort)(graphaddr + (Offset << 8));

            for (var i=0; i < Width; i++)
            {
                if (INDMode)
                {
                    dataaddr = WORD(DmaRead(graphaddr + i), Registers[CHARBASE] + Offset);
                }

                for (var j=0; j < indbytes; j++)
                {
                    if (Holey == 0x02 && ((dataaddr & 0x9000) == 0x9000) || Holey == 0x01 && ((dataaddr & 0x8800) == 0x8800))
                    {
                        hpos += 8;
                        dataaddr++;
                        AssertDebug(!Kangaroo);
                        continue;
                    }

                    int d = DmaRead(dataaddr++);

                    var c = (d & 0xc0) >> 6;
                    if (c != 0)
                    {
                        var val = (byte)(PaletteNo | c);
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = val;
                    }
                    AssertDebug(c != 0 || c == 0 && !Kangaroo);

                    hpos += 2;

                    c = (d & 0x30) >> 4;
                    if (c != 0)
                    {
                        var val = (byte)(PaletteNo | c);
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = val;
                    }
                    AssertDebug(c != 0 || c == 0 && !Kangaroo);

                    hpos += 2;

                    c = (d & 0x0c) >> 2;
                    if (c != 0)
                    {
                        var val = (byte)(PaletteNo | c);
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = val;
                    }
                    AssertDebug(c != 0 || c == 0 && !Kangaroo);

                    hpos += 2;

                    c = d & 0x03;
                    if (c != 0)
                    {
                        var val = (byte)(PaletteNo | c);
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = val;
                    }
                    AssertDebug(c != 0 || c == 0 && !Kangaroo);

                    hpos += 2;
                }
            }
        }

        void BuildLineRAM160B(ushort graphaddr)
        {
            var indbytes = (INDMode && CWidth) ? 2 : 1;
            var hpos = HPOS << 1;
            var dataaddr = (ushort)(graphaddr + (Offset << 8));

            for (var i = 0; i < Width; i++)
            {
                if (INDMode)
                {
                    dataaddr = WORD(DmaRead(graphaddr + i), Registers[CHARBASE] + Offset);
                }

                for (var j=0; j < indbytes; j++)
                {
                    if (Holey == 0x02 && ((dataaddr & 0x9000) == 0x9000) || Holey == 0x01 && ((dataaddr & 0x8800) == 0x8800))
                    {
                        hpos += 4;
                        dataaddr++;
                        continue;
                    }

                    int d = DmaRead(dataaddr++);

                    var c = (d & 0xc0) >> 6;
                    if (c != 0)
                    {
                        var p = ((PaletteNo >> 2) & 0x04) | ((d & 0x0c) >> 2);
                        var val = (byte)((p << 2) | c);
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = val;
                    }
                    else if (Kangaroo)
                    {
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = 0;
                    }

                    hpos += 2;

                    c = (d & 0x30) >> 4;
                    if (c != 0)
                    {
                        var p = ((PaletteNo >> 2) & 0x04) | (d & 0x03);
                        var val = (byte)((p << 2) | c);
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = val;
                    }
                    else if (Kangaroo)
                    {
                        LineRAM[hpos & 0x1ff] = LineRAM[(hpos+1) & 0x1ff] = 0;
                    }

                    hpos += 2;
                }
            }
        }

        void BuildLineRAM320A(ushort graphaddr)
        {
            var color = (byte)(PaletteNo | 2);
            var hpos = HPOS << 1;
            var dataaddr = (ushort)(graphaddr + (Offset << 8));

            AssertDebug(!CWidth);

            for (var i = 0; i < Width; i++)
            {
                if (INDMode)
                {
                    dataaddr = WORD(DmaRead(graphaddr + i), Registers[CHARBASE] + Offset);
                }

                if (Holey == 0x02 && ((dataaddr & 0x9000) == 0x9000) || Holey == 0x01 && ((dataaddr & 0x8800) == 0x8800))
                {
                    hpos += 8;
                    dataaddr++;
                    continue;
                }

                int d = DmaRead(dataaddr++);

                if ((d & 0x80) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x40) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x20) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x10) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x08) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x04) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x02) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x01) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;
            }
        }

        void BuildLineRAM320B(ushort graphaddr)
        {
            var indbytes = (INDMode && CWidth) ? 2 : 1;
            var hpos = HPOS << 1;
            var dataaddr = (ushort)(graphaddr + (Offset << 8));

            for (var i = 0; i < Width; i++)
            {
                if (INDMode)
                {
                    dataaddr = WORD(DmaRead(graphaddr + i), Registers[CHARBASE] + Offset);
                }

                for (var j=0; j < indbytes; j++)
                {
                    if (Holey == 0x02 && ((dataaddr & 0x9000) == 0x9000) || Holey == 0x01 && ((dataaddr & 0x8800) == 0x8800))
                    {
                        hpos += 4;
                        dataaddr++;
                        continue;
                    }

                    int d = DmaRead(dataaddr++);

                    var c = ((d & 0x80) >> 6) | ((d & 0x08) >> 3);
                    if (c != 0)
                    {
                        if ((d & 0xc0) != 0 || Kangaroo)
                            LineRAM[hpos & 0x1ff] = (byte)(PaletteNo | c);
                    }
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;
                    else if ((d & 0xcc) != 0)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x40) >> 5) | ((d & 0x04) >> 2);
                    if (c != 0)
                    {
                        if ((d & 0xc0) != 0 || Kangaroo)
                            LineRAM[hpos & 0x1ff] = (byte)(PaletteNo | c);
                    }
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;
                    else if ((d & 0xcc) != 0)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x20) >> 4) | ((d & 0x02) >> 1);
                    if (c != 0)
                    {
                        if ((d & 0x30) != 0 || Kangaroo)
                            LineRAM[hpos & 0x1ff] = (byte)(PaletteNo | c);
                    }
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;
                    else if ((d & 0x33) != 0)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x10) >> 3) | (d & 0x01);
                    if (c != 0)
                    {
                        if ((d & 0x30) != 0 || Kangaroo)
                            LineRAM[hpos & 0x1ff] = (byte)(PaletteNo | c);
                    }
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;
                    else if ((d & 0x33) != 0)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;
                }
            }
        }

        void BuildLineRAM320C(ushort graphaddr)
        {
            var hpos = HPOS << 1;
            var dataaddr = (ushort)(graphaddr + (Offset << 8));

            AssertDebug(!CWidth);

            for (var i = 0; i < Width; i++)
            {
                if (INDMode)
                {
                    dataaddr = WORD(DmaRead(graphaddr + i), Registers[CHARBASE] + Offset);
                }

                if (Holey == 0x02 && ((dataaddr & 0x9000) == 0x9000) || Holey == 0x01 && ((dataaddr & 0x8800) == 0x8800))
                {
                    hpos += 4;
                    dataaddr++;
                    continue;
                }

                int d = DmaRead(dataaddr++);

                var color = (byte)(((((d & 0x0c) >> 2) | ((PaletteNo >> 2) & 0x04)) << 2) | 2);

                if ((d & 0x80) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x40) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                color = (byte)((((d & 0x03) | ((PaletteNo >> 2) & 0x04)) << 2) | 2);

                if ((d & 0x20) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;

                if ((d & 0x10) != 0)
                    LineRAM[hpos & 0x1ff] = color;
                else if (Kangaroo)
                    LineRAM[hpos & 0x1ff] = 0;

                hpos++;
            }
        }

        void BuildLineRAM320D(ushort graphaddr)
        {
            var indbytes = (INDMode && CWidth) ? 2 : 1;
            var hpos = HPOS << 1;
            var dataaddr = (ushort)(graphaddr + (Offset << 8));

            for (var i = 0; i < Width; i++)
            {
                if (INDMode)
                {
                    dataaddr = WORD(DmaRead(graphaddr + i), Registers[CHARBASE] + Offset);
                }

                for (var j=0; j < indbytes; j++)
                {
                    if (Holey == 0x02 && ((dataaddr & 0x9000) == 0x9000) || Holey == 0x01 && ((dataaddr & 0x8800) == 0x8800))
                    {
                        hpos += 8;
                        dataaddr++;
                        continue;
                    }

                    int d = DmaRead(dataaddr++);

                    var c = ((d & 0x80) >> 6) | (((PaletteNo >> 2) & 2) >> 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x40) >> 5) | ((PaletteNo >> 2) & 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x20) >> 4) | (((PaletteNo >> 2) & 2) >> 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x10) >> 3) | ((PaletteNo >> 2) & 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x08) >> 2) | (((PaletteNo >> 2) & 2) >> 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x04) >> 1) | ((PaletteNo >> 2) & 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = (d & 0x02) | (((PaletteNo >> 2) & 2) >> 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;

                    c = ((d & 0x01) << 1) | ((PaletteNo >> 2) & 1);
                    if (c != 0)
                        LineRAM[hpos & 0x1ff] = (byte)((PaletteNo & 0x10) | c);
                    else if (Kangaroo)
                        LineRAM[hpos & 0x1ff] = 0;

                    hpos++;
                }
            }
        }

        void OutputLineRAM()
        {
            var fbi = ((Scanline + 1) * M.FrameBuffer.VisiblePitch) % M.FrameBuffer.VideoBufferByteLength;

			for (int i = 0; i < M.FrameBuffer.VisiblePitch; i++)
            {
				var colorIndex = LineRAM[i];
				M.FrameBuffer.VideoBuffer[fbi++] = Registers[BACKGRND + ((colorIndex & 3) == 0 ? 0 : colorIndex)];
				if (fbi == M.FrameBuffer.VideoBufferByteLength)
					fbi = 0;
            }

            for (var i = 0; i < LineRAM.Length; i++)
            {
                LineRAM[i] = 0;
            }
        }

        #endregion

        #region Maria Peek

        byte peek(ushort addr)
        {
            addr &= 0x3f;
            var mi = M.InputState;

            switch(addr)
            {
                case MSTAT:
                    var sl = Scanline;
                    return (sl < FirstVisibleScanline || sl >= LastVisibleScanline)
                         ? (byte)0x80  // VBLANK ON
                         : (byte)0;    // VBLANK OFF
                case INPT0: return mi.SampleCapturedControllerActionState(0, ControllerAction.Trigger)  ? (byte)0x80 : (byte)0;     // player1,button R
                case INPT1: return mi.SampleCapturedControllerActionState(0, ControllerAction.Trigger2) ? (byte)0x80 : (byte)0;     // player1,button L
                case INPT2: return mi.SampleCapturedControllerActionState(1, ControllerAction.Trigger)  ? (byte)0x80 : (byte)0;     // player2,button R
                case INPT3: return mi.SampleCapturedControllerActionState(1, ControllerAction.Trigger2) ? (byte)0x80 : (byte)0;     // player2,button L
                case INPT4: return SampleINPTLatched(4)                                                 ? (byte)0    : (byte)0x80;  // player1,button L/R
                case INPT5: return SampleINPTLatched(5)                                                 ? (byte)0    : (byte)0x80;  // player2,button L/R
                default:
                    LogDebug("Maria: Unhandled peek at ${0:x4}, PC=${1:x4}", addr, M.CPU.PC);
                    var retval = Registers[addr];
                    return retval;
            }
        }

        #endregion

        #region Maria Poke

        void poke(ushort addr, byte data)
        {
            addr &= 0x3f;

            switch (addr)
            {
                // INPUT PORT CONTROL
                // Only the first four bits of INPTCTRL are used:
                //     D0: lock mode (after this bit has been set high, no more mode changes can be done until the console is turned off)
                //     D1: 0=disable MARIA (only RIOT RAM is available); 1=enable MARIA (also enables system RAM)
                //     D2: 0=enable BIOS at $8000-$FFFF (actually NTSC only uses 4KB and PAL uses 16KB); 1=disable BIOS and enable cartridge
                //     D3: 0=disable TIA video pull-ups (video output is MARIA instead of TIA); 1=enable TIA video pull-ups (video output is TIA instead of MARIA)
                //
                case INPTCTRL:
                    if (CtrlLock)
                    {
                        Log("Maria: INPTCTRL: LOCKED: Ignoring: ${0:x2}, PC=${1:x4}", data, M.CPU.PC);
                        break;
                    }

                    CtrlLock        = (data & (1 << 0)) != 0;
                    var mariaEnable = (data & (1 << 1)) != 0;
                    var biosDisable = (data & (1 << 2)) != 0;
                    var tiaopEnable = (data & (1 << 3)) != 0;

                    Log("Maria: INPTCTRL: ${0:x2}, PC=${1:x4}, lockMode={2}, mariaEnable={3} biosDisable={4} tiaOutput={5}",
                        data, M.CPU.PC, CtrlLock, mariaEnable, biosDisable, tiaopEnable);

                    if (biosDisable)
                    {
                        M.SwapOutBIOS();
                    }
                    else
                    {
                        M.SwapInBIOS();
                    }
                    break;
                case WSYNC:
                    // Request a CPU preemption to service the delay request
                    M.CPU.EmulatorPreemptRequest = true;
                    break;
                case CTRL:
                    ColorKill = (data & 0x80) != 0;
                    DMAEnabled = (data & 0x60) == 0x40;
                    CWidth = (data & 0x10) != 0;
                    BCntl = (data & 0x08) != 0;
                    Kangaroo = (data & 0x04) != 0;
                    RM = (byte)(data & 0x03);
                    break;
                case MSTAT:
                    break;
                case CHARBASE:
                case DPPH:
                case DPPL:
                    Registers[addr] = data;
                    break;
                case BACKGRND:
                case P0C1:
                case P0C2:
                case P0C3:
                case P1C1:
                case P1C2:
                case P1C3:
                case P2C1:
                case P2C2:
                case P2C3:
                case P3C1:
                case P3C2:
                case P3C3:
                case P4C1:
                case P4C2:
                case P4C3:
                case P5C1:
                case P5C2:
                case P5C3:
                case P6C1:
                case P6C2:
                case P6C3:
                case P7C1:
                case P7C2:
                case P7C3:
                    Registers[addr] = data;
                    break;
                case AUDC0:
                case AUDC1:
                case AUDF0:
                case AUDF1:
                case AUDV0:
                case AUDV1:
                    TIASound.Update(addr, data);
                    break;
                case OFFSET:
                    Log("Maria: OFFSET: ROM wrote ${0:x2}, PC=${1:x4} (reserved for future expansion)", data, M.CPU.PC);
                    break;
                default:
                    Registers[addr] = data;
                    LogDebug("Maria: Unhandled poke:${0:x4} w/${1:x2}, PC=${2:x4}", addr, data, M.CPU.PC);
                    break;
            }
        }

        #endregion

        #region Input Helpers

        bool SampleINPTLatched(int inpt)
        {
            var mi = M.InputState;
            var playerNo = inpt - 4;

            switch (playerNo == 0 ? mi.LeftControllerJack : mi.RightControllerJack)
            {
                case Controller.Joystick:
                    return mi.SampleCapturedControllerActionState(playerNo, ControllerAction.Trigger);
                case Controller.ProLineJoystick:
                    var portbline = 4 << (playerNo << 1);
                    if ((M.PIA.DDRB & portbline) != 0 && (M.PIA.WrittenPortB & portbline) == 0)
                        return false;
                    return mi.SampleCapturedControllerActionState(playerNo, ControllerAction.Trigger)
                        || mi.SampleCapturedControllerActionState(playerNo, ControllerAction.Trigger2);
                case Controller.Lightgun:

                    // This is one area where always running fixed at the faster CPU frequency creates emulation challenges.
                    // Fortunately since lightgun sampling is a dedicated activity on a frame, the job of compensating is tractable.

                    // Track the number of samples this frame, the time of the first sample, and capture the lightgun location.
                    if (_lightgunFirstSampleCpuClock == 0)
                    {
                        _lightgunFirstSampleCpuClock = M.CPU.Clock;
                        _lightgunFrameSamples = 0;
                        mi.SampleCapturedLightGunPosition(playerNo, out _lightgunSampledScanline, out _lightgunSampledVisibleHpos);
                    }
                    _lightgunFrameSamples++;

                    // Magic Adjustment Factor
                    // Seems sufficient to account for the timing impact of successive lightrun reads (i.e., 'slow' memory accesses.)
                    // Obtained through through trial-and-error.
                    const float magicAdjustmentFactor = 2.135f;

                    var firstLightgunSampleMariaFrameClock = (int)((_lightgunFirstSampleCpuClock - _startOfFrameCpuClock) << 2);
                    var mariaClocksSinceFirstLightgunSample = (int)((M.CPU.Clock - _lightgunFirstSampleCpuClock) << 2);
                    var adjustmentMariaClocks = (int)Math.Round(_lightgunFrameSamples * magicAdjustmentFactor);
                    var actualMariaFrameClock = firstLightgunSampleMariaFrameClock + mariaClocksSinceFirstLightgunSample + adjustmentMariaClocks;
                    var actualScanline = actualMariaFrameClock / 456;
                    var actualHpos = actualMariaFrameClock % 456;

                    // Lightgun sampling looks intended to begin at the start of the scanline.
                    // Compensate with another magic constant since we're always off by a fixed amount.
                    actualHpos -= 62;
                    if (actualHpos < 0)
                    {
                        actualHpos += 456;
                        actualScanline--;
                    }

                    var sampledScanline = _lightgunSampledScanline;
                    var sampledVisibleHpos = _lightgunSampledVisibleHpos;

                    // Seems reasonable the gun sees more than a single pixel (more like a circle or oval) and triggers sooner accordingly.
                    // These adjustments were obtained through trial-and-error.
                    if (_isPal)
                    {
                        sampledScanline -= 19;
                    }
                    else
                    {
                        sampledScanline -= 16;
                        sampledVisibleHpos += 4;
                    }
                    return (actualScanline >= sampledScanline) && (actualHpos >= (sampledVisibleHpos + 136 /* HBLANK clocks */));
            }
            return false;
        }

        #endregion

        #region Serialization Members

        public Maria(DeserializationContext input, Machine7800 m, int scanlines)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (m == null)
                throw new ArgumentNullException("m");

            M = m;
            InitializeVisibleScanlineValues(scanlines);
            TIASound = new TIASound(input, M, CPU_TICKS_PER_AUDIO_SAMPLE);

            var version = input.CheckVersion(1, 2);
            LineRAM = input.ReadExpectedBytes(512);
            if (version == 1)
            {
                // formerly persisted values, MariaPalette[8,4]
                for (var i = 0; i < 32; i++) 
                    input.ReadByte();
            }
            Registers = input.ReadExpectedBytes(0x40);
            if (version == 1)
            {
                // formerly persisted value, Scanline
                input.ReadInt32();
            }
            switch (version)
            {
                case 1:
                    WM = (input.ReadByte() != 0);
                    break;
                case 2:
                    WM = input.ReadBoolean();
                    break;
            }
            DLL = input.ReadUInt16();
            DL = input.ReadUInt16();
            Offset = input.ReadInt32();
            Holey = input.ReadInt32();
            Width = input.ReadInt32();
            HPOS = input.ReadByte();
            PaletteNo = input.ReadByte();
            INDMode = input.ReadBoolean();
            if (version == 1)
            {
                // formerly persisted value (DLI)
                input.ReadBoolean();
            }
            CtrlLock = input.ReadBoolean();
            if (version == 1)
            {
                // formerly persisted value (VBLANK)
                input.ReadByte();
            }
            DMAEnabled = input.ReadBoolean();
            if (version == 1)
            {
                // formerly persisted value (DMAOn)
                input.ReadBoolean();
            }
            ColorKill = input.ReadBoolean();
            CWidth = input.ReadBoolean();
            BCntl = input.ReadBoolean();
            Kangaroo = input.ReadBoolean();
            RM = input.ReadByte();
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.Write(TIASound);

            output.WriteVersion(2);
            output.Write(LineRAM);
            output.Write(Registers);
            output.Write(WM);
            output.Write(DLL);
            output.Write(DL);
            output.Write(Offset);
            output.Write(Holey);
            output.Write(Width);
            output.Write(HPOS);
            output.Write((byte)PaletteNo);
            output.Write(INDMode);
            output.Write(CtrlLock);
            output.Write(DMAEnabled);
            output.Write(ColorKill);
            output.Write(CWidth);
            output.Write(BCntl);
            output.Write(Kangaroo);
            output.Write(RM);
        }

        #endregion

        #region Helpers

        void ConsumeNextDLLEntry()
        {
            // Display List List (DLL) entry
            var dll0 = DmaRead(DLL++);   // DLI, Holey, Offset
            var dll1 = DmaRead(DLL++);   // High DL address
            var dll2 = DmaRead(DLL++);   // Low DL address

            var dli = (dll0 & 0x80) != 0;
            Holey = (dll0 & 0x60) >> 5;
            Offset = dll0 & 0x0f;

            // Update current Display List (DL)
            DL = WORD(dll2, dll1);

            if (dli)
            {
                M.CPU.NMIInterruptRequest = true;

                // DMA TIMING: One tick between DMA Shutdown and DLI
                _dmaClocks += 1;
            }
        }

        void InitializeVisibleScanlineValues(int scanlines)
        {
            switch (scanlines)
            {
                case 262: // NTSC
                    FirstVisibleScanline = 11;
                    LastVisibleScanline = FirstVisibleScanline + 242;
                    _isPal = false;
                    break;
                case 312: // PAL
                    FirstVisibleScanline = 11;
                    LastVisibleScanline = FirstVisibleScanline + 292;
                    _isPal = true;
                    break;
                default:
                    throw new ArgumentException("scanlines must be 262 or 312.");
            }
        }

        void Log(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        // convenience overload
        static ushort WORD(int lsb, int msb)
        {
            return WORD((byte)lsb, (byte)msb);
        }

        static ushort WORD(byte lsb, byte msb)
        {
            return (ushort)(lsb | msb << 8);
        }

        // convenience overload
        byte DmaRead(int addr)
        {
            return DmaRead((ushort)addr);
        }

        byte DmaRead(ushort addr)
        {
#if DEBUG
            if (addr < 0x1800)
                LogDebug("Maria: Questionable DMA read at ${0:x4} by PC=${1:x4}", addr, M.CPU.PC);
#endif
            return M.Mem[addr];
        }

        [System.Diagnostics.Conditional("DEBUG")]
        void LogDebug(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        void AssertDebug(bool cond)
        {
            if (!cond)
                System.Diagnostics.Debugger.Break();
        }

        #endregion
    }
}
