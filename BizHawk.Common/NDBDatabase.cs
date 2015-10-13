using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common
{
	/// <summary>
	/// Non-consecutive Disk Block Database
	/// Opens a file and stores blocks in it.
	/// Blocks can be differently sized than the basic block size. Wastage will occur.
	/// TODO: Mount on memory as well?
	/// </summary>
	public class NDBDatabase : IDisposable
	{
		readonly int BlockSize;
		readonly long BlockCount;

		Dictionary<string, Item> Items = new Dictionary<string, Item>();
		LinkedList<Block> FreeList = new LinkedList<Block>();
		long FreeWatermark;
		FileStream Stream;

		class Block
		{
			public long Number;
		}

		class Item
		{
			public LinkedList<Block> Blocks = new LinkedList<Block>();
			public long Size;
		}

		Block AllocBlock()
		{
			if (FreeList.Count != 0)
			{
				var blocknode = FreeList.First;
				FreeList.RemoveFirst();
				Consumed += BlockSize;
				return blocknode.Value;
			}

			if (FreeWatermark == BlockCount)
				throw new OutOfMemoryException("NDBDatabase out of reserved space");

			var b = new Block() { Number = FreeWatermark };
			FreeWatermark++;
			Consumed += BlockSize;

			return b;
		}

		long GetOffsetForBlock(Block b)
		{
			return b.Number * BlockSize;
		}

		/// <summary>
		/// Creates a new instance around a DeleteOnClose file of the provided path
		/// </summary>
		public NDBDatabase(string path, long size, int blocksize)
		{
			Capacity = size;
			Consumed = 0;
			BlockSize = blocksize;
			BlockCount = size / BlockSize;
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			Stream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.None, 4 * 1024, FileOptions.DeleteOnClose);
		}

		/// <summary>
		/// Clears the state of the datastructure to its original condition
		/// </summary>
		public void Clear()
		{
			Consumed = 0;
			Items.Clear();
			FreeList.Clear();
			FreeWatermark = 0;
		}

		public void Dispose()
		{
			Stream.Dispose();
		}

		/// <summary>
		/// Total reserved storage capacity. You may nto be able to fit that much data in here though (due to blockiness)
		/// </summary>
		public readonly long Capacity;

		/// <summary>
		/// The amount of bytes of storage consumed. Not necessarily equal to the total amount of data stored (due to blockiness)
		/// </summary>
		public long Consumed { get; private set; }

		/// <summary>
		/// The amount of bytes of storage available. Store operations <= Remain will always succeed
		/// </summary>
		public long Remain { get { return Capacity - Consumed; } }

		/// <summary>
		/// Stores an item with the given key
		/// </summary>
		public void Store(string name, byte[] buf, int offset, int length)
		{
			if (Items.ContainsKey(name))
				throw new InvalidOperationException(string.Format("Can't add already existing key of name {0}", name));

			if (length > Remain)
				throw new OutOfMemoryException(string.Format("Insufficient storage reserved for {0} bytes", length));

			long todo = length;
			int src = offset;
			Item item = new Item { Size = length };
			Items[name] = item;
			while (todo > 0)
			{
				var b = AllocBlock();
				item.Blocks.AddLast(b);

				long tocopy = todo;
				if (tocopy > BlockSize)
					tocopy = BlockSize;

				Stream.Position = GetOffsetForBlock(b);
				Stream.Write(buf, src, (int)tocopy);

				todo -= tocopy;
				src += (int)tocopy;
			}
		}

		/// <summary>
		/// Fetches an item with the given key
		/// </summary>
		public byte[] FetchAll(string name)
		{
			var buf = new byte[GetSize(name)];
			Fetch(name, buf, 0);
			return buf;
		}

		/// <summary>
		/// Fetches an item with the given key
		/// </summary>
		public void Fetch(string name, byte[] buf, int offset)
		{
			Item item;
			if (!Items.TryGetValue(name, out item))
				throw new KeyNotFoundException();

			long todo = item.Size;
			var curr = item.Blocks.First;
			while (todo > 0)
			{
				long tocopy = todo;
				if (tocopy > BlockSize)
					tocopy = BlockSize;
				Stream.Position = GetOffsetForBlock(curr.Value);
				Stream.Read(buf, offset, (int)tocopy);

				todo -= tocopy;
				offset += (int)tocopy;

				curr = curr.Next;
			}
			System.Diagnostics.Debug.Assert(curr == null);
		}

		/// <summary>
		/// Releases the item with the given key.
		/// Removing a non-existent item is benign, I guess
		/// </summary>
		public void Release(string name)
		{
			Item item;
			if (!Items.TryGetValue(name, out item))
				return;
			Items.Remove(name);
			var blocks = item.Blocks.ToArray();
			item.Blocks.Clear();
			foreach (var block in blocks)
				FreeList.AddLast(block);
			Consumed -= blocks.Length * BlockSize;
		}

		/// <summary>
		/// Gets the size of the item with the given key
		/// </summary>
		public long GetSize(string name)
		{
			return Items[name].Size;
		}
	}

}