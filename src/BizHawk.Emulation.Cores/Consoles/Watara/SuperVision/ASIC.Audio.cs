
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class ASIC : ISoundProvider
	{
		public const double SAMPLE_RATE = 44100;

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
		private CH_Noise _ch4;

		private void InitAudio()
		{
			_ch1 = new CH_Square(this, 1);
			_ch2 = new CH_Square(this, 2);
			_ch4 = new CH_Noise(this);
		}

		public void AudioClock()
		{

		}

		public void SyncAudioState(Serializer ser)
		{
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
			private double _cpuFreq => _asic._sv.CpuFreq;			

			/// <summary>
			/// Square channel frequency
			/// Comprised of 8 bits of CHx_Flow and 3 bits of CHx_Fhigh
			/// </summary>
			public ushort Freq => (ushort)(_asic.Regs[AR_FLow] | ((_asic.Regs[AR_FHigh] & 0b0000_0111) << 8));

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
			public int LenPrescaler
			{
				get => _lenPrescaler;
				set => _lenPrescaler = value & 0x10000; 
			}
			private int _lenPrescaler;

			public CH_Square(ASIC asic, int chIndex)
			{
				_asic = asic;

				if (chIndex < 1 || chIndex > 2)
				{
					throw new System.ArgumentOutOfRangeException(nameof(chIndex), "Squarewave chIndex must be either 1 or 2");
				}

				_chIndex = chIndex;
			}

			public void Clock(int ticks)
			{
				for (int t = 0; t < ticks; t++)
				{
					if (++LenPrescaler == 0)
					{

					}
				}
			}

			/// <summary>
			/// Called whenever the length register is written to
			/// </summary>
			public void LengthChanged() => _lenCounter = Length;

			private static int GetSquareWaveSample(double time, double freq)
			{
				var sTime = time / SAMPLE_RATE;
				var period = 1.0 / freq;
				var halfPeriod = period / 2.0;
				var currentTimeInPeriod = sTime % period;
				return currentTimeInPeriod < halfPeriod ? 32766 : -32766;
			}

			public virtual void SyncState(Serializer ser)
			{
				ser.BeginSection($"AUDIO_CH{_chIndex}_NOISE");
				ser.Sync(nameof(LenPrescaler), ref _lenPrescaler);
				ser.Sync(nameof(_lenCounter), ref _lenCounter);
				ser.EndSection();
			}
		}

		/// <summary>
		/// Noise Channel
		/// </summary>
		public class CH_Noise
		{
			private ASIC _asic;
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
			public int LenPrescaler
			{
				get => _lenPrescaler;
				set => _lenPrescaler = value & 0x10000;
			}
			private int _lenPrescaler;

			/// <summary>
			/// Length counter is clocked by the prescaler, decrementing each time the prescaler expires
			/// </summary>
			private int _lenCounter;


			public CH_Noise(ASIC asic)
			{
				_asic = asic;
				_noiseFreqs = NoiseFreqEntry.Init(_cpuFreq);
			}

			public void Clock(int ticks)
			{
				for (int t = 0; t < ticks; t++)
				{
					if (++LenPrescaler == 0)
					{

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

		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = 1;
			samples = new short[nsamp];
		}
	}
}
