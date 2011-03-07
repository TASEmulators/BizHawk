using System;
using System.IO;

namespace BizHawk.MultiClient
{
	//todo:
	//split into "bind" and "open (the bound thing)"
	//scan archive to flatten interior directories down to a path (maintain our own archive item list)

    public class HawkFile : IDisposable
    {
		/// <summary>
		/// returns whether a bound file exists. if there is no bound file, it can't exist
		/// </summary>
		public bool Exists { get { if (!rootExists) return false; return boundStream != null; } }

		/// <summary>
		/// returns whether the root exists (the actual physical file)
		/// </summary>
		public bool RootExists { get { return rootExists; } }

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
				throw new InvalidOperationException("HawkFil: Can't call GetStream() before youve successfully bound something!");
			return boundStream;
		}

		/// <summary>
		/// indicates whether this instance is bound
		/// </summary>
		public bool IsBound { get { return boundStream != null; } }

		/// <summary>
		/// returns the complete canonical name ("archive|member") of the bound file
		/// </summary>
		public string CanonicalName { get { return MakeCanonicalName(rootPath,memberPath); } }

		/// <summary>
		/// returns the virtual name of the bound file (disregarding the archive)
		/// </summary>
		public string Name { get { return GetBoundNameFromCanonical(MakeCanonicalName(rootPath,memberPath)); } }

		/// <summary>
		/// returns the extension of Name
		/// </summary>
		public string Extension { get { return Path.GetExtension(Name); } }

		//---
		bool rootExists;
		string rootPath;
		string memberPath;
		Stream rootStream, boundStream;
		SevenZip.SevenZipExtractor extractor;

		public static bool PathExists(string path)
		{
			using (var hf = new HawkFile(path))
				return hf.Exists;
		}
		
		public HawkFile(string path)
        {
			string autobind = null;
			if (IsCanonicalArchivePath(path))
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

			AnalyzeArchive(path);
			if (extractor == null)
			{
				rootStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			if (autobind != null)
			{
				autobind = autobind.ToUpperInvariant();
				for (int i = 0; i < extractor.ArchiveFileData.Count; i++)
				{
					if (extractor.ArchiveFileNames[i].ToUpperInvariant() == autobind)
					{
						BindArchiveMember(i);
						return;
					}
				}
			}
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

		void BindArchiveMember(int index)
		{
			boundStream = new MemoryStream();
			extractor.ExtractFile(index, boundStream);
			boundStream.Position = 0;
			memberPath = extractor.ArchiveFileNames[index];
			Console.WriteLine("bound " + CanonicalName);
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

		void BindRoot()
		{
			boundStream = rootStream;
			Console.WriteLine("bound " + CanonicalName);
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
		/// Binds the first item in the archive (or the file itself) if the extension matches one of the supplied templates
		/// </summary>
		public HawkFile BindFirstOf(params string[] extensions)
		{
			if (!rootExists) return this;
			if (boundStream != null) throw new InvalidOperationException("stream already bound!");

			if (extractor == null)
			{
				//open uncompressed file
				string extension = Path.GetExtension(rootPath).Substring(1).ToUpperInvariant();
				if (extensions.Length==0 || extension.In(extensions))
				{
					BindRoot();
				}
				return this;
			}

			for(int i=0;i<extractor.ArchiveFileData.Count;i++)
			{
				var e = extractor.ArchiveFileData[i];
				var extension = Path.GetExtension(e.FileName).Substring(1).ToUpperInvariant();
				if (extensions.Length == 0 || extension.In(extensions))
				{
					BindArchiveMember(i);
					return this;
				}
			}

			return this;
		}


        private void AnalyzeArchive(string path)
        {
			try
			{
				SevenZip.FileChecker.ThrowExceptions = false;
				int offset;
				bool isExecutable;
				if (SevenZip.FileChecker.CheckSignature(path, out offset, out isExecutable) != SevenZip.InArchiveFormat.None)
				{
					extractor = new SevenZip.SevenZipExtractor(path);
					//now would be a good time to scan the archive..
				}
			}
			catch
			{
				//must not be an archive. is there a better way to determine this? the exceptions are as annoying as hell
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