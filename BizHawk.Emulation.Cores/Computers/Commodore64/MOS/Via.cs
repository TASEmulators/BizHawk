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
        private const int PCR_INT_CONTROL_NEGATIVE_EDGE = 0x00;
        private const int PCR_INT_CONTROL_POSITIVE_EDGE = 0x01;
        private const int PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE = 0x00;
        private const int PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE = 0x02;
        private const int PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE = 0x04;
        private const int PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE = 0x06;
        private const int PCR_CONTROL_HANDSHAKE_OUTPUT = 0x08;
        private const int PCR_CONTROL_PULSE_OUTPUT = 0x0A;
        private const int PCR_CONTROL_LOW_OUTPUT = 0x0C;
        private const int PCR_CONTROL_HIGH_OUTPUT = 0x0E;
        private const int ACR_SR_CONTROL_DISABLED = 0x00;
        private const int ACR_SR_CONTROL_SHIFT_IN_T2_ONCE = 0x04;
        private const int ACR_SR_CONTROL_SHIFT_IN_PHI2 = 0x08;
        private const int ACR_SR_CONTROL_SHIFT_IN_CLOCK = 0x0C;
        private const int ACR_SR_CONTROL_SHIFT_OUT_T2 = 0x10;
        private const int ACR_SR_CONTROL_SHIFT_OUT_T2_ONCE = 0x14;
        private const int ACR_SR_CONTROL_SHIFT_OUT_PHI2 = 0x18;
        private const int ACR_SR_CONTROL_SHIFT_OUT_CLOCK = 0x1C;
        private const int ACR_T2_CONTROL_TIMED = 0x00;
        private const int ACR_T2_CONTROL_COUNT_ON_PB6 = 0x20;
        private const int ACR_T1_CONTROL_INTERRUPT_ON_LOAD = 0x00;
        private const int ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS = 0x40;
        private const int ACR_T1_CONTROL_INTERRUPT_ON_LOAD_AND_ONESHOT_PB7 = 0x80;
        private const int ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7 = 0xC0;

        private int _pra;
        private int _ddra;
        private int _prb;
        private int _ddrb;
        private int _t1C;
        private int _t1L;
        private int _t2C;
        private int _t2L;
        private int _sr;
        private int _acr;
        private int _pcr;
        private int _ifr;
        private int _ier;
        private readonly Port _port;

        private int _paLatch;
        private int _pbLatch;

        private int _pcrCa1IntControl;
        private int _pcrCa2Control;
        private int _pcrCb1IntControl;
        private int _pcrCb2Control;
        private bool _acrPaLatchEnable;
        private bool _acrPbLatchEnable;
        private int _acrSrControl;
        private int _acrT1Control;
        private int _acrT2Control;

        private bool _ca1L;
        private bool _ca2L;
        private bool _cb1L;
        private bool _cb2L;

        private int _shiftCount;

        public bool Ca1;
        public bool Ca2;
        public bool Cb1;
        public bool Cb2;

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
        }

        private bool ProcessC2(bool c2, int control)
        {
            switch (control)
            {
                case PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE:
                    return c2;
                case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE:
                    return c2;
                case PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE:
                    return c2;
                case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE:
                    return c2;
                case PCR_CONTROL_HANDSHAKE_OUTPUT:
                    return c2;
                case PCR_CONTROL_PULSE_OUTPUT:
                    return c2;
                case PCR_CONTROL_LOW_OUTPUT:
                    return false;
                case PCR_CONTROL_HIGH_OUTPUT:
                    return true;
            }
            return c2;
        }

        public void ExecutePhase()
        {
            _t1C--;
            if (_t1C < 0)
            {
                if (_acrT1Control == ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS ||
                    _acrT1Control == ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7)
                {
                    _t1C = _t1L;
                }
                _ifr |= 0x40;
            }

            if (_acrT2Control == ACR_T2_CONTROL_TIMED)
            {
                _t2C--;
                if (_t2C < 0)
                {
                    _ifr |= 0x20;
                    _t2C = _t2L;
                }
            }

            Ca2 = ProcessC2(Ca2, _pcrCa2Control);
            Cb2 = ProcessC2(Cb2, _pcrCb2Control);

            // unknown behavior

            if (_acrT1Control != ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS &&
                _acrT1Control != ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7)
            {
                // unknown ACR T1 control
            }

            if (_acrT2Control != ACR_T2_CONTROL_TIMED)
            {
                // unknown ACR T2 control
            }


            // interrupt generation

            if ((_pcrCb1IntControl == PCR_INT_CONTROL_POSITIVE_EDGE && Cb1 && !_cb1L) ||
                (_pcrCb1IntControl == PCR_INT_CONTROL_NEGATIVE_EDGE && !Cb1 && _cb1L))
            {
                _ifr |= 0x01;
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
