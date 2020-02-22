using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Common
{
	/// <summary>
	/// This class can represent a variety of file-like objects—"regular" files on disk, archive members, SMB shares(?)—encapsulating them so any may be opened/read/closed like files on disk.<br/>
	/// When opening an archive, it won't always be clear automatically which member should actually be used.
	/// Therefore, we define the concept of "binding": the <see cref="HawkFile"/> attaches itself to an archive member, which is the file that it will actually be using.<br/>
	/// We also define a simple extension to the Unix path format using <c>'|'</c>: <c>/path/to/file.rom</c> is readable, but so is <c>/path/to/archive.zip|member.rom</c>.
	/// Strings formatted this way are annotated <see cref="HawkFilePathAttribute">[HawkFilePath]</see>.
	/// </summary>
	/// <remarks>
	/// This class is defensively designed around <see cref="IFileDearchivalMethod"/> to allow swapping out implementations (for speed) without adding any dependencies to this project.<br/>
	/// TODO split into "bind" and "open &lt;the bound thing>"<br/>
	/// TODO scan archive to flatten interior directories down to a path (maintain our own archive item list)
	/// </remarks>
	public sealed class HawkFile : IDisposable
	{
		private List<HawkArchiveFileItem>? _archiveItems;

		private Stream? _boundStream;

		private IHawkArchiveFile? _extractor;

		private bool _rootExists;

		private Stream? _rootStream;

		/// <summary>These file extensions are assumed to not be archives (with default value, mitigates high false positive rate caused by weak archive detection signatures).</summary>
		public IReadOnlyCollection<string> NonArchiveExtensions = CommonNonArchiveExtensions;

		/// <exception cref="InvalidOperationException"><see cref="IsArchive"/> is <see langword="false"/></exception>
		public IList<HawkArchiveFileItem> ArchiveItems => (IsArchive ? _archiveItems : null) ?? throw new InvalidOperationException("Can't get archive items from non-archive");

		/// <value>the member path part of the bound file</value>
		public string? ArchiveMemberPath { get; private set; }

		public int? BoundIndex { get; private set; }

		/// <summary>returns the complete canonical full path ("c:\path\to\archive|member") of the bound file</summary>
		[HawkFilePath]
		public string? CanonicalFullPath => MakeCanonicalName(SafeFullPathWithoutMember, ArchiveMemberPath);

		/// <summary>returns the complete canonical name ("archive|member") of the bound file</summary>
		[HawkFilePath]
		public string? CanonicalName => MakeCanonicalName(Path.GetFileName(SafeFullPathWithoutMember), ArchiveMemberPath);

		/// <summary>Gets the directory containing the root</summary>
		public string? Directory => Path.GetDirectoryName(SafeFullPathWithoutMember);

		/// <value><see cref="true"/> iff a file is bound and the bound file exists</value>
		/// <remarks>NOTE: this isn't set until the file is <see cref="Open">Opened</see>. Not too great...</remarks>
		public bool Exists { get; private set; }

		/// <summary>returns the extension of Name</summary>
		public string? Extension => Path.GetExtension(Name).ToUpperInvariant();

		/// <value>returns the complete full path of the bound file, excluding the archive member portion</value>
		/// <remarks>assigned in <see cref="Open"/> and <see cref="Parse"/>, but if neither is called may be <see langword="null"/> and cause NREs</remarks>
		public string? FullPathWithoutMember { get; private set; }

		public bool IsArchive { get; private set; }

		/// <summary>Indicates whether the file is an archive member (IsArchive && IsBound[to member])</summary>
		public bool IsArchiveMember => IsArchive && IsBound;

		/// <summary>Gets a value indicating whether this instance is bound</summary>
		public bool IsBound => _boundStream != null;

		/// <summary>returns the virtual name of the bound file (disregarding the archive). Useful as a basic content identifier.</summary>
		public string Name => ArchiveMemberPath ?? SafeFullPathWithoutMember;

		private string SafeFullPathWithoutMember => FullPathWithoutMember ?? throw new NullReferenceException($"this is related to the deprecated no-arg ctor, {nameof(FullPathWithoutMember)} is only assigned in {nameof(Open)}/{nameof(Parse)}");

		[Obsolete]
		public HawkFile() {}

		/// <summary>Makes a new HawkFile based on the provided path.</summary>
		/// <remarks>If <paramref name="delayIOAndDearchive"/> is <see langword="true"/>, <see cref="Parse"/> will be called instead of <see cref="Open"/>.</remarks>
		public HawkFile([HawkFilePath] string path, bool delayIOAndDearchive = false)
		{
			if (delayIOAndDearchive) Parse(path);
			else Open(path);
		}

		/// <summary>binds the specified ArchiveItem which you should have gotten by interrogating an archive hawkfile</summary>
		public HawkFile? BindArchiveMember(HawkArchiveFileItem item) => BindArchiveMember(item.Index);

		/// <summary>binds the selected archive index</summary>
		/// <exception cref="InvalidOperationException">stream already bound</exception>
		public HawkFile? BindArchiveMember(int index)
		{
			if (!_rootExists) return this;
			if (_boundStream != null) throw new InvalidOperationException("stream already bound!");
			if (_archiveItems == null || _extractor == null) throw new InvalidOperationException("not an archive");

			var archiveIndex = _archiveItems[index].ArchiveIndex;
			_boundStream = new MemoryStream();
			_extractor.ExtractFile(archiveIndex, _boundStream);
			_boundStream.Position = 0;
			ArchiveMemberPath = _archiveItems[index].Name; // TODO - maybe go through our own list of names? maybe not, its indices don't match...
#if DEBUG
			Console.WriteLine($"{nameof(HawkFile)} bound {CanonicalFullPath}");
#endif
			BoundIndex = archiveIndex;
			return this;
		}

		/// <summary>binds a path within the archive; returns null if that path didnt exist.</summary>
		public HawkFile? BindArchiveMember(string? name)
		{
			var ai = FindArchiveMember(name);
			return ai == null ? null : BindArchiveMember(ai.Value);
		}

		/// <exception cref="InvalidOperationException">stream already bound</exception>
		private HawkFile? BindByExtensionCore(bool first, params string[] extensions)
		{
			if (!_rootExists) return this;
			if (_boundStream != null) throw new InvalidOperationException("stream already bound!");

			if (_archiveItems == null || _extractor == null)
			{
				// open uncompressed file
				if (extensions.Length == 0
					|| Path.GetExtension(SafeFullPathWithoutMember).Substring(1).In(extensions))
				{
					BindRoot();
				}
			}
			else
			{
				if (extensions.Length != 0)
				{
					var candidates = _archiveItems.Where(item => Path.GetExtension(item.Name).Substring(1).In(extensions)).ToList();
					if (candidates.Count != 0 && first || candidates.Count == 1) BindArchiveMember(candidates[0].Index);
				}
				else if (first || _archiveItems.Count == 1)
				{
					BindArchiveMember(0);
				}
			}

			return this;
		}

		/// <summary>Binds the first item in the archive (or the file itself), assuming that there is anything in the archive.</summary>
		public HawkFile? BindFirst() => BindFirstOf();

		/// <summary>Binds the first item in the archive (or the file itself) if the extension matches one of the supplied templates.</summary>
		/// <remarks>You probably should use <see cref="BindSoleItemOf"/> or the archive chooser instead.</remarks>
		public HawkFile? BindFirstOf(params string[] extensions) => BindByExtensionCore(true, extensions);

		/// <summary>causes the root to be bound (in the case of non-archive files)</summary>
		private void BindRoot()
		{
			_boundStream = _rootStream;
#if DEBUG
			Console.WriteLine($"{nameof(HawkFile)} bound {CanonicalFullPath}");
#endif
		}

		/// <summary>binds one of the supplied extensions if there is only one match in the archive</summary>
		public HawkFile? BindSoleItemOf(params string[] extensions) => BindByExtensionCore(false, extensions);

		public void Dispose()
		{
			Unbind();
			_extractor?.Dispose();
			_extractor = null;
			_rootStream?.Dispose();
			_rootStream = null;
		}

		/// <summary>finds an ArchiveItem with the specified name (path) within the archive; returns null if it doesnt exist</summary>
		public HawkArchiveFileItem? FindArchiveMember(string? name) => ArchiveItems.FirstOrDefault(ai => ai.Name == name);

		/// <returns>a stream for the currently bound file</returns>
		/// <exception cref="InvalidOperationException">no stream bound (haven't called <see cref="BindArchiveMember(int)"/> or overload)</exception>
		public Stream GetStream() => _boundStream ?? throw new InvalidOperationException($"{nameof(HawkFile)}: Can't call {nameof(GetStream)}() before you've successfully bound something!");

		/// <summary>Opens the file at <paramref name="path"/>. This may take a while if the file is an archive, as it may be accessed and scanned.</summary>
		/// <exception cref="InvalidOperationException">already opened via <see cref="HawkFile(string)"/>, this method, or <see cref="Parse"/></exception>
		public void Open([HawkFilePath] string path)
		{
			if (FullPathWithoutMember != null) throw new InvalidOperationException($"Don't reopen a {nameof(HawkFile)}.");

			string? autobind = null;
			var split = SplitArchiveMemberPath(path);
			if (split != null) (path, autobind) = split.Value;
			_rootExists = new FileInfo(path).Exists;
			if (!_rootExists) return;
			FullPathWithoutMember = path;
			Exists = true;

			if (DearchivalMethod != null
				&& !NonArchiveExtensions.Contains(Path.GetExtension(path).ToLowerInvariant())
				&& DearchivalMethod.CheckSignature(path, out _, out _))
			{
				_extractor = DearchivalMethod.Construct(path);
				try
				{
					_archiveItems = _extractor.Scan();
					IsArchive = true;
				}
				catch
				{
					_archiveItems = null;
					_extractor.Dispose();
					_extractor = null;
				}
			}
			if (_extractor == null)
			{
				_rootStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				// we could autobind here, but i dont want to
				// bind it later with the desired extensions.
			}

			if (autobind == null)
			{
				// non-archive files can be automatically bound this way
				BindRoot();
			}
			else
			{
				if (_extractor != null)
				{
					var scanResults = _extractor.Scan();
					for (int i = 0, l = scanResults.Count; i < l; i++)
					{
						if (string.Equals(scanResults[i].Name, autobind, StringComparison.InvariantCultureIgnoreCase))
						{
							BindArchiveMember(i);
							return;
						}
					}
				}

				Exists = false;
			}
		}

		/// <returns>an unopened <see cref="HawkFile"/> with only some fields populated, specifically those where the value is in <paramref name="path"/></returns>
		public void Parse([HawkFilePath] string path)
		{
			var split = SplitArchiveMemberPath(path);
			if (split != null)
			{
				(path, ArchiveMemberPath) = split.Value;
				IsArchive = true; // we'll assume that the '|' is only used for archives
			}
			FullPathWithoutMember = path;
		}

		/// <summary>attempts to read all the content from the file</summary>
		public byte[] ReadAllBytes()
		{
			using var stream = GetStream();
			using var ms = new MemoryStream((int) stream.Length);
			stream.CopyTo(ms);
			return ms.GetBuffer();
		}

		/// <summary>Removes any existing binding</summary>
		public void Unbind()
		{
			if (_boundStream != _rootStream) _boundStream?.Close();
			_boundStream = null;
			ArchiveMemberPath = null;
			BoundIndex = null;
		}

		/// <summary>Set this with an instance which can construct archive handlers as necessary for archive handling.</summary>
		public static IFileDearchivalMethod<IHawkArchiveFile>? DearchivalMethod;

		private static readonly IReadOnlyCollection<string> CommonNonArchiveExtensions = new[] { ".smc", ".sfc", ".dll" };

		/// <summary>Utility: Uses full HawkFile processing to determine whether a file exists at the provided path</summary>
		public static bool ExistsAt(string path)
		{
			using var file = new HawkFile(path);
			return file.Exists;
		}

		[return: HawkFilePath]
		private static string MakeCanonicalName(string root, string? member) => member == null ? root : $"{root}|{member}";

		/// <summary>reads all the contents of the file at <paramref name="path"/></summary>
		/// <exception cref="FileNotFoundException">could not find <paramref name="path"/></exception>
		public static byte[] ReadAllBytes(string path)
		{
			using var file = new HawkFile(path);
			return file.Exists ? file.ReadAllBytes() : throw new FileNotFoundException(path);
		}

		/// <returns>path / member path pair iff <paramref name="path"/> contains <c>'|'</c>, <see langword="null"/> otherwise</returns>
		private static (string, string)? SplitArchiveMemberPath([HawkFilePath] string path)
		{
			var i = path.LastIndexOf('|');
#if DEBUG
			if (path.IndexOf('|') != i) Console.WriteLine($"{nameof(HawkFile)} path contains multiple '|'");
#endif
			return i == -1 ? ((string, string)?) null : (path.Substring(0, i), path.Substring(i + 1));
		}
	}
}
