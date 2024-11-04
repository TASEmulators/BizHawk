using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BizHawk.Common
{
	/// <summary>
	/// Goals
	/// 1. Allows for encrypting/decrypting strings, suitable for any sensitive info (e.g. api tokens) to be placed in a config file
	/// 2. Encryption is unique to a particular machine, so another person with the string can't decrypt it
	/// 3. ASSUMES ANY ATTACK HAS NO KNOWLEDGE OF THE MACHINE NAME
	/// 4. As such, DOES NOT protect against attacks which have access to said machine or within the same LAN
	/// (This is mainly to mitigate sensitive info leaks in the case the user posted the config publicly, unredacted)
	/// (For anyone who's generally security minded, don't trust this to keep your tokens safe)
	/// </summary>
	public static class SensitiveStrings
	{
		private static readonly Aes _aes;

		static SensitiveStrings()
		{
			_aes = Aes.Create();
			// lame, but this seems to be the best method to do this...
			_aes.Key = SHA256Checksum.Compute(Encoding.UTF8.GetBytes(Environment.MachineName));
			_aes.IV = new byte[_aes.BlockSize / 8];
			_aes.Mode = CipherMode.CBC;
			_aes.Padding = PaddingMode.PKCS7;
		}

		public static string EncryptString(string sensitiveString)
		{
			var bytes = Encoding.UTF8.GetBytes(sensitiveString);
			using var ms = new MemoryStream();
			using var encryptor = _aes.CreateEncryptor();
			using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
			{
				cryptoStream.Write(bytes, 0, bytes.Length);
			}

			return Convert.ToBase64String(ms.ToArray());
		}

		public static string DecryptString(string encryptedString)
		{
			try
			{
				if (string.IsNullOrEmpty(encryptedString))
				{
					return string.Empty;
				}

				var bytes = Convert.FromBase64String(encryptedString);
				using var ms = new MemoryStream();
				using var decryptor = _aes.CreateDecryptor();
				using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
				{
					cryptoStream.Write(bytes, 0, bytes.Length);
				}

				return Encoding.UTF8.GetString(ms.ToArray());
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
