
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class ASIC : ISoundProvider
	{
		/// <summary>
		/// Output sample rate
		/// </summary>
		public const double SAMPLE_RATE = 44100;

		/// <summary>
		/// Total number of audio samples in a frame
		/// </summary>
		public double SamplesPerFrame => SAMPLE_RATE / _sv.RefreshRate;

		/// <summary>
		/// Total CPU cycles in a frame
		/// </summary>
		public int CpuTicksPerFrame => (int)_sv.CpuTicksPerFrame;

		/// <summary>
		/// The calculated CPU tick position within the frame
		/// </summary>
		public int TickPos;

		/// <summary>
		/// Keeps track of additional ticks outside of the frame window that need to be processed as a part of the next frame
		/// </summary>
		public int TicksOverrun;

		/// <summary>
		/// CH1: the right square wave channel
		/// </summary>
		public CH_Square _ch1;

		/// <summary>
		/// CH2: the left square wave channel
		/// </summary>
		public CH_Square _ch2;

		/// <summary>
		/// CH4: the noise channel
		/// </summary>
		public CH_Noise _ch4;

		

		public void InitAudio()
		{
			_ch1 = new CH_Square(this, 1);
			_ch2 = new CH_Square(this, 2);
			_ch4 = new CH_Noise(this);

			ch1Buff = new short[(int)SamplesPerFrame];
			ch2Buff = new short[(int)SamplesPerFrame];

			ch4LBuff = new short[(int)SamplesPerFrame];
			ch4RBuff = new short[(int)SamplesPerFrame];
		}

		public void AudioClock(int incomingTicks)
		{
			int ticks = incomingTicks + TicksOverrun;

			// determine frame boundaries
			if (TickPos + ticks >= CpuTicksPerFrame)
			{
				// we are about to overrun the frame
				TicksOverrun = TickPos + ticks - CpuTicksPerFrame;
				// set ticks to run only up to the frame boundary
				ticks -= TicksOverrun;
			}
			else
			{
				// still within frame boundaries
				TicksOverrun = 0;
			}

			_ch1.Clock(ticks);
			_ch2.Clock(ticks);

			_ch4.Clock(ticks);

			TickPos += ticks;
			if (TickPos == CpuTicksPerFrame)
			{
				TickPos = 0;
			}
		}

		public void SyncAudioState(Serializer ser)
		{
			_ch1.SyncState(ser);
			_ch2.SyncState(ser);

			_ch4.SyncState(ser);
		}

		/// <summary>
		/// Square Wave Channel
		/// </summary>
		public class CH_Square
		{
			/// <summary>
			/// Channel 1 or 2
			/// </summary>
			private int _chIndex;
			private int AR_FLow => _chIndex == 1 ? R_CH1_F_LOW : R_CH2_F_LOW;
			private int AR_FHigh => _chIndex == 1 ? R_CH1_F_HI : R_CH2_F_HI;
			private int AR_VolDuty => _chIndex == 1 ? R_CH1_VOL_DUTY : R_CH2_VOL_DUTY;
			private int AR_Len => _chIndex == 1 ? R_CH1_LENGTH : R_CH2_LENGTH;

			private ASIC _asic;
			private readonly BlipBuffer _blip;
			public BlipBuffer Blip => _blip;
			private double _cpuFreq => _asic._sv.CpuFreq;			

			/// <summary>
			/// Square channel frequency
			/// Comprised of 8 bits of CHx_Flow and 3 bits of CHx_Fhigh
			/// </summary>
			public ushort Freq => (ushort)(_asic.Regs[AR_FLow] | ((_asic.Regs[AR_FHigh] & 0b0000_0111) << 8));

			/// <summary>
			/// Actual frequency of the square wave
			/// The rev. eng. notes say that the frequency is 12500 / (Freq + 1)
			/// If that was based on a 4MHz clock, then this would be cpuClock / 32 / (Freq + 1)
			/// So for now we will assume it's using a 5bit divider
			/// </summary>
			public double FreqActual => _cpuFreq / 32.0 / (Freq + 1.0);

			/// <summary>
			/// True:	channel always produces sound
			/// False:	only plays when length reg is written
			/// </summary>
			public bool AlwaysOutput => _asic.Regs[AR_VolDuty].Bit(6);

			/// <summary>
			/// Square wave duty cycle
			/// 00:	12.5%
			/// 01:	25%
			/// 10:	50%
			/// 11: 75%
			/// </summary>
			public int DutyCycle => (_asic.Regs[AR_VolDuty] & 0b0011_0000) >> 4;

			/// <summary>
			/// 4-bit linear volume
			/// 0-16
			/// 0: silent, F: loudest
			/// </summary>
			public int Volume => _asic.Regs[AR_VolDuty] & 0b0000_1111;

			

			/// <summary>
			/// Sound plays for Length counts if AlwaysOutput is 0
			/// </summary>
			public int Length => _asic.Regs[AR_Len] & 0b0000_0000;

			/// <summary>
			/// Length counter is clocked by the prescaler, decrementing each time the prescaler expires
			/// </summary>
			private int _lenCounter;

			/// <summary>
			/// Length is clocked by a 16bit prescaler
			/// CpuFreq / 65536
			/// This prescaler is never reloaded or reset
			/// So there is a 1 audio clock uncertainty in the length counter, because it's decremented each time the prescaler expires
			/// </summary>
			public ushort LenPrescaler
			{
				get => _lenPrescaler;
				set => _lenPrescaler = (ushort) (value & 0x10000);
			}
			private ushort _lenPrescaler;

			/// <summary>
			/// Keeps track of ticks since the last waveform change
			/// </summary>
			private double _tickBuffer;

			public CH_Square(ASIC asic, int chIndex)
			{
				_asic = asic;

				if (chIndex < 1 || chIndex > 2)
				{
					throw new System.ArgumentOutOfRangeException(nameof(chIndex), "Squarewave chIndex must be either 1 or 2");
				}

				_chIndex = chIndex;

				_blip = new BlipBuffer((int)(_asic.SamplesPerFrame));
				_blip.SetRates(_cpuFreq, SAMPLE_RATE);
			}

			public void Clock(int ticks)
			{
				for (int t = 0; t < ticks; t++)
				{
					if (++LenPrescaler == 0)
					{
						_lenCounter--;
						if (_lenCounter <= 0)
						{
							_lenCounter = 0;
						}
					}

					if (AlwaysOutput || _lenCounter > 0)
					{
						_tickBuffer++;

						double ticksPerFreq = _asic._sv.CpuFreq / FreqActual;

						if (_tickBuffer >= ticksPerFreq)
						{
							_tickBuffer -= ticksPerFreq;
							int sample = GetSquareWaveSample(_asic.TickPos + t, FreqActual, DutyCycle);
							_blip.AddDelta((uint)_asic._sv.FrameClock, (short)sample);
						}
					}
				}
			}

			/// <summary>
			/// Called whenever the length register is written to
			/// </summary>
			public void LengthChanged() => _lenCounter = Length;
		

			private int GetSquareWaveSample(double time, double freq, int dutyCycle)
			{
				double sTime = time / SAMPLE_RATE;
				double period = 1.0 / freq;
				double dutyCycleFraction = dutyCycle switch
				{
					0 => 0.125, // 12.5%
					1 => 0.25,  // 25%
					2 => 0.50,  // 50%
					3 => 0.75,  // 75%
					_ => 0.50
				};
				double currentTimeInPeriod = sTime % period;
				return currentTimeInPeriod < period * dutyCycleFraction ? Volume * 8 : Volume * -8;
			}

			public virtual void SyncState(Serializer ser)
			{
				ser.BeginSection($"AUDIO_CH{_chIndex}_SQUARE");
				ser.Sync(nameof(LenPrescaler), ref _lenPrescaler);
				ser.Sync(nameof(_lenCounter), ref _lenCounter);
				ser.Sync(nameof(_tickBuffer), ref _tickBuffer);
				ser.EndSection();
			}
		}

		/// <summary>
		/// Noise Channel
		/// </summary>
		public class CH_Noise
		{
			private ASIC _asic;
			private readonly BlipBuffer _blipL;
			private readonly BlipBuffer _blipR;
			public BlipBuffer BlipL => _blipL;
			public BlipBuffer BlipR => _blipR;

			private double _cpuFreq => _asic._sv.CpuFreq;

			/// <summary>
			/// The noise frequency is how often (per second) that the noise LFSR is clocked
			/// </summary>
			private readonly NoiseFreqEntry[] _noiseFreqs;

			/// <summary>
			/// Current NF index from the asic registers
			/// </summary>
			private NoiseFreqEntry _currNoiseFreq => _noiseFreqs[(_asic.Regs[R_CH4_FREQ_VOL] & 0b1111_0000) >> 4];

			/// <summary>
			/// 4-bit linear volume
			/// 0-16
			/// 0: silent, F: loudest
			/// </summary>
			public int Volume => _asic.Regs[R_CH4_FREQ_VOL] & 0b0000_1111;

			/// <summary>
			/// Sound plays for Length counts
			/// </summary>
			public int Length => _asic.Regs[R_CH4_LENGTH] & 0b0000_0000;

			/// <summary>
			/// 1 - enable noise channel continuously.  
			/// 0 - use the length register
			/// </summary>
			public bool ChannelEnable => _asic.Regs[R_CH3_CONTROL].Bit(1);

			/// <summary>
			/// Noise enabled
			/// </summary>
			public bool NoiseEnable => _asic.Regs[R_CH3_CONTROL].Bit(4);

			/// <summary>
			/// Left output. 1 - mix audio with left channel
			/// </summary>
			public bool LeftChannelOutput => _asic.Regs[R_CH3_CONTROL].Bit(3);

			/// <summary>
			/// Right output. 1 - mix audio with right channel
			/// </summary>
			public bool RightChannelOutput => _asic.Regs[R_CH3_CONTROL].Bit(2);

			/// <summary>
			/// Linear Feedback Shift Register
			/// </summary>
			public ushort LFSR
			{
				get { return _lfsr; }
				set { _lfsr = value; }
			}
			private ushort _lfsr = 0x7FFF;  // the reset state (all 1s)

			/// <summary>
			/// Length is clocked by a 16bit prescaler
			/// CpuFreq / 65536
			/// This prescaler is never reloaded or reset
			/// So there is a 1 audio clock uncertainty in the length counter, because it's decremented each time the prescaler expires
			/// </summary>
			public ushort LenPrescaler
			{
				get => _lenPrescaler;
				set => _lenPrescaler = (ushort)(value & 0x10000);
			}
			private ushort _lenPrescaler;

			/// <summary>
			/// Length counter is clocked by the prescaler, decrementing each time the prescaler expires
			/// </summary>
			private int _lenCounter;

			/// <summary>
			/// Keeps track of ticks since the last noise change
			/// </summary>
			private double _tickBuffer;


			public CH_Noise(ASIC asic)
			{
				_asic = asic;
				_noiseFreqs = NoiseFreqEntry.Init(_cpuFreq);

				_blipL = new BlipBuffer((int) (_asic.SamplesPerFrame));
				_blipL.SetRates(_cpuFreq, SAMPLE_RATE);

				_blipR = new BlipBuffer((int) (_asic.SamplesPerFrame));
				_blipR.SetRates(_cpuFreq, SAMPLE_RATE);
			}

			public void Clock(int ticks)
			{
				for (int t = 0; t < ticks; t++)
				{					
					if (++LenPrescaler == 0)
					{
						_lenCounter--;
						if (_lenCounter <= 0)
						{
							_lenCounter = 0;
						}
					}

					if (NoiseEnable)
					{
						if (ChannelEnable || _lenCounter > 0)
						{
							_tickBuffer++;

							if (_tickBuffer >= _currNoiseFreq.TicksPerFreq)
							{
								_tickBuffer -= _currNoiseFreq.TicksPerFreq;
								ClockLFSR();
								_blipL.AddDelta((uint)(_asic.TickPos + t), LeftChannelOutput ? (short)LFSR : 0);
								_blipR.AddDelta((uint)(_asic.TickPos + t), RightChannelOutput ? (short)LFSR : 0);
							}
						}
					}
				}
			}

			/// <summary>
			/// Called whenever the length register is written to
			/// </summary>
			public void LengthChanged() => _lenCounter = Length;

			/// <summary>
			/// Advances the LFSR by one state
			/// </summary>
			private void ClockLFSR()
			{
				bool feedback;

				if (_asic.Regs[ASIC.R_CH3_CONTROL].Bit(0))
				{
					// taps at bits 6 and 7
					feedback = ((_lfsr >> 6) & 1) != ((_lfsr >> 5) & 1);
					// shift and apply the feedback bit
					_lfsr = (ushort) ((_lfsr >> 1) | (feedback ? 0x40 : 0));
				}
				else
				{
					// taps at bits 14 and 15
					feedback = ((_lfsr >> 14) & 1) != ((_lfsr >> 13) & 1);
					// shift and apply the feedback bit
					_lfsr = (ushort) ((_lfsr >> 1) | (feedback ? 0x4000 : 0));
				}
			}

			public virtual void SyncState(Serializer ser)
			{
				ser.BeginSection("AUDIO_CH_NOISE");
				ser.Sync(nameof(LFSR), ref _lfsr);
				ser.Sync(nameof(LenPrescaler), ref _lenPrescaler);
				ser.Sync(nameof(_lenCounter), ref _lenCounter);
				ser.Sync(nameof(_tickBuffer), ref _tickBuffer);
				ser.EndSection();
			}
		}

		

		public class NoiseFreqEntry
		{
			/// <summary>
			/// 16-bit
			/// Corresponds to bits 4-7 of the CH4_Freq_Vol register
			/// </summary>
			public int FrequencyIndex { get; private set; }

			/// <summary>
			/// External clock frequency (in MHz)
			/// </summary>
			public double ClockFrequency { get; private set; }

			/// <summary>
			/// Frequency is 4000000 / divisor
			/// (it looks like the implied divisor value is locked to 18 bits)
			/// </summary>
			public int Divisor => (8 << FrequencyIndex) & 0x20000;

			/// <summary>
			/// Idx Frequency	Divisor
			/// 0	500KHz		8
			/// 1	125KHz		32
			/// 2	62.5KHz		64
			/// 3	31.25KHz	128
			/// 4	15.625KHz	256
			/// 5	7.8125KHz	512
			/// 6	3.90625KHz	1024
			/// 7	1.953KHz	2048
			/// 8	976.56Hz	4096
			/// 9	488.28Hz	8192
			/// A	244.14Hz	16384
			/// B	122.07Hz	32768
			/// C	61.035Hz	65536
			/// D	30.52Hz		131072
			/// E	61.035Hz	65536
			/// F	30.52Hz		131072
			/// </summary>
			public double FrequencyInHz => ClockFrequency / Divisor;

			/// <summary>
			/// The number of CPU clocks per frequency tick
			/// </summary>
			public double TicksPerFreq => ClockFrequency / FrequencyInHz;

			public static NoiseFreqEntry[] Init(double extClockFreq)
			{
				var nl = new List<NoiseFreqEntry>();
				for (int i = 0; i < 16; i++)
				{
					var nfe = new NoiseFreqEntry
					{
						FrequencyIndex = i,
						ClockFrequency = extClockFreq
					};
					nl.Add(nfe);
				}

				return nl.ToArray();
			}
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
				throw new InvalidOperationException("Only Sync mode is supported.");
		}

		public void GetSamplesAsync(short[] samples)
			=> throw new NotSupportedException("Async is not available");

		public void DiscardSamples()
		{
			_ch1.Blip.Clear();
			_ch2.Blip.Clear();
			_ch4.BlipL.Clear();
			_ch4.BlipR.Clear();
		}

		private short[] ch1Buff;
		private short[] ch2Buff;
		private short[] ch4LBuff;
		private short[] ch4RBuff;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = (int)SamplesPerFrame;

			// L			
			_ch2.Blip.EndFrame((uint)CpuTicksPerFrame);
			int ch2Nsamp = _ch2.Blip.SamplesAvailable();
			if (ch2Nsamp != nsamp)
			{

			}
			_ch2.Blip.ReadSamples(ch2Buff, nsamp, false);

			_ch4.BlipL.EndFrame((uint)CpuTicksPerFrame);
			int ch4LNsamp = _ch4.BlipL.SamplesAvailable();
			if (ch4LNsamp != nsamp)
			{

			}
			_ch4.BlipL.ReadSamples(ch4LBuff, nsamp, false);

			// R
			_ch1.Blip.EndFrame((uint)CpuTicksPerFrame);
			int ch1Nsamp = _ch1.Blip.SamplesAvailable();
			if (ch1Nsamp != nsamp)
			{

			}
			_ch1.Blip.ReadSamples(ch1Buff, nsamp, false);

			_ch4.BlipR.EndFrame((uint)CpuTicksPerFrame);
			int ch4RNsamp = _ch4.BlipR.SamplesAvailable();
			if (ch4RNsamp != nsamp)
			{

			}
			_ch4.BlipR.ReadSamples(ch4RBuff, nsamp, false);

			// muxing is pretty shit
			// Each of the outputs (left and right) are only 4 bit DACs.
			// But each channel can output up to 3 different things.  (Square, noise, DMA)
			// The hardware simply adds all three together, and clips it at 0fh
			// we will do similar here, just with short.MaxValue and short.MinValue
			samples = new short[nsamp * 2];

			for (int i = 0, o = 0;  i < nsamp; i++, o += 2)
			{
				int left = ch2Buff[i] + ch4LBuff[i];
				int right = ch1Buff[i] + ch4RBuff[i];

				samples[o] = (short)ClampSample(left);
				samples[o + 1] = (short)ClampSample(right);
			}

			DiscardSamples();
		}

		private static int ClampSample(int sample)
		{
			if (sample >= short.MaxValue)
				sample = short.MaxValue;
			else if (sample <= short.MinValue)
				sample = short.MinValue;

			return sample;
		}
	}
}
