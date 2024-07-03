namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Presently, an IBlob doesn't need to work multithreadedly. It's quite an onerous demand.
	/// This should probably be managed by the Disc class somehow, or by the user making another Disc.
	/// </summary>
	public interface IBlob : IDisposable
	{
		/// <summary>
		/// what a weird parameter order. normally the dest buffer would be first. weird.
		/// </summary>
		/// <param name="byte_pos">location in the blob to read from</param>
		/// <param name="buffer">destination buffer for read data</param>
		/// <param name="offset">offset into destination buffer</param>
		/// <param name="count">amount to read</param>
		int Read(long byte_pos, byte[] buffer, int offset, int count);
	}
}