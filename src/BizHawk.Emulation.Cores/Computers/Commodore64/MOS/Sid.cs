using BizHawk.Common;

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
		public int _databus;
		private int _cachedCycles;
		private bool _disableVoice3;
		private int _envelopeOutput0;
		private int _envelopeOutput1;
		private int _envelopeOutput2;
		private readonly Envelope[] _envelopes;
		private readonly Envelope _envelope0;
		private readonly Envelope _envelope1;
		private readonly Envelope _envelope2;
		private bool[] _filterEnable;
		private int _filterFrequency;
		private int _filterResonance;
		private bool _filterSelectBandPass;
		private bool _filterSelectLoPass;
		private bool _filterSelectHiPass;
		private int _mixer;
		private short[] _outputBuffer;
		private readonly int[] _outputBufferFiltered;
		private readonly int[] _outputBufferNotFiltered;
		private readonly int[] _volumeAtSampleTime;
		private int _outputBufferIndex;
		private int _filterIndex;
		private int _lastFilteredValue;
		private int _potCounter;
		private int _potX;
		private int _potY;
		private int _sample;
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

		private RealFFT _fft;
		private double[] _fftBuffer = new double[0];

		private readonly int _cpuCyclesNum;
		private int _sampleCyclesNum;
		private readonly int _sampleCyclesDen;
		private readonly int _sampleRate;

		public Sid(int[][] newWaveformTable, int sampleRate, int cyclesNum, int cyclesDen)
		{
			_sampleRate = sampleRate;
			_cpuCyclesNum = cyclesNum;
			_sampleCyclesDen = cyclesDen * sampleRate;

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

			_outputBufferFiltered = new int[sampleRate];
			_outputBufferNotFiltered = new int[sampleRate];
			_volumeAtSampleTime = new int[sampleRate];
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
			_filterEnable[0] = false;
			_filterEnable[1] = false;
			_filterEnable[2] = false;
			_filterFrequency = 0;
			_filterSelectBandPass = false;
			_filterSelectHiPass = false;
			_filterSelectLoPass = false;
			_filterResonance = 0;
			_volume = 0;
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

		public void Flush(bool flushFilter)
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

				int temp_v0 = (_voiceOutput0 * _envelopeOutput0);
				int temp_v1 = (_voiceOutput1 * _envelopeOutput1);
				int temp_v2 = (_voiceOutput2 * _envelopeOutput2);

				int temp_filtered = 0;
				int temp_not_filtered = 0;

				//note that voice 3 disable is relevent only if it is not going to the filter 
				// see block diargam http://archive.6502.org/datasheets/mos_6581_sid.pdf
				if (!_filterEnable[2] && _disableVoice3)
					temp_v2 = 0;

				// break sound into filtered and non-filtered output
				// we need to process the filtered parts in bulk, so let's do it here
				if (_filterEnable[0])
					temp_filtered += temp_v0;
				else
					temp_not_filtered += temp_v0;

				if (_filterEnable[1])
					temp_filtered += temp_v1;
				else
					temp_not_filtered += temp_v1;

				if (_filterEnable[2])
					temp_filtered += temp_v2;
				else
					temp_not_filtered += temp_v2;

				_sampleCyclesNum += _sampleCyclesDen;
				if (_sampleCyclesNum >= _cpuCyclesNum)
				{
					_sampleCyclesNum -= _cpuCyclesNum;

					if (_outputBufferIndex < _sampleRate)
					{
						_outputBufferNotFiltered[_outputBufferIndex] = temp_not_filtered;
						_outputBufferFiltered[_outputBufferIndex] = temp_filtered;
						_volumeAtSampleTime[_outputBufferIndex] = _volume;
						_outputBufferIndex++;
					}
				}
			}
			//here we need to apply filtering to the samples and add them back to the buffer
			if (flushFilter)
			{
				if (_filterEnable[0] | _filterEnable[1] | _filterEnable[2])
				{
					if ((_outputBufferIndex - _filterIndex) >= 16)
					{
						filter_operator();
					}
					else
					{
						// the length is too short for the FFT to reliably act on the output
						// instead, clamp it to the previous output.
						for (int i = _filterIndex; i < _outputBufferIndex; i++)
						{
							_outputBufferFiltered[i] = _lastFilteredValue;
						}
					}
				}

				_filterIndex = _outputBufferIndex;
				if (_outputBufferIndex>0)
					_lastFilteredValue = _outputBufferFiltered[_outputBufferIndex - 1];
			}

			// if the filter is off, keep updating the filter index to the most recent Flush
			if (!(_filterEnable[0] | _filterEnable[1] | _filterEnable[2]))
			{
				_filterIndex = _outputBufferIndex;
			}	
		}


		public void filter_operator()
		{
			double loc_filterFrequency = (double)(_filterFrequency << 2) + 750;

			double attenuation;

			int nsamp = _outputBufferIndex - _filterIndex;

			// pass the list of filtered samples to the FFT
			// but needs to be a power of 2, so find the next highest power of 2 and re-sample
			int nsamp_2 = 2;
			bool test = true;
			while(test)
			{
				nsamp_2 *= 2;
				if (nsamp_2>nsamp)
				{
					test = false;
				}
			}

			_fft = new RealFFT(nsamp_2);

			// eventually this will settle on a single buffer size and stop reallocating
			if (_fftBuffer.Length < nsamp_2)
				Array.Resize(ref _fftBuffer, nsamp_2);

			// linearly interpolate the original sample set into the new denser sample set
			for (double i = 0; i < nsamp_2; i++)
			{
				_fftBuffer[(int)i] = _outputBufferFiltered[(int)Math.Floor((i / (nsamp_2-1) * (nsamp - 1))) + _filterIndex];
			}

			// now we have everything we need to perform the FFT
			_fft.ComputeForward(_fftBuffer);

			// for each element in the frequency list, attenuate it according to the specs
			for (int i = 1; i < nsamp_2; i++)
			{
				double freq = i * ((double)(880*50)/nsamp);

				// add resonance effect
				// let's assume that frequencies near the peak are doubled in strength at max resonance
				if ((1.2 > freq / loc_filterFrequency) && (freq / loc_filterFrequency > 0.8 ))
				{
					_fftBuffer[i] = _fftBuffer[i] * (1 + (double)_filterResonance/15);
				}

				// low pass filter
				if (_filterSelectLoPass && freq > loc_filterFrequency)
				{
					//attenuated at 12db per octave
					attenuation = Math.Log(freq / loc_filterFrequency, 2);
					attenuation = 12 * attenuation;
					_fftBuffer[i] = _fftBuffer[i] * Math.Pow(2, -attenuation / 10);
				}

				// High pass filter
				if (_filterSelectHiPass && freq < loc_filterFrequency)
				{
					//attenuated at 12db per octave
					attenuation = Math.Log(loc_filterFrequency / freq, 2);
					attenuation = 12 * attenuation;
					_fftBuffer[i] = _fftBuffer[i] * Math.Pow(2, -attenuation / 10);
				}
				
				// Band pass filter
				if (_filterSelectBandPass)
				{
					//attenuated at 6db per octave
					attenuation = Math.Log(freq / loc_filterFrequency, 2);
					attenuation = 6 * attenuation;
					_fftBuffer[i] = _fftBuffer[i] * Math.Pow(2, -Math.Abs(attenuation) / 10);
				}
				
			}

			// now transform back into time space and reassemble the attenuated frequency components
			_fft.ComputeReverse(_fftBuffer);

			int temp = nsamp - 1;
			//re-sample back down to the original number of samples
			for (double i = 0; i < nsamp; i++)
			{
				_outputBufferFiltered[(int)i + _filterIndex] = (int)(_fftBuffer[(int)Math.Ceiling((i / (nsamp - 1) * (nsamp_2 - 1)))] * _fft.CorrectionScaleFactor);

				if (_outputBufferFiltered[(int)i + _filterIndex] < 0)
				{
					_outputBufferFiltered[(int)i + _filterIndex] = 0;
				}

				// the FFT is only an approximate model and fails at low sample rates
				// what we want to do is limit how much the output samples can deviate from previous output
				// thus smoothing out the FT samples
				
				if (i<16)
					_outputBufferFiltered[(int)i + _filterIndex] = (int)((_lastFilteredValue * Math.Pow(15 - i,1) + _outputBufferFiltered[(int)i + _filterIndex] * Math.Pow(i,1))/ Math.Pow(15,1));
			}
		}
		// ----------------------------------

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_databus), ref _databus);
			ser.Sync(nameof(_cachedCycles), ref _cachedCycles);
			ser.Sync(nameof(_disableVoice3), ref _disableVoice3);
			ser.Sync(nameof(_envelopeOutput0), ref _envelopeOutput0);
			ser.Sync(nameof(_envelopeOutput1), ref _envelopeOutput1);
			ser.Sync(nameof(_envelopeOutput2), ref _envelopeOutput2);

			for (int i = 0; i < _envelopes.Length; i++)
			{
				ser.BeginSection("Envelope" + i);
				_envelopes[i].SyncState(ser);
				ser.EndSection();
			}

			ser.Sync(nameof(_filterEnable), ref _filterEnable, useNull: false);
			ser.Sync(nameof(_filterFrequency), ref _filterFrequency);
			ser.Sync(nameof(_filterResonance), ref _filterResonance);
			ser.Sync(nameof(_filterSelectBandPass), ref _filterSelectBandPass);
			ser.Sync(nameof(_filterSelectLoPass), ref _filterSelectLoPass);
			ser.Sync(nameof(_filterSelectHiPass), ref _filterSelectHiPass);
			ser.Sync(nameof(_mixer), ref _mixer);
			ser.Sync(nameof(_potCounter), ref _potCounter);
			ser.Sync(nameof(_potX), ref _potX);
			ser.Sync(nameof(_potY), ref _potY);
			ser.Sync(nameof(_sample), ref _sample);
			ser.Sync(nameof(_voiceOutput0), ref _voiceOutput0);
			ser.Sync(nameof(_voiceOutput1), ref _voiceOutput1);
			ser.Sync(nameof(_voiceOutput2), ref _voiceOutput2);

			for (int i = 0; i < _voices.Length; i++)
			{
				ser.BeginSection("Voice" + i);
				_voices[i].SyncState(ser);
				ser.EndSection();
			}

			ser.Sync(nameof(_volume), ref _volume);
		}
	}
}
