using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Cia
    {
        public int Peek(int addr)
        {
            return ReadRegister(addr & 0xF);
        }

        public bool ReadIrq()
        {
            return (_icr & 0x80) == 0;
        }

        public int ReadPortA()
        {
            return (_pra | ~_ddra) & 0xFF;
        }

        public int Read(int addr)
        {
            addr &= 0xF;
            switch (addr)
            {
                case 0x8:
                    _todLatch = false;
                    return _latch10Ths;
                case 0x9:
                    return _latchSec;
                case 0xA:
                    return _latchMin;
                case 0xB:
                    _todLatch = true;
                    return _latchHr;
                case 0xD:
                    var icrTemp = _icr;
                    _icr = 0;
                    return icrTemp;
            }
            return ReadRegister(addr);
        }

        private int ReadRegister(int addr)
        {
            switch (addr)
            {
                case 0x0:
                    return _port.ReadPra(_pra, _ddra, _prb, _ddrb);
                case 0x1:
                    return _port.ReadPrb(_pra, _ddra, _prb, _ddrb);
                case 0x2:
                    return _ddra;
                case 0x3:
                    return _ddrb;
                case 0x4:
                    return _ta & 0xFF;
                case 0x5:
                    return (_ta >> 8) & 0xFF;
                case 0x6:
                    return _tb & 0xFF;
                case 0x7:
                    return (_tb >> 8) & 0xFF;
                case 0x8:
                    return _tod10Ths;
                case 0x9:
                    return _todSec;
                case 0xA:
                    return _todMin;
                case 0xB:
                    return _todHr;
                case 0xC:
                    return _sdr;
                case 0xD:
                    return _icr;
                case 0xE:
                    return _cra;
                case 0xF:
                    return _crb;
            }
            return 0;
        }

        public void Poke(int addr, int val)
        {
            WriteRegister(addr, val);
        }

        public void Write(int addr, int val)
        {
            addr &= 0xF;
            switch (addr)
            {
                case 0x4:
                    _latcha = (_latcha & 0xFF00) | val;
                    break;
                case 0x5:
                    _latcha = (_latcha & 0xFF) | (val << 8);
                    if ((_cra & 0x01) == 0)
                    {
                        _ta = _latcha;
                    }
                    break;
                case 0x6:
                    _latchb = (_latchb & 0xFF00) | val;
                    break;
                case 0x7:
                    _latchb = (_latchb & 0xFF) | (val << 8);
                    if ((_crb & 0x01) == 0)
                    {
                        _tb = _latchb;
                    }
                    break;
                case 0x8:
                    if ((_crb & 0x80) != 0)
                    {
                        _alm10Ths = val & 0xF;
                    }
                    else
                    {
                        _tod10Ths = val & 0xF;
                    }
                    break;
                case 0x9:
                    if ((_crb & 0x80) != 0)
                    {
                        _almSec = val & 0x7F;
                    }
                    else
                    {
                        _todSec = val & 0x7F;
                    }
                    break;
                case 0xA:
                    if ((_crb & 0x80) != 0)
                    {
                        _almMin = val & 0x7F;
                    }
                    else
                    {
                        _todMin = val & 0x7F;
                    }
                    break;
                case 0xB:
                    if ((_crb & 0x80) != 0)
                    {
                        _almHr = val & 0x9F;
                    }
                    else
                    {
                        _todHr = val & 0x9F;
                    }
                    break;
                case 0xC:
                    WriteRegister(addr, val);
                    TriggerInterrupt(8);
                    break;
                case 0xD:
                    if ((val & 0x80) != 0)
                    {
                        _intMask |= (val & 0x7F);
                    }
                    else
                    {
                        _intMask &= ~val;
                    }
                    if ((_icr & _intMask & 0x1F) != 0)
                    {
                        _icr |= 0x80;
                    }
                    break;
                case 0xE:
                    var oldCra = _cra;
                    WriteRegister(addr, val);
                    
                    // Toggle output begins high when timer starts.
                    if ((_cra & 0x05) == 0x05 && (oldCra & 0x01) == 0)
                        _prb |= 0x40;
                    break;
                case 0xF:
                    var oldCrb = _crb;
                    WriteRegister(addr, val);

                    // Toggle output begins high when timer starts.
                    if ((_crb & 0x05) == 0x05 && (oldCrb & 0x01) == 0)
                        _prb |= 0x80;
                    break;
                default:
                    WriteRegister(addr, val);
                    break;
            }
        }

        private void WriteRegister(int addr, int val)
        {
            switch (addr)
            {
                case 0x0:
                    _pra = val;
                    break;
                case 0x1:
                    _prb = val;
                    break;
                case 0x2:
                    _ddra = val;
                    break;
                case 0x3:
                    _ddrb = val;
                    break;
                case 0x4:
                    _latcha = (_latcha & 0xFF00) | val;
                    _ta = _latcha;
                    break;
                case 0x5:
                    _latcha = (_latcha & 0xFF) | (val << 8);
                    _ta = _latcha;
                    break;
                case 0x6:
                    _latchb = (_latchb & 0xFF00) | val;
                    _tb = _latchb;
                    break;
                case 0x7:
                    _latchb = (_latchb & 0xFF) | (val << 8);
                    _tb = _latchb;
                    break;
                case 0x8:
                    _tod10Ths = val & 0xF;
                    break;
                case 0x9:
                    _todSec = val & 0x7F;
                    break;
                case 0xA:
                    _todMin = val & 0x7F;
                    break;
                case 0xB:
                    _todHr = val & 0x9F;
                    break;
                case 0xC:
                    _sdr = val;
                    break;
                case 0xD:
                    _intMask = val;
                    break;
                case 0xE:
                    _hasNewCra = true;
                    _newCra = val;
                    _taCntPhi2 = ((val & 0x20) == 0);
                    _taCntCnt = ((val & 0x20) == 0x20);
                    break;
                case 0xF:
                    _hasNewCrb = true;
                    _newCrb = val;
                    _tbCntPhi2 = ((val & 0x60) == 0);
                    _tbCntTa = ((val & 0x40) == 0x40);
                    _tbCntCnt = ((val & 0x20) == 0x20);
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
    }
}
