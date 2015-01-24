using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	// This is intended to be a buffer between a synchronous sound provider and the
	// output device (e.g. DirectSound). The idea is to take advantage of the samples
	// buffered up in the output device so that we don't need to keep a bunch buffered
	// up here. This will keep the latency at a minimum. The goal is to keep zero extra
	// samples here on average. As long as we're within +/-5 milliseconds we don't need
	// to touch the source audio. Once it goes outside of that window, we'll start to
	// perform a "soft" correction by resampling it to hopefully get back inside our
	// window shortly. If it ends up going too low (-40 ms) and depleting the output
	// device's buffer, or too high (+40 ms), we will perform a "hard" correction by
	// generating silence or discarding samples.
	public class SoundOutputProvider
	{
		private const int SampleRate = 44100;
		private const int ChannelCount = 2;
		private const int MaxExtraMilliseconds = 45;
		private const int MaxExtraSamples = SampleRate * MaxExtraMilliseconds / 1000;
		private const int MaxTargetOffsetMilliseconds = 5;
		private const int MaxTargetOffsetSamples = SampleRate * MaxTargetOffsetMilliseconds / 1000;
		private const int HardCorrectionMilliseconds = 20;
		private const int HardCorrectionSamples = SampleRate * HardCorrectionMilliseconds / 1000;
		private const int UsableHistoryLength = 20;
		private const int MaxHistoryLength = 60;
		private const int SoftCorrectionLength = 240;
		private const int BaseSampleRateUsableHistoryLength = 60;
		private const int BaseSampleRateMaxHistoryLength = 300;
		private const int MinResamplingDistanceSamples = 3;

		private Queue<short> _buffer = new Queue<short>(MaxExtraSamples * ChannelCount);

		private Queue<int> _extraCountHistory = new Queue<int>();
		private Queue<int> _outputCountHistory = new Queue<int>();
		private Queue<bool> _hardCorrectionHistory = new Queue<bool>();

		private double _lastAdvertisedSamplesPerFrame;
		private Queue<int> _baseSamplesPerFrame = new Queue<int>();

		private short[] _resampleBuffer = new short[0];
		private double _resampleLengthRoundingError;

		public ISyncSoundProvider BaseSoundProvider { get; set; }

		public SoundOutputProvider()
		{
		}

		public void DiscardSamples()
		{
			_buffer.Clear();
			_extraCountHistory.Clear();
			_outputCountHistory.Clear();
			_hardCorrectionHistory.Clear();
			_lastAdvertisedSamplesPerFrame = 0.0;
			_baseSamplesPerFrame.Clear();
			_resampleBuffer = new short[0];
			_resampleLengthRoundingError = 0.0;

			if (BaseSoundProvider != null)
			{
				BaseSoundProvider.DiscardSamples();
			}
		}

		public bool LogDebug { get; set; }

		private double AdvertisedSamplesPerFrame
		{
			get { return SampleRate / Global.Emulator.CoreComm.VsyncRate; }
		}

		public int GetSamples(short[] samples, int idealSampleCount, int minSampleCount)
		{
			double scaleFactor = 1.0;

			if (_extraCountHistory.Count >= UsableHistoryLength && !_hardCorrectionHistory.Any(c => c))
			{
				double offsetFromTarget = _extraCountHistory.Average();
				if (Math.Abs(offsetFromTarget) > MaxTargetOffsetSamples)
				{
					double correctionSpan = _outputCountHistory.Average() * SoftCorrectionLength;
					scaleFactor *= correctionSpan / (correctionSpan + offsetFromTarget);
				}
			}

			GetSamplesFromBase(ref scaleFactor);

			int bufferSampleCount = _buffer.Count / ChannelCount;
			int extraSampleCount = bufferSampleCount - idealSampleCount;
			bool hardCorrected = false;

			if (bufferSampleCount < minSampleCount)
			{
				int generateSampleCount = (minSampleCount - bufferSampleCount) + HardCorrectionSamples;
				if (LogDebug) Console.WriteLine("Generating " + generateSampleCount + " samples");
				for (int i = 0; i < generateSampleCount * ChannelCount; i++)
				{
					_buffer.Enqueue(0);
				}
				hardCorrected = true;
			}
			else if (extraSampleCount > MaxExtraSamples)
			{
				int discardSampleCount = (extraSampleCount - MaxExtraSamples) + HardCorrectionSamples;
				if (LogDebug) Console.WriteLine("Discarding " + discardSampleCount + " samples");
				for (int i = 0; i < discardSampleCount * ChannelCount; i++)
				{
					_buffer.Dequeue();
				}
				hardCorrected = true;
			}

			bufferSampleCount = _buffer.Count / ChannelCount;
			extraSampleCount = bufferSampleCount - idealSampleCount;

			int outputSampleCount = Math.Min(idealSampleCount, bufferSampleCount);

			UpdateHistory(_extraCountHistory, extraSampleCount, MaxHistoryLength);
			UpdateHistory(_outputCountHistory, outputSampleCount, MaxHistoryLength);
			UpdateHistory(_hardCorrectionHistory, hardCorrected, MaxHistoryLength);

			GetSamplesFromBuffer(samples, outputSampleCount);

			if (LogDebug)
			{
				Console.WriteLine("Avg: {0:0.0} ms, Min: {1:0.0} ms, Max: {2:0.0} ms, Scale: {3:0.0000}",
					_extraCountHistory.Average() * 1000.0 / SampleRate,
					_extraCountHistory.Min() * 1000.0 / SampleRate,
					_extraCountHistory.Max() * 1000.0 / SampleRate,
					scaleFactor);
			}

			return outputSampleCount;
		}

		private void GetSamplesFromBase(ref double scaleFactor)
		{
			short[] samples;
			int count;

			BaseSoundProvider.GetSamples(out samples, out count);

			if (AdvertisedSamplesPerFrame != _lastAdvertisedSamplesPerFrame)
			{
				_baseSamplesPerFrame.Clear();
				_lastAdvertisedSamplesPerFrame = AdvertisedSamplesPerFrame;
			}
			if (count != 0)
			{
				UpdateHistory(_baseSamplesPerFrame, count, BaseSampleRateMaxHistoryLength);
			}

			if (_baseSamplesPerFrame.Count >= BaseSampleRateUsableHistoryLength)
			{
				double baseAverageSamplesPerFrame = _baseSamplesPerFrame.Average();
				if (baseAverageSamplesPerFrame != 0.0)
				{
					scaleFactor *= AdvertisedSamplesPerFrame / baseAverageSamplesPerFrame;
				}
			}

			double newCountTarget = count * scaleFactor;
			int newCount = (int)Math.Round(newCountTarget + _resampleLengthRoundingError);
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
			_resampleLengthRoundingError += newCountTarget - count;

			AddSamplesToBuffer(samples, count);
		}

		private void UpdateHistory<T>(Queue<T> queue, T value, int maxLength)
		{
			queue.Enqueue(value);
			while (queue.Count > maxLength)
			{
				queue.Dequeue();
			}
		}

		private void GetSamplesFromBuffer(short[] samples, int count)
		{
			for (int i = 0; i < count * ChannelCount; i++)
			{
				samples[i] = _buffer.Dequeue();
			}
		}

		private void AddSamplesToBuffer(short[] samples, int count)
		{
			for (int i = 0; i < count * ChannelCount; i++)
			{
				_buffer.Enqueue(samples[i]);
			}
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
					output[iOutput * ChannelCount + iChannel] = (short)
						(input[iInput0 * ChannelCount + iChannel] * input0Weight +
						 input[iInput1 * ChannelCount + iChannel] * input1Weight);
				}
			}

			return output;
		}
	}
}
