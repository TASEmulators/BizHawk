using System;

namespace BizHawk.Emulation.Common.Base_Implementations
{
	/// <summary>
	/// A simple sound provider that will operate in sync mode only, offering back whatever data was sent in PutSamples
	/// </summary>
	public class SimpleSyncSoundProvider : ISyncSoundProvider
	{
		private short[] _buffer = Array.Empty<short>();
		private int _nsamp;

		/// <summary>
		/// Add samples to be output.  no queueing; must be drained every frame
		/// </summary>
		public void PutSamples(short[] samples, int nsamp)
		{
			if (_nsamp != 0)
			{
				Console.WriteLine($"Warning: Samples disappeared from {nameof(SimpleSyncSoundProvider)}");
			}

			if (_buffer.Length < nsamp * 2)
			{
				_buffer = new short[nsamp * 2];
			}

			Array.Copy(samples, _buffer, nsamp * 2);
			_nsamp = nsamp;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _buffer;
			nsamp = _nsamp;
			_nsamp = 0;
		}

		public void DiscardSamples()
		{
			_nsamp = 0;
		}
	}
}
