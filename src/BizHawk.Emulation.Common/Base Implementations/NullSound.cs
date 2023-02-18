using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A default and empty implementation of ISoundProvider
	/// that simply outputs "silence"
	/// </summary>
	/// <seealso cref="ISoundProvider" />
	public class NullSound : IAsyncSoundProvider, ISoundProvider, ISyncSoundProvider
	{
		private readonly long _spfNumerator;
		private readonly long _spfDenominator;
		private long _remainder;
		private short[] _buff = Array.Empty<short>();

		/// <summary>
		/// Initializes a new instance of the <see cref="NullSound"/> class
		/// that provides an exact number of audio samples per call when in sync mode
		/// </summary>
		public NullSound(int spf)
		{
			_spfNumerator = spf;
			_spfDenominator = 1;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NullSound"/> class
		/// that exactly matches a given frame rate when in sync mode
		/// </summary>
		public NullSound(long fpsNum, long fpsDen)
		{
			_spfNumerator = fpsDen * 44100;
			_spfDenominator = fpsNum;
		}

		private SyncSoundMode SyncMode = SyncSoundMode.Sync;

		/// <exception cref="InvalidOperationException"><see cref="SyncMode"/> is not <see cref="SyncSoundMode.Sync"/> (call <see cref="AsSyncProvider"/>)</exception>
		public void GetSyncSoundSamples(out short[] samples, out int nsamp)
		{
			if (SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Wrong sound mode");
			}

			int s = (int)((_spfNumerator + _remainder) / _spfDenominator);
			_remainder = (_spfNumerator + _remainder) % _spfDenominator;

			if (_buff.Length < s * 2)
			{
				_buff = new short[s * 2];
			}

			samples = _buff;
			nsamp = s;
		}

		public void DiscardSamples()
		{
		}

		/// <exception cref="InvalidOperationException"><see cref="SyncMode"/> is not <see cref="SyncSoundMode.Async"/> (call <see cref="AsAsyncProvider"/>)</exception>
		public void GetAsyncSoundSamples(short[] samples)
		{
			if (SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Wrong sound mode");
			}

			Array.Clear(samples, 0, samples.Length);
		}

		public IAsyncSoundProvider AsAsyncProvider()
		{
			SyncMode = SyncSoundMode.Async;
			return this;
		}

		public ISyncSoundProvider AsSyncProvider()
		{
			SyncMode = SyncSoundMode.Sync;
			return this;
		}
	}
}
