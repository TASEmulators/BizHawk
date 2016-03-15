using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Via
    {
        public int Peek(int addr)
        {
            return ReadRegister(addr & 0xF);
        }

        public void Poke(int addr, int val)
        {
            WriteRegister(addr & 0xF, val);
        }

        public int Read(int addr)
        {
            addr &= 0xF;
            switch (addr)
            {
                case 0x0:
                    _ifr &= 0xE7;
                    if (_acrPbLatchEnable)
                    {
                        return _pbLatch;
                    }
                    break;
                case 0x1:
                    _ifr &= 0xFC;
                    if (_acrPaLatchEnable)
                    {
                        return _paLatch;
                    }
                    break;
                case 0x4:
                    _ifr &= 0xBF;
                    break;
                case 0x8:
                    _ifr &= 0xDF;
                    break;
                case 0xA:
                    _ifr &= 0xFB;
                    break;
                case 0xF:
                    if (_acrPaLatchEnable)
                    {
                        return _paLatch;
                    }
                    break;
            }
            return ReadRegister(addr);
        }

        private int ReadRegister(int addr)
        {
            switch (addr)
            {
                case 0x0:
                    return _port.ReadPrb(_prb, _ddrb);
                case 0x1:
                    return _port.ReadPra(_pra, _ddra);
                case 0x2:
                    return _ddrb;
                case 0x3:
                    return _ddra;
                case 0x4:
                    return _t1C & 0xFF;
                case 0x5:
                    return (_t1C >> 8) & 0xFF;
                case 0x6:
                    return _t1L & 0xFF;
                case 0x7:
                    return (_t1L >> 8) & 0xFF;
                case 0x8:
                    return _t2C & 0xFF;
                case 0x9:
                    return (_t2C >> 8) & 0xFF;
                case 0xA:
                    return _sr;
                case 0xB:
                    return _acr;
                case 0xC:
                    return _pcr;
                case 0xD:
                    return _ifr;
                case 0xE:
                    return _ier | 0x80;
                case 0xF:
                    return _port.ReadPra(_pra, _ddra);
            }
            return 0xFF;
        }

        public void Write(int addr, int val)
        {
            addr &= 0xF;
            switch (addr)
            {
                case 0x0:
                    _ifr &= 0xE7;
                    if (_pcrCb2Control == PCR_CONTROL_HANDSHAKE_OUTPUT || _pcrCb2Control == PCR_CONTROL_PULSE_OUTPUT)
                    {
                        _handshakeCb2NextClock = true;
                    }
                    WriteRegister(addr, val);
                    break;
                case 0x1:
                    _ifr &= 0xFC;
                    if (_pcrCa2Control == PCR_CONTROL_HANDSHAKE_OUTPUT || _pcrCa2Control == PCR_CONTROL_PULSE_OUTPUT)
                    {
                        _handshakeCa2NextClock = true;
                    }
                    WriteRegister(addr, val);
                    break;
                case 0x4:
                case 0x6:
                    _t1L = (_t1L & 0xFF00) | (val & 0xFF);
                    break;
                case 0x5:
                    _t1L = (_t1L & 0xFF) | ((val & 0xFF) << 8);
                    _ifr &= 0xBF;
                    _t1C = _t1L;
                    _t1CLoaded = true;
                    _t1Delayed = 1;
                    break;
                case 0x7:
                    _t1L = (_t1L & 0xFF) | ((val & 0xFF) << 8);
                    _ifr &= 0xBF;
                    break;
                case 0x8:
                    _t2L = (_t2L & 0xFF00) | (val & 0xFF);
                    break;
                case 0x9:
                    _t2L = (_t2L & 0xFF) | ((val & 0xFF) << 8);
                    _ifr &= 0xDF;
                    if (_acrT2Control == ACR_T2_CONTROL_TIMED)
                    {
                        _t2C = _t2L;
                        _t2CLoaded = true;
                    }
                    _t2Delayed = 1;
                    break;
                case 0xA:
                    _ifr &= 0xFB;
                    WriteRegister(addr, val);
                    break;
                case 0xD:
                    _ifr &= ~val;
                    break;
                case 0xE:
                    if ((val & 0x80) != 0)
                        _ier |= val & 0x7F;
                    else
                        _ier &= ~val;
                    break;
                default:
                    WriteRegister(addr, val);
                    break;
            }
        }

        private void WriteRegister(int addr, int val)
        {
            addr &= 0xF;
            switch (addr)
            {
                case 0x0:
                    _prb = val & 0xFF;
                    break;
                case 0x1:
                case 0xF:
                    _pra = val & 0xFF;
                    break;
                case 0x2:
                    _ddrb = val & 0xFF;
                    break;
                case 0x3:
                    _ddra = val & 0xFF;
                    break;
                case 0x4:
                    _t1C = (_t1C & 0xFF00) | (val & 0xFF);
                    break;
                case 0x5:
                    _t1C = (_t1C & 0xFF) | ((val & 0xFF) << 8);
                    break;
                case 0x6:
                    _t1L = (_t1L & 0xFF00) | (val & 0xFF);
                    break;
                case 0x7:
                    _t1L = (_t1L & 0xFF) | ((val & 0xFF) << 8);
                    break;
                case 0x8:
                    _t2C = (_t2C & 0xFF00) | (val & 0xFF);
                    break;
                case 0x9:
                    _t2C = (_t2C & 0xFF) | ((val & 0xFF) << 8);
                    break;
                case 0xA:
                    _sr = val & 0xFF;
                    break;
                case 0xB:
                    _acr = val & 0xFF;
                    _acrPaLatchEnable = (val & 0x01) != 0;
                    _acrPbLatchEnable = (val & 0x02) != 0;
                    _acrSrControl = (val & 0x1C);
                    _acrT2Control = (val & 0x20);
                    _acrT1Control = (val & 0xC0);
                    break;
                case 0xC:
                    _pcr = val & 0xFF;
                    _pcrCa1IntControl = _pcr & 0x01;
                    _pcrCa2Control = _pcr & 0x0E;
                    _pcrCb1IntControl = (_pcr & 0x10) >> 4;
                    _pcrCb2Control = (_pcr & 0xE0) >> 4;
                    break;
                case 0xD:
                    _ifr = val & 0xFF;
                    break;
                case 0xE:
                    _ier = val & 0xFF;
                    break;
            }
        }

        [SaveState.DoNotSave]
        public int DdrA
        {
            get { return _ddra; }
        }

        [SaveState.DoNotSave]
        public int DdrB
        {
            get { return _ddrb; }
        }

        [SaveState.DoNotSave]
        public int PrA
        {
            get { return _pra; }
        }

        [SaveState.DoNotSave]
        public int PrB
        {
            get { return _prb; }
        }

        [SaveState.DoNotSave]
        public int EffectivePrA
        {
            get { return _pra | ~_ddra; }
        }

        [SaveState.DoNotSave]
        public int EffectivePrB
        {
            get { return _prb | ~_ddrb; }
        }

        [SaveState.DoNotSave]
        public int ActualPrA
        {
            get { return _acrPaLatchEnable ? _paLatch : _port.ReadPra(_pra, _ddra); }
        }

        [SaveState.DoNotSave]
        public int ActualPrB
        {
            get { return _acrPbLatchEnable ? _pbLatch : _port.ReadPrb(_prb, _ddrb); }
        }
    }
}
