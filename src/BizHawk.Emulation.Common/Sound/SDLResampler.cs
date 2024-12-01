#nullable disable

using System.Runtime.InteropServices;

using static SDL2.SDL;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Wrapper against SDL's resampler
	/// </summary>
	public class SDLResampler : IDisposable, ISoundProvider
	{
		// to accept an ISyncSoundProvider input
		private readonly ISoundProvider _input;

		// function to call to dispatch output
		private readonly Action<short[], int> _drainer;

		private short[] _outBuf = Array.Empty<short>();

		// for ISoundProvider output use
		private short[] _outSamples = Array.Empty<short>();
		private int _outNumSamps;

		// opaque pointer to SDL_AudioStream
		private IntPtr _stream;

		/// <param name="src_rate">sampling rate in by hz</param>
		/// <param name="dst_rate">sampling rate out by hz</param>
		/// <param name="drainer">function which accepts output as produced. if null, act as an <see cref="ISoundProvider"/></param>
		/// <param name="input">source to take input from when output is requested. if null, no auto-fetching</param>
		/// <exception cref="ArgumentException"><paramref name="drainer"/> and <paramref name="input"/> are both non-null</exception>
		/// <exception cref="Exception">unmanaged call failed</exception>
		public SDLResampler(int src_rate, int dst_rate, Action<short[], int> drainer = null, ISoundProvider input = null)
		{
			if (drainer is not null && input is not null) throw new ArgumentException(message: $"Can't autofetch without being an {nameof(ISoundProvider)}?", paramName: nameof(input));

			_stream = SDL_NewAudioStream(AUDIO_S16SYS, 2, src_rate, AUDIO_S16SYS, 2, dst_rate);

			if (_stream == IntPtr.Zero)
			{
				throw new($"{nameof(SDL_NewAudioStream)} returned null! SDL error: {SDL_GetError()}");
			}

			_drainer = drainer ?? InternalDrain;
			_input = input;
		}

		// SDL bindings don't have this for some reason :(
		[DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
		private static extern int SDL_AudioStreamFlush(IntPtr stream);

		/// <summary>change sampling rate on the fly</summary>
		/// <param name="src_rate">sampling rate in by hz</param>
		/// <param name="dst_rate">sampling rate out by hz</param>
		public void ChangeRate(int src_rate, int dst_rate)
		{
			// force flush the stream, as we'll be destroying it to change the sample rate...
			if (SDL_AudioStreamFlush(_stream) != 0)
			{
				throw new($"{nameof(SDL_AudioStreamFlush)} failed! SDL error: {SDL_GetError()}");
			}

			Flush();

			SDL_FreeAudioStream(_stream);
			_stream = SDL_NewAudioStream(AUDIO_S16SYS, 2, src_rate, AUDIO_S16SYS, 2, dst_rate);

			if (_stream == IntPtr.Zero)
			{
				throw new($"{nameof(SDL_NewAudioStream)} returned null! SDL error: {SDL_GetError()}");
			}
		}

		/// <summary>
		/// add multiple samples to the queue
		/// </summary>
		/// <param name="userbuf">interleaved stereo samples</param>
		/// <param name="nsamp">number of sample pairs</param>
		public unsafe void EnqueueSamples(short[] userbuf, int nsamp)
		{
			if (userbuf.Length < nsamp * 2)
			{
				throw new("User buffer contained less than nsamp * 2 shorts!");
			}

			fixed (short* ub = userbuf)
			{
				if (SDL_AudioStreamPut(_stream, (IntPtr)ub, nsamp * 4) != 0)
				{
					throw new($"{nameof(SDL_AudioStreamPut)} failed! SDL error: {SDL_GetError()}");
				}
			}
		}

		/// <summary>flush as many input samples as possible, generating output samples right now</summary>
		/// <exception cref="Exception">unmanaged call failed</exception>
		public unsafe void Flush()
		{
			var streamAvail = SDL_AudioStreamAvailable(_stream);
			if (streamAvail == 0)
			{
				return;
			}

			// stereo s16 audio always has 4 bytes per interleaved sample
			if (streamAvail % 4 != 0)
			{
				throw new("SDL audio stream contained partial sample frames?");
			}

			if (streamAvail / 2 > _outBuf.Length)
			{
				_outBuf = new short[streamAvail / 2];
			}

			fixed (short* outBuf = _outBuf)
			{
				var numRead = SDL_AudioStreamGet(_stream, (IntPtr)outBuf, streamAvail);
				if (numRead == -1)
				{
					throw new($"{nameof(SDL_AudioStreamGet)} failed! SDL error: {SDL_GetError()}");
				}

				if (numRead != streamAvail)
				{
					throw new($"{nameof(SDL_AudioStreamGet)} didn't eat the whole array?");
				}
			}

			_drainer(_outBuf, streamAvail / 4);
		}

		public void Dispose()
		{
			if (_stream != IntPtr.Zero)
			{
				SDL_FreeAudioStream(_stream);
				_stream = IntPtr.Zero;
			}
		}

		private void InternalDrain(short[] buf, int nsamp)
		{
			if (_outNumSamps + nsamp * 2 > _outSamples.Length)
			{
				var newBuf = new short[_outNumSamps + nsamp * 2];
				Buffer.BlockCopy(_outSamples, 0, newBuf, 0, _outNumSamps * sizeof(short));
				_outSamples = newBuf;
			}

			Buffer.BlockCopy(buf, 0, _outSamples, _outNumSamps * sizeof(short), nsamp * 2 * sizeof(short));
			_outNumSamps += nsamp * 2;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (_input != null)
			{
				_input.GetSamplesSync(out var sampin, out var nsampin);
				EnqueueSamples(sampin, nsampin);
			}

			Flush();
			nsamp = _outNumSamps / 2;
			samples = _outSamples;
			_outNumSamps = 0;
		}

		public void DiscardSamples()
		{
			_outNumSamps = 0;
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
