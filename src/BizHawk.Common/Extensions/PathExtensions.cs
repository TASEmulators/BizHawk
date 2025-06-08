using System.IO;

using BizHawk.Common.StringExtensions;

using Windows.Win32;

namespace BizHawk.Common.PathExtensions
{
	public static class PathExtensions
	{
		/// <returns><see langword="true"/> iff <paramref name="childPath"/> indicates a child of <paramref name="parentPath"/>, with <see langword="false"/> being returned if either path is <see langword="null"/></returns>
		/// <remarks>algorithm for Windows taken from https://stackoverflow.com/a/7710620/7467292</remarks>
		public static bool IsSubfolderOf(this string? childPath, string? parentPath)
		{
			if (childPath == null || parentPath == null) return false;
			if (childPath == parentPath || childPath.StartsWithOrdinal($"{parentPath}{Path.DirectorySeparatorChar}")) return true;

			if (OSTailoredCode.IsUnixHost)
			{
#if true
				var c = OSTailoredCode.SimpleSubshell("realpath", $"-Lm \"{childPath}\"", $"invalid path {childPath} or missing realpath binary");
				var p = OSTailoredCode.SimpleSubshell("realpath", $"-Lm \"{parentPath}\"", $"invalid path {parentPath} or missing realpath binary");
				return c == p || c.StartsWithOrdinal($"{p}/");
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

		/// <returns><see langword="true"/> iff absolute (OS-dependent)</returns>
		/// <seealso cref="IsRelative"/>
		public static bool IsAbsolute(this string path)
		{
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
			return Path.IsPathFullyQualified(path);
#else
			if (OSTailoredCode.IsUnixHost)
			{
				return path.StartsWith(Path.DirectorySeparatorChar);
			}
			else
			{
				var root = Path.GetPathRoot(path);
				return root.StartsWithOrdinal(@"\\") || root.EndsWith('\\') && root is not @"\";
			}
#endif
		}

		/// <returns><see langword="false"/> iff absolute (OS-dependent)</returns>
		/// <remarks>that means it may return <see langword="true"/> for invalid paths</remarks>
		/// <seealso cref="IsAbsolute"/>
		public static bool IsRelative(this string path) => !path.IsAbsolute();

		/// <exception cref="ArgumentException">running on Windows host, and unmanaged call failed</exception>
		/// <exception cref="FileNotFoundException">running on Windows host, and either path is not a regular file or directory</exception>
		/// <remarks>
		/// always returns a relative path, even if it means going up first<br/>
		/// algorithm for Windows taken from https://stackoverflow.com/a/485516/7467292<br/>
		/// the parameter names seem backwards, but those are the names used in the Win32 API we're calling
		/// </remarks>
		public static string? GetRelativePath(string? fromPath, string? toPath)
		{
			if (fromPath == null || toPath == null) return null;
			if (OSTailoredCode.IsUnixHost)
			{
				var realpathOutput = OSTailoredCode.SimpleSubshell("realpath", $"--relative-to=\"{fromPath}\" \"{toPath}\"", $"invalid path {toPath}, invalid path {fromPath}, or missing realpath binary");
				return !realpathOutput.StartsWithOrdinal("../") && realpathOutput != "." && realpathOutput != ".." ? $"./{realpathOutput}" : realpathOutput;
			}

			//TODO merge this with the Windows implementation in MakeRelativeTo
			static FileAttributes GetPathAttribute(string path1)
			{
				if (Directory.Exists(path1.SubstringBefore('|'))) return FileAttributes.Directory;
				if (File.Exists(path1.SubstringBefore('|'))) return FileAttributes.Normal;
				throw new FileNotFoundException();
			}
			var path = new char[Win32Imports.MAX_PATH];
			return Win32Imports.PathRelativePathToW(path, fromPath, GetPathAttribute(fromPath), toPath, GetPathAttribute(toPath))
				? new string(path).TrimEnd('\0')
				: throw new ArgumentException(message: "Paths must have a common prefix", paramName: nameof(toPath));
		}

		/// <returns>absolute path (OS-dependent) equivalent to <paramref name="path"/></returns>
		/// <remarks>
		/// unless <paramref name="cwd"/> is given, uses <see cref="CWDHacks.Get">CWDHacks.Get</see>/<see cref="Environment.CurrentDirectory">Environment.CurrentDirectory</see>,
		/// so take care when calling this after startup
		/// </remarks>
		public static string MakeAbsolute(this string path, string? cwd = null)
		{
			if (path.IsAbsolute())
				return path;
			else
			{
				// FileInfo for normalisation ("C:\a\b\..\c" => "C:\a\c")
				var mycwd = cwd ?? (OSTailoredCode.IsUnixHost ? Environment.CurrentDirectory : CWDHacks.Get());
				var finalpath = $"{mycwd}/{path}";
				var fi = new FileInfo(finalpath);
				return fi.FullName;
			}
		}

		/// <returns>the absolute path equivalent to <paramref name="path"/> which contains <c>%exe%</c> (expanded) as a prefix</returns>
		/// <remarks>
		/// returned string omits trailing slash<br/>
		/// note that the returned string is an absolute path and not a relative path; but TODO it was intended to be relative
		/// </remarks>
		public static string MakeProgramRelativePath(this string path) => Path.Combine(PathUtils.ExeDirectoryPath, path);

		/// <returns>the relative path which is equivalent to <paramref name="absolutePath"/> when the CWD is <paramref name="basePath"/>, or <see langword="null"/> if either path is <see langword="null"/></returns>
		/// <remarks>
		/// only returns a relative path if <paramref name="absolutePath"/> is a child of <paramref name="basePath"/> (uses <see cref="IsSubfolderOf"/>), otherwise returns <paramref name="absolutePath"/><br/>
		/// returned string omits trailing slash
		/// </remarks>
		public static string? MakeRelativeTo(this string? absolutePath, string? basePath)
		{
			if (absolutePath == null || basePath == null) return null;
			if (!absolutePath.IsSubfolderOf(basePath)) return absolutePath;
			if (!OSTailoredCode.IsUnixHost) return absolutePath.Replace(basePath, ".").RemoveSuffix(Path.DirectorySeparatorChar);
#if true // Unix implementation using realpath
			var realpathOutput = OSTailoredCode.SimpleSubshell("realpath", $"--relative-base=\"{basePath}\" \"{absolutePath}\"", $"invalid path {absolutePath}, invalid path {basePath}, or missing realpath binary");
			return !realpathOutput.StartsWithOrdinal("../") && realpathOutput != "." && realpathOutput != ".." ? $"./{realpathOutput}" : realpathOutput;
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
			return (Uri.UriSchemeFile.EqualsIgnoreCase(toUri.Scheme)
				? relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
				: relativePath
			).TrimEnd(dirSepChar);
#endif
		}

		/// <returns><see langword="false"/> iff <paramref name="path"/> is blank, or is <c>"."</c> (relative path to CWD), regardless of trailing slash</returns>
		public static bool PathIsSet(this string path) => !string.IsNullOrWhiteSpace(path) && path != "." && path != "./" && path != ".\\";

		public static string RemoveInvalidFileSystemChars(this string name) => string.Concat(name.Split(Path.GetInvalidFileNameChars()));

		public static (string? Dir, string File) SplitPathToDirAndFile(this string path)
			=> (Path.GetDirectoryName(path), Path.GetFileName(path));

		public static (string? Dir, string FileNoExt, string? FileExt) SplitPathToDirFileAndExt(this string path)
			=> (
				string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path),
				Path.GetFileNameWithoutExtension(path),
				Path.GetExtension(path) is { Length: not 0 } ext
					? ext
					: null);
	}

