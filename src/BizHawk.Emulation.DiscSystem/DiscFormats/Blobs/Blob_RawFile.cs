﻿using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	internal sealed class Blob_RawFile : IBlob
	{
		public string PhysicalPath
		{
			get => physicalPath;
			set
			{
				physicalPath = value;
				Length = new FileInfo(physicalPath).Length;
			}
		}

		private string physicalPath;

		public readonly long Offset = 0;

		private BufferedStream fs;
		
		public void Dispose()
		{
			fs?.Dispose();
			fs = null;
		}
		
		public int Read(long byte_pos, byte[] buffer, int offset, int count)
		{
			//use quite a large buffer, because normally we will be reading these sequentially but in small chunks.
			//this enhances performance considerably

			//NOTE: wouldnt very large buffering create stuttering? this would depend on how it's implemented.
			//really, we need a smarter asynchronous read-ahead buffer. that requires substantially more engineering, some kind of 'DiscUniverse' of carefully managed threads and such.

			const int buffersize = 2352 * 75 * 2;
			fs ??= new(new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read), buffersize);
			var target = byte_pos + Offset;
			if (fs.Position != target)
				fs.Position = target;
			return fs.Read(buffer, offset, count);
		}
		
		public long Length { get; private set; }
	}
}
