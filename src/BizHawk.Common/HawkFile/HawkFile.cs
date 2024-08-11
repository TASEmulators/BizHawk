using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using BizHawk.Common.CollectionExtensions;
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
	/// This class is defensively designed around <see cref="IFileDearchivalMethod{T}"/> to allow swapping out implementations (for speed) without adding any dependencies to this project.<br/>
	/// TODO split into "bind" and "open &lt;the bound thing>"<br/>
	/// TODO scan archive to flatten interior directories down to a path (maintain our own archive item list)
	/// </remarks>
	public sealed class HawkFile : IDisposable
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool PathContainsPipe(string s)
			=> s.ContainsOrdinal('|');

		private readonly List<HawkArchiveFileItem>? _archiveItems;

		private Stream? _boundStream;

		private IHawkArchiveFile? _extractor;

		private readonly bool _rootExists;

		private Stream? _rootStream;

		/// <exception cref="InvalidOperationException"><see cref="IsArchive"/> is <see langword="false"/></exception>
		public IList<HawkArchiveFileItem> ArchiveItems => (IsArchive ? _archiveItems : null) ?? throw new InvalidOperationException("Can't get archive items from non-archive");

		/// <value>the member path part of the bound file</value>
		public string? ArchiveMemberPath { get; private set; }

		public int? BoundIndex { get; private set; }

		/// <summary>returns the complete canonical full path ("c:\path\to\archive|member") of the bound file</summary>
		[HawkFilePath]
		public string CanonicalFullPath => MakeCanonicalName(FullPathWithoutMember, ArchiveMemberPath);

		/// <summary>returns the complete canonical name ("archive|member") of the bound file</summary>
		[HawkFilePath]
		public string CanonicalName => MakeCanonicalName(Path.GetFileName(FullPathWithoutMember), ArchiveMemberPath);

		/// <summary>Gets the directory containing the root</summary>
		public string? Directory => Path.GetDirectoryName(FullPathWithoutMember);

		/// <value>true if a file is bound and the bound file exists</value>
		public readonly bool Exists;

		/// <value>the file extension (of <see cref="Name"/>); including the leading period and in lowercase</value>
		public string Extension => Path.GetExtension(Name).ToLowerInvariant();

		/// <value>returns the complete full path of the bound file, excluding the archive member portion</value>
		public readonly string FullPathWithoutMember;

		public readonly bool IsArchive;

		/// <summary>Indicates whether the file is an archive member (IsArchive &amp;&amp; IsBound[to member])</summary>
		public bool IsArchiveMember => IsArchive && IsBound;

		/// <summary>Gets a value indicating whether this instance is bound</summary>
		public bool IsBound => _boundStream != null;

		/// <summary>returns the virtual name of the bound file (disregarding the archive). Useful as a basic content identifier.</summary>
		public string Name => ArchiveMemberPath ?? FullPathWithoutMember;

		/// <summary>Makes a new HawkFile based on the provided path.</summary>
		/// <param name="delayIOAndDearchive">Pass <see langword="true"/> to only populate a few fields (those that can be computed from the string <paramref name="path"/>), which is less computationally expensive.</param>
		public HawkFile([HawkFilePath] string path, bool delayIOAndDearchive = false, bool allowArchives = true)
		{
			if (delayIOAndDearchive)
			{
				var split = SplitArchiveMemberPath(path);
				if (split != null)
				{
					(path, ArchiveMemberPath) = split.Value;
					IsArchive = true; // we'll assume that the '|' is only used for archives
				}
				FullPathWithoutMember = path;
				return;
			}

			string? autobind = null;
			var split1 = SplitArchiveMemberPath(path);
			if (split1 != null) (path, autobind) = split1.Value;
			FullPathWithoutMember = path;
			Exists = _rootExists = File.Exists(path);
			if (!_rootExists) return;

			if (DearchivalMethod != null && allowArchives)
			{
				var ext = Path.GetExtension(path).ToLowerInvariant();
				if (DearchivalMethod.AllowedArchiveExtensions.Contains(ext))
				{
					if (DearchivalMethod.CheckSignature(path, out _, out _))
					{
						_extractor = DearchivalMethod.Construct(path);
						try
						{
							_archiveItems = _extractor.Scan()!;
							IsArchive = true;
						}
						catch
						{
							Console.WriteLine($"Failed to scan file list of {FullPathWithoutMember}");
							_archiveItems = null;
							_extractor.Dispose();
							_extractor = null;
						}
					}
				}
			}

			if (_extractor == null)
			{
				_rootStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
				// we could autobind here, but i don't want to
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
					if (scanResults == null)
					{
						Console.WriteLine($"Failed to scan file list of {FullPathWithoutMember}");
						Exists = false;
						return;
					}
					for (int i = 0, l = scanResults.Count; i < l; i++)
					{
						if (string.Equals(scanResults[i].Name, autobind, StringComparison.OrdinalIgnoreCase))
						{
							BindArchiveMember(i);
							return;
						}
					}
				}

				Exists = false;
			}
		}

		/// <summary>binds the specified ArchiveItem which you should have gotten by interrogating an archive hawkfile</summary>
		public HawkFile BindArchiveMember(HawkArchiveFileItem item) => BindArchiveMember(item.Index);

		/// <summary>binds the selected archive index</summary>
		/// <exception cref="InvalidOperationException">stream already bound</exception>
		public HawkFile BindArchiveMember(int index)
		{
			if (!_rootExists) return this;
			if (_boundStream != null) throw new InvalidOperationException("stream already bound!");
			if (_archiveItems == null || _extractor == null) throw new InvalidOperationException("not an archive");

			var archiveIndex = _archiveItems[index].ArchiveIndex;
			_boundStream = new MemoryStream();
			_extractor.ExtractFile(archiveIndex, _boundStream);
			_boundStream.Position = 0;
			ArchiveMemberPath = _archiveItems[index].Name; // TODO - maybe go through our own list of names? maybe not, its indices don't match...
			Util.DebugWriteLine($"{nameof(HawkFile)} bound {CanonicalFullPath}");
			if (_boundStream.Length is 0) Console.WriteLine("bound file is 0 bytes long?");
			BoundIndex = index;
			return this;
		}

		/// <summary>binds a path within the archive; returns null if that path didnt exist.</summary>
		public HawkFile? BindArchiveMember(string? name)
		{
			var ai = FindArchiveMember(name);
			return ai == null ? null : BindArchiveMember(ai.Value);
		}

		/// <param name="extensions">File extensions; include the leading period in each, and use lowercase.</param>
		/// <exception cref="InvalidOperationException">stream already bound</exception>
		private HawkFile BindByExtensionCore(IReadOnlyCollection<string> extensions, bool onlyBindSingle = false)
		{
			if (!_rootExists) return this;
			if (_boundStream != null) throw new InvalidOperationException("stream already bound!");

			if (!(_archiveItems == null || _extractor == null))
			{
				if (extensions.Count != 0)
				{
					var candidates = _archiveItems.Where(item => extensions.Contains(Path.GetExtension(item.Name).ToLowerInvariant())).ToList();
					if (onlyBindSingle ? candidates.Count == 1 : candidates.Count != 0) BindArchiveMember(candidates[0].Index);
					return this;
				}
				else if (!onlyBindSingle || _archiveItems.Count == 1)
				{
					BindArchiveMember(0);
					return this;
				}
				else
				{
					return this;
				}
			}

			// open uncompressed file
			if (extensions.Count == 0
				|| extensions.Contains(Path.GetExtension(FullPathWithoutMember).ToLowerInvariant()))
			{
				BindRoot();
			}

			return this;
		}

		/// <summary>Binds the first archive member if one exists, or for non-archives, binds the file.</summary>
		public HawkFile BindFirst() => BindByExtensionCore(Array.Empty<string>());

		/// <summary>
		/// Binds the first archive member whose file extension is in <paramref name="extensions"/> if one exists,
		/// or for non-archives, binds the file if its file extension is in <paramref name="extensions"/>.
		/// </summary>
		/// <param name="extensions">File extensions; include the leading period in each, and use lowercase.</param>
		/// <remarks>You probably should use <see cref="BindSoleItemOf"/> or the archive chooser instead.</remarks>
		public HawkFile BindFirstOf(IReadOnlyCollection<string> extensions) => BindByExtensionCore(extensions);

		/// <summary>
		/// Binds the first archive member whose file extension is <paramref name="extension"/> if one exists,
		/// or for non-archives, binds the file if its file extension is <paramref name="extension"/>.
		/// </summary>
		/// <param name="extension">File extension; include the leading period, and use lowercase.</param>
		/// <remarks>You probably should use <see cref="BindSoleItemOf"/> or the archive chooser instead.</remarks>
		public HawkFile BindFirstOf(string extension) => BindByExtensionCore(new[] { extension });

		/// <summary>causes the root to be bound (in the case of non-archive files)</summary>
		private void BindRoot()
		{
			_boundStream = _rootStream;
			Util.DebugWriteLine($"{nameof(HawkFile)} bound {CanonicalFullPath}");
		}

		/// <summary>As <see cref="BindFirstOf(IReadOnlyCollection{string})"/>, but doesn't bind anything if there are multiple archive members with a matching file extension.</summary>
		/// <param name="extensions">File extensions; include the leading period in each, and use lowercase.</param>
		public HawkFile BindSoleItemOf(IReadOnlyCollection<string> extensions) => BindByExtensionCore(extensions, onlyBindSingle: true);

		public void Dispose()
		{
			Unbind();
			_extractor?.Dispose();
			_extractor = null;
			_rootStream?.Dispose();
			_rootStream = null;
		}

		/// <summary>finds an ArchiveItem with the specified name (path) within the archive; returns null if it doesnt exist</summary>
		public HawkArchiveFileItem? FindArchiveMember(string? name)
			=> ArchiveItems.FirstOrNull(ai => ai.Name == name);

		/// <returns>a stream for the currently bound file</returns>
		/// <exception cref="InvalidOperationException">no stream bound (haven't called <see cref="BindArchiveMember(int)"/> or overload)</exception>
		public Stream GetStream() => _boundStream ?? throw new InvalidOperationException($"{nameof(HawkFile)}: Can't call {nameof(GetStream)}() before you've successfully bound something!");

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
		public static IFileDearchivalMethod<IHawkArchiveFile>? DearchivalMethod { get; set; }

		[return: HawkFilePath]
		private static string MakeCanonicalName(string root, string? member) => member == null ? root : $"{root}|{member}";

		/// <returns>path / member path pair iff <paramref name="path"/> contains <c>'|'</c>, <see langword="null"/> otherwise</returns>
		private static (string, string)? SplitArchiveMemberPath([HawkFilePath] string path)
		{
			var i = path.LastIndexOf('|');
#if DEBUG
			if (path.IndexOf('|') != i) Console.WriteLine($"{nameof(HawkFile)} path contains multiple '|'");
#endif
			return i == -1 ? null : (path.Substring(0, i), path.Substring(i + 1));
		}
	}
}
