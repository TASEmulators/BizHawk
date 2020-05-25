using System;

namespace BizHawk.BizInvoke
{
	/// <summary>
	/// Platform abstraction layer over mmap like functionality
	/// </summary>
	public interface IMemoryBlockPal : IDisposable
	{
		/// <summary>
		/// Map in the memory area at the predetermined address.  uncommitted space should be unreadable.
		/// For all other space, there is no requirement on initial protection value;
		/// correct protections will be applied via Protect() immediately after this call.
		/// </summary>
		void Activate();
		/// <summary>
		/// Unmap the memory area from memory.  All data needs to be preserved for next load.
		/// </summary>
		void Deactivate();
		/// <summary>
		/// Change protection on [start, start + size), guaranteed to be page aligned and in the committed area.
		/// Will only be called when active.
		/// </summary>
		void Protect(ulong start, ulong size, MemoryBlock.Protection prot);
		/// <summary>
		/// mark [Block.Start, Block.Start + length) as committed.  Always greater than a previous length;
		/// no uncommitting is allowed.
		/// Will only be called when active.
		/// there is no requirement on initial protection value of any committed memory (newly or otherwise)
		/// after this call; protections will be applied via Protect() immediately after this call.
		/// </summary>
		void Commit(ulong length);
	}
}
