namespace GarboDev
{
    using System;
    using System.Collections.Generic;

    public class Memory
    {
        public const uint REG_BASE = 0x4000000;
        public const uint PAL_BASE = 0x5000000;
        public const uint VRAM_BASE = 0x6000000;
        public const uint OAM_BASE = 0x7000000;

        public const uint DISPCNT = 0x0;
        public const uint DISPSTAT = 0x4;
        public const uint VCOUNT = 0x6;

        public const uint BG0CNT = 0x8;
        public const uint BG1CNT = 0xA;
        public const uint BG2CNT = 0xC;
        public const uint BG3CNT = 0xE;

        public const uint BG0HOFS = 0x10;
        public const uint BG0VOFS = 0x12;
        public const uint BG1HOFS = 0x14;
        public const uint BG1VOFS = 0x16;
        public const uint BG2HOFS = 0x18;
        public const uint BG2VOFS = 0x1A;
        public const uint BG3HOFS = 0x1C;
        public const uint BG3VOFS = 0x1E;

        public const uint BG2PA = 0x20;
        public const uint BG2PB = 0x22;
        public const uint BG2PC = 0x24;
        public const uint BG2PD = 0x26;
        public const uint BG2X_L = 0x28;
        public const uint BG2X_H = 0x2A;
        public const uint BG2Y_L = 0x2C;
        public const uint BG2Y_H = 0x2E;
        public const uint BG3PA = 0x30;
        public const uint BG3PB = 0x32;
        public const uint BG3PC = 0x34;
        public const uint BG3PD = 0x36;
        public const uint BG3X_L = 0x38;
        public const uint BG3X_H = 0x3A;
        public const uint BG3Y_L = 0x3C;
        public const uint BG3Y_H = 0x3E;

        public const uint WIN0H = 0x40;
        public const uint WIN1H = 0x42;
        public const uint WIN0V = 0x44;
        public const uint WIN1V = 0x46;
        public const uint WININ = 0x48;
        public const uint WINOUT = 0x4A;

        public const uint BLDCNT = 0x50;
        public const uint BLDALPHA = 0x52;
        public const uint BLDY = 0x54;

        public const uint SOUNDCNT_L = 0x80;
        public const uint SOUNDCNT_H = 0x82;
        public const uint SOUNDCNT_X = 0x84;

        public const uint FIFO_A_L = 0xA0;
        public const uint FIFO_A_H = 0xA2;
        public const uint FIFO_B_L = 0xA4;
        public const uint FIFO_B_H = 0xA6;

        public const uint DMA0SAD = 0xB0;
        public const uint DMA0DAD = 0xB4;
        public const uint DMA0CNT_L = 0xB8;
        public const uint DMA0CNT_H = 0xBA;
        public const uint DMA1SAD = 0xBC;
        public const uint DMA1DAD = 0xC0;
        public const uint DMA1CNT_L = 0xC4;
        public const uint DMA1CNT_H = 0xC6;
        public const uint DMA2SAD = 0xC8;
        public const uint DMA2DAD = 0xCC;
        public const uint DMA2CNT_L = 0xD0;
        public const uint DMA2CNT_H = 0xD2;
        public const uint DMA3SAD = 0xD4;
        public const uint DMA3DAD = 0xD8;
        public const uint DMA3CNT_L = 0xDC;
        public const uint DMA3CNT_H = 0xDE;

        public const uint TM0D = 0x100;
        public const uint TM0CNT = 0x102;
        public const uint TM1D = 0x104;
        public const uint TM1CNT = 0x106;
        public const uint TM2D = 0x108;
        public const uint TM2CNT = 0x10A;
        public const uint TM3D = 0x10C;
        public const uint TM3CNT = 0x10E;

        public const uint KEYINPUT = 0x130;
        public const uint KEYCNT = 0x132;
        public const uint IE = 0x200;
        public const uint IF = 0x202;
        public const uint IME = 0x208;

        public const uint HALTCNT = 0x300;

		private const uint biosRamMask = 0x3FFF;
        private const uint ewRamMask = 0x3FFFF;
        private const uint iwRamMask = 0x7FFF;
        private const uint ioRegMask = 0x4FF;
        private const uint vRamMask = 0x1FFFF;
        private const uint palRamMask = 0x3FF;
        private const uint oamRamMask = 0x3FF;
        private const uint sRamMask = 0xFFFF;

        private byte[] biosRam = new byte[Memory.biosRamMask + 1];
        private byte[] ewRam = new byte[Memory.ewRamMask + 1];
        private byte[] iwRam = new byte[Memory.iwRamMask + 1];
        private byte[] ioReg = new byte[Memory.ioRegMask + 1];
        private byte[] vRam = new byte[Memory.vRamMask + 1];
        private byte[] palRam = new byte[Memory.palRamMask + 1];
        private byte[] oamRam = new byte[Memory.oamRamMask + 1];
        private byte[] sRam = new byte[Memory.sRamMask + 1];

        public byte[] VideoRam
        {
            get
            {
                return this.vRam;
            }
        }

        public byte[] PaletteRam
        {
            get
            {
                return this.palRam;
            }
        }

        public byte[] OamRam
        {
            get
            {
                return this.oamRam;
            }
        }

        public byte[] IORam
        {
            get
            {
                return this.ioReg;
            }
        }

        private ushort keyState = 0x3FF;

        public ushort KeyState
        {
            get { return this.keyState; }
            set { this.keyState = value; }
        }

        private Arm7Processor processor = null;
        public Arm7Processor Processor
        {
            get { return this.processor; }
            set { this.processor = value; }
        }

        private SoundManager soundManager = null;
        public SoundManager SoundManager
        {
            get { return this.soundManager; }
            set { this.soundManager = value; }
        }

        private byte[] romBank1 = null;
        private byte[] romBank2 = null;
        private uint romBank1Mask = 0;
        private uint romBank2Mask = 0;

        private int[] bankSTimes = new int[0x10];
        private int[] bankNTimes = new int[0x10];

        private int waitCycles = 0;
        
        public int WaitCycles
        {
            get { int tmp = this.waitCycles; this.waitCycles = 0; return tmp; }
        }

        private bool inUnreadable = false;

        private delegate byte ReadU8Delegate(uint address);
        private delegate void WriteU8Delegate(uint address, byte value);
        private delegate ushort ReadU16Delegate(uint address);
        private delegate void WriteU16Delegate(uint address, ushort value);
        private delegate uint ReadU32Delegate(uint address);
        private delegate void WriteU32Delegate(uint address, uint value);

        private ReadU8Delegate[] ReadU8Funcs = null;
        private WriteU8Delegate[] WriteU8Funcs = null;
        private ReadU16Delegate[] ReadU16Funcs = null;
        private WriteU16Delegate[] WriteU16Funcs = null;
        private ReadU32Delegate[] ReadU32Funcs = null;
        private WriteU32Delegate[] WriteU32Funcs = null;

        private uint[,] dmaRegs = new uint[4, 4];
        private uint[] timerCnt = new uint[4];
        private int[] bgx = new int[2], bgy = new int[2];

        public uint[] TimerCnt
        {
            get { return this.timerCnt; }
        }

        public int[] Bgx
        {
            get { return this.bgx; }
        }

        public int[] Bgy
        {
            get { return this.bgy; }
        }

