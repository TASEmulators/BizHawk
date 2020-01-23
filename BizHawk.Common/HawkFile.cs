#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common.StringExtensions;

// the HawkFile class is excessively engineered with the IHawkFileArchiveHandler to decouple the archive handling from the basic file handling.
// This is so we could drop in an unamanged dearchiver library optionally later as a performance optimization without ruining the portability of the code.
// Also, we want to be able to use HawkFiles in BizHawk.Common withuot bringing in a large 7-zip dependency
namespace BizHawk.Common
{
	// TODO:
	// split into "bind" and "open (the bound thing)"
	// scan archive to flatten interior directories down to a path (maintain our own archive item list)

	/// <summary>
	/// Bridge between HawkFile and the frontend's implementation of archive management
	/// </summary>
	public interface IHawkFileArchiveHandler : IDisposable
	{
		// TODO - could this receive a hawkfile itself? possibly handy, in very clever scenarios of mounting fake files
		bool CheckSignature(string fileName, out int offset, out bool isExecutable);

		List<HawkFileArchiveItem> Scan();

		IHawkFileArchiveHandler Construct(string path);

		void ExtractFile(int index, Stream stream);
	}

	/// <summary>
	/// HawkFile allows a variety of objects (actual files, archive members) to be treated as normal filesystem objects to be opened, closed, and read.
	/// It can understand paths in 'canonical' format which includes /path/to/archive.zip|member.rom as well as /path/to/file.rom
	/// When opening an archive, it won't always be clear automatically which member should actually be used. 
	/// Therefore there is a concept of 'binding' where a HawkFile attaches itself to an archive member which is the file that it will actually be using.
	/// </summary>
	public sealed class HawkFile : IDisposable
	{
		private bool _exists;
		private bool _rootExists;
		private string _rootPath;
		private string _memberPath;
		private Stream _rootStream, _boundStream;
		private IHawkFileArchiveHandler _extractor;
		private bool _isArchive;
		private List<HawkFileArchiveItem> _archiveItems;
		private int? _boundIndex;

		public HawkFile() { }

		/// <summary>
		/// Set this with an instance which can construct archive handlers as necessary for archive handling.
		/// </summary>
		public static IHawkFileArchiveHandler ArchiveHandlerFactory { get; set; }

		/// <summary>
		/// Gets a value indicating whether a bound file exists. if there is no bound file, it can't exist.
		/// NOTE: this isn't set until the file is Opened. Not too great...
		/// </summary>
		public bool Exists => _exists;

		/// <summary>
		/// Gets the directory containing the root
		/// </summary>
		public string Directory => Path.GetDirectoryName(_rootPath);

		/// <summary>
		/// Gets a value indicating whether this instance is bound
		/// </summary>
		public bool IsBound => _boundStream != null;

		/// <summary>
		/// returns the complete canonical full path ("c:\path\to\archive|member") of the bound file
		/// </summary>
		public string CanonicalFullPath => MakeCanonicalName(_rootPath, _memberPath);

		/// <summary>
		/// returns the complete canonical name ("archive|member") of the bound file
		/// </summary>
		public string CanonicalName => MakeCanonicalName(Path.GetFileName(_rootPath), _memberPath);

		/// <summary>
		/// returns the virtual name of the bound file (disregarding the archive).
		/// Useful as a basic content identifier.
		/// </summary>
		public string Name => GetBoundNameFromCanonical(MakeCanonicalName(_rootPath, _memberPath));

		/// <summary>
		/// returns the complete full path of the bound file, excluding the archive member portion
		/// </summary>
		public string FullPathWithoutMember => _rootPath;

		/// <summary>
		/// returns the member path part of the bound file
		/// </summary>
		public string ArchiveMemberPath => _memberPath;

		/// <summary>
		/// returns the extension of Name
		/// </summary>
		public string Extension => Path.GetExtension(Name).ToUpper();

		/// <summary>
		/// Indicates whether this file is an archive
		/// </summary>
		public bool IsArchive => _isArchive;

		/// <summary>
		/// Indicates whether the file is an archive member (IsArchive && IsBound[to member])
		/// </summary>
		public bool IsArchiveMember => IsArchive && IsBound;

		/// <exception cref="InvalidOperationException"><see cref="IsArchive"/> is <see langword="false"/></exception>
		public IList<HawkFileArchiveItem> ArchiveItems => IsArchive
			? _archiveItems
			: throw new InvalidOperationException("Cant get archive items from non-archive");

		/// <returns>a stream for the currently bound file</returns>
		/// <exception cref="InvalidOperationException">no stream bound (haven't called <see cref="BindArchiveMember(int)"/> or overload)</exception>
		public Stream GetStream()
		{
			if (_boundStream == null)
			{
				throw new InvalidOperationException($"{nameof(HawkFile)}: Can't call {nameof(GetStream)}() before you've successfully bound something!");
			}

			return _boundStream;
		}

