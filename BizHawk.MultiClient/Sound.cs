using System;
using BizHawk.Emulation.Sound;
#if WINDOWS
using SlimDX.DirectSound;
using SlimDX.Multimedia;
#endif

using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
#if WINDOWS
	public class Sound : IDisposable
	{
		public bool Muted = false;
		private bool disposed = false;

		private SecondarySoundBuffer DSoundBuffer;
		private byte[] SoundBuffer;
		private const int BufferSize = 4410 * 2 * 2; // 1/10th of a second, 2 bytes per sample, 2 channels;
		//private int SoundBufferPosition; //TODO: use this
		public bool needDiscard;

		private BufferedAsync semisync = new BufferedAsync();

		public Sound(IntPtr handle, DirectSound device)
		{
			if (device != null)
			{
				device.SetCooperativeLevel(handle, CooperativeLevel.Priority);

				var format = new WaveFormat();
				format.SamplesPerSecond = 44100;
				format.BitsPerSample = 16;
				format.Channels = 2;
				format.FormatTag = WaveFormatTag.Pcm;
				format.BlockAlignment = 4;
				format.AverageBytesPerSecond = format.SamplesPerSecond * format.Channels * (format.BitsPerSample / 8);

				var desc = new SoundBufferDescription();
				desc.Format = format;
				desc.Flags = BufferFlags.GlobalFocus | BufferFlags.Software | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume;
				desc.SizeInBytes = BufferSize;
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

		public void UpdateSound(ISoundProvider soundProvider)
		{
			if (Global.Config.SoundEnabled == false || disposed)
			{
				soundProvider.DiscardSamples();
				return;
			}

			int samplesNeeded = SNDDXGetAudioSpace() * 2;
			short[] samples;

			if (Global.Config.SoundThrottle)
			{
				if (DSoundBuffer == null) return; // can cause SNDDXGetAudioSpace() = 0
				int samplesWanted = 2 * (int) (0.8 + 44100.0 / Global.Emulator.CoreOutputComm.VsyncRate);

				while (samplesNeeded < samplesWanted)
				{
					System.Threading.Thread.Sleep((samplesWanted - samplesNeeded) / 88); // let audio clock control sleep time
					samplesNeeded = SNDDXGetAudioSpace() * 2;
				}
				samplesNeeded = samplesWanted;
				samples = new short[samplesNeeded];
				// no semisync
				if (!Muted)
					soundProvider.GetSamples(samples);
				else
					soundProvider.DiscardSamples();
			}
			else
			{
				if (samplesNeeded == 0)
					return;
				samples = new short[samplesNeeded];
				if (soundProvider != null && Muted == false)
				{
					semisync.BaseSoundProvider = soundProvider;
					semisync.GetSamples(samples);
				}
				else soundProvider.DiscardSamples();
			}
			
			//Console.WriteLine(samplesNeeded/2);


			int cursor = soundoffset;
			for (int i = 0; i < samples.Length; i++)
			{
				short s = samples[i];
				SoundBuffer[cursor++] = (byte)(s & 0xFF);
				SoundBuffer[cursor++] = (byte)(s >> 8);

				if (cursor >= SoundBuffer.Length)
					cursor = 0;
			}

			DSoundBuffer.Write(SoundBuffer, 0, LockFlags.EntireBuffer);

			soundoffset += samplesNeeded * 2;
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
			int vol = Global.Config.SoundVolume;
			if (!Global.Config.SoundEnabled || Global.Config.SoundVolume == 0)
				DSoundBuffer.Volume = -5000;
			else
				DSoundBuffer.Volume = 0 - ((100 - Global.Config.SoundVolume) * 15);

			/* //adelikat: I've been told this isn't TAS safe, so I'm disabling this speed hack
			if (Global.Emulator is NES)
			{
				NES n = Global.Emulator as NES;
				if (Global.Config.SoundEnabled == false)
					n.SoundOn = false;
				else
					n.SoundOn = true;
			}
			*/
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