	public static class PathUtils
	{
		/// <returns>absolute path of the user data dir <c>$BIZHAWK_DATA_HOME</c>, or fallback value equal to <see cref="ExeDirectoryPath"/></returns>
		/// <remarks>
		/// returned string omits trailing slash<br/>
		/// on Windows, the env. var is ignored and the fallback of <see cref="ExeDirectoryPath"/> is always used
		/// </remarks>
		public static readonly string DataDirectoryPath;

		/// <returns>absolute path of the dll dir (sibling of EmuHawk.exe)</returns>
		/// <remarks>returned string omits trailing slash</remarks>
		public static readonly string DllDirectoryPath;

		/// <returns>absolute path of the parent dir of DiscoHawk.exe/EmuHawk.exe, commonly referred to as <c>%exe%</c> though none of our code adds it to the environment</returns>
		/// <remarks>returned string omits trailing slash</remarks>
		public static readonly string ExeDirectoryPath;

		public static string SpecialRecentsDir
			=> Environment.GetFolderPath(Environment.SpecialFolder.Recent, Environment.SpecialFolderOption.DoNotVerify);

		static PathUtils()
		{
			static string? ReadPathFromEnvVar(string envVarName)
			{
				var envVar = Environment.GetEnvironmentVariable(envVarName);
				try
				{
					envVar = envVar?.MakeAbsolute() ?? string.Empty;
					if (Directory.Exists(envVar)) return envVar;
				}
				catch
				{
					// ignored
				}
				return null;
			}
			if (OSTailoredCode.IsUnixHost)
			{
				var dirPath = ReadPathFromEnvVar("BIZHAWK_HOME") ?? AppContext.BaseDirectory;
				ExeDirectoryPath = string.IsNullOrEmpty(dirPath) || dirPath == "/" ? string.Empty : dirPath;
				DllDirectoryPath = Path.Combine(ExeDirectoryPath.Length is 0 ? "/" : ExeDirectoryPath, "dll");
				// yes, this is a lot of extra code to make sure BizHawk can run in `/` on Unix, but I've made up for it by caching these for the program lifecycle --yoshi
				DataDirectoryPath = ReadPathFromEnvVar("BIZHAWK_DATA_HOME") ?? ExeDirectoryPath;
			}
			else
			{
				var dirPath = AppContext.BaseDirectory;
				DataDirectoryPath = ExeDirectoryPath = string.IsNullOrEmpty(dirPath)
					? throw new Exception("failed to get location of executable, very bad things must have happened")
					: dirPath.RemoveSuffix('\\');
				DllDirectoryPath = Path.Combine(ExeDirectoryPath, "dll");
			}
		}
	}
}
