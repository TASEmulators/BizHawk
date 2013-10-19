using System;
using System.Globalization;
using System.IO;

namespace BizHawk.Emulation.CPUs.H6280
{
    public sealed partial class HuC6280
    {
        public HuC6280()
        {
            Reset();
        }

        public void Reset()
        {
            A = 0;
            X = 0;
            Y = 0;
            //P = 0x14; // Set I and B
            P = 0x04; // Set I
            S = 0;
            PC = 0;
            PendingCycles = 0;
            TotalExecutedCycles = 0;
            LagIFlag = true;
            LowSpeed = true;
        }

        public void ResetPC()
        {
            PC = ReadWord(ResetVector);
        }

        // ==== CPU State ====

        public byte A;
        public byte X;
        public byte Y;
        public byte P;
        public ushort PC;
        public byte S;
        public byte[] MPR = new byte[8];

        public bool LagIFlag;
        public bool IRQ1Assert;
        public bool IRQ2Assert;
        public bool TimerAssert;
        public byte IRQControlByte, IRQNextControlByte;

        public long TotalExecutedCycles;
        public int PendingCycles;
        public bool LowSpeed;

        private bool InBlockTransfer = false;
        private ushort btFrom;
        private ushort btTo;
        private ushort btLen;
        private int btAlternator;

        // -- Timer Support --

