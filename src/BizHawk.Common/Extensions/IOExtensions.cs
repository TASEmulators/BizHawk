using System.IO;
using System.Text;

namespace BizHawk.Common.IOExtensions
{
	public static class IOExtensions
	{
		public static readonly Encoding ShiftJISEncoding = Encoding.GetEncoding("shift_jis");

		public static Span<byte> GetBufferAsSpan(this MemoryStream ms)
			=> ms.GetBuffer().AsSpan().Slice(start: 0, length: (int) ms.Length);

		public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
		{
			fixed (byte* p = bytes) return encoding.GetString(p, bytes.Length);
		}

		public static byte[] ReadAllBytes(this Stream stream)
		{
			var outStream = new MemoryStream();
			stream.CopyTo(outStream);

			return outStream.ToArray();
		}

		public static byte[] ReadAllBytes(this BinaryReader br)
		{
			const int BUFF_SIZE = 4096;
			var buffer = new byte[BUFF_SIZE];

			int bytesRead;
			var outStream = new MemoryStream();

			while ((bytesRead = br.Read(buffer, 0, BUFF_SIZE)) > 0)
			{
				outStream.Write(buffer, 0, bytesRead);
			}

			return outStream.ToArray();
		}

		/// <summary>
		/// Read a string from a binary reader using utf8 encoding and known byte length
		/// </summary>
		/// <param name="bytes">exact number of bytes to read</param>
		public static string ReadStringFixedUtf8(this BinaryReader r, int bytes)
		{
			var read = new byte[bytes];
			r.Read(read, 0, bytes);
			return Encoding.UTF8.GetString(read);
		}

		/// <summary>
		/// Read a null terminated string from a binary reader using utf8 encoding
		/// </summary>
		public static string ReadStringUtf8NullTerminated(this BinaryReader br)
		{
			using var ms = new MemoryStream();
			for (;;)
			{
				var b = br.ReadByte();
				if (b == 0)
				{
					return Encoding.UTF8.GetString(ms.ToArray());
				}

				ms.WriteByte(b);
			}
		}

		public static void Write(this BinaryWriter bw, int[] buffer)
		{
			foreach (int b in buffer)
			{
				bw.Write(b);
			}
		}

		public static void Write(this BinaryWriter bw, uint[] buffer)
		{
			foreach (uint b in buffer)
			{
				bw.Write(b);
			}
		}

		public static void Write(this BinaryWriter bw, short[] buffer)
		{
			foreach (short b in buffer)
			{
				bw.Write(b);
			}
		}

		public static void Write(this BinaryWriter bw, ushort[] buffer)
		{
			foreach (ushort t in buffer)
			{
				bw.Write(t);
			}
		}

		public static void WriteBit(this BinaryWriter bw, Bit bit)
		{
			bw.Write((bool)bit);
		}

		public static Bit ReadBit(this BinaryReader br)
		{
			return br.ReadBoolean();
		}
	}
}
