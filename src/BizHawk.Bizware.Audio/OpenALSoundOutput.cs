using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;

using Silk.NET.Core.Native;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Creative;
using Silk.NET.OpenAL.Extensions.Enumeration;
using Silk.NET.OpenAL.Extensions.EXT;

namespace BizHawk.Bizware.Audio
{
	public class OpenALSoundOutput : ISoundOutput
	{
		private static readonly AL _al = AL.GetApi();
		private static readonly ALContext _alc = ALContext.GetApi();

		private static unsafe T GetExtensionOrNull<T>() where T : NativeExtension<ALContext>
			=> _alc.TryGetExtension<T>(null, out var ext) ? ext : null;

		private static readonly EnumerateAll _enumAllExt = GetExtensionOrNull<EnumerateAll>();
		private static readonly Enumeration _enumExt = GetExtensionOrNull<Enumeration>();

		private bool _disposed;
		private readonly IHostAudioManager _sound;
		private AudioContext _context;
		private uint _sourceID, _wavSourceID;
		private BufferPool _bufferPool;
		private uint _wavBufferID;
		private int _currentSamplesQueued;
		private short[] _tempSampleBuffer;
		private unsafe Device* _device;
		private Disconnect _disconnectExt;
		private FloatFormat _floatExt;

		public OpenALSoundOutput(IHostAudioManager sound, string chosenDeviceName)
		{
			_sound = sound;
			_context = new(
				GetDeviceNames().Contains(chosenDeviceName) ? chosenDeviceName : null,
				_sound.SampleRate
			);

			unsafe
			{
				_device = _alc.GetContextsDevice(_alc.GetCurrentContext());
				_disconnectExt = _alc.TryGetExtension<Disconnect>(_device, out var disconnectExt) ? disconnectExt : null;
			}

			_floatExt = _al.TryGetExtension<FloatFormat>(out var floatFormatExt) ? floatFormatExt : null;
		}

		public void Dispose()
		{
			if (_disposed) return;

			StopWav();

			_context.Dispose();
			_context = null;

			_disposed = true;
		}