        public Memory()
        {
            this.ReadU8Funcs = new ReadU8Delegate[]
                {
                    this.ReadBiosRam8,
                    this.ReadNop8,
                    this.ReadEwRam8,
                    this.ReadIwRam8,
                    this.ReadIO8,
                    this.ReadPalRam8,
                    this.ReadVRam8,
                    this.ReadOamRam8,
                    this.ReadNop8,
                    this.ReadNop8,
                    this.ReadNop8,
                    this.ReadNop8,
                    this.ReadNop8,
                    this.ReadNop8,
                    this.ReadSRam8,
                    this.ReadNop8
                };

            this.WriteU8Funcs = new WriteU8Delegate[]
                {
                    this.WriteNop8,
                    this.WriteNop8,
                    this.WriteEwRam8,
                    this.WriteIwRam8,
                    this.WriteIO8,
                    this.WritePalRam8,
                    this.WriteVRam8,
                    this.WriteOamRam8,
                    this.WriteNop8,
                    this.WriteNop8,
                    this.WriteNop8,
                    this.WriteNop8,
                    this.WriteNop8,
                    this.WriteNop8,
                    this.WriteSRam8,
                    this.WriteNop8
                };

            this.ReadU16Funcs = new ReadU16Delegate[]
                {
                    this.ReadBiosRam16,
                    this.ReadNop16,
                    this.ReadEwRam16,
                    this.ReadIwRam16,
                    this.ReadIO16,
                    this.ReadPalRam16,
                    this.ReadVRam16,
                    this.ReadOamRam16,
                    this.ReadNop16,
                    this.ReadNop16,
                    this.ReadNop16,
                    this.ReadNop16,
                    this.ReadNop16,
                    this.ReadNop16,
                    this.ReadSRam16,
                    this.ReadNop16
                };

            this.WriteU16Funcs = new WriteU16Delegate[]
                {
                    this.WriteNop16,
                    this.WriteNop16,
                    this.WriteEwRam16,
                    this.WriteIwRam16,
                    this.WriteIO16,
                    this.WritePalRam16,
                    this.WriteVRam16,
                    this.WriteOamRam16,
                    this.WriteNop16,
                    this.WriteNop16,
                    this.WriteNop16,
                    this.WriteNop16,
                    this.WriteNop16,
                    this.WriteNop16,
                    this.WriteSRam16,
                    this.WriteNop16
                };

            this.ReadU32Funcs = new ReadU32Delegate[]
                {
                    this.ReadBiosRam32,
                    this.ReadNop32,
                    this.ReadEwRam32,
                    this.ReadIwRam32,
                    this.ReadIO32,
                    this.ReadPalRam32,
                    this.ReadVRam32,
                    this.ReadOamRam32,
                    this.ReadNop32,
                    this.ReadNop32,
                    this.ReadNop32,
                    this.ReadNop32,
                    this.ReadNop32,
                    this.ReadNop32,
                    this.ReadSRam32,
                    this.ReadNop32
                };

            this.WriteU32Funcs = new WriteU32Delegate[]
                {
                    this.WriteNop32,
                    this.WriteNop32,
                    this.WriteEwRam32,
                    this.WriteIwRam32,
                    this.WriteIO32,
                    this.WritePalRam32,
                    this.WriteVRam32,
                    this.WriteOamRam32,
                    this.WriteNop32,
                    this.WriteNop32,
                    this.WriteNop32,
                    this.WriteNop32,
                    this.WriteNop32,
                    this.WriteNop32,
                    this.WriteSRam32,
                    this.WriteNop32
                };
        }

        public void Reset()
        {
            Array.Clear(this.ewRam, 0, this.ewRam.Length);
            Array.Clear(this.iwRam, 0, this.iwRam.Length);
            Array.Clear(this.ioReg, 0, this.ioReg.Length);
            Array.Clear(this.vRam, 0, this.vRam.Length);
            Array.Clear(this.palRam, 0, this.palRam.Length);
            Array.Clear(this.oamRam, 0, this.oamRam.Length);
            Array.Clear(this.sRam, 0, this.sRam.Length);

            Memory.WriteU16(this.ioReg, Memory.BG2PA, 0x0100);
            Memory.WriteU16(this.ioReg, Memory.BG2PD, 0x0100);
            Memory.WriteU16(this.ioReg, Memory.BG3PA, 0x0100);
            Memory.WriteU16(this.ioReg, Memory.BG3PD, 0x0100);
        }

        public void HBlankDma()
        {
            for (int i = 0; i < 4; i++)
            {
                if (((this.dmaRegs[i, 3] >> 12) & 0x3) == 2)
                {
                    this.DmaTransfer(i);
                }
            }
        }

        public void VBlankDma()
        {
            for (int i = 0; i < 4; i++)
            {
                if (((this.dmaRegs[i, 3] >> 12) & 0x3) == 1)
                {
                    this.DmaTransfer(i);
                }
            }
        }

        public void FifoDma(int channel)
        {
            if (((this.dmaRegs[channel, 3] >> 12) & 0x3) == 0x3)
            {
                this.DmaTransfer(channel);
            }
        }

        public void DmaTransfer(int channel)
        {
            // Check if DMA is enabled
            if ((this.dmaRegs[channel, 3] & (1 << 15)) != 0)
            {
                bool wideTransfer = (this.dmaRegs[channel, 3] & (1 << 10)) != 0;

                uint srcDirection = 0, destDirection = 0;
                bool reload = false;

                switch ((this.dmaRegs[channel, 3] >> 5) & 0x3)
                {
                    case 0: destDirection = 1; break;
                    case 1: destDirection = 0xFFFFFFFF; break;
                    case 2: destDirection = 0; break;
                    case 3: destDirection = 1; reload = true;  break;
                }

                switch ((this.dmaRegs[channel, 3] >> 7) & 0x3)
                {
                    case 0: srcDirection = 1; break;
                    case 1: srcDirection = 0xFFFFFFFF; break;
                    case 2: srcDirection = 0; break;
                    case 3: if (channel == 3)
                        {
                            // TODO
                            return;
                        }
                        throw new Exception("Unhandled DMA mode.");
                }

                int numElements = (int)this.dmaRegs[channel, 2];
                if (numElements == 0) numElements = 0x4000;

                if (((this.dmaRegs[channel, 3] >> 12) & 0x3) == 0x3)
                {
                    // Sound FIFO mode
                    wideTransfer = true;
                    destDirection = 0;
                    numElements = 4;
                    reload = false;
                }

                if (wideTransfer)
                {
                    srcDirection *= 4;
                    destDirection *= 4;
                    while (numElements-- > 0)
                    {
                        this.WriteU32(this.dmaRegs[channel, 1], this.ReadU32(this.dmaRegs[channel, 0]));
                        this.dmaRegs[channel, 1] += destDirection;
                        this.dmaRegs[channel, 0] += srcDirection;
                    }
                }
                else
                {
                    srcDirection *= 2;
                    destDirection *= 2;
                    while (numElements-- > 0)
                    {
                        this.WriteU16(this.dmaRegs[channel, 1], this.ReadU16(this.dmaRegs[channel, 0]));
                        this.dmaRegs[channel, 1] += destDirection;
                        this.dmaRegs[channel, 0] += srcDirection;
                    }
                }

                // If not a repeating DMA, then disable the DMA
                if ((this.dmaRegs[channel, 3] & (1 << 9)) == 0)
                {
                    this.dmaRegs[channel, 3] &= 0x7FFF;
                }
                else
                {
                    // Reload dest and count
                    switch (channel)
                    {
                        case 0:
                            if (reload) this.dmaRegs[0, 1] = Memory.ReadU32(this.ioReg, Memory.DMA0DAD) & 0x07FFFFFF;
                            this.dmaRegs[0, 2] = Memory.ReadU16(this.ioReg, Memory.DMA0CNT_L);
                            break;
                        case 1:
                            if (reload) this.dmaRegs[1, 1] = Memory.ReadU32(this.ioReg, Memory.DMA1DAD) & 0x07FFFFFF;
                            this.dmaRegs[1, 2] = Memory.ReadU16(this.ioReg, Memory.DMA1CNT_L);
                            break;
                        case 2:
                            if (reload) this.dmaRegs[2, 1] = Memory.ReadU32(this.ioReg, Memory.DMA2DAD) & 0x07FFFFFF;
                            this.dmaRegs[2, 2] = Memory.ReadU16(this.ioReg, Memory.DMA2CNT_L);
                            break;
                        case 3:
                            if (reload) this.dmaRegs[3, 1] = Memory.ReadU32(this.ioReg, Memory.DMA3DAD) & 0x0FFFFFFF;
                            this.dmaRegs[3, 2] = Memory.ReadU16(this.ioReg, Memory.DMA3CNT_L);
                            break;
                    }
                }

                if ((this.dmaRegs[channel, 3] & (1 << 14)) != 0)
                {
                    this.processor.RequestIrq(8 + channel);
                }
            }
        }

