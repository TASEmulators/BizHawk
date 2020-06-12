using System;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using static BizHawk.Emulation.Common.SpeexResampler;

namespace BizHawk.Emulation.Common
{
	public abstract class LibSpeexDSP
	{
		public enum RESAMPLER_ERR
		{
			SUCCESS = 0,
			ALLOC_FAILED = 1,
			BAD_STATE = 2,
			INVALID_ARG = 3,
			PTR_OVERLAP = 4,
			MAX_ERROR
		}

		/// <summary>
		/// Create a new resampler with integer input and output rates.
		/// </summary>
		/// <param name="nb_channels">Number of channels to be processed</param>
		/// <param name="in_rate">Input sampling rate (integer number of Hz).</param>
		/// <param name="out_rate">Output sampling rate (integer number of Hz).</param>
		/// <param name="quality">Resampling quality between 0 and 10, where 0 has poor quality and 10 has very high quality.</param>
		/// <param name="err">The error state</param>
		/// <returns>Newly created resampler state</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr speex_resampler_init(uint nb_channels, uint in_rate, uint out_rate, Quality quality, ref RESAMPLER_ERR err);

		/// <summary>
		/// Create a new resampler with fractional input/output rates. The sampling
		/// rate ratio is an arbitrary rational number with both the numerator and
		/// denominator being 32-bit integers.
		/// </summary>
		/// <param name="nb_channels">Number of channels to be processed</param>
		/// <param name="ratio_num">Numerator of the sampling rate ratio</param>
		/// <param name="ratio_den">Denominator of the sampling rate ratio</param>
		/// <param name="in_rate">Input sampling rate rounded to the nearest integer (in Hz).</param>
		/// <param name="out_rate">Output sampling rate rounded to the nearest integer (in Hz).</param>
		/// <param name="quality">Resampling quality between 0 and 10, where 0 has poor quality and 10 has very high quality.</param>
		/// <param name="err">The error state</param>
		/// <returns>Newly created resampler state</returns>
		[BizImport(CallingConvention.Cdecl)]
		public abstract IntPtr speex_resampler_init_frac(uint nb_channels, uint ratio_num, uint ratio_den, uint in_rate, uint out_rate, Quality quality, ref RESAMPLER_ERR err);

		/// <summary>
		/// Destroy a resampler state.
		/// </summary>
		/// <param name="st">Resampler state</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_destroy(IntPtr st);

		/// <summary>
		/// Resample a float array. The input and output buffers must *not* overlap.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="channel_index">Index of the channel to process for the multi-channel base (0 otherwise)</param>
		/// <param name="inp">Input buffer</param>
		/// <param name="in_len">Number of input samples in the input buffer. Returns the number of samples processed</param>
		/// <param name="outp">Output buffer</param>
		/// <param name="out_len">Size of the output buffer. Returns the number of samples written</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_process_float(IntPtr st, uint channel_index, float[] inp, ref uint in_len, float[] outp, ref uint out_len);

		/// <summary>
		/// Resample an int array. The input and output buffers must *not* overlap.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="channel_index">Index of the channel to process for the multi-channel base (0 otherwise)</param>
		/// <param name="inp">Input buffer</param>
		/// <param name="in_len">Number of input samples in the input buffer. Returns the number of samples processed</param>
		/// <param name="outp">Output buffer</param>
		/// <param name="out_len">Size of the output buffer. Returns the number of samples written</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_process_int(IntPtr st, uint channel_index, short[] inp, ref uint in_len, short[] outp, ref uint out_len);

		/// <summary>
		/// Resample an interleaved float array. The input and output buffers must *not* overlap.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="inp">Input buffer</param>
		/// <param name="in_len">Number of input samples in the input buffer. Returns the number of samples processed. This is all per-channel.</param>
		/// <param name="outp">Output buffer</param>
		/// <param name="out_len">Size of the output buffer. Returns the number of samples written. This is all per-channel.</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_process_interleaved_float(IntPtr st, float[] inp, ref uint in_len, float[] outp, ref uint out_len);

		/// <summary>
		/// Resample an interleaved int array. The input and output buffers must *not* overlap.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="inp">Input buffer</param>
		/// <param name="in_len">Number of input samples in the input buffer. Returns the number of samples processed. This is all per-channel.</param>
		/// <param name="outp">Output buffer</param>
		/// <param name="out_len">Size of the output buffer. Returns the number of samples written. This is all per-channel.</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_process_interleaved_int(IntPtr st, short[] inp, ref uint in_len, short[] outp, ref uint out_len);

