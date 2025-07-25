using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class StatableStream : IStatable
	{
		public bool AvoidRewind => false;
		public void LoadStateBinary(BinaryReader reader) => throw new NotImplementedException();

		private Stream _stream;
		private int _length;
		public StatableStream(Stream stream, int length)
		{
			_stream = stream;
			_length = length;
		}
		public void SaveStateBinary(BinaryWriter writer)
		{
			int copied = 0;
			const int bufferSize = 81920; // It's the default of CopyTo's buffer size
			byte[] buffer = new byte[bufferSize];
			while (copied < _length - bufferSize)
			{
				if (_stream.Read(buffer, 0, bufferSize) != bufferSize)
					throw new Exception("Unexpected end of stream.");
				writer.Write(buffer);
				copied += bufferSize;
			}
			int remaining = _length - copied;
			if (_stream.Read(buffer, 0, remaining) != remaining)
				throw new Exception("Unexpected end of stream.");
			writer.Write(buffer, 0, remaining);
		}
	}
}
