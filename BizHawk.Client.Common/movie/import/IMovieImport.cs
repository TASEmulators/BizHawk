using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Client.Common
{
	public interface IMovieImport
	{
		ImportResult Import(string path);
	}

	public abstract class MovieImporter : IMovieImport
	{
		public const string Comment = "comment";
		public const string Coreorigin = "CoreOrigin";
		public const string CRC16 = "CRC16";
		public const string CRC32 = "CRC32";
		protected const string Emulationorigin = "emuOrigin";
		public const string Gamecode = "GameCode";
		public const string InternalChecksum = "InternalChecksum";
		public const string Japan = "Japan";
		protected const string MD5 = "MD5";
		protected const string Movieorigin = "MovieOrigin";
		public const string Port1 = "port1";
		public const string Port2 = "port2";
		public const string ProjectId = "ProjectID";
		public const string SHA256 = "SHA256";
		public const string SuperGameboyMode = "SuperGameBoyMode";
		public const string StartSecond = "StartSecond";
		public const string StartSubSecond = "StartSubSecond";
		public const string SyncHack = "SyncHack";
		public const string UnitCode = "UnitCode";

		public ImportResult Import(string path)
		{
			SourceFile = new FileInfo(path);

			if (!SourceFile.Exists)
			{
				Result.Errors.Add($"Could not find the file {path}");
				return Result;
			}

			var newFileName = $"{SourceFile.FullName}.{Bk2Movie.Extension}";
			Result.Movie = new Bk2Movie(newFileName);

			RunImport();

			return Result;
		}

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
	}

	public class ImportResult
	{
		public IList<string> Warnings { get; } = new List<string>();
		public IList<string> Errors { get; } = new List<string>();

		public Bk2Movie Movie { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class ImportExtensionAttribute : Attribute
	{
		public ImportExtensionAttribute(string extension)
		{
			Extension = extension;
		}

		public string Extension { get; }
	}
}
