using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	internal class Blob_CHD : IBlob
	{
		private IntPtr _chdFile;

		private readonly uint _hunkSize;
		private readonly byte[] _hunkCache;
		private int _currentHunk;

		public Blob_CHD(IntPtr chdFile, uint hunkSize)
		{
			_chdFile = chdFile;
			_hunkSize = hunkSize;
			_hunkCache = new byte[hunkSize];
			_currentHunk = -1;
		}

		public void Dispose()
		{
			if (_chdFile != IntPtr.Zero)
			{
				LibChd.chd_close(_chdFile);
				_chdFile = IntPtr.Zero;
			}
		}

		public int Read(long byte_pos, byte[] buffer, int offset, int count)
		{
			var ret = count;
			while (count > 0)
			{
				var targetHunk = (uint)(byte_pos / _hunkSize);
				if (targetHunk != _currentHunk)
				{
					var err = LibChd.chd_read(_chdFile, targetHunk, _hunkCache);
					if (err != LibChd.chd_error.CHDERR_NONE)
					{
						// shouldn't ever happen in practice, unless something has gone terribly wrong
						throw new IOException($"CHD read failed with error {err}");
					}

					_currentHunk = (int)targetHunk;
				}

				var hunkOffset = (uint)(byte_pos - targetHunk * _hunkSize);
				var bytesToCopy = Math.Min((int)(_hunkSize - hunkOffset), count);
				Buffer.BlockCopy(_hunkCache, (int)hunkOffset, buffer, offset, bytesToCopy);
				offset += bytesToCopy;
				count -= bytesToCopy;
			}

			return ret;
		}
	}
}
