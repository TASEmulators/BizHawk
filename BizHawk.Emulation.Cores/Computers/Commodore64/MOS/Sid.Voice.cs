using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Sid
	{
	    private sealed class Voice
		{
		    private int _accBits;
		    private int _accNext;
		    private int _accumulator;
		    private bool _controlTestPrev;
		    private int _controlWavePrev;
		    private int _delay;
		    private int _floatOutputTtl;
		    private int _frequency;
		    private bool _msbRising;
		    private int _noise;
		    private int _noNoise;
		    private int _noNoiseOrNoise;
		    private int _noPulse;
		    private int _output;
		    private int _pulse;
		    private int _pulseWidth;
		    private bool _ringMod;
		    private int _ringMsbMask;
		    private int _shiftRegister;
		    private int _shiftRegisterReset;
		    private bool _sync;
		    private bool _test;
		    [SaveState.DoNotSave] private int[] _wave;
		    private int _waveform;
		    private int _waveformIndex;
            [SaveState.DoNotSave] private readonly int[][] _waveTable;

			public Voice(int[][] newWaveTable)
			{
				_waveTable = newWaveTable;
				HardReset();
			}

			public void HardReset()
			{
				_accumulator = 0;
				_delay = 0;
				_floatOutputTtl = 0;
				_frequency = 0;
				_msbRising = false;
				_noNoise = 0xFFF;
				_noPulse = 0xFFF;
				_output = 0x000;
				_pulse = 0xFFF;
				_pulseWidth = 0;
				_ringMsbMask = 0;
				_sync = false;
				_test = false;
				_wave = _waveTable[0];
				_waveform = 0;

				ResetShiftReg();
			}

			public void ExecutePhase2()
			{

				{
					if (_test)
					{
						if (_shiftRegisterReset != 0 && --_shiftRegisterReset == 0)
						{
							ResetShiftReg();
						}
						_pulse = 0xFFF;
					}
					else
					{
						_accNext = (_accumulator + _frequency) & 0xFFFFFF;
						_accBits = ~_accumulator & _accNext;
						_accumulator = _accNext;
						_msbRising = (_accBits & 0x800000) != 0;

						if ((_accBits & 0x080000) != 0)
							_delay = 2;
						else if (_delay != 0 && --_delay == 0)
							ClockShiftReg();
					}
				}
			}

			// ------------------------------------

			private void ClockShiftReg()
			{

				{
					_shiftRegister = ((_shiftRegister << 1) |
						(((_shiftRegister >> 22) ^ (_shiftRegister >> 17)) & 0x1)
						) & 0x7FFFFF;
					SetNoise();
				}
			}

			private void ResetShiftReg()
			{

				{
					_shiftRegister = 0x7FFFFF;
					_shiftRegisterReset = 0;
					SetNoise();
				}
			}

			private void SetNoise()
			{

				{
					_noise =
						((_shiftRegister & 0x100000) >> 9) |
						((_shiftRegister & 0x040000) >> 8) |
						((_shiftRegister & 0x004000) >> 5) |
						((_shiftRegister & 0x000800) >> 3) |
						((_shiftRegister & 0x000200) >> 2) |
						((_shiftRegister & 0x000020) << 1) |
						((_shiftRegister & 0x000004) << 3) |
						((_shiftRegister & 0x000001) << 4);
					_noNoiseOrNoise = _noNoise | _noise;
				}
			}

			private void WriteShiftReg()
			{

				{
					_output &=
						0xBB5DA |
						((_output & 0x800) << 9) |
						((_output & 0x400) << 8) |
						((_output & 0x200) << 5) |
						((_output & 0x100) << 3) |
						((_output & 0x040) >> 1) |
						((_output & 0x020) >> 3) |
						((_output & 0x010) >> 4);
					_noise &= _output;
					_noNoiseOrNoise = _noNoise | _noise;
				}
			}

			// ------------------------------------

			public int Control
			{
				set
				{
					_controlWavePrev = _waveform;
					_controlTestPrev = _test;

					_sync = (value & 0x02) != 0;
					_ringMod = (value & 0x04) != 0;
					_test = (value & 0x08) != 0;
					_waveform = (value >> 4) & 0x0F;
					_wave = _waveTable[_waveform & 0x07];
					_ringMsbMask = ((~value >> 5) & (value >> 2) & 0x1) << 23;
					_noNoise = (_waveform & 0x8) != 0 ? 0x000 : 0xFFF;
					_noNoiseOrNoise = _noNoise | _noise;
					_noPulse = (_waveform & 0x4) != 0 ? 0x000 : 0xFFF;

					if (!_controlTestPrev && _test)
					{
						_accumulator = 0;
						_delay = 0;
						_shiftRegisterReset = 0x8000;
					}
					else if (_controlTestPrev && !_test)
					{
						_shiftRegister = ((_shiftRegister << 1) |
							((~_shiftRegister >> 17) & 0x1)
							) & 0x7FFFFF;
						SetNoise();
					}

					if (_waveform == 0 && _controlWavePrev != 0)
						_floatOutputTtl = 0x28000;
				}
			}

			public int FrequencyLo
			{
				get
				{
					return _frequency & 0xFF;
				}
				set
				{
					_frequency &= 0xFF00;
					_frequency |= value & 0x00FF;
				}
			}

			public int FrequencyHi
			{
				get
				{
					return _frequency >> 8;
				}
				set
				{
					_frequency &= 0x00FF;
					_frequency |= (value & 0x00FF) << 8;
				}
			}

			public int Output(Voice ringModSource)
			{

				{
					if (_waveform != 0)
					{
						_waveformIndex = (_accumulator ^ (ringModSource._accumulator & _ringMsbMask)) >> 12;
						_output = _wave[_waveformIndex] & (_noPulse | _pulse) & _noNoiseOrNoise;
						if (_waveform > 8)
							WriteShiftReg();
					}
					else
					{
						if (_floatOutputTtl != 0 && --_floatOutputTtl == 0)
							_output = 0x000;
					}
					_pulse = _accumulator >> 12 >= _pulseWidth ? 0xFFF : 0x000;
					return _output;
				}
			}

			public int PulseWidthLo
			{
				get
				{
					return _pulseWidth & 0xFF;
				}
				set
				{
					_pulseWidth &= 0x0F00;
					_pulseWidth |= value & 0x00FF;
				}
			}

			public int PulseWidthHi
			{
				get
				{
					return _pulseWidth >> 8;
				}
				set
				{
					_pulseWidth &= 0x00FF;
					_pulseWidth |= (value & 0x000F) << 8;
				}
			}

			public bool RingMod
			{
				get
				{
					return _ringMod;
				}
			}

			public bool Sync
			{
				get
				{
					return _sync;
				}
			}

			public void Synchronize(Voice target, Voice source)
			{
				if (_msbRising && target._sync && !(_sync && source._msbRising))
					target._accumulator = 0;
			}

			public bool Test
			{
				get
				{
					return _test;
				}
			}

			public int Waveform
			{
				get
				{
					return _waveform;
				}
			}

			// ------------------------------------

			public void SyncState(Serializer ser)
			{
				SaveState.SyncObject(ser, this);
                _wave = _waveTable[_waveform & 0x07];
            }
		}

	}
}
