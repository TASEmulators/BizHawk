using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Manages a ring buffer of storage which can continually chow its own tail to keep growing forward.
	/// Probably only useful for the rewind buffer
	/// </summary>
	public class StreamBlobDatabase : IDisposable
	{
		private Func<byte[], long, bool, byte[]> _mBufferManage;
		private byte[] _mAllocatedBuffer;
		private Stream _mStream;
		private LinkedList<ListItem> _mBookmarks = new LinkedList<ListItem>();
		private LinkedListNode<ListItem> _mHead, _mTail;
		private long _mCapacity, _mSize;

		public StreamBlobDatabase(bool onDisk, long capacity, Func<byte[], long, bool, byte[]> mBufferManage)
		{
			_mBufferManage = mBufferManage;
			_mCapacity = capacity;
			if (onDisk)
			{
				var path = Path.Combine(System.IO.Path.GetTempPath(), "bizhawk.rewindbuf-pid" + System.Diagnostics.Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString());

				// I checked the DeleteOnClose operation to make sure it cleans up when the process is aborted, and it seems to.
				// Otherwise we would have a more complex tempfile management problem here.
				// 4KB buffer chosen due to similarity to .net defaults, and fear of anything larger making hiccups for small systems (we could try asyncing this stuff though...)
				_mStream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.None, 4 * 1024, FileOptions.DeleteOnClose);
			}
			else
			{
				_mAllocatedBuffer = _mBufferManage(null, capacity, true);
				_mStream = new MemoryStream(_mAllocatedBuffer);
			}
		}

		/// <summary>
		/// Returns the amount of the buffer that's used
		/// </summary>
		public long Size { get { return _mSize; } }

		/// <summary>
		/// Gets the current fullness ratio (Size/Capacity). Note that this wont reach 100% due to the buffer size not being a multiple of a fixed savestate size.
		/// </summary>
		public float FullnessRatio { get { return (float)((double)Size / (double)_mCapacity); } }

		/// <summary>
		/// the number of frames stored here
		/// </summary>
		public int Count { get { return _mBookmarks.Count; } }

		/// <summary>
		/// The underlying stream to 
		/// </summary>
		public Stream Stream { get { return _mStream; } }

		public void Dispose()
		{
			_mStream.Dispose();
			_mStream = null;
			if (_mAllocatedBuffer != null)
			{
				_mBufferManage(_mAllocatedBuffer, 0, false);
			}
		}

		public void Clear()
		{
			_mHead = _mTail = null;
			_mSize = 0;
			_mBookmarks.Clear();
		}

		/// <summary>
		/// The push and pop semantics are for historical reasons and not resemblence to normal definitions
		/// </summary>
		public void Push(ArraySegment<byte> seg)
		{
			var buf = seg.Array;
			int len = seg.Count;
			long offset = Enqueue(0, len);
			_mStream.Position = offset;
			_mStream.Write(buf, seg.Offset, len);
		}

		/// <summary>
		/// The push and pop semantics are for historical reasons and not resemblence to normal definitions
		/// </summary>
		public MemoryStream PopMemoryStream()
		{
			var item = Pop();
			var buf = new byte[item.Length];
			_mStream.Position = item.Index;
			_mStream.Read(buf, 0, item.Length);
			var ret = new MemoryStream(buf, 0, item.Length, false, true);
			return ret;
		}

		public long Enqueue(int timestamp, int amount)
		{
			_mSize += amount;

			if (_mHead == null)
			{
				_mTail = _mHead = _mBookmarks.AddFirst(new ListItem(timestamp, 0, amount));
				return 0;
			}

			long target = _mHead.Value.EndExclusive + amount;
			if (_mTail != null && target <= _mTail.Value.Index)
			{
				// theres room to add a new head before the tail
				_mHead = _mBookmarks.AddAfter(_mHead, new ListItem(timestamp, _mHead.Value.EndExclusive, amount));
				goto CLEANUP;
			}

			// maybe the tail is earlier than the head
			if (_mTail.Value.Index < _mHead.Value.Index)
			{
				if (target <= _mCapacity)
				{
					// theres room to add a new head before the end of capacity
					_mHead = _mBookmarks.AddAfter(_mHead, new ListItem(timestamp, _mHead.Value.EndExclusive, amount));
					goto CLEANUP;
				}
			}
			else
			{
				// nope, tail is after head. we'll have to clobber from the tail
				_mHead = _mBookmarks.AddAfter(_mHead, new ListItem(timestamp, _mHead.Value.EndExclusive, amount));
				goto CLEANUP;
			}

			// no room before the tail, or before capacity. head needs to wrap around.
			_mHead = _mBookmarks.AddAfter(_mHead, new ListItem(timestamp, 0, amount));

		CLEANUP:
			// while the head impinges on tail items, discard them
			for (; ; )
			{
				if (_mTail == null)
				{
					break;
				}

				if (_mHead.Value.EndExclusive > _mTail.Value.Index && _mHead.Value.Index <= _mTail.Value.Index)
				{
					LinkedListNode<ListItem> nextTail = _mTail.Next;
					_mSize -= _mTail.Value.Length;
					_mBookmarks.Remove(_mTail);
					_mTail = nextTail;
				}
				else break;
			}

			return _mHead.Value.Index;
		}

		public ListItem Pop()
		{
			if (_mHead == null)
			{
				throw new InvalidOperationException("Attempted to pop from an empty data structure");
			}

			var ret = _mHead.Value;
			_mSize -= ret.Length;
			LinkedListNode<ListItem> nextHead = _mHead.Previous;
			_mBookmarks.Remove(_mHead);
			if (_mHead == _mTail)
			{
				_mTail = null;
			}

			_mHead = nextHead;

			if (_mHead == null)
			{
				_mHead = _mBookmarks.Last;
			}

			return ret;
		}

		public ListItem Dequeue()
		{
			if (_mTail == null)
			{
				throw new InvalidOperationException("Attempted to dequeue from an empty data structure");
			}

			var ret = _mTail.Value;
			_mSize -= ret.Length;
			LinkedListNode<ListItem> nextTail = _mTail.Next;
			_mBookmarks.Remove(_mTail);
			if (_mTail == _mHead)
			{
				_mHead = null;
			}

			_mTail = nextTail;
			if (_mTail == null)
			{
				_mTail = _mBookmarks.First;
			}

			return ret;
		}

		//-------- tests ---------
		public void AssertMonotonic()
		{
			if (_mTail == null)
			{
				return;
			}

			int ts = _mTail.Value.Timestamp;
			LinkedListNode<ListItem> curr = _mTail;
			for (; ; )
			{
				if (curr == null)
				{
					curr = _mBookmarks.First;
					break;
				}

				System.Diagnostics.Debug.Assert(curr.Value.Timestamp >= ts);
				if (curr == _mHead)
				{
					return;
				}

				ts = curr.Value.Timestamp;
				curr = curr.Next;
			}
		}

		public class ListItem
		{
			public ListItem(int timestamp, long index, int length)
			{
				Timestamp = timestamp;
				Index = index;
				Length = length;
			}

			public int Timestamp { get; private set; }
			public long Index { get; private set; }
			public int Length { get; private set; }
			
			public long EndExclusive
			{
				get { return Index + Length; }
			}
		}
	}
}