		public int? GetBoundIndex()
		{
			return _boundIndex;
		}

		/// <summary>
		/// Utility: Uses full HawkFile processing to determine whether a file exists at the provided path
		/// </summary>
		public static bool ExistsAt(string path)
		{
			using var file = new HawkFile(path);
			return file.Exists;
		}

		/// <summary>reads all the contents of the file at <paramref name="path"/></summary>
		/// <exception cref="FileNotFoundException">could not find <paramref name="path"/></exception>
		public static byte[] ReadAllBytes(string path)
		{
			using var file = new HawkFile(path);
			if (!file.Exists)
			{
				throw new FileNotFoundException(path);
			}

			using Stream stream = file.GetStream();
			using var ms = new MemoryStream((int)stream.Length);
			stream.CopyTo(ms);
			return ms.GetBuffer();
		}

		/// <summary>
		/// attempts to read all the content from the file
		/// </summary>
		public byte[] ReadAllBytes()
		{
			using (Stream stream = GetStream())
			{
				var ms = new MemoryStream((int)stream.Length);
				stream.CopyTo(ms);
				return ms.GetBuffer();
			}
		}

		/// <summary>
		/// these extensions won't even be tried as archives (removes spurious archive detects since some of the signatures are pretty damn weak)
		/// </summary>
		public string[] NonArchiveExtensions = { ".smc", ".sfc", ".dll" };

		/// <summary>
		/// Parses the given filename to create an un-opened HawkFile with some information available about its path constitution
		/// </summary>
		public void Parse(string path)
		{
			bool isArchivePath = IsCanonicalArchivePath(path);
			if (isArchivePath)
			{
				var parts = path.Split('|');
				path = parts[0];
				_memberPath = parts[1];

				// we're gonna assume, on parsing, that this is 
				_isArchive = true;
			}
			_rootPath = path;
		}

		/// <summary>Opens the file at <paramref name="path"/>. This may take a while if the file is an archive, as it may be accessed and scanned.</summary>
		/// <exception cref="InvalidOperationException">already opened via <see cref="HawkFile(string)"/>, this method, or <see cref="Parse"/></exception>
		public void Open(string path)
		{
			if (_rootPath != null)
			{
				throw new InvalidOperationException($"Don't reopen a {nameof(HawkFile)}.");
			}

			string autobind = null;
			bool isArchivePath = IsCanonicalArchivePath(path);
			if (isArchivePath)
			{
				var parts = path.Split('|');
				path = parts[0];
				autobind = parts[1];
			}

			var fi = new FileInfo(path);

			_rootExists = fi.Exists;
			if (fi.Exists == false)
			{
				return;
			}

			_rootPath = path;
			_exists = true;

			AnalyzeArchive(path);
			if (_extractor == null)
			{
				_rootStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				// we could autobind here, but i dont want to
				// bind it later with the desired extensions.
			}

			if (autobind == null)
			{
				// non-archive files can be automatically bound this way
				if (!isArchivePath)
				{
					BindRoot();
				}
			}
			else
			{
				autobind = autobind.ToUpperInvariant();
				if (_extractor != null)
				{
					var scanResults = _extractor.Scan();
					for (int i = 0; i < scanResults.Count; i++)
					{
						if (scanResults[i].Name.ToUpperInvariant() == autobind)
						{
							BindArchiveMember(i);
							return;
						}
					}
				}

				_exists = false;
			}
		}

		/// <summary>
		/// Makes a new HawkFile based on the provided path.
		/// </summary>
		public HawkFile(string path)
		{
			Open(path);
		}

		/// <summary>
		/// binds the specified ArchiveItem which you should have gotten by interrogating an archive hawkfile
		/// </summary>
		public HawkFile BindArchiveMember(HawkFileArchiveItem item)
		{
			return BindArchiveMember(item.Index);
		}

		/// <summary>
		/// finds an ArchiveItem with the specified name (path) within the archive; returns null if it doesnt exist
		/// </summary>
		public HawkFileArchiveItem FindArchiveMember(string name)
		{
			return ArchiveItems.FirstOrDefault(ai => ai.Name == name);
		}

		/// <summary>
		/// binds a path within the archive; returns null if that path didnt exist.
		/// </summary>
		public HawkFile BindArchiveMember(string name)
		{
			var ai = FindArchiveMember(name);
			if (ai == null)
			{
				return null;
			}

			return BindArchiveMember(ai);
		}

