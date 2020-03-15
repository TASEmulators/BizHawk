using System;
using System.Linq;
using System.IO;

namespace BizHawk.Common.PathExtensions
{
	public static class PathExtensions
	{
		public static string RemoveInvalidFileSystemChars(this string name)
		{
			var newStr = name;
			var chars = Path.GetInvalidFileNameChars();
			return chars.Aggregate(newStr, (current, c) => current.Replace(c.ToString(), ""));
		}

		/// <summary>
		/// Decides if a path is non-empty, not . and not .\
		/// </summary>
		public static bool PathIsSet(this string path)
		{
			return !string.IsNullOrWhiteSpace(path) && path != "." && path != ".\\";
		}

		/// <remarks>Algorithm for Windows taken from https://stackoverflow.com/a/7710620/7467292</remarks>
		public static bool IsSubfolderOf(this string childPath, string parentPath)
		{
			if (OSTailoredCode.IsUnixHost)
			{
#if true
				return OSTailoredCode.SimpleSubshell("realpath", $"-L \"{childPath}\"", $"invalid path {childPath} or missing realpath binary")
					.StartsWith(OSTailoredCode.SimpleSubshell("realpath", $"-L \"{parentPath}\"", $"invalid path {parentPath} or missing realpath binary"));
#else // written for Unix port but may be useful for Windows when moving to .NET Core
				var parentUriPath = new Uri(parentPath.TrimEnd('.')).AbsolutePath.TrimEnd('/');
				try
				{
					for (var childUri = new DirectoryInfo(childPath).Parent; childUri != null; childUri = childUri?.Parent)
					{
						if (new Uri(childUri.FullName).AbsolutePath.TrimEnd('/') == parentUriPath) return true;
					}
				}
				catch
				{
					// ignored
				}
				return false;
#endif
			}

			var parentUri = new Uri(parentPath);
			for (var childUri = new DirectoryInfo(childPath).Parent; childUri != null; childUri = childUri?.Parent)
			{
				if (new Uri(childUri.FullName) == parentUri) return true;
			}

			return false;
		}

		public static string MakeRelativeTo(this string absolutePath, string basePath)
		{
			if (absolutePath.IsSubfolderOf(basePath))
			{
				return absolutePath.Replace(basePath, ".");
			}

			return absolutePath;
		}

		public static string FilesystemSafeName(this string? name)
		{
			name ??= "";

			var filesystemSafeName = name
				.Replace("|", "+")
				.Replace(":", " -") // Path.GetFileName scraps everything to the left of a colon unfortunately, so we need this hack here
				.Replace("\"", "")  // Ivan IronMan Stewart's Super Off-Road has quotes in game name
				.Replace("/", "+"); // Mario Bros / Duck hunt has a slash in the name which GetDirectoryName and GetFileName treat as if it were a folder

			// zero 06-nov-2015 - regarding the below, i changed my mind. for libretro i want subdirectories here.
			var filesystemDir = Path.GetDirectoryName(filesystemSafeName);
			filesystemSafeName = Path.GetFileName(filesystemSafeName);

			filesystemSafeName = filesystemSafeName.RemoveInvalidFileSystemChars();

			// zero 22-jul-2012 - i don't think this is used the same way it used to. game.Name shouldn't be a path, so this stuff is illogical.
			// if game.Name is a path, then someone should have made it not-a-path already.
			// return Path.Combine(Path.GetDirectoryName(filesystemSafeName), Path.GetFileNameWithoutExtension(filesystemSafeName));

			// adelikat:
			// This hack is to prevent annoying things like Super Mario Bros..bk2
			if (filesystemSafeName.EndsWith("."))
			{
				filesystemSafeName = filesystemSafeName.Remove(filesystemSafeName.Length - 1, 1);
			}

			return Path.Combine(filesystemDir, filesystemSafeName);
		}
	}
}
