using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Sid
	{
        /*
            Commodore SID 6581/8580 core.

            Many thanks to:
            - Michael Huth for die shots of the 6569R3 chip (to get ideas how to implement)
              http://mail.lipsia.de/~enigma/neu/6581.html
            - Kevtris for figuring out ADSR tables
              http://blog.kevtris.org/?p=13
            - Mixer for a lot of useful SID info
              http://www.sid.fi/sidwiki/doku.php?id=sid-knowledge
            - Documentation collected by the libsidplayfp team
              https://sourceforge.net/projects/sidplay-residfp/
        */

        // ------------------------------------

        private int _cachedCycles;
	    private bool _disableVoice3;
	    private int _envelopeOutput0;
        private int _envelopeOutput1;
        private int _envelopeOutput2;
        private readonly Envelope[] _envelopes;
	    [SaveState.DoNotSave] private readonly Envelope _envelope0;
        [SaveState.DoNotSave] private readonly Envelope _envelope1;
        [SaveState.DoNotSave] private readonly Envelope _envelope2;
        private readonly bool[] _filterEnable;
	    private int _filterFrequency;
	    private int _filterResonance;
	    private bool _filterSelectBandPass;
	    private bool _filterSelectLoPass;
	    private bool _filterSelectHiPass;
	    private int _mixer;
        [SaveState.DoNotSave] private readonly short[] _outputBuffer;
	    [SaveState.DoNotSave] private int _outputBufferIndex;
        private int _potCounter;
	    private int _potX;
	    private int _potY;
        private short _sample;
	    private int _voiceOutput0;
        private int _voiceOutput1;
        private int _voiceOutput2;
        private readonly Voice[] _voices;
	    [SaveState.DoNotSave] private readonly Voice _voice0;
        [SaveState.DoNotSave] private readonly Voice _voice1;
        [SaveState.DoNotSave] private readonly Voice _voice2;
        private int _volume;

	    public Func<int> ReadPotX;
		public Func<int> ReadPotY;

	    [SaveState.DoNotSave] private readonly int _cpuCyclesNum;
	    [SaveState.DoNotSave] private int _sampleCyclesNum;
	    [SaveState.DoNotSave] private readonly int _sampleCyclesDen;
	    [SaveState.DoNotSave] private readonly int _sampleRate;

        public Sid(int[][] newWaveformTable, int sampleRate, int cyclesNum, int cyclesDen)
        {
            _sampleRate = sampleRate;
            _cpuCyclesNum = cyclesNum;
		    _sampleCyclesDen = cyclesDen*sampleRate;

            _envelopes = new Envelope[3];
			for (var i = 0; i < 3; i++)
				_envelopes[i] = new Envelope();
		    _envelope0 = _envelopes[0];
            _envelope1 = _envelopes[1];
            _envelope2 = _envelopes[2];

            _voices = new Voice[3];
			for (var i = 0; i < 3; i++)
				_voices[i] = new Voice(newWaveformTable);
		    _voice0 = _voices[0];
            _voice1 = _voices[1];
            _voice2 = _voices[2];

            _filterEnable = new bool[3];
			for (var i = 0; i < 3; i++)
				_filterEnable[i] = false;

		    _outputBuffer = new short[sampleRate];
		}

		// ------------------------------------

		public void HardReset()
		{
			for (var i = 0; i < 3; i++)
			{
				_envelopes[i].HardReset();
				_voices[i].HardReset();
			}
			_potCounter = 0;
			_potX = 0;
			_potY = 0;
		}

		// ------------------------------------

		public void ExecutePhase()
		{
			_cachedCycles++;

			// potentiometer values refresh every 512 cycles
			if (_potCounter == 0)
			{
				_potCounter = 512;
				_potX = ReadPotX();
				_potY = ReadPotY();
			}
			_potCounter--;
		}

		public void Flush()
		{
            while (_cachedCycles > 0)
            {
                _cachedCycles--;

				// process voices and envelopes
				_voice0.ExecutePhase2();
				_voice1.ExecutePhase2();
				_voice2.ExecutePhase2();
				_envelope0.ExecutePhase2();
				_envelope1.ExecutePhase2();
				_envelope2.ExecutePhase2();

                _voice0.Synchronize(_voice1, _voice2);
                _voice1.Synchronize(_voice2, _voice0);
                _voice2.Synchronize(_voice0, _voice1);

                // get output
                _voiceOutput0 = _voice0.Output(_voice2);
				_voiceOutput1 = _voice1.Output(_voice0);
				_voiceOutput2 = _voice2.Output(_voice1);
				_envelopeOutput0 = _envelope0.Level;
				_envelopeOutput1 = _envelope1.Level;
				_envelopeOutput2 = _envelope2.Level;

			    _sampleCyclesNum += _sampleCyclesDen;
			    if (_sampleCyclesNum >= _cpuCyclesNum)
			    {
			        _sampleCyclesNum -= _cpuCyclesNum;
                    _mixer = (_voiceOutput0 * _envelopeOutput0) >> 7;
                    _mixer += (_voiceOutput1 * _envelopeOutput1) >> 7;
                    _mixer += (_voiceOutput2 * _envelopeOutput2) >> 7;
                    _mixer = (_mixer * _volume) >> 4;
                    _mixer -= _volume << 8;

                    if (_mixer > 0x7FFF)
                    {
                        _mixer = 0x7FFF;
                    }
                    if (_mixer < -0x8000)
                    {
                        _mixer = -0x8000;
                    }

                    _sample = unchecked((short)_mixer);
                    if (_outputBufferIndex < _sampleRate)
                    {
                        _outputBuffer[_outputBufferIndex++] = _sample;
                        _outputBuffer[_outputBufferIndex++] = _sample;
                    }
                }
            }
        }

        // ----------------------------------

        public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}
	}
}
