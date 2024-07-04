#nullable enable

using System.IO;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public class ProxiedFile : IDisposable
	{
		private FileStream? _fileStream;

		public bool OpenMsuTrack(string romPath, ushort id) => Open($"{romPath}-{id}.pcm");

		public bool Open(string path)
		{
			_fileStream?.Dispose();
			try
			{
				_fileStream = File.OpenRead(path);
				return true;
			}
			catch
			{
				_fileStream = null;
				return false;
			}
		}

		public void Seek(long offset, bool relative)
		{
			_fileStream?.Seek(offset, relative ? SeekOrigin.Current : SeekOrigin.Begin);
		}

		public byte ReadByte()
		{
			return (byte)(_fileStream?.ReadByte() ?? 0);
		}

		public bool AtEnd()
		{
			return _fileStream?.Position == _fileStream?.Length;
		}

		public void Dispose()
		{
			_fileStream?.Dispose();
		}
	}
}