		public static IEnumerable<string> GetDeviceNames()
			=> _enumAllExt?.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier)
				?? _enumExt?.GetStringList(GetEnumerationContextStringList.DeviceSpecifiers)
				?? Enumerable.Empty<string>();

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			_al.SetSourceProperty(_sourceID, SourceFloat.Gain, (float)volume);
		}

		public void StartSound()
		{
			BufferSizeSamples = _sound.MillisecondsToSamples(_sound.ConfigBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			_sourceID = _al.GenSource();

			_bufferPool = new();
			_currentSamplesQueued = 0;
		}

		public void StopSound()
		{
			_al.SourceStop(_sourceID);
			_al.DeleteSource(_sourceID);

			_bufferPool.Dispose();
			_bufferPool = null;

			BufferSizeSamples = 0;
		}

		private unsafe void ResetToDefaultDeviceIfDisconnected()
		{
			var connected = 1;
			_disconnectExt?.GetContextProperty(_device, DisconnectContextInteger.Connected, 1, &connected);
			if (connected != 0)
			{
				return;
			}

			StopSound();
			StopWav();
			_context.Dispose();

			_context = new(device: null, _sound.SampleRate);
			_device = _alc.GetContextsDevice(_alc.GetCurrentContext());
			_disconnectExt = _alc.TryGetExtension<Disconnect>(_device, out var ext) ? ext : null;
			_floatExt = _al.TryGetExtension<FloatFormat>(out var floatFormatExt) ? floatFormatExt : null;

			StartSound();
		}

		public int CalculateSamplesNeeded()
		{
			ResetToDefaultDeviceIfDisconnected();
			var currentSamplesPlayed = GetSource(GetSourceInteger.SampleOffset);
			var sourceState = GetSourceState();
			var isInitializing = sourceState == SourceState.Initial;
			var detectedUnderrun = sourceState == SourceState.Stopped;
			if (detectedUnderrun)
			{
				// SampleOffset should reset to 0 when stopped; update the queued sample count to match
				UnqueueProcessedBuffers();
				currentSamplesPlayed = 0;
			}
			var samplesAwaitingPlayback = _currentSamplesQueued - currentSamplesPlayed;
			var samplesNeeded = Math.Max(BufferSizeSamples - samplesAwaitingPlayback, 0);
			if (isInitializing || detectedUnderrun)
			{
				_sound.HandleInitializationOrUnderrun(detectedUnderrun, ref samplesNeeded);
			}
			return samplesNeeded;
		}

		public unsafe void WriteSamples(short[] samples, int sampleOffset, int sampleCount)
		{
			if (sampleCount == 0) return;
			UnqueueProcessedBuffers();
			var byteCount = sampleCount * _sound.BlockAlign;
			if (sampleOffset != 0)
			{
				AllocateTempSampleBuffer(sampleCount);
				samples.AsSpan(sampleOffset * _sound.BlockAlign / 2, byteCount / 2)
					.CopyTo(_tempSampleBuffer);
				samples = _tempSampleBuffer;
			}
			var buffer = _bufferPool.Obtain(byteCount);
			fixed (short* sptr = samples)
			{
				_al.BufferData(buffer.BufferID, BufferFormat.Stereo16, sptr, byteCount, _sound.SampleRate);
			}
			var bid = buffer.BufferID;
			_al.SourceQueueBuffers(_sourceID, 1, &bid);
			_currentSamplesQueued += sampleCount;
			if (GetSourceState() != SourceState.Playing)
			{
				_al.SourcePlay(_sourceID);
			}
		}

		private void StopWav()
		{
			if (_wavSourceID != 0)
			{
				_al.SourceStop(_wavSourceID);
				_al.DeleteSource(_wavSourceID);
				_wavSourceID = 0;
			}

			if (_wavBufferID != 0)
			{
				_al.DeleteBuffer(_wavBufferID);
				_wavBufferID = 0;
			}
		}

		public void PlayWavFile(Stream wavFile, double volume)
		{
			using var wavStream = new SDL2WavStream(wavFile);
			if (wavStream.Channels > 2)
			{
				throw new NotSupportedException("OpenAL does not support more than 2 channels");
			}

			var format = wavStream.Format switch
			{
				SDL2WavStream.AudioFormat.U8 => wavStream.Channels == 1 ? BufferFormat.Mono8 : BufferFormat.Stereo8,
				SDL2WavStream.AudioFormat.S16LSB or SDL2WavStream.AudioFormat.S16MSB => wavStream.Channels == 1 ? BufferFormat.Mono16 : BufferFormat.Stereo16,
				SDL2WavStream.AudioFormat.S32LSB => throw new NotSupportedException("OpenAL does not support s32 samples"),
				SDL2WavStream.AudioFormat.F32LSB when _floatExt == null => throw new NotSupportedException("This OpenAL implementation does not support f32 samples"),
				SDL2WavStream.AudioFormat.F32LSB => (BufferFormat)(wavStream.Channels == 1 ? FloatBufferFormat.Mono : FloatBufferFormat.Stereo),
				_ => throw new InvalidOperationException(),
			};

			StopWav();
			_wavSourceID = _al.GenSource();
			_wavBufferID = _al.GenBuffer();

			var tempBuffer = wavStream.ReadAllBytes();
			if (wavStream.Format == SDL2WavStream.AudioFormat.S16MSB)
			{
				EndiannessUtils.MutatingByteSwap16(tempBuffer);
			}

			_al.BufferData(_wavBufferID, format, tempBuffer, wavStream.Frequency);
			_al.SetSourceProperty(_wavSourceID, SourceFloat.Gain, (float)volume);
			unsafe
			{
				var bid = _wavBufferID;
				_al.SourceQueueBuffers(_wavSourceID, 1, &bid);
			}
			_al.SourcePlay(_wavSourceID);
		}

		private unsafe void UnqueueProcessedBuffers()
		{
			var releaseCount = GetSource(GetSourceInteger.BuffersProcessed);
			var bids = stackalloc uint[releaseCount];
			_al.SourceUnqueueBuffers(_sourceID, releaseCount, bids);
			for (var i = 0; i < releaseCount; i++)
			{
				var releasedBuffer = _bufferPool.ReleaseOne();
				_currentSamplesQueued -= releasedBuffer.Length / _sound.BlockAlign;
			}
		}

		private int GetSource(GetSourceInteger param)
		{
			_al.GetSourceProperty(_sourceID, param, out var value);
			return value;
		}

		private SourceState GetSourceState()
			=> (SourceState)GetSource(GetSourceInteger.SourceState);

		private void AllocateTempSampleBuffer(int sampleCount)
		{
			var length = sampleCount * _sound.ChannelCount;
			if (_tempSampleBuffer == null || _tempSampleBuffer.Length < length)
			{
				_tempSampleBuffer = new short[length];
			}
		}

		private class BufferPool : IDisposable
		{
			private readonly Stack<BufferPoolItem> _availableItems = new();
			private readonly Queue<BufferPoolItem> _obtainedItems = new();

			public void Dispose()
			{
				foreach (var item in _availableItems.Concat(_obtainedItems))
				{
					_al.DeleteBuffer(item.BufferID);
				}
				_availableItems.Clear();
				_obtainedItems.Clear();
			}

			public BufferPoolItem Obtain(int length)
			{
				var item = _availableItems.Count != 0 ? _availableItems.Pop() : new();
				item.Length = length;
				_obtainedItems.Enqueue(item);
				return item;
			}

			public BufferPoolItem ReleaseOne()
			{
				var item = _obtainedItems.Dequeue();
				_availableItems.Push(item);
				return item;
			}

			public class BufferPoolItem
			{
				public uint BufferID { get; } = _al.GenBuffer();
				public int Length { get; set; }
			}
		}
	}
}
