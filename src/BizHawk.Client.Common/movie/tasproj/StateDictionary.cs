using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
				var bytesRead = _streams[key].Read(bytes, offset: 0, count: bytes.Length);
				Debug.Assert(bytesRead == bytes.Length, "reached end-of-file while reading state");
				return bytes;
			}
			set => SetState(key, new MemoryStream(value));
		}

		public void SetState(int frame, Stream stream)
		{
			if (_streams.TryGetValue(frame, out var foundStream))
			{
				foundStream.Seek(0, SeekOrigin.Begin);
			}
			else
			{
				_streams[frame] = foundStream = new FileStream(
					TempFileManager.GetTempFilename("State"),
					FileMode.Create,
					FileAccess.ReadWrite,
					FileShare.None,
					4096,
					FileOptions.DeleteOnClose);
			}

			foundStream.SetLength(stream.Length);
			stream.CopyTo(foundStream);
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
			=> _streams.Select(kvp => new KeyValuePair<int, byte[]>(kvp.Key, this[kvp.Key])).GetEnumerator();

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
