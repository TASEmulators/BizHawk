using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BizHawk.Emulation.Cores.Waterbox
{
	/// <summary>
	/// a heap that supports basic alloc, free, and realloc calls
	/// </summary>
	internal sealed class MapHeap : IBinaryStateable, IDisposable
	{
		public MemoryBlock Memory { get; private set; }
		/// <summary>
		/// name, used in identifying errors
		/// </summary>
		public string Name { get; private set; }

		public byte[] XorHash { get; private set; }

		/// <summary>
		/// get a page index within the block
		/// </summary>
		private int GetPage(ulong addr)
		{
			return (int)((addr - Memory.Start) >> WaterboxUtils.PageShift);
		}

		/// <summary>
		/// get a start address for a page index within the block
		/// </summary>
		private ulong GetStartAddr(int page)
		{
			return ((ulong)page << WaterboxUtils.PageShift) + Memory.Start;
		}

		private class Bin
		{
			public int StartPage;
			public int PageCount;
			public MemoryBlock.Protection Protection;
			public bool Free
			{
				get
				{
					return (byte)Protection == 255;
				}
				set
				{
					Protection = value ? (MemoryBlock.Protection)255 : MemoryBlock.Protection.None;
				}
			}

			public Bin Next;

			/// <summary>
			/// split this bin, keeping only numPages pages
			/// </summary>
			public bool Cleave(int numPages)
			{
				int nextPages = PageCount - numPages;
				if (nextPages > 0)
				{
					Next = new Bin
					{
						StartPage = StartPage + numPages,
						PageCount = nextPages,
						Next = Next
					};
					PageCount = numPages;
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		private Bin _root;

		public MapHeap(ulong start, ulong size, string name)
		{
			size = WaterboxUtils.AlignUp(size);
			Memory = new MemoryBlock(start, size);
			Name = name;
			Console.WriteLine("Created mapheap `{1}` at {0:x16}:{2:x16}", start, name, start + size);

			_root = new Bin
			{
				StartPage = 0,
				PageCount = (int)(size >> WaterboxUtils.PageShift),
				Free = true
			};
		}

		/// <summary>
		/// gets the bin that contains a page
		/// </summary>
		private Bin GetBinForStartPage(int page)
		{
			Bin curr = _root;
			while (curr.StartPage + curr.PageCount <= page)
				curr = curr.Next;
			return curr;
		}

		/// <summary>
		/// gets the bin that contains the page before the passed page, returning null if 
		/// any bin along the way is Free
		/// </summary>
		private Bin GetBinForEndPageEnsureAllocated(int page, Bin start)
		{
			Bin curr = start;
			while (curr != null && curr.StartPage + curr.PageCount < page)
			{
				if (curr.Free)
					return null;
				curr = curr.Next;
			}
			return curr;
		}

		public ulong Map(ulong size, MemoryBlock.Protection prot)
		{
			int numPages = WaterboxUtils.PagesNeeded(size);
			Bin best = null;
			Bin curr = _root;

			// find smallest potential bin
			do
			{
				if (curr.Free && curr.PageCount >= numPages)
				{
					if (best == null || curr.PageCount < best.PageCount)
					{
						best = curr;
						if (curr.PageCount == numPages)
							break;
					}
				}
				curr = curr.Next;
			} while (curr != null);

			if (best == null)
				return 0;

			if (best.Cleave(numPages))
				best.Next.Free = true;
			best.Protection = prot;

			var ret = GetStartAddr(best.StartPage);
			Memory.Protect(ret, ((ulong)numPages) << WaterboxUtils.PageShift, prot);
			return ret;
		}

		public ulong Remap(ulong start, ulong oldSize, ulong newSize, bool canMove)
		{
			if (start < Memory.Start || start + oldSize > Memory.End)
				return 0;

			var oldStartPage = GetPage(start);
			var oldStartBin = GetBinForStartPage(oldStartPage);
			if (oldSize == 0 && canMove)
				return Map(newSize, oldStartBin.Protection);

			var oldNumPages = WaterboxUtils.PagesNeeded(oldSize);
			var oldEndPage = oldStartPage + oldNumPages;
			// first, check if the requested area is actually mapped
			var oldEndBin = GetBinForEndPageEnsureAllocated(oldEndPage, oldStartBin);
			if (oldEndBin == null)
				return 0;

			var newNumPages = WaterboxUtils.PagesNeeded(newSize);
			var newEndPage = oldStartPage + newNumPages;
			if (newEndPage > oldEndPage)
			{
				// increase size
				// the only way this will work in place is if all of the remaining space is free
				Bin nextBin;
				if (oldEndBin.StartPage + oldEndBin.PageCount == oldEndPage // if end bin is too bag, space after that is used by something else
					&& (nextBin = oldEndBin.Next) != null // can't go off the edge
					&& nextBin.Free
					&& nextBin.StartPage + nextBin.PageCount >= newEndPage)
				{
					nextBin.Protection = oldStartBin.Protection;
					if (nextBin.Cleave(newEndPage - nextBin.StartPage))
						nextBin.Next.Free = true;
					return start;
				}
				// could not increase in place, so move
				if (!canMove)
					return 0;

				// if there's some free space right before `start`, and some right after, but not enough
				// to extend in place, it's possible that a realloc would succeed reusing the same space,
				// but would fail anywhere else due to heavy memory pressure.

				// that would be a much more complicated algorithm; we'd need to compute a new allocation
				// as if this one had been freed, but still be able to preserve this if that allocation
				// still failed.  instead, we ignore this case.
				var ret = Map(newSize, oldStartBin.Protection);
				if (ret != 0)
				{
					// move data
					// NB: oldSize > 0
					Memory.Protect(start, oldSize, MemoryBlock.Protection.R);
					var ss = Memory.GetStream(start, oldSize, false);
					Memory.Protect(ret, oldSize, MemoryBlock.Protection.RW);
					var ds = Memory.GetStream(ret, oldSize, true);
					ss.CopyTo(ds);
					Memory.Protect(ret, oldSize, oldStartBin.Protection);
					UnmapPagesInternal(oldStartPage, oldNumPages, oldStartBin);
					return ret;
				}
				else
				{
					return 0;
				}
			}
			else if (newEndPage < oldEndPage)
			{
				// shrink in place
				var s = GetBinForStartPage(newEndPage);
				UnmapPagesInternal(newEndPage, oldEndPage - newEndPage, s);
				return start;
			}
			else
			{
				// no change
				return start;
			}
		}

		public bool Unmap(ulong start, ulong size)
		{
			if (start < Memory.Start || start + size > Memory.End)
				return false;
			if (size == 0)
				return true;

			var startPage = GetPage(start);
			var numPages = WaterboxUtils.PagesNeeded(size);
			var endPage = startPage + numPages;
			// check to see if the requested area is actually mapped
			var startBin = GetBinForStartPage(startPage);
			if (GetBinForEndPageEnsureAllocated(endPage, startBin) == null)
				return false;

			UnmapPagesInternal(startPage, numPages, startBin);
			return true;
		}

		/// <summary>
		/// frees some pages.  assumes they are all allocated
		/// </summary>
		private void UnmapPagesInternal(int startPage, int numPages, Bin startBin)
		{
			// from the various paths we took to get here, we must be unmapping at least one page

			var endPage = startPage + numPages;
			Bin freeBin = startBin;
			if (!freeBin.Free && freeBin.StartPage != startPage)
			{
				freeBin.Cleave(startPage - freeBin.StartPage);
				freeBin = freeBin.Next;
				freeBin.Free = true;
			}
			MemoryBlock.Protection lastEaten = MemoryBlock.Protection.None;
			while (freeBin.StartPage + freeBin.PageCount < endPage)
			{
				freeBin.PageCount += freeBin.Next.PageCount;
				lastEaten = freeBin.Next.Protection;
				freeBin.Next = freeBin.Next.Next;
			}
			if (freeBin.Cleave(freeBin.StartPage + freeBin.PageCount - endPage))
			{
				freeBin.Next.Protection = lastEaten;
			}
			Memory.Protect(GetStartAddr(startPage), ((ulong)numPages) << WaterboxUtils.PageShift, MemoryBlock.Protection.None);
		}

		public void Dispose()
		{
			if (Memory != null)
			{
				Memory.Dispose();
				Memory = null;
			}
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
		}

		public void LoadStateBinary(BinaryReader reader)
		{
		}
	}
}
