using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[VideoWriterIgnore]
	public class AudioStretcher : AVStretcher
	{
		public AudioStretcher(IVideoWriter w)
		{
			this.w = w;
		}

		private long _soundRemainder; // audio timekeeping for video dumping

		public void DumpAV(IVideoProvider v, ISoundProvider asyncSoundProvider, out short[] samples, out int samplesprovided)
		{
			// Sound refactor TODO: we could try set it here, but we want the client to be responsible for mode switching? There may be non-trivial complications with when to switch modes that we don't want this object worrying about
			if (asyncSoundProvider.SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Only async mode is supported, set async mode before passing in the sound provider");
			}

			if (!aset || !vset)
				throw new InvalidOperationException("Must set params first!");

			long nsampnum = samplerate * (long)fpsden + _soundRemainder;
			long nsamp = nsampnum / fpsnum;

			// exactly remember fractional parts of an audio sample
			_soundRemainder = nsampnum % fpsnum;

			samples = new short[nsamp * channels];
			asyncSoundProvider.GetSamplesAsync(samples);
			samplesprovided = (int)nsamp;

			w.AddFrame(v);
			w.AddSamples(samples);
		}
	}

	[VideoWriterIgnore]
	public class VideoStretcher : AVStretcher
	{
		public VideoStretcher(IVideoWriter w)
		{
			this.w = w;
		}

		private short[] _samples = new short[0];

		// how many extra audio samples there are (* fpsnum)
		private long exaudio_num;

		private bool pset = false;
		private long threshone;
		private long threshmore;
		private long threshtotal;

		private void VerifyParams()
		{
			if (!aset || !vset)
				throw new InvalidOperationException("Must set params first!");

			if (!pset)
			{
				pset = true;

				// each video frame committed counts as (fpsden * samplerate / fpsnum) audio samples
				threshtotal = fpsden * (long)samplerate;

				// blah blah blah
				threshone = (long)(threshtotal * 0.4);
				threshmore = (long)(threshtotal * 0.9);
			}
		}

		public void DumpAV(IVideoProvider v, ISoundProvider syncSoundProvider, out short[] samples, out int samplesprovided)
		{
			// Sound refactor TODO: we could just set it here, but we want the client to be responsible for mode switching? There may be non-trivial complications with when to switch modes that we don't want this object worrying about
			if (syncSoundProvider.SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only sync mode is supported, set sync mode before passing in the sound provider");
			}

			VerifyParams();
			syncSoundProvider.GetSamplesSync(out samples, out samplesprovided);
			exaudio_num += samplesprovided * (long)fpsnum;

			// todo: scan for duplicate frames (ie, video content exactly matches previous frame) and for them, skip the threshone step
			// this is a good idea, but expensive on time.  is it worth it?

			if (exaudio_num >= threshone)
			{
				// add frame once
				w.AddFrame(v);
				exaudio_num -= threshtotal;
			}
			else
			{
				Console.WriteLine("Dropped Frame!");
			}
			while (exaudio_num >= threshmore)
			{
				// add frame again!
				w.AddFrame(v);
				exaudio_num -= threshtotal;
				Console.WriteLine("Dupped Frame!");
			}

			// a bit of hackey due to the fact that this api can't read a
			// usable buffer length separately from the actual length of the buffer
			if (samples.Length == samplesprovided * channels)
			{
				w.AddSamples(samples);
			}
			else
			{
				if (_samples.Length != samplesprovided * channels)
					_samples = new short[samplesprovided * channels];

				Buffer.BlockCopy(samples, 0, _samples, 0, samplesprovided * channels * sizeof(short));
				w.AddSamples(_samples);
			}
		}
	}

	public abstract class AVStretcher : VWWrap, IVideoWriter
	{
		protected int fpsnum;
		protected int fpsden;
		protected bool vset = false;

		protected int samplerate;
		protected int channels;
		protected int bits;
		protected bool aset = false;


		public new virtual void SetMovieParameters(int fpsnum, int fpsden)
		{
			if (vset)
				throw new InvalidOperationException();
			vset = true;
			this.fpsnum = fpsnum;
			this.fpsden = fpsden;

			base.SetMovieParameters(fpsnum, fpsden);
		}

		public new virtual void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			if (aset)
				throw new InvalidOperationException();
			if (bits != 16)
				throw new InvalidOperationException("Only 16 bit audio is supported!");
			aset = true;
			this.samplerate = sampleRate;
			this.channels = channels;
			this.bits = bits;

			base.SetAudioParameters(sampleRate, channels, bits);
		}

		public new virtual void SetFrame(int frame)
		{
			// this writer will never support this capability
		}

		public new virtual void AddFrame(IVideoProvider source)
		{
			throw new InvalidOperationException("Must call AddAV()!");
		}

		public new virtual void AddSamples(short[] samples)
		{
			throw new InvalidOperationException("Must call AddAV()!");
		}

	}

	public abstract class VWWrap : IVideoWriter
	{
		protected IVideoWriter w;

		public bool UsesAudio { get { return w.UsesAudio; } }
		public bool UsesVideo { get { return w.UsesVideo; } }

		public void SetVideoCodecToken(IDisposable token)
		{
			w.SetVideoCodecToken(token);
		}

		public void SetDefaultVideoCodecToken()
		{
			w.SetDefaultVideoCodecToken();
		}

		public void OpenFile(string baseName)
		{
			w.OpenFile(baseName);
		}

		public void CloseFile()
		{
			w.CloseFile();
		}

		public void SetFrame(int frame)
		{
			w.SetFrame(frame);
		}

		public void AddFrame(IVideoProvider source)
		{
			w.AddFrame(source);
		}

		public void AddSamples(short[] samples)
		{
			w.AddSamples(samples);
		}

		public IDisposable AcquireVideoCodecToken(System.Windows.Forms.IWin32Window hwnd)
		{
			return w.AcquireVideoCodecToken(hwnd);
		}

		public void SetMovieParameters(int fpsnum, int fpsden)
		{
			w.SetMovieParameters(fpsnum, fpsden);
		}

		public void SetVideoParameters(int width, int height)
		{
			w.SetVideoParameters(width, height);
		}

		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			w.SetAudioParameters(sampleRate, channels, bits);
		}

		public void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
			w.SetMetaData(gameName, authors, lengthMS, rerecords);
		}

		public string DesiredExtension()
		{
			return w.DesiredExtension();
		}

		public void Dispose()
		{
			w.Dispose();
		}
	}
}
