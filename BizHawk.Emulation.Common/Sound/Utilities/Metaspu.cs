using System;

namespace BizHawk.Emulation.Common
{
	public static class Metaspu
	{
		public static ISynchronizingAudioBuffer MetaspuConstruct(ESynchMethod method)
		{
			switch (method)
			{
				case ESynchMethod.ESynchMethod_Z:
					return new ZeromusSynchronizer();
				case ESynchMethod.ESynchMethod_N:
					return new NitsujaSynchronizer();
				case ESynchMethod.ESynchMethod_V:
					return new VecnaSynchronizer();
				default:
					return new NitsujaSynchronizer();
			}
		}
	}

	/// <summary>
	/// uses Metaspu to provide async sound to an ISoundProvider that does not provide its own async implementation
	/// </summary>
	// Sound Refactor TODO: rename me to MetaspuAsyncSoundProvider
	public class MetaspuAsync : ISoundProvider
	{
		private readonly ISynchronizingAudioBuffer _buffer;
		private readonly ISoundProvider _input;

		public MetaspuAsync(ISoundProvider input, ESynchMethod method)
		{
			input.SetSyncMode(SyncSoundMode.Sync);
			_buffer = Metaspu.MetaspuConstruct(method);
			_input = input;
		}

		public void GetSamplesAsync(short[] samples)
		{
			short[] sampin;
			int numsamp;
			_input.GetSamplesSync(out sampin, out numsamp);
			_buffer.EnqueueSamples(sampin, numsamp);
			_buffer.OutputSamples(samples, samples.Length / 2);
		}

		public void DiscardSamples()
		{
			_input.DiscardSamples();
			_buffer.Clear();
		}

		public bool CanProvideAsync => true;

		public SyncSoundMode SyncMode => SyncSoundMode.Async;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Async)
			{
				throw new NotSupportedException("Only Async mode is supported");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			throw new InvalidOperationException("Sync mode not supported");
		}
	}

	public enum ESynchMethod
	{
		ESynchMethod_N, // nitsuja's
		ESynchMethod_Z, // zero's
		////ESynchMethod_P, //PCSX2 spu2-x //ohno! not available yet in c#
		ESynchMethod_V // vecna
	}
}