using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizHawk.Common.IOExtensions
{
	public static class IOExtensions
	{
		public static byte[] ReadAllBytes(this Stream stream)
		{
			const int BUFF_SIZE = 4096;
			var buffer = new byte[BUFF_SIZE];

			int bytesRead;
			var inStream = new BufferedStream(stream);
			var outStream = new MemoryStream();

			while ((bytesRead = inStream.Read(buffer, 0, BUFF_SIZE)) > 0)
			{
				outStream.Write(buffer, 0, bytesRead);
			}

			return outStream.ToArray();
		}

		// Read bytes from a BinaryReader and translate them into the UTF-8 string they represent.
		//WHAT? WHY IS THIS NAMED ASCII BUT USING UTF8
		public static string ReadStringFixedAscii(this BinaryReader r, int bytes)
		{
			var read = new byte[bytes];
			r.Read(read, 0, bytes);
			return Encoding.UTF8.GetString(read);
		}

		public static string ReadStringUtf8NullTerminated(this BinaryReader br)
		{
			MemoryStream ms = new MemoryStream();
			for (; ; )
			{
				var b = br.ReadByte();
				if (b == 0)
					return System.Text.Encoding.UTF8.GetString(ms.ToArray());
				ms.WriteByte(b);
			}
		}

		public static void CopyTo(this Stream src, Stream dest)
		{
			int size = (src.CanSeek) ? Math.Min((int)(src.Length - src.Position), 0x2000) : 0x2000;
			byte[] buffer = new byte[size];
			int n;
			do
			{
				n = src.Read(buffer, 0, buffer.Length);
				dest.Write(buffer, 0, n);
			} while (n != 0);
		}

		public static void CopyTo(this MemoryStream src, Stream dest)
		{
			dest.Write(src.GetBuffer(), (int)src.Position, (int)(src.Length - src.Position));
		}

		public static void CopyTo(this Stream src, MemoryStream dest)
		{
			if (src.CanSeek)
			{
				int pos = (int)dest.Position;
				int length = (int)(src.Length - src.Position) + pos;
				dest.SetLength(length);

				while (pos < length)
				{
					pos += src.Read(dest.GetBuffer(), pos, length - pos);
				}
			}
			else
			{
				src.CopyTo(dest);
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

		public static int[] ReadInt32s(this BinaryReader br, int num)
		{
			int[] ret = new int[num];
			for (int i = 0; i < num; i++)
			{
				ret[i] = br.ReadInt32();
			}

			return ret;
		}

		public static short[] ReadInt16s(this BinaryReader br, int num)
		{
			short[] ret = new short[num];
			for (int i = 0; i < num; i++)
			{
				ret[i] = br.ReadInt16();
			}

			return ret;
		}

		public static ushort[] ReadUInt16s(this BinaryReader br, int num)
		{
			ushort[] ret = new ushort[num];
			for (int i = 0; i < num; i++)
			{
				ret[i] = br.ReadUInt16();
			}

			return ret;
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
