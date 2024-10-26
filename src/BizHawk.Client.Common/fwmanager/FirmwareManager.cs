#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class FirmwareManager
	{
		private static readonly FirmwareID NDS_FIRMWARE = new("NDS", "firmware");
		private const int DSI_NAND_LENGTH = 251658240 + 64;

		public static (byte[] Patched, string ActualHash) PerformPatchInMemory(byte[] @base, in FirmwarePatchOption patchOption)
		{
			var patched = patchOption.Patches.Aggregate(seed: @base, (a, fpd) => fpd.ApplyToMutating(a));
			return (patched, SHA1Checksum.ComputeDigestHex(patched));
		}

		public static (string FilePath, int FileSize, FirmwareFile FF) PerformPatchOnDisk(string baseFilename, in FirmwarePatchOption patchOption, PathEntryCollection pathEntries)
		{
			var @base = File.ReadAllBytes(baseFilename);
			var (patched, actualHash) = PerformPatchInMemory(@base, in patchOption);
			if (actualHash != patchOption.TargetHash) throw new InvalidOperationException("patch produced incorrect output");
			var patchedParentDir = Path.Combine(pathEntries[PathEntryCollection.GLOBAL, "Temp Files"].Path, "AutopatchedFirmware");
			Directory.CreateDirectory(patchedParentDir);
			var ff = FirmwareDatabase.FirmwareFilesByHash[patchOption.TargetHash];
			var patchedFilePath = Path.Combine(patchedParentDir, ff.RecommendedName);
			File.WriteAllBytes(patchedFilePath, patched);
			return (patchedFilePath, @base.Length, ff); // patches can't change length with the current implementation
		}

		/// <summary>a list of expected file sizes, used as a simple filter to speed up scanning</summary>
		private readonly HashSet<long> _firmwareSizes = new(FirmwareDatabase.FirmwareFiles.Select(ff => ff.Size));

		private readonly List<FirmwareEventArgs> _recentlyServed = new();

		private readonly Dictionary<FirmwareRecord, ResolutionInfo> _resolutionDictionary = new();

		public ICollection<FirmwareEventArgs> RecentlyServed => _recentlyServed;

		private ResolutionInfo? AttemptPatch(FirmwareRecord requested, PathEntryCollection pathEntries, IDictionary<string, string> userSpecifications)
		{
			// look for patchsets where 1. they produce a file that fulfils the request, and 2. a matching input file is present in the firmware dir
			var targetOptionHashes = FirmwareDatabase.FirmwareOptions.Where(fo => fo.ID == requested.ID).Select(fo => fo.Hash).ToList();
			var presentBaseFiles = _resolutionDictionary.Values.Select(ri => ri.Hash).ToList(); //TODO might want to use files which are known (in the database), but not assigned to any record
			var patchOption = FirmwareDatabase.AllPatches.FirstOrNull(fpo => targetOptionHashes.Contains(fpo.TargetHash) && presentBaseFiles.Contains(fpo.BaseHash));
			if (patchOption is null) return null;
			// found one, proceed with patching the base file
			var baseFilename = _resolutionDictionary.Values.First(ri => ri.Hash == patchOption.Value.BaseHash).FilePath!;
			var (patchedFilePath, patchedFileLength, ff) = PerformPatchOnDisk(baseFilename, patchOption.Value, pathEntries);
			// cache and return this new file's metadata
			userSpecifications[requested.ID.ConfigKey] = patchedFilePath;
			return _resolutionDictionary[requested] = new()
			{
				FilePath = patchedFilePath,
				KnownFirmwareFile = ff,
				Hash = patchOption.Value.TargetHash,
				Size = patchedFileLength
			};
		}

		/// <remarks>
		/// Sometimes this is called from a loop in <c>FirmwareConfig.DoScan</c>.
		/// In that case, we don't want to call <see cref="DoScanAndResolve"/> repeatedly, so we use <paramref name="forbidScan"/> to skip it.
		/// </remarks>
		public ResolutionInfo? Resolve(PathEntryCollection pathEntries, IDictionary<string, string> userSpecifications, FirmwareRecord record, bool forbidScan = false)
		{
			if (_resolutionDictionary.TryGetValue(record, out var resolved)) return resolved;
			// else couldn't find it

			if (forbidScan) return null;
			// try harder by doing a scan and resolve
			// NOTE: this could result in bad performance in some cases if the scanning happens repeatedly...
			DoScanAndResolve(pathEntries, userSpecifications);
			return _resolutionDictionary.TryGetValue(record, out var resolved1) ? resolved1 : null;
		}

		// Requests the specified firmware. tries really hard to scan and resolve as necessary
		public string? Request(PathEntryCollection pathEntries, IDictionary<string, string> userSpecifications, FirmwareID id)
		{
			var requestedRecord = FirmwareDatabase.FirmwareRecords.First(fr => fr.ID == id);
			var resolved = Resolve(pathEntries, userSpecifications, requestedRecord)
				?? AttemptPatch(requestedRecord, pathEntries, userSpecifications);
			if (resolved == null) return null;
			RecentlyServed.Add(new(id, resolved.Hash, resolved.Size));
			return resolved.FilePath;
		}

		private sealed class RealFirmwareReader
		{
			private readonly Dictionary<string, RealFirmwareFile> _dict = new();

			public IReadOnlyDictionary<string, RealFirmwareFile> Dict => _dict;

			public RealFirmwareFile Read(FileInfo fi)
			{
				using var fs = fi.OpenRead();

				// DSi NAND is huge, and the hash is useless (can't use it to identify a good dump)
				// Not sure how well the system can handle a dummy hash, so let's just hash the nocash footer (which should be unique for each NAND)
				if (fs.Length == DSI_NAND_LENGTH)
				{
					fs.Seek(-64, SeekOrigin.End);
					// we can let it fall through here, as ReadAllBytes just reads all bytes starting from the stream position :)
				}

				var hash = SHA1Checksum.ComputeDigestHex(fs.ReadAllBytes());
				return _dict![hash] = new RealFirmwareFile(fi, hash);
			}
		}

		/// <summary>
		/// Test to determine whether the supplied firmware file matches something in the firmware database
		/// </summary>
		public bool CanFileBeImported(string f)
		{
			try
			{
				var fi = new FileInfo(f);
				if (!fi.Exists) return false;

				// weed out filesizes first to reduce the unnecessary overhead of a hashing operation
				if (!_firmwareSizes.Contains(fi.Length)) return false;

				// check the hash
				var reader = new RealFirmwareReader();
				reader.Read(fi);
				var hash = reader.Dict.Values.First().Hash;
				return FirmwareDatabase.FirmwareFiles.Any(a => a.Hash == hash);
			}
			catch
			{
				return false;
			}
		}

		public void DoScanAndResolve(PathEntryCollection pathEntries, IDictionary<string, string> userSpecifications)
		{
			var reader = new RealFirmwareReader();

			// build a list of files under the global firmware path, and build a hash for each of them (as ResolutionInfo) while we're at it
			Queue<DirectoryInfo> todo = [ new(pathEntries.FirmwareAbsolutePath()) ];
			while (todo.Count != 0)
			{
				var di = todo.Dequeue();
				if (!di.Exists) continue;

				foreach (var subDir in di.GetDirectories()) todo.Enqueue(subDir); // recurse

				foreach (var fi in di.GetFiles().Where(fi => _firmwareSizes.Contains(fi.Length))) reader.Read(fi);
			}

			// now, for each firmware record, try to resolve it
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				_resolutionDictionary.Remove(fr); // clear previous resolution results
				// check each acceptable option for this firmware, looking for the first available sorted by status
				var found = FirmwareDatabase.FirmwareOptions
					.Where(fo1 => fo1.ID == fr.ID && fo1.IsAcceptableOrIdeal && reader.Dict.ContainsKey(fo1.Hash))
					.OrderByDescending(fo => fo.Status)
					.Take(1).ToArray(); // 0..1 elements (essentially, first or null)
				if (found is [ FirmwareOption fo ]) _resolutionDictionary[fr] = new()
				{
					FilePath = reader.Dict[fo.Hash].FileInfo.FullName,
					KnownFirmwareFile = FirmwareDatabase.FirmwareFilesByHash[fo.Hash],
					Hash = fo.Hash,
					Size = fo.Size
				};
			}

			// apply user overrides
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				// do we have a user specification for this firmware record?
				if (!userSpecifications.TryGetValue(fr.ID.ConfigKey, out var userSpec)) continue;

				var ri = _resolutionDictionary.GetValueOrPutNew(fr);
				// local ri is a reference to a ResolutionInfo which is now definitely in the dict

				// flag it as user specified
				ri.UserSpecified = true;
				ri.KnownFirmwareFile = null;
				ri.FilePath = userSpec;
				ri.Hash = null;

				// check whether it exists
				var fi = new FileInfo(userSpec);
				if (!fi.Exists)
				{
					ri.Missing = true;
					continue;
				}

				// compute its hash
				// NDS's firmware file contains user settings; these are over-written by sync settings, so we shouldn't allow them to impact the hash
				// fixme: there's way more stuff in nds firmware which can affect the hash. rtc fuckery, mac address, wifi settings, etc etc. a proper way to clear these out needs to come
				/*var rff = reader.Read(fr.ID == NDS_FIRMWARE
					? new FileInfo(Emulation.Cores.Consoles.Nintendo.NDS.NDS.CreateModifiedFirmware(userSpec))
					: fi);*/
				var rff = reader.Read(fi);
				ri.Size = fi.Length;
				ri.Hash = rff.Hash;

				// check whether it was a known file anyway, and go ahead and bind to the known file, as a perk (the firmware config doesn't really use this information right now)
				if (FirmwareDatabase.FirmwareFilesByHash.TryGetValue(rff.Hash, out var ff))
				{
					ri.KnownFirmwareFile = ff;

					// assume the firmware file is for a different firmware, unflag it in case it isn't
					// logic here is important, see: https://github.com/TASEmulators/BizHawk/issues/3095
					ri.KnownMismatching = true;
					if (FirmwareDatabase.FirmwareOptions.Any(fo => fo.Hash == rff.Hash && fo.ID == fr.ID))
					{
						ri.KnownMismatching = false;
					}
				}
			}
		}
	}
}
