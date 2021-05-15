using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BizHawk.Common;

namespace BizHawk.Client.Common
{
	internal class TempFileStateDictionary : IDictionary<int, byte[]>, IDisposable
	{
		private readonly IDictionary<int, Stream> _streams = new Dictionary<int, Stream>();

		public byte[] this[int key]
		{
			get
			{
				byte[] bytes = new byte[_streams[key].Length];
				_streams[key].Seek(0, SeekOrigin.Begin);
				_streams[key].Read(bytes, 0, bytes.Length);
				return bytes;
			}
			set => SetState(key, new MemoryStream(value));
		}

		public void SetState(int frame, Stream stream)
		{
			if (!_streams.ContainsKey(frame))
			{
				string filename =  TempFileManager.GetTempFilename("State");
				_streams[frame] = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
			}
			else
				_streams[frame].Seek(0, SeekOrigin.Begin);

			_streams[frame].SetLength(stream.Length);
			stream.CopyTo(_streams[frame]);
		}

		public ICollection<int> Keys => _streams.Keys;

		public ICollection<byte[]> Values => throw new NotImplementedException();

		public int Count => _streams.Count;

		public bool IsReadOnly => false;

		public void Add(int key, byte[] value)
		{
			this[key] = value;
		}

		public void Add(KeyValuePair<int, byte[]> item)
		{
			this[item.Key] = item.Value;
		}

		public void Clear()
		{
			foreach (var kvp in _streams)
				kvp.Value.Dispose();

			_streams.Clear();
		}

		public bool Contains(KeyValuePair<int, byte[]> item)
		{
			throw new NotImplementedException();
		}

		public bool ContainsKey(int key)
		{
			return _streams.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<int, byte[]>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<KeyValuePair<int, byte[]>> GetEnumerator()
		{
			foreach (var kvp in _streams)
				yield return new KeyValuePair<int, byte[]>(kvp.Key, this[kvp.Key]);
		}

		public bool Remove(int key)
		{
			if (ContainsKey(key))
			{
				_streams[key].Dispose();
				return _streams.Remove(key);
			}
			else
				return false;
		}

		public bool Remove(KeyValuePair<int, byte[]> item)
		{
			throw new NotImplementedException();
		}

		public bool TryGetValue(int key, out byte[] value)
		{
			if (!ContainsKey(key))
			{
				value = null;
				return false;
			}
			else
			{
				value = this[key];
				return true;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void Dispose()
		{
			Clear();
		}
	}
}
