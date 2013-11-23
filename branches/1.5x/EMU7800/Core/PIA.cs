/*
 * PIA.cs
 *
 * The Peripheral Interface Adapter (6532) device.
 * a.k.a. RIOT (RAM I/O Timer?)
 *
 * Copyright © 2003, 2004, 2012 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public sealed class PIA : IDevice
    {
        readonly MachineBase M;

        readonly byte[] RAM = new byte[0x80];

        ulong TimerTarget;
        int TimerShift;
        bool IRQEnabled, IRQTriggered;

        public byte DDRA { get; private set; }
        public byte DDRB { get; private set; }

        public byte WrittenPortA { get; private set; }
        public byte WrittenPortB { get; private set; }

        #region IDevice Members

        public void Reset()
        {
            // Some games will loop/hang on $0284 if these are initialized to zero
            TimerShift = 10;
            TimerTarget = M.CPU.Clock + (ulong)(0xff << TimerShift);

            IRQEnabled = false;
            IRQTriggered = false;

            DDRA = 0;

            Log("{0} reset", this);
        }

        public byte this[ushort addr]
        {
            get { return peek(addr); }
            set { poke(addr, value); }
        }

        #endregion

        public override string ToString()
        {
            return "PIA/RIOT M6532";
        }

        #region Constructors

        private PIA()
        {
        }

        public PIA(MachineBase m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            M = m;
        }

        #endregion

        byte peek(ushort addr)
        {
            if ((addr & 0x200) == 0)
            {
                return RAM[addr & 0x7f];
            }

            switch ((byte)(addr & 7))
            {
                case 0:  // SWCHA: Controllers
                    return ReadPortA();
                case 1:  // SWCHA DDR: 0=input, 1=output
                    return DDRA;
                case 2:  // SWCHB: Console switches (on 7800, PB2 & PB4 are used)
                    return ReadPortB();
                case 3:  // SWCHB DDR: 0=input, 1=output
                    return 0;
                case 4:  // INTIM
                case 6:
                    return ReadTimerRegister();
                case 5:  // INTFLG
                case 7:
                    return ReadInterruptFlag();
                default:
                    LogDebug("PIA: Unhandled peek ${0:x4}, PC=${1:x4}", addr, M.CPU.PC);
                    return 0;
            }
        }

        void poke(ushort addr, byte data)
        {
            if ((addr & 0x200) == 0)
            {
                RAM[addr & 0x7f] = data;
                return;
            }

            // A2 Distinguishes I/O registers from the Timer
            if ((addr & 0x04) != 0)
            {
                if ((addr & 0x10) != 0)
                {
                    IRQEnabled = (addr & 0x08) != 0;
                    SetTimerRegister(data, addr & 3);
                }
                else
                {
                    LogDebug("PIA: Timer: Unhandled poke ${0:x4} w/${1:x2}, PC=${2:x4}", addr, data, M.CPU.PC);
                }
            }
            else
            {
                switch ((byte)(addr & 3))
                {
                    case 0:  // SWCHA:  Port A
                        WritePortA(data);
                        break;
                    case 1:  // SWACNT: Port A DDR
                        DDRA = data;
                        break;
                    case 2:  // SWCHB:  Port B
                        WritePortB(data);
                        break;
                    case 3:  // SWBCNT: Port B DDR
                        DDRB = data;
                        break;
                }
            }
        }

        // 0: TIM1T:  set    1 clock interval (  838 nsec/interval)
        // 1: TIM8T:  set    8 clock interval (  6.7 usec/interval)
        // 2: TIM64T: set   64 clock interval ( 53.6 usec/interval)
        // 3: T1024T: set 1024 clock interval (858.2 usec/interval)
        void SetTimerRegister(byte data, int interval)
        {
            IRQTriggered = false;
            TimerShift = new[] { 0, 3, 6, 10 }[interval];
            TimerTarget = M.CPU.Clock + (ulong)(data << TimerShift);
        }

        byte ReadTimerRegister()
        {
            IRQTriggered = false;
            var delta = (int)(TimerTarget - M.CPU.Clock);
            if (delta >= 0)
            {
                return (byte)(delta >> TimerShift);
            }
            if (delta != -1)
            {
                IRQTriggered = true;
            }
            return (byte)(delta >= -256 ? delta : 0);
        }

        byte ReadInterruptFlag()
        {
            var delta = (int)(TimerTarget - M.CPU.Clock);
            return (byte)((delta >= 0 || IRQEnabled && IRQTriggered) ? 0x00 : 0x80);
        }

        // PortA: Controller Jacks
        //
        //            Left Jack                Right Jack
        //          -------------             -------------
        //          \ 1 2 3 4 5 /             \ 1 2 3 4 5 /
        //           \ 6 7 8 9 /               \ 6 7 8 9 /
        //            ---------                 ---------
        //
        // pin 1   D4 PIA SWCHA            D0 PIA SWCHA
        // pin 2   D5 PIA SWCHA            D1 PIA SWCHA
        // pin 3   D6 PIA SWCHA            D2 PIA SWCHA
        // pin 4   D7 PIA SWCHA            D3 PIA SWCHA
        // pin 5   D7 TIA INPT1 (Dumped)   D7 TIA INPT3 (Dumped)   7800: Right Fire
        // pin 6   D7 TIA INPT4 (Latched)  D7 TIA INPT5 (Latched)  2600: Fire
        // pin 7   +5                      +5
        // pin 8   GND                     GND
        // pin 9   D7 TIA INPT0 (Dumped)   D7 TIA INPT2 (Dumped)   7800: Left Fire
        //
        byte ReadPortA()
        {
            var porta = 0;
            var mi = M.InputState;

            switch (mi.LeftControllerJack)
            {
                case Controller.Joystick:
                case Controller.ProLineJoystick:
                case Controller.BoosterGrip:
                    porta |= mi.SampleCapturedControllerActionState(0, ControllerAction.Up)    ? 0 : (1 << 4);
                    porta |= mi.SampleCapturedControllerActionState(0, ControllerAction.Down)  ? 0 : (1 << 5);
                    porta |= mi.SampleCapturedControllerActionState(0, ControllerAction.Left)  ? 0 : (1 << 6);
                    porta |= mi.SampleCapturedControllerActionState(0, ControllerAction.Right) ? 0 : (1 << 7);
                    break;
                case Controller.Driving:
                    porta |= mi.SampleCapturedDrivingState(0) << 4;
                    break;
                case Controller.Paddles:
                    porta |= mi.SampleCapturedControllerActionState(0, ControllerAction.Trigger) ? 0 : (1 << 7);
                    porta |= mi.SampleCapturedControllerActionState(1, ControllerAction.Trigger) ? 0 : (1 << 6);
                    break;
                case Controller.Lightgun:
                    porta |= mi.SampleCapturedControllerActionState(0, ControllerAction.Trigger) ? (1 << 4) : 0;
                    break;
            }

            switch (mi.RightControllerJack)
            {
                case Controller.Joystick:
                case Controller.ProLineJoystick:
                case Controller.BoosterGrip:
                    porta |= mi.SampleCapturedControllerActionState(1, ControllerAction.Up)    ? 0 : (1 << 0);
                    porta |= mi.SampleCapturedControllerActionState(1, ControllerAction.Down)  ? 0 : (1 << 1);
                    porta |= mi.SampleCapturedControllerActionState(1, ControllerAction.Left)  ? 0 : (1 << 2);
                    porta |= mi.SampleCapturedControllerActionState(1, ControllerAction.Right) ? 0 : (1 << 3);
                    break;
                case Controller.Driving:
                    porta |= mi.SampleCapturedDrivingState(1);
                    break;
                case Controller.Paddles:
                    porta |= mi.SampleCapturedControllerActionState(2, ControllerAction.Trigger) ? 0 : (1 << 3);
                    porta |= mi.SampleCapturedControllerActionState(3, ControllerAction.Trigger) ? 0 : (1 << 2);
                    break;
                case Controller.Lightgun:
                    porta |= mi.SampleCapturedControllerActionState(1, ControllerAction.Trigger) ? (1 << 0) : 0;
                    break;
            }

            return (byte)porta;
        }

        void WritePortA(byte porta)
        {
            WrittenPortA = (byte)((porta & DDRA) | (WrittenPortA & (~DDRA)));
        }

        void WritePortB(byte portb)
        {
            WrittenPortB = (byte)((portb & DDRB) | (WrittenPortB & (~DDRB)));
        }

        // PortB: Console Switches
        //
        // D0 Game Reset  0=on
        // D1 Game Select 0=on
        // D2 (used on 7800)
        // D3 Console Color 1=Color, 0=B/W
        // D4 (used on 7800)
        // D5 (unused)
        // D6 Left  Difficulty A 1=A (pro), 0=B (novice)
        // D7 Right Difficulty A 1=A (pro), 0=B (novice)
        //
        byte ReadPortB()
        {
            var portb = 0;
            var mi = M.InputState;

            portb |= mi.SampleCapturedConsoleSwitchState(ConsoleSwitch.GameReset)        ? 0 : (1 << 0);
            portb |= mi.SampleCapturedConsoleSwitchState(ConsoleSwitch.GameSelect)       ? 0 : (1 << 1);
            portb |= mi.SampleCapturedConsoleSwitchState(ConsoleSwitch.GameBW)           ? 0 : (1 << 3);
            portb |= mi.SampleCapturedConsoleSwitchState(ConsoleSwitch.LeftDifficultyA)  ? (1 << 6) : 0;
            portb |= mi.SampleCapturedConsoleSwitchState(ConsoleSwitch.RightDifficultyA) ? (1 << 7) : 0;

            return (byte)portb;
        }

        #region Serialization Members

        public PIA(DeserializationContext input, MachineBase m) : this(m)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            var version = input.CheckVersion(1, 2);
            RAM = input.ReadExpectedBytes(0x80);
            TimerTarget = input.ReadUInt64();
            TimerShift = input.ReadInt32();
            IRQEnabled = input.ReadBoolean();
            IRQTriggered = input.ReadBoolean();
            DDRA = input.ReadByte();
            WrittenPortA = input.ReadByte();
            if (version > 1)
            {
                DDRB = input.ReadByte();
                WrittenPortB = input.ReadByte();
            }
        }

        public void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(2);
            output.Write(RAM);
            output.Write(TimerTarget);
            output.Write(TimerShift);
            output.Write(IRQEnabled);
            output.Write(IRQTriggered);
            output.Write(DDRA);
            output.Write(WrittenPortA);
            output.Write(DDRB);
            output.Write(WrittenPortB);
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

        [System.Diagnostics.Conditional("DEBUG")]
        void AssertDebug(bool cond)
        {
            if (!cond)
                System.Diagnostics.Debugger.Break();
        }

        #endregion
    }
}