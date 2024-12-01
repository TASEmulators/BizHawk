using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	// This is intended to be a buffer between a synchronous sound provider and the
	// output device (e.g. DirectSound). The idea is to take advantage of the samples
	// buffered up in the output device so that we don't need to keep a bunch buffered
	// up here. This will keep the latency at a minimum. The goal is to keep zero extra
	// samples here on average. As long as we're within +/-5 milliseconds we don't need
	// to touch the source audio. Once it goes outside of that window, we'll start to
	// perform a "soft" correction by resampling it to hopefully get back inside our
	// window shortly. If it ends up going too low or too high, we will perform a
	// "hard" correction by generating silence or discarding samples.
	public class SoundOutputProvider : IBufferedSoundProvider
	{
		private const int SampleRate = 44100;
		private const int ChannelCount = 2;
		private const int SoftCorrectionThresholdSamples = 5 * SampleRate / 1000;
		private const int StartupMaxSamplesSurplusDeficit = 10 * SampleRate / 1000;
		private const int MaxSamplesSurplus = 50 * SampleRate / 1000;
		private const int UsableHistoryLength = 20;
		private const int MaxHistoryLength = 60;
		private const int SoftCorrectionLength = 240;
		private const int BaseMaxConsecutiveEmptyFrames = 1;
		private const int BaseSampleRateUsableHistoryLength = 60;
		private const int BaseSampleRateMaxHistoryLength = 300;
		private const int MinResamplingDistanceSamples = 3;

		private readonly Func<double> _getCoreVsyncRateCallback;

		private readonly List<short> _buffer = [ ];
		private readonly bool _standaloneMode;
		private readonly int _targetExtraSamples;
		private int _maxSamplesDeficit;

		private readonly Queue<int> _extraCountHistory = new Queue<int>();
		private readonly Queue<int> _outputCountHistory = new Queue<int>();
		private readonly Queue<bool> _hardCorrectionHistory = new Queue<bool>();

		private int _baseConsecutiveEmptyFrames;
		private readonly Queue<bool> _baseEmptyFrameCorrectionHistory = new Queue<bool>();

		private double _lastAdvertisedSamplesPerFrame;
		private readonly Queue<int> _baseSamplesPerFrame = new Queue<int>();

		private short[] _outputBuffer = Array.Empty<short>();

		private short[] _resampleBuffer = Array.Empty<short>();
		private double _resampleLengthRoundingError;

		public SoundOutputProvider(Func<double> getCoreVsyncRateCallback, bool standaloneMode = false)
		{
			_getCoreVsyncRateCallback = getCoreVsyncRateCallback;
			_standaloneMode = standaloneMode;
			if (_standaloneMode)
			{
				const double targetExtraMs = 10.0;
				_targetExtraSamples = (int)Math.Ceiling(targetExtraMs * SampleRate / 1000.0);
			}

			ResetBuffer();
		}

		/// <exception cref="InvalidOperationException">(from setter) constructed in standalone mode</exception>
		public int MaxSamplesDeficit
		{
			get => _maxSamplesDeficit;
			set
			{
				if (_standaloneMode) throw new InvalidOperationException();
				_maxSamplesDeficit = value;
			}
		}

		private int EffectiveMaxSamplesDeficit => _maxSamplesDeficit + _targetExtraSamples;

		public ISoundProvider BaseSoundProvider { get; set; }

		public void DiscardSamples()
		{
			ResetBuffer();
			_extraCountHistory.Clear();
			_outputCountHistory.Clear();
			_hardCorrectionHistory.Clear();
			_baseConsecutiveEmptyFrames = 0;
			_baseEmptyFrameCorrectionHistory.Clear();
			_lastAdvertisedSamplesPerFrame = 0.0;
			_baseSamplesPerFrame.Clear();
			_outputBuffer = Array.Empty<short>();
			_resampleBuffer = Array.Empty<short>();
			_resampleLengthRoundingError = 0.0;

			BaseSoundProvider?.DiscardSamples();
		}

		private void ResetBuffer()
		{
			_buffer.Clear();
			for (int i = 0; i < _targetExtraSamples * ChannelCount; i++)
			{
				_buffer.Add(0);
			}
		}

		// To let us know about buffer underruns, rewinding, fast-forwarding, etc.
		public void OnVolatility()
		{
			_extraCountHistory.Clear();
			_outputCountHistory.Clear();
			_hardCorrectionHistory.Clear();
		}

		public bool LogDebug { get; set; }

		private double AdvertisedSamplesPerFrame => SampleRate / _getCoreVsyncRateCallback();

		/// <exception cref="InvalidOperationException">not constructed in standalone mode</exception>
		public void GetSamples(short[] samples)
		{
			if (!_standaloneMode) throw new InvalidOperationException();
			int returnSampleCount = samples.Length / ChannelCount;
			GetSamples(returnSampleCount);
			GetSamplesFromBuffer(samples, returnSampleCount);
		}

		/// <exception cref="InvalidOperationException">constructed in standalone mode</exception>
		public void GetSamples(int idealSampleCount, out short[] samples, out int sampleCount)
		{
			if (_standaloneMode) throw new InvalidOperationException();
			sampleCount = GetSamples(idealSampleCount);
			samples = GetOutputBuffer(sampleCount);
			GetSamplesFromBuffer(samples, sampleCount);
		}

		private int GetSamples(int idealSampleCount)
		{
			double scaleFactor = 1.0;

			if (_extraCountHistory.Count >= UsableHistoryLength && !_hardCorrectionHistory.Any(c => c))
			{
				double offsetFromTarget = CalculatePowerMean(_extraCountHistory, 0.6);
				if (Math.Abs(offsetFromTarget) > SoftCorrectionThresholdSamples)
				{
					double correctionSpan = _outputCountHistory.Average() * SoftCorrectionLength;
					scaleFactor *= correctionSpan / (correctionSpan + offsetFromTarget);
				}
			}

			GetSamplesFromBase(ref scaleFactor);

			int bufferSampleCount = _buffer.Count / ChannelCount;
			int extraSampleCount = bufferSampleCount - _targetExtraSamples - idealSampleCount;
			int maxSamplesDeficit = _extraCountHistory.Count >= UsableHistoryLength ?
				EffectiveMaxSamplesDeficit : Math.Min(StartupMaxSamplesSurplusDeficit, EffectiveMaxSamplesDeficit);
			int maxSamplesSurplus = _extraCountHistory.Count >= UsableHistoryLength ?
				MaxSamplesSurplus : Math.Min(StartupMaxSamplesSurplusDeficit, MaxSamplesSurplus);
			bool hardCorrected = false;

			if (extraSampleCount < -maxSamplesDeficit)
			{
				int generateSampleCount = -extraSampleCount;
				if (LogDebug) Console.WriteLine($"Generating {generateSampleCount} samples");
				for (int i = 0; i < generateSampleCount * ChannelCount; i++)
				{
					_buffer.Add(0);
				}

				hardCorrected = true;
			}
			else if (extraSampleCount > maxSamplesSurplus)
			{
				int discardSampleCount = extraSampleCount;
				if (LogDebug) Console.WriteLine($"Discarding {discardSampleCount} samples");
				_buffer.RemoveRange(0, discardSampleCount * ChannelCount);

				hardCorrected = true;
			}

			bufferSampleCount = _buffer.Count / ChannelCount;
			extraSampleCount = bufferSampleCount - _targetExtraSamples - idealSampleCount;

			int outputSampleCount = Math.Min(idealSampleCount, bufferSampleCount);

			UpdateHistory(_extraCountHistory, extraSampleCount, MaxHistoryLength);
			UpdateHistory(_outputCountHistory, outputSampleCount, MaxHistoryLength);
			UpdateHistory(_hardCorrectionHistory, hardCorrected, MaxHistoryLength);

			if (LogDebug)
			{
				Console.WriteLine("Avg: {0:0.0} ms, Min: {1:0.0}, Max: {2:0.0}, Scale: {3:0.0000}",
					CalculatePowerMean(_extraCountHistory, 0.6) * 1000.0 / SampleRate,
					_extraCountHistory.Min() * 1000.0 / SampleRate,
					_extraCountHistory.Max() * 1000.0 / SampleRate,
					scaleFactor);
			}

			return outputSampleCount;
		}

		private void GetSamplesFromBase(ref double scaleFactor)
		{
			if (BaseSoundProvider.SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Base sound provider must be in sync mode.");
			}
			BaseSoundProvider.GetSamplesSync(out var samples, out var count);

			bool correctedEmptyFrame = false;
			if (count == 0)
			{
				_baseConsecutiveEmptyFrames++;
				if (_baseConsecutiveEmptyFrames > BaseMaxConsecutiveEmptyFrames)
				{
					int silenceCount = (int)Math.Round(AdvertisedSamplesPerFrame);
					samples = Resample(samples, count, silenceCount);
					count = silenceCount;
					correctedEmptyFrame = true;
				}
			}
			else if (_baseConsecutiveEmptyFrames != 0)
			{
				_baseConsecutiveEmptyFrames = 0;
			}

			UpdateHistory(_baseEmptyFrameCorrectionHistory, correctedEmptyFrame, MaxHistoryLength);

			if (AdvertisedSamplesPerFrame != _lastAdvertisedSamplesPerFrame)
			{
				_baseSamplesPerFrame.Clear();
				_lastAdvertisedSamplesPerFrame = AdvertisedSamplesPerFrame;
			}

			UpdateHistory(_baseSamplesPerFrame, count, BaseSampleRateMaxHistoryLength);

			if (_baseSamplesPerFrame.Count >= BaseSampleRateUsableHistoryLength
				&& !_baseEmptyFrameCorrectionHistory.Contains(true))
			{
				double baseAverageSamplesPerFrame = _baseSamplesPerFrame.Average();
				if (baseAverageSamplesPerFrame != 0.0)
				{
					scaleFactor *= AdvertisedSamplesPerFrame / baseAverageSamplesPerFrame;
				}
			}

			double newCountExact = (count * scaleFactor) + _resampleLengthRoundingError;
			int newCount = (int)Math.Round(newCountExact);
			// Due to small inaccuracies and rounding errors, it's pointless to resample by
			// just a sample or two because those may be fluctuations that will average out
			// over time. So instead of immediately resampling to cover small differences, we
			// will just keep track of it as part of the rounding error and only resample later
			// if a more significant difference accumulates.
			if (Math.Abs(newCount - count) >= MinResamplingDistanceSamples)
			{
				samples = Resample(samples, count, newCount);
				count = newCount;
			}
			// Although the rounding error may seem insignificant, it definitely matters over
			// time so we need to keep track of it. With NTSC @ 59.94 FPS, for example, if we
			// were to always round to 736 samples per frame ignoring the rounding error, we
			// would drift by ~22 milliseconds per minute.
			_resampleLengthRoundingError = newCountExact - count;

			AddSamplesToBuffer(samples, count);
		}

		private static double CalculatePowerMean(IEnumerable<int> values, double power)
		{
			double x = values.Average(n => Math.Pow(Math.Abs(n), power) * Math.Sign(n));
			return Math.Pow(Math.Abs(x), 1.0 / power) * Math.Sign(x);
		}

		private static void UpdateHistory<T>(Queue<T> queue, T value, int maxLength)
		{
			queue.Enqueue(value);
			while (queue.Count > maxLength)
			{
				queue.Dequeue();
			}
		}

		private void GetSamplesFromBuffer(short[] samples, int count)
		{
			_buffer.CopyTo(0, samples, 0, count * ChannelCount);
			_buffer.RemoveRange(0, count * ChannelCount);
		}

		private void AddSamplesToBuffer(short[] samples, int count)
		{
			_buffer.AddRange(new ArraySegment<short>(samples, 0, count * ChannelCount));
		}

		private short[] GetOutputBuffer(int count)
		{
			if (_outputBuffer.Length < count * ChannelCount)
			{
				_outputBuffer = new short[count * ChannelCount];
			}

			return _outputBuffer;
		}

		private short[] GetResampleBuffer(int count)
		{
			if (_resampleBuffer.Length < count * ChannelCount)
			{
				_resampleBuffer = new short[count * ChannelCount];
			}

			return _resampleBuffer;
		}

		// This uses simple linear interpolation which is supposedly not a great idea for
		// resampling audio, but it sounds surprisingly good to me. Maybe it works well
		// because we are typically stretching by very small amounts.
		private short[] Resample(short[] input, int inputCount, int outputCount)
		{
			if (inputCount == outputCount)
			{
				return input;
			}

			short[] output = GetResampleBuffer(outputCount);

			if (inputCount == 0 || outputCount == 0)
			{
				Array.Clear(output, 0, outputCount * ChannelCount);
				return output;
			}

			for (int iOutput = 0; iOutput < outputCount; iOutput++)
			{
				double iInput = ((double)iOutput / (outputCount - 1)) * (inputCount - 1);
				int iInput0 = (int)iInput;
				int iInput1 = iInput0 + 1;
				double input0Weight = iInput1 - iInput;
				double input1Weight = iInput - iInput0;

				if (iInput1 == inputCount)
					iInput1 = inputCount - 1;

				for (int iChannel = 0; iChannel < ChannelCount; iChannel++)
				{
					double value =
						input[iInput0 * ChannelCount + iChannel] * input0Weight +
						input[iInput1 * ChannelCount + iChannel] * input1Weight;

					output[iOutput * ChannelCount + iChannel] = (short)((int)(value + 32768.5) - 32768);
				}
			}

			return output;
		}
	}
}
