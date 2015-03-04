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
		public const string COMMENT = "comment";
		public const string COREORIGIN = "CoreOrigin";
		public const string CRC16 = "CRC16";
		public const string CRC32 = "CRC32";
		public const string EMULATIONORIGIN = "emuOrigin";
		public const string GAMECODE = "GameCode";
		public const string INTERNALCHECKSUM = "InternalChecksum";
		public const string JAPAN = "Japan";
		public const string MD5 = "MD5";
		public const string MOVIEORIGIN = "MovieOrigin";
		public const string PORT1 = "port1";
		public const string PORT2 = "port2";
		public const string PROJECTID = "ProjectID";
		public const string SHA256 = "SHA256";
		public const string SUPERGAMEBOYMODE = "SuperGameBoyMode";
		public const string STARTSECOND = "StartSecond";
		public const string STARTSUBSECOND = "StartSubSecond";
		public const string SYNCHACK = "SyncHack";
		public const string UNITCODE = "UnitCode";

		public ImportResult Import(string path)
		{
			SourceFile = new FileInfo(path);

			if (!SourceFile.Exists)
			{
				Result.Errors.Add(string.Format("Could not find the file {0}", path));
				return Result;
			}

			var newFileName = SourceFile.FullName + "." + Bk2Movie.Extension;
			Result.Movie = new Bk2Movie(newFileName);

			RunImport();

			return Result;
		}


		protected ImportResult Result = new ImportResult();

		protected FileInfo SourceFile;

		protected abstract void RunImport();

		// Get the content for a particular header.
		protected static string ParseHeader(string line, string headerName)
		{
			// Case-insensitive search.
			int x = line.ToLower().LastIndexOf(
				headerName.ToLower()
			) + headerName.Length;
			string str = line.Substring(x + 1, line.Length - x - 1);
			return str.Trim();
		}
	}

	public class ImportResult
	{
		public ImportResult()
		{
			Warnings = new List<string>();
			Errors = new List<string>();
		}

		public IList<string> Warnings { get; private set; }
		public IList<string> Errors { get; private set; }

		public Bk2Movie Movie { get; set; }
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class ImportExtension : Attribute
	{
		public ImportExtension(string extension)
		{
			Extension = extension;
		}

		public string Extension { get; private set; }
	}
}
