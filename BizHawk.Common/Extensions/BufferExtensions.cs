#nullable disable

using System;
using System.Text;
using System.Security.Cryptography;

namespace BizHawk.Common.BufferExtensions
{
	public static class BufferExtensions
	{
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
				return fidx == pattern.Length;
			});

			return result >= pattern.Length - 1;
		}

		public static string HashMD5(this byte[] data, int offset, int len)
		{
			using var md5 = MD5.Create();
			md5.ComputeHash(data, offset, len);
			return md5.Hash.BytesToHexString();
		}

		public static string HashMD5(this byte[] data)
		{
			return HashMD5(data, 0, data.Length);
		}

		public static string HashSHA1(this byte[] data, int offset, int len)
		{
			using var sha1 = SHA1.Create();
			sha1.ComputeHash(data, offset, len);
			return sha1.Hash.BytesToHexString();
		}

		public static string HashSHA1(this byte[] data)
		{
			return HashSHA1(data, 0, data.Length);
		}
	}
}
