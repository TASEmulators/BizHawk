using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Sound.Utilities
{
	/// <summary>
	/// wrapper around blargg's unmanaged blip_buf
	/// </summary>
	public class BlipBuffer : IDisposable
	{
		// this is transitional only.  if the band-limited synthesis idea works out, i'll
		// make a managed MIT implementation

		static class BlipBufDll
		{
			/** Creates new buffer that can hold at most sample_count samples. Sets rates
			so that there are blip_max_ratio clocks per sample. Returns pointer to new
			buffer, or NULL if insufficient memory. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr blip_new(int sample_count);

			/** Sets approximate input clock rate and output sample rate. For every
			clock_rate input clocks, approximately sample_rate samples are generated. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_set_rates(IntPtr context, double clock_rate, double sample_rate);

			/** Maximum clock_rate/sample_rate ratio. For a given sample_rate,
			clock_rate must not be greater than sample_rate*blip_max_ratio. */
			public const int blip_max_ratio = 1 << 20;

			/** Clears entire buffer. Afterwards, blip_samples_avail() == 0. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_clear(IntPtr context);

			/** Adds positive/negative delta into buffer at specified clock time. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_add_delta(IntPtr context, uint clock_time, int delta);

			/** Same as blip_add_delta(), but uses faster, lower-quality synthesis. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_add_delta_fast(IntPtr context, uint clock_time, int delta);

			/** Length of time frame, in clocks, needed to make sample_count additional
			samples available. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int blip_clocks_needed(IntPtr context, int sample_count);

			/** Maximum number of samples that can be generated from one time frame. */
			public const int blip_max_frame = 4000;

			/** Makes input clocks before clock_duration available for reading as output
			samples. Also begins new time frame at clock_duration, so that clock time 0 in
			the new time frame specifies the same clock as clock_duration in the old time
			frame specified. Deltas can have been added slightly past clock_duration (up to
			however many clocks there are in two output samples). */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_end_frame(IntPtr context, uint clock_duration);

			/** Number of buffered samples available for reading. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int blip_samples_avail(IntPtr context);

			/** Reads and removes at most 'count' samples and writes them to 'out'. If
			'stereo' is true, writes output to every other element of 'out', allowing easy
			interleaving of two buffers into a stereo sample stream. Outputs 16-bit signed
			samples. Returns number of samples actually read.  */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern int blip_read_samples(IntPtr context, short[] @out, int count, int stereo);

			/** Frees buffer. No effect if NULL is passed. */
			[DllImport("blip_buf.dll", CallingConvention = CallingConvention.Cdecl)]
			public static extern void blip_delete(IntPtr context);
		}

		IntPtr context;

		public BlipBuffer(int sample_count)
		{
			context = BlipBufDll.blip_new(sample_count);
			if (context == IntPtr.Zero)
				throw new Exception("blip_new returned NULL!");
		}

		public void Dispose()
		{
			BlipBufDll.blip_delete(context);
			context = IntPtr.Zero;
		}

		public void SetRates(double clock_rate, double sample_rate)
		{
			BlipBufDll.blip_set_rates(context, clock_rate, sample_rate);
		}

		public const int MaxRatio = BlipBufDll.blip_max_ratio;

		public void Clear()
		{
			BlipBufDll.blip_clear(context);
		}


		public void AddDelta(uint clock_time, int delta)
		{
			BlipBufDll.blip_add_delta(context, clock_time, delta);
		}

		public void AddDeltaFast(uint clock_time, int delta)
		{
			BlipBufDll.blip_add_delta_fast(context, clock_time, delta);
		}

		public int ClocksNeeded(int sample_count)
		{
			return BlipBufDll.blip_clocks_needed(context, sample_count);
		}

		public const int MaxFrame = BlipBufDll.blip_max_frame;

		public void EndFrame(uint clock_duration)
		{
			BlipBufDll.blip_end_frame(context, clock_duration);
		}

		public int SamplesAvailable()
		{
			return BlipBufDll.blip_samples_avail(context);
		}

		public int ReadSamples(short[] output, int count, bool stereo)
		{
			if (output.Length < count * (stereo ? 2 : 1))
				throw new ArgumentOutOfRangeException();
			return BlipBufDll.blip_read_samples(context, output, count, stereo ? 1 : 0);
		}
	}
}