		/// <summary>binds the selected archive index</summary>
		/// <exception cref="InvalidOperationException">stream already bound</exception>
		public HawkFile BindArchiveMember(int index)
		{
			if (!_rootExists)
			{
				return this;
			}

			if (_boundStream != null)
			{
				throw new InvalidOperationException("stream already bound!");
			}

			_boundStream = new MemoryStream();
			int archiveIndex = _archiveItems[index].ArchiveIndex;
			_extractor.ExtractFile(archiveIndex, _boundStream);
			_boundStream.Position = 0;
			_memberPath = _archiveItems[index].Name; // TODO - maybe go through our own list of names? maybe not, its indexes dont match..
			Console.WriteLine($"{nameof(HawkFile)} bound {CanonicalFullPath}");
			_boundIndex = archiveIndex;
			return this;
		}

		/// <summary>
		/// Removes any existing binding
		/// </summary>
		public void Unbind()
		{
			if (_boundStream != null && _boundStream != _rootStream)
			{
				_boundStream.Close();
			}

			_boundStream = null;
			_memberPath = null;
			_boundIndex = null;
		}

		/// <summary>
		/// causes the root to be bound (in the case of non-archive files)
		/// </summary>
		private void BindRoot()
		{
			_boundStream = _rootStream;
			Console.WriteLine($"{nameof(HawkFile)} bound {CanonicalFullPath}");
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
		/// You probably should not use  use BindSoleItemOf or the archive chooser instead
		/// </summary>
		public HawkFile BindFirstOf(params string[] extensions)
		{
			return BindByExtensionCore(true, extensions);
		}

		/// <exception cref="InvalidOperationException">stream already bound</exception>
		private HawkFile BindByExtensionCore(bool first, params string[] extensions)
		{
			if (!_rootExists)
			{
				return this;
			}

			if (_boundStream != null)
			{
				throw new InvalidOperationException("stream already bound!");
			}

			if (_extractor == null)
			{
				// open uncompressed file
				var extension = Path.GetExtension(_rootPath).Substring(1).ToUpperInvariant();
				if (extensions.Length == 0 || extension.In(extensions))
				{
					BindRoot();
				}

				return this;
			}

			var candidates = new List<int>();
			for (int i = 0; i < _archiveItems.Count; i++)
			{
				var e = _archiveItems[i];
				var extension = Path.GetExtension(e.Name).ToUpperInvariant();
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
			{
				BindArchiveMember(candidates[0]);
			}

			return this;
		}

		private void ScanArchive()
		{
			_archiveItems = _extractor.Scan();
		}

		private void AnalyzeArchive(string path)
		{
			// no archive handler == no analysis
			if (ArchiveHandlerFactory == null)
			{
				return;
			}

			int offset;
			bool isExecutable;

			if (NonArchiveExtensions.Any(ext => Path.GetExtension(path).ToLower() == ext.ToLower()))
			{
				return;
			}

			if (ArchiveHandlerFactory.CheckSignature(path, out offset, out isExecutable))
			{
				_extractor = ArchiveHandlerFactory.Construct(path);
				try
				{
					ScanArchive();
					_isArchive = true;
				}
				catch
				{
					_extractor.Dispose();
					_extractor = null;
					_archiveItems = null;
				}
			}
		}

		public void Dispose()
		{
			Unbind();

			_extractor?.Dispose();
			_rootStream?.Dispose();

			_extractor = null;
			_rootStream = null;
		}

		/// <summary>
		/// is the supplied path a canonical name including an archive?
		/// </summary>
		private static bool IsCanonicalArchivePath(string path)
		{
			return path.IndexOf('|') != -1;
		}

		/// <summary>
		/// Repairs paths from an archive which contain offensive characters
		/// </summary>
		public static string Util_FixArchiveFilename(string fn)
		{
			return fn.Replace('\\', '/');
		}

		/// <summary>
		/// converts a canonical name to a bound name (the bound part, whether or not it is an archive)
		/// </summary>
		static string GetBoundNameFromCanonical(string canonical)
		{
			var parts = canonical.Split('|');
			return parts[parts.Length - 1];
		}

		/// <summary>
		/// makes a canonical name from two parts
		/// </summary>
		string MakeCanonicalName(string root, string member)
		{
			if (member == null)
			{
				return root;
			}

			return $"{root}|{member}";
		}
	} 

	/// <summary>
	/// Members returned by IHawkFileArchiveHandler
	/// </summary>
	public class HawkFileArchiveItem
	{
		/// <summary>
		/// Gets or sets the member name
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the size of member file
		/// </summary>
		public long Size { get; set; }

		/// <summary>
		/// Gets or sets the index of this archive item
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// Gets or sets the index WITHIN THE ARCHIVE (for internal tracking by a IHawkFileArchiveHandler) of the member
		/// </summary>
		public int ArchiveIndex { get; set; }
	}
}
 