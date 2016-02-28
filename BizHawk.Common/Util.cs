using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Common.BufferExtensions;
using System.Reflection;

namespace BizHawk.Common
{
	public static unsafe partial class Util
	{
		public static void CopyStream(Stream src, Stream dest, long len)
		{
			const int size = 0x2000;
			byte[] buffer = new byte[size];
			while (len > 0)
			{
				long todo = len;
				if (len > size) todo = size;
				int n = src.Read(buffer, 0, (int)todo);
				dest.Write(buffer, 0, n);
				len -= n;
			}
		}

		/// <summary>
		/// Waits 250ms for a file to vanish. Returns whether it succeeded
		/// </summary>
		public static bool TryWaitForFileToVanish(string path)
		{
			for (int i = 0; i < 25; i++) //250ms
			{
				if (!File.Exists(path))
					return true;
				System.Threading.Thread.Sleep(10);
			}
			return false;
		}

		/// <summary>
		/// Tries to moves `pathWant` out of the way to `pathBackup`, delaying as needed to accomodate filesystem being sucky.
		/// `pathWant` might not be removed after all, in case it's snagged by something.
		/// </summary>
		public static bool TryMoveBackupFile(string pathWant, string pathBackup)
		{
			//If the path we want is available we dont actually have to make a backup
			if (!File.Exists(pathWant))
				return true;

			//delete any existing backup
			try
			{
				if (File.Exists(pathBackup))
					File.Delete(pathBackup);
			}
			catch
			{
				//just give up on the whole thing in case of exceptions. pathWant will get overwritten by the caller.
				return false;
			}

			//deletes are asynchronous, need to wait for it to be gone
			if(!TryWaitForFileToVanish(pathBackup))
			{
				//gave up waiting for existing backup to be gone. the whole thing's a total loss
				return false;
			}

			try
			{
				//actually move pathWant out of the way to pathBackup now that pathBackup is free
				File.Move(pathWant, pathBackup);
			}
			catch
			{
				// Eat it, this will happen rarely and the user will rarely need the file, so the odds of simply not making the backup is very unlikely
				return false;
			}

			//hmm these might be asynchronous too
			//wait for the move to complete, at least enough for pathWant to be cleared up
			return TryWaitForFileToVanish(pathWant);
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

		public static bool[] ByteBufferToBoolBuffer(byte[] buf)
		{
			var ret = new bool[buf.Length];
			for (int i = 0; i < buf.Length; i++)
			{
				ret[i] = buf[i] != 0;
			}
			return ret;
		}

		public static byte[] BoolBufferToByteBuffer(bool[] buf)
		{
			var ret = new byte[buf.Length];
			for (int i = 0; i < buf.Length; i++)
			{
				ret[i] = (byte)(buf[i] ? 1 : 0);
			}
			return ret;
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

		public static ushort[] ByteBufferToUshortBuffer(byte[] buf)
		{
			int num = buf.Length / 2;
			var ret = new ushort[num];
			for (int i = 0; i < num; i++)
			{
				ret[i] = (ushort)(buf[i * 2] | (buf[i * 2 + 1] << 8));
			}

			return ret;
		}

		public static byte[] UshortBufferToByteBuffer(ushort[] buf)
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

		public static string FormatFileSize(long filesize)
		{
			decimal size = filesize;

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
				suffix = "B";
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
	}

	public static class BitConverterLE
	{
		public static void WriteBytes(ushort value, byte[] dst, int index)
		{
			dst[index    ] = (byte)(value      );
			dst[index + 1] = (byte)(value >>  8);
		}

		public static void WriteBytes(uint value, byte[] dst, int index)
		{
			dst[index    ] = (byte)(value      );
			dst[index + 1] = (byte)(value >>  8);
			dst[index + 2] = (byte)(value >> 16);
			dst[index + 3] = (byte)(value >> 24);
		}
	}

	public static class VLInteger
	{
		public static void WriteUnsigned(uint value, byte[] data, ref int index)
		{
			// This is optimized for good performance on both the x86 and x64 JITs. Don't change anything without benchmarking.
			do
			{
				uint x = value & 0x7FU;
				value >>= 7;
				data[index++] = (byte)((value != 0U ? 0x80U : 0U) | x);
			}
			while (value != 0U);
		}

		public static uint ReadUnsigned(byte[] data, ref int index)
		{
			// This is optimized for good performance on both the x86 and x64 JITs. Don't change anything without benchmarking.
			uint value = 0U;
			int shiftCount = 0;
			bool isLastByte; // Negating the comparison and moving it earlier in the loop helps a lot on x86 for some reason
			do
			{
				uint x = (uint)data[index++];
				isLastByte = (x & 0x80U) == 0U;
				value |= (x & 0x7FU) << shiftCount;
				shiftCount += 7;
			}
			while (!isLastByte);
			return value;
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

	public static class ReflectionUtil
	{
		// http://stackoverflow.com/questions/9273629/avoid-giving-namespace-name-in-type-gettype
		/// <summary>
		/// Gets a all Type instances matching the specified class name with just non-namespace qualified class name.
		/// </summary>
		/// <param name="className">Name of the class sought.</param>
		/// <returns>Types that have the class name specified. They may not be in the same namespace.</returns>
		public static Type[] GetTypeByName(string className)
		{
			var returnVal = new List<Type>();

			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type[] assemblyTypes = a.GetTypes();
				for (int j = 0; j < assemblyTypes.Length; j++)
				{
					if (assemblyTypes[j].Name.ToLower() == className.ToLower())
					{
						returnVal.Add(assemblyTypes[j]);
					}
				}
			}

			return returnVal.ToArray();
		}
	}
}
