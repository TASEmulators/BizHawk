using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

#if WINDOWS
using SlimDX.DirectSound;
using SlimDX.Multimedia;
#else
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
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

		private bool _muted;
		private bool _disposed;
		private DirectSound _device;
		private SecondarySoundBuffer _deviceBuffer;
		private readonly BufferedAsync _semiSync = new BufferedAsync();
		private SoundOutputProvider _outputProvider;
		private ISoundProvider _asyncSoundProvider;
		private ISyncSoundProvider _syncSoundProvider;
		private int _actualWriteOffsetBytes = -1;
		private int _filledBufferSizeBytes;
		private long _lastWriteTime;
		private int _lastWriteCursor;

		public Sound(IntPtr handle, DirectSound device)
		{
			if (device == null) return;

			device.SetCooperativeLevel(handle, CooperativeLevel.Priority);
			_device = device;
		}

		private int BufferSizeSamples { get; set; }

		private int BufferSizeBytes
		{
			get { return BufferSizeSamples * BlockAlign; }
		}

		private void CreateDeviceBuffer()
		{
			BufferSizeSamples = MillisecondsToSamples(Global.Config.SoundBufferSizeMs);

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
					Flags = BufferFlags.GlobalFocus | BufferFlags.Software | BufferFlags.GetCurrentPosition2 | BufferFlags.ControlVolume,
					SizeInBytes = BufferSizeBytes
				};

			_deviceBuffer = new SecondarySoundBuffer(_device, desc);
		}

		public void ApplyVolumeSettings()
		{
			if (_deviceBuffer == null) return;

			double volume = Global.Config.SoundVolume / 100.0;
			if (volume < 0.0) volume = 0.0;
			if (volume > 1.0) volume = 1.0;
			_deviceBuffer.Volume = CalculateDirectSoundVolumeLevel(volume);
		}

		public void StartSound()
		{
			if (_disposed) throw new ObjectDisposedException("Sound");
			if (!Global.Config.SoundEnabled) return;
			if (_deviceBuffer != null) return;

			CreateDeviceBuffer();
			ApplyVolumeSettings();

			_deviceBuffer.Write(new byte[BufferSizeBytes], 0, LockFlags.EntireBuffer);
			_deviceBuffer.CurrentPlayPosition = 0;
			_deviceBuffer.Play(0, PlayFlags.Looping);

			_actualWriteOffsetBytes = -1;
			_filledBufferSizeBytes = 0;
			_lastWriteTime = 0;
			_lastWriteCursor = 0;

			int minBufferFullnessMs =
				Global.Config.SoundBufferSizeMs < 80 ? 35 :
				Global.Config.SoundBufferSizeMs < 100 ? 45 :
				55;

			_outputProvider = new SoundOutputProvider();
			_outputProvider.MaxSamplesDeficit = BufferSizeSamples - MillisecondsToSamples(minBufferFullnessMs);
			_outputProvider.BaseSoundProvider = _syncSoundProvider;

			Global.SoundMaxBufferDeficitMs = Global.Config.SoundBufferSizeMs - minBufferFullnessMs;

			//LogUnderruns = true;
			//_outputProvider.LogDebug = true;
		}

		public void StopSound()
		{
			if (_deviceBuffer == null) return;

			_deviceBuffer.Write(new byte[BufferSizeBytes], 0, LockFlags.EntireBuffer);
			_deviceBuffer.Stop();
			_deviceBuffer.Dispose();
			_deviceBuffer = null;

			_outputProvider = null;

			Global.SoundMaxBufferDeficitMs = 0;

			BufferSizeSamples = 0;
		}

		public void Dispose()
		{
			if (_disposed) return;
			StopSound();
			_disposed = true;
		}

		public void SetSyncInputPin(ISyncSoundProvider source)
		{
			if (_asyncSoundProvider != null)
			{
				_asyncSoundProvider.DiscardSamples();
				_asyncSoundProvider = null;
			}
			_semiSync.DiscardSamples();
			_semiSync.BaseSoundProvider = null;
			_syncSoundProvider = source;
			if (_outputProvider != null)
			{
				_outputProvider.BaseSoundProvider = source;
			}
		}

		public void SetAsyncInputPin(ISoundProvider source)
		{
			if (_syncSoundProvider != null)
			{
				_syncSoundProvider.DiscardSamples();
				_syncSoundProvider = null;
			}
			if (_outputProvider != null)
			{
				_outputProvider.DiscardSamples();
				_outputProvider.BaseSoundProvider = null;
			}
			_asyncSoundProvider = source;
			_semiSync.BaseSoundProvider = source;
			_semiSync.RecalculateMagic(Global.CoreComm.VsyncRate);
		}

		private bool InitializeBufferWithSilence
		{
			get { return true; }
		}

		private bool RecoverFromUnderrunsWithSilence
		{
			get { return true; }
		}

		private int SilenceLeaveRoomForFrameCount
		{
			get { return Global.Config.SoundThrottle ? 1 : 2; } // Why 2? I don't know, but it seems to work well with the clock throttle's behavior.
		}

		public bool LogUnderruns { get; set; }

		private int CalculateSamplesNeeded()
		{
			long currentWriteTime = Stopwatch.GetTimestamp();
			int playCursor = _deviceBuffer.CurrentPlayPosition;
			int writeCursor = _deviceBuffer.CurrentWritePosition;
			bool detectedUnderrun = false;
			if (_actualWriteOffsetBytes != -1)
			{
				double elapsedSeconds = (currentWriteTime - _lastWriteTime) / (double)Stopwatch.Frequency;
				double bufferSizeSeconds = (double)BufferSizeSamples / SampleRate;
				int cursorDelta = CircularDistance(_lastWriteCursor, writeCursor, BufferSizeBytes);
				cursorDelta += BufferSizeBytes * (int)Math.Round((elapsedSeconds - (cursorDelta / (double)(SampleRate * BlockAlign))) / bufferSizeSeconds);
				_filledBufferSizeBytes -= cursorDelta;
				if (_filledBufferSizeBytes < 0)
				{
					if (LogUnderruns) Console.WriteLine("DirectSound underrun detected!");
					detectedUnderrun = true;
					_outputProvider.OnUnderrun();
				}
			}
			bool isInitializing = _actualWriteOffsetBytes == -1;
			if (isInitializing || detectedUnderrun)
			{
				_actualWriteOffsetBytes = writeCursor;
				_filledBufferSizeBytes = 0;
			}
			int samplesNeeded = CircularDistance(_actualWriteOffsetBytes, playCursor, BufferSizeBytes) / BlockAlign;
			if ((isInitializing && InitializeBufferWithSilence) || (detectedUnderrun && RecoverFromUnderrunsWithSilence))
			{
				int samplesPerFrame = (int)Math.Round(SampleRate / Global.Emulator.CoreComm.VsyncRate);
				int silenceSamples = Math.Max(samplesNeeded - (SilenceLeaveRoomForFrameCount * samplesPerFrame), 0);
				WriteSamples(new short[silenceSamples * 2], silenceSamples);
				samplesNeeded -= silenceSamples;
			}
			_lastWriteTime = currentWriteTime;
			_lastWriteCursor = writeCursor;
			return samplesNeeded;
		}

		private int CircularDistance(int start, int end, int size)
		{
			return (end - start + size) % size;
		}

		private void WriteSamples(short[] samples, int sampleCount)
		{
			if (sampleCount == 0) return;
			_deviceBuffer.Write(samples, 0, sampleCount * ChannelCount, _actualWriteOffsetBytes, LockFlags.None);
			AdvanceWriteOffset(sampleCount);
		}

		private void AdvanceWriteOffset(int sampleCount)
		{
			_actualWriteOffsetBytes = (_actualWriteOffsetBytes + (sampleCount * BlockAlign)) % BufferSizeBytes;
			_filledBufferSizeBytes += sampleCount * BlockAlign;
		}

		public void UpdateSilence()
		{
			_muted = true;
			UpdateSound();
			_muted = false;
		}

		public void UpdateSound()
		{
			if (!Global.Config.SoundEnabled || _deviceBuffer == null || _disposed)
			{
				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
				if (_outputProvider != null) _outputProvider.DiscardSamples();
				return;
			}

			short[] samples;
			int samplesNeeded = CalculateSamplesNeeded();
			int samplesProvided;

			if (_muted)
			{
				samples = new short[samplesNeeded * ChannelCount];
				samplesProvided = samplesNeeded;

				if (_asyncSoundProvider != null) _asyncSoundProvider.DiscardSamples();
				if (_syncSoundProvider != null) _syncSoundProvider.DiscardSamples();
				if (_outputProvider != null) _outputProvider.DiscardSamples();
			}
			else if (_syncSoundProvider != null)
			{
				if (Global.Config.SoundThrottle)
				{
					_syncSoundProvider.GetSamples(out samples, out samplesProvided);

					while (samplesNeeded < samplesProvided && !Global.DisableSecondaryThrottling)
					{
						Thread.Sleep((samplesProvided - samplesNeeded) / (SampleRate / 1000)); // let audio clock control sleep time
						samplesNeeded = CalculateSamplesNeeded();
					}
				}
				else
				{
					_outputProvider.GetSamples(samplesNeeded, out samples, out samplesProvided);
				}
			}
			else if (_asyncSoundProvider != null)
			{
				samples = new short[samplesNeeded * ChannelCount];

				_semiSync.GetSamples(samples);

				samplesProvided = samplesNeeded;
			}
			else
			{
				return;
			}

			WriteSamples(samples, samplesProvided);
		}

		private static int MillisecondsToSamples(int milliseconds)
		{
			return milliseconds * SampleRate / 1000;
		}

		/// <param name="level">Percent volume level from 0.0 to 1.0.</param>
		private static int CalculateDirectSoundVolumeLevel(double level)
		{
			// I'm not sure if this is "technically" correct but it works okay
			int range = (int)Volume.Maximum - (int)Volume.Minimum;
			return (int)(Math.Pow(level, 0.15) * range) + (int)Volume.Minimum;
		}
	}
