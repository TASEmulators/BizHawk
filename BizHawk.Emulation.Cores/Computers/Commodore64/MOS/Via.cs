using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Via
    {
        [SaveState.DoNotSave] private const int PCR_INT_CONTROL_NEGATIVE_EDGE = 0x00;
        [SaveState.DoNotSave] private const int PCR_INT_CONTROL_POSITIVE_EDGE = 0x01;
        [SaveState.DoNotSave] private const int PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE = 0x00;
        [SaveState.DoNotSave] private const int PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE = 0x02;
        [SaveState.DoNotSave] private const int PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE = 0x04;
        [SaveState.DoNotSave] private const int PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE = 0x06;
        [SaveState.DoNotSave] private const int PCR_CONTROL_HANDSHAKE_OUTPUT = 0x08;
        [SaveState.DoNotSave] private const int PCR_CONTROL_PULSE_OUTPUT = 0x0A;
        [SaveState.DoNotSave] private const int PCR_CONTROL_LOW_OUTPUT = 0x0C;
        [SaveState.DoNotSave] private const int PCR_CONTROL_HIGH_OUTPUT = 0x0E;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_DISABLED = 0x00;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_SHIFT_IN_T2_ONCE = 0x04;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_SHIFT_IN_PHI2 = 0x08;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_SHIFT_IN_CLOCK = 0x0C;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_SHIFT_OUT_T2 = 0x10;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_SHIFT_OUT_T2_ONCE = 0x14;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_SHIFT_OUT_PHI2 = 0x18;
        [SaveState.DoNotSave] private const int ACR_SR_CONTROL_SHIFT_OUT_CLOCK = 0x1C;
        [SaveState.DoNotSave] private const int ACR_T2_CONTROL_TIMED = 0x00;
        [SaveState.DoNotSave] private const int ACR_T2_CONTROL_COUNT_ON_PB6 = 0x20;
        [SaveState.DoNotSave] private const int ACR_T1_CONTROL_INTERRUPT_ON_LOAD = 0x00;
        [SaveState.DoNotSave] private const int ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS = 0x40;
        [SaveState.DoNotSave] private const int ACR_T1_CONTROL_INTERRUPT_ON_LOAD_AND_ONESHOT_PB7 = 0x80;
        [SaveState.DoNotSave] private const int ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7 = 0xC0;

        [SaveState.SaveWithName("PortOutputA")]
        private int _pra;
        [SaveState.SaveWithName("PortDirectionA")]
        private int _ddra;
        [SaveState.SaveWithName("PortOutputB")]
        private int _prb;
        [SaveState.SaveWithName("PortDirectionB")]
        private int _ddrb;
        [SaveState.SaveWithName("Timer1Counter")]
        private int _t1C;
        [SaveState.SaveWithName("Timer1Latch")]
        private int _t1L;
        [SaveState.SaveWithName("Timer2Counter")]
        private int _t2C;
        [SaveState.SaveWithName("Timer2Latch")]
        private int _t2L;
        [SaveState.SaveWithName("ShiftRegister")]
        private int _sr;
        [SaveState.SaveWithName("AuxiliaryControlRegister")]
        private int _acr;
        [SaveState.SaveWithName("PeripheralControlRegister")]
        private int _pcr;
        [SaveState.SaveWithName("InterruptFlagRegister")]
        private int _ifr;
        [SaveState.SaveWithName("InterruptEnableRegister")]
        private int _ier;
        [SaveState.SaveWithName("Port")]
        private readonly Port _port;

        [SaveState.SaveWithName("PortLatchA")]
        private int _paLatch;
        [SaveState.SaveWithName("PortLatchB")]
        private int _pbLatch;

        [SaveState.SaveWithName("CA1InterruptControl")]
        private int _pcrCa1IntControl;
        [SaveState.SaveWithName("CA2Control")]
        private int _pcrCa2Control;
        [SaveState.SaveWithName("CB1InterruptControl")]
        private int _pcrCb1IntControl;
        [SaveState.SaveWithName("CB2Control")]
        private int _pcrCb2Control;
        [SaveState.SaveWithName("PortLatchEnableA")]
        private bool _acrPaLatchEnable;
        [SaveState.SaveWithName("PortLatchEnableB")]
        private bool _acrPbLatchEnable;
        [SaveState.SaveWithName("ShiftRegisterControl")]
        private int _acrSrControl;
        [SaveState.SaveWithName("Timer1Control")]
        private int _acrT1Control;
        [SaveState.SaveWithName("Timer2Control")]
        private int _acrT2Control;

        [SaveState.SaveWithName("PreviousCA1")]
        private bool _ca1L;
        [SaveState.SaveWithName("PreviousCA2")]
        private bool _ca2L;
        [SaveState.SaveWithName("PreviousCB1")]
        private bool _cb1L;
        [SaveState.SaveWithName("PreviousCB2")]
        private bool _cb2L;
        [SaveState.SaveWithName("PreviousPB6")]
        private bool _pb6L;

        [SaveState.SaveWithName("ResetCa2NextClock")]
        private bool _resetCa2NextClock;
        [SaveState.SaveWithName("ResetCb2NextClock")]
        private bool _resetCb2NextClock;

        [SaveState.SaveWithName("HandshakeCa2NextClock")]
        private bool _handshakeCa2NextClock;
        [SaveState.SaveWithName("HandshakeCb2NextClock")]
        private bool _handshakeCb2NextClock;

        [SaveState.SaveWithName("CA1")]
        public bool Ca1;
        [SaveState.SaveWithName("CA2")]
        public bool Ca2;
        [SaveState.SaveWithName("CB1")]
        public bool Cb1;
        [SaveState.SaveWithName("CB2")]
        public bool Cb2;
        [SaveState.SaveWithName("PB6")]
        private bool _pb6;

        [SaveState.SaveWithName("InterruptNextClock")]
        private int _interruptNextClock;
        [SaveState.SaveWithName("T1Loaded")]
        private bool _t1CLoaded;
        [SaveState.SaveWithName("T2Loaded")]
        private bool _t2CLoaded;
        [SaveState.SaveWithName("T1Delayed")]
        private int _t1Delayed;
        [SaveState.SaveWithName("T2Delayed")]
        private int _t2Delayed;

        public Via()
        {
            _port = new DisconnectedPort();
        }

        public Via(Func<int> readPrA, Func<int> readPrB)
        {
            _port = new DriverPort(readPrA, readPrB);
        }

        public Via(Func<bool> readClock, Func<bool> readData, Func<bool> readAtn, int driveNumber)
        {
            _port = new IecPort(readClock, readData, readAtn, driveNumber);
            _ca1L = true;
        }

        [SaveState.DoNotSave]
        public bool Irq
        {
            get { return (_ifr & 0x80) == 0; }
        }

        public void HardReset()
        {
            _pra = 0;
            _prb = 0;
            _ddra = 0;
            _ddrb = 0;
            _t1C = 0;
            _t1L = 0;
            _t2C = 0;
            _t2L = 0;
            _sr = 0;
            _acr = 0;
            _pcr = 0;
            _ifr = 0;
            _ier = 0;
            _paLatch = 0;
            _pbLatch = 0;
            _pcrCa1IntControl = 0;
            _pcrCa2Control = 0;
            _pcrCb1IntControl = 0;
            _pcrCb2Control = 0;
            _acrPaLatchEnable = false;
            _acrPbLatchEnable = false;
            _acrSrControl = 0;
            _acrT1Control = 0;
            _acrT2Control = 0;
            _ca1L = false;
            _cb1L = false;
            Ca1 = false;
            Ca2 = false;
            Cb1 = false;
            Cb2 = false;

            _pb6L = false;
            _pb6 = false;
            _resetCa2NextClock = false;
            _resetCb2NextClock = false;
            _handshakeCa2NextClock = false;
            _handshakeCb2NextClock = false;
            _interruptNextClock = 0;
            _t1CLoaded = false;
            _t2CLoaded = false;
        }

        public void ExecutePhase()
        {
            // Process delayed interrupts
            _ifr |= _interruptNextClock;
            _interruptNextClock = 0;

            // Process 'pulse' and 'handshake' outputs on CA2 and CB2

            if (_resetCa2NextClock)
            {
                Ca2 = true;
                _resetCa2NextClock = false;
            }
            else if (_handshakeCa2NextClock)
            {
                Ca2 = false;
                _resetCa2NextClock = _pcrCa2Control == PCR_CONTROL_PULSE_OUTPUT;
                _handshakeCa2NextClock = false;
            }

            if (_resetCb2NextClock)
            {
                Cb2 = true;
                _resetCb2NextClock = false;
            }
            else if (_handshakeCb2NextClock)
            {
                Cb2 = false;
                _resetCb2NextClock = _pcrCb2Control == PCR_CONTROL_PULSE_OUTPUT;
                _handshakeCb2NextClock = false;
            }

            // Count timers

            if (_t1Delayed > 0)
            {
                _t1Delayed--;
            }
            else
            {
                _t1C--;
                if (_t1C < 0)
                {
                    if (_t1CLoaded)
                    {
                        _interruptNextClock |= 0x40;
                        _t1CLoaded = false;
                    }
                    switch (_acrT1Control)
                    {
                        case ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS:
                            _t1C = _t1L;
                            _t1CLoaded = true;
                            break;
                        case ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7:
                            _t1C = _t1L;
                            _prb ^= 0x80;
                            _t1CLoaded = true;
                            break;
                    }
                    _t1C &= 0xFFFF;
                }
            }

            if (_t2Delayed > 0)
            {
                _t2Delayed--;
            }
            else
            {
                switch (_acrT2Control)
                {
                    case ACR_T2_CONTROL_TIMED:
                        _t2C--;
                        if (_t2C < 0)
                        {
                            if (_t2CLoaded)
                            {
                                _interruptNextClock |= 0x20;
                                _t2CLoaded = false;
                            }
                            _t2C = _t2L;
                        }
                        break;
                    case ACR_T2_CONTROL_COUNT_ON_PB6:
                        _pb6L = _pb6;
                        _pb6 = (_port.ReadExternalPrb() & 0x40) != 0;
                        if (!_pb6 && _pb6L)
                        {
                            _t2C--;
                            if (_t2C < 0)
                            {
                                _ifr |= 0x20;
                                _t2C = 0xFFFF;
                            }
                        }
                        break;
                }
            }

            // Process CA2

            switch (_pcrCa2Control)
            {
                case PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE:
                case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE:
                    if (_ca2L && !Ca2)
                        _ifr |= 0x01;
                    break;
                case PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE:
                case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE:
                    if (!_ca2L && Ca2)
                        _ifr |= 0x01;
                    break;
                case PCR_CONTROL_HANDSHAKE_OUTPUT:
                    if (_ca1L && !Ca1)
                    {
                        Ca2 = true;
                        _ifr |= 0x01;
                    }
                    break;
                case PCR_CONTROL_PULSE_OUTPUT:
                    break;
                case PCR_CONTROL_LOW_OUTPUT:
                    Ca2 = false;
                    break;
                case PCR_CONTROL_HIGH_OUTPUT:
                    Ca2 = true;
                    break;
            }

            // Process CB2

            switch (_pcrCb2Control)
            {
                case PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE:
                case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE:
                    if (_cb2L && !Cb2)
                        _ifr |= 0x08;
                    break;
                case PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE:
                case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE:
                    if (!_cb2L && Cb2)
                        _ifr |= 0x08;
                    break;
                case PCR_CONTROL_HANDSHAKE_OUTPUT:
                    if (_cb1L && !Cb1)
                    {
                        Cb2 = true;
                        _ifr |= 0x08;
                    }
                    break;
                case PCR_CONTROL_PULSE_OUTPUT:
                    break;
                case PCR_CONTROL_LOW_OUTPUT:
                    Cb2 = false;
                    break;
                case PCR_CONTROL_HIGH_OUTPUT:
                    Cb2 = true;
                    break;
            }

            // interrupt generation

            if ((_pcrCb1IntControl == PCR_INT_CONTROL_POSITIVE_EDGE && Cb1 && !_cb1L) ||
                (_pcrCb1IntControl == PCR_INT_CONTROL_NEGATIVE_EDGE && !Cb1 && _cb1L))
            {
                _ifr |= 0x10;
                if (_acrPbLatchEnable)
                {
                    _pbLatch = _port.ReadExternalPrb();
                }
            }

            if ((_pcrCa1IntControl == PCR_INT_CONTROL_POSITIVE_EDGE && Ca1 && !_ca1L) ||
                (_pcrCa1IntControl == PCR_INT_CONTROL_NEGATIVE_EDGE && !Ca1 && _ca1L))
            {
                _ifr |= 0x02;
                if (_acrPaLatchEnable)
                {
                    _paLatch = _port.ReadExternalPra();
                }
            }

            switch (_acrSrControl)
            {
                case ACR_SR_CONTROL_DISABLED:
                    _ifr &= 0xFB;
                    break;
                default:
                    break;
            }

            if ((_ifr & _ier & 0x7F) != 0)
            {
                _ifr |= 0x80;
            }
            else
            {
                _ifr &= 0x7F;
            }

            _ca1L = Ca1;
            _ca2L = Ca2;
            _cb1L = Cb1;
            _cb2L = Cb2;
        }

        public void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }
    }
}
