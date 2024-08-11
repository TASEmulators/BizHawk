using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.Common
{
	public sealed class MemoryMappedFiles
	{
		private readonly Dictionary<string, MemoryMappedFile> _mmfFiles = new Dictionary<string, MemoryMappedFile>();

		private readonly Func<byte[]> _takeScreenshotCallback;

		public string Filename;

		public MemoryMappedFiles(Func<byte[]> takeScreenshotCallback, string filename)
		{
			_takeScreenshotCallback = takeScreenshotCallback;
			Filename = filename;
		}

		public string ReadFromFile(string filename, int expectedSize)
		{
			var bytes = ReadBytesFromFile(filename, expectedSize);
			return Encoding.UTF8.GetString(bytes);
		}

		public byte[] ReadBytesFromFile(string filename, int expectedSize)
		{
			var mmfFile = _mmfFiles.GetValueOrPut(filename, MemoryMappedFile.OpenExisting);
			using var viewAccessor = mmfFile.CreateViewAccessor(0, expectedSize, MemoryMappedFileAccess.Read);
			var bytes = new byte[expectedSize];
			viewAccessor.ReadArray(0, bytes, 0, expectedSize);
			return bytes;
		}

		public int ScreenShotToFile()
		{
			if (Filename is null)
			{
				Console.WriteLine("MMF screenshot target not set; start EmuHawk with `--mmf=filename`");
				return 0;
			}
			return WriteToFile(Filename, _takeScreenshotCallback());
		}

		public int WriteToFile(string filename, byte[] outputBytes)
		{
			int TryWrite(MemoryMappedFile m)
			{
				using var accessor = m.CreateViewAccessor(0, outputBytes.Length, MemoryMappedFileAccess.Write);
				accessor.WriteArray(0, outputBytes, 0, outputBytes.Length);
				return outputBytes.Length;
			}
			var mmfFile = _mmfFiles.GetValueOrPut(filename, s => MemoryMappedFile.CreateOrOpen(s, outputBytes.Length));
			try
			{
				return TryWrite(mmfFile);
			}
			catch (UnauthorizedAccessException)
			{
				try
				{
					mmfFile.Dispose();
				}
				catch (Exception)
				{
					// ignored
					//TODO are Dispose() implementations allowed to throw? does this one ever throw? --yoshi
				}
				return TryWrite(_mmfFiles[filename] = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length));
			}
		}

		public int WriteToFile(string filename, string outputString) => WriteToFile(filename, Encoding.UTF8.GetBytes(outputString));
	}
}
