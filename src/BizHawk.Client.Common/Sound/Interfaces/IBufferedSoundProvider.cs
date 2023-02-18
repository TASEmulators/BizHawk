using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IBufferedSoundProvider
	{
		/// <summary>
		/// The source audio provider.
		/// </summary>
		ISoundProviderBase BaseSoundProvider { get; set; }

		/// <summary>
		/// Clears any internally buffered samples, and discards samples from the base provider (if set).
		/// </summary>
		void DiscardSamples();
	}
}
