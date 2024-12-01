using System.IO;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32;

namespace BizHawk.Common
{
	/// <summary>
	/// Goals
	/// 1. Allows for encrypting/decrypting strings, suitable for any secret info (e.g. api tokens) to be placed in a config file
	/// 2. Encryption is unique to a particular machine, so another person with the string can't decrypt it
	/// 3. Uses very unique GUIDs to create an encryption key, along with Environment.MachineName (if as at least a fallback)
	/// 4. Generally, this should be sufficient to protect any string, assuming the attack does not have access to the target machine (no way to protect the strings at that point)
	/// 5. In the case Environment.MachineName is the only thing available, the protection is low for a skilled attack, but sufficient for low grade attacks.
	/// (Probably should just keep these strings outside of the main config file, but still protect them as to prevent secrets being present as plaintext)
	/// </summary>
	public static class SecretStrings
	{
		private static readonly Aes _aes;

		/// <summary>
		/// Obtains a machine GUID
		/// This is unique for a given machine and does not change based on the network config or hardware changes
		/// It should be considered a secret, something never intended to be exposed in the network
		/// (Note: Implementation matches Chromium's BrowserDMTokenStorage::Delegate::InitClientId)
		/// </summary>
		/// <returns>machine guid</returns>
		private static string GetMachineGuid()
		{
			try
			{
				if (OSTailoredCode.IsUnixHost)
				{
					// see https://www.freedesktop.org/software/systemd/man/latest/machine-id.html
					return File.ReadAllText("/etc/machine-id");
				}

				// windows can use HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography\MachineGuid
				using var subKey = RegistryKey
					.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
					.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
				return subKey!.GetValue("MachineGuid")!.ToString();
			}
			catch
			{
				// bad case mentioned in 5.
				// hopefully never happens in practice
				return string.Empty;
			}
		}

		static SecretStrings()
		{
			_aes = Aes.Create();
			var machineIdBytes = Encoding.UTF8.GetBytes($"{GetMachineGuid()}/{Environment.MachineName}");
			var machineIdHash = SHA256Checksum.Compute(machineIdBytes);
			_aes.Key = machineIdHash;
			_aes.IV = SHA256Checksum.Compute([ ..machineIdHash, ..machineIdBytes ]).AsSpan(0, 16).ToArray();
			_aes.Mode = CipherMode.CBC;
			_aes.Padding = PaddingMode.PKCS7;
		}

		public static string EncryptString(string secretString)
		{
			var bytes = Encoding.UTF8.GetBytes(secretString);
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
