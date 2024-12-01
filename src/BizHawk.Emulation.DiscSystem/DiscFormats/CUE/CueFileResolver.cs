using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.DiscSystem.CUE
{
	/// <summary>
	/// The CUE module user's hook for controlling how cue member file paths get resolved
	/// </summary>
	public class CueFileResolver
	{
		public bool caseSensitive = false;

		private string _baseDir;
		private string[] _baseDirPaths;

		/// <summary>
		/// sets the base directory and caches the list of files in the directory
		/// </summary>
		public void SetBaseDirectory(string baseDir)
		{
			this._baseDir = baseDir;
			//list all files, so we don't scan repeatedly.
			_baseDirPaths = Directory.GetFiles(baseDir).Select(Path.GetFullPath).ToArray();
		}

		/// <summary>
		/// Performs cue-intelligent logic to acquire a file requested by the cue.
		/// Returns the resulting full path(s).
		/// If there are multiple options, it returns them all.
		/// Returns the requested path first in the list (if it was found) for more simple use.
		/// Kind of an unusual design, I know. Consider them sorted by confidence.
		/// </summary>
		public List<string> Resolve(string path)
		{
			string targetFile = Path.GetFileName(path);
			string targetFragment = Path.GetFileNameWithoutExtension(path);

			var directory = Path.GetDirectoryName(path);
			var filePaths = Directory.Exists(directory) ? Directory.GetFiles(directory).Select(Path.GetFullPath) : _baseDirPaths;
			//TODO - don't do the search until a resolve fails // leftover comment from 3c26d48a59f64a7a94bda57fbcfd13eca49d8b9d, is this still relevant?

			var results = new List<string>();
			foreach (var filePath in filePaths)
			{
				var ext = Path.GetExtension(filePath).ToLowerInvariant();

				//some choices are always bad: (we're looking for things like .bin and .wav)
				//it's a little unclear whether we should go for a whitelist or a blacklist here.
				//there's similar numbers of cases either way.
				//perhaps we could code both (and prefer choices from the whitelist)
				if (ext is not ".iso" && (Disc.IsValidExtension(ext) || ext is ".sbi" or ".sub"))
					continue;

				//continuing the bad plan: forbid archives (always a wrong choice, not supported anyway)
				//we should have a list prioritized by extension and score that way
				if (ext is ".7z" or ".rar" or ".zip" or ".bz2" or ".gz")
					continue;

				var fragment = Path.GetFileNameWithoutExtension(filePath);
				//match files with differing extensions
				var cmp = string.Compare(fragment, targetFragment, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
				if (cmp != 0)
					//match files with another extension added on (likely to be mygame.bin.ecm)
					cmp = string.Compare(fragment, targetFile, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
				if (cmp == 0)
				{
					//take care to add an exact match at the beginning
					if (string.Equals(filePath, Path.Combine(_baseDir, path), StringComparison.OrdinalIgnoreCase))
						results.Insert(0, filePath);
					else
						results.Add(filePath);
				}
			}

			return results;
		}
	}
}