        public void WriteDmaControl(int channel)
        {
            switch (channel)
            {
                case 0:
                    if (((this.dmaRegs[0, 3] ^ Memory.ReadU16(this.ioReg, Memory.DMA0CNT_H)) & (1 << 15)) == 0) return;
                    this.dmaRegs[0, 0] = Memory.ReadU32(this.ioReg, Memory.DMA0SAD) & 0x07FFFFFF;
                    this.dmaRegs[0, 1] = Memory.ReadU32(this.ioReg, Memory.DMA0DAD) & 0x07FFFFFF;
                    this.dmaRegs[0, 2] = Memory.ReadU16(this.ioReg, Memory.DMA0CNT_L);
                    this.dmaRegs[0, 3] = Memory.ReadU16(this.ioReg, Memory.DMA0CNT_H);
                    break;
                case 1:
                    if (((this.dmaRegs[1, 3] ^ Memory.ReadU16(this.ioReg, Memory.DMA1CNT_H)) & (1 << 15)) == 0) return;
                    this.dmaRegs[1, 0] = Memory.ReadU32(this.ioReg, Memory.DMA1SAD) & 0x0FFFFFFF;
                    this.dmaRegs[1, 1] = Memory.ReadU32(this.ioReg, Memory.DMA1DAD) & 0x07FFFFFF;
                    this.dmaRegs[1, 2] = Memory.ReadU16(this.ioReg, Memory.DMA1CNT_L);
                    this.dmaRegs[1, 3] = Memory.ReadU16(this.ioReg, Memory.DMA1CNT_H);
                    break;
                case 2:
                    if (((this.dmaRegs[2, 3] ^ Memory.ReadU16(this.ioReg, Memory.DMA2CNT_H)) & (1 << 15)) == 0) return;
                    this.dmaRegs[2, 0] = Memory.ReadU32(this.ioReg, Memory.DMA2SAD) & 0x0FFFFFFF;
                    this.dmaRegs[2, 1] = Memory.ReadU32(this.ioReg, Memory.DMA2DAD) & 0x07FFFFFF;
                    this.dmaRegs[2, 2] = Memory.ReadU16(this.ioReg, Memory.DMA2CNT_L);
                    this.dmaRegs[2, 3] = Memory.ReadU16(this.ioReg, Memory.DMA2CNT_H);
                    break;
                case 3:
                    if (((this.dmaRegs[3, 3] ^ Memory.ReadU16(this.ioReg, Memory.DMA3CNT_H)) & (1 << 15)) == 0) return;
                    this.dmaRegs[3, 0] = Memory.ReadU32(this.ioReg, Memory.DMA3SAD) & 0x0FFFFFFF;
                    this.dmaRegs[3, 1] = Memory.ReadU32(this.ioReg, Memory.DMA3DAD) & 0x0FFFFFFF;
                    this.dmaRegs[3, 2] = Memory.ReadU16(this.ioReg, Memory.DMA3CNT_L);
                    this.dmaRegs[3, 3] = Memory.ReadU16(this.ioReg, Memory.DMA3CNT_H);
                    break;
            }

            // Channel start timing
            switch ((this.dmaRegs[channel, 3] >> 12) & 0x3)
            {
                case 0:
                    // Start immediately
                    this.DmaTransfer(channel);
                    break;
                case 1:
                case 2:
                    // Hblank and Vblank DMA's
                    break;
                case 3:
                    // TODO (DMA sound)
                    return;
            }
        }

        private void WriteTimerControl(int timer, ushort newCnt)
        {
            ushort control = Memory.ReadU16(this.ioReg, Memory.TM0CNT + (uint)(timer * 4));
            uint count = Memory.ReadU16(this.ioReg, Memory.TM0D + (uint)(timer * 4));

            if ((newCnt & (1 << 7)) != 0 && (control & (1 << 7)) == 0)
            {
                this.timerCnt[timer] = count << 10;
            }
        }

        #region Read/Write Helpers
        public static ushort ReadU16(byte[] array, uint position)
        {
            return (ushort)(array[position] | (array[position + 1] << 8));
        }

        public static uint ReadU32(byte[] array, uint position)
        {
            return (uint)(array[position] | (array[position + 1] << 8) |
                          (array[position + 2] << 16) | (array[position + 3] << 24));
        }

        public static void WriteU16(byte[] array, uint position, ushort value)
        {
            array[position] = (byte)(value & 0xff);
            array[position + 1] = (byte)(value >> 8);
        }

        public static void WriteU32(byte[] array, uint position, uint value)
        {
            array[position] = (byte)(value & 0xff);
            array[position + 1] = (byte)((value >> 8) & 0xff);
            array[position + 2] = (byte)((value >> 16) & 0xff);
            array[position + 3] = (byte)(value >> 24);
        }
        #endregion

        #region Memory Reads
        private uint ReadUnreadable()
        {
            if (this.inUnreadable)
            {
                return 0;
            }

            this.inUnreadable = true;

            uint res;

            if (this.processor.ArmState)
            {
                res = this.ReadU32(this.processor.Registers[15]);
            }
            else
            {
                ushort val = this.ReadU16(this.processor.Registers[15]);
                res = (uint)(val | (val << 16));
            }

            this.inUnreadable = false;

            return res;
        }

        private byte ReadNop8(uint address)
        {
            return (byte)(this.ReadUnreadable() & 0xFF);
        }

        private ushort ReadNop16(uint address)
        {
            return (ushort)(this.ReadUnreadable() & 0xFFFF);
        }

        private uint ReadNop32(uint address)
        {
            return this.ReadUnreadable();
        }

        private byte ReadBiosRam8(uint address)
        {
            this.waitCycles++;
            if (this.processor.Registers[15] < 0x01000000)
            {
                return this.biosRam[address & Memory.biosRamMask];
            }
            return (byte)(this.ReadUnreadable() & 0xFF);
        }

        private ushort ReadBiosRam16(uint address)
        {
            this.waitCycles++;
            if (this.processor.Registers[15] < 0x01000000)
            {
                return Memory.ReadU16(this.biosRam, address & Memory.biosRamMask);
            }
            return (ushort)(this.ReadUnreadable() & 0xFFFF);
        }

        private uint ReadBiosRam32(uint address)
        {
            this.waitCycles++;
            if (this.processor.Registers[15] < 0x01000000)
            {
                return Memory.ReadU32(this.biosRam, address & Memory.biosRamMask);
            }
            return this.ReadUnreadable();
        }

