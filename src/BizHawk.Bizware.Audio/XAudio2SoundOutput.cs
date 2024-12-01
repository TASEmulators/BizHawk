using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;

using Vortice.MediaFoundation;
using Vortice.Multimedia;
using Vortice.XAudio2;

namespace BizHawk.Bizware.Audio
{
	public sealed class XAudio2SoundOutput : ISoundOutput
	{
		private bool _disposed;
		private readonly IHostAudioManager _sound;
		private volatile bool _deviceResetRequired;
		private IXAudio2 _device;
		private IXAudio2MasteringVoice _masteringVoice;
		private IXAudio2SourceVoice _sourceVoice, _wavVoice;
		private BufferPool _bufferPool;
		private AudioBuffer _wavBuffer;
		private long _runningSamplesQueued;

		private static string GetDeviceId(string deviceName)
		{
			if (string.IsNullOrEmpty(deviceName))
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
			// this is for fatal errors which require resetting to the default audio device
			// note that this won't be called on the main thread, so we'll defer the reset to the main thread
			_device.CriticalError += (_, _) => _deviceResetRequired = true;
			_masteringVoice = _device.CreateMasteringVoice(
				inputChannels: _sound.ChannelCount,
				inputSampleRate: _sound.SampleRate,
				deviceId: GetDeviceId(chosenDeviceName));
		}

		public void Dispose()
		{
			if (_disposed) return;

			StopWav();
			_masteringVoice.Dispose();
			_device.Dispose();

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

		private void ResetToDefaultDeviceIfNeeded()
		{
			if (_deviceResetRequired)
			{
				StopSound();
				StopWav();
				_masteringVoice.Dispose();
				_device.Dispose();

				_deviceResetRequired = false;
				_device = XAudio2.XAudio2Create();
				_device.CriticalError += (_, _) => _deviceResetRequired = true;
				_masteringVoice = _device.CreateMasteringVoice(
					inputChannels: _sound.ChannelCount,
					inputSampleRate: _sound.SampleRate);

				StartSound();
			}
		}

		public int CalculateSamplesNeeded()
		{
			ResetToDefaultDeviceIfNeeded();
			var isInitializing = _runningSamplesQueued == 0;
			var voiceState = _sourceVoice.State;
			var detectedUnderrun = !isInitializing && voiceState.BuffersQueued == 0;
			var samplesAwaitingPlayback = _runningSamplesQueued - (long)voiceState.SamplesPlayed;
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
			samples.AsSpan(sampleOffset * _sound.BlockAlign / 2, byteCount / 2)
				.CopyTo(item.AudioBuffer.AsSpan<short>());
			item.AudioBuffer.AudioBytes = byteCount;
			_sourceVoice.SubmitSourceBuffer(item.AudioBuffer);
			_runningSamplesQueued += sampleCount;
		}

		private void StopWav()
		{
			_wavVoice?.Stop();
			_wavVoice?.Dispose();
			_wavVoice = null;

			_wavBuffer?.Dispose();
			_wavBuffer = null;
		}

		public void PlayWavFile(Stream wavFile, double volume)
		{
			using var wavStream = new SDL2WavStream(wavFile);
			var format = wavStream.Format == SDL2WavStream.AudioFormat.F32LSB
				? WaveFormat.CreateIeeeFloatWaveFormat(wavStream.Frequency, wavStream.Channels)
				: new(wavStream.Frequency, wavStream.BitsPerSample, wavStream.Channels);

			StopWav();
			_wavVoice = _device.CreateSourceVoice(format);
			_wavBuffer = new(unchecked((int)wavStream.Length));
			var bufSpan = _wavBuffer.AsSpan();
			var bytesRead = wavStream.Read(bufSpan);
			Debug.Assert(bytesRead == bufSpan.Length, "reached end-of-file while reading .wav");
			if (wavStream.Format == SDL2WavStream.AudioFormat.S16MSB)
			{
				EndiannessUtils.MutatingByteSwap16(bufSpan);
			}

			_wavVoice.SubmitSourceBuffer(_wavBuffer);
			_wavVoice.Volume = (float)volume;
			_wavVoice.Start();
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
