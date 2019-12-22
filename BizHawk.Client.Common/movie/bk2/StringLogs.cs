using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class StringLogUtil
	{
		public static bool DefaultToDisk { get; set; }
		public static bool DefaultToAWE { get; set; }
		public static IStringLog MakeStringLog()
		{
			if (DefaultToDisk)
			{
				return new StreamStringLog(true);
			}

			if (DefaultToAWE)
			{
				return OSTailoredCode.IsUnixHost
					? throw new InvalidOperationException("logging to AWE is only available on Windows for now")
					: new StreamStringLog(false);
			}

			return new ListStringLog();
		}
	}

	public interface IStringLog : IDisposable, IEnumerable<string>
	{
		void RemoveAt(int index);
		int Count { get; }
		void Clear();
		void Add(string str);
		string this[int index] { get; set; }
		void Insert(int index, string val);
		void InsertRange(int index, IEnumerable<string> collection);
		void AddRange(IEnumerable<string> collection);
		void RemoveRange(int index, int count);
		IStringLog Clone();
		void CopyTo(string[] array);
		void CopyTo(int index, string[] array, int arrayIndex, int count);
	}

	internal class ListStringLog : List<string>, IStringLog
	{
		public IStringLog Clone()
		{
			ListStringLog ret = new ListStringLog();
			ret.AddRange(this);
			return ret;
		}

		public void Dispose() { }
	}

	/// <summary>
	/// A dumb IStringLog with storage on disk with no provision for recovering lost space, except upon Clear()
	/// The purpose here is to avoid having too complicated buggy logic or a dependency on SQLite or such.
	/// It should be faster than those alternatives, but wasteful of disk space.
	/// It should also be easier to add new IList&lt;string&gt;-like methods than dealing with a database
	/// </summary>
	internal class StreamStringLog : IStringLog
	{
		private readonly Stream stream;
		private readonly List<long> _offsets = new List<long>();
		private readonly BinaryWriter _bw;
		private readonly BinaryReader _br;
		private readonly bool _mDisk;

		private long _cursor;

		public StreamStringLog(bool disk)
		{
			_mDisk = disk;
			if (disk)
			{
				var path = TempFileManager.GetTempFilename("movieOnDisk");
				stream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.None, 4 * 1024, FileOptions.DeleteOnClose);
			}
			else
			{
				stream = new AWEMemoryStream();
			}

			_bw = new BinaryWriter(stream);
			_br = new BinaryReader(stream);
		}

		public IStringLog Clone()
		{
			StreamStringLog ret = new StreamStringLog(_mDisk); // doesn't necessarily make sense to copy the mDisk value, they could be designated for different targets...
			for (int i = 0; i < Count; i++)
			{
				ret.Add(this[i]);
			}

			return ret;
		}

		public void Dispose()
		{
			stream.Dispose();
		}

		public int Count => _offsets.Count;

		public void Clear()
		{
			stream.SetLength(0);
			_offsets.Clear();
			_cursor = 0;
		}

		public void Add(string str)
		{
			stream.Position = stream.Length;
			_offsets.Add(stream.Position);
			_bw.Write(str);
			_bw.Flush();
		}

		public void RemoveAt(int index)
		{
			// no garbage collection in the disk file... oh well.
			_offsets.RemoveAt(index);
		}

		public string this[int index]
		{
			get
			{
				stream.Position = _offsets[index];
				return _br.ReadString();
			}

			set
			{
				stream.Position = stream.Length;
				_offsets[index] = stream.Position;
				_bw.Write(value);
				_bw.Flush();
			}
		}

		public void Insert(int index, string val)
		{
			stream.Position = stream.Length;
			_offsets.Insert(index, stream.Position);
			_bw.Write(val);
			_bw.Flush();
		}

		public void InsertRange(int index, IEnumerable<string> collection)
		{
			foreach (var item in collection)
			{
				Insert(index++, item);
			}
		}

		public void AddRange(IEnumerable<string> collection)
		{
			foreach (var item in collection)
			{
				Add(item);
			}
		}

		private class Enumerator : IEnumerator<string>
		{
			public StreamStringLog Log { get; set; }
			private int _index = -1;

			public string Current => Log[_index];
			object System.Collections.IEnumerator.Current => Log[_index];

			bool System.Collections.IEnumerator.MoveNext()
			{
				_index++;
				if (_index >= Log.Count)
				{
					_index = Log.Count;
					return false;
				}

				return true;
			}

			void System.Collections.IEnumerator.Reset() { _index = -1; }
			
			public void Dispose() { }
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return new Enumerator { Log = this };
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new Enumerator { Log = this };
		}

		public void RemoveRange(int index, int count)
		{
			int end = index + count - 1;
			for (int i = 0; i < count; i++)
			{
				RemoveAt(end);
				end--;
			}
		}

		public void CopyTo(string[] array)
		{
			for (int i = 0; i < Count; i++)
			{
				array[i] = this[i];
			}
		}

		public void CopyTo(int index, string[] array, int arrayIndex, int count)
		{
			for (int i = 0; i < count; i++)
			{
				array[i + arrayIndex] = this[index + i];
			}
		}
	}
}
