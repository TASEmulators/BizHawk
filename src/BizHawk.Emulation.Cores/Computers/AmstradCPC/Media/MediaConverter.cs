using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// Abtract class that represents all Media Converters
	/// </summary>
	public abstract class MediaConverter
	{
		/// <summary>
		/// The type of serializer
		/// </summary>
		public abstract MediaConverterType FormatType { get; }

		/// <summary>
		/// Signs whether this class can be used to read the data format
		/// </summary>
		public virtual bool IsReader => false;

		/// <summary>
		/// Signs whether this class can be used to write the data format
		/// </summary>
		public virtual bool IsWriter => false;

		protected abstract string SelfTypeName { get; }

		/// <summary>
		/// Serialization method
		/// </summary>
		public virtual void Read(byte[] data)
			=> throw new NotImplementedException($"Read operation is not implemented for {SelfTypeName}");

		/// <summary>
		/// DeSerialization method
		/// </summary>
		public virtual void Write(byte[] data)
			=> throw new NotImplementedException($"Write operation is not implemented for {SelfTypeName}");

		/// <summary>
		/// Serializer does a quick check, returns TRUE if file is detected as this type
		/// </summary>
		public virtual bool CheckType(byte[] data)
			=> throw new NotImplementedException($"Check type operation is not implemented for {SelfTypeName}");

		/// <summary>
		/// Returns an int32 from a byte array based on offset
		/// </summary>
		public static int GetInt32(ReadOnlySpan<byte> buf, int offsetIndex)
			=> BinaryPrimitives.ReadInt32LittleEndian(buf.Slice(start: offsetIndex));

		/// <summary>
		/// Returns an uint16 from a byte array based on offset
		/// </summary>
		public static ushort GetWordValue(ReadOnlySpan<byte> buf, int offsetIndex)
			=> BinaryPrimitives.ReadUInt16LittleEndian(buf.Slice(start: offsetIndex));

		/// <summary>
		/// Takes a PauseInMilliseconds value and returns the value in T-States
		/// </summary>
		public static int TranslatePause(int pauseInMS)
		{
			// t-states per millisecond
			var tspms = (69888 * 50) / 1000;
			// get value
			int res = pauseInMS * tspms;

			return res;
		}

		/// <summary>
		/// Decompresses a byte array that is Z-RLE compressed
		/// </summary>
		public static void DecompressZRLE(byte[] sourceBuffer, ref byte[] destBuffer)
		{
			MemoryStream stream = new MemoryStream();
			stream.Write(sourceBuffer, 0, sourceBuffer.Length);
			stream.Position = 0;
			stream.ReadByte();
			stream.ReadByte();
			DeflateStream ds = new DeflateStream(stream, CompressionMode.Decompress, false);
			ds.Read(destBuffer, 0, destBuffer.Length);
		}
	}
}
