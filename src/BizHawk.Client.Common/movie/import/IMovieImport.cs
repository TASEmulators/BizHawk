using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieImport
	{
		ImportResult Import(
			IDialogParent dialogParent,
			IMovieSession session,
			string path,
			Config config);
	}

	internal abstract class MovieImporter : IMovieImport
	{
		protected const string EmulationOrigin = "emuOrigin";
		protected const string MovieOrigin = "MovieOrigin";

		protected IDialogParent _dialogParent;
		private delegate bool MatchesMovieHash(ReadOnlySpan<byte> romData);

		public ImportResult Import(
			IDialogParent dialogParent,
			IMovieSession session,
			string path,
			Config config)
		{
			_dialogParent = dialogParent;
			SourceFile = new FileInfo(path);
			Config = config;

			if (!SourceFile.Exists)
			{
				Result.Errors.Add($"Could not find the file {path}");
				return Result;
			}

			var newFileName = $"{SourceFile.FullName}.{Bk2Movie.Extension}";
			Result.Movie = session.Get(newFileName);
			RunImport();

			if (!Result.Errors.Any())
			{
				if (string.IsNullOrEmpty(Result.Movie.Hash))
				{
					string hash = null;
					// try to generate a matching hash from the original ROM
					if (Result.Movie.HeaderEntries.TryGetValue(HeaderKeys.Crc32, out string crcHash))
					{
						hash = PromptForRom(data => string.Equals(CRC32Checksum.ComputeDigestHex(data), crcHash, StringComparison.OrdinalIgnoreCase));
					}
					else if (Result.Movie.HeaderEntries.TryGetValue(HeaderKeys.Md5, out string md5Hash))
					{
						hash = PromptForRom(data => string.Equals(MD5Checksum.ComputeDigestHex(data), md5Hash, StringComparison.OrdinalIgnoreCase));
					}
					else if (Result.Movie.HeaderEntries.TryGetValue(HeaderKeys.Sha256, out string sha256Hash))
					{
						hash = PromptForRom(data => string.Equals(SHA256Checksum.ComputeDigestHex(data), sha256Hash, StringComparison.OrdinalIgnoreCase));
					}

					if (hash is not null)
						Result.Movie.Hash = hash;
				}

				Result.Movie.Save();
			}

			return Result;
		}

		/// <summary>
		/// Prompts the user for a ROM file that matches the original movie file's hash
		/// and returns a SHA1 hash of that ROM file.
		/// </summary>
		/// <param name="matchesMovieHash">Function that checks whether the ROM data matches the original hash</param>
		/// <returns>SHA1 hash of the selected ROM file</returns>
		private string PromptForRom(MatchesMovieHash matchesMovieHash)
		{
			string messageBoxText = "Please select the original ROM to finalize the import process.";
			while (true)
			{
				if (!_dialogParent.ModalMessageBox2(messageBoxText, "ROM required to populate hash", useOKCancel: true))
					return null;

				var result = _dialogParent.ShowFileOpenDialog(
					filter: RomLoader.RomFilter,
					initDir: Config.PathEntries.RomAbsolutePath(Result.Movie.SystemID));
				if (result is null)
					return null; // skip hash migration when the dialog was canceled

				using var rom = new HawkFile(result);
				if (rom.IsArchive) rom.BindFirst();
				var romData = (ReadOnlySpan<byte>) rom.ReadAllBytes();
				int headerBytes = romData.Length % 1024; // assume that all roms have sizes divisible by 1024, and any rest is header
				romData = romData[headerBytes..];
				if (matchesMovieHash(romData))
					return SHA1Checksum.ComputeDigestHex(romData);

				messageBoxText = "The selected ROM does not match the movie's hash. Please try again.";
			}
		}

		protected Config Config { get; private set; }

		protected ImportResult Result { get; } = new ImportResult();

		protected FileInfo SourceFile { get; private set; }

		protected abstract void RunImport();

		// Get the content for a particular header.
		protected static string ParseHeader(string line, string headerName)
		{
			// Case-insensitive search.
			int x = line.LastIndexOf(headerName, StringComparison.OrdinalIgnoreCase) + headerName.Length;
			string str = line.Substring(x + 1, line.Length - x - 1);
			return str.Trim();
		}

		private static readonly Regex WhitespacePattern = new(@"\s+");

		// Reduce all whitespace to single spaces.
		protected static string SingleSpaces(string line)
			=> WhitespacePattern.Replace(line, " ");

		// Ends the string where a NULL character is found.
		protected static string NullTerminated(string str)
		{
			int pos = str.IndexOf('\0');
			if (pos != -1)
			{
				str = str.Substring(0, pos);
			}

			return str;
		}
	}

	public class ImportResult
	{
		public IList<string> Warnings { get; } = new List<string>();
		public IList<string> Errors { get; } = new List<string>();

		public IMovie Movie { get; set; }

		public static ImportResult Error(string errorMsg)
		{
			var result = new ImportResult();
			result.Errors.Add(errorMsg);
			return result;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ImporterForAttribute : Attribute
	{
		public ImporterForAttribute(string emulator, string extension)
		{
			Emulator = emulator;
			Extension = extension;
		}

		public string Emulator { get; }
		public string Extension { get; }
	}
}