		/// <summary>
		/// Set (change) the input/output sampling rates (integer value).
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="in_rate">Input sampling rate (integer number of Hz).</param>
		/// <param name="out_rate">Output sampling rate (integer number of Hz).</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_set_rate(IntPtr st, uint in_rate, uint out_rate);

		/// <summary>
		/// Get the current input/output sampling rates (integer value).
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="in_rate">Input sampling rate (integer number of Hz) copied.</param>
		/// <param name="out_rate">Output sampling rate (integer number of Hz) copied.</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_get_rate(IntPtr st, ref uint in_rate, ref uint out_rate);

		/// <summary>
		/// Set (change) the input/output sampling rates and resampling ratio (fractional values in Hz supported).
		/// </summary>
		/// <param name="st">resampler state</param>
		/// <param name="ratio_num">Numerator of the sampling rate ratio</param>
		/// <param name="ratio_den">Denominator of the sampling rate ratio</param>
		/// <param name="in_rate">Input sampling rate rounded to the nearest integer (in Hz).</param>
		/// <param name="out_rate">Output sampling rate rounded to the nearest integer (in Hz).</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_set_rate_frac(IntPtr st, uint ratio_num, uint ratio_den, uint in_rate, uint out_rate);

		/// <summary>
		/// Get the current resampling ratio. This will be reduced to the least common denominator.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="ratio_num">Numerator of the sampling rate ratio copied</param>
		/// <param name="ratio_den">Denominator of the sampling rate ratio copied</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_get_ratio(IntPtr st, ref uint ratio_num, ref uint ratio_den);

		/// <summary>
		/// Set (change) the conversion quality.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="quality">Resampling quality between 0 and 10, where 0 has poor quality and 10 has very high quality.</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_set_quality(IntPtr st, Quality quality);

		/// <summary>
		/// Get the conversion quality.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="quality">Resampling quality between 0 and 10, where 0 has poor quality and 10 has very high quality.</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_get_quality(IntPtr st, ref Quality quality);

		/// <summary>
		/// Set (change) the input stride.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="stride">Input stride</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_set_input_stride(IntPtr st, uint stride);

		/// <summary>
		/// Get the input stride.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="stride">Input stride copied</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_get_input_stride(IntPtr st, ref uint stride);

		/// <summary>
		/// Set (change) the output stride.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="stride">Output stride</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_set_output_stride(IntPtr st, uint stride);

		/// <summary>
		/// Get the output stride.
		/// </summary>
		/// <param name="st">Resampler state</param>
		/// <param name="stride">Output stride copied</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract void speex_resampler_get_output_stride(IntPtr st, ref uint stride);

		/*these two functions don't exist in our version of the dll

		/// <summary>
		/// Get the latency in input samples introduced by the resampler.
		/// </summary>
		/// <param name="st">Resampler state</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract int speex_resampler_get_input_latency(IntPtr st);

		/// <summary>
		/// Get the latency in output samples introduced by the resampler.
		/// </summary>
		/// <param name="st">Resampler state</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract int speex_resampler_get_output_latency(IntPtr st);

		*/

		/// <summary>
		/// Make sure that the first samples to go out of the resampler don't have
		/// leading zeros. This is only useful before starting to use a newly created
		/// resampler. It is recommended to use that when resampling an audio file, as
		/// it will generate a file with the same length. For real-time processing,
		/// it is probably easier not to use this call (so that the output duration
		/// is the same for the first frame).
		/// </summary>
		/// <param name="st">Resampler state</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_skip_zeros(IntPtr st);

		/// <summary>
		/// Reset a resampler so a new (unrelated) stream can be processed.
		/// </summary>
		/// <param name="st">Resampler state</param>
		[BizImport(CallingConvention.Cdecl)]
		public abstract RESAMPLER_ERR speex_resampler_reset_mem(IntPtr st);

		/// <summary>
		/// Returns the English meaning for an error code
		/// </summary>
		/// <param name="err">Error code</param>
		/// <returns>English string</returns>
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract string speex_resampler_strerror(RESAMPLER_ERR err);
	}
}
