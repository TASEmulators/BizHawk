using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	class N64Audio : IDisposable
	{
		/// <summary>
		/// mupen64 DLL Api
		/// </summary>
		private mupen64plusAudioApi api;

		private mupen64plusApi coreAPI;

		/// <summary>
		/// Buffer for audio data
		/// </summary>
		private short[] audioBuffer = new short[0];
		private uint _samplingRate = 0;
		/// <summary>
		/// Currently active sampling rate
		/// </summary>
		public uint SamplingRate
		{
			get
			{
				return _samplingRate;
			}
			private set
			{
				_samplingRate = value;
				Resampler.ChangeRate(_samplingRate, 44100, _samplingRate, 44100);
			}
		}
		/// <summary>
		/// Resampler for audio output
		/// </summary>
		public SpeexResampler Resampler { get; private set; }
		public bool RenderSound { get; set; }

		/// <summary>
		/// Creates a N64 Audio subsystem
		/// </summary>
		/// <param name="api">Mupen64 api which is used for fetching sound</param>
		public N64Audio(mupen64plusApi core)
		{
			this.api = new mupen64plusAudioApi(core);

			_samplingRate = api.GetSamplingRate();
			Resampler = new SpeexResampler(6, SamplingRate, 44100,
				SamplingRate, 44100);

			coreAPI = core;
			coreAPI.VInterrupt += DoAudioFrame;
		}

		/// <summary>
		/// Fetches the audio buffer from mupen64plus and pushes it into the
		/// Resampler for audio output
		/// </summary>
		public void DoAudioFrame()
		{
			uint m64pSamplingRate = api.GetSamplingRate();
			if (m64pSamplingRate != SamplingRate)
				SamplingRate = m64pSamplingRate;

			int audioBufferSize = api.GetAudioBufferSize();
			if (audioBuffer.Length < audioBufferSize)
				audioBuffer = new short[audioBufferSize];

			if (audioBufferSize > 0)
			{
				api.GetAudioBuffer(audioBuffer);
				if (RenderSound)
					Resampler.EnqueueSamples(audioBuffer, audioBufferSize / 2);
			}
		}

		public void Dispose()
		{
			coreAPI.VInterrupt -= DoAudioFrame;
			if(Resampler != null)
				Resampler.Dispose();
			Resampler = null;
			api = null;
		}
	}
}
