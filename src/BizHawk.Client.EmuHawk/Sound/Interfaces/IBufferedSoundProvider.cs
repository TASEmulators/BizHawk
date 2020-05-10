using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IBufferedSoundProvider
	{
		/// <summary>
		/// The source audio provider.
		/// </summary>
		ISoundProvider BaseSoundProvider { get; set; }

		/// <summary>
		/// Clears any internally buffered samples, and discards samples from the base provider (if set).
		/// </summary>
		void DiscardSamples();
	}
}
