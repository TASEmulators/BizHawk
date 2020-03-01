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
	/// turns an <seealso cref="IAsyncSoundProvider"/> into a full ISoundProvider
	/// This is used in cores that have an async only sound implementation
	/// better is implement a sync sound option in those cores without the need for 
	/// this class or an IAsyncSoundProvider interface
	/// </summary>
	internal class FakeSyncSound : ISoundProvider
	{
		private readonly IAsyncSoundProvider _source;
		private readonly int _spf;

		/// <summary>
		/// Initializes a new instance of the <see cref="FakeSyncSound"/> class. 
		/// </summary>
		/// <param name="source">The async sound provider</param>
		/// <param name="spf">number of sample pairs to request and provide on each GetSamples() call</param>
		public FakeSyncSound(IAsyncSoundProvider source, int spf)
		{
			_source = source;
			_spf = spf;
			SyncMode = SyncSoundMode.Sync;
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Must be in sync mode to call a sync method");
			}

			short[] ret = new short[_spf * 2];
			_source.GetSamples(ret);
			samples = ret;
			nsamp = _spf;
		}

		public void DiscardSamples()
		{
			_source.DiscardSamples();
		}

		public void GetSamplesAsync(short[] samples)
		{
			if (SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Must be in async mode to call an async method");
			}

			_source.GetSamples(samples);
		}

		public bool CanProvideAsync => true;

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
			Buffer = Metaspu.MetaspuConstruct(method);
		}

		public ISynchronizingAudioBuffer Buffer { get; }

		public bool CanProvideAsync => true;

		public SyncSoundMode SyncMode => SyncSoundMode.Async;

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
			Buffer.OutputSamples(samples, samples.Length / 2);
		}

		public void DiscardSamples()
		{
			Buffer.Clear();
		}
	}
}
