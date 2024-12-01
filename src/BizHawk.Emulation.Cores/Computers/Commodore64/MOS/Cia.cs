using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Cia
	{
		/*
			Commodore CIA 6526 core.

			Many thanks to:
			- 6502.org for hosting the 6526 datasheet
				http://archive.6502.org/datasheets/mos_6526_cia.pdf
			- Christian Bauer for information on the delayed interrupt mechanism on the 6526
				http://frodo.cebix.net/
		*/


		// operation of the interrupt at the serial data port occurs in 2 instances:
		// 1. Being in output mode and having a complete transfer as defined by clocking of timer A
		// 2. Being in input mode and receiving 8 clocks from the /CNT pin
		// This is TODO

		private enum TimerState
		{
			Stop = 0,
			WaitThenCount = 1,
			LoadThenStop = 2,
			LoadThenCount = 3,
			LoadThenWaitThenCount = 4,
			Count = 5,
			CountThenStop = 6
		}

		public Func<bool> ReadFlag = () => true;
		public Func<bool> ReadCnt = () => true;
		public Func<bool> ReadSp = () => true; 
		public bool DelayedInterrupts = true;

		private int _pra;
		private int _prb;
		private int _ddra;
		private int _ddrb;
		private int _ta;
		private int _tb;
		private int _latcha;
		private int _latchb;
		private int _tod10Ths;
		private int _todSec;
		private int _todMin;
		private int _todHr;
		private int _latch10Ths;
		private int _latchSec;
		private int _latchMin;
		private int _latchHr;
		private int _alm10Ths;
		private int _almSec;
		private int _almMin;
		private int _almHr;
		private int _sdr;
		private int _icr;
		private int _cra;
		private int _crb;
		private int _intMask;
		private bool _todLatch;
		private bool _taCntPhi2;
		private bool _taCntCnt;
		private bool _tbCntPhi2;
		private bool _tbCntTa;
		private bool _tbCntCnt;
		private bool _taIrqNextCycle;
		private bool _taPrb6NegativeNextCycle;
		private bool _tbIrqNextCycle;
		private bool _tbPrb7NegativeNextCycle;
		private bool _hasNewCra;
		private bool _hasNewCrb;
		private TimerState _taState;
		private TimerState _tbState;
		private int _newCra;
		private int _newCrb;
		private bool _flagLatch;
		private bool _flagInput;
		private bool _taUnderflow;
		private bool _lastCnt;
		private bool _thisCnt;
		private bool _tbCntTaCnt;

		private readonly IPort _port;
		private int _todlo;
		private int _todhi;
		private readonly int _todNum;
		private readonly int _todDen;
		private int _todCounter;

		private Cia(int todNum, int todDen)
		{
			_todNum = todNum;
			_todDen = todDen;
		}

		public Cia(int todNum, int todDen, Func<bool[]> keyboard, Func<bool[]> joysticks) : this(todNum, todDen)
		{
			_port = new JoystickKeyboardPort(joysticks, keyboard);
		}

		public Cia(int todNum, int todDen, Func<int> readIec, Func<int> readUserPort) : this(todNum, todDen)
		{
			_port = new IecPort(readIec, readUserPort);
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
			_tod10Ths = 0;
			_todSec = 0;
			_todMin = 0;
			_todHr = 0;
			_alm10Ths = 0;
			_almSec = 0;
			_almMin = 0;
			_almHr = 0;
			_sdr = 0;
			_icr = 0;
			_cra = 0;
			_crb = 0;
			_intMask = 0;
			_todLatch = false;
			_taCntPhi2 = false;
			_taCntCnt = false;
			_tbCntPhi2 = false;
			_tbCntTa = false;
			_tbCntCnt = false;
			_taIrqNextCycle = false;
			_tbIrqNextCycle = false;
			_taState = TimerState.Stop;
			_tbState = TimerState.Stop;
			_lastCnt = true;
		}

		public void ExecutePhase()
		{
			_thisCnt = ReadCnt();
			_taUnderflow = false;

			if (_taIrqNextCycle)
			{
				_taIrqNextCycle = false;
				TriggerInterrupt(1);
			}
			
			if (_tbIrqNextCycle)
			{
				_tbIrqNextCycle = false;
				TriggerInterrupt(2);
			}

			if (_taPrb6NegativeNextCycle)
			{
				_prb &= 0xBF;
				_taPrb6NegativeNextCycle = false;
			}

			if (_tbPrb7NegativeNextCycle)
			{
				_prb &= 0x7F;
				_tbPrb7NegativeNextCycle = false;
			}


			switch (_taState)
			{
				case TimerState.WaitThenCount:
					_taState = TimerState.Count;
					Ta_Idle();
					break;
				case TimerState.Stop:
					Ta_Idle();
					break;
				case TimerState.LoadThenStop:
					_taState = TimerState.Stop;
					_ta = _latcha;
					Ta_Idle();
					break;
				case TimerState.LoadThenCount:
					_taState = TimerState.Count;
					_ta = _latcha;
					Ta_Idle();
					break;
				case TimerState.LoadThenWaitThenCount:
					_taState = TimerState.WaitThenCount;
					if (_ta == 1)
					{
						Ta_Interrupt();
						_taUnderflow = true;
					}
					else
					{
						_ta = _latcha;
					}
					Ta_Idle();
					break;
				case TimerState.Count:
					Ta_Count();
					break;
				case TimerState.CountThenStop:
					_taState = TimerState.Stop;
					Ta_Count();
					break;
			}

			switch (_tbState)
			{
				case TimerState.WaitThenCount:
					_tbState = TimerState.Count;
					Tb_Idle();
					break;
				case TimerState.Stop:
					Tb_Idle();
					break;
				case TimerState.LoadThenStop:
					_tbState = TimerState.Stop;
					_tb = _latchb;
					Tb_Idle();
					break;
				case TimerState.LoadThenCount:
					_tbState = TimerState.Count;
					_tb = _latchb;
					Tb_Idle();
					break;
				case TimerState.LoadThenWaitThenCount:
					_tbState = TimerState.WaitThenCount;
					if (_tb == 1)
					{
						Tb_Interrupt();
					}
					else
					{
						_tb = _latchb;
					}
					Tb_Idle();
					break;
				case TimerState.Count:
					Tb_Count();
					break;
				case TimerState.CountThenStop:
					_tbState = TimerState.Stop;
					Tb_Count();
					break;
			}

			CountTod();

			if (!_todLatch)
			{
				_latch10Ths = _tod10Ths;
				_latchSec = _todSec;
				_latchMin = _todMin;
				_latchHr = _todHr;
			}

			_flagInput = ReadFlag();
			if (!_flagInput && _flagLatch)
			{
				TriggerInterrupt(16);
			}
			_flagLatch = _flagInput;

			if ((_cra & 0x02) != 0)
				_ddra |= 0x40;
			if ((_crb & 0x02) != 0)
				_ddrb |= 0x80;

			_lastCnt = _thisCnt;
		}

		private void Ta_Count()
		{
			if (_taCntPhi2 || (_taCntCnt && !_lastCnt && _thisCnt))
			{
				if (_ta <= 0 || --_ta == 0)
				{
					if (_taState != TimerState.Stop)
					{
						Ta_Interrupt();
					}
					_taUnderflow = true;
				}
			}
			Ta_Idle();
		}

		private void Ta_Interrupt()
		{
			_ta = _latcha;

			if (DelayedInterrupts)
				_taIrqNextCycle = true;
			else
				TriggerInterrupt(1);

			_icr |= 1;

			if ((_cra & 0x08) != 0)
			{
				_cra &= 0xFE;
				_newCra &= 0xFE;
				_taState = TimerState.LoadThenStop;
			}
			else
			{
				_taState = TimerState.LoadThenCount;
			}

			if ((_cra & 0x02) != 0)
			{
				if ((_cra & 0x04) != 0)
				{
					_taPrb6NegativeNextCycle = true;
					_prb |= 0x40;
				}
				else
				{
					_prb ^= 0x40;
				}
				_ddrb |= 0x40;
			}
		}

		private void Ta_Idle()
		{
			if (_hasNewCra)
			{
				switch (_taState)
				{
					case TimerState.Stop:
					case TimerState.LoadThenStop:
						if ((_newCra & 0x01) != 0)
						{
							_taState = (_newCra & 0x10) != 0
								? TimerState.LoadThenWaitThenCount
								: TimerState.WaitThenCount;
						}
						else
						{
							if ((_newCra & 0x10) != 0)
							{
								_taState = TimerState.LoadThenStop;
							}
						}
						break;
					case TimerState.Count:
						if ((_newCra & 0x01) != 0)
						{
							if ((_newCra & 0x10) != 0)
							{
								_taState = TimerState.LoadThenWaitThenCount;
							}
						}
						else
						{
							_taState = (_newCra & 0x10) != 0
								? TimerState.LoadThenStop
								: TimerState.CountThenStop;
						}
						break;
					case TimerState.LoadThenCount:
					case TimerState.WaitThenCount:
						if ((_newCra & 0x01) != 0)
						{
							if ((_newCra & 0x08) != 0)
							{
								_newCra &= 0xFE;
								_taState = TimerState.Stop;
							}
							else if ((_newCra & 0x10) != 0)
							{
								_taState = TimerState.LoadThenWaitThenCount;
							}
						}
						else
						{
							_taState = TimerState.Stop;
						}
						break;
				}
				_cra = _newCra & 0xEF;
				_hasNewCra = false;
			}
		}

		private void Tb_Count()
		{
			if (_tbCntPhi2 || (_tbCntTa && _taUnderflow) || (_tbCntTaCnt && _taUnderflow && _thisCnt) || (_tbCntCnt && !_lastCnt && _thisCnt))
			{
				if (_tb <= 0 || --_tb == 0)
				{
					if (_tbState != TimerState.Stop)
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
			if (DelayedInterrupts)
				_tbIrqNextCycle = true;
			else
				TriggerInterrupt(2);

			_icr |= 2;

			if ((_crb & 0x08) != 0)
			{
				_crb &= 0xFE;
				_newCrb &= 0xFE;
				_tbState = TimerState.LoadThenStop;
			}
			else
			{
				_tbState = TimerState.LoadThenCount;
			}

			if ((_crb & 0x02) != 0)
			{
				if ((_crb & 0x04) != 0)
				{
					_tbPrb7NegativeNextCycle = true;
					_prb |= 0x80;
				}
				else
				{
					_prb ^= 0x80;
				}
			}
		}

		private void Tb_Idle()
		{
			if (_hasNewCrb)
			{
				switch (_tbState)
				{
					case TimerState.Stop:
					case TimerState.LoadThenStop:
						if ((_newCrb & 0x01) != 0)
						{
							_tbState = (_newCrb & 0x10) != 0
								? TimerState.LoadThenWaitThenCount
								: TimerState.WaitThenCount;
						}
						else
						{
							if ((_newCrb & 0x10) != 0)
							{
								_tbState = TimerState.LoadThenStop;
							}
						}
						break;
					case TimerState.Count:
						if ((_newCrb & 0x01) != 0)
						{
							if ((_newCrb & 0x10) != 0)
							{
								_tbState = TimerState.LoadThenWaitThenCount;
							}
						}
						else
						{
							_tbState = (_newCrb & 0x10) != 0
								? TimerState.LoadThenStop
								: TimerState.CountThenStop;
						}
						break;
					case TimerState.LoadThenCount:
					case TimerState.WaitThenCount:
						if ((_newCrb & 0x01) != 0)
						{
							if ((_newCrb & 0x08) != 0)
							{
								_newCrb &= 0xFE;
								_tbState = TimerState.Stop;
							}
							else if ((_newCrb & 0x10) != 0)
							{
								_tbState = TimerState.LoadThenWaitThenCount;
							}
						}
						else
						{
							_tbState = TimerState.Stop;
						}
						break;
				}
				_crb = _newCrb & 0xEF;
				_hasNewCrb = false;
			}
		}

		private void TriggerInterrupt(int bit)
		{
			_icr |= bit;
			if ((_intMask & bit) == 0) return;
			_icr |= 0x80;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(DelayedInterrupts), ref DelayedInterrupts);
			ser.Sync(nameof(_pra), ref _pra);
			ser.Sync(nameof(_prb), ref _prb);
			ser.Sync(nameof(_ddra), ref _ddra);
			ser.Sync(nameof(_ddrb), ref _ddrb);
			ser.Sync(nameof(_ta), ref _ta);
			ser.Sync(nameof(_tb), ref _tb);
			ser.Sync(nameof(_latcha), ref _latcha);
			ser.Sync(nameof(_latchb), ref _latchb);
			ser.Sync(nameof(_tod10Ths), ref _tod10Ths);
			ser.Sync(nameof(_todSec), ref _todSec);
			ser.Sync(nameof(_todMin), ref _todMin);
			ser.Sync(nameof(_todHr), ref _todHr);
			ser.Sync(nameof(_latch10Ths), ref _latch10Ths);
			ser.Sync(nameof(_latchSec), ref _latchSec);
			ser.Sync(nameof(_latchMin), ref _latchMin);
			ser.Sync(nameof(_latchHr), ref _latchHr);
			ser.Sync(nameof(_alm10Ths), ref _alm10Ths);
			ser.Sync(nameof(_almSec), ref _almSec);
			ser.Sync(nameof(_almMin), ref _almMin);
			ser.Sync(nameof(_almHr), ref _almHr);
			ser.Sync(nameof(_sdr), ref _sdr);
			ser.Sync(nameof(_icr), ref _icr);
			ser.Sync(nameof(_cra), ref _cra);
			ser.Sync(nameof(_crb), ref _crb);
			ser.Sync(nameof(_intMask), ref _intMask);
			ser.Sync(nameof(_todLatch), ref _todLatch);
			ser.Sync(nameof(_taCntPhi2), ref _taCntPhi2);
			ser.Sync(nameof(_taCntCnt), ref _taCntCnt);
			ser.Sync(nameof(_tbCntPhi2), ref _tbCntPhi2);
			ser.Sync(nameof(_tbCntTa), ref _tbCntTa);
			ser.Sync(nameof(_tbCntCnt), ref _tbCntCnt);
			ser.Sync(nameof(_taIrqNextCycle), ref _taIrqNextCycle);
			ser.Sync(nameof(_taPrb6NegativeNextCycle), ref _taPrb6NegativeNextCycle);
			ser.Sync(nameof(_tbIrqNextCycle), ref _tbIrqNextCycle);
			ser.Sync(nameof(_tbPrb7NegativeNextCycle), ref _tbPrb7NegativeNextCycle);
			ser.Sync(nameof(_hasNewCra), ref _hasNewCra);
			ser.Sync(nameof(_hasNewCrb), ref _hasNewCrb);
			ser.SyncEnum(nameof(_taState), ref _taState);
			ser.SyncEnum(nameof(_tbState), ref _tbState);
			ser.Sync(nameof(_newCra), ref _newCra);
			ser.Sync(nameof(_newCrb), ref _newCrb);
			ser.Sync(nameof(_flagLatch), ref _flagLatch);
			ser.Sync(nameof(_lastCnt), ref _lastCnt);
			ser.Sync(nameof(_thisCnt), ref _thisCnt);
			ser.Sync(nameof(_tbCntTaCnt), ref _tbCntTaCnt);
			ser.Sync(nameof(_todCounter), ref _todCounter);
		}
	}
}
