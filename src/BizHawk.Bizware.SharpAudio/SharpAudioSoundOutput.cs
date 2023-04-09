using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.Client.Common;

using SharpAudio;

namespace BizHawk.Bizware.SharpAudio
{
	public abstract class SharpAudioSoundOutput : ISoundOutput
	{
		private bool _disposed;
		private readonly IHostAudioManager _sound;
		private AudioEngine _engine;
		private AudioSource _sourceVoice;
		private BufferPool _bufferPool;
		private long _runningSamplesQueued;

		protected SharpAudioSoundOutput(AudioBackend backend, IHostAudioManager sound, string chosenDeviceName)
		{
			_sound = sound;
			var options = new AudioEngineOptions { SampleRate = _sound.SampleRate, SampleChannels = _sound.ChannelCount, DeviceName = chosenDeviceName };
			_engine = backend switch
			{
				AudioBackend.XAudio2 => AudioEngine.CreateXAudio(options),
				AudioBackend.OpenAL => AudioEngine.CreateOpenAL(options),
				_ => null,
			};
			if (_engine is null) throw new InvalidOperationException();
		}

		public void Dispose()
		{
			if (_disposed) return;

			_engine.Dispose();
			_engine = null;

			_disposed = true;
		}

		protected static IEnumerable<string> GetDeviceNames(AudioBackend backend) => AudioEngine.GetDeviceNames(backend);

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			_sourceVoice.Volume = (float)volume;
		}

		public void StartSound()
		{
			BufferSizeSamples = _sound.MillisecondsToSamples(_sound.ConfigBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			_sourceVoice = _engine.CreateSource();

			_bufferPool = new(_engine, new()
			{
				SampleRate = _sound.SampleRate,
				BitsPerSample = (short) (_sound.BytesPerSample * 8),
				Channels = (short) _sound.ChannelCount,
			});
			_runningSamplesQueued = 0;

			_sourceVoice.Play();
		}

		public void StopSound()
		{
			_sourceVoice.Stop();
			_sourceVoice.Dispose();
			_sourceVoice = null;

			_bufferPool.Dispose();
			_bufferPool = null;

			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			var isInitializing = _runningSamplesQueued == 0;
			var detectedUnderrun = !isInitializing && _sourceVoice.BuffersQueued == 0;
			var samplesAwaitingPlayback = _runningSamplesQueued - _sourceVoice.SamplesPlayed;
			var samplesNeeded = (int)Math.Max(BufferSizeSamples - samplesAwaitingPlayback, 0);
			if (isInitializing || detectedUnderrun)
			{
				_sound.HandleInitializationOrUnderrun(detectedUnderrun, ref samplesNeeded);
			}
			return samplesNeeded;
		}

		public void WriteSamples(short[] samples, int sampleOffset, int sampleCount)
		{
			if (sampleCount == 0) return;
			_bufferPool.Release(_sourceVoice.BuffersQueued);
			var byteCount = sampleCount * _sound.BlockAlign;
			var item = _bufferPool.Obtain(byteCount);
			MemoryMarshal.AsBytes(samples.AsSpan()).Slice(sampleOffset * _sound.BlockAlign, byteCount).CopyTo(item.Bytes);
			_sourceVoice.QueueBuffer(item.AudioBuffer);
			_runningSamplesQueued += sampleCount;
		}

		private class BufferPool : IDisposable
		{
			private readonly List<BufferPoolItem> _availableItems = new();
			private readonly Queue<BufferPoolItem> _obtainedItems = new();

			private readonly AudioEngine _engine;
			private readonly AudioFormat _format;

			public BufferPool(AudioEngine engine, AudioFormat format)
			{
				_engine = engine;
				_format = format;
			}

			public void Dispose()
			{
				foreach (var item in _availableItems.Concat(_obtainedItems))
				{
					item.AudioBuffer.Dispose();
				}
				_availableItems.Clear();
				_obtainedItems.Clear();
			}

			public BufferPoolItem Obtain(int length)
			{
				var item = GetAvailableItem(length) ?? new(_engine, _format, length);
				_obtainedItems.Enqueue(item);
				return item;
			}

			private BufferPoolItem GetAvailableItem(int length)
			{
				var foundIndex = -1;
				for (var i = 0; i < _availableItems.Count; i++)
				{
					if (_availableItems[i].MaxLength >= length && (foundIndex == -1 || _availableItems[i].MaxLength < _availableItems[foundIndex].MaxLength))
						foundIndex = i;
				}
				if (foundIndex == -1) return null;
				var item = _availableItems[foundIndex];
				_availableItems.RemoveAt(foundIndex);
				return item;
			}

			public void Release(int buffersQueued)
			{
				while (_obtainedItems.Count > buffersQueued)
					_availableItems.Add(_obtainedItems.Dequeue());
			}

			public class BufferPoolItem
			{
				public int MaxLength { get; }
				public byte[] Bytes { get; }
				public AudioBuffer AudioBuffer { get; }

				public BufferPoolItem(AudioEngine engine, AudioFormat format, int length)
				{
					MaxLength = length;
					Bytes = new byte[MaxLength];
					AudioBuffer = engine.CreateBuffer();
					AudioBuffer.BufferData(Bytes, format);
				}
			}
		}
	}
}
