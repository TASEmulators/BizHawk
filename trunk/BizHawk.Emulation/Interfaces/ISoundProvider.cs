namespace BizHawk
{
    public interface ISoundProvider
    {
        void GetSamples(short[] samples);
		void DiscardSamples();
    }
}
