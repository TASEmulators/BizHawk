using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common.Base_Implementations
{
	/// <summary>
	/// A simple sound provider that will operate in sync mode only, offering back whatever data was sent in PutSamples
	/// </summary>
	public class SimpleSyncSoundProvider : ISoundProvider
	{
		private short[] _buffer = new short[0];
		private int _nsamp = 0;

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
				throw new ArgumentException("Only supports Sync mode");
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		/// <summary>
		/// Add samples to be output.  no queueing; must be drained every frame
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="nsamp"></param>
		public void PutSamples(short[] samples, int nsamp)
		{
			if (_nsamp != 0)
				Console.WriteLine("Warning: Samples disappeared from SimpleSyncSoundProvider");

			if (_buffer.Length < nsamp * 2)
				_buffer = new short[nsamp * 2];
			Array.Copy(samples, _buffer, nsamp * 2);
			_nsamp = nsamp;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _buffer;
			nsamp = _nsamp;
			_nsamp = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotImplementedException();
		}

		public void DiscardSamples()
		{
			_nsamp = 0;
		}
	}
}
