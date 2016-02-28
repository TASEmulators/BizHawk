namespace BizHawk.Emulation.Common
{
	public interface ISoundProvider
	{
		void GetSamples(short[] samples);
		void DiscardSamples();

		// TODO: we want to remove this property. Clients do not need this information.  This is only used by cores themselves, they should use their own interface/implementation to pass this information around
		int MaxVolume { get; set; }
	}
}
