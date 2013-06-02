using System;
using BizHawk.Emulation.Sound;
using System.Collections.Generic;
#if WINDOWS
using SlimDX.DirectSound;
using SlimDX.Multimedia;
#endif

namespace BizHawk.MultiClient
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
		public bool needDiscard;

		private bool Muted;
		private readonly bool disposed;
		private SecondarySoundBuffer DSoundBuffer;
		private readonly byte[] SoundBuffer;
		private const int BufferSize = 4410 * 2 * 2; // 1/10th of a second, 2 bytes per sample, 2 channels;
		//private int SoundBufferPosition; //TODO: use this
		private readonly BufferedAsync semisync = new BufferedAsync();
		private ISoundProvider asyncsoundProvider;
		private ISyncSoundProvider syncsoundProvider;

		public Sound(IntPtr handle, DirectSound device)
		{
			if (device != null)
			{
				device.SetCooperativeLevel(handle, CooperativeLevel.Priority);

				var format = new WaveFormat
					{
						SamplesPerSecond = 44100,
						BitsPerSample = 16,
						Channels = 2,
						FormatTag = WaveFormatTag.Pcm,
						BlockAlignment = 4
					};
				format.AverageBytesPerSecond = format.SamplesPerSecond * format.Channels * (format.BitsPerSample / 8);

				var desc = new SoundBufferDescription
					{
						Format = format,
						Flags =
							BufferFlags.GlobalFocus | BufferFlags.Software | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume,
						SizeInBytes = BufferSize
					};
				DSoundBuffer = new SecondarySoundBuffer(device, desc);
				ChangeVolume(Global.Config.SoundVolume);
			}
			SoundBuffer = new byte[BufferSize];

			disposed = false;
		}

		public void StartSound()
		{
			if (disposed) throw new ObjectDisposedException("Sound");
			if (Global.Config.SoundEnabled == false) return;
			if (DSoundBuffer == null) return;
			if (IsPlaying)
				return;

			needDiscard = true;

			DSoundBuffer.Write(SoundBuffer, 0, LockFlags.EntireBuffer);
			DSoundBuffer.CurrentPlayPosition = 0;
			DSoundBuffer.Play(0, PlayFlags.Looping);
		}

		bool IsPlaying
		{
			get
			{
				if (DSoundBuffer == null) return false;
				if ((DSoundBuffer.Status & BufferStatus.Playing) != 0) return true;
				return false;
			}
		}

		public void StopSound()
		{
			if (!IsPlaying)
				return;
			for (int i = 0; i < SoundBuffer.Length; i++)
				SoundBuffer[i] = 0;
			DSoundBuffer.Write(SoundBuffer, 0, LockFlags.EntireBuffer);
			DSoundBuffer.Stop();
		}

		public void Dispose()
		{
			if (disposed) return;
			if (DSoundBuffer != null && DSoundBuffer.Disposed == false)
			{
				DSoundBuffer.Dispose();
				DSoundBuffer = null;
			}
		}

		public void SetSyncInputPin(ISyncSoundProvider source)
		{
			syncsoundProvider = source;
			asyncsoundProvider = null;
			semisync.DiscardSamples();
		}

		public void SetAsyncInputPin(ISoundProvider source)
		{
			syncsoundProvider = null;
			asyncsoundProvider = source;
			semisync.BaseSoundProvider = source;
			semisync.RecalculateMagic(Global.CoreComm.VsyncRate);
		}

		static int circularDist(int from, int to, int size)
		{
			if (size == 0)
				return 0;
			int diff = (to - from);
			while (diff < 0)
				diff += size;
			return diff;
		}

		int soundoffset;
		int SNDDXGetAudioSpace()
		{
			if (DSoundBuffer == null) return 0;

			int playcursor = DSoundBuffer.CurrentPlayPosition;
			int writecursor = DSoundBuffer.CurrentWritePosition;

			int curToWrite = circularDist(soundoffset, writecursor, BufferSize);
			int curToPlay = circularDist(soundoffset, playcursor, BufferSize);

			if (curToWrite < curToPlay)
				return 0; // in-between the two cursors. we shouldn't write anything during this time.

			return curToPlay / 4;
		}

		public void UpdateSilence()
		{
			Muted = true;
			UpdateSound();
			Muted = false;
		}

		public void UpdateSound()
		{
			if (Global.Config.SoundEnabled == false || disposed)
			{
				if (asyncsoundProvider != null) asyncsoundProvider.DiscardSamples();
				if (syncsoundProvider != null) syncsoundProvider.DiscardSamples();
				return;
			}

			int samplesNeeded = SNDDXGetAudioSpace() * 2;
			short[] samples;

			int samplesProvided;


			if (Muted)
			{
				if (samplesNeeded == 0)
					return;
				samples = new short[samplesNeeded];
				samplesProvided = samplesNeeded;

				if (asyncsoundProvider != null) asyncsoundProvider.DiscardSamples();
				if (syncsoundProvider != null) syncsoundProvider.DiscardSamples();
			}
			else if (syncsoundProvider != null)
			{
				if (DSoundBuffer == null) return; // can cause SNDDXGetAudioSpace() = 0
				int nsampgot;

				syncsoundProvider.GetSamples(out samples, out nsampgot);

				samplesProvided = 2 * nsampgot;

				if (!Global.ForceNoThrottle)
					while (samplesNeeded < samplesProvided)
					{
						System.Threading.Thread.Sleep((samplesProvided - samplesNeeded) / 88); // let audio clock control sleep time
						samplesNeeded = SNDDXGetAudioSpace() * 2;
					}
			}
			else if (asyncsoundProvider != null)
			{
				if (samplesNeeded == 0)
					return;
				samples = new short[samplesNeeded];
				//if (asyncsoundProvider != null && Muted == false)
				//{
				semisync.BaseSoundProvider = asyncsoundProvider;
				semisync.GetSamples(samples);
				//}
				//else asyncsoundProvider.DiscardSamples();
				samplesProvided = samplesNeeded;
			}
			else
				return;
			
			int cursor = soundoffset;
			for (int i = 0; i < samplesProvided; i++)
			{
				short s = samples[i];
				SoundBuffer[cursor++] = (byte)(s & 0xFF);
				SoundBuffer[cursor++] = (byte)(s >> 8);

				if (cursor >= SoundBuffer.Length)
					cursor = 0;
			}

			DSoundBuffer.Write(SoundBuffer, 0, LockFlags.EntireBuffer);

			soundoffset += samplesProvided * 2;
			soundoffset %= BufferSize;
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
				DSoundBuffer.Volume = -5000;
			else
				DSoundBuffer.Volume = 0 - ((100 - Global.Config.SoundVolume) * 15);
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
