using System;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This interface is for legacy sound implementations in some older cores
	/// This needs to go away, but is provided here, for now
	/// </summary>
	public interface IAsyncSoundProvider
	{
		void GetSamples(short[] samples);
		void DiscardSamples();
	}

	/// <summary>
	/// TODO: this is a shim for now
	/// turns an IAsyncSoundPRovider into a full ISoundProvider
	/// This is used in cores that have an async only sound implementation
	/// better is impleemnt a sync sound option in those cores without the need for 
	/// this class or an IAsyncSoundPRovider interface
	/// </summary>
	public class FakeSyncSound : ISoundProvider
	{
		private readonly IAsyncSoundProvider source;
		private readonly int spf;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="spf">number of sample pairs to request and provide on each GetSamples() call</param>
		public FakeSyncSound(IAsyncSoundProvider source, int spf)
		{
			this.source = source;
			this.spf = spf;
			SyncMode = SyncSoundMode.Sync;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Must be in sync mode to call a sync method");
			}

			short[] ret = new short[spf * 2];
			source.GetSamples(ret);
			samples = ret;
			nsamp = spf;
		}

		public void DiscardSamples()
		{
			source.DiscardSamples();
		}

		public void GetSamplesAsync(short[] samples)
		{
			if (SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Must be in async mode to call an async method");
			}

			source.GetSamples(samples);
		}

		public bool CanProvideAsync
		{
			get { return true; }
		}

		public SyncSoundMode SyncMode { get; private set; }

		public void SetSyncMode(SyncSoundMode mode)
		{
			SyncMode = mode;
		}
	}
}
