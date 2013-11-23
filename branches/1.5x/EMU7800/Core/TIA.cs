/*
 * TIA.cs
 *
 * The Television Interface Adaptor device.
 *
 * Copyright © 2003-2008 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    #region Collision Flags

    [Flags]
    public enum TIACxFlags
    {
        PF = 1 << 0,
        BL = 1 << 1,
        M0 = 1 << 2,
        M1 = 1 << 3,
        P0 = 1 << 4,
        P1 = 1 << 5
    };

    [Flags]
    public enum TIACxPairFlags
    {
        M0P1 = 1 << 0,
        M0P0 = 1 << 1,
        M1P0 = 1 << 2,
        M1P1 = 1 << 3,
        P0PF = 1 << 4,
        P0BL = 1 << 5,
        P1PF = 1 << 6,
        P1BL = 1 << 7,
        M0PF = 1 << 8,
        M0BL = 1 << 9,
        M1PF = 1 << 10,
        M1BL = 1 << 11,
        BLPF = 1 << 12,
        P0P1 = 1 << 13,
        M0M1 = 1 << 14
    };

    #endregion

    public sealed class TIA : IDevice
    {
        #region Constants

        const int
            VSYNC   = 0x00, // Write: vertical sync set-clear (D1)
            VBLANK  = 0x01, // Write: vertical blank set-clear (D7-6,D1)
            WSYNC   = 0x02, // Write: wait for leading edge of hrz. blank (strobe)
            RSYNC   = 0x03, // Write: reset hrz. sync counter (strobe)
            NUSIZ0  = 0x04, // Write: number-size player-missle 0 (D5-0)
            NUSIZ1  = 0x05, // Write: number-size player-missle 1 (D5-0)
            COLUP0  = 0x06, // Write: color-lum player 0 (D7-1)
            COLUP1  = 0x07, // Write: color-lum player 1 (D7-1)
            COLUPF  = 0x08, // Write: color-lum playfield (D7-1)
            COLUBK  = 0x09, // Write: color-lum background (D7-1)
            CTRLPF  = 0x0a, // Write: cntrl playfield ballsize & coll. (D5-4,D2-0)
            REFP0   = 0x0b, // Write: reflect player 0 (D3)
            REFP1   = 0x0c, // Write: reflect player 1 (D3)
            PF0     = 0x0d, // Write: playfield register byte 0 (D7-4)
            PF1     = 0x0e, // Write: playfield register byte 1 (D7-0)
            PF2     = 0x0f, // Write: playfield register byte 2 (D7-0)
            RESP0   = 0x10, // Write: reset player 0 (strobe)
            RESP1   = 0x11, // Write: reset player 1 (strobe)
            RESM0   = 0x12, // Write: reset missle 0 (strobe)
            RESM1   = 0x13, // Write: reset missle 1 (strobe)
            RESBL   = 0x14, // Write: reset ball (strobe)
            AUDC0   = 0x15, // Write: audio control 0 (D3-0)
            AUDC1   = 0x16, // Write: audio control 1 (D4-0)
            AUDF0   = 0x17, // Write: audio frequency 0 (D4-0)
            AUDF1   = 0x18, // Write: audio frequency 1 (D3-0)
            AUDV0   = 0x19, // Write: audio volume 0 (D3-0)
            AUDV1   = 0x1a, // Write: audio volume 1 (D3-0)
            GRP0    = 0x1b, // Write: graphics player 0 (D7-0)
            GRP1    = 0x1c, // Write: graphics player 1 (D7-0)
            ENAM0   = 0x1d, // Write: graphics (enable) missle 0 (D1)
            ENAM1   = 0x1e, // Write: graphics (enable) missle 1 (D1)
            ENABL   = 0x1f, // Write: graphics (enable) ball (D1)
            HMP0    = 0x20, // Write: horizontal motion player 0 (D7-4)
            HMP1    = 0x21, // Write: horizontal motion player 1 (D7-4)
            HMM0    = 0x22, // Write: horizontal motion missle 0 (D7-4)
            HMM1    = 0x23, // Write: horizontal motion missle 1 (D7-4)
            HMBL    = 0x24, // Write: horizontal motion ball (D7-4)
            VDELP0  = 0x25, // Write: vertical delay player 0 (D0)
            VDELP1  = 0x26, // Write: vertical delay player 1 (D0)
            VDELBL  = 0x27, // Write: vertical delay ball (D0)
            RESMP0  = 0x28, // Write: reset missle 0 to player 0 (D1)
            RESMP1  = 0x29, // Write: reset missle 1 to player 1 (D1)
            HMOVE   = 0x2a, // Write: apply horizontal motion (strobe)
            HMCLR   = 0x2b, // Write: clear horizontal motion registers (strobe)
            CXCLR   = 0x2c, // Write: clear collision latches (strobe)

            CXM0P   = 0x00, // Read collision: D7=(M0,P1); D6=(M0,P0)
            CXM1P   = 0x01, // Read collision: D7=(M1,P0); D6=(M1,P1)
            CXP0FB  = 0x02, // Read collision: D7=(P0,PF); D6=(P0,BL)
            CXP1FB  = 0x03, // Read collision: D7=(P1,PF); D6=(P1,BL)
            CXM0FB  = 0x04, // Read collision: D7=(M0,PF); D6=(M0,BL)
            CXM1FB  = 0x05, // Read collision: D7=(M1,PF); D6=(M1,BL)
            CXBLPF  = 0x06, // Read collision: D7=(BL,PF); D6=(unused)
            CXPPMM  = 0x07, // Read collision: D7=(P0,P1); D6=(M0,M1)
            INPT0   = 0x08, // Read pot port: D7
            INPT1   = 0x09, // Read pot port: D7
            INPT2   = 0x0a, // Read pot port: D7
            INPT3   = 0x0b, // Read pot port: D7
            INPT4   = 0x0c, // Read P1 joystick trigger: D7
            INPT5   = 0x0d; // Read P2 joystick trigger: D7

        const int CPU_TICKS_PER_AUDIO_SAMPLE = 38;

        #endregion

        #region Data Structures

        readonly byte[] RegW = new byte[0x40];
        readonly MachineBase M;
        readonly TIASound TIASound;

        delegate void PokeOpTyp(ushort addr, byte data);

        PokeOpTyp[] PokeOp;

        #endregion

        #region Position Counters

        // backing fields for properties--dont reference directly
        int _HSync, _P0, _P1, _M0, _M1, _BL, _HMoveCounter;

        // Horizontal Sync Counter
        // this represents HSync of the last rendered CLK
        // has period of 57 counts, 0-56, at 1/4 CLK (57*4=228 CLK)
        // provide all horizontal timing for constructing a valid TV signal
        // other movable object counters can be reset out-of-phase with HSync, hence %228 and not %57
        int HSync
        {
            get { return _HSync; }
            set { _HSync = value % 228; }
        }

        // determines the difference between HSync and PokeOpHSync
        int PokeOpHSyncDelta
        {
            get { return (int)(Clock - LastEndClock); }
        }

        // this represents the current HSync
        int PokeOpHSync
        {
            get { return (HSync + PokeOpHSyncDelta) % 228; }
        }

        // scanline last rendered to
        int ScanLine;

        // current position in the frame buffer
        int FrameBufferIndex;

        // bytes are batched here for writing to the FrameBuffer
        //BufferElement FrameBufferElement;

        // signals when to start an HMOVE
        ulong StartHMOVEClock;

        // indicates where in the HMOVE operation it is
        int HMoveCounter
        {
            get { return _HMoveCounter; }
            set { _HMoveCounter = value < 0 ? -1 : value & 0xf; }
        }

        // true when there is an HMOVE executing on the current scanline
        bool HMoveLatch;

        // represents the TIA color clock (CLK)
        // computed off of the CPU clock, but in real life, the CPU is driven by the color CLK signal
        ulong Clock
        {
            get { return 3 * M.CPU.Clock; }
        }

        // represents the first CLK of the unrendered scanline segment
        ulong StartClock;

        // represents the last CLK of the previously rendered scanline segment
        ulong LastEndClock
        {
            get { return StartClock - 1; }
        }

        #endregion

        #region Player 0 Object

        // Player 0 Horizontal Position Counter
        // has period of 40 counts, 0-39, at 1/4 CLK (40*4=160 CLK=visible scanline length)
        // player position counter controls the position of the respective player graphics object on each scanline
        // can be reset out-of-phase with HSync, hence %160 and not %40
        int P0
        {
            get { return _P0; }
            set { _P0 = value % 160; }
        }

        // HMOVE "more motion required" latch
        bool P0mmr;

        // Player 0 graphics registers
        byte EffGRP0, OldGRP0;

        // Player 0 stretch mode
        int P0type;

        // 1=currently suppressing copy 1 on Player 0
        int P0suppress;

        #endregion

        #region Player 1 Object

        // Player 1 Horizontal Position Counter (identical to P0)
        int P1
        {
            get { return _P1; }
            set { _P1 = value % 160; }
        }

        // HMOVE "more motion required" latch
        bool P1mmr;

        // Player 1 graphics registers
        byte EffGRP1, OldGRP1;

        // Player 1 stretch mode
        int P1type;

        // 1=currently suppressing copy 1 on Player 1
        int P1suppress;

        #endregion

        #region Missile 0 Object

        // Missile 0 Horizontal Position Counter
        // similar to player position counters
        int M0
        {
            get { return _M0; }
            set { _M0 = value % 160; }
        }

        // HMOVE "more motion required" latch
        bool M0mmr;

        int M0type, M0size;

        bool m0on;

        #endregion

        #region Missle 1 Object

        // Missile 1 Horizontal Position Counter (identical to M0)
        int M1
        {
            get { return _M1; }
            set { _M1 = value % 160; }
        }

        // HMOVE "more motion required" latch
        bool M1mmr;

        int M1type, M1size;

        bool m1on;

        #endregion

        #region Ball Object

        // Ball Horizontal Position Counter
        // similar to player position counters
        int BL
        {
            get { return _BL; }
            set { _BL = value % 160; }
        }

        // HMOVE "more motion required" latch
        bool BLmmr;

        bool OldENABL;

        int BLsize;

        bool blon;

        #endregion

        #region Misc

        uint PF210;
        int PFReflectionState;

        // color-luminance for background, playfield, player 0, player 1
        byte colubk, colupf, colup0, colup1;

        bool vblankon, scoreon, pfpriority;

        bool DumpEnabled;
        ulong DumpDisabledCycle;

        TIACxPairFlags Collisions;

        #endregion

        #region Public Members

        public int WSYNCDelayClocks { get; set; }
        public bool EndOfFrame { get; private set; }

        public void Reset()
        {
            for (var i = 0; i < RegW.Length; i++)
            {
                RegW[i] = 0;
            }
            vblankon = scoreon = pfpriority = false;
            m0on = m1on = blon = false;
            colubk = colupf = colup0 = colup1 = 0;
            PFReflectionState = 0;

            StartClock = Clock;
            HSync = -1;
            P0 = P1 = M0 = M1 = BL = -1;
            P0mmr = P1mmr = M0mmr = M1mmr = BLmmr = false;
            StartHMOVEClock = ulong.MaxValue;
            HMoveCounter = -1;

            FrameBufferIndex = 0;

            TIASound.Reset();

            Log("{0} reset", this);
        }

        public override String ToString()
        {
            return "TIA 1A";
        }

        public void StartFrame()
        {
            WSYNCDelayClocks = 0;
            EndOfFrame = false;
            ScanLine = 0;
            FrameBufferIndex %= 160;
            RenderFromStartClockTo(Clock);
            TIASound.StartFrame();
        }

        public byte this[ushort addr]
        {
            get { return peek(addr); }
            set { poke(addr, value); }
        }

        public void EndFrame()
        {
            TIASound.EndFrame();
        }

        #endregion

        #region Constructors

        private TIA()
        {
            BuildPokeOpTable();
        }

        public TIA(MachineBase m) : this()
        {
            if (m == null)
                throw new ArgumentNullException("m");

            M = m;
            TIASound = new TIASound(M, CPU_TICKS_PER_AUDIO_SAMPLE);
        }

        #endregion

        #region Scanline Segment Renderer

        void RenderFromStartClockTo(ulong endClock)
        {

        RenderClock:
            if (StartClock >= endClock)
                return;

            ++HSync;

            if (StartClock == StartHMOVEClock)
            {
                // turn on HMOVE
                HMoveLatch = true;
                HMoveCounter = 0xf;
                P0mmr = P1mmr = M0mmr = M1mmr = BLmmr = true;
            }
            else if (HSync == 0)
            {
                // just wrapped around, clear late HBLANK
                HMoveLatch = false;
            }

            // position counters are incremented during the visible portion of the scanline
            if (HSync >= 68 + (HMoveLatch ? 8 : 0))
            {
                ++P0; ++P1; ++M0; ++M1; ++BL;
            }

            // HMOVE compare, once every 1/4 CLK when on
            if (HMoveCounter >= 0 && (HSync & 3) == 0)
            {
                if (((HMoveCounter ^ RegW[HMP0]) & 0xf) == 0xf) P0mmr = false;
                if (((HMoveCounter ^ RegW[HMP1]) & 0xf) == 0xf) P1mmr = false;
                if (((HMoveCounter ^ RegW[HMM0]) & 0xf) == 0xf) M0mmr = false;
                if (((HMoveCounter ^ RegW[HMM1]) & 0xf) == 0xf) M1mmr = false;
                if (((HMoveCounter ^ RegW[HMBL]) & 0xf) == 0xf) BLmmr = false;
                if (HMoveCounter >= 0) HMoveCounter--;
            }

            // HMOVE increment, once every 1/4 CLK, 2 CLK after first compare when on
            if (HMoveCounter < 0xf && (HSync & 3) == 2)
            {
                if (HSync < 68 + (HMoveLatch ? 8 : 0))
                {
                    if (P0mmr) ++P0;
                    if (P1mmr) ++P1;
                    if (M0mmr) ++M0;
                    if (M1mmr) ++M1;
                    if (BLmmr) ++BL;
                }
            }

            if (HSync == 68 + 76) PFReflectionState = RegW[CTRLPF] & 0x01;

            var fbyte = (byte)0;
            var fbyte_colupf = colupf;
            TIACxFlags cxflags = 0;

            if (vblankon || HSync < 68 + (HMoveLatch ? 8 : 0)) goto WritePixel;

            fbyte = colubk;

            var colupfon = false;
            if ((PF210 & TIATables.PFMask[PFReflectionState][HSync - 68]) != 0)
            {
                if (scoreon) fbyte_colupf = (HSync - 68) < 80 ? colup0 : colup1;
                colupfon = true;
                cxflags |= TIACxFlags.PF;
            }
            if (blon && BL >= 0 && TIATables.BLMask[BLsize][BL])
            {
                colupfon = true;
                cxflags |= TIACxFlags.BL;
            }
            if (!pfpriority && colupfon)
            {
                fbyte = fbyte_colupf;
            }
            if (m1on && M1 >= 0 && TIATables.MxMask[M1size][M1type][M1])
            {
                fbyte = colup1;
                cxflags |= TIACxFlags.M1;
            }
            if (P1 >= 0 && (TIATables.PxMask[P1suppress][P1type][P1] & EffGRP1) != 0)
            {
                fbyte = colup1;
                cxflags |= TIACxFlags.P1;
            }
            if (m0on && M0 >= 0 && TIATables.MxMask[M0size][M0type][M0])
            {
                fbyte = colup0;
                cxflags |= TIACxFlags.M0;
            }
            if (P0 >= 0 && (TIATables.PxMask[P0suppress][P0type][P0] & EffGRP0) != 0)
            {
                fbyte = colup0;
                cxflags |= TIACxFlags.P0;
            } 
            if (pfpriority && colupfon)
            {
                fbyte = fbyte_colupf;
            }

        WritePixel:
            Collisions |= TIATables.CollisionMask[(int)cxflags];

            if (HSync >= 68)
            {
				M.FrameBuffer.VideoBuffer[FrameBufferIndex++] = fbyte;
				if (FrameBufferIndex == M.FrameBuffer.VideoBufferByteLength)
					FrameBufferIndex = 0;
                if (HSync == 227)
                    ScanLine++;
            }

            if (P0 >= 156) P0suppress = 0;
            if (P1 >= 156) P1suppress = 0;

            // denote this CLK has been completed by incrementing to the next
            StartClock++;

            goto RenderClock;
        }

        #endregion

        #region TIA Peek

        byte peek(ushort addr)
        {
            var retval = 0;
            addr &= 0xf;

            RenderFromStartClockTo(Clock);

            switch (addr)
            {
                case CXM0P:
                    retval |= ((Collisions & TIACxPairFlags.M0P1) != 0 ? 0x80 : 0);
                    retval |= ((Collisions & TIACxPairFlags.M0P0) != 0 ? 0x40 : 0);
                    break;
                case CXM1P:
                    retval |= ((Collisions & TIACxPairFlags.M1P0) != 0 ? 0x80 : 0);
                    retval |= ((Collisions & TIACxPairFlags.M1P1) != 0 ? 0x40 : 0);
                    break;
                case CXP0FB:
                    retval |= ((Collisions & TIACxPairFlags.P0PF) != 0 ? 0x80 : 0);
                    retval |= ((Collisions & TIACxPairFlags.P0BL) != 0 ? 0x40 : 0);
                    break;
                case CXP1FB:
                    retval |= ((Collisions & TIACxPairFlags.P1PF) != 0 ? 0x80 : 0);
                    retval |= ((Collisions & TIACxPairFlags.P1BL) != 0 ? 0x40 : 0);
                    break;
                case CXM0FB:
                    retval |= ((Collisions & TIACxPairFlags.M0PF) != 0 ? 0x80 : 0);
                    retval |= ((Collisions & TIACxPairFlags.M0BL) != 0 ? 0x40 : 0);
                    break;
                case CXM1FB:
                    retval |= ((Collisions & TIACxPairFlags.M1PF) != 0 ? 0x80 : 0);
                    retval |= ((Collisions & TIACxPairFlags.M1BL) != 0 ? 0x40 : 0);
                    break;
                case CXBLPF:
                    retval |= ((Collisions & TIACxPairFlags.BLPF) != 0 ? 0x80 : 0);
                    break;
                case CXPPMM:
                    retval |= ((Collisions & TIACxPairFlags.P0P1) != 0 ? 0x80 : 0);
                    retval |= ((Collisions & TIACxPairFlags.M0M1) != 0 ? 0x40 : 0);
                    break;
                case INPT0:
                    retval = DumpedInputPort(SampleINPT(0));
                    break;
                case INPT1:
                    retval = DumpedInputPort(SampleINPT(1));
                    break;
                case INPT2:
                    retval = DumpedInputPort(SampleINPT(2));
                    break;
                case INPT3:
                    retval = DumpedInputPort(SampleINPT(3));
                    break;
                case INPT4:
                    var scanline = ScanLine;
                    var hpos = PokeOpHSync - 68;
                    if (hpos < 0)
                    {
                        hpos += 228;
                        scanline--;
                    }
                    if (SampleINPTLatched(4, scanline, hpos))
                    {
                        retval &= 0x7f;
                    }
                    else
                    {
                        retval |= 0x80;
                    }
                    break;
                case INPT5:
                    scanline = ScanLine;
                    hpos = PokeOpHSync - 68;
                    if (hpos < 0)
                    {
                        hpos += 228;
                        scanline--;
                    }
                    if (SampleINPTLatched(5, scanline, hpos))
                    {
                        retval &= 0x7f;
                    }
                    else
                    {
                        retval |= 0x80;
                    }
                    break;
            }
            return (byte)(retval | (M.Mem.DataBusState & 0x3f));
        }

        byte DumpedInputPort(int resistance)
        {
            byte retval = 0;

            if (resistance == 0)
            {
                retval = 0x80;
            }
            else if (DumpEnabled || resistance == Int32.MaxValue)
            {
                retval = 0x00;
            }
            else
            {
                var chargeTime = 1.6 * resistance * 0.01e-6;
                var needed = (ulong)(chargeTime * M.FrameBuffer.Scanlines * 228 * M.FrameHZ / 3);
                if (M.CPU.Clock > DumpDisabledCycle + needed)
                {
                    retval = 0x80;
                }
            }
            return retval;
        }

        #endregion

        #region TIA Poke

        void poke(ushort addr, byte data)
        {
            addr &= 0x3f;

            var endClock = Clock;

            // writes to the TIA may take a few extra CLKs to actually affect TIA state
            // such a delay would seem to be applicable across all possible TIA writes
            // without hardware to confirm conclusively, this is updated only as seemingly required
            switch (addr)
            {
                case GRP0:
                case GRP1:
                    // stem in the T in activision logo on older titles
                    endClock += 1;
                    break;
                case PF0:
                case PF1:
                case PF2:
                    // +2 prevents minor notching in berzerk walls
                    // +4 prevents shield fragments floating in fuzz field in yars revenge,
                    //    but creates minor artifact in chopper command
                    switch (PokeOpHSync & 3)
                    {
                        case 0: endClock += 4; break;
                        case 1: endClock += 3; break;
                        case 2: endClock += 2; break;
                        case 3: endClock += 5; break;
                    }
                    break;
            }

            RenderFromStartClockTo(endClock);

            PokeOp[addr](addr, data);
        }

        static void opNULL(ushort addr, byte data)
        {
        }

        void opVSYNC(ushort addr, byte data)
        {
            // Many games don't appear to supply 3 scanlines of
            // VSYNC in accordance with the Atari documentation.
            // Enduro turns on VSYNC, then turns it off twice.
            // Centipede turns off VSYNC several times, in addition to normal usage.
            // One of the Atari Bowling ROMs turns it off, but never turns it on.
            // So, we always end the frame if VSYNC is turned on and then off.
            // We also end the frame if VSYNC is turned off after scanline 258 to accomodate Bowling.
            if ((data & 0x02) == 0)
            {
                if ((RegW[VSYNC] & 0x02) != 0 || ScanLine > 258)
                {
                    EndOfFrame = true;
                    M.CPU.EmulatorPreemptRequest = true;
                }
            }

            RegW[VSYNC] = data;
        }

        void opVBLANK(ushort addr, byte data)
        {
            if ((RegW[VBLANK] & 0x80) == 0)
            {
                // dump to ground is clear and will be set
                // thus discharging all INPTx capacitors
                if ((data & 0x80) != 0)
                {
                    DumpEnabled = true;
                }
            }
            else
            {
                // dump to ground is set and will be cleared
                // thus starting all INPTx capacitors charging
                if ((data & 0x80) == 0)
                {
                    DumpEnabled = false;
                    DumpDisabledCycle = M.CPU.Clock;
                }
            }
            RegW[VBLANK] = data;
            vblankon = (data & 0x02) != 0;
        }

        void opWSYNC(ushort addr, byte data)
        {
            // on scanline=44, tetris seems to occasionally not get a WSYNC in until 3 clk in on scanline=45 causing jitter
            if (PokeOpHSync > 0)
            {
                // report the number of remaining system clocks on the scanline to delay the CPU
                WSYNCDelayClocks = 228 - PokeOpHSync;
                // request a CPU preemption to service the delay request (only if there is a delay necessary)
                M.CPU.EmulatorPreemptRequest = true;
            }
        }

        void opRSYNC(ushort addr, byte data)
        {
            LogDebug("TIA RSYNC: frame={0} scanline={0} hsync={0}", M.FrameNumber, ScanLine, PokeOpHSync);
        }

        void opNUSIZ0(ushort addr, byte data)
        {
            RegW[NUSIZ0] = (byte)(data & 0x37);

            M0size = (RegW[NUSIZ0] & 0x30) >> 4;
            M0type = RegW[NUSIZ0] & 0x07;
            P0type = M0type;

            P0suppress = 0;
        }

        void opNUSIZ1(ushort addr, byte data)
        {
            RegW[NUSIZ1] = (byte)(data & 0x37);

            M1size = (RegW[NUSIZ1] & 0x30) >> 4;
            M1type = RegW[NUSIZ1] & 0x07;
            P1type = M1type;

            P1suppress = 0;
        }

        void opCOLUBK(ushort addr, byte data)
        {
            colubk = data;
        }

        void opCOLUPF(ushort addr, byte data)
        {
            colupf = data;
        }

        void opCOLUP0(ushort addr, byte data)
        {
            colup0 = data;
        }

        void opCOLUP1(ushort addr, byte data)
        {
            colup1 = data;
        }

        void opCTRLPF(ushort addr, byte data)
        {
            RegW[CTRLPF] = data;

            BLsize = (data & 0x30) >> 4;
            scoreon = (data & 0x02) != 0;
            pfpriority = (data & 0x04) != 0;
        }

        void SetEffGRP0()
        {
            var grp0 = RegW[VDELP0] != 0 ? OldGRP0 : RegW[GRP0];
            EffGRP0 = RegW[REFP0] != 0 ? TIATables.GRPReflect[grp0] : grp0;
        }

        void SetEffGRP1()
        {
            var grp1 = RegW[VDELP1] != 0 ? OldGRP1 : RegW[GRP1];
            EffGRP1 = RegW[REFP1] != 0 ? TIATables.GRPReflect[grp1] : grp1;
        }

        void Setblon()
        {
            blon = RegW[VDELBL] != 0 ? OldENABL : RegW[ENABL] != 0;
        }

        void opREFP0(ushort addr, byte data)
        {
            RegW[REFP0] = (byte)(data & 0x08);
            SetEffGRP0();
        }

        void opREFP1(ushort addr, byte data)
        {
            RegW[REFP1] = (byte)(data & 0x08);
            SetEffGRP1();
        }

        void opPF(ushort addr, byte data)
        {
            RegW[addr] = data;
            PF210 = (uint)((RegW[PF2] << 12) | (RegW[PF1] << 4) |((RegW[PF0] >> 4) & 0x0f));
        }

        void opRESP0(ushort addr, byte data)
        {
            if (PokeOpHSync < 68)
            {
                P0 = 0;
            }
            else if (HMoveLatch && PokeOpHSync >= 68 && PokeOpHSync < 76)
            {
                // this is an attempt to model observed behavior--may not be completely correct
                // only three hsync values are really possible:
                // 69: parkerbros actionman
                // 72: activision grandprix
                // 75: barnstorming, riverraid
                P0 = -((PokeOpHSync - 68) >> 1);
            }
            else
            {
                P0 = -4;
            }
            P0 -= PokeOpHSyncDelta;
            P0suppress = 1;
        }

        void opRESP1(ushort addr, byte data)
        {
            if (PokeOpHSync < 68)
            {
                P1 = 0;
            }
            else if (HMoveLatch && PokeOpHSync >= 68 && PokeOpHSync < 76)
            {
                // this is an attempt to model observed behavior--may not be completely correct
                // only three hsync values are really possible:
                // 69: parkerbros actionman
                // 72: parkerbros actionman
                // 75: parkerbros actionman
                P1 = -((PokeOpHSync - 68) >> 1);
            }
            else
            {
                P1 = -4;
            }
            P1 -= PokeOpHSyncDelta;
            P1suppress = 1;
        }

        void opRESM0(ushort addr, byte data)
        {
            // -2 to mirror M1
            M0 = PokeOpHSync < 68 ? -2 : -4;
            M0 -= PokeOpHSyncDelta;
        }

        void opRESM1(ushort addr, byte data)
        {
            // -2 cleans up edges on activision pitfall ii
            M1 = PokeOpHSync < 68 ? -2 : -4;
            M1 -= PokeOpHSyncDelta;
        }

        void opRESBL(ushort addr, byte data)
        {
            // -2 cleans up edges on activision boxing
            // -4 confirmed via activision choppercommand; used to clean up edges
            BL = PokeOpHSync < 68 ? -2 : -4;
            BL -= PokeOpHSyncDelta;
        }

        void opAUD(ushort addr, byte data)
        {
            RegW[addr] = data;
            TIASound.Update(addr, data);
        }

        void opGRP0(ushort addr, byte data)
        {
            RegW[GRP0] = data;
            OldGRP1 = RegW[GRP1];

            SetEffGRP0();
            SetEffGRP1();
        }

        void opGRP1(ushort addr, byte data)
        {
            RegW[GRP1] = data;
            OldGRP0 = RegW[GRP0];

            OldENABL = RegW[ENABL] != 0;

            SetEffGRP0();
            SetEffGRP1();
            Setblon();
        }

        void opENAM0(ushort addr, byte data)
        {
            RegW[ENAM0] = (byte)(data & 0x02);
            m0on = RegW[ENAM0] != 0 && RegW[RESMP0] == 0;
        }

        void opENAM1(ushort addr, byte data)
        {
            RegW[ENAM1] = (byte)(data & 0x02);
            m1on = RegW[ENAM1] != 0 && RegW[RESMP1] == 0;
        }

        void opENABL(ushort addr, byte data)
        {
            RegW[ENABL] = (byte)(data & 0x02);
            Setblon();
        }

        void SetHmr(int hmr, byte data)
        {
            // marshal via >>4 for compare convenience
            RegW[hmr] = (byte)((data ^ 0x80) >> 4);
        }

        void opHM(ushort addr, byte data)
        {
            SetHmr(addr, data);
        }

        void opVDELP0(ushort addr, byte data)
        {
            RegW[VDELP0] = (byte)(data & 0x01);
            SetEffGRP0();
        }

        void opVDELP1(ushort addr, byte data)
        {
            RegW[VDELP1] = (byte)(data & 0x01);
            SetEffGRP1();
        }

        void opVDELBL(ushort addr, byte data)
        {
            RegW[VDELBL] = (byte)(data & 0x01);
            Setblon();
        }

        void opRESMP0(ushort addr, byte data)
        {
            if (RegW[RESMP0] != 0 && (data & 0x02) == 0)
            {
                var middle = 4;
                switch (RegW[NUSIZ0] & 0x07)
                {
                    case 0x05: middle <<= 1; break;  // double size
                    case 0x07: middle <<= 2; break;  // quad size
                }
                M0 = P0 - middle;
            }
            RegW[RESMP0] = (byte)(data & 0x02);
            m0on = RegW[ENAM0] != 0 && RegW[RESMP0] == 0;
        }

        void opRESMP1(ushort addr, byte data)
        {
            if (RegW[RESMP1] != 0 && (data & 0x02) == 0)
            {
                var middle = 4;
                switch (RegW[NUSIZ1] & 0x07)
                {
                    case 0x05: middle <<= 1; break;  // double size
                    case 0x07: middle <<= 2; break;  // quad size
                }
                M1 = P1 - middle;
            }
            RegW[RESMP1] = (byte)(data & 0x02);
            m1on = RegW[ENAM1] != 0 && RegW[RESMP1] == 0;
        }

        void opHMOVE(ushort addr, byte data)
        {
            P0suppress = 0;
            P1suppress = 0;
            StartHMOVEClock = Clock + 3;

            // Activision Spiderfighter Cheat (Score and Logo)
            // Delaying the start of the HMOVE here results in it completing on the next scanline (to have visible effect.)
            // HMOVEing during the visible scanline probably has extra consequences,
            // however, it seems not many carts try to do this.
            if (PokeOpHSync == 201) StartHMOVEClock++; // any increment >0 works
        }

        void opHMCLR(ushort addr, byte data)
        {
            SetHmr(HMP0, 0);
            SetHmr(HMP1, 0);
            SetHmr(HMM0, 0);
            SetHmr(HMM1, 0);
            SetHmr(HMBL, 0);
        }

        void opCXCLR(ushort addr, byte data)
        {
            Collisions = 0;
        }

        void BuildPokeOpTable()
        {
            PokeOp = new PokeOpTyp[64];
            for (var i = 0; i < PokeOp.Length; i++)
            {
                PokeOp[i] = opNULL;
            }
            PokeOp[VSYNC]  = opVSYNC;
            PokeOp[VBLANK] = opVBLANK;
            PokeOp[WSYNC]  = opWSYNC;
            PokeOp[RSYNC]  = opRSYNC;
            PokeOp[NUSIZ0] = opNUSIZ0;
            PokeOp[NUSIZ1] = opNUSIZ1;
            PokeOp[COLUP0] = opCOLUP0;
            PokeOp[COLUP1] = opCOLUP1;
            PokeOp[COLUPF] = opCOLUPF;
            PokeOp[COLUBK] = opCOLUBK;
            PokeOp[CTRLPF] = opCTRLPF;
            PokeOp[REFP0]  = opREFP0;
            PokeOp[REFP1]  = opREFP1;
            PokeOp[PF0]    = opPF;
            PokeOp[PF1]    = opPF;
            PokeOp[PF2]    = opPF;
            PokeOp[RESP0]  = opRESP0;
            PokeOp[RESP1]  = opRESP1;
            PokeOp[RESM0]  = opRESM0;
            PokeOp[RESM1]  = opRESM1;
            PokeOp[RESBL]  = opRESBL;
            PokeOp[AUDC0]  = opAUD;
            PokeOp[AUDC1]  = opAUD;
            PokeOp[AUDF0]  = opAUD;
            PokeOp[AUDF1]  = opAUD;
            PokeOp[AUDV0]  = opAUD;
            PokeOp[AUDV1]  = opAUD;
            PokeOp[GRP0]   = opGRP0;
            PokeOp[GRP1]   = opGRP1;
            PokeOp[ENAM0]  = opENAM0;
            PokeOp[ENAM1]  = opENAM1;
            PokeOp[ENABL]  = opENABL;
            PokeOp[HMP0]   = opHM;
            PokeOp[HMP1]   = opHM;
            PokeOp[HMM0]   = opHM;
            PokeOp[HMM1]   = opHM;
            PokeOp[HMBL]   = opHM;
            PokeOp[VDELP0] = opVDELP0;
            PokeOp[VDELP1] = opVDELP1;
            PokeOp[VDELBL] = opVDELBL;
            PokeOp[RESMP0] = opRESMP0;
            PokeOp[RESMP1] = opRESMP1;
            PokeOp[HMOVE]  = opHMOVE;
            PokeOp[HMCLR]  = opHMCLR;
            PokeOp[CXCLR]  = opCXCLR;
        }

        #endregion

        #region Input Helpers

        int SampleINPT(int inpt)
        {
            var mi = M.InputState;

            switch (inpt <= 1 ? mi.LeftControllerJack : mi.RightControllerJack)
            {
                case Controller.Paddles:
                    // playerno = inpt
                    return mi.SampleCapturedOhmState(inpt & 3);
                case Controller.ProLineJoystick:
                    // playerno = inpt/2
                    switch (inpt & 3)
                    {
                        case 0: return mi.SampleCapturedControllerActionState(0, ControllerAction.Trigger)  ? 0 : Int32.MaxValue;
                        case 1: return mi.SampleCapturedControllerActionState(0, ControllerAction.Trigger2) ? 0 : Int32.MaxValue;
                        case 2: return mi.SampleCapturedControllerActionState(1, ControllerAction.Trigger)  ? 0 : Int32.MaxValue;
                        case 3: return mi.SampleCapturedControllerActionState(1, ControllerAction.Trigger2) ? 0 : Int32.MaxValue;
                    }
                    break;
                case Controller.BoosterGrip:
                    // playerno = inpt
                    return mi.SampleCapturedControllerActionState(inpt & 3, ControllerAction.Trigger2) ? 0 : Int32.MaxValue;
                case Controller.Keypad:
                    return SampleKeypadStateDumped(inpt & 3);
            }
            return int.MaxValue;
        }

        bool SampleINPTLatched(int inpt, int scanline, int hpos)
        {
            var mi = M.InputState;
            var playerNo = inpt - 4;

            switch (playerNo == 0 ? mi.LeftControllerJack : mi.RightControllerJack)
            {
                case Controller.Joystick:
                case Controller.ProLineJoystick:
                case Controller.Driving:
                case Controller.BoosterGrip:
                    return mi.SampleCapturedControllerActionState(playerNo, ControllerAction.Trigger);
                case Controller.Keypad:
                    return SampleKeypadStateLatched(playerNo);
                case Controller.Lightgun:
                    int sampledScanline, sampledHpos;
                    mi.SampleCapturedLightGunPosition(playerNo, out sampledScanline, out sampledHpos);
                    return ((scanline - 4) >= sampledScanline && (hpos - 23) >= sampledHpos);
            }
            return false;
        }

        bool SampleKeypadStateLatched(int deviceno)
        {
            ControllerAction action;

            if ((M.PIA.WrittenPortA & 0x01) == 0)
            {
                action = ControllerAction.Keypad3;
            }
            else if ((M.PIA.WrittenPortA & 0x02) == 0)
            {
                action = ControllerAction.Keypad6;
            }
            else if ((M.PIA.WrittenPortA & 0x04) == 0)
            {
                action = ControllerAction.Keypad9;
            }
            else if ((M.PIA.WrittenPortA & 0x08) == 0)
            {
                action = ControllerAction.KeypadP;
            }
            else
            {
                return false;
            }

            return M.InputState.SampleCapturedControllerActionState(deviceno, action);
        }

        int SampleKeypadStateDumped(int inpt)
        {
            ControllerAction action;

            if ((M.PIA.WrittenPortA & 0x01) == 0)
            {
                action = (inpt & 1) == 0 ? ControllerAction.Keypad1 : ControllerAction.Keypad2;
            }
            else if ((M.PIA.WrittenPortA & 0x02) == 0)
            {
                action = (inpt & 1) == 0 ? ControllerAction.Keypad4 : ControllerAction.Keypad5;
            }
            else if ((M.PIA.WrittenPortA & 0x04) == 0)
            {
                action = (inpt & 1) == 0 ? ControllerAction.Keypad7 : ControllerAction.Keypad8;
            }
            else if ((M.PIA.WrittenPortA & 0x08) == 0)
            {
                action = (inpt & 1) == 0 ? ControllerAction.KeypadA : ControllerAction.Keypad0;
            }
            else
            {
                return Int32.MaxValue;
            }

            // playerno = inpt/2
            return M.InputState.SampleCapturedControllerActionState(inpt >> 1, action) ? Int32.MaxValue : 0;
        }

        #endregion

        #region Serialization Members

        public TIA(DeserializationContext input, MachineBase m) : this()
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (m == null)
                throw new ArgumentNullException("m");

            M = m;
            TIASound = input.ReadTIASound(M, CPU_TICKS_PER_AUDIO_SAMPLE);

            input.CheckVersion(2);
            RegW = input.ReadExpectedBytes(0x40);
            HSync = input.ReadInt32();
            HMoveCounter = input.ReadInt32();
            ScanLine = input.ReadInt32();
            FrameBufferIndex = input.ReadInt32();
            //FrameBufferElement = input.ReadBufferElement();
            StartHMOVEClock = input.ReadUInt64();
            HMoveLatch = input.ReadBoolean();
            StartClock = input.ReadUInt64();
            P0 = input.ReadInt32();
            P0mmr = input.ReadBoolean();
            EffGRP0 = input.ReadByte();
            OldGRP0 = input.ReadByte();
            P0type = input.ReadInt32();
            P0suppress = input.ReadInt32();
            P1 = input.ReadInt32();
            P1mmr = input.ReadBoolean();
            EffGRP1 = input.ReadByte();
            OldGRP1 = input.ReadByte();
            P1type = input.ReadInt32();
            P1suppress = input.ReadInt32();
            M0 = input.ReadInt32();
            M0mmr = input.ReadBoolean();
            M0type = input.ReadInt32();
            M0size = input.ReadInt32();
            m0on = input.ReadBoolean();
            M1 = input.ReadInt32();
            M1mmr = input.ReadBoolean();
            M1type = input.ReadInt32();
            M1size = input.ReadInt32();
            m1on = input.ReadBoolean();
            BL = input.ReadInt32();
            BLmmr = input.ReadBoolean();
            OldENABL = input.ReadBoolean();
            BLsize = input.ReadInt32();
            blon = input.ReadBoolean();
            PF210 = input.ReadUInt32();
            PFReflectionState = input.ReadInt32();
            colubk = input.ReadByte();
            colupf = input.ReadByte();
            colup0 = input.ReadByte();
            colup1 = input.ReadByte();
            vblankon = input.ReadBoolean();
            scoreon = input.ReadBoolean();
            pfpriority = input.ReadBoolean();
            DumpEnabled = input.ReadBoolean();
            DumpDisabledCycle = input.ReadUInt64();
            Collisions = (TIACxPairFlags)input.ReadInt32();
            WSYNCDelayClocks = input.ReadInt32();
            EndOfFrame = input.ReadBoolean();
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.Write(TIASound);

            output.WriteVersion(2);
            output.Write(RegW);
            output.Write(HSync);
            output.Write(HMoveCounter);
            output.Write(ScanLine);
            output.Write(FrameBufferIndex);
            //output.Write(FrameBufferElement);
            output.Write(StartHMOVEClock);
            output.Write(HMoveLatch);
            output.Write(StartClock);
            output.Write(P0);
            output.Write(P0mmr);
            output.Write(EffGRP0);
            output.Write(OldGRP0);
            output.Write(P0type);
            output.Write(P0suppress);
            output.Write(P1);
            output.Write(P1mmr);
            output.Write(EffGRP1);
            output.Write(OldGRP1);
            output.Write(P1type);
            output.Write(P1suppress);
            output.Write(M0);
            output.Write(M0mmr);
            output.Write(M0type);
            output.Write(M0size);
            output.Write(m0on);
            output.Write(M1);
            output.Write(M1mmr);
            output.Write(M1type);
            output.Write(M1size);
            output.Write(m1on);
            output.Write(BL);
            output.Write(BLmmr);
            output.Write(OldENABL);
            output.Write(BLsize);
            output.Write(blon);
            output.Write(PF210);
            output.Write(PFReflectionState);
            output.Write(colubk);
            output.Write(colupf);
            output.Write(colup0);
            output.Write(colup1);
            output.Write(vblankon);
            output.Write(scoreon);
            output.Write(pfpriority);
            output.Write(DumpEnabled);
            output.Write(DumpDisabledCycle);
            output.Write((int)Collisions);
            output.Write(WSYNCDelayClocks);
            output.Write(EndOfFrame);
        }

        #endregion

        #region Helpers

        void Log(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        void LogDebug(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        #endregion
    }
}