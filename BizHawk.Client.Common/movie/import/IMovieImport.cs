using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public interface IMovieImport
	{
		ImportResult Import(string path);
	}

	public abstract class MovieImporter : IMovieImport
	{
		protected const string EmulationOrigin = "emuOrigin";
		protected const string MD5 = "MD5";
		protected const string MovieOrigin = "MovieOrigin";

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

		[Obsolete("Use ConfigService.SaveWithType() instead")]
		protected static string ToJson(object syncSettings)
		{
			// Annoying kludge to force the json serializer to serialize the type name for "o" object.
			// For just the "o" object to have type information, it must be cast to a superclass such
			// that the TypeNameHandling.Auto decides to serialize the type as well as the object
			// contents.  As such, the object cast is NOT redundant
			var jsonSettings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto
			};

			return JsonConvert.SerializeObject(new { o = (object)syncSettings }, jsonSettings);
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
