using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.MultiClient
{
	//todo:
	//split into "bind" and "open (the bound thing)"
	//scan archive to flatten interior directories down to a path (maintain our own archive item list)

	public class HawkFile : IDisposable
	{
		public static bool ExistsAt(string path)
		{
			using (var file = new HawkFile(path))
			{
				return file.Exists;
			}
		}

		public static byte[] ReadAllBytes(string path)
		{
			using (var file = new HawkFile(path))
			{
				if (!file.Exists) throw new FileNotFoundException(path);
				using (Stream stream = file.GetStream())
				{
					MemoryStream ms = new MemoryStream((int)stream.Length);
					stream.CopyTo(ms);
					return ms.GetBuffer();
				}
			}
		}


		/// <summary>
		/// returns whether a bound file exists. if there is no bound file, it can't exist
		/// </summary>
		public bool Exists { get { return exists; } }

		/// <summary>
		/// gets the directory containing the root
		/// </summary>
		public string Directory { get { return Path.GetDirectoryName(rootPath); } }

		/// <summary>
		/// returns a stream for the currently bound file
		/// </summary>
		public Stream GetStream()
		{
			if (boundStream == null)
				throw new InvalidOperationException("HawkFile: Can't call GetStream() before youve successfully bound something!");
			return boundStream;
		}

		/// <summary>
		/// indicates whether this instance is bound
		/// </summary>
		public bool IsBound { get { return boundStream != null; } }

		/// <summary>
		/// returns the complete canonical full path ("c:\path\to\archive|member") of the bound file
		/// </summary>
		public string CanonicalFullPath { get { return MakeCanonicalName(rootPath, memberPath); } }

		/// <summary>
		/// returns the complete canonical name ("archive|member") of the bound file
		/// </summary>
		public string CanonicalName { get { return MakeCanonicalName(Path.GetFileName(rootPath), memberPath); } }

		/// <summary>
		/// returns the virtual name of the bound file (disregarding the archive)
		/// </summary>
		public string Name { get { return GetBoundNameFromCanonical(MakeCanonicalName(rootPath, memberPath)); } }

		/// <summary>
		/// returns the extension of Name
		/// </summary>
		public string Extension { get { return Path.GetExtension(Name).ToUpper(); } }

		/// <summary>
		/// Indicates whether this file is an archive
		/// </summary>
		public bool IsArchive { get { return extractor != null; } }


		public class ArchiveItem
		{
			public string name;
			public long size;
			public int index;
		}

		public IEnumerable<ArchiveItem> ArchiveItems
		{
			get
			{
				if (!IsArchive) throw new InvalidOperationException("Cant get archive items from non-archive");
				return archiveItems;
			}
		}

		/// <summary>
		/// these extensions won't even be tried as archives (removes spurious archive detects since some of the signatures are pretty damn weak)
		/// </summary>
		public string[] NonArchiveExtensions = new string[] { };

		//---
		bool exists;
		bool rootExists;
		string rootPath;
		string memberPath;
		Stream rootStream, boundStream;
		SevenZip.SevenZipExtractor extractor;
		List<ArchiveItem> archiveItems;

		public HawkFile()
		{
		}

		public void Open(string path)
		{
			if (rootPath != null) throw new InvalidOperationException("Don't reopen a HawkFile.");

			string autobind = null;
			bool isArchivePath = IsCanonicalArchivePath(path);
			if (isArchivePath)
			{
				string[] parts = path.Split('|');
				path = parts[0];
				autobind = parts[1];
			}

			var fi = new FileInfo(path);

			rootExists = fi.Exists;
			if (fi.Exists == false)
				return;

			rootPath = path;
			exists = true;

			AnalyzeArchive(path);
			if (extractor == null)
			{
				rootStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				//we could autobind here, but i dont want to
				//bind it later with the desired extensions.
			}

			if (autobind == null)
			{
				//non-archive files can be automatically bound this way
				if (!isArchivePath)
					BindRoot();
			}
			else
			{
				autobind = autobind.ToUpperInvariant();
				for (int i = 0; i < extractor.ArchiveFileData.Count; i++)
				{
					if (FixArchiveFilename(extractor.ArchiveFileNames[i]).ToUpperInvariant() == autobind)
					{
						BindArchiveMember(i);
						return;
					}
				}

				exists = false;
			}
		}

		public HawkFile(string path)
		{
			Open(path);
		}

		/// <summary>
		/// is the supplied path a canonical name including an archive?
		/// </summary>
		bool IsCanonicalArchivePath(string path)
		{
			return (path.IndexOf('|') != -1);
		}

		/// <summary>
		/// converts a canonical name to a bound name (the bound part, whether or not it is an archive)
		/// </summary>
		string GetBoundNameFromCanonical(string canonical)
		{
			string[] parts = canonical.Split('|');
			return parts[parts.Length - 1];
		}

		/// <summary>
		/// makes a canonical name from two parts
		/// </summary>
		string MakeCanonicalName(string root, string member)
		{
			if (member == null) return root;
			else return string.Format("{0}|{1}", root, member);
		}

		string FixArchiveFilename(string fn)
		{
			return fn.Replace('\\', '/');
		}

		/// <summary>
		/// binds the specified ArchiveItem which you should have gotten by interrogating an archive hawkfile
		/// </summary>
		public HawkFile BindArchiveMember(ArchiveItem item)
		{
			return BindArchiveMember(item.index);
		}

		/// <summary>
		/// finds an ArchiveItem with the specified name (path) within the archive; returns null if it doesnt exist
		/// </summary>
		public ArchiveItem FindArchiveMember(string name)
		{
			return ArchiveItems.FirstOrDefault(ai => ai.name == name);
		}

		/// <summary>
		/// binds a path within the archive; returns null if that path didnt exist.
		/// </summary>
		public HawkFile BindArchiveMember(string name)
		{
			var ai = FindArchiveMember(name);
			if (ai == null) return null;
			else return BindArchiveMember(ai);
		}

		/// <summary>
		/// binds the selected archive index
		/// </summary>
		public HawkFile BindArchiveMember(int archiveIndex)
		{
			if (!rootExists) return this;
			if (boundStream != null) throw new InvalidOperationException("stream already bound!");

			boundStream = new MemoryStream();
			extractor.ExtractFile(archiveIndex, boundStream);
			boundStream.Position = 0;
			memberPath = FixArchiveFilename(extractor.ArchiveFileNames[archiveIndex]); //TODO - maybe go through our own list of names? maybe not, its indexes dont match..
			Console.WriteLine("HawkFile bound " + CanonicalFullPath);

			return this;
		}

		/// <summary>
		/// Removes any existing binding
		/// </summary>
		public void Unbind()
		{
			if (boundStream != null && boundStream != rootStream) boundStream.Close();
			boundStream = null;
			memberPath = null;
		}

		/// <summary>
		/// causes the root to be bound (in the case of non-archive files)
		/// </summary>
		void BindRoot()
		{
			boundStream = rootStream;
			Console.WriteLine("HawkFile bound " + CanonicalFullPath);
		}

		/// <summary>
		/// Binds the first item in the archive (or the file itself). Supposing that there is anything in the archive.
		/// </summary>
		public HawkFile BindFirst()
		{
			BindFirstOf();
			return this;
		}

		/// <summary>
		/// binds one of the supplied extensions if there is only one match in the archive
		/// </summary>
		public HawkFile BindSoleItemOf(params string[] extensions)
		{
			return BindByExtensionCore(false, extensions);
		}

		/// <summary>
		/// Binds the first item in the archive (or the file itself) if the extension matches one of the supplied templates.
		/// You probably should not use this. use BindSoleItemOf or the archive chooser instead
		/// </summary>
		public HawkFile BindFirstOf(params string[] extensions)
		{
			return BindByExtensionCore(true, extensions);
		}

		HawkFile BindByExtensionCore(bool first, params string[] extensions)
		{
			if (!rootExists) return this;
			if (boundStream != null) throw new InvalidOperationException("stream already bound!");

			if (extractor == null)
			{
				//open uncompressed file
				string extension = Path.GetExtension(rootPath).Substring(1).ToUpperInvariant();
				if (extensions.Length == 0 || extension.In(extensions))
				{
					BindRoot();
				}
				return this;
			}

			var candidates = new List<int>();
			for (int i = 0; i < extractor.ArchiveFileData.Count; i++)
			{
				var e = extractor.ArchiveFileData[i];
				if (e.IsDirectory) continue;
				var extension = Path.GetExtension(e.FileName).ToUpperInvariant();
				extension = extension.TrimStart('.');
				if (extensions.Length == 0 || extension.In(extensions))
				{
					if (first)
					{
						BindArchiveMember(i);
						return this;
					}
					candidates.Add(i);
				}
			}
			if (candidates.Count == 1)
				BindArchiveMember(candidates[0]);
			return this;
		}


		void ScanArchive()
		{
			archiveItems = new List<ArchiveItem>();
			for (int i = 0; i < extractor.ArchiveFileData.Count; i++)
			{
				var afd = extractor.ArchiveFileData[i];
				if (afd.IsDirectory) continue;
				var ai = new ArchiveItem {name = FixArchiveFilename(afd.FileName), size = (long) afd.Size, index = i};
				archiveItems.Add(ai);
			}
		}

		private void AnalyzeArchive(string path)
		{
			SevenZip.FileChecker.ThrowExceptions = false;
			int offset;
			bool isExecutable;
			if (NonArchiveExtensions.Any(ext => Path.GetExtension(path).Substring(1).ToLower() == ext.ToLower()))
			{
				return;
			}
			
			if (SevenZip.FileChecker.CheckSignature(path, out offset, out isExecutable) != SevenZip.InArchiveFormat.None)
			{
				extractor = new SevenZip.SevenZipExtractor(path);
				try
				{
					ScanArchive();
				}
				catch
				{
					extractor.Dispose();
					extractor = null;
					archiveItems = null;
				}
			}
		}

		public void Dispose()
		{
			Unbind();

			if (extractor != null) extractor.Dispose();
			if (rootStream != null) rootStream.Dispose();

			extractor = null;
			rootStream = null;
		}
	}
}