        private byte ReadEwRam8(uint address)
        {
            this.waitCycles += 3;
            return this.ewRam[address & Memory.ewRamMask];
        }

        private ushort ReadEwRam16(uint address)
        {
            this.waitCycles += 3;
            return Memory.ReadU16(this.ewRam, address & Memory.ewRamMask);
        }

        private uint ReadEwRam32(uint address)
        {
            this.waitCycles += 6;
            return Memory.ReadU32(this.ewRam, address & Memory.ewRamMask);
        }

        private byte ReadIwRam8(uint address)
        {
            this.waitCycles++;
            return this.iwRam[address & Memory.iwRamMask];
        }

        private ushort ReadIwRam16(uint address)
        {
            this.waitCycles++;
            return Memory.ReadU16(this.iwRam, address & Memory.iwRamMask);
        }

        private uint ReadIwRam32(uint address)
        {
            this.waitCycles++;
            return Memory.ReadU32(this.iwRam, address & Memory.iwRamMask);
        }

        private byte ReadIO8(uint address)
        {
            this.waitCycles++;
            address &= 0xFFFFFF;
            if (address >= Memory.ioRegMask) return 0;

            switch (address)
            {
                case KEYINPUT:
                    return (byte)(this.keyState & 0xFF);
                case KEYINPUT + 1:
                    return (byte)(this.keyState >> 8);

                case DMA0CNT_H:
                    return (byte)(this.dmaRegs[0, 3] & 0xFF);
                case DMA0CNT_H + 1:
                    return (byte)(this.dmaRegs[0, 3] >> 8);
                case DMA1CNT_H:
                    return (byte)(this.dmaRegs[1, 3] & 0xFF);
                case DMA1CNT_H + 1:
                    return (byte)(this.dmaRegs[1, 3] >> 8);
                case DMA2CNT_H:
                    return (byte)(this.dmaRegs[2, 3] & 0xFF);
                case DMA2CNT_H + 1:
                    return (byte)(this.dmaRegs[2, 3] >> 8);
                case DMA3CNT_H:
                    return (byte)(this.dmaRegs[3, 3] & 0xFF);
                case DMA3CNT_H + 1:
                    return (byte)(this.dmaRegs[3, 3] >> 8);

                case TM0D:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[0] >> 10) & 0xFF);
                case TM0D + 1:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[0] >> 10) >> 8);
                case TM1D:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[1] >> 10) & 0xFF);
                case TM1D + 1:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[1] >> 10) >> 8);
                case TM2D:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[2] >> 10) & 0xFF);
                case TM2D + 1:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[2] >> 10) >> 8);
                case TM3D:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[3] >> 10) & 0xFF);
                case TM3D + 1:
                    this.processor.UpdateTimers();
                    return (byte)((this.timerCnt[3] >> 10) >> 8);

                default:
                    return this.ioReg[address];
            }
        }

        private ushort ReadIO16(uint address)
        {
            this.waitCycles++;
            address &= 0xFFFFFF;
            if (address >= Memory.ioRegMask) return 0;

            switch (address)
            {
                case KEYINPUT:
                    return this.keyState;

                case DMA0CNT_H:
                    return (ushort)this.dmaRegs[0, 3];
                case DMA1CNT_H:
                    return (ushort)this.dmaRegs[1, 3];
                case DMA2CNT_H:
                    return (ushort)this.dmaRegs[2, 3];
                case DMA3CNT_H:
                    return (ushort)this.dmaRegs[3, 3];

                case TM0D:
                    this.processor.UpdateTimers();
                    return (ushort)((this.timerCnt[0] >> 10) & 0xFFFF);
                case TM1D:
                    this.processor.UpdateTimers();
                    return (ushort)((this.timerCnt[1] >> 10) & 0xFFFF);
                case TM2D:
                    this.processor.UpdateTimers();
                    return (ushort)((this.timerCnt[2] >> 10) & 0xFFFF);
                case TM3D:
                    this.processor.UpdateTimers();
                    return (ushort)((this.timerCnt[3] >> 10) & 0xFFFF);

                default:
                    return Memory.ReadU16(this.ioReg, address);
            }
        }

        private uint ReadIO32(uint address)
        {
            this.waitCycles++;
            address &= 0xFFFFFF;
            if (address >= Memory.ioRegMask) return 0;

            switch (address)
            {
                case KEYINPUT:
                    return this.keyState | ((uint)Memory.ReadU16(this.ioReg, address + 0x2) << 16);

                case DMA0CNT_L:
                    return (uint)Memory.ReadU16(this.ioReg, address) | (this.dmaRegs[0, 3] << 16);
                case DMA1CNT_L:
                    return (uint)Memory.ReadU16(this.ioReg, address) | (this.dmaRegs[1, 3] << 16);
                case DMA2CNT_L:
                    return (uint)Memory.ReadU16(this.ioReg, address) | (this.dmaRegs[2, 3] << 16);
                case DMA3CNT_L:
                    return (uint)Memory.ReadU16(this.ioReg, address) | (this.dmaRegs[3, 3] << 16);

                case TM0D:
                    this.processor.UpdateTimers();
                    return (uint)(((this.timerCnt[0] >> 10) & 0xFFFF) | (uint)(Memory.ReadU16(this.ioReg, address + 2) << 16));
                case TM1D:
                    this.processor.UpdateTimers();
                    return (uint)(((this.timerCnt[1] >> 10) & 0xFFFF) | (uint)(Memory.ReadU16(this.ioReg, address + 2) << 16));
                case TM2D:
                    this.processor.UpdateTimers();
                    return (uint)(((this.timerCnt[2] >> 10) & 0xFFFF) | (uint)(Memory.ReadU16(this.ioReg, address + 2) << 16));
                case TM3D:
                    this.processor.UpdateTimers();
                    return (uint)(((this.timerCnt[3] >> 10) & 0xFFFF) | (uint)(Memory.ReadU16(this.ioReg, address + 2) << 16));

                default:
                    return Memory.ReadU32(this.ioReg, address);
            }
        }

        private byte ReadPalRam8(uint address)
        {
            this.waitCycles++;
            return this.palRam[address & Memory.palRamMask];
        }

        private ushort ReadPalRam16(uint address)
        {
            this.waitCycles++;
            return Memory.ReadU16(this.palRam, address & Memory.palRamMask);
        }

        private uint ReadPalRam32(uint address)
        {
            this.waitCycles += 2;
            return Memory.ReadU32(this.palRam, address & Memory.palRamMask);
        }

        private byte ReadVRam8(uint address)
        {
            this.waitCycles++;
            address &= Memory.vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            return this.vRam[address];
        }

        private ushort ReadVRam16(uint address)
        {
            this.waitCycles++;
            address &= Memory.vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            return Memory.ReadU16(this.vRam, address);
        }

        private uint ReadVRam32(uint address)
        {
            this.waitCycles += 2;
            address &= Memory.vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            return Memory.ReadU32(this.vRam, address & Memory.vRamMask);
        }

        private byte ReadOamRam8(uint address)
        {
            this.waitCycles++;
            return this.oamRam[address & Memory.oamRamMask];
        }

        private ushort ReadOamRam16(uint address)
        {
            this.waitCycles++;
            return Memory.ReadU16(this.oamRam, address & Memory.oamRamMask);
        }

        private uint ReadOamRam32(uint address)
        {
            this.waitCycles++;
            return Memory.ReadU32(this.oamRam, address & Memory.oamRamMask);
        }

        private byte ReadRom1_8(uint address)
        {
            this.waitCycles += this.bankSTimes[(address >> 24) & 0xf];
            return this.romBank1[address & this.romBank1Mask];
        }

        private ushort ReadRom1_16(uint address)
        {
            this.waitCycles += this.bankSTimes[(address >> 24) & 0xf];
            return Memory.ReadU16(this.romBank1, address & this.romBank1Mask);
        }

        private uint ReadRom1_32(uint address)
        {
            this.waitCycles += this.bankSTimes[(address >> 24) & 0xf] * 2 + 1;
            return Memory.ReadU32(this.romBank1, address & this.romBank1Mask);
        }

        private byte ReadRom2_8(uint address)
        {
            this.waitCycles += this.bankSTimes[(address >> 24) & 0xf];
            return this.romBank2[address & this.romBank2Mask];
        }

        private ushort ReadRom2_16(uint address)
        {
            this.waitCycles += this.bankSTimes[(address >> 24) & 0xf];
            return Memory.ReadU16(this.romBank2, address & this.romBank2Mask);
        }

        private uint ReadRom2_32(uint address)
        {
            this.waitCycles += this.bankSTimes[(address >> 24) & 0xf] * 2 + 1;
            return Memory.ReadU32(this.romBank2, address & this.romBank2Mask);
        }

        private byte ReadSRam8(uint address)
        {
            return this.sRam[address & Memory.sRamMask];
        }

        private ushort ReadSRam16(uint address)
        {
            // TODO
            return 0;
        }

        private uint ReadSRam32(uint address)
        {
            // TODO
            return 0;
        }
        #endregion

        #region Memory Writes
        private void WriteNop8(uint address, byte value)
        {
        }

        private void WriteNop16(uint address, ushort value)
        {
        }

        private void WriteNop32(uint address, uint value)
        {
        }

        private void WriteEwRam8(uint address, byte value)
        {
            this.waitCycles += 3;
            this.ewRam[address & Memory.ewRamMask] = value;
        }

        private void WriteEwRam16(uint address, ushort value)
        {
            this.waitCycles += 3;
            Memory.WriteU16(this.ewRam, address & Memory.ewRamMask, value);
        }

        private void WriteEwRam32(uint address, uint value)
        {
            this.waitCycles += 6;
            Memory.WriteU32(this.ewRam, address & Memory.ewRamMask, value);
        }

        private void WriteIwRam8(uint address, byte value)
        {
            this.waitCycles++;
            this.iwRam[address & Memory.iwRamMask] = value;
        }

        private void WriteIwRam16(uint address, ushort value)
        {
            this.waitCycles++;
            Memory.WriteU16(this.iwRam, address & Memory.iwRamMask, value);
        }

        private void WriteIwRam32(uint address, uint value)
        {
            this.waitCycles++;
            Memory.WriteU32(this.iwRam, address & Memory.iwRamMask, value);
        }

        private void WriteIO8(uint address, byte value)
        {
            this.waitCycles++;
            address &= 0xFFFFFF;
            if (address >= Memory.ioRegMask) return;

            switch (address)
            {
                case BG2X_L:
                case BG2X_L + 1:
                case BG2X_L + 2:
                case BG2X_L + 3:
                    {
                        this.ioReg[address] = value;
                        uint tmp = Memory.ReadU32(this.ioReg, BG2X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG2X_L, tmp);

                        this.bgx[0] = (int)tmp;
                    }
                    break;

                case BG3X_L:
                case BG3X_L + 1:
                case BG3X_L + 2:
                case BG3X_L + 3:
                    {
                        this.ioReg[address] = value;
                        uint tmp = Memory.ReadU32(this.ioReg, BG3X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG3X_L, tmp);

                        this.bgx[1] = (int)tmp;
                    }
                    break;

                case BG2Y_L:
                case BG2Y_L + 1:
                case BG2Y_L + 2:
                case BG2Y_L + 3:
                    {
                        this.ioReg[address] = value;
                        uint tmp = Memory.ReadU32(this.ioReg, BG2Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG2Y_L, tmp);

                        this.bgy[0] = (int)tmp;
                    }
                    break;

                case BG3Y_L:
                case BG3Y_L + 1:
                case BG3Y_L + 2:
                case BG3Y_L + 3:
                    {
                        this.ioReg[address] = value;
                        uint tmp = Memory.ReadU32(this.ioReg, BG3Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG3Y_L, tmp);

                        this.bgy[1] = (int)tmp;
                    }
                    break;

                case DMA0CNT_H:
                case DMA0CNT_H + 1:
                    this.ioReg[address] = value;
                    this.WriteDmaControl(0);
                    break;

                case DMA1CNT_H:
                case DMA1CNT_H + 1:
                    this.ioReg[address] = value;
                    this.WriteDmaControl(1);
                    break;

                case DMA2CNT_H:
                case DMA2CNT_H + 1:
                    this.ioReg[address] = value;
                    this.WriteDmaControl(2);
                    break;

                case DMA3CNT_H:
                case DMA3CNT_H + 1:
                    this.ioReg[address] = value;
                    this.WriteDmaControl(3);
                    break;

                case TM0CNT:
                case TM0CNT + 1:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM0CNT);
                        this.ioReg[address] = value;
                        this.WriteTimerControl(0, oldCnt);
                    }
                    break;

                case TM1CNT:
                case TM1CNT + 1:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM1CNT);
                        this.ioReg[address] = value;
                        this.WriteTimerControl(1, oldCnt);
                    }
                    break;

                case TM2CNT:
                case TM2CNT + 1:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM2CNT);
                        this.ioReg[address] = value;
                        this.WriteTimerControl(2, oldCnt);
                    }
                    break;

                case TM3CNT:
                case TM3CNT + 1:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM3CNT);
                        this.ioReg[address] = value;
                        this.WriteTimerControl(3, oldCnt);
                    }
                    break;

                case FIFO_A_L:
                case FIFO_A_L+1:
                case FIFO_A_H:
                case FIFO_A_H+1:
                    this.ioReg[address] = value;
                    this.soundManager.IncrementFifoA();
                    break;

                case FIFO_B_L:
                case FIFO_B_L + 1:
                case FIFO_B_H:
                case FIFO_B_H + 1:
                    this.ioReg[address] = value;
                    this.soundManager.IncrementFifoB();
                    break;

                case IF:
                case IF + 1:
                    this.ioReg[address] &= (byte)~value;
                    break;

                case HALTCNT + 1:
                    this.ioReg[address] = value;
                    this.processor.Halt();
                    break;

                default:
                    this.ioReg[address] = value;
                    break;
            }
        }

        private void WriteIO16(uint address, ushort value)
        {
            this.waitCycles++;
            address &= 0xFFFFFF;
            if (address >= Memory.ioRegMask) return;

            switch (address)
            {
                case BG2X_L:
                case BG2X_L + 2:
                    {
                        Memory.WriteU16(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG2X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG2X_L, tmp);

                        this.bgx[0] = (int)tmp;
                    }
                    break;

                case BG3X_L:
                case BG3X_L + 2:
                    {
                        Memory.WriteU16(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG3X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG3X_L, tmp);

                        this.bgx[1] = (int)tmp;
                    }
                    break;

                case BG2Y_L:
                case BG2Y_L + 2:
                    {
                        Memory.WriteU16(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG2Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG2Y_L, tmp);

                        this.bgy[0] = (int)tmp;
                    }
                    break;

                case BG3Y_L:
                case BG3Y_L + 2:
                    {
                        Memory.WriteU16(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG3Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG3Y_L, tmp);

                        this.bgy[1] = (int)tmp;
                    }
                    break;

                case DMA0CNT_H:
                    Memory.WriteU16(this.ioReg, address, value);
                    this.WriteDmaControl(0);
                    break;

                case DMA1CNT_H:
                    Memory.WriteU16(this.ioReg, address, value);
                    this.WriteDmaControl(1);
                    break;

                case DMA2CNT_H:
                    Memory.WriteU16(this.ioReg, address, value);
                    this.WriteDmaControl(2);
                    break;

                case DMA3CNT_H:
                    Memory.WriteU16(this.ioReg, address, value);
                    this.WriteDmaControl(3);
                    break;

                case TM0CNT:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM0CNT);
                        Memory.WriteU16(this.ioReg, address, value);
                        this.WriteTimerControl(0, oldCnt);
                    }
                    break;

                case TM1CNT:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM1CNT);
                        Memory.WriteU16(this.ioReg, address, value);
                        this.WriteTimerControl(1, oldCnt);
                    }
                    break;

                case TM2CNT:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM2CNT);
                        Memory.WriteU16(this.ioReg, address, value);
                        this.WriteTimerControl(2, oldCnt);
                    }
                    break;

                case TM3CNT:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM3CNT);
                        Memory.WriteU16(this.ioReg, address, value);
                        this.WriteTimerControl(3, oldCnt);
                    }
                    break;

                case FIFO_A_L:
                case FIFO_A_H:
                    Memory.WriteU16(this.ioReg, address, value);
                    this.soundManager.IncrementFifoA();
                    break;

                case FIFO_B_L:
                case FIFO_B_H:
                    Memory.WriteU16(this.ioReg, address, value);
                    this.soundManager.IncrementFifoB();
                    break;

                case SOUNDCNT_H:
                    Memory.WriteU16(this.ioReg, address, value);
                    if ((value & (1 << 11)) != 0)
                    {
                        this.soundManager.ResetFifoA();
                    }
                    if ((value & (1 << 15)) != 0)
                    {
                        this.soundManager.ResetFifoB();
                    }
                    break;

                case IF:
                    {
                        ushort tmp = Memory.ReadU16(this.ioReg, address);
                        Memory.WriteU16(this.ioReg, address, (ushort)(tmp & (~value)));
                    }
                    break;

                case HALTCNT:
                    Memory.WriteU16(this.ioReg, address, value);
                    this.processor.Halt();
                    break;

                default:
                    Memory.WriteU16(this.ioReg, address, value);
                    break;
            }
        }

        private void WriteIO32(uint address, uint value)
        {
            this.waitCycles++;
            address &= 0xFFFFFF;
            if (address >= Memory.ioRegMask) return;

            switch (address)
            {
                case BG2X_L:
                    {
                        Memory.WriteU32(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG2X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG2X_L, tmp);

                        this.bgx[0] = (int)tmp;
                    }
                    break;

                case BG3X_L:
                    {
                        Memory.WriteU32(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG3X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG3X_L, tmp);

                        this.bgx[1] = (int)tmp;
                    }
                    break;

                case BG2Y_L:
                    {
                        Memory.WriteU32(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG2Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG2Y_L, tmp);

                        this.bgy[0] = (int)tmp;
                    }
                    break;

                case BG3Y_L:
                    {
                        Memory.WriteU32(this.ioReg, address, value);
                        uint tmp = Memory.ReadU32(this.ioReg, BG3Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        Memory.WriteU32(this.ioReg, BG3Y_L, tmp);

                        this.bgy[1] = (int)tmp;
                    }
                    break;

                case DMA0CNT_L:
                    Memory.WriteU32(this.ioReg, address, value);
                    this.WriteDmaControl(0);
                    break;

                case DMA1CNT_L:
                    Memory.WriteU32(this.ioReg, address, value);
                    this.WriteDmaControl(1);
                    break;

                case DMA2CNT_L:
                    Memory.WriteU32(this.ioReg, address, value);
                    this.WriteDmaControl(2);
                    break;

                case DMA3CNT_L:
                    Memory.WriteU32(this.ioReg, address, value);
                    this.WriteDmaControl(3);
                    break;

                case TM0D:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM0CNT);
                        Memory.WriteU32(this.ioReg, address, value);
                        this.WriteTimerControl(0, oldCnt);
                    }
                    break;

                case TM1D:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM1CNT);
                        Memory.WriteU32(this.ioReg, address, value);
                        this.WriteTimerControl(1, oldCnt);
                    }
                    break;

                case TM2D:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM2CNT);
                        Memory.WriteU32(this.ioReg, address, value);
                        this.WriteTimerControl(2, oldCnt);
                    }
                    break;

                case TM3D:
                    {
                        ushort oldCnt = Memory.ReadU16(this.ioReg, TM3CNT);
                        Memory.WriteU32(this.ioReg, address, value);
                        this.WriteTimerControl(3, oldCnt);
                    }
                    break;

                case FIFO_A_L:
                    Memory.WriteU32(this.ioReg, address, value);
                    this.soundManager.IncrementFifoA();
                    break;

                case FIFO_B_L:
                    Memory.WriteU32(this.ioReg, address, value);
                    this.soundManager.IncrementFifoB();
                    break;

                case SOUNDCNT_L:
                    Memory.WriteU32(this.ioReg, address, value);
                    if (((value >> 16) & (1 << 11)) != 0)
                    {
                        this.soundManager.ResetFifoA();
                    }
                    if (((value >> 16) & (1 << 15)) != 0)
                    {
                        this.soundManager.ResetFifoB();
                    }
                    break;

                case IE:
                    {
                        uint tmp = Memory.ReadU32(this.ioReg, address);
                        Memory.WriteU32(this.ioReg, address, (uint)((value & 0xFFFF) | (tmp & (~(value & 0xFFFF0000)))));
                    }
                    break;

                case HALTCNT:
                    Memory.WriteU32(this.ioReg, address, value);
                    this.processor.Halt();
                    break;

                default:
                    Memory.WriteU32(this.ioReg, address, value);
                    break;
            }
        }

        private void WritePalRam8(uint address, byte value)
        {
            this.waitCycles++;
            address &= Memory.palRamMask & ~1U;
            this.palRam[address] = value;
            this.palRam[address + 1] = value;
        }

        private void WritePalRam16(uint address, ushort value)
        {
            this.waitCycles++;
            Memory.WriteU16(this.palRam, address & Memory.palRamMask, value);
        }

        private void WritePalRam32(uint address, uint value)
        {
            this.waitCycles += 2;
            Memory.WriteU32(this.palRam, address & Memory.palRamMask, value);
        }

        private void WriteVRam8(uint address, byte value)
        {
            this.waitCycles++;
            address &= Memory.vRamMask & ~1U;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            this.vRam[address] = value;
            this.vRam[address + 1] = value;
        }

        private void WriteVRam16(uint address, ushort value)
        {
            this.waitCycles++;
            address &= Memory.vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            Memory.WriteU16(this.vRam, address, value);
        }

        private void WriteVRam32(uint address, uint value)
        {
            this.waitCycles += 2;
            address &= Memory.vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            Memory.WriteU32(this.vRam, address, value);
        }

        private void WriteOamRam8(uint address, byte value)
        {
            this.waitCycles++;
            address &= Memory.oamRamMask & ~1U;

            this.oamRam[address] = value;
            this.oamRam[address + 1] = value;
        }

        private void WriteOamRam16(uint address, ushort value)
        {
            this.waitCycles++;
            Memory.WriteU16(this.oamRam, address & Memory.oamRamMask, value);
        }

        private void WriteOamRam32(uint address, uint value)
        {
            this.waitCycles++;
            Memory.WriteU32(this.oamRam, address & Memory.oamRamMask, value);
        }

        private void WriteSRam8(uint address, byte value)
        {
            this.sRam[address & Memory.sRamMask] = value;
        }

        private void WriteSRam16(uint address, ushort value)
        {
            // TODO
        }

        private void WriteSRam32(uint address, uint value)
        {
            // TODO
        }

        private enum EepromModes
        {
            Idle,
            ReadData
        }

        private EepromModes eepromMode = EepromModes.Idle;
        private byte[] eeprom = new byte[0xffff];
        private byte[] eepromStore = new byte[0xff];
        private int curEepromByte;
        private int eepromReadAddress = -1;

        private void WriteEeprom8(uint address, byte value)
        {
            // EEPROM writes must be done by DMA 3
            if ((this.dmaRegs[3, 3] & (1 << 15)) == 0) return;
            // 0 length eeprom writes are bad
            if (this.dmaRegs[3, 2] == 0) return;

            if (this.eepromMode != EepromModes.ReadData)
            {
                this.curEepromByte = 0;
                this.eepromMode = EepromModes.ReadData;
                this.eepromReadAddress = -1;

                for (int i = 0; i < this.eepromStore.Length; i++) this.eepromStore[i] = 0;
            }

            this.eepromStore[this.curEepromByte >> 3] |= (byte)(value << (7 - (this.curEepromByte & 0x7)));
            this.curEepromByte++;

            if (this.curEepromByte == this.dmaRegs[3, 2])
            {
                if ((this.eepromStore[0] & 0x80) == 0) return;

                if ((this.eepromStore[0] & 0x40) != 0)
                {
                    // Read request
                    if (this.curEepromByte == 9)
                    {
                        this.eepromReadAddress = this.eepromStore[0] & 0x3F;
                    }
                    else
                    {
                        this.eepromReadAddress = ((this.eepromStore[0] & 0x3F) << 8) | this.eepromStore[1];
                    }
                    
                    this.curEepromByte = 0;
                }
                else
                {
                    // Write request
                    int eepromAddress, offset;
                    if (this.curEepromByte == 64 + 9)
                    {
                        eepromAddress = (int)(this.eepromStore[0] & 0x3F);
                        offset = 1;
                    }
                    else
                    {
                        eepromAddress = ((this.eepromStore[0] & 0x3F) << 8) | this.eepromStore[1];
                        offset = 2;
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        this.eeprom[eepromAddress * 8 + i] = this.eepromStore[i + offset];
                    }

                    this.eepromMode = EepromModes.Idle;
                }
            }
        }

        private void WriteEeprom16(uint address, ushort value)
        {
            this.WriteEeprom8(address, (byte)(value & 0xff));
        }

        private void WriteEeprom32(uint address, uint value)
        {
            this.WriteEeprom8(address, (byte)(value & 0xff));
        }

        private byte ReadEeprom8(uint address)
        {
            if (this.eepromReadAddress == -1) return 1;

            byte retval = 0;

            if (this.curEepromByte >= 4)
            {
                retval = (byte)((this.eeprom[this.eepromReadAddress * 8 + ((this.curEepromByte - 4) / 8)] >> (7 - ((this.curEepromByte - 4) & 7))) & 1);
            }

            this.curEepromByte++;

            if (this.curEepromByte == this.dmaRegs[3, 2])
            {
                this.eepromReadAddress = -1;
                this.eepromMode = EepromModes.Idle;
            }

            return retval;
        }

        private ushort ReadEeprom16(uint address)
        {
            return (ushort)this.ReadEeprom8(address);
        }

        private uint ReadEeprom32(uint address)
        {
            return (uint)this.ReadEeprom8(address);
        }
        #endregion

        #region Shader Renderer Vram Writes
        private List<uint> vramUpdated = new List<uint>();
        private List<uint> palUpdated = new List<uint>();
        public const int VramBlockSize = 64;
        public const int PalBlockSize = 32;
        private bool[] vramHit = new bool[(Memory.vRamMask + 1) / VramBlockSize];
        private bool[] palHit = new bool[(Memory.palRamMask + 1) / PalBlockSize];

        public List<uint> VramUpdated
        {
            get
            {
                List<uint> old = this.vramUpdated;
                for (int i = 0; i < old.Count; i++)
                {
                    vramHit[old[i]] = false;
                }
                this.vramUpdated = new List<uint>();
                return old;
            }
        }


        public List<uint> PalUpdated
        {
            get
            {
                List<uint> old = this.palUpdated;
                for (int i = 0; i < old.Count; i++)
                {
                    palHit[old[i]] = false;
                }
                this.palUpdated = new List<uint>();
                return old;
            }
        }

        private void UpdatePal(uint address)
        {
            uint index = address / PalBlockSize;
            if (!palHit[index])
            {
                palHit[index] = true;
                this.palUpdated.Add(index);
            }
        }

        private void UpdateVram(uint address)
        {
            uint index = address / VramBlockSize;
            if (!vramHit[index])
            {
                vramHit[index] = true;
                this.vramUpdated.Add(index);
            }
        }

        private void ShaderWritePalRam8(uint address, byte value)
        {
            this.waitCycles++;
            address &= Memory.palRamMask & ~1U;
            this.palRam[address] = value;
            this.palRam[address + 1] = value;

            this.UpdatePal(address);
        }

        private void ShaderWritePalRam16(uint address, ushort value)
        {
            this.waitCycles++;
            Memory.WriteU16(this.palRam, address & Memory.palRamMask, value);

            this.UpdatePal(address & Memory.palRamMask);
        }

        private void ShaderWritePalRam32(uint address, uint value)
        {
            this.waitCycles += 2;
            Memory.WriteU32(this.palRam, address & Memory.palRamMask, value);

            this.UpdatePal(address & Memory.palRamMask);
        }

        private void ShaderWriteVRam8(uint address, byte value)
        {
            this.waitCycles++;
            address &= Memory.vRamMask & ~1U;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            this.vRam[address] = value;
            this.vRam[address + 1] = value;
            
            this.UpdateVram(address);
        }

        private void ShaderWriteVRam16(uint address, ushort value)
        {
            this.waitCycles++;
            address &= Memory.vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            Memory.WriteU16(this.vRam, address, value);

            this.UpdateVram(address);
        }

        private void ShaderWriteVRam32(uint address, uint value)
        {
            this.waitCycles += 2;
            address &= Memory.vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            Memory.WriteU32(this.vRam, address, value);

            this.UpdateVram(address);
        }

        public void EnableVramUpdating()
        {
            this.WriteU8Funcs[0x5] = this.ShaderWritePalRam8;
            this.WriteU16Funcs[0x5] = this.ShaderWritePalRam16;
            this.WriteU32Funcs[0x5] = this.ShaderWritePalRam32;
            this.WriteU8Funcs[0x6] = this.ShaderWriteVRam8;
            this.WriteU16Funcs[0x6] = this.ShaderWriteVRam16;
            this.WriteU32Funcs[0x6] = this.ShaderWriteVRam32;

            for (uint i = 0; i < (Memory.vRamMask + 1) / Memory.VramBlockSize; i++)
            {
                this.vramUpdated.Add(i);
            }

            for (uint i = 0; i < (Memory.palRamMask + 1) / Memory.PalBlockSize; i++)
            {
                this.palUpdated.Add(i);
            }
        }
        #endregion

        public byte ReadU8(uint address)
        {
            uint bank = (address >> 24) & 0xf;
            return this.ReadU8Funcs[bank](address);
        }

        public ushort ReadU16(uint address)
        {
            address &= ~1U;
            uint bank = (address >> 24) & 0xf;
            return this.ReadU16Funcs[bank](address);
        }

        public uint ReadU32(uint address)
        {
            int shiftAmt = (int)((address & 3U) << 3);
            address &= ~3U;
            uint bank = (address >> 24) & 0xf;
            uint res = this.ReadU32Funcs[bank](address);
            return (res >> shiftAmt) | (res << (32 - shiftAmt));
        }

        public uint ReadU32Aligned(uint address)
        {
            uint bank = (address >> 24) & 0xf;
            return this.ReadU32Funcs[bank](address);
        }

        public ushort ReadU16Debug(uint address)
        {
            address &= ~1U;
            uint bank = (address >> 24) & 0xf;
            int oldWaitCycles = this.waitCycles;
            ushort res = this.ReadU16Funcs[bank](address);
            this.waitCycles = oldWaitCycles;
            return res;
        }

        public uint ReadU32Debug(uint address)
        {
            int shiftAmt = (int)((address & 3U) << 3);
            address &= ~3U;
            uint bank = (address >> 24) & 0xf;
            int oldWaitCycles = this.waitCycles;
            uint res = this.ReadU32Funcs[bank](address);
            this.waitCycles = oldWaitCycles;
            return (res >> shiftAmt) | (res << (32 - shiftAmt));
        }

        public void WriteU8(uint address, byte value)
        {
            uint bank = (address >> 24) & 0xf;
            this.WriteU8Funcs[bank](address, value);
        }

        public void WriteU16(uint address, ushort value)
        {
            address &= ~1U;
            uint bank = (address >> 24) & 0xf;
            this.WriteU16Funcs[bank](address, value);
        }

        public void WriteU32(uint address, uint value)
        {
            address &= ~3U;
            uint bank = (address >> 24) & 0xf;
            this.WriteU32Funcs[bank](address, value);
        }

        public void WriteU8Debug(uint address, byte value)
        {
            uint bank = (address >> 24) & 0xf;
            int oldWaitCycles = this.waitCycles;
            this.WriteU8Funcs[bank](address, value);
            this.waitCycles = oldWaitCycles;
        }

        public void WriteU16Debug(uint address, ushort value)
        {
            address &= ~1U;
            uint bank = (address >> 24) & 0xf;
            int oldWaitCycles = this.waitCycles;
            this.WriteU16Funcs[bank](address, value);
            this.waitCycles = oldWaitCycles;
        }

        public void WriteU32Debug(uint address, uint value)
        {
            address &= ~3U;
            uint bank = (address >> 24) & 0xf;
            int oldWaitCycles = this.waitCycles;
            this.WriteU32Funcs[bank](address, value);
            this.waitCycles = oldWaitCycles;
        }

        public void LoadBios(byte[] biosRom)
        {
            Array.Copy(biosRom, this.biosRam, this.biosRam.Length);
        }

        public void LoadCartridge(byte[] cartRom)
        {
            this.ResetRomBank1();
            this.ResetRomBank2();

            // Set up the appropriate cart size
            int cartSize = 1;
            while (cartSize < cartRom.Length)
            {
                cartSize <<= 1;
            }

            if (cartSize != cartRom.Length)
            {
                throw new Exception("Unable to load non power of two carts");
            }

            // Split across bank 1 and 2 if cart is too big
            if (cartSize > 1 << 24)
            {
                this.romBank1 = cartRom;
                this.romBank1Mask = (1 << 24) - 1;

                cartRom.CopyTo(this.romBank2, 1 << 24);
                this.romBank2Mask = (1 << 24) - 1;
            }
            else
            {
                this.romBank1 = cartRom;
                this.romBank1Mask = (uint)(cartSize - 1);
            }

            if (this.romBank1Mask != 0)
            {
                // TODO: Writes (i.e. eeprom, and other stuff)
                this.ReadU8Funcs[0x8] = this.ReadRom1_8;
                this.ReadU8Funcs[0xA] = this.ReadRom1_8;
                this.ReadU8Funcs[0xC] = this.ReadRom1_8;
                this.ReadU16Funcs[0x8] = this.ReadRom1_16;
                this.ReadU16Funcs[0xA] = this.ReadRom1_16;
                this.ReadU16Funcs[0xC] = this.ReadRom1_16;
                this.ReadU32Funcs[0x8] = this.ReadRom1_32;
                this.ReadU32Funcs[0xA] = this.ReadRom1_32;
                this.ReadU32Funcs[0xC] = this.ReadRom1_32;
            }

            if (this.romBank2Mask != 0)
            {
                this.ReadU8Funcs[0x9] = this.ReadRom2_8;
                this.ReadU8Funcs[0xB] = this.ReadRom2_8;
                this.ReadU8Funcs[0xD] = this.ReadRom2_8;
                this.ReadU16Funcs[0x9] = this.ReadRom2_16;
                this.ReadU16Funcs[0xB] = this.ReadRom2_16;
                this.ReadU16Funcs[0xD] = this.ReadRom2_16;
                this.ReadU32Funcs[0x9] = this.ReadRom2_32;
                this.ReadU32Funcs[0xB] = this.ReadRom2_32;
                this.ReadU32Funcs[0xD] = this.ReadRom2_32;
            }
        }

        private void ResetRomBank1()
        {
            this.romBank1 = null;
            this.romBank1Mask = 0;

            for (int i = 0; i < this.bankSTimes.Length; i++)
            {
                this.bankSTimes[i] = 2;
            }

            this.ReadU8Funcs[0x8] = this.ReadNop8;
            this.ReadU8Funcs[0xA] = this.ReadNop8;
            this.ReadU8Funcs[0xC] = this.ReadNop8;
            this.ReadU16Funcs[0x8] = this.ReadNop16;
            this.ReadU16Funcs[0xA] = this.ReadNop16;
            this.ReadU16Funcs[0xC] = this.ReadNop16;
            this.ReadU32Funcs[0x8] = this.ReadNop32;
            this.ReadU32Funcs[0xA] = this.ReadNop32;
            this.ReadU32Funcs[0xC] = this.ReadNop32;
        }

        private void ResetRomBank2()
        {
            this.romBank2 = null;
            this.romBank2Mask = 0;

            this.ReadU8Funcs[0x9] = this.ReadEeprom8;
            this.ReadU8Funcs[0xB] = this.ReadEeprom8;
            this.ReadU8Funcs[0xD] = this.ReadEeprom8;
            this.ReadU16Funcs[0x9] = this.ReadEeprom16;
            this.ReadU16Funcs[0xB] = this.ReadEeprom16;
            this.ReadU16Funcs[0xD] = this.ReadEeprom16;
            this.ReadU32Funcs[0x9] = this.ReadEeprom32;
            this.ReadU32Funcs[0xB] = this.ReadEeprom32;
            this.ReadU32Funcs[0xD] = this.ReadEeprom32;

            this.WriteU8Funcs[0x9] = this.WriteEeprom8;
            this.WriteU8Funcs[0xB] = this.WriteEeprom8;
            this.WriteU8Funcs[0xD] = this.WriteEeprom8;
            this.WriteU16Funcs[0x9] = this.WriteEeprom16;
            this.WriteU16Funcs[0xB] = this.WriteEeprom16;
            this.WriteU16Funcs[0xD] = this.WriteEeprom16;
            this.WriteU32Funcs[0x9] = this.WriteEeprom32;
            this.WriteU32Funcs[0xB] = this.WriteEeprom32;
            this.WriteU32Funcs[0xD] = this.WriteEeprom32;
        }
    }
}
