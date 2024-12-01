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
		/// Converts an int32 value into a byte array
		/// </summary>
		public static byte[] GetBytes(int value)
		{
			byte[] buf = new byte[4];
			buf[0] = (byte)value;
			buf[1] = (byte)(value >> 8);
			buf[2] = (byte)(value >> 16);
			buf[3] = (byte)(value >> 24);
			return buf;
		}

		/// <summary>
		/// Returns an int32 from a byte array based on offset
		/// </summary>
		public static int GetInt32(byte[] buf, int offsetIndex)
		{
			return buf[offsetIndex] | buf[offsetIndex + 1] << 8 | buf[offsetIndex + 2] << 16 | buf[offsetIndex + 3] << 24;
		}

		/// <summary>
		/// Returns an uint16 from a byte array based on offset
		/// </summary>
		public static ushort GetWordValue(byte[] buf, int offsetIndex)
		{
			return (ushort)(buf[offsetIndex] | buf[offsetIndex + 1] << 8);
		}

		/// <summary>
		/// Updates a byte array with a uint16 value based on offset
		/// </summary>
		public static void SetWordValue(byte[] buf, int offsetIndex, ushort value)
		{
			buf[offsetIndex] = (byte)value;
			buf[offsetIndex + 1] = (byte)(value >> 8);
		}

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
