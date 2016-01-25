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
            return _pra | ~_ddra;
        }

        public int Read(int addr)
        {
            addr &= 0xF;
            switch (addr)
            {
                case 0x8:
                    _tod_halt = false;
                    return ReadRegister(addr);
                case 0xB:
                    _tod_halt = true;
                    return ReadRegister(addr);
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
                    return _tod_10ths;
                case 0x9:
                    return _tod_sec;
                case 0xA:
                    return _tod_min;
                case 0xB:
                    return _tod_hr;
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
                        _alm_10ths = val & 0xF;
                    }
                    else
                    {
                        _tod_10ths = val & 0xF;
                    }
                    break;
                case 0x9:
                    if ((_crb & 0x80) != 0)
                    {
                        _alm_sec = val & 0x7F;
                    }
                    else
                    {
                        _tod_sec = val & 0x7F;
                    }
                    break;
                case 0xA:
                    if ((_crb & 0x80) != 0)
                    {
                        _alm_min = val & 0x7F;
                    }
                    else
                    {
                        _tod_min = val & 0x7F;
                    }
                    break;
                case 0xB:
                    if ((_crb & 0x80) != 0)
                    {
                        _alm_hr = val & 0x9F;
                    }
                    else
                    {
                        _tod_hr = val & 0x9F;
                    }
                    break;
                case 0xC:
                    WriteRegister(addr, val);
                    TriggerInterrupt(8);
                    break;
                case 0xD:
                    if ((val & 0x80) != 0)
                    {
                        _int_mask |= (val & 0x7F);
                    }
                    else
                    {
                        _int_mask &= ~val;
                    }
                    break;
                case 0xE:
                    _has_new_cra = true;
                    _new_cra = val;
                    _ta_cnt_phi2 = ((val & 0x20) == 0);
                    break;
                case 0xF:
                    _has_new_crb = true;
                    _new_crb = val;
                    _tb_cnt_phi2 = ((val & 0x60) == 0);
                    _tb_cnt_ta = ((val & 0x60) == 0x40);
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
                    _tod_10ths = val & 0xF;
                    break;
                case 0x9:
                    _tod_sec = val & 0x7F;
                    break;
                case 0xA:
                    _tod_min = val & 0x7F;
                    break;
                case 0xB:
                    _tod_hr = val & 0x9F;
                    break;
                case 0xC:
                    _sdr = val;
                    break;
                case 0xD:
                    _int_mask = val;
                    break;
                case 0xE:
                    _cra = val;
                    _ta_cnt_phi2 = ((val & 0x20) == 0);
                    break;
                case 0xF:
                    _crb = val;
                    _tb_cnt_phi2 = ((val & 0x60) == 0);
                    _tb_cnt_ta = ((val & 0x60) == 0x40);
                    break;
            }
        }
    }
}
