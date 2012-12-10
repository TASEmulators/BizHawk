namespace GarboDev
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class Arm7Processor
    {
        private Memory memory = null;
        private FastArmCore armCore = null;
        private ThumbCore thumbCore = null;
        private Dictionary<uint, bool> breakpoints = null;

        private int cycles = 0;
        private int timerCycles = 0;
        private int soundCycles = 0;

        // CPU mode definitions
        public const uint USR = 0x10;
        public const uint FIQ = 0x11;
        public const uint IRQ = 0x12;
        public const uint SVC = 0x13;
        public const uint ABT = 0x17;
        public const uint UND = 0x1B;
        public const uint SYS = 0x1F;

        // CPSR bit definitions
        public const int N_BIT = 31;
        public const int Z_BIT = 30;
        public const int C_BIT = 29;
        public const int V_BIT = 28;
        public const int I_BIT = 7;
        public const int F_BIT = 6;
        public const int T_BIT = 5;

        public const uint N_MASK = (uint)(1U << N_BIT);
        public const uint Z_MASK = (uint)(1U << Z_BIT);
        public const uint C_MASK = (uint)(1U << C_BIT);
        public const uint V_MASK = (uint)(1U << V_BIT);
        public const uint I_MASK = (uint)(1U << I_BIT);
        public const uint F_MASK = (uint)(1U << F_BIT);
        public const uint T_MASK = (uint)(1U << T_BIT);

        // Standard registers
        private uint[] registers = new uint[16];
        private uint cpsr = 0;

        // Banked registers
        private uint[] bankedFIQ = new uint[7];
        private uint[] bankedIRQ = new uint[2];
        private uint[] bankedSVC = new uint[2];
        private uint[] bankedABT = new uint[2];
        private uint[] bankedUND = new uint[2];

        // Saved CPSR's
        private uint spsrFIQ = 0;
        private uint spsrIRQ = 0;
        private uint spsrSVC = 0;
        private uint spsrABT = 0;
        private uint spsrUND = 0;

        private ushort keyState;

        private bool breakpointHit = false;
        private bool cpuHalted = false;

        public ushort KeyState
        {
            set { this.keyState = value; }
        }

        public int Cycles
        {
            get { return this.cycles; }
            set { this.cycles = value; }
        }

        public bool ArmState
        {
            get { return (this.cpsr & Arm7Processor.T_MASK) != Arm7Processor.T_MASK; }
        }

        public uint[] Registers
        {
            get { return this.registers; }
        }

        public uint CPSR
        {
            get { return this.cpsr; }
            set { this.cpsr = value; }
        }

        public bool SPSRExists
        {
            get
            {
                switch (this.cpsr & 0x1F)
                {
                    case USR:
                    case SYS:
                        return false;
                    case FIQ:
                    case SVC:
                    case ABT:
                    case IRQ:
                    case UND:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public uint SPSR
        {
            get
            {
                switch (this.cpsr & 0x1F)
                {
                    case USR:
                    case SYS:
                        return 0xFFFFFFFF;
                    case FIQ:
                        return this.spsrFIQ;
                    case SVC:
                        return this.spsrSVC;
                    case ABT:
                        return this.spsrABT;
                    case IRQ:
                        return this.spsrIRQ;
                    case UND:
                        return this.spsrUND;
                    default:
                        throw new Exception("Unhandled CPSR state...");
                }
            }
            set
            {
                switch (this.cpsr & 0x1F)
                {
                    case USR:
                    case SYS:
                        break;
                    case FIQ:
                        this.spsrFIQ = value;
                        break;
                    case SVC:
                        this.spsrSVC = value;
                        break;
                    case ABT:
                        this.spsrABT = value;
                        break;
                    case IRQ:
                        this.spsrIRQ = value;
                        break;
                    case UND:
                        this.spsrUND = value;
                        break;
                    default:
                        throw new Exception("Unhandled CPSR state...");
                }
            }
        }

        public Dictionary<uint, bool> Breakpoints
        {
            get
            {
                return this.breakpoints;
            }
        }

        public bool BreakpointHit
        {
            get { return this.breakpointHit; }
            set { this.breakpointHit = value; }
        }

        public Arm7Processor(Memory memory)
        {
            this.memory = memory;
            this.memory.Processor = this;
            this.armCore = new FastArmCore(this, this.memory);
            this.thumbCore = new ThumbCore(this, this.memory);
            this.breakpoints = new Dictionary<uint, bool>();
            this.breakpointHit = false;
        }

        private void SwapRegsHelper(uint[] swapRegs)
        {
            for (int i = 14; i > 14 - swapRegs.Length; i--)
            {
                uint tmp = this.registers[i];
                this.registers[i] = swapRegs[swapRegs.Length - (14 - i) - 1];
                swapRegs[swapRegs.Length - (14 - i) - 1] = tmp;
            }
        }

        private void SwapRegisters(uint bank)
        {
            switch (bank & 0x1F)
            {
                case FIQ:
                    this.SwapRegsHelper(this.bankedFIQ);
                    break;
                case SVC:
                    this.SwapRegsHelper(this.bankedSVC);
                    break;
                case ABT:
                    this.SwapRegsHelper(this.bankedABT);
                    break;
                case IRQ:
                    this.SwapRegsHelper(this.bankedIRQ);
                    break;
                case UND:
                    this.SwapRegsHelper(this.bankedUND);
                    break;
            }
        }

        public void WriteCpsr(uint newCpsr)
        {
            if ((newCpsr & 0x1F) != (this.cpsr & 0x1F))
            {
                // Swap out the old registers
                this.SwapRegisters(this.cpsr);
                // Swap in the new registers
                this.SwapRegisters(newCpsr);
            }

            this.cpsr = newCpsr;
        }

        public void EnterException(uint mode, uint vector, bool interruptsDisabled, bool fiqDisabled)
        {
            uint oldCpsr = this.cpsr;

            if ((oldCpsr & Arm7Processor.T_MASK) != 0)
            {
                registers[15] += 2U;
            }

            // Clear T bit, and set mode
            uint newCpsr = (oldCpsr & ~0x3FU) | mode;
            if (interruptsDisabled) newCpsr |= 1 << 7;
            if (fiqDisabled) newCpsr |= 1 << 6;
            this.WriteCpsr(newCpsr);

            this.SPSR = oldCpsr;
            registers[14] = registers[15];
            registers[15] = vector;

            this.ReloadQueue();
        }

        public void RequestIrq(int irq)
        {
            ushort iflag = Memory.ReadU16(this.memory.IORam, Memory.IF);
            iflag |= (ushort)(1 << irq);
            Memory.WriteU16(this.memory.IORam, Memory.IF, iflag);
        }

        public void FireIrq()
        {
            ushort ime = Memory.ReadU16(this.memory.IORam, Memory.IME);
            ushort ie = Memory.ReadU16(this.memory.IORam, Memory.IE);
            ushort iflag = Memory.ReadU16(this.memory.IORam, Memory.IF);

            if ((ie & (iflag)) != 0 && (ime & 1) != 0 && (this.cpsr & (1 << 7)) == 0)
            {
                // Off to the irq exception vector
                this.EnterException(Arm7Processor.IRQ, 0x18, true, false);
            }
        }

        public void Reset(bool skipBios)
        {
            this.breakpointHit = false;
            this.cpuHalted = false;

            // Default to ARM state
            this.cycles = 0;
            this.timerCycles = 0;
            this.soundCycles = 0;

            this.bankedSVC[0] = 0x03007FE0;
            this.bankedIRQ[0] = 0x03007FA0;

            this.cpsr = SYS;
            this.spsrSVC = this.cpsr;
            for (int i = 0; i < 15; i++) this.registers[i] = 0;

            if (skipBios)
            {
                this.registers[15] = 0x8000000;
            }
            else
            {
                this.registers[15] = 0;
            }

            this.armCore.BeginExecution();
        }

        public void Halt()
        {
            this.cpuHalted = true;
            this.cycles = 0;
        }

        public void Step()
        {
            this.breakpointHit = false;

            if ((this.cpsr & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
            {
                this.thumbCore.Step();
            }
            else
            {
                this.armCore.Step();
            }

            this.UpdateTimers();
        }

        public void ReloadQueue()
        {
            if ((this.cpsr & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
            {
                this.thumbCore.BeginExecution();
            }
            else
            {
                this.armCore.BeginExecution();
            }
        }

        private void UpdateTimer(int timer, int cycles, bool countUp)
        {
            ushort control = Memory.ReadU16(this.memory.IORam, Memory.TM0CNT + (uint)(timer * 4));

            // Make sure timer is enabled, or count up is disabled
            if ((control & (1 << 7)) == 0) return;
            if (!countUp && (control & (1 << 2)) != 0) return;

            if (!countUp)
            {
                switch (control & 3)
                {
                    case 0: cycles *= 1 << 10; break;
                    case 1: cycles *= 1 << 4; break;
                    case 2: cycles *= 1 << 2; break;
                    // Don't need to do anything for case 3
                }
            }

            this.memory.TimerCnt[timer] += (uint)cycles;
            uint timerCnt = this.memory.TimerCnt[timer] >> 10;

            if (timerCnt > 0xffff)
            {
                ushort soundCntX = Memory.ReadU16(this.memory.IORam, Memory.SOUNDCNT_X);
                if ((soundCntX & (1 << 7)) != 0)
                {
                    ushort soundCntH = Memory.ReadU16(this.memory.IORam, Memory.SOUNDCNT_H);
                    if (timer == ((soundCntH >> 10) & 1))
                    {
                        // FIFO A overflow
                        this.memory.SoundManager.DequeueA();
                        if (this.memory.SoundManager.QueueSizeA < 16)
                        {
                            this.memory.FifoDma(1);
                            // TODO
                            if (this.memory.SoundManager.QueueSizeA < 16)
                            {
                            }
                        }
                    }
                    if (timer == ((soundCntH >> 14) & 1))
                    {
                        // FIFO B overflow
                        this.memory.SoundManager.DequeueB();
                        if (this.memory.SoundManager.QueueSizeB < 16)
                        {
                            this.memory.FifoDma(2);
                        }
                    }
                }

                // Overflow, attempt to fire IRQ
                if ((control & (1 << 6)) != 0)
                {
                    this.RequestIrq(3 + timer);
                }

                if (timer < 3)
                {
                    ushort control2 = Memory.ReadU16(this.memory.IORam, Memory.TM0CNT + (uint)((timer + 1) * 4));
                    if ((control2 & (1 << 2)) != 0)
                    {
                        // Count-up
                        this.UpdateTimer(timer + 1, (int)((timerCnt >> 16) << 10), true);
                    }
                }

                // Reset the original value
                uint count = Memory.ReadU16(this.memory.IORam, Memory.TM0D + (uint)(timer * 4));
                this.memory.TimerCnt[timer] = count << 10;
            }
        }

        public void UpdateTimers()
        {
            int cycles = this.timerCycles - this.cycles;

            for (int i = 0; i < 4; i++)
            {
                this.UpdateTimer(i, cycles, false);
            }

            this.timerCycles = this.cycles;
        }

        public void UpdateKeyState()
        {
            ushort KEYCNT = this.memory.ReadU16Debug(Memory.REG_BASE + Memory.KEYCNT);

            if ((KEYCNT & (1 << 14)) != 0)
            {
                if ((KEYCNT & (1 << 15)) != 0)
                {
                    KEYCNT &= 0x3FF;
                    if (((~this.keyState) & KEYCNT) == KEYCNT)
                        this.RequestIrq(12);
                }
                else
                {
                    KEYCNT &= 0x3FF;
                    if (((~this.keyState) & KEYCNT) != 0)
                        this.RequestIrq(12);
                }
            }

            this.memory.KeyState = this.keyState;
        }

        public void UpdateSound()
        {
            this.memory.SoundManager.Mix(this.soundCycles);
            this.soundCycles = 0;
        }

        public void Execute(int cycles)
        {
            this.cycles += cycles;
            this.timerCycles += cycles;
            this.soundCycles += cycles;
            this.breakpointHit = false;

            if (this.cpuHalted)
            {
                ushort ie = Memory.ReadU16(this.memory.IORam, Memory.IE);
                ushort iflag = Memory.ReadU16(this.memory.IORam, Memory.IF);

                if ((ie & iflag) != 0)
                {
                    this.cpuHalted = false;
                }
                else
                {
                    this.cycles = 0;
                    this.UpdateTimers();
                    this.UpdateSound();
                    return;
                }
            }

            while (this.cycles > 0)
            {
                if ((this.cpsr & Arm7Processor.T_MASK) == Arm7Processor.T_MASK)
                {
                    this.thumbCore.Execute();
                }
                else
                {
                    this.armCore.Execute();
                }

                this.UpdateTimers();
                this.UpdateSound();

                if (this.breakpointHit)
                {
                    break;
                }
            }
        }
    }
}