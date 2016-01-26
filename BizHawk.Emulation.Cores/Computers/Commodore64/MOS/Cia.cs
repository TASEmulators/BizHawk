using BizHawk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
    public sealed partial class Cia
    {
        private const int T_STOP = 0;
        private const int T_WAIT_THEN_COUNT = 1;
        private const int T_LOAD_THEN_STOP = 2;
        private const int T_LOAD_THEN_COUNT = 3;
        private const int T_LOAD_THEN_WAIT_THEN_COUNT = 4;
        private const int T_COUNT = 5;
        private const int T_COUNT_THEN_STOP = 6;

        private int _pra;
        private int _prb;
        private int _ddra;
        private int _ddrb;
        private int _ta;
        private int _tb;
        private int _latcha;
        private int _latchb;
        private int _tod_10ths;
        private int _tod_sec;
        private int _tod_min;
        private int _tod_hr;
        private int _alm_10ths;
        private int _alm_sec;
        private int _alm_min;
        private int _alm_hr;
        private int _sdr;
        private int _icr;
        private int _cra;
        private int _crb;
        private int _int_mask;
        private int _tod_divider;
        private bool _tod_halt;
        private bool _ta_cnt_phi2;
        private bool _tb_cnt_phi2;
        private bool _tb_cnt_ta;
        private bool _ta_irq_next_cycle;
        private bool _tb_irq_next_cycle;
        private bool _has_new_cra;
        private bool _has_new_crb;
        private int _ta_state;
        private int _tb_state;
        private int _new_cra;
        private int _new_crb;
        private bool _ta_underflow;

        private Port _port;
        private int _todlo;
        private int _todhi;
        private int _tod_num;
        private int _tod_den;
        private int _tod_counter;

        private Cia(int todNum, int todDen)
        {
            _tod_num = todNum;
            _tod_den = todDen;
        }

        public Cia(int todNum, int todDen, int[] keyboard, int[] joysticks) : this(todNum, todDen)
        {
            _port = new JoystickKeyboardPort(joysticks, keyboard);
        }

        public Cia(int todNum, int todDen, Func<int> readIec) : this(todNum, todDen)
        {
            _port = new IecPort(readIec);
        }

        public void HardReset()
        {
            _pra = 0;
            _prb = 0;
            _ddra = 0;
            _ddrb = 0;
            _ta = 0xFFFF;
            _tb = 0xFFFF;
            _latcha = 1;
            _latchb = 1;
            _tod_10ths = 0;
            _tod_sec = 0;
            _tod_min = 0;
            _tod_hr = 0;
            _alm_10ths = 0;
            _alm_sec = 0;
            _alm_min = 0;
            _alm_hr = 0;
            _sdr = 0;
            _icr = 0;
            _cra = 0;
            _crb = 0;
            _int_mask = 0;
            _tod_halt = false;
            _tod_divider = 0;
            _ta_cnt_phi2 = false;
            _tb_cnt_phi2 = false;
            _tb_cnt_ta = false;
            _ta_irq_next_cycle = false;
            _tb_irq_next_cycle = false;
            _ta_state = T_STOP;
            _tb_state = T_STOP;
        }

        private void CheckIrqs()
        {
            if (_ta_irq_next_cycle)
            {
                _ta_irq_next_cycle = false;
                TriggerInterrupt(1);
            }
            if (_tb_irq_next_cycle)
            {
                _tb_irq_next_cycle = false;
                TriggerInterrupt(2);
            }
        }

        public void ExecutePhase1()
        {
            
        }

        public void ExecutePhase2()
        {
            CheckIrqs();
            _ta_underflow = false;

            switch (_ta_state)
            {
                case T_WAIT_THEN_COUNT:
                    _ta_state = T_COUNT;
                    Ta_Idle();
                    break;
                case T_STOP:
                    Ta_Idle();
                    break;
                case T_LOAD_THEN_STOP:
                    _ta_state = T_STOP;
                    _ta = _latcha;
                    Ta_Idle();
                    break;
                case T_LOAD_THEN_COUNT:
                    _ta_state = T_COUNT;
                    _ta = _latcha;
                    Ta_Idle();
                    break;
                case T_LOAD_THEN_WAIT_THEN_COUNT:
                    _ta_state = T_WAIT_THEN_COUNT;
                    if (_ta == 1)
                    {
                        Ta_Interrupt();
                    }
                    else
                    {
                        _ta = _latcha;
                        Ta_Idle();
                    }
                    break;
                case T_COUNT:
                    Ta_Count();
                    break;
                case T_COUNT_THEN_STOP:
                    _ta_state = T_STOP;
                    Ta_Count();
                    break;
            }

            switch (_tb_state)
            {
                case T_WAIT_THEN_COUNT:
                    _tb_state = T_COUNT;
                    Tb_Idle();
                    break;
                case T_STOP:
                    Tb_Idle();
                    break;
                case T_LOAD_THEN_STOP:
                    _tb_state = T_STOP;
                    _tb = _latchb;
                    Tb_Idle();
                    break;
                case T_LOAD_THEN_COUNT:
                    _tb_state = T_COUNT;
                    _tb = _latchb;
                    Tb_Idle();
                    break;
                case T_LOAD_THEN_WAIT_THEN_COUNT:
                    _tb_state = T_WAIT_THEN_COUNT;
                    if (_tb == 1)
                    {
                        Tb_Interrupt();
                    }
                    else
                    {
                        _tb = _latchb;
                        Tb_Idle();
                    }
                    break;
                case T_COUNT:
                    Tb_Count();
                    break;
                case T_COUNT_THEN_STOP:
                    _tb_state = T_STOP;
                    Tb_Count();
                    break;
            }

            CountTod();
        }

        private void Ta_Count()
        {
            if (_ta_cnt_phi2)
            {
                if (_ta == 0 || --_ta == 0)
                {
                    if (_ta_state != T_STOP)
                    {
                        Ta_Interrupt();
                    }
                    _ta_underflow = true;
                }
            }
            Ta_Idle();
        }

        private void Ta_Interrupt()
        {
            _ta = _latcha;
            _ta_irq_next_cycle = true;
            _icr |= 1;

            if ((_cra & 0x08) != 0)
            {
                _cra &= 0xFE;
                _new_cra &= 0xFE;
                _ta_state = T_LOAD_THEN_STOP;
            }
            else
            {
                _ta_state = T_LOAD_THEN_COUNT;
            }
        }

        private void Ta_Idle()
        {
            if (_has_new_cra)
            {
                switch (_ta_state)
                {
                    case T_STOP:
                    case T_LOAD_THEN_STOP:
                        if ((_new_cra & 0x01) != 0)
                        {
                            if ((_new_cra & 0x10) != 0)
                            {
                                _ta_state = T_LOAD_THEN_WAIT_THEN_COUNT;
                            }
                            else
                            {
                                _ta_state = T_WAIT_THEN_COUNT;
                            }
                        }
                        else
                        {
                            if ((_new_cra & 0x10) != 0)
                            {
                                _ta_state = T_LOAD_THEN_STOP;
                            }
                        }
                        break;
                    case T_LOAD_THEN_COUNT:
                    case T_WAIT_THEN_COUNT:
                        if ((_new_cra & 0x01) != 0)
                        {
                            if ((_new_cra & 0x08) != 0)
                            {
                                _new_cra &= 0xFE;
                                _ta_state = T_STOP;
                            }
                            else if ((_new_cra & 0x10) != 0)
                            {
                                _ta_state = T_LOAD_THEN_WAIT_THEN_COUNT;
                            }
                        }
                        else
                        {
                            _ta_state = T_STOP;
                        }
                        break;
                }
                _cra = _new_cra & 0xEF;
                _has_new_cra = false;
            }
        }

        private void Tb_Count()
        {
            if (_tb_cnt_phi2 || (_tb_cnt_ta && _ta_underflow))
            {
                if (_tb == 0 || --_tb == 0)
                {
                    if (_tb_state != T_STOP)
                    {
                        Tb_Interrupt();
                    }
                }
            }
            Tb_Idle();
        }

        private void Tb_Interrupt()
        {
            _tb = _latchb;
            _tb_irq_next_cycle = true;
            _icr |= 2;

            if ((_crb & 0x08) != 0)
            {
                _crb &= 0xFE;
                _new_crb &= 0xFE;
                _tb_state = T_LOAD_THEN_STOP;
            }
            else
            {
                _tb_state = T_LOAD_THEN_COUNT;
            }

        }

        private void Tb_Idle()
        {
            if (_has_new_crb)
            {
                switch (_tb_state)
                {
                    case T_STOP:
                    case T_LOAD_THEN_STOP:
                        if ((_new_crb & 0x01) != 0)
                        {
                            if ((_new_crb & 0x10) != 0)
                            {
                                _tb_state = T_LOAD_THEN_WAIT_THEN_COUNT;
                            }
                            else
                            {
                                _tb_state = T_WAIT_THEN_COUNT;
                            }
                        }
                        else
                        {
                            if ((_new_crb & 0x10) != 0)
                            {
                                _tb_state = T_LOAD_THEN_STOP;
                            }
                        }
                        break;
                    case T_LOAD_THEN_COUNT:
                    case T_WAIT_THEN_COUNT:
                        if ((_new_crb & 0x01) != 0)
                        {
                            if ((_new_crb & 0x08) != 0)
                            {
                                _new_crb &= 0xFE;
                                _ta_state = T_STOP;
                            }
                            else if ((_new_crb & 0x10) != 0)
                            {
                                _tb_state = T_LOAD_THEN_WAIT_THEN_COUNT;
                            }
                        }
                        else
                        {
                            _ta_state = T_STOP;
                        }
                        break;
                }
                _crb = _new_crb & 0xEF;
                _has_new_crb = false;
            }
        }

        private void CountTod()
        {
            if (_tod_counter > 0)
            {
                _tod_counter -= _tod_den;
                return;
            }

            _tod_counter += _tod_num * ((_cra & 0x80) != 0 ? 6 : 5);
             _tod_10ths++;
            if (_tod_10ths > 9)
            {
                _tod_10ths = 0;
                _todlo = (_tod_sec & 0x0F) + 1;
                _todhi = (_tod_sec >> 4);
                if (_todlo > 9)
                {
                    _todlo = 0;
                    _todhi++;
                }
                if (_todhi > 5)
                {
                    _tod_sec = 0;
                    _todlo = (_tod_min & 0x0F) + 1;
                    _todhi = (_tod_min >> 4);
                    if (_todlo > 9)
                    {
                        _todlo = 0;
                        _todhi++;
                    }
                    if (_todhi > 5)
                    {
                        _tod_min = 0;
                        _todlo = (_tod_hr & 0x0F) + 1;
                        _todhi = (_tod_hr >> 4);
                        _tod_hr &= 0x80;
                        if (_todlo > 9)
                        {
                            _todlo = 0;
                            _todhi++;
                        }
                        _tod_hr |= (_todhi << 4) | _todlo;
                        if ((_tod_hr & 0x1F) > 0x11)
                        {
                            _tod_hr &= 0x80 ^ 0x80;
                        }
                    }
                    else
                    {
                        _tod_min = (_todhi << 4) | _todlo;
                    }
                }
                else
                {
                    _tod_sec = (_todhi << 4) | _todlo;
                }
            }

            if (_tod_10ths == _alm_10ths && _tod_sec == _alm_sec && _tod_min == _alm_min && _tod_hr == _alm_hr)
            {
                TriggerInterrupt(4);
            }
        }

        private void TriggerInterrupt(int num)
        {
            _icr |= num;
            if ((_int_mask & num) == 0) return;
            _icr |= 0x80;
        }

        public void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }
    }
}