#else
	//OpenAL implementation for other platforms
	public class Sound
	{
		public bool Muted = false;
		public bool needDiscard;
		private AudioContext _audContext;
		private int _audSource;
		private const int BUFFER_SIZE = 735 * 2 * 2; // 1/60th of a second, 2 bytes per sample, 2 channels;
		private ISoundProvider asyncsoundProvider;
		private ISyncSoundProvider syncsoundProvider;
		private BufferedAsync semisync = new BufferedAsync();

		public Sound()
		{
			try
			{
				_audContext = new AudioContext();
				_audSource = AL.GenSource();
				//Buffer 6 frames worth of sound
				//How large of a buffer we need seems to depend on the console
				//For GameGear, 3 or 4 is usually fine. For NES I need 6 frames or it can get choppy.
				for(int i=0; i<6; i++)
				{
					int buffer = AL.GenBuffer();
					short[] samples = new short[BUFFER_SIZE>>1]; //Initialize with empty sound
					AL.BufferData(buffer, ALFormat.Stereo16, samples, BUFFER_SIZE, 44100);
					AL.SourceQueueBuffer(_audSource, buffer);
				}
			}
			catch(AudioException e)
			{
				System.Windows.Forms.MessageBox.Show("Unable to initalize sound. That's too bad.");
			}
		}

		public void StartSound()
		{
			AL.SourcePlay(_audSource);
		}

		public bool IsPlaying = false;

		public void StopSound()
		{
			AL.SourceStop(_audSource);
		}

		public void Dispose()
		{
			//Todo: Should I delete the buffers?
			AL.DeleteSource(_audSource);
			_audContext.Dispose();
		}

		int CalculateSamplesNeeded()
		{
			return BUFFER_SIZE>>2;
		}

		public void UpdateSilence()
		{
			Muted = true;
			UpdateSound();
			Muted = false;
		}

		public void UpdateSound()
		{
			if (Global.Config.SoundEnabled == false)
			{
				if (asyncsoundProvider != null) asyncsoundProvider.DiscardSamples();
				if (syncsoundProvider != null) syncsoundProvider.DiscardSamples();
				return;
			}
			int amtToFill = 0;
			AL.GetSource(_audSource, ALGetSourcei.BuffersProcessed, out amtToFill);
			for(int i=0; i<amtToFill; i++)
			{
				int samplesNeeded = SNDDXGetAudioSpace() * 2;
				int samplesProvided = 0;
				short[] samples;

				if (Muted)
				{
					if (samplesNeeded == 0)
						return;
					samples = new short[samplesNeeded];
					samplesProvided = samplesNeeded;
				}
				else if (syncsoundProvider != null)
				{
					int nsampgot;
					syncsoundProvider.GetSamples(out samples, out nsampgot);
					samplesProvided = 2 * nsampgot;
				}
				else if (asyncsoundProvider != null)
				{
					if (samplesNeeded == 0)
						return;
					samples = new short[samplesNeeded];
					semisync.BaseSoundProvider = asyncsoundProvider;
					semisync.GetSamples(samples);
					samplesProvided = samplesNeeded;
				}
				else
					return;

				AL.GetSource(_audSource, ALGetSourcei.BuffersProcessed, out amtToFill);
				int buffer = AL.SourceUnqueueBuffer(_audSource);
				AL.BufferData(buffer, ALFormat.Stereo16, samples, samplesProvided*2, 44100);
				AL.SourceQueueBuffer(_audSource, buffer);
				if (syncsoundProvider != null)
				{
					if (!Global.ForceNoThrottle)
					{
						int buffersProcessed;
						do
						{
							AL.GetSource(_audSource, ALGetSourcei.BuffersProcessed, out buffersProcessed);
							if (buffersProcessed < 1)
								System.Threading.Thread.Sleep(1);
						} while(buffersProcessed < 1);
					}
					break; //We're syncing via audio, so I can only fill the buffer once here
				}
			}
			if(AL.GetSourceState(_audSource) != ALSourceState.Playing)
			{
				AL.SourcePlay(_audSource);
			}
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
			//Don't need this, we mute when we're told
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
		}
	}
#endif
}
