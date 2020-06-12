using System;
using BizHawk.BizInvoke;
using BizHawk.Common;

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable StyleCop.SA1300
// ReSharper disable InconsistentNaming
namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// junk wrapper around LibSpeexDSP.  quite inefficient.  will be replaced
	/// </summary>
	public class SpeexResampler : IDisposable, ISoundProvider
	{
		private static readonly LibSpeexDSP NativeDSP;
		private static readonly IImportResolver NativeDLL;
		static SpeexResampler()
		{
			NativeDLL = new DynamicLibraryImportResolver(OSTailoredCode.IsUnixHost ? "libspeexdsp.so.1" : "libspeexdsp.dll");
			NativeDSP = BizInvoker.GetInvoker<LibSpeexDSP>(NativeDLL, CallingConventionAdapters.Native);
		}

		// to accept an ISyncSoundProvider input
		private readonly ISoundProvider _input;

		// function to call to dispatch output
		private readonly Action<short[], int> _drainer;

		// TODO: this size is roughly based on how big you can make the buffer before the snes resampling (32040.5 -> 44100) gets screwed up
		private readonly short[] _inbuf = new short[512]; // [8192]; // [512];

		/// <summary>
		/// quality of the resampler.  values other than those listed are valid, provided they are between MIN and MAX
		/// </summary>
		public enum Quality
		{
			QUALITY_MAX = 10,
			QUALITY_MIN = 0,
			QUALITY_DEFAULT = 4,
			QUALITY_VOIP = 3,
			QUALITY_DESKTOP = 5
		}

		// opaque pointer to state
		private IntPtr _st = IntPtr.Zero;

		private short[] _outbuf;

		// for sync
		private short[] _outbuf2 = new short[16];
		private int _outbuf2pos;

		// in buffer position in samples (not sample pairs)
		private int _inbufpos;

		/// <summary>
		/// throw an exception based on error state
		/// </summary>
		private static void CheckError(LibSpeexDSP.RESAMPLER_ERR e)
		{
			switch (e)
			{
				case LibSpeexDSP.RESAMPLER_ERR.SUCCESS:
					return;
				case LibSpeexDSP.RESAMPLER_ERR.ALLOC_FAILED:
					throw new InsufficientMemoryException($"{nameof(LibSpeexDSP)}: Alloc failed");
				case LibSpeexDSP.RESAMPLER_ERR.BAD_STATE:
					throw new Exception($"{nameof(LibSpeexDSP)}: Bad state");
				case LibSpeexDSP.RESAMPLER_ERR.INVALID_ARG:
					throw new ArgumentException($"{nameof(LibSpeexDSP)}: Bad Argument");
				case LibSpeexDSP.RESAMPLER_ERR.PTR_OVERLAP:
					throw new Exception($"{nameof(LibSpeexDSP)}: Buffers cannot overlap");
			}
		}

		/// <param name="quality">0 to 10</param>
		/// <param name="rationum">numerator of sample rate change ratio (inrate / outrate)</param>
		/// <param name="ratioden">denominator of sample rate change ratio (inrate / outrate)</param>
		/// <param name="sratein">sampling rate in, rounded to nearest hz</param>
		/// <param name="srateout">sampling rate out, rounded to nearest hz</param>
		/// <param name="drainer">function which accepts output as produced. if null, act as an <seealso cref="ISoundProvider"/></param>
		/// <param name="input">source to take input from when output is requested. if null, no auto-fetching</param>
		/// <exception cref="ArgumentException"><paramref name="drainer"/> and <paramref name="input"/> are both non-null</exception>
		/// <exception cref="Exception">unmanaged call failed</exception>
		public SpeexResampler(Quality quality, uint rationum, uint ratioden, uint sratein, uint srateout, Action<short[], int> drainer = null, ISoundProvider input = null)
		{
			if (drainer != null && input != null)
			{
				throw new ArgumentException($"Can't autofetch without being an {nameof(ISoundProvider)}?");
			}

			var err = LibSpeexDSP.RESAMPLER_ERR.SUCCESS;
			_st = NativeDSP.speex_resampler_init_frac(2, rationum, ratioden, sratein, srateout, quality, ref err);

			if (_st == IntPtr.Zero)
			{
				throw new Exception($"{nameof(LibSpeexDSP)} returned null!");
			}

			CheckError(err);

			_drainer = drainer ?? InternalDrain;
			_input = input;

			_outbuf = new short[(_inbuf.Length * ratioden / rationum / 2 * 2) + 128];
		}

		/// <summary>change sampling rate on the fly</summary>
		/// <param name="rationum">numerator of sample rate change ratio (inrate / outrate)</param>
		/// <param name="ratioden">denominator of sample rate change ratio (inrate / outrate)</param>
		/// <param name="sratein">sampling rate in, rounded to nearest hz</param>
		/// <param name="srateout">sampling rate out, rounded to nearest hz</param>
		public void ChangeRate(uint rationum, uint ratioden, uint sratein, uint srateout)
		{
			CheckError(NativeDSP.speex_resampler_set_rate_frac(_st, rationum, ratioden, sratein, srateout));
			_outbuf = new short[(_inbuf.Length * ratioden / rationum / 2 * 2) + 128];
		}

		/// <summary>
		/// add a sample to the queue
		/// </summary>
		public void EnqueueSample(short left, short right)
		{
			_inbuf[_inbufpos++] = left;
			_inbuf[_inbufpos++] = right;

			if (_inbufpos == _inbuf.Length)
			{
				Flush();
			}
		}

		/// <summary>
		/// add multiple samples to the queue
		/// </summary>
		/// <param name="userbuf">interleaved stereo samples</param>
		/// <param name="nsamp">number of sample pairs</param>
		public void EnqueueSamples(short[] userbuf, int nsamp)
		{
			int numused = 0;
			while (numused < nsamp)
			{
				int shortstocopy = Math.Min(_inbuf.Length - _inbufpos, (nsamp - numused) * 2);

				Buffer.BlockCopy(userbuf, numused * 2 * sizeof(short), _inbuf, _inbufpos * sizeof(short), shortstocopy * sizeof(short));
				_inbufpos += shortstocopy;
				numused += shortstocopy / 2;

				if (_inbufpos == _inbuf.Length)
				{
					Flush();
				}
			}
		}

		/// <summary>flush as many input samples as possible, generating output samples right now</summary>
		/// <exception cref="Exception">unmanaged call failed</exception>
		public void Flush()
		{
			uint inal = (uint)_inbufpos / 2;

			uint outal = (uint)_outbuf.Length / 2;

			NativeDSP.speex_resampler_process_interleaved_int(_st, _inbuf, ref inal, _outbuf, ref outal);

			// reset inbuf
			if (inal != _inbufpos / 2)
			{
				throw new Exception("Speexresampler didn't eat the whole array?");
			}

			_inbufpos = 0;
			_drainer(_outbuf, (int)outal);
		}

		public void Dispose()
		{
			if (_st != IntPtr.Zero)
			{
				NativeDSP.speex_resampler_destroy(_st);
				_st = IntPtr.Zero;
				GC.SuppressFinalize(this);
			}
		}

		~SpeexResampler()
		{
			Dispose();
		}

		private void InternalDrain(short[] buf, int nsamp)
		{
			if (_outbuf2pos + (nsamp * 2) > _outbuf2.Length)
			{
				short[] newbuf = new short[_outbuf2pos + (nsamp * 2)];
				Buffer.BlockCopy(_outbuf2, 0, newbuf, 0, _outbuf2pos * sizeof(short));
				_outbuf2 = newbuf;
			}

			Buffer.BlockCopy(buf, 0, _outbuf2, _outbuf2pos * sizeof(short), nsamp * 2 * sizeof(short));
			_outbuf2pos += nsamp * 2;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (_input != null)
			{
				_input.GetSamplesSync(out var sampin, out int nsampin);
				EnqueueSamples(sampin, nsampin);
			}

			Flush();
			nsamp = _outbuf2pos / 2;
			samples = _outbuf2;
			_outbuf2pos = 0;
		}

		public void DiscardSamples()
		{
			_outbuf2pos = 0;
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		/// <exception cref="InvalidOperationException">always</exception>
		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		/// <exception cref="NotSupportedException"><paramref name="mode"/> is <see cref="SyncSoundMode.Async"/></exception>
		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}
	}
}
