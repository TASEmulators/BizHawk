using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
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
		/// Returns an int32 from a byte array based on offset (in BIG ENDIAN format)
		/// </summary>
		public static int GetBEInt32(ReadOnlySpan<byte> buf, int offsetIndex)
			=> BinaryPrimitives.ReadInt32BigEndian(buf.Slice(start: offsetIndex));

		/// <summary>
		/// Returns an int32 from a byte array based on the length of the byte array (in BIG ENDIAN format)
		/// </summary>
		public static int GetBEInt32FromByteArray(ReadOnlySpan<byte> buf)
			=> buf.Length switch
			{
				0 => 0,
				1 => buf[0],
				2 => BinaryPrimitives.ReadUInt16BigEndian(buf),
				3 => buf[2] | (buf[1] << 8) | (buf[0] << 16),
				4 => BinaryPrimitives.ReadInt32BigEndian(buf),
				_ => throw new ArgumentException(paramName: nameof(buf), message: "cannot decode integers wider than 4 octets (s32)"),
			};

		/// <summary>
		/// Returns an int32 from a byte array based on offset
		/// </summary>
		public static uint GetUInt32(ReadOnlySpan<byte> buf, int offsetIndex)
			=> BinaryPrimitives.ReadUInt32LittleEndian(buf.Slice(start: offsetIndex));

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
			const double tspms = 69888.0 * 50.0 / 1000.0;
			int res = (int)(pauseInMS * tspms);

			return res;
		}

		/// <summary>
		/// Caluclate a data block XOR checksum
		/// </summary>
		public static bool CheckChecksum(byte[] buf, int len)
		{
			byte c = 0;
			for (int n = 0; n < len - 1; n++)
			{
				c ^= buf[n];
			}

			if (c == buf[len - 1])
			{
				return true;
			}

			return false;
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


		public static byte[] SerializeRaw(object obj)
		{
			int rSize = Marshal.SizeOf(obj);
			IntPtr buff = Marshal.AllocHGlobal(rSize);
			Marshal.StructureToPtr(obj, buff, false);
			byte[] rData = new byte[rSize];
			Marshal.Copy(buff, rData, 0, rSize);
			return rData;
		}

		public static T DeserializeRaw<T>(byte[] rData, int pos)
		{
			int rSize = Marshal.SizeOf(typeof(T));
			if (rSize > rData.Length - pos)
				throw new Exception();
			IntPtr buff = Marshal.AllocHGlobal(rSize);
			Marshal.Copy(rData, pos, buff, rSize);
			T rObj = (T)Marshal.PtrToStructure(buff, typeof(T));
			Marshal.FreeHGlobal(buff);
			return rObj;
		}
	}
}
