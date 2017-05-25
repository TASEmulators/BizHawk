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
		private readonly short[] _outputBuffer;
		private int[] _outputBuffer_filtered;
		private int[] _outputBuffer_not_filtered;
		private int _outputBufferIndex;
		private int last_index;
		private int filter_index;
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

		public RealFFT fft;

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

			_outputBuffer = new short[sampleRate];
			_outputBuffer_filtered = new int[sampleRate];
			_outputBuffer_not_filtered = new int[sampleRate];
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
						_outputBuffer_not_filtered[_outputBufferIndex] = temp_not_filtered;
						_outputBuffer_filtered[filter_index] = temp_filtered;
						_outputBufferIndex++;
						filter_index++;
					}
				}
			}
			//here we need to apply filtering to the samples and add them back to the buffer
			
			if (_filterEnable[0] | _filterEnable[1] | _filterEnable[2])
				if (filter_index >= 2)
					filter_operator();
			
			for (int i = last_index; i < _outputBufferIndex; i++)
			{
				_mixer = _outputBuffer_not_filtered[i] + _outputBuffer_filtered[i-last_index];
				_mixer = _mixer >> 7;
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

				_outputBuffer[i * 2] = (short)_mixer;
				_outputBuffer[i * 2 + 1] = (short)_mixer;

			}

			last_index = _outputBufferIndex;
			filter_index = 0;
		}

		public void filter_operator()
		{
			double loc_filterFrequency = (double)(_filterFrequency<<2);

			double attenuation;

			int nsamp = filter_index;

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

			fft = new RealFFT(nsamp_2);

			double[] temp_buffer = new double[nsamp_2];

			// linearly interpolate the original sample set into the new denser sample set
			for (double i = 0; i < nsamp_2; i++)
			{
				temp_buffer[(int)i] = _outputBuffer_filtered[(int)Math.Floor((i / (nsamp_2-1) * (filter_index - 1)))];
			}

			// now we have everything we need to perform the FFT
			fft.ComputeForward(temp_buffer);

			// for each element in the frequency list, attenuate it according to the specs
			for (int i = 0; i < nsamp_2; i++)
			{
				double freq = i * ((double)(880*50)/filter_index);

				// low pass filter
				if (_filterSelectLoPass && freq > loc_filterFrequency)
				{
					//attenuated at 12db per octave
					attenuation = Math.Log(freq / loc_filterFrequency, 2);
					attenuation = 12 * attenuation;
					temp_buffer[i] = temp_buffer[i] * Math.Pow(2, -attenuation / 10);
				}

				// High pass filter
				if (_filterSelectHiPass && freq < _filterFrequency)
				{
					//attenuated at 12db per octave
					attenuation = Math.Log(freq / _filterFrequency, 2);
					attenuation = 12 * attenuation;
					temp_buffer[i] = temp_buffer[i] * Math.Pow(2, -attenuation / 10);
				}
				
				// Band pass filter
				if (_filterSelectBandPass)
				{
					//attenuated at 6db per octave
					attenuation = Math.Log(freq / _filterFrequency, 2);
					temp_buffer[i] = temp_buffer[i] - 6 * Math.Abs(attenuation);
				}
				
			}
			
			// now transform back into time space and reassemble the attenuated frequency components
			fft.ComputeReverse(temp_buffer);

			//re-sample back down to the original number of samples
			for (double i = 0; i < filter_index; i++)
			{
				_outputBuffer_filtered[(int)i] = (int)(temp_buffer[(int)Math.Floor((i / (filter_index-1) * (nsamp_2 - 1)))]/(nsamp_2/2));
				if (loc_filterFrequency==0)
				{
					_outputBuffer_filtered[(int)i] = 0;
				}
			}

		}
		// ----------------------------------

		public void SyncState(Serializer ser)
		{
			ser.Sync("last index", ref last_index);
			ser.Sync("_databus", ref _databus);
			ser.Sync("_cachedCycles", ref _cachedCycles);
			ser.Sync("_disableVoice3", ref _disableVoice3);
			ser.Sync("_envelopeOutput0", ref _envelopeOutput0);
			ser.Sync("_envelopeOutput1", ref _envelopeOutput1);
			ser.Sync("_envelopeOutput2", ref _envelopeOutput2);

			for (int i = 0; i < _envelopes.Length; i++)
			{
				ser.BeginSection("Envelope" + i);
				_envelopes[i].SyncState(ser);
				ser.EndSection();
			}

			ser.Sync("_filterEnable", ref _filterEnable, useNull: false);
			ser.Sync("_filterFrequency", ref _filterFrequency);
			ser.Sync("_filterResonance", ref _filterResonance);
			ser.Sync("_filterSelectBandPass", ref _filterSelectBandPass);
			ser.Sync("_filterSelectLoPass", ref _filterSelectLoPass);
			ser.Sync("_filterSelectHiPass", ref _filterSelectHiPass);
			ser.Sync("_mixer", ref _mixer);
			ser.Sync("_potCounter", ref _potCounter);
			ser.Sync("_potX", ref _potX);
			ser.Sync("_potY", ref _potY);
			ser.Sync("_sample", ref _sample);
			ser.Sync("_voiceOutput0", ref _voiceOutput0);
			ser.Sync("_voiceOutput1", ref _voiceOutput1);
			ser.Sync("_voiceOutput2", ref _voiceOutput2);

			for (int i = 0; i < _voices.Length; i++)
			{
				ser.BeginSection("Voice" + i);
				_voices[i].SyncState(ser);
				ser.EndSection();
			}

			ser.Sync("_volume", ref _volume);
		}
	}
}
