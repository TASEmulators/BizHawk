namespace BizHawk.Common
{
	/// <summary>
	/// Platform abstraction layer over mmap like functionality
	/// </summary>
	public interface IMemoryBlockPal : IDisposable
	{
		public ulong Start { get; }

		/// <summary>
		/// Change protection on [start, start + size), guaranteed to be page aligned and in the allocated area
		/// </summary>
		void Protect(ulong start, ulong size, MemoryBlock.Protection prot);
	}
}
