using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class AudioStretcher : AVStretcher
	{
		public AudioStretcher(IVideoWriter w)
		{
			this.W = w;
		}

		private long _soundRemainder; // audio timekeeping for video dumping

		/// <exception cref="InvalidOperationException">
		/// <paramref name="asyncSoundProvider"/>'s mode is not <see cref="SyncSoundMode.Async"/>, or
		/// A/V parameters haven't been set (need to call <see cref="AVStretcher.SetAudioParameters"/> and <see cref="AVStretcher.SetMovieParameters"/>)
		/// </exception>
		public void DumpAV(IVideoProvider v, ISoundProvider asyncSoundProvider, out short[] samples, out int samplesProvided)
		{
			// Sound refactor TODO: we could try set it here, but we want the client to be responsible for mode switching? There may be non-trivial complications with when to switch modes that we don't want this object worrying about
			if (asyncSoundProvider.SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Only async mode is supported, set async mode before passing in the sound provider");
			}

			if (!ASet || !VSet)
				throw new InvalidOperationException("Must set params first!");

			long nSampNum = Samplerate * (long)FpsDen + _soundRemainder;
			long nsamp = nSampNum / FpsNum;

			// exactly remember fractional parts of an audio sample
			_soundRemainder = nSampNum % FpsNum;

			samples = new short[nsamp * Channels];
			asyncSoundProvider.GetSamplesAsync(samples);
			samplesProvided = (int)nsamp;

			W.AddFrame(v);
			W.AddSamples(samples);
		}
	}

	public class VideoStretcher : AVStretcher
	{
		public VideoStretcher(IVideoWriter w)
		{
			W = w;
		}

		private short[] _samples = Array.Empty<short>();

		// how many extra audio samples there are (* fpsNum)
		private long _exAudioNum;

		private bool _pSet;
		private long _threshOne;
		private long _threshMore;
		private long _threshTotal;

		private void VerifyParams()
		{
			if (!ASet || !VSet)
			{
				throw new InvalidOperationException("Must set params first!");
			}

			if (!_pSet)
			{
				_pSet = true;

				// each video frame committed counts as (fpsDen * samplerate / fpsNum) audio samples
				_threshTotal = FpsDen * (long)Samplerate;

				// blah blah blah
				_threshOne = (long)(_threshTotal * 0.4);
				_threshMore = (long)(_threshTotal * 0.9);
			}
		}

		/// <exception cref="InvalidOperationException"><paramref name="syncSoundProvider"/>'s mode is not <see cref="SyncSoundMode.Sync"/></exception>
		public void DumpAV(IVideoProvider v, ISoundProvider syncSoundProvider, out short[] samples, out int samplesProvided)
		{
			// Sound refactor TODO: we could just set it here, but we want the client to be responsible for mode switching? There may be non-trivial complications with when to switch modes that we don't want this object worrying about
			if (syncSoundProvider.SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only sync mode is supported, set sync mode before passing in the sound provider");
			}

			VerifyParams();
			syncSoundProvider.GetSamplesSync(out samples, out samplesProvided);
			_exAudioNum += samplesProvided * (long)FpsNum;

			// todo: scan for duplicate frames (ie, video content exactly matches previous frame) and for them, skip the threshone step
			// this is a good idea, but expensive on time.  is it worth it?
			if (_exAudioNum >= _threshOne)
			{
				// add frame once
				W.AddFrame(v);
				_exAudioNum -= _threshTotal;
			}
			else
			{
				Console.WriteLine("Dropped Frame!");
			}
			while (_exAudioNum >= _threshMore)
			{
				// add frame again!
				W.AddFrame(v);
				_exAudioNum -= _threshTotal;
				Console.WriteLine("Dupped Frame!");
			}

			// a bit of hackey due to the fact that this api can't read a
			// usable buffer length separately from the actual length of the buffer
			if (samples.Length == samplesProvided * Channels)
			{
				W.AddSamples(samples);
			}
			else
			{
				if (_samples.Length != samplesProvided * Channels)
				{
					_samples = new short[samplesProvided * Channels];
				}

				Buffer.BlockCopy(samples, 0, _samples, 0, samplesProvided * Channels * sizeof(short));
				W.AddSamples(_samples);
			}
		}
	}

	public abstract class AVStretcher : VwWrap, IVideoWriter
	{
		protected int FpsNum;
		protected int FpsDen;
		protected bool VSet;

		protected int Samplerate;
		protected int Channels;
		protected int Bits;
		protected bool ASet;

		/// <exception cref="InvalidOperationException">already set</exception>
		public new virtual void SetMovieParameters(int fpsNum, int fpsDen)
		{
			if (VSet)
			{
				throw new InvalidOperationException();
			}

			VSet = true;
			FpsNum = fpsNum;
			FpsDen = fpsDen;

			base.SetMovieParameters(fpsNum, fpsDen);
		}

		/// <exception cref="InvalidOperationException">already set, or <paramref name="bits"/> is not <c>16</c></exception>
		public new virtual void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			if (ASet)
			{
				throw new InvalidOperationException();
			}

			if (bits != 16)
			{
				throw new InvalidOperationException("Only 16 bit audio is supported!");
			}

			ASet = true;
			Samplerate = sampleRate;
			Channels = channels;
			Bits = bits;

			base.SetAudioParameters(sampleRate, channels, bits);
		}

		public new virtual void SetFrame(int frame)
		{
			// this writer will never support this capability

			// but it needs to for syncless recorder, otherwise it won't work at all
			if (W is SynclessRecorder)
			{
				W.SetFrame(frame);
			}
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public new virtual void AddFrame(IVideoProvider source)
		{
			throw new InvalidOperationException("Must call AddAV()!");
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public new virtual void AddSamples(short[] samples)
		{
			throw new InvalidOperationException("Must call AddAV()!");
		}
	}

	public abstract class VwWrap : IVideoWriter
	{
		protected IVideoWriter W;

		public bool UsesAudio => W.UsesAudio;

		public bool UsesVideo => W.UsesVideo;

		public void SetVideoCodecToken(IDisposable token)
		{
			W.SetVideoCodecToken(token);
		}

		public void SetDefaultVideoCodecToken(Config config)
		{
			W.SetDefaultVideoCodecToken(config);
		}

		public void OpenFile(string baseName)
		{
			W.OpenFile(baseName);
		}

		public void CloseFile()
		{
			W.CloseFile();
		}

		public void SetFrame(int frame)
		{
			W.SetFrame(frame);
		}

		public void AddFrame(IVideoProvider source)
		{
			W.AddFrame(source);
		}

		public void AddSamples(short[] samples)
		{
			W.AddSamples(samples);
		}

		public IDisposable AcquireVideoCodecToken(Config config)
		{
			return W.AcquireVideoCodecToken(config);
		}

		public void SetMovieParameters(int fpsNum, int fpsDen)
		{
			W.SetMovieParameters(fpsNum, fpsDen);
		}

		public void SetVideoParameters(int width, int height)
		{
			W.SetVideoParameters(width, height);
		}

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			W.SetAudioParameters(sampleRate, channels, bits);
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMs, ulong rerecords)
		{
			W.SetMetaData(gameName, authors, lengthMs, rerecords);
		}

		public string DesiredExtension()
		{
			return W.DesiredExtension();
		}

		public void Dispose()
		{
			W.Dispose();
		}
	}
}
