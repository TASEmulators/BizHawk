using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Common.PathExtensions
{
	public static class PathExtensions
	{
		/// <returns><see langword="true"/> iff <paramref name="childPath"/> indicates a child of <paramref name="parentPath"/>, with <see langword="false"/> being returned if either path is <see langword="null"/></returns>
		/// <remarks>algorithm for Windows taken from https://stackoverflow.com/a/7710620/7467292</remarks>
		public static bool IsSubfolderOf(this string? childPath, string? parentPath)
		{
			if (childPath == null || parentPath == null) return false;
			if (childPath == parentPath || childPath.StartsWith($"{parentPath}{Path.DirectorySeparatorChar}")) return true;

			if (OSTailoredCode.IsUnixHost)
			{
#if true
				var c = OSTailoredCode.SimpleSubshell("realpath", $"-Lm \"{childPath}\"", $"invalid path {childPath} or missing realpath binary");
				var p = OSTailoredCode.SimpleSubshell("realpath", $"-Lm \"{parentPath}\"", $"invalid path {parentPath} or missing realpath binary");
				return c == p || c.StartsWith($"{p}/");
#else // written for Unix port but may be useful for Windows when moving to .NET Core
				var parentUriPath = new Uri(parentPath.TrimEnd('.')).AbsolutePath.TrimEnd('/');
				try
				{
					for (var childUri = new DirectoryInfo(childPath).Parent; childUri != null; childUri = childUri.Parent)
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

			var parentUri = new Uri(parentPath.RemoveSuffix(Path.DirectorySeparatorChar));
			for (var childUri = new DirectoryInfo(childPath); childUri != null; childUri = childUri.Parent)
			{
				if (new Uri(childUri.FullName) == parentUri) return true;
			}
			return false;
		}

		/// <exception cref="ArgumentException">running on Windows host, and unmanaged call failed</exception>
		/// <exception cref="FileNotFoundException">running on Windows host, and either path is not a regular file or directory</exception>
		/// <remarks>Algorithm for Windows taken from https://stackoverflow.com/a/485516/7467292</remarks>
		public static string? GetRelativePath(string? fromPath, string? toPath)
		{
			if (fromPath == null || toPath == null) return null;
			if (OSTailoredCode.IsUnixHost) return fromPath.MakeRelativeTo(toPath);

			//TODO merge this with the Windows implementation in MakeRelativeTo
			static FileAttributes GetPathAttribute(string path1)
			{
				var di = new DirectoryInfo(path1.Split('|').First());
				if (di.Exists)
				{
					return FileAttributes.Directory;
				}

				var fi = new FileInfo(path1.Split('|').First());
				if (fi.Exists)
				{
					return FileAttributes.Normal;
				}

				throw new FileNotFoundException();
			}
			var path = new StringBuilder(260 /* = MAX_PATH */);
			return Win32Imports.PathRelativePathTo(path, fromPath, GetPathAttribute(fromPath), toPath, GetPathAttribute(toPath))
				? path.ToString()
				: throw new ArgumentException("Paths must have a common prefix");
		}

		/// <returns>the absolute path equivalent to <paramref name="path"/> which contains <c>%exe%</c> (expanded) as a prefix</returns>
		/// <remarks>
		/// returned string omits trailing slash<br/>
		/// note that the returned string is an absolute path and not a relative path; but TODO it was intended to be relative
		/// </remarks>
		public static string MakeProgramRelativePath(this string path) => Path.Combine(PathUtils.ExeDirectoryPath, path);

		/// <returns>the relative path which is equivalent to <paramref name="absolutePath"/> when the CWD is <paramref name="basePath"/>, or <see langword="null"/> if either path is <see langword="null"/></returns>
		/// <remarks>returned string omits trailing slash; implementation calls <see cref="IsSubfolderOf"/> for you</remarks>
		public static string? MakeRelativeTo(this string? absolutePath, string? basePath)
		{
			if (absolutePath == null || basePath == null) return null;
			if (!absolutePath.IsSubfolderOf(basePath)) return absolutePath;
			if (!OSTailoredCode.IsUnixHost) return absolutePath.Replace(basePath, ".").RemoveSuffix(Path.DirectorySeparatorChar);
#if true // Unix implementation using realpath
			var realpathOutput = OSTailoredCode.SimpleSubshell("realpath", $"--relative-to=\"{basePath}\" \"{absolutePath}\"", $"invalid path {absolutePath}, invalid path {basePath}, or missing realpath binary");
			return !realpathOutput.StartsWith("../") && realpathOutput != "." && realpathOutput != ".." ? $"./{realpathOutput}" : realpathOutput;
#else // for some reason there were two Unix implementations in the codebase before me? --yoshi
			// alt. #1
			if (!IsSubfolder(basePath, absolutePath)) return OSTailoredCode.IsUnixHost && basePath.TrimEnd('.') == $"{absolutePath}/" ? "." : absolutePath;
			return OSTailoredCode.IsUnixHost ? absolutePath.Replace(basePath.TrimEnd('.'), "./") : absolutePath.Replace(basePath, ".");

			// alt. #2; algorithm taken from https://stackoverflow.com/a/340454/7467292
			var dirSepChar = Path.DirectorySeparatorChar;
			var fromUri = new Uri(absolutePath.EndsWith(dirSepChar.ToString()) ? absolutePath : absolutePath + dirSepChar);
			var toUri = new Uri(basePath.EndsWith(dirSepChar.ToString()) ? basePath : basePath + dirSepChar);
			if (fromUri.Scheme != toUri.Scheme) return basePath;

			var relativePath = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());
			return (toUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase)
				? relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
				: relativePath
			).TrimEnd(dirSepChar);
#endif
		}

		/// <returns><see langword="false"/> iff <paramref name="path"/> is blank, or is <c>"."</c> (relative path to CWD), regardless of trailing slash</returns>
		public static bool PathIsSet(this string path) => !string.IsNullOrWhiteSpace(path) && path != "." && path != "./" && path != ".\\";

		public static string RemoveInvalidFileSystemChars(this string name) => string.Concat(name.Split(Path.GetInvalidFileNameChars()));
	}

	public static class PathUtils
	{
		/// <returns>absolute path of the dll dir (sibling of EmuHawk.exe)</returns>
		/// <remarks>returned string omits trailing slash</remarks>
		public static readonly string DllDirectoryPath;

		/// <returns>absolute path of the parent dir of DiscoHawk.exe/EmuHawk.exe, commonly referred to as <c>%exe%</c> though none of our code adds it to the environment</returns>
		/// <remarks>returned string omits trailing slash</remarks>
		public static readonly string ExeDirectoryPath;

		static PathUtils()
		{
			var dirPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
			ExeDirectoryPath = OSTailoredCode.IsUnixHost
				? string.IsNullOrEmpty(dirPath) || dirPath == "/" ? string.Empty : dirPath
				: string.IsNullOrEmpty(dirPath) ? throw new Exception("failed to get location of executable, very bad things must have happened") : dirPath.RemoveSuffix('\\');
			DllDirectoryPath = Path.Combine(OSTailoredCode.IsUnixHost && ExeDirectoryPath == string.Empty ? "/" : ExeDirectoryPath, "dll");
			// yes, this is a lot of extra code to make sure BizHawk can run in `/` on Unix, but I've made up for it by caching these for the program lifecycle --yoshi
		}
	}
}
