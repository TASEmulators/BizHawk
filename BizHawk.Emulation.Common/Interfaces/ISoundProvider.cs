namespace BizHawk.Emulation.Common
{
	public interface IAsyncSoundProvider
	{
		void GetSamples(short[] samples);
		void DiscardSamples();
	}
}
