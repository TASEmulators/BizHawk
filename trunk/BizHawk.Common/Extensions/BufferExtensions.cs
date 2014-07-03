using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace BizHawk.Common.BufferExtensions
{
	public static class BufferExtensions
	{
		public static void SaveAsHex(this byte[] buffer, TextWriter writer)
		{
			foreach (var b in buffer)
			{
				writer.Write("{0:X2}", b);
			}

			writer.WriteLine();
		}

		public unsafe static void SaveAsHexFast(this byte[] buffer, TextWriter writer)
		{
			char* table = Util.HexConvPtr;
			if (buffer.Length > 0)
			{
				int len = buffer.Length;
				fixed (byte* src = &buffer[0])
					for (int i = 0; i < len; i++)
					{
						writer.Write(table[src[i] >> 4]);
						writer.Write(table[src[i] & 15]);
					}
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this byte[] buffer, TextWriter writer, int length)
		{
			for (int i = 0; i < length; i++)
			{
				writer.Write("{0:X2}", buffer[i]);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this short[] buffer, TextWriter writer)
		{
			foreach (var b in buffer)
			{
				writer.Write("{0:X4}", b);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this ushort[] buffer, TextWriter writer)
		{
			foreach (var b in buffer)
			{
				writer.Write("{0:X4}", b);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this int[] buffer, TextWriter writer)
		{
			foreach (int b in buffer)
			{
				writer.Write("{0:X8}", b);
			}
			writer.WriteLine();
		}

		public static void SaveAsHex(this uint[] buffer, TextWriter writer)
		{
			foreach (var b in buffer)
			{
				writer.Write("{0:X8}", b);
			}
			writer.WriteLine();
		}

		public static void ReadFromHex(this byte[] buffer, string hex)
		{
			if (hex.Length % 2 != 0)
			{
				throw new Exception("Hex value string does not appear to be properly formatted.");
			}

			for (int i = 0; i < buffer.Length && i * 2 < hex.Length; i++)
			{
				var bytehex = "" + hex[i * 2] + hex[i * 2 + 1];
				buffer[i] = byte.Parse(bytehex, NumberStyles.HexNumber);
			}
		}

		public static unsafe void ReadFromHexFast(this byte[] buffer, string hex)
		{
			if (buffer.Length * 2 != hex.Length)
			{
				throw new Exception("Data size mismatch");
			}

			int count = buffer.Length;
			fixed (byte* _dst = buffer)
			fixed (char* _src = hex)
			{
				byte* dst = _dst;
				char* src = _src;
				while (count > 0)
				{
					// in my tests, replacing Hex2Int() with a 256 entry LUT slowed things down slightly
					*dst++ = (byte)(Hex2Int(*src++) << 4 | Hex2Int(*src++));
					count--;
				}
			}
		}

		public static void ReadFromHex(this short[] buffer, string hex)
		{
			if (hex.Length % 4 != 0)
			{
				throw new Exception("Hex value string does not appear to be properly formatted.");
			}

			for (int i = 0; i < buffer.Length && i * 4 < hex.Length; i++)
			{
				var shorthex = "" + hex[i * 4] + hex[(i * 4) + 1] + hex[(i * 4) + 2] + hex[(i * 4) + 3];
				buffer[i] = short.Parse(shorthex, NumberStyles.HexNumber);
			}
		}

		public static void ReadFromHex(this ushort[] buffer, string hex)
		{
			if (hex.Length % 4 != 0)
			{
				throw new Exception("Hex value string does not appear to be properly formatted.");
			}

			for (int i = 0; i < buffer.Length && i * 4 < hex.Length; i++)
			{
				var ushorthex = "" + hex[i * 4] + hex[(i * 4) + 1] + hex[(i * 4) + 2] + hex[(i * 4) + 3];
				buffer[i] = ushort.Parse(ushorthex, NumberStyles.HexNumber);
			}
		}

		public static void ReadFromHex(this int[] buffer, string hex)
		{
			if (hex.Length % 8 != 0)
			{
				throw new Exception("Hex value string does not appear to be properly formatted.");
			}

			for (int i = 0; i < buffer.Length && i * 8 < hex.Length; i++)
			{
				//string inthex = "" + hex[i * 8] + hex[(i * 8) + 1] + hex[(i * 4) + 2] + hex[(i * 4) + 3] + hex[(i*4
				var inthex = hex.Substring(i * 8, 8);
				buffer[i] = int.Parse(inthex, NumberStyles.HexNumber);
			}
		}

		/// <summary>
		/// Converts bytes to an uppercase string of hex numbers in upper case without any spacing or anything
		/// </summary>
		public static string BytesToHexString(this byte[] bytes)
		{
			var sb = new StringBuilder();
			foreach (var b in bytes)
			{
				sb.AppendFormat("{0:X2}", b);
			}

			return sb.ToString();
		}

		public static bool FindBytes(this byte[] array, byte[] pattern)
		{
			var fidx = 0;
			int result = Array.FindIndex(array, 0, array.Length, (byte b) =>
			{
				fidx = (b == pattern[fidx]) ? fidx + 1 : 0;
				return (fidx == pattern.Length);
			});

			return (result >= pattern.Length - 1);
		}

		public static string HashMD5(this byte[] data, int offset, int len)
		{
			using (var md5 = MD5.Create())
			{
				md5.ComputeHash(data, offset, len);
				return md5.Hash.BytesToHexString();
			}
		}

		public static string HashMD5(this byte[] data)
		{
			return HashMD5(data, 0, data.Length);
		}

		public static string HashSHA1(this byte[] data, int offset, int len)
		{
			using (var sha1 = SHA1.Create())
			{
				sha1.ComputeHash(data, offset, len);
				return sha1.Hash.BytesToHexString();
			}
		}

		public static string HashSHA1(this byte[] data)
		{
			return HashSHA1(data, 0, data.Length);
		}

		#region Helpers

		private static int Hex2Int(char c)
		{
			if (c <= '9')
			{
				return c - '0';
			}

			if (c <= 'F')
			{
				return c - '7';
			}

			return c - 'W';
		}

		#endregion
	}
}
