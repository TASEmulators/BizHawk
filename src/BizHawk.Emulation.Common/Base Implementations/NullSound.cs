namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A default and empty implementation of ISoundProvider
	/// that simply outputs "silence"
	/// </summary>
	/// <seealso cref="ISoundProvider" />
	public class NullSound : ISoundProvider
	{
		private readonly long _spfNumerator;
		private readonly long _spfDenominator;
		private long _remainder;
		private short[] _buff = Array.Empty<short>();

		private NullSound()
		{
			SyncMode = SyncSoundMode.Sync;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NullSound"/> class
		/// that provides an exact number of audio samples per call when in sync mode
		/// </summary>
		public NullSound(int spf)
			: this()
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

		public bool CanProvideAsync => true;

		public SyncSoundMode SyncMode { get; private set; }

		/// <exception cref="InvalidOperationException"><see cref="SyncMode"/> is not <see cref="SyncSoundMode.Sync"/> (call <see cref="SetSyncMode"/>)</exception>
		public void GetSamplesSync(out short[] samples, out int nsamp)
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

		public void SetSyncMode(SyncSoundMode mode)
		{
			SyncMode = mode;
		}

		/// <exception cref="InvalidOperationException"><see cref="SyncMode"/> is not <see cref="SyncSoundMode.Async"/> (call <see cref="SetSyncMode"/>)</exception>
		public void GetSamplesAsync(short[] samples)
		{
			if (SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Wrong sound mode");
			}

			Array.Clear(samples, 0, samples.Length);
		}
	}
}
