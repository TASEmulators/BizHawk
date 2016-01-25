using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// MOS technology 6526 "CIA"
	//
	// emulation notes:
	// * CS, R/W and RS# pins are not emulated. (not needed)
	// * A low RES pin is emulated via HardReset().

	public sealed class Chip6526
	{
		// ------------------------------------

	    private enum InMode
		{
			Phase2,
			Cnt,
			TimerAUnderflow,
			TimerAUnderflowCnt
		}

	    private enum OutMode
		{
			Pulse,
			Toggle
		}

	    private enum RunMode
		{
			Continuous,
			Oneshot
		}

	    private enum SpMode
		{
			Input,
			Output
		}

	    private static readonly int[] PbOnBit = { 0x40, 0x80 };
	    private static readonly int[] PbOnMask = { 0xBF, 0x7F };

		// ------------------------------------

		public Func<bool> ReadCnt;
		public Func<bool> ReadFlag;
		public Func<bool> ReadSp;

		// ------------------------------------

	    private bool _alarmSelect;
	    private bool _cntPos;
	    private bool _enableIntAlarm;
	    private bool _enableIntFlag;
	    private bool _enableIntSp;
	    private readonly bool[] _enableIntTimer;
	    private bool _intAlarm;
	    private bool _intFlag;
	    private bool _intSp;
	    private readonly bool[] _intTimer;
	    private bool _pinCnt;
	    private bool _pinCntLast;
	    private bool _pinPc;
	    private bool _pinSp;
	    private int _sr;
	    private readonly int[] _timerDelay;
	    private readonly InMode[] _timerInMode;
	    private readonly OutMode[] _timerOutMode;
	    private readonly bool[] _timerPortEnable;
	    private readonly bool[] _timerPulse;
	    private readonly RunMode[] _timerRunMode;
	    private SpMode _timerSpMode;
	    private readonly int[] _tod;
	    private readonly int[] _todAlarm;
	    private bool _todAlarmPm;
	    private int _todCounter;
	    private bool _todIn;
	    private bool _todPm;
	    private bool _oldFlag;
		// ------------------------------------

	    private readonly int _todStepsNum;
	    private readonly int _todStepsDen;

		// todStepsNum/todStepsDen is the number of clock cycles it takes the external clock source to advance one cycle
		// (50 or 60 Hz depending on AC frequency in use).
		// By default the CIA assumes 60 Hz and will thus count incorrectly when fed with 50 Hz.
		public Chip6526(int todStepsNum, int todStepsDen)
		{
			_todStepsNum = todStepsNum;
			_todStepsDen = todStepsDen;
            _enableIntTimer = new bool[2];
			_intTimer = new bool[2];
			_timerDelay = new int[2];
			_timerInMode = new InMode[2];
			_timerOutMode = new OutMode[2];
			_timerPortEnable = new bool[2];
			_timerPulse = new bool[2];
			_timerRunMode = new RunMode[2];
			_tod = new int[4];
			_todAlarm = new int[4];

			_portA = new LatchedPort();
			_portB = new LatchedPort();
			_timer = new int[2];
			_timerLatch = new int[2];
			_timerOn = new bool[2];
			_underflow = new bool[2];

			_pinSp = true;
		}

		// ------------------------------------

		public void ExecutePhase1()
		{
			// unsure if the timer actually operates in ph1
			_pinIrq = !(
				(_intTimer[0] && _enableIntTimer[0]) ||
				(_intTimer[1] && _enableIntTimer[1]) ||
				(_intAlarm && _enableIntAlarm) ||
				(_intSp && _enableIntSp) ||
				(_intFlag && _enableIntFlag)
				);
		}

		public void ExecutePhase2()
		{
			{
				var sumCnt = ReadCnt();
				_cntPos |= !_pinCntLast && sumCnt;
				_pinCntLast = sumCnt;

				_pinPc = true;
				TodRun();

				if (_timerPulse[0])
				{
					_portA.Latch &= PbOnMask[0];
				}
				if (_timerPulse[1])
				{
					_portB.Latch &= PbOnMask[1];
				}

				if (_timerDelay[0] == 0)
					TimerRun(0);
				else
					_timerDelay[0]--;

				if (_timerDelay[1] == 0)
					TimerRun(1);
				else
					_timerDelay[1]--;

				_intAlarm |= _tod[0] == _todAlarm[0] &&
				            _tod[1] == _todAlarm[1] &&
				            _tod[2] == _todAlarm[2] &&
				            _tod[3] == _todAlarm[3] &&
				            _todPm == _todAlarmPm;

				_cntPos = false;
				_underflow[0] = false;
				_underflow[1] = false;

				var newFlag = ReadFlag();
				_intFlag |= _oldFlag && !newFlag;
				_oldFlag = newFlag;
			}
		}

		public void HardReset()
		{
			HardResetInternal();
			_alarmSelect = false;
			_cntPos = false;
			_enableIntAlarm = false;
			_enableIntFlag = false;
			_enableIntSp = false;
			_enableIntTimer[0] = false;
			_enableIntTimer[1] = false;
			_intAlarm = false;
			_intFlag = false;
			_intSp = false;
			_intTimer[0] = false;
			_intTimer[1] = false;
			_sr = 0;
			_timerDelay[0] = 0;
			_timerDelay[1] = 0;
			_timerInMode[0] = InMode.Phase2;
			_timerInMode[1] = InMode.Phase2;
			_timerOn[0] = false;
			_timerOn[1] = false;
			_timerOutMode[0] = OutMode.Pulse;
			_timerOutMode[1] = OutMode.Pulse;
			_timerPortEnable[0] = false;
			_timerPortEnable[1] = false;
			_timerPulse[0] = false;
			_timerPulse[1] = false;
			_timerRunMode[0] = RunMode.Continuous;
			_timerRunMode[1] = RunMode.Continuous;
			_timerSpMode = SpMode.Input;
			_tod[0] = 0;
			_tod[1] = 0;
			_tod[2] = 0;
			_tod[3] = 0x12;
			_todAlarm[0] = 0;
			_todAlarm[1] = 0;
			_todAlarm[2] = 0;
			_todAlarm[3] = 0;
			_todCounter = 0;
			_todIn = false;
			_todPm = false;

			_pinCnt = false;
			_pinPc = true;
		}

		// ------------------------------------

		private static int BcdAdd(int i, int j, out bool overflow)
		{
			var lo = (i & 0x0F) + (j & 0x0F);
			var hi = (i & 0x70) + (j & 0x70);
			if (lo > 0x09)
			{
				hi += 0x10;
				lo += 0x06;
			}
			if (hi > 0x50)
			{
				hi += 0xA0;
			}
			overflow = hi >= 0x60;
			var result = (hi & 0x70) + (lo & 0x0F);
			return result & 0xFF;
		}

		private void TimerRun(int index)
		{
            if (!_timerOn[index])
            {
                return;
            }

            var t = _timer[index];
            var u = false;
            switch (_timerInMode[index])
            {
                case InMode.Cnt:
                    // CNT positive
                    if (_cntPos)
                    {
                        t--;
                        u = t == 0;
                        _intTimer[index] |= t == 0;
                    }
                    break;
                case InMode.Phase2:
                    // every clock
                    t--;
                    u = t == 0;
                    _intTimer[index] |= t == 0;
                    break;
                case InMode.TimerAUnderflow:
                    // every underflow[0]
                    if (_underflow[0])
                    {
                        t--;
                        u = t == 0;
                        _intTimer[index] |= t == 0;
                    }
                    break;
                case InMode.TimerAUnderflowCnt:
                    // every underflow[0] while CNT high
                    if (_underflow[0] && _pinCnt)
                    {
                        t--;
                        u = t == 0;
                        _intTimer[index] |= t == 0;
                    }
                    break;
            }

            // underflow?
            if (u)
            {
                _timerDelay[index] = 1;
                t = _timerLatch[index];
                if (_timerRunMode[index] == RunMode.Oneshot)
                    _timerOn[index] = false;

                if (_timerPortEnable[index])
                {
                    // force port B bit to output
                    _portB.Direction |= PbOnBit[index];
                    switch (_timerOutMode[index])
                    {
                        case OutMode.Pulse:
                            _timerPulse[index] = true;
                            _portB.Latch |= PbOnBit[index];
                            break;
                        case OutMode.Toggle:
                            _portB.Latch ^= PbOnBit[index];
                            break;
                    }
                }
            }

            _underflow[index] = u;
            _timer[index] = t;
        }

        private void TodRun()
		{

            if (_todCounter <= 0)
            {
                _todCounter += _todStepsNum * (_todIn ? 6 : 5);
                bool todV;
                _tod[0] = BcdAdd(_tod[0], 1, out todV);
                if (_tod[0] >= 10)
                {
                    _tod[0] = 0;
                    _tod[1] = BcdAdd(_tod[1], 1, out todV);
                    if (todV)
                    {
                        _tod[1] = 0;
                        _tod[2] = BcdAdd(_tod[2], 1, out todV);
                        if (todV)
                        {
                            _tod[2] = 0;
                            _tod[3] = BcdAdd(_tod[3], 1, out todV);
                            if (_tod[3] > 12)
                            {
                                _tod[3] = 1;
                            }
                            else if (_tod[3] == 12)
                            {
                                _todPm = !_todPm;
                            }
                        }
                    }
                }
            }
            _todCounter -= _todStepsDen;
        }

        // ------------------------------------

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
			return Read(addr, 0xFF);
		}

		public int Read(int addr, int mask)
		{
			addr &= 0xF;
            int val;

			switch (addr)
			{
				case 0x01:
					val = ReadRegister(addr);
					_pinPc = false;
					break;
				case 0x0D:
					val = ReadRegister(addr);
					_intTimer[0] = false;
					_intTimer[1] = false;
					_intAlarm = false;
					_intSp = false;
					_intFlag = false;
					_pinIrq = true;
					break;
				default:
					val = ReadRegister(addr);
					break;
			}

			val &= mask;
			return val;
		}

		public bool ReadCntBuffer()
		{
			return _pinCnt;
		}

		public bool ReadPcBuffer()
		{
			return _pinPc;
		}

		private int ReadRegister(int addr)
		{
            var val = 0x00; //unused pin value
			int timerVal;

			switch (addr)
			{
				case 0x0:
					val =_portA.ReadInput(ReadPortA()) & PortAMask;
					break;
				case 0x1:
					val = _portB.ReadInput(ReadPortB()) & PortBMask;
					break;
				case 0x2:
					val = _portA.Direction;
					break;
				case 0x3:
					val = _portB.Direction;
					break;
				case 0x4:
					timerVal = ReadTimerValue(0);
					val = timerVal & 0xFF;
					break;
				case 0x5:
					timerVal = ReadTimerValue(0);
					val = timerVal >> 8;
					break;
				case 0x6:
					timerVal = ReadTimerValue(1);
					val = timerVal & 0xFF;
					break;
				case 0x7:
					timerVal = ReadTimerValue(1);
					val = timerVal >> 8;
					break;
				case 0x8:
					val = _tod[0];
					break;
				case 0x9:
					val = _tod[1];
					break;
				case 0xA:
					val = _tod[2];
					break;
				case 0xB:
					val = _tod[3];
					break;
				case 0xC:
					val = _sr;
					break;
				case 0xD:
					val = (_intTimer[0] ? 0x01 : 0x00) |
					      (_intTimer[1] ? 0x02 : 0x00) |
					      (_intAlarm ? 0x04 : 0x00) |
					      (_intSp ? 0x08 : 0x00) |
					      (_intFlag ? 0x10 : 0x00) |
					      (!_pinIrq ? 0x80 : 0x00);
					break;
				case 0xE:
					val = (_timerOn[0] ? 0x01 : 0x00) |
					      (_timerPortEnable[0] ? 0x02 : 0x00) |
					      (_todIn ? 0x80 : 0x00);
					if (_timerOutMode[0] == OutMode.Toggle)
						val |= 0x04;
					if (_timerRunMode[0] == RunMode.Oneshot)
						val |= 0x08;
					if (_timerInMode[0] == InMode.Cnt)
						val |= 0x20;
					if (_timerSpMode == SpMode.Output)
						val |= 0x40;
					break;
				case 0xF:
					val = (_timerOn[1] ? 0x01 : 0x00) |
					      (_timerPortEnable[1] ? 0x02 : 0x00) |
					      (_alarmSelect ? 0x80 : 0x00);
					if (_timerOutMode[1] == OutMode.Toggle)
						val |= 0x04;
					if (_timerRunMode[1] == RunMode.Oneshot)
						val |= 0x08;
					switch (_timerInMode[1])
					{
						case InMode.Cnt:
							val |= 0x20;
							break;
						case InMode.TimerAUnderflow:
							val |= 0x40;
							break;
						case InMode.TimerAUnderflowCnt:
							val |= 0x60;
							break;
					}
					break;
			}

			return val;
		}

		public bool ReadSpBuffer()
		{
			return _pinSp;
		}

		private int ReadTimerValue(int index)
		{
		    if (!_timerOn[index])
		    {
                return _timer[index];
		    }

		    return _timer[index] == 0 
                ? _timerLatch[index] 
                : _timer[index];
		}

	    public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}

		public void Write(int addr, int val)
		{
			Write(addr, val, 0xFF);
		}

		public void Write(int addr, int val, int mask)
		{
			addr &= 0xF;
			val &= mask;
			val |= ReadRegister(addr) & ~mask;

			switch (addr)
			{
				case 0x1:
					WriteRegister(addr, val);
					_pinPc = false;
					break;
				case 0x5:
					WriteRegister(addr, val);
					if (!_timerOn[0])
						_timer[0] = _timerLatch[0];
					break;
				case 0x7:
					WriteRegister(addr, val);
					if (!_timerOn[1])
						_timer[1] = _timerLatch[1];
					break;
				case 0xE:
					WriteRegister(addr, val);
					if ((val & 0x10) != 0)
						_timer[0] = _timerLatch[0];
					break;
				case 0xF:
					WriteRegister(addr, val);
					if ((val & 0x10) != 0)
						_timer[1] = _timerLatch[1];
					break;
				default:
					WriteRegister(addr, val);
					break;
			}
		}

		public void WriteRegister(int addr, int val)
		{
		    switch (addr)
			{
				case 0x0:
					_portA.Latch = val;
					break;
				case 0x1:
					_portB.Latch = val;
					break;
				case 0x2:
					_portA.Direction = val;
					break;
				case 0x3:
					_portB.Direction = val;
					break;
				case 0x4:
					_timerLatch[0] &= 0xFF00;
					_timerLatch[0] |= val;
					break;
				case 0x5:
					_timerLatch[0] &= 0x00FF;
					_timerLatch[0] |= val << 8;
					break;
				case 0x6:
					_timerLatch[1] &= 0xFF00;
					_timerLatch[1] |= val;
					break;
				case 0x7:
					_timerLatch[1] &= 0x00FF;
					_timerLatch[1] |= val << 8;
					break;
				case 0x8:
					if (_alarmSelect)
						_todAlarm[0] = val & 0xF;
					else
						_tod[0] = val & 0xF;
					break;
				case 0x9:
					if (_alarmSelect)
						_todAlarm[1] = val & 0x7F;
					else
						_tod[1] = val & 0x7F;
					break;
				case 0xA:
					if (_alarmSelect)
						_todAlarm[2] = val & 0x7F;
					else
						_tod[2] = val & 0x7F;
					break;
				case 0xB:
					if (_alarmSelect)
					{
						_todAlarm[3] = val & 0x1F;
						_todAlarmPm = (val & 0x80) != 0;
					}
					else
					{
						_tod[3] = val & 0x1F;
						_todPm = (val & 0x80) != 0;
					}
					break;
				case 0xC:
					_sr = val;
					break;
				case 0xD:
					var intReg = (val & 0x80) != 0;
					if ((val & 0x01) != 0)
						_enableIntTimer[0] = intReg;
					if ((val & 0x02) != 0)
						_enableIntTimer[1] = intReg;
					if ((val & 0x04) != 0)
						_enableIntAlarm = intReg;
					if ((val & 0x08) != 0)
						_enableIntSp = intReg;
					if ((val & 0x10) != 0)
						_enableIntFlag = intReg;
					break;
				case 0xE:
					if ((val & 0x01) != 0 && !_timerOn[0])
						_timerDelay[0] = 2;
					_timerOn[0] = (val & 0x01) != 0;
					_timerPortEnable[0] = (val & 0x02) != 0;
					_timerOutMode[0] = (val & 0x04) != 0 ? OutMode.Toggle : OutMode.Pulse;
					_timerRunMode[0] = (val & 0x08) != 0 ? RunMode.Oneshot : RunMode.Continuous;
					_timerInMode[0] = (val & 0x20) != 0 ? InMode.Cnt : InMode.Phase2;
					_timerSpMode = (val & 0x40) != 0 ? SpMode.Output : SpMode.Input;
					_todIn = (val & 0x80) != 0;
					break;
				case 0xF:
					if ((val & 0x01) != 0 && !_timerOn[1])
						_timerDelay[1] = 2;
					_timerOn[1] = (val & 0x01) != 0;
					_timerPortEnable[1] = (val & 0x02) != 0;
					_timerOutMode[1] = (val & 0x04) != 0 ? OutMode.Toggle : OutMode.Pulse;
					_timerRunMode[1] = (val & 0x08) != 0 ? RunMode.Oneshot : RunMode.Continuous;
					switch (val & 0x60)
					{
						case 0x00: _timerInMode[1] = InMode.Phase2; break;
						case 0x20: _timerInMode[1] = InMode.Cnt; break;
						case 0x40: _timerInMode[1] = InMode.TimerAUnderflow; break;
						case 0x60: _timerInMode[1] = InMode.TimerAUnderflowCnt; break;
					}
					_alarmSelect = (val & 0x80) != 0;
					break;
			}
		}

		// ------------------------------------

		public int PortAMask = 0xFF;
		public int PortBMask = 0xFF;

	    private bool _pinIrq;
	    private readonly LatchedPort _portA;
	    private readonly LatchedPort _portB;
	    private readonly int[] _timer;
	    private readonly int[] _timerLatch;
	    private readonly bool[] _timerOn;
	    private readonly bool[] _underflow;

		public Func<int> ReadPortA = () => 0xFF;
		public Func<int> ReadPortB = () => 0xFF;

	    private void HardResetInternal()
		{
			_timer[0] = 0xFFFF;
			_timer[1] = 0xFFFF;
			_timerLatch[0] = _timer[0];
			_timerLatch[1] = _timer[1];
			_pinIrq = true;
		}

		public int PortAData
		{
			get
			{
				return _portA.ReadOutput();
			}
		}

		public int PortADirection
		{
			get
			{
				return _portA.Direction;
			}
		}

		public int PortALatch
		{
			get
			{
				return _portA.Latch;
			}
		}

		public int PortBData
		{
			get
			{
				return _portB.ReadOutput();
			}
		}

		public int PortBDirection
		{
			get
			{
				return _portB.Direction;
			}
		}

		public int PortBLatch
		{
			get
			{
				return _portB.Latch;
			}
		}

		public bool ReadIrq() { return _pinIrq; }
	}
}
