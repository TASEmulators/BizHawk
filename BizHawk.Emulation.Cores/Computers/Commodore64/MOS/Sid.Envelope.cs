using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Sid
	{
	    private sealed class Envelope
		{
		    [SaveState.DoNotSave] private const int StateAttack = 0;
		    [SaveState.DoNotSave] private const int StateDecay = 1;
		    [SaveState.DoNotSave] private const int StateRelease = 2;

		    private int _attack;
		    private int _decay;
		    private bool _delay;
		    private int _envCounter;
		    private int _expCounter;
		    private int _expPeriod;
		    private bool _freeze;
		    private int _lfsr;
		    private bool _gate;
		    private int _rate;
		    private int _release;
		    private int _state;
		    private int _sustain;

		    private static readonly int[] AdsrTable = {
				0x7F00, 0x0006, 0x003C, 0x0330,
				0x20C0, 0x6755, 0x3800, 0x500E,
				0x1212, 0x0222, 0x1848, 0x59B8,
				0x3840, 0x77E2, 0x7625, 0x0A93
			};

		    private static readonly int[] ExpCounterTable = {
				0xFF, 0x5D, 0x36, 0x1A, 0x0E, 0x06, 0x00
			};

		    private static readonly int[] ExpPeriodTable = {
				0x01, 0x02, 0x04, 0x08, 0x10, 0x1E, 0x01
			};

		    private static readonly int[] SustainTable = {
				0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
				0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
			};

			public Envelope()
			{
				HardReset();
			}

			public void ExecutePhase2()
			{
				if (!_delay)
				{
					_envCounter--;
					_delay = true;
					UpdateExpCounter();
				}

				if (_lfsr != _rate)
				{
					var feedback = ((_lfsr >> 14) ^ (_lfsr >> 13)) & 0x1;
					_lfsr = ((_lfsr << 1) & 0x7FFF) | feedback;
					return;
				}
				_lfsr = 0x7FFF;

				if (_state != StateAttack && ++_expCounter != _expPeriod)
				{
				    return;
				}

				_expCounter = 0;
				if (_freeze)
				    return;

				switch (_state)
				{
				    case StateAttack:
				        _envCounter++;
				        if (_envCounter == 0xFF)
				        {
				            _state = StateDecay;
				            _rate = AdsrTable[_decay];
				        }
				        break;
				    case StateDecay:
				        if (_envCounter == SustainTable[_sustain])
				        {
				            return;
				        }
				        if (_expPeriod != 1)
				        {
				            _delay = false;
				            return;
				        }
				        _envCounter--;
				        break;
				    case StateRelease:
				        if (_expPeriod != 1)
				        {
				            _delay = false;
				            return;
				        }
				        _envCounter--;
				        break;
				}
				_envCounter &= 0xFF;
				UpdateExpCounter();
			}

			public void HardReset()
			{
				_attack = 0;
				_decay = 0;
				_delay = true;
				_envCounter = 0;
				_expCounter = 0;
				_expPeriod = ExpPeriodTable[0];
				_freeze = false;
				_gate = false;
				_lfsr = 0x7FFF;
				_rate = AdsrTable[_release];
				_release = 0;
				_state = StateRelease;
				_sustain = 0;
			}

			private void UpdateExpCounter()
			{

				{
					for (var i = 0; i < 7; i++)
					{
						if (_envCounter == ExpCounterTable[i])
							_expPeriod = ExpPeriodTable[i];
					}
					if (_envCounter == 0)
						_freeze = true;
				}
			}

			// ------------------------------------

			public int Attack
			{
				get
				{
					return _attack;
				}
				set
				{
					_attack = value & 0xF;
					if (_state == StateAttack)
						_rate = AdsrTable[_attack];
				}
			}

			public int Decay
			{
				get
				{
					return _decay;
				}
				set
				{
					_decay = value & 0xF;
					if (_state == StateDecay)
						_rate = AdsrTable[_decay];
				}
			}

			public bool Gate
			{
				get
				{
					return _gate;
				}
				set
				{
					var nextGate = value;
					if (nextGate && !_gate)
					{
						_state = StateAttack;
						_rate = AdsrTable[_attack];
						_delay = true;
						_freeze = false;
					}
					else if (!nextGate && _gate)
					{
						_state = StateRelease;
						_rate = AdsrTable[_release];
					}
					_gate = nextGate;
				}
			}

			public int Level
			{
				get
				{
					return _envCounter;
				}
			}

			public int Release
			{
				get
				{
					return _release;
				}
				set
				{
					_release = value & 0xF;
					if (_state == StateRelease)
						_rate = AdsrTable[_release];
				}
			}

			public int Sustain
			{
				get
				{
					return _sustain;
				}
				set
				{
					_sustain = value & 0xF;
				}
			}

			// ------------------------------------

			public void SyncState(Serializer ser)
			{
				SaveState.SyncObject(ser, this);
			}

			// ------------------------------------
		}
	}
}
