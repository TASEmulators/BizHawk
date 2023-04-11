using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

using Silk.NET.Core.Native;
using Silk.NET.OpenAL;
using Silk.NET.OpenAL.Extensions.Creative;
using Silk.NET.OpenAL.Extensions.Enumeration;

namespace BizHawk.Bizware.SilkNET
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
		private uint _sourceID;
		private BufferPool _bufferPool;
		private int _currentSamplesQueued;
		private short[] _tempSampleBuffer;

		public OpenALSoundOutput(IHostAudioManager sound, string chosenDeviceName)
		{
			_sound = sound;
			_context = new(
				GetDeviceNames().Contains(chosenDeviceName) ? chosenDeviceName : null,
				_sound.SampleRate
			);
		}

		public void Dispose()
		{
			if (_disposed) return;

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

		public int CalculateSamplesNeeded()
		{
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
				samples.AsSpan(sampleOffset * _sound.BlockAlign, byteCount / 2)
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
				public uint BufferID { get; }
				public int Length { get; set; }

				public BufferPoolItem()
				{
					BufferID = _al.GenBuffer();
				}
			}
		}
	}
}
