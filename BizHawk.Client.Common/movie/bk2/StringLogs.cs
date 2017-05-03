using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class StringLogUtil
	{
		public static bool DefaultToDisk;
		public static bool DefaultToAWE;
		public static IStringLog MakeStringLog()
		{
			if (DefaultToDisk)
			{
				return new StreamStringLog(true);
			}

			if (DefaultToAWE)
			{
				return new StreamStringLog(false);
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

	class ListStringLog : List<string>, IStringLog
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
	/// A dumb-ish IStringLog with storage on disk with no provision for recovering lost space, except upon Clear()
	/// The purpose here is to avoid having too complicated buggy logic or a dependency on sqlite or such.
	/// It should be faster than those alternatives, but wasteful of disk space.
	/// It should also be easier to add new IList<string>-like methods than dealing with a database
	/// </summary>
	class StreamStringLog : IStringLog
	{
		List<long> Offsets = new List<long>();
		long cursor = 0;
		BinaryWriter bw;
		BinaryReader br;
		bool mDisk;

		Stream stream;
		public StreamStringLog(bool disk)
		{
			mDisk = disk;
			if (disk)
			{
				var path = TempFileCleaner.GetTempFilename("movieOnDisk");
				stream = new FileStream(path, FileMode.Create, System.Security.AccessControl.FileSystemRights.FullControl, FileShare.None, 4 * 1024, FileOptions.DeleteOnClose);
			}
			else
			{
				stream = new AWEMemoryStream();
			}

			bw = new BinaryWriter(stream);
			br = new BinaryReader(stream);
		}

		public IStringLog Clone()
		{
			StreamStringLog ret = new StreamStringLog(mDisk); // doesnt necessarily make sense to copy the mDisk value, they could be designated for different targets...
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

		public int Count => Offsets.Count;

		public void Clear()
		{
			stream.SetLength(0);
			Offsets.Clear();
			cursor = 0;
		}

		public void Add(string str)
		{
			stream.Position = stream.Length;
			Offsets.Add(stream.Position);
			bw.Write(str);
			bw.Flush();
		}

		public void RemoveAt(int index)
		{
			// no garbage collection in the disk file... oh well.
			Offsets.RemoveAt(index);
		}

		public string this[int index]
		{
			get
			{
				stream.Position = Offsets[index];
				return br.ReadString();
			}
			set
			{
				stream.Position = stream.Length;
				Offsets[index] = stream.Position;
				bw.Write(value);
				bw.Flush();
			}
		}

		public void Insert(int index, string val)
		{
			stream.Position = stream.Length;
			Offsets.Insert(index, stream.Position);
			bw.Write(val);
			bw.Flush();
		}

		public void InsertRange(int index, IEnumerable<string> collection)
		{
			foreach (var item in collection)
			{
				Insert(index++,item);
			}
		}

		public void AddRange(IEnumerable<string> collection)
		{
			foreach (var item in collection)
			{
				Add(item);
			}
		}

		class Enumerator : IEnumerator<string>
		{
			public StreamStringLog log;
			int index = -1;
			public string Current { get { return log[index]; } }
			object System.Collections.IEnumerator.Current { get { return log[index]; } }
			bool System.Collections.IEnumerator.MoveNext()
			{
				index++;
				if (index >= log.Count)
				{
					index = log.Count;
					return false;
				}
				return true;
			}
			void System.Collections.IEnumerator.Reset() { index = -1; }
			
			public void Dispose() { }
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return new Enumerator { log = this };
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new Enumerator { log = this };
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
