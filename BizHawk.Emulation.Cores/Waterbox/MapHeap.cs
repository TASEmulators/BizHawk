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

		/// <summary>
		/// total number of bytes allocated
		/// </summary>
		public ulong Used { get; private set; }

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
			/// <summary>
			/// first page# in this bin, inclusive
			/// </summary>
			public int StartPage;
			/// <summary>
			/// numbe of pages in this bin
			/// </summary>
			public int PageCount;
			/// <summary>
			/// first page# not in this bin
			/// </summary>
			public int EndPage => StartPage + PageCount;
			public MemoryBlock.Protection Protection;
			/// <summary>
			/// true if not mapped (we distinguish between PROT_NONE and not mapped)
			/// </summary>
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

			/// <summary>
			/// activate the protection specified by this block
			/// </summary>
			public void ApplyProtection(MemoryBlock m)
			{
				var prot = Free ? MemoryBlock.Protection.None : Protection;
				var start = ((ulong)StartPage << WaterboxUtils.PageShift) + m.Start;
				var length = (ulong)PageCount << WaterboxUtils.PageShift;
				m.Protect(start, length, prot);
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
			while (curr != null)
			{
				if (curr.Free)
					return null;
				if (curr.EndPage >= page)
					return curr;
				curr = curr.Next;
			}
			return curr; // ran off the end
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
			var totalSize = ((ulong)numPages) << WaterboxUtils.PageShift;
			Memory.Protect(ret, totalSize, prot);
			Used += totalSize;
			Console.WriteLine($"Allocated {totalSize} bytes on {Name}, utilization {Used}/{Memory.Size} ({100.0 * Used / Memory.Size:0.#}%)");
			//EnsureUsedInternal();
			return ret;
		}

		public ulong Remap(ulong start, ulong oldSize, ulong newSize, bool canMove)
		{
			// TODO: what is the expected behavior when everything requested for remap is allocated,
			// but with different protections?

			if (start < Memory.Start || start + oldSize > Memory.End)
				return 0;

			var oldStartPage = GetPage(start);
			var oldStartBin = GetBinForStartPage(oldStartPage);
			if (oldSize == 0 && canMove)
			{
				if (oldStartBin.Free)
					return 0;
				else
					return Map(newSize, oldStartBin.Protection);
			}

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
				if (oldEndBin.EndPage == oldEndPage // if end bin is too bag, space after that is used by something else
					&& (nextBin = oldEndBin.Next) != null // can't go off the edge
					&& nextBin.Free
					&& nextBin.EndPage >= newEndPage)
				{
					nextBin.Protection = oldStartBin.Protection;
					if (nextBin.Cleave(newEndPage - nextBin.StartPage))
						nextBin.Next.Free = true;

					nextBin.ApplyProtection(Memory);

					var oldTotalSize = ((ulong)oldNumPages) << WaterboxUtils.PageShift;
					var newTotalSize = ((ulong)newNumPages) << WaterboxUtils.PageShift;
					Used += newTotalSize;
					Used -= oldTotalSize;
					Console.WriteLine($"Reallocated from {oldTotalSize} bytes to {newTotalSize} bytes on {Name}, utilization {Used}/{Memory.Size} ({100.0 * Used / Memory.Size:0.#}%)");
					//EnsureUsedInternal();
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
					//EnsureUsedInternal();
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
				//EnsureUsedInternal();
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
			if (freeBin.StartPage != startPage)
			{
				freeBin.Cleave(startPage - freeBin.StartPage);
				freeBin = freeBin.Next;
			}
			freeBin.Free = true;
			MemoryBlock.Protection lastEaten = MemoryBlock.Protection.None;
			while (freeBin.EndPage < endPage)
			{
				freeBin.PageCount += freeBin.Next.PageCount;
				lastEaten = freeBin.Next.Protection;
				freeBin.Next = freeBin.Next.Next;
			}
			if (freeBin.Cleave(endPage - freeBin.StartPage))
			{
				freeBin.Next.Protection = lastEaten;
			}
			freeBin.ApplyProtection(Memory);

			var totalSize = ((ulong)numPages) << WaterboxUtils.PageShift;
			Used -= totalSize;
			Console.WriteLine($"Freed {totalSize} bytes on {Name}, utilization {Used}/{Memory.Size} ({100.0 * Used / Memory.Size:0.#}%)");
			//EnsureUsedInternal();
		}

		public void Dispose()
		{
			if (Memory != null)
			{
				Memory.Dispose();
				Memory = null;
			}
		}

		private ulong CalcUsedInternal()
		{
			ulong ret = 0;
			var bin = _root;
			while (bin != null)
			{
				if (!bin.Free)
					ret += (ulong)bin.PageCount << WaterboxUtils.PageShift;
				bin = bin.Next;
			}
			return ret;
		}

		private void EnsureUsedInternal()
		{
			if (Used != CalcUsedInternal())
				throw new Exception();
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			bw.Write(Name);
			bw.Write(Memory.Size);
			bw.Write(Used);
			bw.Write(Memory.XorHash);
			var bin = _root;
			do
			{
				bw.Write(bin.PageCount);
				bw.Write((byte)bin.Protection);
				if (!bin.Free)
				{
					var start = GetStartAddr(bin.StartPage);
					var length = (ulong)bin.PageCount << WaterboxUtils.PageShift;
					if (bin.Protection == MemoryBlock.Protection.None)
						Memory.Protect(start, length, MemoryBlock.Protection.R);
					Memory.GetXorStream(start, length, false).CopyTo(bw.BaseStream);
					if (bin.Protection == MemoryBlock.Protection.None)
						Memory.Protect(start, length, MemoryBlock.Protection.None);
				}
				bin = bin.Next;
			} while (bin != null);
			bw.Write(-1);
		}

		public void LoadStateBinary(BinaryReader br)
		{
			var name = br.ReadString();
			if (name != Name)
				throw new InvalidOperationException(string.Format("Name did not match for mapheap {0}", Name));
			var size = br.ReadUInt64();
			if (size != Memory.Size)
				throw new InvalidOperationException(string.Format("Size did not match for mapheap {0}", Name));
			var used = br.ReadUInt64();
			var hash = br.ReadBytes(Memory.XorHash.Length);
			if (!hash.SequenceEqual(Memory.XorHash))
				throw new InvalidOperationException(string.Format("Hash did not match for mapheap {0}.  Is this the same rom?", Name));

			Used = 0;

			int startPage = 0;
			int pageCount;
			Bin scratch = new Bin(), curr = scratch;
			while ((pageCount = br.ReadInt32()) != -1)
			{
				var next = new Bin
				{
					StartPage = startPage,
					PageCount = pageCount,
					Protection = (MemoryBlock.Protection)br.ReadByte()
				};
				startPage += pageCount;
				if (!next.Free)
				{
					var start = GetStartAddr(next.StartPage);
					var length = (ulong)pageCount << WaterboxUtils.PageShift;
					Memory.Protect(start, length, MemoryBlock.Protection.RW);
					WaterboxUtils.CopySome(br.BaseStream, Memory.GetXorStream(start, length, true), (long)length);
					Used += length;
				}
				next.ApplyProtection(Memory);
				curr.Next = next;
				curr = next;
			}

			if (used != Used)
				throw new InvalidOperationException(string.Format("Inernal error loading mapheap {0}", Name));

			_root = scratch.Next;
		}

		public static void StressTest()
		{
			var allocs = new Dictionary<ulong, ulong>();
			var mmo = new MapHeap(0x36a00000000, 256 * 1024 * 1024, "ballsacks");
			var rnd = new Random(12512);

			for (int i = 0; i < 40; i++)
			{
				ulong siz = (ulong)(rnd.Next(256 * 1024) + 384 * 1024);
				siz = siz / 4096 * 4096;
				var ptr = mmo.Map(siz, Waterbox.MemoryBlock.Protection.RW);
				allocs.Add(ptr, siz);
			}

			for (int i = 0; i < 20; i++)
			{
				int idx = rnd.Next(allocs.Count);
				var elt = allocs.ElementAt(idx);
				mmo.Unmap(elt.Key, elt.Value);
				allocs.Remove(elt.Key);
			}

			for (int i = 0; i < 40; i++)
			{
				ulong siz = (ulong)(rnd.Next(256 * 1024) + 384 * 1024);
				siz = siz / 4096 * 4096;
				var ptr = mmo.Map(siz, Waterbox.MemoryBlock.Protection.RW);
				allocs.Add(ptr, siz);
			}

			for (int i = 0; i < 20; i++)
			{
				int idx = rnd.Next(allocs.Count);
				var elt = allocs.ElementAt(idx);
				mmo.Unmap(elt.Key, elt.Value);
			}
		}
	}
}
