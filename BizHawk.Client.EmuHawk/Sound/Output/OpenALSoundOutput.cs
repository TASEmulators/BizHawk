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
		private int _currentSamplesQueued;

		public OpenALSoundOutput(Sound sound)
		{
			_sound = sound;
			string deviceName = GetDeviceNames().FirstOrDefault(n => n == Global.Config.SoundDevice);
			_context = new AudioContext(deviceName, Sound.SampleRate);
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
			if (!Alc.IsExtensionPresent(IntPtr.Zero, "ALC_ENUMERATION_EXT")) return Enumerable.Empty<string>();
			return Alc.GetString(IntPtr.Zero, AlcGetStringList.AllDevicesSpecifier);
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
			_currentSamplesQueued = 0;
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
			int currentSamplesPlayed = GetSource(ALGetSourcei.SampleOffset);
			ALSourceState sourceState = AL.GetSourceState(_sourceID);
			bool isInitializing = sourceState == ALSourceState.Initial;
			bool detectedUnderrun = sourceState == ALSourceState.Stopped;
			if (detectedUnderrun)
			{
				// SampleOffset should reset to 0 when stopped; update the queued sample count to match
				UnqueueProcessedBuffers();
				currentSamplesPlayed = 0;
			}
			int samplesAwaitingPlayback = _currentSamplesQueued - currentSamplesPlayed;
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
			_currentSamplesQueued += sampleCount;
			if (AL.GetSourceState(_sourceID) != ALSourceState.Playing)
			{
				AL.SourcePlay(_sourceID);
			}
		}

		private void UnqueueProcessedBuffers()
		{
			int releaseCount = GetSource(ALGetSourcei.BuffersProcessed);
			for (int i = 0; i < releaseCount; i++)
			{
				AL.SourceUnqueueBuffer(_sourceID);
				var releasedBuffer = _bufferPool.ReleaseOne();
				_currentSamplesQueued -= releasedBuffer.Length / Sound.BlockAlign;
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
			private Stack<BufferPoolItem> _availableItems = new Stack<BufferPoolItem>();
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
				BufferPoolItem item = _availableItems.Count != 0 ? _availableItems.Pop() : new BufferPoolItem();
				item.Length = length;
				_obtainedItems.Enqueue(item);
				return item;
			}

			public BufferPoolItem ReleaseOne()
			{
				BufferPoolItem item = _obtainedItems.Dequeue();
				_availableItems.Push(item);
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
