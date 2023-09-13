using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Emulation.DiscSystem.CUE
{
	/// <summary>
	/// The CUE module user's hook for controlling how cue member file paths get resolved
	/// </summary>
	public class CueFileResolver
	{
		public bool caseSensitive = false;
		public bool IsHardcodedResolve { get; private set; }
		private string baseDir;

		/// <summary>
		/// Retrieving the FullName from a FileInfo can be slow (and probably other operations), so this will cache all the needed values
		/// TODO - could we treat it like an actual cache and only fill the FullName if it's null?
		/// </summary>
		private struct MyFileInfo
		{
			public string FullName;
			public FileInfo FileInfo;
		}

		private DirectoryInfo diBasedir;
		private MyFileInfo[] fisBaseDir;

		/// <summary>
		/// sets the base directory and caches the list of files in the directory
		/// </summary>
		public void SetBaseDirectory(string baseDir)
		{
			this.baseDir = baseDir;
			diBasedir = new DirectoryInfo(baseDir);
			//list all files, so we don't scan repeatedly.
			fisBaseDir = MyFileInfosFromFileInfos(diBasedir.GetFiles());
		}

		/// <summary>
		/// TODO - doesnt seem like we're using this...
		/// </summary>
		public void SetHardcodeResolve(IDictionary<string, string> hardcodes)
		{
			IsHardcodedResolve = true;
			fisBaseDir = new MyFileInfo[hardcodes.Count];
			var i = 0;
			foreach (var kvp in hardcodes)
			{
				fisBaseDir[i++] = new() { FullName = kvp.Key, FileInfo = new(kvp.Value) };
			}
		}

		private MyFileInfo[] MyFileInfosFromFileInfos(FileInfo[] fis)
		{
			var myfis = new MyFileInfo[fis.Length];
			for (var i = 0; i < fis.Length; i++)
			{
				myfis[i].FileInfo = fis[i];
				myfis[i].FullName = fis[i].FullName;
			}
			return myfis;
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
			var (targetFile, targetFragment, _) = path.SplitPathToDirFileAndExt();
			DirectoryInfo di = null;
			MyFileInfo[] fileInfos;
			if (!string.IsNullOrEmpty(Path.GetDirectoryName(path)))
			{
				di = new FileInfo(path).Directory;
				//fileInfos = di.GetFiles(Path.GetFileNameWithoutExtension(path)); //does this work?
				fileInfos = MyFileInfosFromFileInfos(di.GetFiles()); //we (probably) have to enumerate all the files to do a search anyway, so might as well do this
				//TODO - don't do the search until a resolve fails
			}
			else
			{
				di = diBasedir;
				fileInfos = fisBaseDir;
			}

			var results = new List<FileInfo>();
			foreach (var fi in fileInfos)
			{
				var ext = Path.GetExtension(fi.FullName).ToLowerInvariant();

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

				var fragment = Path.GetFileNameWithoutExtension(fi.FullName);
				//match files with differing extensions
				var cmp = string.Compare(fragment, targetFragment, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
				if (cmp != 0)
					//match files with another extension added on (likely to be mygame.bin.ecm)
					cmp = string.Compare(fragment, targetFile, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
				if (cmp == 0)
				{
					//take care to add an exact match at the beginning
					if (string.Equals(fi.FullName, Path.Combine(baseDir, path), StringComparison.OrdinalIgnoreCase))
						results.Insert(0, fi.FileInfo);
					else
						results.Add(fi.FileInfo);
				}
			}

			return results.Select(fi => fi.FullName).ToList();
		}
	}
}
