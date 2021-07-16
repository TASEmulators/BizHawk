using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace BizHawk.Client.Common
{
	public sealed class MemoryMappedFiles
	{
		private readonly Dictionary<string, MemoryMappedFile> _mmfFiles = new Dictionary<string, MemoryMappedFile>();
		private readonly Func<byte[]> _takeScreenshotCallback;

		public string? Filename;

		public MemoryMappedFiles(Func<byte[]> takeScreenshotCallback, string? filename)
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
			if (!_mmfFiles.TryGetValue(filename, out var mmfFile))
			{
				mmfFile = _mmfFiles[filename] = MemoryMappedFile.OpenExisting(filename);
			}

			var viewAccessor = mmfFile.CreateViewAccessor(0, expectedSize, MemoryMappedFileAccess.Read);
			var bytes = new byte[expectedSize];
			viewAccessor.ReadArray(0, bytes, 0, expectedSize);
			viewAccessor.Dispose();
			return bytes;
		}

		public int ScreenShotToFile() => WriteToFile(Filename!, _takeScreenshotCallback());

		public int WriteToFile(string filename, byte[] outputBytes)
		{
			int TryWrite(MemoryMappedFile m)
			{
				using var accessor = m.CreateViewAccessor(0, outputBytes.Length, MemoryMappedFileAccess.Write);
				accessor.WriteArray(0, outputBytes, 0, outputBytes.Length);
				accessor.Dispose();
				return outputBytes.Length;
			}

			if (!_mmfFiles.TryGetValue(filename, out var mmfFile))
			{
				mmfFile = _mmfFiles[filename] = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length);
			}

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
