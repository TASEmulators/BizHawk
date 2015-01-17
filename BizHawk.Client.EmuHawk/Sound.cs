using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

#if WINDOWS
using SlimDX.DirectSound;
using SlimDX.Multimedia;
#endif

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
#if WINDOWS
	public static class SoundEnumeration
	{
		public static DirectSound Create()
		{
			var dc = DirectSound.GetDevices();
			foreach (var dev in dc)
			{
				if (dev.Description == Global.Config.SoundDevice)
					return new DirectSound(dev.DriverGuid);
			}
			return new DirectSound();
		}

		public static IEnumerable<string> DeviceNames()
		{
			var ret = new List<string>();
			var dc = DirectSound.GetDevices();
			foreach (var dev in dc)
				ret.Add(dev.Description);
			return ret;
		}
	}

	public class Sound : IDisposable
	{
		private const int SampleRate = 44100;
		private const int BytesPerSample = 2;
		private const int ChannelCount = 2;
		private const int BlockAlign = BytesPerSample * ChannelCount;
		private const int BufferSize = (SampleRate / 10) * BlockAlign; // 1/10th of a second
		private const double BufferDuration = (double)(BufferSize / BlockAlign) / SampleRate;

		private bool _muted;
		private bool _disposed;
		private SecondarySoundBuffer _deviceBuffer;
		private readonly BufferedAsync _semiSync = new BufferedAsync();
		private ISoundProvider _asyncSoundProvider;
		private ISyncSoundProvider _syncSoundProvider;
		private int _actualWriteOffset = -1;
		private int _filledBufferSize;
		private long _lastWriteTime;
		private int _lastWriteCursor;

		public Sound(IntPtr handle, DirectSound device)
		{
			if (device == null) return;

			device.SetCooperativeLevel(handle, CooperativeLevel.Priority);

			var format = new WaveFormat
				{
					SamplesPerSecond = SampleRate,
					BitsPerSample = BytesPerSample * 8,
					Channels = ChannelCount,
					FormatTag = WaveFormatTag.Pcm,
					BlockAlignment = BlockAlign,
					AverageBytesPerSecond = SampleRate * BlockAlign
				};

			var desc = new SoundBufferDescription
				{
					Format = format,
					Flags =
						BufferFlags.GlobalFocus | BufferFlags.Software | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume,
					SizeInBytes = BufferSize
				};

			_deviceBuffer = new SecondarySoundBuffer(device, desc);

			ChangeVolume(Global.Config.SoundVolume);
		}

		public void StartSound()
		{
			if (_disposed) throw new ObjectDisposedException("Sound");
			if (Global.Config.SoundEnabled == false) return;
			if (_deviceBuffer == null) return;
			if (IsPlaying) return;

			_deviceBuffer.Write(new byte[BufferSize], 0, LockFlags.EntireBuffer);
			_deviceBuffer.CurrentPlayPosition = 0;
			_deviceBuffer.Play(0, PlayFlags.Looping);
		}

		bool IsPlaying
		{
			get
			{
				if (_deviceBuffer == null) return false;
				if ((_deviceBuffer.Status & BufferStatus.Playing) != 0) return true;
				return false;
			}
		}

		public void StopSound()
		{
			if (!IsPlaying) return;

			_deviceBuffer.Write(new byte[BufferSize], 0, LockFlags.EntireBuffer);
			_deviceBuffer.Stop();
		}

		public void Dispose()
		{
			if (_disposed) return;
			if (_deviceBuffer != null && _deviceBuffer.Disposed == false)
			{
				_deviceBuffer.Dispose();
				_deviceBuffer = null;
			}
			_disposed = true;
		}

		public void SetSyncInputPin(ISyncSoundProvider source)
		{
			_syncSoundProvider = source;
			_asyncSoundProvider = null;
			_semiSync.DiscardSamples();
		}

		public void SetAsyncInputPin(ISoundProvider source)
		{
			_syncSoundProvider = null;
			_asyncSoundProvider = source;
			_semiSync.BaseSoundProvider = source;
			_semiSync.RecalculateMagic(Global.CoreComm.VsyncRate);
		}

		private int CalculateSamplesNeeded()
		{
			long currentWriteTime = Stopwatch.GetTimestamp();
			int playCursor = _deviceBuffer.CurrentPlayPosition;
			int writeCursor = _deviceBuffer.CurrentWritePosition;
			if (_actualWriteOffset != -1)
			{
				double elapsedTime = (currentWriteTime - _lastWriteTime) / (double)Stopwatch.Frequency; // Seconds
				int cursorDelta = CircularDistance(_lastWriteCursor, writeCursor, BufferSize);
				cursorDelta += BufferSize * (int)Math.Round((elapsedTime - (cursorDelta / (double)(SampleRate * BlockAlign))) / BufferDuration);
				_filledBufferSize -= cursorDelta;
				if (_filledBufferSize < 0)
				{
					// Buffer underflow
					_actualWriteOffset = -1;
				}
			}
			if (_actualWriteOffset == -1)
			{
				_actualWriteOffset = writeCursor;
				_filledBufferSize = 0;
			}
			_lastWriteTime = currentWriteTime;
			_lastWriteCursor = writeCursor;

			int bytesNeeded = CircularDistance(_actualWriteOffset, playCursor, BufferSize);
			return bytesNeeded / BlockAlign;
		}

		private int CircularDistance(int start, int end, int size)
		{
			return (end - start + size) % size;
		}

		public void UpdateSilence()
		{
			_muted = true;
			UpdateSound();
			_muted = false;
		}

		public void UpdateSound()
		{
			if (Global.Config.SoundEnabled == false || _disposed)
			{
				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
				return;
			}

			int samplesNeeded = CalculateSamplesNeeded();
			short[] samples;

			int samplesProvided;

			if (_muted)
			{
				if (samplesNeeded == 0) return;

				samples = new short[samplesNeeded * ChannelCount];
				samplesProvided = samplesNeeded;

				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
			}
			else if (_syncSoundProvider != null)
			{
				if (_deviceBuffer == null) return; // can cause CalculateSamplesNeeded() = 0
				int nsampgot;

				_syncSoundProvider.GetSamples(out samples, out nsampgot);

				samplesProvided = nsampgot;

				if (!Global.DisableSecondaryThrottling)
					while (samplesNeeded < samplesProvided)
					{
						Thread.Sleep((samplesProvided - samplesNeeded) / (SampleRate / 1000)); // let audio clock control sleep time
						samplesNeeded = CalculateSamplesNeeded();
					}
			}
			else if (_asyncSoundProvider != null)
			{
				samples = new short[samplesNeeded * ChannelCount];
				//if (asyncsoundProvider != null && Muted == false)
				//{
				_semiSync.BaseSoundProvider = _asyncSoundProvider;
				_semiSync.GetSamples(samples);
				//}
				//else asyncsoundProvider.DiscardSamples();

				if (samplesNeeded == 0) return;

				samplesProvided = samplesNeeded;
			}
			else
				return;

			_deviceBuffer.Write(samples, 0, samplesProvided * ChannelCount, _actualWriteOffset, LockFlags.None);
			_actualWriteOffset = (_actualWriteOffset + (samplesProvided * BlockAlign)) % BufferSize;
			_filledBufferSize += samplesProvided * BlockAlign;
		}

		/// <summary>
		/// Range: 0-100
		/// </summary>
		/// <param name="vol"></param>
		public void ChangeVolume(int vol)
		{
			if (vol > 100)
				vol = 100;
			if (vol < 0)
				vol = 0;
			Global.Config.SoundVolume = vol;
			UpdateSoundSettings();
		}

		/// <summary>
		/// Uses Global.Config.SoundEnabled, this just notifies the object to read it
		/// </summary>
		public void UpdateSoundSettings()
		{
			if (!Global.Config.SoundEnabled || Global.Config.SoundVolume == 0)
				_deviceBuffer.Volume = -5000;
			else
				_deviceBuffer.Volume = 0 - ((100 - Global.Config.SoundVolume) * 45);
		}
	}
#else
	// Dummy implementation for non-Windows platforms for now.
	public class Sound
	{
		public bool Muted = false;
		public bool needDiscard;

		public Sound()
		{
		}

		public void StartSound()
		{
		}

		public bool IsPlaying = false;

		public void StopSound()
		{
		}

		public void Dispose()
		{
		}

		int CalculateSamplesNeeded()
		{
			return 0;
		}

		public void UpdateSound(ISoundProvider soundProvider)
		{
			soundProvider.DiscardSamples();
		}

		/// <summary>
		/// Range: 0-100
		/// </summary>
		/// <param name="vol"></param>
		public void ChangeVolume(int vol)
		{
			Global.Config.SoundVolume = vol;
			UpdateSoundSettings();
		}

		/// <summary>
		/// Uses Global.Config.SoundEnabled, this just notifies the object to read it
		/// </summary>
		public void UpdateSoundSettings()
		{
			if (Global.Emulator is NES)
			{
				NES n = Global.Emulator as NES;
				if (Global.Config.SoundEnabled == false)
					n.SoundOn = false;
				else
					n.SoundOn = true;
			}
		}
	}
#endif
}