        public int TimerTickCounter;
        public byte TimerReloadValue;
        public byte TimerValue;
        public bool TimerEnabled;

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[HuC6280]");
            writer.WriteLine("A {0:X2}", A);
            writer.WriteLine("X {0:X2}", X);
            writer.WriteLine("Y {0:X2}", Y);
            writer.WriteLine("P {0:X2}", P);
            writer.WriteLine("PC {0:X4}", PC);
            writer.WriteLine("S {0:X2}", S);
            writer.Write("MPR ");
            MPR.SaveAsHex(writer);
            writer.WriteLine("LagIFlag {0}", LagIFlag);
            writer.WriteLine("IRQ1Assert {0}", IRQ1Assert);
            writer.WriteLine("IRQ2Assert {0}", IRQ2Assert);
            writer.WriteLine("TimerAssert {0}", TimerAssert);
            writer.WriteLine("IRQControlByte {0:X2}", IRQControlByte);
            writer.WriteLine("IRQNextControlByte {0:X2}", IRQNextControlByte);
            writer.WriteLine("ExecutedCycles {0}", TotalExecutedCycles);
            writer.WriteLine("PendingCycles {0}", PendingCycles);
            writer.WriteLine("LowSpeed {0}", LowSpeed);
            writer.WriteLine("TimerTickCounter {0}", TimerTickCounter);
            writer.WriteLine("TimerReloadValue {0}", TimerReloadValue);
            writer.WriteLine("TimerValue {0}", TimerValue);
            writer.WriteLine("TimerEnabled {0}", TimerEnabled);
            writer.WriteLine("InBlockTransfer {0}", InBlockTransfer);
            writer.WriteLine("BTFrom {0}", btFrom);
            writer.WriteLine("BTTo {0}", btTo);
            writer.WriteLine("BTLen {0}", btLen);
            writer.WriteLine("BTAlternator {0}", btAlternator);
            writer.WriteLine("[/HuC6280]\n");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[/HuC6280]") break;
                if (args[0] == "A")
                    A = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "X")
                    X = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "Y")
                    Y = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "P")
                    P = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "PC")
                    PC = ushort.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "S")
                    S = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "MPR")
                    MPR.ReadFromHex(args[1]);
                else if (args[0] == "LagIFlag")
                    LagIFlag = bool.Parse(args[1]);
                else if (args[0] == "IRQ1Assert")
                    IRQ1Assert = bool.Parse(args[1]);
                else if (args[0] == "IRQ2Assert")
                    IRQ2Assert = bool.Parse(args[1]);
                else if (args[0] == "TimerAssert")
                    TimerAssert = bool.Parse(args[1]);
                else if (args[0] == "IRQControlByte")
                    IRQControlByte = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "IRQNextControlByte")
                    IRQNextControlByte = byte.Parse(args[1], NumberStyles.HexNumber);
                else if (args[0] == "ExecutedCycles")
                    TotalExecutedCycles = long.Parse(args[1]);
                else if (args[0] == "PendingCycles")
                    PendingCycles = int.Parse(args[1]);
                else if (args[0] == "LowSpeed")
                    LowSpeed = bool.Parse(args[1]);
                else if (args[0] == "TimerTickCounter")
                    TimerTickCounter = int.Parse(args[1]);
                else if (args[0] == "TimerReloadValue")
                    TimerReloadValue = byte.Parse(args[1]);
                else if (args[0] == "TimerValue")
                    TimerValue = byte.Parse(args[1]);
                else if (args[0] == "TimerEnabled")
                    TimerEnabled = bool.Parse(args[1]);
                else if (args[0] == "InBlockTransfer")
                    InBlockTransfer = bool.Parse(args[1]);
                else if (args[0] == "BTFrom")
                    btFrom = ushort.Parse(args[1]);
                else if (args[0] == "BTTo")
                    btTo = ushort.Parse(args[1]);
                else if (args[0] == "BTLen")
                    btLen = ushort.Parse(args[1]);
                else if (args[0] == "BTAlternator")
                    btAlternator = int.Parse(args[1]);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }

        public void SaveStateBinary(BinaryWriter writer)
        {
            writer.Write(A);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(P);
            writer.Write(PC);
            writer.Write(S);
            writer.Write(MPR);
            writer.Write(LagIFlag);
            writer.Write(IRQ1Assert);
            writer.Write(IRQ2Assert);
            writer.Write(TimerAssert);
            writer.Write(IRQControlByte);
            writer.Write(IRQNextControlByte);
            writer.Write(TotalExecutedCycles);
            writer.Write(PendingCycles);
            writer.Write(LowSpeed);

            writer.Write(TimerTickCounter);
            writer.Write(TimerReloadValue);
            writer.Write(TimerValue);
            writer.Write(TimerEnabled);

            writer.Write(InBlockTransfer);
            writer.Write(btFrom);
            writer.Write(btTo);
            writer.Write(btLen);
            writer.Write((byte)btAlternator);
        }

        public void LoadStateBinary(BinaryReader reader)
        {
            A = reader.ReadByte();
            X = reader.ReadByte();
            Y = reader.ReadByte();
            P = reader.ReadByte();
            PC = reader.ReadUInt16();
            S = reader.ReadByte();
            MPR = reader.ReadBytes(8);
            LagIFlag = reader.ReadBoolean();
            IRQ1Assert = reader.ReadBoolean();
            IRQ2Assert = reader.ReadBoolean();
            TimerAssert = reader.ReadBoolean();
            IRQControlByte = reader.ReadByte();
            IRQNextControlByte = reader.ReadByte();
            TotalExecutedCycles = reader.ReadInt64();
            PendingCycles = reader.ReadInt32();
            LowSpeed = reader.ReadBoolean();

            TimerTickCounter = reader.ReadInt32();
            TimerReloadValue = reader.ReadByte();
            TimerValue = reader.ReadByte();
            TimerEnabled = reader.ReadBoolean();

            InBlockTransfer = reader.ReadBoolean();
            btFrom = reader.ReadUInt16();
            btTo = reader.ReadUInt16();
            btLen = reader.ReadUInt16();
            btAlternator = reader.ReadByte();
        }

        // ==== Interrupts ====

        private const ushort ResetVector = 0xFFFE;
        private const ushort NMIVector   = 0xFFFC;
        private const ushort TimerVector = 0xFFFA;
        private const ushort IRQ1Vector  = 0xFFF8;
        private const ushort IRQ2Vector  = 0xFFF6;

        private const byte IRQ2Selector  = 0x01;
        private const byte IRQ1Selector  = 0x02;
        private const byte TimerSelector = 0x04;

        public void WriteIrqControl(byte value)
        {
            // There is a single-instruction delay before writes to the IRQ Control Byte take effect.
            value &= 7;
            IRQNextControlByte = value;
        }

        public void WriteIrqStatus()
        {
            TimerAssert = false;
        }

        public byte ReadIrqStatus()
        {
            byte status = 0;
            if (IRQ2Assert) status  |= 1;
            if (IRQ1Assert) status  |= 2;
            if (TimerAssert) status |= 4;
            return status;
        }

        public void WriteTimer(byte value)
        {
            value &= 0x7F;
            TimerReloadValue = value;
        }

        public void WriteTimerEnable(byte value)
        {
            if (TimerEnabled == false && (value & 1) == 1)
            {
                TimerValue = TimerReloadValue; // timer value is reset when toggled from off to on
                TimerTickCounter = 0;
            }
            TimerEnabled = (value & 1) == 1;
        }

        public byte ReadTimerValue()
        {
            if (TimerTickCounter + 5 > 1024)
            {
                // There exists a slight delay between when the timer counter is decremented and when 
                // the interrupt fires; games can detect it, so we hack it this way.
                return (byte) ((TimerValue - 1) & 0x7F);
            }
            return TimerValue;
        }

        // ==== Flags ====

        /// <summary>Carry Flag</summary>
        private bool FlagC
        {
            get { return (P & 0x01) != 0; }
            set { P = (byte)((P & ~0x01) | (value ? 0x01 : 0x00)); }
        }

        /// <summary>Zero Flag</summary>
        private bool FlagZ
        {
            get { return (P & 0x02) != 0; }
            set { P = (byte)((P & ~0x02) | (value ? 0x02 : 0x00)); }
        }

        /// <summary>Interrupt Disable Flag</summary>
        private bool FlagI
        {
            get { return (P & 0x04) != 0; }
            set { P = (byte)((P & ~0x04) | (value ? 0x04 : 0x00)); }
        }

        /// <summary>Decimal Mode Flag</summary>
        private bool FlagD
        {
            get { return (P & 0x08) != 0; }
            set { P = (byte)((P & ~0x08) | (value ? 0x08 : 0x00)); }
        }

        /// <summary>Break Flag</summary>
        private bool FlagB
        {
            get { return (P & 0x10) != 0; }
            set { P = (byte)((P & ~0x10) | (value ? 0x10 : 0x00)); }
        }

        /// <summary>T... Flag</summary>
        private bool FlagT
        {
            get { return (P & 0x20) != 0; }
            set { P = (byte)((P & ~0x20) | (value ? 0x20 : 0x00)); }
        }

        /// <summary>Overflow Flag</summary>
        private bool FlagV
        {
            get { return (P & 0x40) != 0; }
            set { P = (byte)((P & ~0x40) | (value ? 0x40 : 0x00)); }
        }

        /// <summary>Negative Flag</summary>
        private bool FlagN
        {
            get { return (P & 0x80) != 0; }
            set { P = (byte)((P & ~0x80) | (value ? 0x80 : 0x00)); }
        }

        // ==== Memory ====

        public Func<int, byte> ReadMemory21;
        public Action<int, byte> WriteMemory21;
        public Action<int, byte> WriteVDC;
        public Action<int> ThinkAction = delegate { };

        public byte ReadMemory(ushort address)
        {
            byte page = MPR[address >> 13];
            return ReadMemory21((page << 13) | (address & 0x1FFF));
        }

        public void WriteMemory(ushort address, byte value)
        {
            byte page = MPR[address >> 13];
            WriteMemory21((page << 13) | (address & 0x1FFF), value);
        }

        private ushort ReadWord(ushort address)
        {
            byte l = ReadMemory(address);
            byte h = ReadMemory(++address);
            return (ushort)((h << 8) | l);
        }

        private void WriteWord(ushort address, ushort value)
        {
            byte l = (byte)(value & 0xFF);
            byte h = (byte)(value >> 8);
            WriteMemory(address, l);
            WriteMemory(++address, h);
        }

        private ushort ReadWordPageWrap(ushort address)
        {
            ushort highAddress = (ushort)((address & 0xFF00) + ((address + 1) & 0xFF));
            return (ushort)(ReadMemory(address) | (ReadMemory(highAddress) << 8));
        }

        public string State()
        {
            int notused;
            string a = string.Format("{0:X4}  {1:X2} {2} ", PC, ReadMemory(PC), Disassemble(PC, out notused)).PadRight(41);
            string b = string.Format("A:{0:X2} X:{1:X2} Y:{2:X2} P:{3:X2} SP:{4:X2} Cy:{5}", A, X, Y, P, S, TotalExecutedCycles);
            string val = a + b + "   ";
            if (FlagN) val = val + "N";
            if (FlagV) val = val + "V";
            if (FlagT) val = val + "T";
            if (FlagB) val = val + "B";
            if (FlagD) val = val + "D";
            if (FlagI) val = val + "I";
            if (FlagZ) val = val + "Z";
            if (FlagC) val = val + "C";
            return val;
        }

        private static readonly byte[] TableNZ = 
        { 
            0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80
        };
    }
}