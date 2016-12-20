using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components
{
	/// <summary>
	/// This interface is for legacy sound implementations in some older cores
	/// This needs to go away, but is provided here, for now
	/// </summary>
	internal interface IAsyncSoundProvider
	{
		void GetSamples(short[] samples);
		void DiscardSamples();
	}

	/// <summary>
	/// TODO: this is a shim for now, and needs to go away
	/// turns an IAsyncSoundPRovider into a full ISoundProvider
	/// This is used in cores that have an async only sound implementation
	/// better is implement a sync sound option in those cores without the need for 
	/// this class or an IAsyncSoundPRovider interface
	/// </summary>
	internal class FakeSyncSound : ISoundProvider
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

	// An async sound provider
	// This class needs to go away, it takes an IAsyncSoundProvider
	// and is only used by legacy sound implementations
	internal class MetaspuSoundProvider : ISoundProvider
	{
		public MetaspuSoundProvider(ESynchMethod method)
		{
			Buffer = Metaspu.metaspu_construct(method);
		}

		public ISynchronizingAudioBuffer Buffer { get; set; }
		private readonly short[] pullBuffer = new short[1470];

		public MetaspuSoundProvider()
			: this(ESynchMethod.ESynchMethod_V)
		{
		}

		public void PullSamples(IAsyncSoundProvider source)
		{
			Array.Clear(pullBuffer, 0, 1470);
			source.GetSamples(pullBuffer);
			Buffer.enqueue_samples(pullBuffer, 735);
		}

		public bool CanProvideAsync
		{
			get { return true; }
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Async; }
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Async)
			{
				throw new NotSupportedException("Only Async mode is supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			throw new InvalidOperationException("Sync mode is not supported.");
		}

		public void GetSamplesAsync(short[] samples)
		{
			Buffer.output_samples(samples, samples.Length / 2);
		}

		public void DiscardSamples()
		{
			Buffer.clear();
		}
	}
}
