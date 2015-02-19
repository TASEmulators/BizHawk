using System;
using System.Collections.Generic;
using System.Linq;

using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class OpenALSoundOutput : ISoundOutput
	{
		private bool _disposed;
		private Sound _sound;
		private AudioContext _context;
		private int _sourceID;
		private BufferPool _bufferPool;
		private long _runningSamplesPlayed;
		private long _runningSamplesQueued;

		public OpenALSoundOutput(Sound sound)
		{
			_sound = sound;
			_context = new AudioContext();
		}

		public void Dispose()
		{
			if (_disposed) return;

			_context.Dispose();
			_context = null;

			_disposed = true;
		}

		public static IEnumerable<string> GetDeviceNames()
		{
			return Enumerable.Empty<string>();
		}

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			AL.Source(_sourceID, ALSourcef.Gain, (float)volume);
		}

		public void StartSound()
		{
			BufferSizeSamples = Sound.MillisecondsToSamples(Global.Config.SoundBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			_sourceID = AL.GenSource();

			_bufferPool = new BufferPool();
			_runningSamplesQueued = 0;
		}

		public void StopSound()
		{
			AL.SourceStop(_sourceID);

			AL.DeleteSource(_sourceID);

			_bufferPool.Dispose();
			_bufferPool = null;

			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			bool isInitializing = _runningSamplesQueued == 0;
			bool detectedUnderrun = !isInitializing && GetSource(ALGetSourcei.BuffersProcessed) == GetSource(ALGetSourcei.BuffersQueued);
			if (detectedUnderrun)
			{
				_sound.OnUnderrun();
			}
			UnqueueProcessedBuffers();
			long samplesAwaitingPlayback = _runningSamplesQueued - (_runningSamplesPlayed + GetSource(ALGetSourcei.SampleOffset));
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
			UnqueueProcessedBuffers();
			int byteCount = sampleCount * Sound.BlockAlign;
			var buffer = _bufferPool.Obtain(byteCount);
			AL.BufferData(buffer.BufferID, ALFormat.Stereo16, samples, byteCount, Sound.SampleRate);
			AL.SourceQueueBuffer(_sourceID, buffer.BufferID);
			_runningSamplesQueued += sampleCount;
			if (AL.GetSourceState(_sourceID) != ALSourceState.Playing)
			{
				AL.SourcePlay(_sourceID);
			}
		}

		private void UnqueueProcessedBuffers()
		{
			int releaseCount = GetSource(ALGetSourcei.BuffersProcessed);
			while (releaseCount > 0)
			{
				AL.SourceUnqueueBuffer(_sourceID);
				var releasedBuffer = _bufferPool.ReleaseOne();
				_runningSamplesPlayed += releasedBuffer.Length / Sound.BlockAlign;
				releaseCount--;
			}
		}

		private int GetSource(ALGetSourcei param)
		{
			int value;
			AL.GetSource(_sourceID, param, out value);
			return value;
		}

		private class BufferPool : IDisposable
		{
			private List<BufferPoolItem> _availableItems = new List<BufferPoolItem>();
			private Queue<BufferPoolItem> _obtainedItems = new Queue<BufferPoolItem>();

			public void Dispose()
			{
				foreach (BufferPoolItem item in _availableItems.Concat(_obtainedItems))
				{
					AL.DeleteBuffer(item.BufferID);
				}
				_availableItems.Clear();
				_obtainedItems.Clear();
			}

			public BufferPoolItem Obtain(int length)
			{
				BufferPoolItem item = GetAvailableItem() ?? new BufferPoolItem();
				item.Length = length;
				_obtainedItems.Enqueue(item);
				return item;
			}

			private BufferPoolItem GetAvailableItem()
			{
				if (_availableItems.Count == 0) return null;
				BufferPoolItem item = _availableItems[0];
				_availableItems.RemoveAt(0);
				return item;
			}

			public BufferPoolItem ReleaseOne()
			{
				BufferPoolItem item = _obtainedItems.Dequeue();
				_availableItems.Add(item);
				return item;
			}

			public class BufferPoolItem
			{
				public int BufferID { get; private set; }
				public int Length { get; set; }

				public BufferPoolItem()
				{
					BufferID = AL.GenBuffer();
				}
			}
		}
	}
}
