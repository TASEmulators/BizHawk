using System;

namespace BizHawk.Emulation.Common
{
	public static class Metaspu
	{
		public static ISynchronizingAudioBuffer MetaspuConstruct(ESynchMethod method)
		{
			switch (method)
			{
				case ESynchMethod.Zeromus:
					return new ZeromusSynchronizer();
				case ESynchMethod.Nitsuja:
					return new NitsujaSynchronizer();
				case ESynchMethod.Vecna:
					return new VecnaSynchronizer();
				default:
					return new NitsujaSynchronizer();
			}
		}
	}

	/// <summary>
	/// uses <seealso cref="Metaspu"/> to provide async sound to an <seealso cref="ISoundProvider"/> that does not provide its own async implementation
	/// </summary>
	public class MetaspuAsyncSoundProvider : ISoundProvider
	{
		private readonly ISynchronizingAudioBuffer _buffer;
		private readonly ISoundProvider _input;

		public MetaspuAsyncSoundProvider(ISoundProvider input, ESynchMethod method)
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

		/// <exception cref="NotSupportedException"><paramref name="mode"/> is not <see cref="SyncSoundMode.Async"/></exception>
		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Async)
			{
				throw new NotSupportedException("Only Async mode is supported");
			}
		}

		/// <exception cref="InvalidOperationException">always</exception>
		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			throw new InvalidOperationException("Sync mode not supported");
		}
	}

	public enum ESynchMethod
	{
		Nitsuja, // nitsuja's
		Zeromus, // zero's
		////PCSX2, //PCSX2 spu2-x //ohno! not available yet in c#
		Vecna // vecna
	}
}