namespace BizHawk.Emulation.Common
{
	public interface ISoundProvider
	{
		void GetSamples(short[] samples);
		void DiscardSamples();
		int MaxVolume { get; set; }
	}
}
