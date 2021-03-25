using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieImport
	{
		ImportResult Import(
			IDialogParent dialogParent,
			IMovieSession session,
			IEmulator emulator,
			string path,
			Config config);
	}

	internal abstract class MovieImporter : IMovieImport
	{
		protected const string EmulationOrigin = "emuOrigin";
		protected const string Md5 = "MD5";
		protected const string MovieOrigin = "MovieOrigin";

		protected IDialogParent _dialogParent;

		public ImportResult Import(
			IDialogParent dialogParent,
			IMovieSession session,
			IEmulator emulator,
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
			Result.Movie.Attach(emulator);
			RunImport();

			if (!Result.Errors.Any())
			{
				Result.Movie.Save();
			}

			return Result;
		}

		protected Config Config { get; private set; }

		protected ImportResult Result { get; } = new ImportResult();

		protected FileInfo SourceFile { get; private set; }

		protected abstract void RunImport();

		// Get the content for a particular header.
		protected static string ParseHeader(string line, string headerName)
		{
			// Case-insensitive search.
			int x = line.ToLower().LastIndexOf(
				headerName.ToLower()) + headerName.Length;
			string str = line.Substring(x + 1, line.Length - x - 1);
			return str.Trim();
		}

		// Reduce all whitespace to single spaces.
		protected static string SingleSpaces(string line)
		{
			line = line.Replace("\t", " ");
			line = line.Replace("\n", " ");
			line = line.Replace("\r", " ");
			line = line.Replace("\r\n", " ");
			string prev;
			do
			{
				prev = line;
				line = line.Replace("  ", " ");
			}
			while (prev != line);
			return line;
		}

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
