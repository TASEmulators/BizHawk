using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class StringLogUtil
	{
		public static bool DefaultToDisk { get; set; }

		public static IStringLog MakeStringLog()
		{
			if (DefaultToDisk)
			{
				return new StreamStringLog(true);
			}

			return new ListStringLog();
		}

		public static int? DivergentPoint(this IStringLog currentLog, IStringLog newLog)
		{
			int max = newLog.Count;
			if (currentLog.Count < newLog.Count)
			{
				max = currentLog.Count;
			}

			for (int i = 0; i < max; i++)
			{
				if (newLog[i] != currentLog[i])
				{
					return i;
				}
			}

			return null;
		}

		public static string ToInputLog(this IStringLog log)
		{
			var sb = new StringBuilder();
			foreach (var record in log)
			{
				sb.AppendLine(record);
			}

			return sb.ToString();
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
		void CopyTo(int index, string[] array, int arrayIndex, int count);
	}

	internal class ListStringLog : List<string>, IStringLog
	{
		public IStringLog Clone()
		{
			var ret = new ListStringLog();
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
		private readonly Stream _stream;
		private readonly List<long> _offsets = new List<long>();
		private readonly BinaryWriter _bw;
		private readonly BinaryReader _br;
		private readonly bool _mDisk;

		public StreamStringLog(bool disk)
		{
			_mDisk = disk;
			if (disk)
			{
				var path = TempFileManager.GetTempFilename("movieOnDisk");
				_stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4 * 1024, FileOptions.DeleteOnClose);
			}
			else
			{
				//but leave this in case we want to retain the option for more efficient management than the list of strings
				throw new InvalidOperationException("Not supported anymore");
			}

			_bw = new BinaryWriter(_stream);
			_br = new BinaryReader(_stream);
		}

		public IStringLog Clone()
		{
			var ret = new StreamStringLog(_mDisk); // doesn't necessarily make sense to copy the mDisk value, they could be designated for different targets...
			for (int i = 0; i < Count; i++)
			{
				ret.Add(this[i]);
			}

			return ret;
		}

		public void Dispose()
		{
			_stream.Dispose();
		}

		public int Count => _offsets.Count;

		public void Clear()
		{
			_stream.SetLength(0);
			_offsets.Clear();
		}

		public void Add(string str)
		{
			_stream.Position = _stream.Length;
			_offsets.Add(_stream.Position);
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
				_stream.Position = _offsets[index];
				return _br.ReadString();
			}

			set
			{
				_stream.Position = _stream.Length;
				_offsets[index] = _stream.Position;
				_bw.Write(value);
				_bw.Flush();
			}
		}

		public void Insert(int index, string val)
		{
			_stream.Position = _stream.Length;
			_offsets.Insert(index, _stream.Position);
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
			object IEnumerator.Current => Log[_index];

			bool IEnumerator.MoveNext()
			{
				_index++;
				if (_index >= Log.Count)
				{
					_index = Log.Count;
					return false;
				}

				return true;
			}

			void IEnumerator.Reset() { _index = -1; }
			
			public void Dispose() { }
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return new Enumerator { Log = this };
		}

		IEnumerator IEnumerable.GetEnumerator()
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

		public void CopyTo(int index, string[] array, int arrayIndex, int count)
		{
			for (int i = 0; i < count; i++)
			{
				array[i + arrayIndex] = this[index + i];
			}
		}
	}
}
