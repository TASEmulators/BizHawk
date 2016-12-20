#if WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;

using SlimDX;
using SlimDX.Multimedia;
using SlimDX.XAudio2;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class XAudio2SoundOutput : ISoundOutput
	{
		private bool _disposed;
		private Sound _sound;
		private XAudio2 _device;
		private MasteringVoice _masteringVoice;
		private SourceVoice _sourceVoice;
		private BufferPool _bufferPool;
		private long _runningSamplesQueued;

		public XAudio2SoundOutput(Sound sound)
		{
			_sound = sound;
			_device = new XAudio2();
			int? deviceIndex = Enumerable.Range(0, _device.DeviceCount)
				.Select(n => (int?)n)
				.FirstOrDefault(n => _device.GetDeviceDetails(n.Value).DisplayName == Global.Config.SoundDevice);
			_masteringVoice = deviceIndex == null ?
				new MasteringVoice(_device, Sound.ChannelCount, Sound.SampleRate) :
				new MasteringVoice(_device, Sound.ChannelCount, Sound.SampleRate, deviceIndex.Value);
		}

		public void Dispose()
		{
			if (_disposed) return;

			_masteringVoice.Dispose();
			_masteringVoice = null;

			_device.Dispose();
			_device = null;

			_disposed = true;
		}

		public static IEnumerable<string> GetDeviceNames()
		{
			using (XAudio2 device = new XAudio2())
			{
				return Enumerable.Range(0, device.DeviceCount).Select(n => device.GetDeviceDetails(n).DisplayName).ToList();
			}
		}

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			_sourceVoice.Volume = (float)volume;
		}

		public void StartSound()
		{
			BufferSizeSamples = Sound.MillisecondsToSamples(Global.Config.SoundBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			var format = new WaveFormat
				{
					SamplesPerSecond = Sound.SampleRate,
					BitsPerSample = Sound.BytesPerSample * 8,
					Channels = Sound.ChannelCount,
					FormatTag = WaveFormatTag.Pcm,
					BlockAlignment = Sound.BlockAlign,
					AverageBytesPerSecond = Sound.SampleRate * Sound.BlockAlign
				};

			_sourceVoice = new SourceVoice(_device, format);

			_bufferPool = new BufferPool();
			_runningSamplesQueued = 0;

			_sourceVoice.Start();
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
			bool isInitializing = _runningSamplesQueued == 0;
			bool detectedUnderrun = !isInitializing && _sourceVoice.State.BuffersQueued == 0;
			long samplesAwaitingPlayback = _runningSamplesQueued - _sourceVoice.State.SamplesPlayed;
			int samplesNeeded = (int)Math.Max(BufferSizeSamples - samplesAwaitingPlayback, 0);
			if (isInitializing || detectedUnderrun)
			{
				_sound.HandleInitializationOrUnderrun(detectedUnderrun, ref samplesNeeded);
			}
			return samplesNeeded;
		}

		public void WriteSamples(short[] samples, int sampleCount)
		{
			if (sampleCount == 0) return;
			_bufferPool.Release(_sourceVoice.State.BuffersQueued);
			int byteCount = sampleCount * Sound.BlockAlign;
			var buffer = _bufferPool.Obtain(byteCount);
			Buffer.BlockCopy(samples, 0, buffer.Bytes, 0, byteCount);
			_sourceVoice.SubmitSourceBuffer(new AudioBuffer
				{
					AudioBytes = byteCount,
					AudioData = buffer.DataStream
				});
			_runningSamplesQueued += sampleCount;
		}

		private class BufferPool : IDisposable
		{
			private List<BufferPoolItem> _availableItems = new List<BufferPoolItem>();
			private Queue<BufferPoolItem> _obtainedItems = new Queue<BufferPoolItem>();

			public void Dispose()
			{
				foreach (BufferPoolItem item in _availableItems.Concat(_obtainedItems))
				{
					item.DataStream.Dispose();
				}
				_availableItems.Clear();
				_obtainedItems.Clear();
			}

			public BufferPoolItem Obtain(int length)
			{
				BufferPoolItem item = GetAvailableItem(length) ?? new BufferPoolItem(length);
				_obtainedItems.Enqueue(item);
				return item;
			}

			private BufferPoolItem GetAvailableItem(int length)
			{
				int foundIndex = -1;
				for (int i = 0; i < _availableItems.Count; i++)
				{
					if (_availableItems[i].MaxLength >= length && (foundIndex == -1 || _availableItems[i].MaxLength < _availableItems[foundIndex].MaxLength))
						foundIndex = i;
				}
				if (foundIndex == -1) return null;
				BufferPoolItem item = _availableItems[foundIndex];
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
				public int MaxLength { get; private set; }
				public byte[] Bytes { get; private set; }
				public DataStream DataStream { get; private set; }

				public BufferPoolItem(int length)
				{
					MaxLength = length;
					Bytes = new byte[MaxLength];
					DataStream = new DataStream(Bytes, true, false);
				}
			}
		}
	}
}
#endif
