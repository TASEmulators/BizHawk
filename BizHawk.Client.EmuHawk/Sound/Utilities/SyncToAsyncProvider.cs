using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class SyncToAsyncProvider : ISoundProvider
	{
		private const int SampleRate = 44100;
		private const int ChannelCount = 2;
		private const double MinExpectedFrameRate = 50;

		private readonly SoundOutputProvider _outputProvider = new SoundOutputProvider();
		private readonly int _bufferSizeSamples;
		private readonly Queue<short> _buffer;

		public SyncToAsyncProvider(ISoundProvider baseProvider, double bufferSizeMs)
		{
			_outputProvider.BaseSoundProvider = baseProvider;
			_bufferSizeSamples = (int)Math.Ceiling(bufferSizeMs * SampleRate / 1000.0);
			_buffer = new Queue<short>((_bufferSizeSamples + (int)Math.Ceiling(SampleRate / MinExpectedFrameRate)) * ChannelCount);

			DiscardSamples();
		}

		public void DiscardSamples()
		{
			_buffer.Clear();
			_outputProvider.DiscardSamples();

			for (int i = 0; i < _bufferSizeSamples * ChannelCount; i++)
			{
				_buffer.Enqueue(0);
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			GetSamplesFromBase(samples.Length / ChannelCount);
			for (int i = 0; i < samples.Length; i++)
			{
				samples[i] = _buffer.Dequeue();
			}
		}

		public bool CanProvideAsync
		{
			get { return true; }
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Async; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Async)
			{
				throw new NotSupportedException("Sync mode is not supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			throw new InvalidOperationException("Sync mode is not supported.");
		}

		private void GetSamplesFromBase(int minResultingSampleCount)
		{
			int idealSampleCount = Math.Max(_bufferSizeSamples + minResultingSampleCount - (_buffer.Count / ChannelCount), 0);
			short[] samples;
			int samplesProvided;
			_outputProvider.MaxSamplesDeficit = _bufferSizeSamples;
			_outputProvider.GetSamples(idealSampleCount, out samples, out samplesProvided);
			for (int i = 0; i < samplesProvided * ChannelCount; i++)
			{
				_buffer.Enqueue(samples[i]);
			}
		}
	}
}
