using System;
using System.Collections.Generic;

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
		private const int BufferSize = (SampleRate / 10) * BytesPerSample * ChannelCount; // 1/10th of a second

		private bool _muted;
		private bool _disposed;
		private SecondarySoundBuffer _deviceBuffer;
		private readonly BufferedAsync _semiSync = new BufferedAsync();
		private ISoundProvider _asyncSoundProvider;
		private ISyncSoundProvider _syncSoundProvider;
		private int _actualWriteOffset = -1;
		private long _lastWriteTime;

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
					BlockAlignment = BytesPerSample * ChannelCount,
					AverageBytesPerSecond = SampleRate * BytesPerSample * ChannelCount
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

		private int SNDDXGetAudioSpace()
		{
			int playOffset = _deviceBuffer.CurrentPlayPosition;
			int writeOffset = _deviceBuffer.CurrentWritePosition;

			if (_actualWriteOffset == -1)
			{
				_actualWriteOffset = writeOffset;
			}

			int bytesNeeded = (playOffset - _actualWriteOffset + BufferSize) % BufferSize;
			return bytesNeeded / (BytesPerSample * ChannelCount);
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

			int samplesNeeded = SNDDXGetAudioSpace() * 2;
			short[] samples;

			int samplesProvided;

			if (_muted)
			{
				if (samplesNeeded == 0) return;

				samples = new short[samplesNeeded];
				samplesProvided = samplesNeeded;

				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
			}
			else if (_syncSoundProvider != null)
			{
				if (_deviceBuffer == null) return; // can cause SNDDXGetAudioSpace() = 0
				int nsampgot;

				_syncSoundProvider.GetSamples(out samples, out nsampgot);

				samplesProvided = 2 * nsampgot;

				if (!Global.DisableSecondaryThrottling)
					while (samplesNeeded < samplesProvided)
					{
						System.Threading.Thread.Sleep((samplesProvided - samplesNeeded) / 88); // let audio clock control sleep time
						samplesNeeded = SNDDXGetAudioSpace() * 2;
					}
			}
			else if (_asyncSoundProvider != null)
			{
				samples = new short[samplesNeeded];
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

			_deviceBuffer.Write(samples, 0, samplesProvided, _actualWriteOffset, LockFlags.None);
			_actualWriteOffset = (_actualWriteOffset + (samplesProvided * BytesPerSample)) % BufferSize;
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

		int SNDDXGetAudioSpace()
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
