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
		/// There is no assumption for the values of WriteStatus either, which will be supplied immediately
		/// after this call
		/// </summary>
		void Activate();
		/// <summary>
		/// Unmap the memory area from memory.  All data needs to be preserved for next load.
		/// </summary>
		void Deactivate();
		/// <summary>
		/// Change protection on [start, start + size), guaranteed to be page aligned and in the committed area.
		/// Will only be called when active.  Will not be called with RW_Invisible, which is a front end artifact.
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
		/// <summary>
		/// Gets the current write detection status on each page in the block.  Pages marked with CanChange
		/// that are also committed and set to R, will not trigger a segmentation violation on write; instead
		/// automatically changing to RW and setting DidChange
		/// </summary>
		/// <param name="dest">Caller-owned array that the PAL will overwrite with page data</param>
		/// <param name="pagedata">
		/// Caller-owned array that should indicate which areas were set to RW_Stack.
		/// Will not be modified by callee.  Some implementations need this to get all of the correct information in dest.
		/// </param>
		void GetWriteStatus(WriteDetectionStatus[] dest, MemoryBlock.Protection[] pagedata);
		/// <summary>
		/// Sets the current write detection status on each page in the block.  Pages marked with CanChange
		/// that are also committed and set to R, will not trigger a segmentation violation on write; instead
		/// automatically changing to RW and setting DidChange
		/// </summary>
		/// <param name="src">Caller-owned array that the PAL will read data from into its internal buffers</param>
		void SetWriteStatus(WriteDetectionStatus[] src);
	}
	[Flags]
	public enum WriteDetectionStatus : byte
	{
		/// <summary>If set, the page will be allowed to transition from R to RW</summary>
		CanChange = 1,
		/// <summary>If set, the page transitioned from R to RW</summary>
		DidChange = 2
	}
}
