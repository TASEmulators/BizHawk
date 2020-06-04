using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

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
		public string Name { get; }

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

		/// <summary>
		/// Sentinel value used to indicate unmapped pages.
		/// Needed because mapped pages are allowed to be protected to None
		/// </summary>
		private const MemoryBlock.Protection FREE = (MemoryBlock.Protection)255;

		/// <summary>
		/// Bitmap of protections.  Similar to MemoryBlock._pageData (and unfortunately mirrors the same information),
		/// but also handles FREE
		/// </summary>
		private readonly MemoryBlock.Protection[] _pages;
		/// <summary>
		/// alias of _pages used for serialization
		/// </summary>
		private readonly byte[] _pagesAsBytes;

		public MapHeap(ulong start, ulong size, string name)
		{
			size = WaterboxUtils.AlignUp(size);
			Memory = MemoryBlock.Create(start, size);
			Name = name;
			_pagesAsBytes = new byte[size >> WaterboxUtils.PageShift];
			_pages = (MemoryBlock.Protection[])(object)_pagesAsBytes;
			for (var i = 0; i < _pages.Length; i++)
				_pages[i] = FREE;
			Console.WriteLine($"Created {nameof(MapHeap)} `{name}` at {start:x16}:{start + size:x16}");
		}

		// find consecutive unused pages to map
		private int FindConsecutiveFreePages(int count)
		{
			return FindConsecutiveFreePagesAssumingFreed(count, -1, -1);
		}

		// find consecutive unused pages to map, pretending that [startPage..startPage + numPages) is free
		// used in realloc
		private int FindConsecutiveFreePagesAssumingFreed(int count, int startPage, int numPages)
		{
			// TODO: I'm sure there are sublinear algorithms for this, if the right data is maintained
			var starts = new List<int>();
			var sizes = new List<int>();

			var currStart = 0;
			for (var i = 0; i <= _pages.Length; i++)
			{
				if (i == _pages.Length || _pages[i] != FREE && (i < startPage || i >= startPage + numPages))
				{
					if (currStart < i)
					{
						starts.Add(currStart);
						var size = i - currStart;
						if (size == count)
							return currStart;
						sizes.Add(i - currStart);
					}
					currStart = i + 1;
				}
			}
			// find smallest hole to reduce fragmentation
			// TODO: Is this needed?
			int bestIdx = -1;
			int bestSize = int.MaxValue;
			for (int i = 0; i < sizes.Count; i++)
			{
				if (sizes[i] < bestSize && sizes[i] >= count)
				{
					bestSize = sizes[i];
					bestIdx = i;
				}
			}
			if (bestIdx != -1)
				return starts[bestIdx];
			else
				return -1;
		}

		private void ProtectInternal(int startPage, int numPages, MemoryBlock.Protection prot, bool wasUsed)
		{
			for (var i = startPage; i < startPage + numPages; i++)
				_pages[i] = prot;

			ulong start = GetStartAddr(startPage);
			ulong length = ((ulong)numPages) << WaterboxUtils.PageShift;
			if (prot == FREE)
			{
				Memory.Protect(start, length, MemoryBlock.Protection.RW);
				WaterboxUtils.ZeroMemory(Z.US(start), (long)length);
				Memory.Protect(start, length, MemoryBlock.Protection.None);
				Used -= length;
				Console.WriteLine($"Freed {length} bytes on {Name}, utilization {Used}/{Memory.Size} ({100.0 * Used / Memory.Size:0.#}%)");
			}
			else
			{
				Memory.Protect(start, length, prot);
				if (wasUsed)
				{
					Console.WriteLine($"Set protection for {length} bytes on {Name} to {prot}");
				}
				else
				{
					Used += length;
					Console.WriteLine($"Allocated {length} bytes on {Name}, utilization {Used}/{Memory.Size} ({100.0 * Used / Memory.Size:0.#}%)");
				}
			}
		}

		private void RefreshProtections(int startPage, int pageCount)
		{
			int ps = startPage;
			for (int i = startPage; i < startPage + pageCount; i++)
			{
				if (i == startPage + pageCount - 1 || _pages[i] != _pages[i + 1])
				{
					var p = _pages[i];
					ulong zstart = GetStartAddr(ps);
					ulong zlength = (ulong)(i - ps + 1) << WaterboxUtils.PageShift;
					Memory.Protect(zstart, zlength, p == FREE ? MemoryBlock.Protection.None : p);
					ps = i + 1;
				}
			}
		}

		private void RefreshAllProtections()
		{
			RefreshProtections(0, _pages.Length);
		}

		private bool EnsureMapped(int startPage, int pageCount)
		{
			for (int i = startPage; i < startPage + pageCount; i++)
			{
				if (_pages[i] == FREE)
					return false;
			}
			return true;
		}
		private bool EnsureMappedNonStack(int startPage, int pageCount)
		{
			for (int i = startPage; i < startPage + pageCount; i++)
			{
				if (_pages[i] == FREE || _pages[i] == MemoryBlock.Protection.RW_Stack)
					return false;
			}
			return true;
		}

		public ulong Map(ulong size, MemoryBlock.Protection prot)
		{
			if (size == 0)
				return 0;
			int numPages = WaterboxUtils.PagesNeeded(size);
			int startPage = FindConsecutiveFreePages(numPages);
			if (startPage == -1)
				return 0;
			var ret = GetStartAddr(startPage);
			ProtectInternal(startPage, numPages, prot, false);
			return ret;
		}

		public ulong Remap(ulong start, ulong oldSize, ulong newSize, bool canMove)
		{
			// TODO: what is the expected behavior when everything requested for remap is allocated,
			// but with different protections?
			if (start < Memory.Start || start + oldSize > Memory.EndExclusive || oldSize == 0 || newSize == 0)
				return 0;

			var oldStartPage = GetPage(start);
			var oldNumPages = WaterboxUtils.PagesNeeded(oldSize);
			if (!EnsureMappedNonStack(oldStartPage, oldNumPages))
				return 0;
			var oldProt = _pages[oldStartPage];

			int newNumPages = WaterboxUtils.PagesNeeded(newSize);

			if (!canMove)
			{
				if (newNumPages <= oldNumPages)
				{
					if (newNumPages < oldNumPages)
						ProtectInternal(oldStartPage + newNumPages, oldNumPages - newNumPages, FREE, true);
					return start;
				}
				else if (newNumPages > oldNumPages)
				{
					for (var i = oldStartPage + oldNumPages; i < oldStartPage + newNumPages; i++)
						if (_pages[i] != FREE)
							return 0;
					ProtectInternal(oldStartPage + oldNumPages, newNumPages - oldNumPages, oldProt, false);
					return start;
				}
			}

			// if moving is allowed, we always move to simplify and defragment when possible
			int newStartPage = FindConsecutiveFreePagesAssumingFreed(newNumPages, oldStartPage, oldNumPages);
			if (newStartPage == -1)
				return 0;

			var copyDataLen = Math.Min(oldSize, newSize);
			var copyPageLen = Math.Min(oldNumPages, newNumPages);

			var data = new byte[copyDataLen];
			Memory.Protect(start, copyDataLen, MemoryBlock.Protection.RW);
			Marshal.Copy(Z.US(start), data, 0, (int)copyDataLen);

			var pages = new MemoryBlock.Protection[copyPageLen];
			Array.Copy(_pages, oldStartPage, pages, 0, copyPageLen);

			ProtectInternal(oldStartPage, oldNumPages, FREE, true);
			ProtectInternal(newStartPage, newNumPages, MemoryBlock.Protection.RW, false);

			var ret = GetStartAddr(newStartPage);
			Marshal.Copy(data, 0, Z.US(ret), (int)copyDataLen);

			Array.Copy(pages, 0, _pages, newStartPage, copyPageLen);
			RefreshProtections(newStartPage, copyPageLen);
			if (newNumPages > oldNumPages)
				ProtectInternal(newStartPage + oldNumPages, newNumPages - oldNumPages, oldProt, true);

			return ret;
		}

		public bool Unmap(ulong start, ulong size)
		{
			// TODO: eliminate copy+pasta between unmap and protect
			if (start < Memory.Start || start + size > Memory.EndExclusive || size == 0)
				return false;

			var startPage = GetPage(start);
			var numPages = WaterboxUtils.PagesNeeded(size);
			if (!EnsureMapped(startPage, numPages))
				return false;

			ProtectInternal(startPage, numPages, FREE, true);
			return true;
		}

		public bool Protect(ulong start, ulong size, MemoryBlock.Protection prot)
		{
			// TODO: eliminate copy+pasta between unmap and protect
			if (start < Memory.Start || start + size > Memory.EndExclusive || size == 0)
				return false;

			var startPage = GetPage(start);
			var numPages = WaterboxUtils.PagesNeeded(size);
			if (!EnsureMappedNonStack(startPage, numPages))
				return false;

			ProtectInternal(startPage, numPages, prot, true);
			return true;
		}

		public void Dispose()
		{
			if (Memory != null)
			{
				Memory.Dispose();
				Memory = null;
			}
		}

		private const ulong MAGIC = 0x1590abbcdeef5910;

		public void SaveStateBinary(BinaryWriter bw)
		{
			bw.Write(Name);
			bw.Write(Memory.Size);
			bw.Write(Used);
			bw.Write(_pagesAsBytes);
			Memory.SaveState(bw);
			bw.Write(MAGIC);
		}

		public void LoadStateBinary(BinaryReader br)
		{
			var name = br.ReadString();
			if (name != Name)
				throw new InvalidOperationException($"Name did not match for {nameof(MapHeap)} {Name}");
			var size = br.ReadUInt64();
			if (size != Memory.Size)
				throw new InvalidOperationException($"Size did not match for {nameof(MapHeap)} {Name}");
			Used = br.ReadUInt64();
			br.Read(_pagesAsBytes, 0, _pagesAsBytes.Length);
			Memory.LoadState(br);
			if (br.ReadUInt64() != MAGIC)
				throw new InvalidOperationException("Savestate internal error");
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
				var ptr = mmo.Map(siz, MemoryBlock.Protection.RW);
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
				var ptr = mmo.Map(siz, MemoryBlock.Protection.RW);
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
