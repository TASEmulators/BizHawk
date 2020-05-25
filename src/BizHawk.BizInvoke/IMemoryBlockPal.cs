using System;

namespace BizHawk.BizInvoke
{
	/// <summary>
	/// Platform abstraction layer over mmap like functionality
	/// </summary>
	public interface IMemoryBlockPal : IDisposable
	{
		/// <summary>
		/// Map in the memory area at the predetermined address
		/// </summary>
		void Activate();
		/// <summary>
		/// Unmap the memory area
		/// </summary>
		void Deactivate();
		/// <summary>
		/// Change protection on some addresses, guaranteed to be page aligned and in the memory area
		/// </summary>
		void Protect(ulong start, ulong size, MemoryBlock.Protection prot);
	}
}
