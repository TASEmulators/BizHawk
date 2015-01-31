#if WINDOWS
using System;
using System.Collections.Generic;
using System.Linq;

using SlimDX;
using SlimDX.Multimedia;
using SlimDX.XAudio2;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class XAudio2SoundOutput : ISoundOutput
	{
		private bool _disposed;
		private Sound _sound;
		private XAudio2 _device;
		private MasteringVoice _masteringVoice;
		private SourceVoice _sourceVoice;
		private long _runningSamplesQueued;

		public XAudio2SoundOutput(Sound sound)
		{
			_sound = sound;
			_device = new XAudio2();
			int? deviceIndex = Enumerable.Range(0, _device.DeviceCount)
				.Select(n => (int?)n)
				.FirstOrDefault(n => _device.GetDeviceDetails(n.Value).DisplayName == Global.Config.SoundDevice);
			_masteringVoice = deviceIndex == null ?
				new MasteringVoice(_device, Sound.ChannelCount, Sound.SampleRate) :
				new MasteringVoice(_device, Sound.ChannelCount, Sound.SampleRate, deviceIndex.Value);
		}

		public void Dispose()
		{
			if (_disposed) return;

			_masteringVoice.Dispose();
			_masteringVoice = null;

			_device.Dispose();
			_device = null;

			_disposed = true;
		}

		public static IEnumerable<string> GetDeviceNames()
		{
			using (XAudio2 device = new XAudio2())
			{
				return Enumerable.Range(0, device.DeviceCount).Select(n => device.GetDeviceDetails(n).DisplayName).ToList();
			}
		}

		private int BufferSizeSamples { get; set; }

		public int MaxSamplesDeficit { get; private set; }

		public void ApplyVolumeSettings(double volume)
		{
			_sourceVoice.Volume = (float)volume;
		}

		public void StartSound()
		{
			BufferSizeSamples = Sound.MillisecondsToSamples(Global.Config.SoundBufferSizeMs);
			MaxSamplesDeficit = BufferSizeSamples;

			var format = new WaveFormat
				{
					SamplesPerSecond = Sound.SampleRate,
					BitsPerSample = Sound.BytesPerSample * 8,
					Channels = Sound.ChannelCount,
					FormatTag = WaveFormatTag.Pcm,
					BlockAlignment = Sound.BlockAlign,
					AverageBytesPerSecond = Sound.SampleRate * Sound.BlockAlign
				};

			_sourceVoice = new SourceVoice(_device, format);

			_runningSamplesQueued = 0;

			_sourceVoice.Start();
		}

		public void StopSound()
		{
			_sourceVoice.Stop();
			_sourceVoice.Dispose();
			_sourceVoice = null;

			BufferSizeSamples = 0;
		}

		public int CalculateSamplesNeeded()
		{
			bool isInitializing = _runningSamplesQueued == 0;
			bool detectedUnderrun = !isInitializing && _sourceVoice.State.BuffersQueued == 0;
			if (detectedUnderrun)
			{
				_sound.OnUnderrun();
			}
			long samplesAwaitingPlayback = _runningSamplesQueued - _sourceVoice.State.SamplesPlayed;
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
			// TODO: Re-use these buffers
			byte[] bytes = new byte[sampleCount * Sound.BlockAlign];
			Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
			_sourceVoice.SubmitSourceBuffer(new AudioBuffer
			{
				AudioBytes = bytes.Length,
				AudioData = new DataStream(bytes, true, false)
			});
			_runningSamplesQueued += sampleCount;
		}
	}
}
#endif
