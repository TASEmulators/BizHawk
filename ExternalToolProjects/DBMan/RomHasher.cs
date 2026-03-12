#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.DBManTool
{
	public class InitialRomInfo
	{
		public string FileName;
		public string Name;
		public string VersionTags;
		public string GuessedSystem;
		public string GuessedRegion;
		public string CRC32;
		public string MD5;
		public string SHA1;
		public long Size;

		public override string ToString()
		{
			return FileName;
		}
	}

	public static class RomHasher
	{
		public static InitialRomInfo Generate(string file)
		{
			//if (isDiscImage(file))
//				return HashDiscImage(file);

			return GenerateRomHashDirect(file, File.ReadAllBytes(file));
		}

		static char[] modifierStartChars = { '(', '[' };
		static InitialRomInfo GenerateRomHashDirect(string file, byte[] filebytes)
		{
			var info = new InitialRomInfo();
			var fileInfo = new FileInfo(file);
			string ext = fileInfo.Extension.ToLowerInvariant().Replace(".", "");
			info.FileName = fileInfo.Name;

			// Parse the filename to guess things about the rom
			var name = Path.GetFileNameWithoutExtension(fileInfo.Name);
			if (name.StartsWithOrdinal("[BIOS] "))
				name = name.Replace("[BIOS] ","") + " [BIOS]";

			string modifiers = "";
			int modIndex = name.IndexOfAny(modifierStartChars);
			if (modIndex > 0)
			{
				modifiers = name.Substring(modIndex);
				name = name.Substring(0, modIndex);
			}
			info.Name = name.Trim();

			// parse out modifiers
			var mods = new List<string>();
			modifiers = modifiers.Replace(')', ';').Replace(']', ';');
			modifiers = modifiers.Replace("(", "").Replace("[", "");
			var m_ = modifiers.Split(';');
			foreach (var mi in m_)
			{
				var m = mi.Trim();
				if (m.Length == 0) continue;
				mods.Add(m);
			}

			info.VersionTags = "";
			foreach (var mi in mods)
			{
				if (info.VersionTags.Length != 0)
					info.VersionTags += ";";
				
				switch (mi.ToLowerInvariant())
				{
					case "j":
					case "jp":
					case "jpn":
					case "japan":
						info.GuessedRegion = "Japan";
						break;
					case "usa":
					case "us":
					case "u":
						info.GuessedRegion = "USA";
						break;
					case "europe":
					case "eur":
					case "e":
						info.GuessedRegion = "Europe";
						break;
					case "world":
					case "w":
						info.GuessedRegion = "World";
						break;
					case "korea":
					case "kr":
					case "k":
						info.GuessedRegion = "Korea";
						break;
					case "brazil":
					case "br":
						info.GuessedRegion = "Brazil";
						break;
					case "taiwan":
					case "tw":
						info.GuessedRegion = "Taiwan";
						break;
					case "usa, europe":
						info.GuessedRegion = "USA;Europe";
						break;
					case "japan, europe":
						info.GuessedRegion = "Europe;Japan";
						break;
					case "japan, usa":
						info.GuessedRegion = "USA;Japan";
						break;

					default:
						info.VersionTags += mi;
						break;
				}
			}

			// transform binary to canonical binary representation (de-header/de-stripe/de-swap)
			byte[] romBytes = filebytes;
			switch (ext)
			{
				case "sms":
				case "gg":
				case "sg":
				case "pce":
				case "sgx":
					romBytes = MaybeStripHeader512(filebytes);
					break;

				case "smd":
					if (filebytes.Length % 1024 == 512)
						System.Windows.Forms.MessageBox.Show("derp");
					romBytes = DeInterleaveSMD(filebytes);
					break;

				case "z64":
				case "n64":
				case "v64":
					throw new NotImplementedException("n64 demutate not done");
			}
			
			// guess system
			switch (ext)
			{
				case "sms": info.GuessedSystem = "SMS"; break;
				case "gg":  info.GuessedSystem = "GG"; break;
				case "sg":  info.GuessedSystem = "SG"; break;
				case "pce": info.GuessedSystem = "PCE"; break;
				case "sgx": info.GuessedSystem = "SGX"; break;
				case "smd":
				case "gen": info.GuessedSystem = "GEN"; break;
				case "nes": info.GuessedSystem = "NES"; break;
				default: info.GuessedSystem = "Unknown"; break;
			}

			// Perform hashing
			info.CRC32 = Hash_CRC32(romBytes);
			info.MD5 = Hash_MD5(romBytes);
			info.SHA1 = Hash_SHA1(romBytes);
			info.Size = romBytes.Length;

			return info;
		}

		static string HashDiscImage(string file)
		{
			try
			{
				string ext = new FileInfo(file).Extension.ToLowerInvariant();
				using (var disc = Disc.LoadAutomagic(file))
				{
					var hasher = new DiscHasher(disc);
					return hasher.OldHash();
				}
			}
			catch
			{
				return "Error Hashing Disc";
			}
		}

		static string Hash_CRC32(byte[] data) => $"{CRC32.Calculate(data):X8}";

		static string Hash_SHA1(byte[] data)
		{
			using (var sha1 = System.Security.Cryptography.SHA1.Create())
			{
				sha1.TransformFinalBlock(data, 0, data.Length);
				return BytesToHexString(sha1.Hash);
			}
		}

		static string Hash_MD5(byte[] data)
		{
			using (var md5 = System.Security.Cryptography.MD5.Create())
			{
				md5.TransformFinalBlock(data, 0, data.Length);
				return BytesToHexString(md5.Hash);
			}
		}

		static string BytesToHexString(byte[] bytes)
		{
			var sb = new StringBuilder();
			foreach (var b in bytes)
				sb.AppendFormat("{0:X2}", b);

			return sb.ToString();
		}

		static byte[] MaybeStripHeader512(byte[] fileBytes)
		{
			if (fileBytes.Length % 1024 != 512)
				return fileBytes;

			var romBytes = new byte[fileBytes.Length - 512];
			Array.Copy(fileBytes, 512, romBytes, 0, fileBytes.Length - 512);
			return romBytes;
		}

		static byte[] DeInterleaveSMD(byte[] source)
		{
			int size = source.Length;
			if (size > 0x400000)
				size = 0x400000;

			int pages = size / 0x4000;
			var output = new byte[size];

			for (int page = 0; page < pages; page++)
			{
				for (int i = 0; i < 0x2000; i++)
				{
					output[(page * 0x4000) + (i * 2) + 0] = source[(page * 0x4000) + 0x2000 + i];
					output[(page * 0x4000) + (i * 2) + 1] = source[(page * 0x4000) + 0x0000 + i];
				}
			}
			return output;
		}
		
		static bool isDiscImage(string file)
		{
			var ext = new FileInfo(file).Extension.ToLowerInvariant();
			if (ext == ".cue" || ext == ".iso")
				return true;
			return false;
		}
	}
}
