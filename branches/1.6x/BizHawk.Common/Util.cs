using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BizHawk.Common
{
	public static unsafe class Util
	{
		private static readonly char[] HexConvArr = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
		private static System.Runtime.InteropServices.GCHandle HexConvHandle;

		static Util()
		{
			HexConvHandle = System.Runtime.InteropServices.GCHandle.Alloc(HexConvArr, System.Runtime.InteropServices.GCHandleType.Pinned);
			HexConvPtr = (char*)HexConvHandle.AddrOfPinnedObject().ToPointer();
		}

		public static char* HexConvPtr { get; set; }

		public static string Hash_MD5(byte[] data, int offset, int len)
		{
			using (var md5 = System.Security.Cryptography.MD5.Create())
			{
				md5.ComputeHash(data, offset, len);
				return BytesToHexString(md5.Hash);
			}
		}

		public static string Hash_MD5(byte[] data)
		{
			return Hash_MD5(data, 0, data.Length);
		}

		public static string Hash_SHA1(byte[] data, int offset, int len)
		{
			using (var sha1 = System.Security.Cryptography.SHA1.Create())
			{
				sha1.ComputeHash(data, offset, len);
				return BytesToHexString(sha1.Hash);
			}
		}

		public static string Hash_SHA1(byte[] data)
		{
			return Hash_SHA1(data, 0, data.Length);
		}

		public static bool IsPowerOfTwo(int x)
		{
			if (x == 0 || x == 1)
			{
				return true;
			}

			return (x & (x - 1)) == 0;
		}

		public static int SaveRamBytesUsed(byte[] saveRam)
		{
			for (var i = saveRam.Length - 1; i >= 0; i--)
			{
				if (saveRam[i] != 0)
				{
					return i + 1;
				}
			}

			return 0;
		}

		// Read bytes from a BinaryReader and translate them into the UTF-8 string they represent.
		public static string ReadStringFixedAscii(this BinaryReader r, int bytes)
		{
			var read = new byte[bytes];
			for (var b = 0; b < bytes; b++)
			{
				read[b] = r.ReadByte();
			}

			return Encoding.UTF8.GetString(read);
		}

		public static string ReadStringAsciiZ(this BinaryReader r)
		{
			var sb = new StringBuilder();
			for (;;)
			{
				int b = r.ReadByte();
				if (b <= 0)
				{
					break;
				}

				sb.Append((char)b);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Converts bytes to an uppercase string of hex numbers in upper case without any spacing or anything
		/// //could be extension method
		/// </summary>
		public static string BytesToHexString(byte[] bytes)
		{
			var sb = new StringBuilder();
			foreach (var b in bytes)
			{
				sb.AppendFormat("{0:X2}", b);
			}

			return sb.ToString();
		}

		// Could be extension method
		public static byte[] HexStringToBytes(string str)
		{
			var ms = new MemoryStream();
			if (str.Length % 2 != 0)
			{
				throw new ArgumentException();
			}

			int len = str.Length / 2;
			for (int i = 0; i < len; i++)
			{
				int d = 0;
				for (int j = 0; j < 2; j++)
				{
					var c = char.ToLower(str[(i * 2) + j]);
					if (c >= '0' && c <= '9')
					{
						d += c - '0';
					}
					else if (c >= 'a' && c <= 'f')
					{
						d += (c - 'a') + 10;
					}
					else
					{
						throw new ArgumentException();
					}

					if (j == 0)
					{
						d <<= 4;
					}
				}

				ms.WriteByte((byte)d);
			}

			return ms.ToArray();
		}

		// Could be extension method
		public static void WriteByteBuffer(BinaryWriter bw, byte[] data)
		{
			if (data == null)
			{
				bw.Write(0);
			}
			else
			{
				bw.Write(data.Length);
				bw.Write(data);
			}
		}

		public static short[] ByteBufferToShortBuffer(byte[] buf)
		{
			int num = buf.Length / 2;
			var ret = new short[num];
			for (int i = 0; i < num; i++)
			{
				ret[i] = (short)(buf[i * 2] | (buf[i * 2 + 1] << 8));
			}

			return ret;
		}

		public static byte[] ShortBufferToByteBuffer(short[] buf)
		{
			int num = buf.Length;
			var ret = new byte[num * 2];
			for (int i = 0; i < num; i++)
			{
				ret[i * 2 + 0] = (byte)(buf[i] & 0xFF);
				ret[i * 2 + 1] = (byte)((buf[i] >> 8) & 0xFF);
			}

			return ret;
		}

		public static uint[] ByteBufferToUintBuffer(byte[] buf)
		{
			int num = buf.Length / 4;
			var ret = new uint[num];
			for (int i = 0; i < num; i++)
			{
				ret[i] = (uint)(buf[i * 4] | (buf[i * 4 + 1] << 8) | (buf[i * 4 + 2] << 16) | (buf[i * 4 + 3] << 24));
			}

			return ret;
		}

		public static byte[] UintBufferToByteBuffer(uint[] buf)
		{
			int num = buf.Length;
			var ret = new byte[num * 4];
			for (int i = 0; i < num; i++)
			{
				ret[i * 4 + 0] = (byte)(buf[i] & 0xFF);
				ret[i * 4 + 1] = (byte)((buf[i] >> 8) & 0xFF);
				ret[i * 4 + 2] = (byte)((buf[i] >> 16) & 0xFF);
				ret[i * 4 + 3] = (byte)((buf[i] >> 24) & 0xFF);
			}

			return ret;
		}

		public static int[] ByteBufferToIntBuffer(byte[] buf)
		{
			int num = buf.Length / 4;
			var ret = new int[num];
			for (int i = 0; i < num; i++)
			{
				ret[i] = buf[(i * 4) + 3];
				ret[i] <<= 8;
				ret[i] |= buf[(i * 4) + 2];
				ret[i] <<= 8;
				ret[i] |= buf[(i * 4) + 1];
				ret[i] <<= 8;
				ret[i] |= buf[(i * 4)];
			}

			return ret;
		}

		public static byte[] IntBufferToByteBuffer(int[] buf)
		{
			int num = buf.Length;
			var ret = new byte[num * 4];
			for (int i = 0; i < num; i++)
			{
				ret[i * 4 + 0] = (byte)(buf[i] & 0xFF);
				ret[i * 4 + 1] = (byte)((buf[i] >> 8) & 0xFF);
				ret[i * 4 + 2] = (byte)((buf[i] >> 16) & 0xFF);
				ret[i * 4 + 3] = (byte)((buf[i] >> 24) & 0xFF);
			}

			return ret;
		}

		public static byte[] ReadByteBuffer(BinaryReader br, bool returnNull)
		{
			int len = br.ReadInt32();
			if (len == 0 && returnNull)
			{
				return null;
			}

			var ret = new byte[len];
			int ofs = 0;
			while (len > 0)
			{
				int done = br.Read(ret, ofs, len);
				ofs += done;
				len -= done;
			}

			return ret;
		}

		public static int Memcmp(void* a, string b, int len)
		{
			fixed (byte* bp = Encoding.ASCII.GetBytes(b))
				return Memcmp(a, bp, len);
		}

		public static int Memcmp(void* a, void* b, int len)
		{
			var ba = (byte*)a;
			var bb = (byte*)b;
			for (int i = 0; i < len; i++)
			{
				byte _a = ba[i];
				byte _b = bb[i];
				int c = _a - _b;
				if (c != 0)
				{
					return c;
				}
			}

			return 0;
		}

		public static void Memset(void* ptr, int val, int len)
		{
			var bptr = (byte*)ptr;
			for (int i = 0; i < len; i++)
			{
				bptr[i] = (byte)val;
			}
		}

		public static void Memset32(void* ptr, int val, int len)
		{
			System.Diagnostics.Debug.Assert(len % 4 == 0);
			int dwords = len / 4;
			int* dwptr = (int*)ptr;
			for (int i = 0; i < dwords; i++)
			{
				dwptr[i] = val;
			}
		}

		public static byte[] ReadAllBytes(Stream stream)
		{
			const int BUFF_SIZE = 4096;
			var buffer = new byte[BUFF_SIZE];

			int bytesRead = 0;
			var inStream = new BufferedStream(stream);
			var outStream = new MemoryStream();

			while ((bytesRead = inStream.Read(buffer, 0, BUFF_SIZE)) > 0)
			{
				outStream.Write(buffer, 0, bytesRead);
			}

			return outStream.ToArray();
		}

		public static byte BinToBCD(this byte v)
		{
			return (byte)(((v / 10) * 16) + (v % 10));
		}

		public static byte BCDtoBin(this byte v)
		{
			return (byte)(((v / 16) * 10) + (v % 16));
		}

		public static string FormatFileSize(long filesize)
		{
			decimal size = filesize;

			const decimal OneKiloByte = 1024M;
			const decimal OneMegaByte = OneKiloByte * 1024M;
			decimal OneGigaByte = OneMegaByte * 1024M;

			string suffix;
			if (size > 1024 * 1024 * 1024)
			{
				size /= 1024 * 1024 * 1024;
				suffix = "GB";
			}
			else if (size > 1024 * 1024)
			{
				size /= 1024 * 1024;
				suffix = "MB";
			}
			else if (size > 1024)
			{
				size /= 1024;
				suffix = "KB";
			}
			else
			{
				suffix = " B";
			}

			const string precision = "2";
			return string.Format("{0:N" + precision + "}{1}", size, suffix);
		}

		// http://stackoverflow.com/questions/3928822/comparing-2-dictionarystring-string-instances
		public static bool DictionaryEqual<TKey, TValue>(
			IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
		{
			if (first == second)
			{
				return true;
			}

			if ((first == null) || (second == null))
			{
				return false;
			}

			if (first.Count != second.Count)
			{
				return false;
			}

			var comparer = EqualityComparer<TValue>.Default;

			foreach (var kvp in first)
			{
				TValue secondValue;
				if (!second.TryGetValue(kvp.Key, out secondValue))
				{
					return false;
				}

				if (!comparer.Equals(kvp.Value, secondValue))
				{
					return false;
				}
			}

			return true;
		}

		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
		{
			TValue ret;
			dict.TryGetValue(key, out ret);
			return ret;
		}

		public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultvalue)
		{
			TValue ret;
			if (!dict.TryGetValue(key, out ret))
			{
				return defaultvalue;
			}
			
			return ret;
		}
	}

	[Serializable]
	public class NotTestedException : Exception
	{
	}

	internal class SuperGloballyUniqueID
	{
		private static readonly string StaticPart;
		private static int ctr;

		static SuperGloballyUniqueID()
		{
			StaticPart = "bizhawk-" + System.Diagnostics.Process.GetCurrentProcess().Id + "-" + Guid.NewGuid();
		}

		public static string Next()
		{
			int myctr;
			lock (typeof(SuperGloballyUniqueID))
			{
				myctr = ctr++;
			}

			return StaticPart + "-" + myctr;
		}
	}
}
