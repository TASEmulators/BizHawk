namespace BizHawk
{
	public interface ISyncSoundProvider
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="samples"></param>
		/// <param name="nsamp">number of sample PAIRS available</param>
		void GetSamples(out short[] samples, out int nsamp);
		/// <summary>
		/// 
		/// </summary>
		void DiscardSamples();
	}

	public class FakeSyncSound : ISyncSoundProvider
	{
		ISoundProvider source;
		int spf;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="spf">number of sample pairs to request for each call</param>
		public FakeSyncSound(ISoundProvider source, int spf)
		{
			this.source = source;
			this.spf = spf;
		}

		public void GetSamples(out short[] samples, out int nsamp)
		{
			short[] ret = new short[spf * 2];
			source.GetSamples(ret);
			samples = ret;
			nsamp = spf;
		}

		public void DiscardSamples()
		{
			source.DiscardSamples();
		}
	}
}
