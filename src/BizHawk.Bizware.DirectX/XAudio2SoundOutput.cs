using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.Client.Common;

using Vortice.XAudio2;
using Vortice.MediaFoundation;
using Vortice.Multimedia;

namespace BizHawk.Bizware.DirectX
{
	public sealed class XAudio2SoundOutput : ISoundOutput
	{
		private bool _disposed;
		private readonly IHostAudioManager _sound;
		private IXAudio2 _device;
		private IXAudio2MasteringVoice _masteringVoice;
		private IXAudio2SourceVoice _sourceVoice;
		private BufferPool _bufferPool;
		private long _runningSamplesQueued;

		private static string GetDeviceId(string deviceName)
		{
			if (deviceName is null)
			{
				return null;
			}

			using var enumerator = new IMMDeviceEnumerator();
			var devices = enumerator.EnumAudioEndpoints(DataFlow.Render);
			var device = devices.FirstOrDefault(capDevice => capDevice.FriendlyName == deviceName);
			if (device is null)
			{
				return null;
			}

			const string MMDEVAPI_TOKEN = @"\\?\SWD#MMDEVAPI#";
			const string DEVINTERFACE_AUDIO_RENDER = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";
			return $"{MMDEVAPI_TOKEN}{device.Id}{DEVINTERFACE_AUDIO_RENDER}";
		}

		public XAudio2SoundOutput(IHostAudioManager sound, string chosenDeviceName)
		{
			_sound = sound;
			_device = XAudio2.XAudio2Create();
			_masteringVoice = _device.CreateMasteringVoice(
				inputChannels: _sound.ChannelCount, 
				inputSampleRate: _sound.SampleRate,
				deviceId: GetDeviceId(chosenDeviceName));
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
			using var enumerator = new IMMDeviceEnumerator();
			var devices = enumerator.EnumAudioEndpoints(DataFlow.Render);
			return devices.Select(capDevice => capDevice.FriendlyName);
		}

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

			var format = new WaveFormat(_sound.SampleRate, _sound.BytesPerSample * 8, _sound.ChannelCount);
			_sourceVoice = _device.CreateSourceVoice(format);

			_bufferPool = new();
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
			var isInitializing = _runningSamplesQueued == 0;
			var detectedUnderrun = !isInitializing && _sourceVoice.State.BuffersQueued == 0;
			var samplesAwaitingPlayback = _runningSamplesQueued - (long)_sourceVoice.State.SamplesPlayed;
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
			_bufferPool.Release(_sourceVoice.State.BuffersQueued);
			var byteCount = sampleCount * _sound.BlockAlign;
			var item = _bufferPool.Obtain(byteCount);
			MemoryMarshal.AsBytes(samples.AsSpan())
				.Slice(sampleOffset * _sound.BlockAlign, byteCount)
				.CopyTo(item.AudioBuffer.AsSpan());
			item.AudioBuffer.AudioBytes = byteCount;
			_sourceVoice.SubmitSourceBuffer(item.AudioBuffer);
			_runningSamplesQueued += sampleCount;
		}

		private class BufferPool : IDisposable
		{
			private readonly List<BufferPoolItem> _availableItems = new();
			private readonly Queue<BufferPoolItem> _obtainedItems = new();

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
				var item = GetAvailableItem(length) ?? new BufferPoolItem(length);
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
				item.AudioBuffer.AudioBytes = item.MaxLength; // this might have shrunk from earlier use, set it back to MaxLength so AsSpan() works as expected
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
				public AudioBuffer AudioBuffer { get; }

				public BufferPoolItem(int length)
				{
					MaxLength = length;
					AudioBuffer = new(length, BufferFlags.None);
				}
			}
		}
	}
}
