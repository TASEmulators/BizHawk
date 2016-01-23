using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Sid
	{

		// ------------------------------------

		public SpeexResampler Resampler;

	    private int _cachedCycles;
	    private bool _disableVoice3;
	    private int _envelopeOutput0;
        private int _envelopeOutput1;
        private int _envelopeOutput2;
        private readonly Envelope[] _envelopes;
	    private readonly Envelope _envelope0;
        private readonly Envelope _envelope1;
        private readonly Envelope _envelope2;
        private readonly bool[] _filterEnable;
	    private int _filterFrequency;
	    private int _filterResonance;
	    private bool _filterSelectBandPass;
	    private bool _filterSelectLoPass;
	    private bool _filterSelectHiPass;
	    private int _mixer;
	    private int _potCounter;
	    private int _potX;
	    private int _potY;
	    private short _sample;
	    private int _voiceOutput0;
        private int _voiceOutput1;
        private int _voiceOutput2;
        private readonly Voice[] _voices;
	    private readonly Voice _voice0;
        private readonly Voice _voice1;
        private readonly Voice _voice2;
        private int _volume;

	    public Func<int> ReadPotX;
		public Func<int> ReadPotY;

		public Sid(int[][] newWaveformTable, uint sampleRate, uint cyclesNum, uint cyclesDen)
		{
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

			Resampler = new SpeexResampler(0, cyclesNum, sampleRate * cyclesDen, cyclesNum, sampleRate * cyclesDen, null, null);
		}

		public void Dispose()
		{
			if (Resampler != null)
			{
				Resampler.Dispose();
				Resampler = null;
			}
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

		public void ExecutePhase2()
		{
			_cachedCycles++;

			// potentiometer values refresh every 512 cycles
			if (_potCounter == 0)
			{
				_potCounter = 512;
				_potX = ReadPotX();
				_potY = ReadPotY();
				Flush(); //this is here unrelated to the pots, just to keep the buffer somewhat loaded
			}
			_potCounter--;
		}

		public void Flush()
		{
			while (_cachedCycles-- > 0)
			{
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
				Resampler.EnqueueSample(_sample, _sample);
			}
		}

		// ----------------------------------

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
			ser.BeginSection("env0");
			_envelopes[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav0");
			_voices[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("env1");
			_envelopes[1].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav1");
			_voices[1].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("env2");
			_envelopes[2].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav2");
			_voices[2].SyncState(ser);
			ser.EndSection();
		}
	}
}
