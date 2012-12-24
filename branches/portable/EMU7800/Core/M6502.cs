/*
 * M6502.cs
 *
 * CPU emulator for the MOS Technology 6502 microprocessor.
 * 
 * Copyright © 2003-2005 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public sealed class M6502
    {
        delegate void OpcodeHandler();

        OpcodeHandler[] Opcodes;

        const ushort
            // non-maskable interrupt vector
            NMI_VEC = 0xfffa,
            // reset vector
            RST_VEC = 0xfffc,
            // interrupt request vector
            IRQ_VEC = 0xfffe;

        readonly MachineBase M;
        AddressSpace Mem { get { return M.Mem; } }

        public ulong Clock { get; set; }
        public int RunClocks { get; set; }
        public int RunClocksMultiple { get; private set; }

        public bool EmulatorPreemptRequest { get; set; }
        public bool Jammed { get; set; }
        public bool IRQInterruptRequest { get; set; }
        public bool NMIInterruptRequest { get; set; }

        // 16-bit register
        // program counter
        public ushort PC { get; set; }

        // 8-bit registers
        // accumulator
        public byte A { get; set; }
        // x index register
        public byte X { get; set; }
        // y index register
        public byte Y { get; set; }
        // stack pointer
        public byte S { get; set; }
        // processor status
        public byte P { get; set; }

        public void Reset()
        {
            Jammed = false;

            // clear the stack
            S = 0xff;

            fI = fZ = true;

            // reset the program counter
            PC = WORD(Mem[RST_VEC], Mem[RST_VEC + 1]);

            clk(6);

            Log("{0} (PC:${1:x4}) reset", this, PC);
        }

        public override String ToString()
        {
            return "M6502 CPU";
        }

        public void Execute()
        {
            EmulatorPreemptRequest = false;

            while (RunClocks > 0 && !EmulatorPreemptRequest && !Jammed)
            {
                if (NMIInterruptRequest)
                {
                    InterruptNMI();
                    NMIInterruptRequest = false;
                }
                else if (IRQInterruptRequest)
                {
                    InterruptIRQ();
                    IRQInterruptRequest = false;
                }
                else
                {
                    Opcodes[Mem[PC++]]();
                }
            }
        }

        private M6502()
        {
            InstallOpcodes();

            Clock = 0;
            RunClocks = 0;
            RunClocksMultiple = 1;

            // initialize processor status, bit 5 is always set
            P = 1 << 5;
        }

        public M6502(MachineBase m, int runClocksMultiple) : this()
        {
            if (m == null)
                throw new ArgumentNullException("m");
            if (runClocksMultiple <= 0)
                throw new ArgumentException("runClocksMultiple must be greater than zero.");

            M = m;
            RunClocksMultiple = runClocksMultiple;
        }

        static byte MSB(ushort u16)
        {
            return (byte)(u16 >> 8);
        }

        static byte LSB(ushort u16)
        {
            return (byte)u16;
        }

        static ushort WORD(byte lsb, byte msb)
        {
            return (ushort)(lsb | msb << 8);
        }

        // Processor Status Flag Bits
        //

        // Flag bit setters and getters
        void fset(byte flag, bool value)
        {
            P = (byte)(value ? P | flag : P & ~flag);
        }

        bool fget(byte flag)
        {
            return (P & flag) != 0;
        }

        // Carry: set if the add produced a carry, if the subtraction
        //      produced a borrow.  Also used in shift instructions.
        bool fC
        {
            get { return fget(1 << 0); }
            set { fset(1 << 0, value); }
        }
    
        // Zero: set if the result of the last operation was zero
        bool fZ
        {
            get { return fget(1 << 1); }
            set { fset(1 << 1, value); }
        }

        // Irq Disable: set if maskable interrupts are disabled
        bool fI
        {
            get { return fget(1 << 2); }
            set { fset(1 << 2, value); }
        }

        // Decimal Mode: set if decimal mode active
        bool fD
        {
            get { return fget(1 << 3); }
            set { fset(1 << 3, value); }
        }

        // Brk: set if an interrupt caused by a BRK instruction,
        //      reset if caused by an internal interrupt
        bool fB
        {
            get { return fget(1 << 4); }
            set { fset(1 << 4, value); }
        }

        // Overflow: set if the addition of two-like-signed numbers
        //      or the subtraction of two unlike-signed numbers
        //      produces a result greater than +127 or less than -128.
        bool fV
        {
            get { return fget(1 << 6); }
            set { fset(1 << 6, value); }
        }

        // Negative: set if bit 7 of the accumulator is set
        bool fN
        {
            get { return fget(1 << 7); }
            set { fset(1 << 7, value); }
        }

        void set_fNZ(byte u8)
        {
            fN = (u8 & 0x80) != 0;
            fZ = (u8 & 0xff) == 0;
        }

        byte pull()
        {
            S++;
            return Mem[(ushort)(0x0100 + S)];
        }

        void push(byte data)
        {
            Mem[(ushort)(0x0100 + S)] = data;
            S--;
        }

        void clk(int ticks)
        {
            Clock += (ulong)ticks;
            RunClocks -= (ticks*RunClocksMultiple);
        }

        void InterruptNMI()
        {
            push(MSB(PC));
            push(LSB(PC));
            fB = false;
            push(P);
            fI = true;
            PC = WORD(Mem[NMI_VEC], Mem[NMI_VEC + 1]);
            clk(7);
        }

        void InterruptIRQ()
        {
            if (IRQInterruptRequest && !fI)
            {
                push(MSB(PC));
                push(LSB(PC));
                fB = false;
                push(P);
                fI = true;
                PC = WORD(Mem[IRQ_VEC], Mem[IRQ_VEC + 1]);
            }
            clk(7);
        }

        void br(bool cond, ushort ea)
        {
            if (cond)
            {
                clk( (MSB(PC) == MSB(ea)) ? 1 : 2 );
                PC = ea;
            }
        }


        // Relative: Bxx $aa  (branch instructions only)
        ushort aREL()
        {
            var bo = (sbyte)Mem[PC];
            PC++;
            return (ushort)(PC + bo);
        }

        // Zero Page: $aa
        ushort aZPG()
        {
            return WORD(Mem[PC++], 0x00);
        }

        // Zero Page Indexed,X: $aa,X
        ushort aZPX()
        {
            return WORD((byte)(Mem[PC++] + X), 0x00);
        }

        // Zero Page Indexed,Y: $aa,Y
        ushort aZPY()
        {
            return WORD((byte)(Mem[PC++] + Y), 0x00);
        }

        // Absolute: $aaaa
        ushort aABS()
        {
            var lsb = Mem[PC++];
            var msb = Mem[PC++];
            return WORD(lsb, msb);
        }

        // Absolute Indexed,X: $aaaa,X
        ushort aABX(int eclk)
        {
            var ea = aABS();
            if (LSB(ea) + X > 0xff)
            {
                clk(eclk);
            }
            return (ushort)(ea + X);
        }

        // Absolute Indexed,Y: $aaaa,Y
        ushort aABY(int eclk)
        {
            var ea = aABS();
            if (LSB(ea) + Y > 0xff)
            {
                clk(eclk);
            }
            return (ushort)(ea + Y);
        }

        // Indexed Indirect: ($aa,X)
        ushort aIDX()
        {
            var zpa = (byte)(Mem[PC++] + X);
            var lsb = Mem[zpa++];
            var msb = Mem[zpa];
            return WORD(lsb, msb);
        }

        // Indirect Indexed: ($aa),Y
        ushort aIDY(int eclk)
        {
            var zpa = Mem[PC++];
            var lsb = Mem[zpa++];
            var msb = Mem[zpa];
            if (lsb + Y > 0xff) 
            {
                clk(eclk);
            }
            return (ushort)(WORD(lsb, msb) + Y);
        }

        // Indirect Absolute: ($aaaa)    (only used by JMP)
        ushort aIND()
        {
            var ea = aABS();
            var lsb = Mem[ea];
            ea = WORD((byte)(LSB(ea) + 1), MSB(ea));   // NMOS 6502/7 quirk: does not fetch across page boundaries
            var msb = Mem[ea];
            return WORD(lsb, msb);
        }

        // aACC = Accumulator
        // aIMM = Immediate
        // aIMP = Implied

        // ADC: Add with carry
        void iADC(byte mem)
        {
            var c = fC ? 1 : 0;
            var sum = A + mem + c;
            fV = (~(A ^ mem) & (A ^ (sum & 0xff)) & 0x80) != 0;
            if (fD)
            {
                // NMOS 6502/7 quirk: The N, V, and Z flags reflect the binary result, not the BCD result
                var lo = (A & 0xf) + (mem & 0xf) + c;
                var hi = (A >> 4) + (mem >> 4);
                if (lo > 9)
                {
                    lo += 6;
                    hi++;
                }
                if (hi > 9)
                {
                    hi += 6;
                }
                A = (byte)((lo & 0xf) | (hi << 4));
                fC = (hi & 0x10) != 0;
            }
            else
            {
                A = (byte)sum;
                fC = (sum & 0x100) != 0;
            }
            set_fNZ((byte)sum);
        }

        // AND: Logical and
        void iAND(byte mem)
        {
            A &= mem;
            set_fNZ(A);
        }

        // ASL: Arithmetic shift left: C <- [7][6][5][4][3][2][1][0] <- 0
        byte iASL(byte mem)
        {
            fC = (mem & 0x80) != 0;
            mem <<= 1;
            set_fNZ(mem);
            return mem;
        }

        // BIT: Bit test
        void iBIT(byte mem)
        {
            fN = (mem & 0x80) != 0;
            fV = (mem & 0x40) != 0;
            fZ = (mem & A) == 0;
        }

        // BRK Force Break  (cause software interrupt)
        void iBRK()
        {
            PC++;
            fB = true;
            push(MSB(PC));
            push(LSB(PC));
            push(P);
            fI = true;
            var lsb = Mem[IRQ_VEC];
            var msb = Mem[IRQ_VEC+1];
            PC = WORD(lsb, msb);
        }

        // CLC: Clear carry flag
        void iCLC()
        {
            fC = false;
        }

        // CLD: Clear decimal mode
        void iCLD()
        {
            fD = false;
        }

        // CLI: Clear interrupt disable */
        void iCLI()
        {
            fI = false;
        }

        // CLV: Clear overflow flag
        void iCLV()
        {
            fV = false;
        }

        // CMP: Compare accumulator
        void iCMP(byte mem)
        {
            fC = A >= mem;
            set_fNZ((byte)(A - mem));
        }

        // CPX: Compare index X
        void iCPX(byte mem)
        {
            fC = X >= mem;
            set_fNZ((byte)(X - mem));
        }

        // CPY: Compare index Y
        void iCPY(byte mem)
        {
            fC = Y >= mem;
            set_fNZ((byte)(Y - mem));
        }

        // DEC: Decrement memory
        byte iDEC(byte mem)
        {
            mem--;
            set_fNZ(mem);
            return mem;
        }

        // DEX: Decrement index x
        void iDEX()
        {
            X--;
            set_fNZ(X);
        }

        // DEY: Decrement index y
        void iDEY()
        {
            Y--;
            set_fNZ(Y);
        }

        // EOR: Logical exclusive or
        void iEOR(byte mem)
        {
            A ^= mem;
            set_fNZ(A);
        }

        // INC: Increment memory
        byte iINC(byte mem)
        {
            mem++;
            set_fNZ(mem);
            return mem;
        }

        // INX: Increment index x
        void iINX()
        {
            X++;
            set_fNZ(X);
        }

        // INY: Increment index y
        void iINY()
        {
            Y++;
            set_fNZ(Y);
        }

        // JMP Jump to address
        void iJMP(ushort ea)
        {
            PC = ea;
        }

        // JSR Jump to subroutine
        void iJSR(ushort ea)
        {
            PC--;                   // NMOS 6502/7 quirk: iRTS compensates
            push(MSB(PC));
            push(LSB(PC));
            PC = ea;
        }

        // LDA: Load accumulator
        void iLDA(byte mem)
        {
            A = mem;
            set_fNZ(A);
        }

        // LDX: Load index X
        void iLDX(byte mem)
        {
            X = mem;
            set_fNZ(X);
        }

        // LDY: Load index Y
        void iLDY(byte mem)
        {
            Y = mem;
            set_fNZ(Y);
        }

        // LSR: Logic shift right: 0 -> [7][6][5][4][3][2][1][0] -> C
        byte iLSR(byte mem)
        {
            fC = (mem & 0x01) != 0;
            mem >>= 1;
            set_fNZ(mem);
            return mem;
        }

        // NOP: No operation
        void iNOP()
        {
            if (M.NOPRegisterDumping)
            {
                Log("NOP: {0}", M6502DASM.GetRegisters(this));
            }
        }

        // ORA: Logical inclusive or
        void iORA(byte mem)
        {
            A |= mem;
            set_fNZ(A);
        }

        // PHA: Push accumulator
        void iPHA()
        {
            push(A);
        }

        // PHP: Push processor status (flags)
        void iPHP()
        {
            push(P);
        }

        // PLA: Pull accumuator
        void iPLA()
        {
            A = pull();
            set_fNZ(A);
        }

        // PLP: Pull processor status (flags)
        void iPLP()
        {
            P = pull();
            fB = true;
        }

        // ROL: Rotate left: new C <- [7][6][5][4][3][2][1][0] <- C
        byte iROL(byte mem)
        {
            var d0 = (byte)(fC ? 0x01 : 0x00);

            fC = (mem & 0x80) != 0;
            mem <<= 1;
            mem |= d0;
            set_fNZ(mem);
            return mem;
        }

        // ROR: Rotate right: C -> [7][6][5][4][3][2][1][0] -> new C
        byte iROR(byte mem)
        {
            var d7 = (byte)(fC ? 0x80 : 0x00);

            fC = (mem & 0x01) != 0;
            mem >>= 1;
            mem |= d7;
            set_fNZ(mem);
            return mem;
        }

        // RTI: Return from interrupt
        void iRTI()
        {
            P = pull();
            var lsb = pull();
            var msb = pull();
            PC = WORD(lsb, msb);
            fB = true;
        }

        // RTS: Return from subroutine
        void iRTS()
        {
            var lsb = pull();
            var msb = pull();
            PC = WORD(lsb, msb);
            PC++;                   // NMOS 6502/7 quirk: iJSR compensates
        }

        // SBC: Subtract with carry (borrow)
        void iSBC(byte mem)
        {
            var c = fC ? 0 : 1;
            var sum = A - mem - c;
            fV = ((A ^ mem) & (A ^ (sum & 0xff)) & 0x80) != 0;
            if (fD)
            {
                // NMOS 6502/7 quirk: The N, V, and Z flags reflect the binary result, not the BCD result
                var lo = (A & 0xf) - (mem & 0xf) - c;
                var hi = (A >> 4) - (mem >> 4);
                if ((lo & 0x10) != 0)
                {
                    lo -= 6;
                    hi--;
                }
                if ((hi & 0x10) != 0)
                {
                    hi -= 6;
                }
                A = (byte)((lo & 0xf) | (hi << 4));
            }
            else
            {
                A = (byte)sum;
            }
            fC = (sum & 0x100) == 0;
            set_fNZ((byte)sum);
        }

        // SEC: Set carry flag
        void iSEC()
        {
            fC = true;
        }

        // SED: Set decimal mode
        void iSED()
        {
            fD = true;
        }

        // SEI: Set interrupt disable
        void iSEI()
        {
            fI = true;
        }

        // STA: Store accumulator
        byte iSTA()
        {
            return A;
        }

        // STX: Store index X
        byte iSTX()
        {
            return X;
        }

        // STY: Store index Y
        byte iSTY()
        {
            return Y;
        }

        // TAX: Transfer accumlator to index X
        void iTAX()
        {
            X = A;
            set_fNZ(X);
        }

        // TAY: Transfer accumlator to index Y
        void iTAY()
        {
            Y = A;
            set_fNZ(Y);
        }

        // TSX: Transfer stack to index X
        void iTSX()
        {
            X = S;
            set_fNZ(X);
        }

        // TXA: Transfer index X to accumlator
        void iTXA()
        {
            A = X;
            set_fNZ(A);
        }

        // TXS: Transfer index X to stack
        void iTXS()
        {
            S = X;
            // No flags set..!  Weird, huh?
        }

        // TYA: Transfer index Y to accumulator
        void iTYA()
        {
            A = Y;
            set_fNZ(A);
        }

        // Illegal opcodes

        // KIL: Jam the processor
        void iKIL()
        {
            Jammed = true;
            Log("{0}: Processor jammed!", this);
        }

        // LAX: Load accumulator and index x
        void iLAX(byte mem)
        {
            A = X = mem;
            set_fNZ(A);
        }

        // ISB: Increment and subtract with carry
        void iISB(byte mem)
        {
            mem++;
            iSBC(mem);
        }

        // RLA: Rotate left and logical and accumulator
        // new C <- [7][6][5][4][3][2][1][0] <- C
        void iRLA(byte mem)
        {
            var d0 = (byte)(fC ? 0x01 : 0x00);

            fC = (mem & 0x80) != 0;
            mem <<= 1;
            mem |= d0;

            A &= mem;
            set_fNZ(A);
        }

        // SAX: logical and accumulator with index X and store
        byte iSAX()
        {
            return (byte)(A & X);
        }

        void InstallOpcodes()
        {
            Opcodes = new OpcodeHandler[0x100];
            ushort EA;

            Opcodes[0x65] = delegate { EA = aZPG();  clk(3); iADC(Mem[EA]); };
            Opcodes[0x75] = delegate { EA = aZPX();  clk(4); iADC(Mem[EA]); };
            Opcodes[0x61] = delegate { EA = aIDX();  clk(6); iADC(Mem[EA]); };
            Opcodes[0x71] = delegate { EA = aIDY(1); clk(5); iADC(Mem[EA]); };
            Opcodes[0x79] = delegate { EA = aABY(1); clk(4); iADC(Mem[EA]); };
            Opcodes[0x6d] = delegate { EA = aABS();  clk(4); iADC(Mem[EA]); };
            Opcodes[0x7d] = delegate { EA = aABX(1); clk(4); iADC(Mem[EA]); };
            Opcodes[0x69] = delegate { /*aIMM*/      clk(2); iADC(Mem[PC++]); };

            Opcodes[0x25] = delegate { EA = aZPG();  clk(3); iAND(Mem[EA]); }; // may be 2 clk
            Opcodes[0x35] = delegate { EA = aZPX();  clk(4); iAND(Mem[EA]); }; // may be 3 clk
            Opcodes[0x21] = delegate { EA = aIDX();  clk(6); iAND(Mem[EA]); };
            Opcodes[0x31] = delegate { EA = aIDY(1); clk(5); iAND(Mem[EA]); };
            Opcodes[0x2d] = delegate { EA = aABS();  clk(4); iAND(Mem[EA]); };
            Opcodes[0x39] = delegate { EA = aABY(1); clk(4); iAND(Mem[EA]); };
            Opcodes[0x3d] = delegate { EA = aABX(1); clk(4); iAND(Mem[EA]); };
            Opcodes[0x29] = delegate {    /*aIMM*/   clk(2); iAND(Mem[PC++]); };

            Opcodes[0x06] = delegate { EA = aZPG();  clk(5); Mem[EA] = iASL(Mem[EA]); };
            Opcodes[0x16] = delegate { EA = aZPX();  clk(6); Mem[EA] = iASL(Mem[EA]); };
            Opcodes[0x0e] = delegate { EA = aABS();  clk(6); Mem[EA] = iASL(Mem[EA]); };
            Opcodes[0x1e] = delegate { EA = aABX(0); clk(7); Mem[EA] = iASL(Mem[EA]); };
            Opcodes[0x0a] = delegate {    /*aACC*/   clk(2);       A = iASL(A); };

            Opcodes[0x24] = delegate { EA = aZPG();  clk(3); iBIT(Mem[EA]); };
            Opcodes[0x2c] = delegate { EA = aABS();  clk(4); iBIT(Mem[EA]); };

            Opcodes[0x10] = delegate { EA = aREL();  clk(2); br(!fN, EA); /* BPL */ };
            Opcodes[0x30] = delegate { EA = aREL();  clk(2); br( fN, EA); /* BMI */ };
            Opcodes[0x50] = delegate { EA = aREL();  clk(2); br(!fV, EA); /* BVC */ };
            Opcodes[0x70] = delegate { EA = aREL();  clk(2); br( fV, EA); /* BVS */ };
            Opcodes[0x90] = delegate { EA = aREL();  clk(2); br(!fC, EA); /* BCC */ };
            Opcodes[0xb0] = delegate { EA = aREL();  clk(2); br( fC, EA); /* BCS */ };
            Opcodes[0xd0] = delegate { EA = aREL();  clk(2); br(!fZ, EA); /* BNE */ };
            Opcodes[0xf0] = delegate { EA = aREL();  clk(2); br( fZ, EA); /* BEQ */ };

            Opcodes[0x00] = delegate {    /*aIMP*/   clk(7); iBRK(); };

            Opcodes[0x18] = delegate {    /*aIMP*/   clk(2); iCLC(); };

            Opcodes[0xd8] = delegate {    /*aIMP*/   clk(2); iCLD(); };

            Opcodes[0x58] = delegate {    /*aIMP*/   clk(2); iCLI(); };

            Opcodes[0xb8] = delegate {    /*aIMP*/   clk(2); iCLV(); };

            Opcodes[0xc5] = delegate { EA = aZPG();  clk(3); iCMP(Mem[EA]); };
            Opcodes[0xd5] = delegate { EA = aZPX();  clk(4); iCMP(Mem[EA]); };
            Opcodes[0xc1] = delegate { EA = aIDX();  clk(6); iCMP(Mem[EA]); };
            Opcodes[0xd1] = delegate { EA = aIDY(1); clk(5); iCMP(Mem[EA]); };
            Opcodes[0xcd] = delegate { EA = aABS();  clk(4); iCMP(Mem[EA]); };
            Opcodes[0xdd] = delegate { EA = aABX(1); clk(4); iCMP(Mem[EA]); };
            Opcodes[0xd9] = delegate { EA = aABY(1); clk(4); iCMP(Mem[EA]); };
            Opcodes[0xc9] = delegate { /*aIMM*/      clk(2); iCMP(Mem[PC++]); };

            Opcodes[0xe4] = delegate { EA = aZPG();  clk(3); iCPX(Mem[EA]); };
            Opcodes[0xec] = delegate { EA = aABS();  clk(4); iCPX(Mem[EA]); };
            Opcodes[0xe0] = delegate { /*aIMM*/      clk(2); iCPX(Mem[PC++]); };

            Opcodes[0xc4] = delegate { EA = aZPG();  clk(3); iCPY(Mem[EA]); };
            Opcodes[0xcc] = delegate { EA = aABS();  clk(4); iCPY(Mem[EA]); };
            Opcodes[0xc0] = delegate { /*aIMM*/      clk(2); iCPY(Mem[PC++]); };

            Opcodes[0xc6] = delegate { EA = aZPG();  clk(5); Mem[EA] = iDEC(Mem[EA]); };
            Opcodes[0xd6] = delegate { EA = aZPX();  clk(6); Mem[EA] = iDEC(Mem[EA]); };
            Opcodes[0xce] = delegate { EA = aABS();  clk(6); Mem[EA] = iDEC(Mem[EA]); };
            Opcodes[0xde] = delegate { EA = aABX(0); clk(7); Mem[EA] = iDEC(Mem[EA]); };

            Opcodes[0xca] = delegate {    /*aIMP*/   clk(2); iDEX(); };

            Opcodes[0x88] = delegate {    /*aIMP*/   clk(2); iDEY(); };

            Opcodes[0x45] = delegate { EA = aZPG();  clk(3); iEOR(Mem[EA]); };
            Opcodes[0x55] = delegate { EA = aZPX();  clk(4); iEOR(Mem[EA]); };
            Opcodes[0x41] = delegate { EA = aIDX();  clk(6); iEOR(Mem[EA]); };
            Opcodes[0x51] = delegate { EA = aIDY(1); clk(5); iEOR(Mem[EA]); };
            Opcodes[0x4d] = delegate { EA = aABS();  clk(4); iEOR(Mem[EA]); };
            Opcodes[0x5d] = delegate { EA = aABX(1); clk(4); iEOR(Mem[EA]); };
            Opcodes[0x59] = delegate { EA = aABY(1); clk(4); iEOR(Mem[EA]); };
            Opcodes[0x49] = delegate {    /*aIMM*/   clk(2); iEOR(Mem[PC++]); };

            Opcodes[0xe6] = delegate { EA = aZPG();  clk(5); Mem[EA] = iINC(Mem[EA]); };
            Opcodes[0xf6] = delegate { EA = aZPX();  clk(6); Mem[EA] = iINC(Mem[EA]); };
            Opcodes[0xee] = delegate { EA = aABS();  clk(6); Mem[EA] = iINC(Mem[EA]); };
            Opcodes[0xfe] = delegate { EA = aABX(0); clk(7); Mem[EA] = iINC(Mem[EA]); };

            Opcodes[0xe8] = delegate {    /*aIMP*/   clk(2); iINX(); };

            Opcodes[0xc8] = delegate {    /*aIMP*/   clk(2); iINY(); };

            Opcodes[0xa5] = delegate { EA = aZPG();  clk(3); iLDA(Mem[EA]); };
            Opcodes[0xb5] = delegate { EA = aZPX();  clk(4); iLDA(Mem[EA]); };
            Opcodes[0xa1] = delegate { EA = aIDX();  clk(6); iLDA(Mem[EA]); };
            Opcodes[0xb1] = delegate { EA = aIDY(1); clk(5); iLDA(Mem[EA]); };
            Opcodes[0xad] = delegate { EA = aABS();  clk(4); iLDA(Mem[EA]); };
            Opcodes[0xbd] = delegate { EA = aABX(1); clk(4); iLDA(Mem[EA]); };
            Opcodes[0xb9] = delegate { EA = aABY(1); clk(4); iLDA(Mem[EA]); };
            Opcodes[0xa9] = delegate {    /*aIMM*/   clk(2); iLDA(Mem[PC++]); };

            Opcodes[0xa6] = delegate { EA = aZPG();  clk(3); iLDX(Mem[EA]); };
            Opcodes[0xb6] = delegate { EA = aZPY();  clk(4); iLDX(Mem[EA]); };
            Opcodes[0xae] = delegate { EA = aABS();  clk(4); iLDX(Mem[EA]); };
            Opcodes[0xbe] = delegate { EA = aABY(1); clk(4); iLDX(Mem[EA]); };
            Opcodes[0xa2] = delegate {    /*aIMM*/   clk(2); iLDX(Mem[PC++]); };

            Opcodes[0xa4] = delegate { EA = aZPG();  clk(3); iLDY(Mem[EA]); };
            Opcodes[0xb4] = delegate { EA = aZPX();  clk(4); iLDY(Mem[EA]); };
            Opcodes[0xac] = delegate { EA = aABS();  clk(4); iLDY(Mem[EA]); };
            Opcodes[0xbc] = delegate { EA = aABX(1); clk(4); iLDY(Mem[EA]); };
            Opcodes[0xa0] = delegate {    /*aIMM*/   clk(2); iLDY(Mem[PC++]); };

            Opcodes[0x46] = delegate { EA = aZPG();  clk(5); Mem[EA] = iLSR(Mem[EA]); };
            Opcodes[0x56] = delegate { EA = aZPX();  clk(6); Mem[EA] = iLSR(Mem[EA]); };
            Opcodes[0x4e] = delegate { EA = aABS();  clk(6); Mem[EA] = iLSR(Mem[EA]); };
            Opcodes[0x5e] = delegate { EA = aABX(0); clk(7); Mem[EA] = iLSR(Mem[EA]); };
            Opcodes[0x4a] = delegate {    /*aACC*/   clk(2);       A = iLSR(A); };

            Opcodes[0x4c] = delegate { EA = aABS();  clk(3); iJMP(EA); };
            Opcodes[0x6c] = delegate { EA = aIND();  clk(5); iJMP(EA); };

            Opcodes[0x20] = delegate { EA = aABS();  clk(6); iJSR(EA); };

            Opcodes[0xea] = delegate {    /*aIMP*/   clk(2); iNOP(); };

            Opcodes[0x05] = delegate { EA = aZPG();  clk(3); iORA(Mem[EA]); }; // may be 2 clk
            Opcodes[0x15] = delegate { EA = aZPX();  clk(4); iORA(Mem[EA]); }; // may be 3 clk
            Opcodes[0x01] = delegate { EA = aIDX();  clk(6); iORA(Mem[EA]); };
            Opcodes[0x11] = delegate { EA = aIDY(1); clk(5); iORA(Mem[EA]); };
            Opcodes[0x0d] = delegate { EA = aABS();  clk(4); iORA(Mem[EA]); };
            Opcodes[0x1d] = delegate { EA = aABX(1); clk(4); iORA(Mem[EA]); };
            Opcodes[0x19] = delegate { EA = aABY(1); clk(4); iORA(Mem[EA]); };
            Opcodes[0x09] = delegate {    /*aIMM*/   clk(2); iORA(Mem[PC++]); };

            Opcodes[0x48] = delegate {    /*aIMP*/   clk(3); iPHA(); };

            Opcodes[0x68] = delegate {    /*aIMP*/   clk(4); iPLA(); };

            Opcodes[0x08] = delegate {    /*aIMP*/   clk(3); iPHP(); };

            Opcodes[0x28] = delegate {    /*aIMP*/   clk(4); iPLP(); };

            Opcodes[0x26] = delegate { EA = aZPG();  clk(5); Mem[EA] = iROL(Mem[EA]); };
            Opcodes[0x36] = delegate { EA = aZPX();  clk(6); Mem[EA] = iROL(Mem[EA]); };
            Opcodes[0x2e] = delegate { EA = aABS();  clk(6); Mem[EA] = iROL(Mem[EA]); };
            Opcodes[0x3e] = delegate { EA = aABX(0); clk(7); Mem[EA] = iROL(Mem[EA]); };
            Opcodes[0x2a] = delegate {    /*aACC*/   clk(2);       A = iROL(A);       };

            Opcodes[0x66] = delegate { EA = aZPG();  clk(5); Mem[EA] = iROR(Mem[EA]); };
            Opcodes[0x76] = delegate { EA = aZPX();  clk(6); Mem[EA] = iROR(Mem[EA]); };
            Opcodes[0x6e] = delegate { EA = aABS();  clk(6); Mem[EA] = iROR(Mem[EA]); };
            Opcodes[0x7e] = delegate { EA = aABX(0); clk(7); Mem[EA] = iROR(Mem[EA]); };
            Opcodes[0x6a] = delegate {    /*aACC*/   clk(2);       A = iROR(A); };

            Opcodes[0x40] = delegate {    /*aIMP*/   clk(6); iRTI(); };

            Opcodes[0x60] = delegate {    /*aIMP*/   clk(6); iRTS(); };

            Opcodes[0xe5] = delegate { EA = aZPG();  clk(3); iSBC(Mem[EA]); };
            Opcodes[0xf5] = delegate { EA = aZPX();  clk(4); iSBC(Mem[EA]); };
            Opcodes[0xe1] = delegate { EA = aIDX();  clk(6); iSBC(Mem[EA]); };
            Opcodes[0xf1] = delegate { EA = aIDY(1); clk(5); iSBC(Mem[EA]); };
            Opcodes[0xed] = delegate { EA = aABS();  clk(4); iSBC(Mem[EA]); };
            Opcodes[0xfd] = delegate { EA = aABX(1); clk(4); iSBC(Mem[EA]); };
            Opcodes[0xf9] = delegate { EA = aABY(1); clk(4); iSBC(Mem[EA]); };
            Opcodes[0xe9] = delegate {    /*aIMM*/   clk(2); iSBC(Mem[PC++]); };

            Opcodes[0x38] = delegate {    /*aIMP*/   clk(2); iSEC(); };

            Opcodes[0xf8] = delegate {    /*aIMP*/   clk(2); iSED(); };

            Opcodes[0x78] = delegate {    /*aIMP*/   clk(2); iSEI(); };

            Opcodes[0x85] = delegate { EA = aZPG();  clk(3); Mem[EA] = iSTA(); };
            Opcodes[0x95] = delegate { EA = aZPX();  clk(4); Mem[EA] = iSTA(); };
            Opcodes[0x81] = delegate { EA = aIDX();  clk(6); Mem[EA] = iSTA(); };
            Opcodes[0x91] = delegate { EA = aIDY(0); clk(6); Mem[EA] = iSTA(); };
            Opcodes[0x8d] = delegate { EA = aABS();  clk(4); Mem[EA] = iSTA(); };
            Opcodes[0x99] = delegate { EA = aABY(0); clk(5); Mem[EA] = iSTA(); };
            Opcodes[0x9d] = delegate { EA = aABX(0); clk(5); Mem[EA] = iSTA(); };

            Opcodes[0x86] = delegate { EA = aZPG();  clk(3); Mem[EA] = iSTX(); };
            Opcodes[0x96] = delegate { EA = aZPY();  clk(4); Mem[EA] = iSTX(); };
            Opcodes[0x8e] = delegate { EA = aABS();  clk(4); Mem[EA] = iSTX(); };

            Opcodes[0x84] = delegate { EA = aZPG();  clk(3); Mem[EA] = iSTY(); };
            Opcodes[0x94] = delegate { EA = aZPX();  clk(4); Mem[EA] = iSTY(); };
            Opcodes[0x8c] = delegate { EA = aABS();  clk(4); Mem[EA] = iSTY(); };

            Opcodes[0xaa] = delegate {    /*aIMP*/   clk(2); iTAX(); };

            Opcodes[0xa8] = delegate {    /*aIMP*/   clk(2); iTAY(); };

            Opcodes[0xba] = delegate {    /*aIMP*/   clk(2); iTSX(); };

            Opcodes[0x8a] = delegate {    /*aIMP*/   clk(2); iTXA(); };

            Opcodes[0x9a] = delegate {    /*aIMP*/   clk(2); iTXS(); };

            Opcodes[0x98] = delegate {    /*aIMP*/   clk(2); iTYA(); };

            // Illegal opcodes
            foreach (int opCode in new ushort[] { 0x02, 0x12, 0x22, 0x32, 0x42, 0x52, 0x62, 0x72, 0x92, 0xb2, 0xd2, 0xf2 })
            {
                Opcodes[opCode] = delegate { clk(2); iKIL(); };
            }
            Opcodes[0x3f] = delegate { EA = aABX(0); clk(4); iRLA(Mem[EA]); };
            Opcodes[0xa7] = delegate { EA = aZPX();  clk(3); iLAX(Mem[EA]); };
            Opcodes[0xb3] = delegate { EA = aIDY(0); clk(6); iLAX(Mem[EA]); };
            Opcodes[0xef] = delegate { EA = aABS();  clk(6); iISB(Mem[EA]); };
            Opcodes[0x0c] = delegate { EA = aABS();  clk(2); iNOP(); };
            foreach (int opCode in new ushort[] { 0x1c, 0x3c, 0x5c, 0x7c, 0x9c, 0xdc, 0xfc })
            {
                Opcodes[opCode] = delegate { EA = aABX(0); clk(2); iNOP(); };
            }
            Opcodes[0x83] = delegate { EA = aIDX();  clk(6); Mem[EA] = iSAX(); };
            Opcodes[0x87] = delegate { EA = aZPG();  clk(3); Mem[EA] = iSAX(); };
            Opcodes[0x8f] = delegate { EA = aABS();  clk(4); Mem[EA] = iSAX(); };
            Opcodes[0x97] = delegate { EA = aZPY();  clk(4); Mem[EA] = iSAX(); };
            Opcodes[0xa3] = delegate { EA = aIDX();  clk(6); iLAX(Mem[EA]); };
            Opcodes[0xb7] = delegate { EA = aZPY();  clk(4); iLAX(Mem[EA]); };
            Opcodes[0xaf] = delegate { EA = aABS();  clk(5); iLAX(Mem[EA]); };
            Opcodes[0xbf] = delegate { EA = aABY(0); clk(6); iLAX(Mem[EA]); };
            Opcodes[0xff] = delegate { EA = aABX(0); clk(7); iISB(Mem[EA]); };

            OpcodeHandler opNULL = () => Log("{0}:**UNKNOWN OPCODE: ${1:x2} at ${2:x4}\n", this, Mem[(ushort)(PC - 1)], PC - 1);

            for (var i=0; i < Opcodes.Length; i++)
            {
                if (Opcodes[i] == null)
                {
                    Opcodes[i] = opNULL;
                }
            }
        }

        #region Serialization Members

        public M6502(DeserializationContext input, MachineBase m, int runClocksMultiple) : this(m, runClocksMultiple)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
            Clock = input.ReadUInt64();
            RunClocks = input.ReadInt32();
            RunClocksMultiple = input.ReadInt32();
            EmulatorPreemptRequest = input.ReadBoolean();
            Jammed = input.ReadBoolean();
            IRQInterruptRequest = input.ReadBoolean();
            NMIInterruptRequest = input.ReadBoolean();
            PC = input.ReadUInt16();
            A = input.ReadByte();
            X = input.ReadByte();
            Y = input.ReadByte();
            S = input.ReadByte();
            P = input.ReadByte();
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
            output.Write(Clock);
            output.Write(RunClocks);
            output.Write(RunClocksMultiple);
            output.Write(EmulatorPreemptRequest);
            output.Write(Jammed);
            output.Write(IRQInterruptRequest);
            output.Write(NMIInterruptRequest);
            output.Write(PC);
            output.Write(A);
            output.Write(X);
            output.Write(Y);
            output.Write(S);
            output.Write(P);
        }

        #endregion

        #region Helpers

        void Log(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        #endregion
    }
}