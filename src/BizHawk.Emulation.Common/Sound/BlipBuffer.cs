#nullable disable

using System;
using System.Runtime.InteropServices;

// ReSharper disable StyleCop.SA1300
// ReSharper disable InconsistentNaming
namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// wrapper around blargg's unmanaged blip_buf
	/// </summary>
	public sealed class BlipBuffer : IDisposable
	{
		// this is transitional only.  if the band-limited synthesis idea works out, i'll
		// make a managed MIT implementation
		private static class BlipBufDll
		{
			/** Creates new buffer that can hold at most sample_count samples. Sets rates
			so that there are blip_max_ratio clocks per sample. Returns pointer to new
			buffer, or NULL if insufficient memory. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr blip_new(int sample_count);

			/** Sets approximate input clock rate and output sample rate. For every
			clock_rate input clocks, approximately sample_rate samples are generated. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_set_rates(IntPtr context, double clock_rate, double sample_rate);

			/** Maximum clock_rate/sample_rate ratio. For a given sample_rate,
			clock_rate must not be greater than sample_rate*blip_max_ratio. */
			public const int BlipMaxRatio = 1 << 20;

			/** Clears entire buffer. Afterwards, blip_samples_avail() == 0. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_clear(IntPtr context);

			/** Adds positive/negative delta into buffer at specified clock time. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_add_delta(IntPtr context, uint clock_time, int delta);

			/** Same as blip_add_delta(), but uses faster, lower-quality synthesis. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_add_delta_fast(IntPtr context, uint clock_time, int delta);

			/** Length of time frame, in clocks, needed to make sample_count additional
			samples available. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern int blip_clocks_needed(IntPtr context, int sample_count);

			/** Maximum number of samples that can be generated from one time frame. */
			public const int BlipMaxFrame = 4000;

			/** Makes input clocks before clock_duration available for reading as output
			samples. Also begins new time frame at clock_duration, so that clock time 0 in
			the new time frame specifies the same clock as clock_duration in the old time
			frame specified. Deltas can have been added slightly past clock_duration (up to
			however many clocks there are in two output samples). */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_end_frame(IntPtr context, uint clock_duration);

			/** Number of buffered samples available for reading. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern int blip_samples_avail(IntPtr context);

			/** Reads and removes at most 'count' samples and writes them to 'out'. If
			'stereo' is true, writes output to every other element of 'out', allowing easy
			interleaving of two buffers into a stereo sample stream. Outputs 16-bit signed
			samples. Returns number of samples actually read.  */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern int blip_read_samples(IntPtr context, short[] @out, int count, int stereo);
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern int blip_read_samples(IntPtr context, IntPtr @out, int count, int stereo);

			/** Frees buffer. No effect if NULL is passed. */
			[DllImport("blip_buf", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_delete(IntPtr context);
		}

		private IntPtr _context;

		/// <exception cref="Exception">unmanaged call failed</exception>
		public BlipBuffer(int sampleCount)
		{
			_context = BlipBufDll.blip_new(sampleCount);
			if (_context == IntPtr.Zero)
			{
				throw new Exception("blip_new returned NULL!");
			}
		}

		~BlipBuffer()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (_context != IntPtr.Zero)
			{
				BlipBufDll.blip_delete(_context);
				_context = IntPtr.Zero;
				GC.SuppressFinalize(this);
			}
		}

		public void SetRates(double clockRate, double sampleRate)
		{
			BlipBufDll.blip_set_rates(_context, clockRate, sampleRate);
		}

		public const int MaxRatio = BlipBufDll.BlipMaxRatio;

		public void Clear()
		{
			BlipBufDll.blip_clear(_context);
		}

		public void AddDelta(uint clockTime, int delta)
		{
			BlipBufDll.blip_add_delta(_context, clockTime, delta);
		}

		public void AddDeltaFast(uint clockTime, int delta)
		{
			BlipBufDll.blip_add_delta_fast(_context, clockTime, delta);
		}

		public int ClocksNeeded(int sampleCount)
		{
			return BlipBufDll.blip_clocks_needed(_context, sampleCount);
		}

		public const int MaxFrame = BlipBufDll.BlipMaxFrame;

		public void EndFrame(uint clockDuration)
		{
			BlipBufDll.blip_end_frame(_context, clockDuration);
		}

		public int SamplesAvailable()
		{
			return BlipBufDll.blip_samples_avail(_context);
		}

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="output"/> can't hold <paramref name="count"/> samples (or twice that if <paramref name="stereo"/> is <see langword="true"/>)</exception>
		public int ReadSamples(short[] output, int count, bool stereo)
		{
			if (output.Length < count * (stereo ? 2 : 1))
			{
				throw new ArgumentOutOfRangeException();
			}

			return BlipBufDll.blip_read_samples(_context, output, count, stereo ? 1 : 0);
		}

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="output"/> can't hold 2 * <paramref name="count"/> samples</exception>
		public int ReadSamplesLeft(short[] output, int count)
		{
			if (output.Length < count * 2)
			{
				throw new ArgumentOutOfRangeException();
			}

			return BlipBufDll.blip_read_samples(_context, output, count, 1);
		}

		/// <exception cref="ArgumentOutOfRangeException"><paramref name="output"/> can't hold 2 * <paramref name="count"/> samples</exception>
		public int ReadSamplesRight(short[] output, int count)
		{
			if (output.Length < count * 2)
			{
				throw new ArgumentOutOfRangeException();
			}

			unsafe
			{
				fixed (short* s = &output[1])
					return BlipBufDll.blip_read_samples(_context, new IntPtr(s), count, 1);
			}
		}
	}
}
