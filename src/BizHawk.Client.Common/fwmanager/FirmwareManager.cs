using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class FirmwareManager
	{
		private static readonly FirmwareID NDS_FIRMWARE = new("NDS", "firmware");

		public List<FirmwareEventArgs> RecentlyServed { get; } = new List<FirmwareEventArgs>();

		private readonly Dictionary<FirmwareRecord, ResolutionInfo> _resolutionDictionary = new();

		// purpose of forbidScan: sometimes this is called from a loop in Scan(). we don't want to repeatedly DoScanAndResolve in that case, its already been done.
		public ResolutionInfo Resolve(PathEntryCollection pathEntries, IDictionary<string, string> userSpecifications, FirmwareRecord record, bool forbidScan = false)
		{
			_resolutionDictionary.TryGetValue(record, out var resolved);
			// couldn't find it! do a scan and resolve to try harder
			// NOTE: this could result in bad performance in some cases if the scanning happens repeatedly...
			if (resolved == null && !forbidScan)
			{
				DoScanAndResolve(pathEntries, userSpecifications);
				_resolutionDictionary.TryGetValue(record, out resolved);
			}
			return resolved;
		}

		// Requests the specified firmware. tries really hard to scan and resolve as necessary
		public string Request(PathEntryCollection pathEntries, IDictionary<string, string> userSpecifications, FirmwareID id)
		{
			var resolved = Resolve(
				pathEntries,
				userSpecifications,
				FirmwareDatabase.FirmwareRecords.First(fr => fr.ID == id));
			if (resolved == null)
			{
				return null;
			}
			RecentlyServed.Add(new(id, resolved.Hash, resolved.Size));
			return resolved.FilePath;
		}

		private class RealFirmwareReader : IDisposable
		{
			private SHA1 _sha1 = SHA1.Create();

			public void Dispose()
			{
				_sha1.Dispose();
				_sha1 = null;
			}

			public RealFirmwareFile Read(FileInfo fi)
			{
				using (var fs = fi.OpenRead())
				{
					_sha1.ComputeHash(fs);
				}

				var hash = _sha1.Hash.BytesToHexString();
				var rff = new RealFirmwareFile(fi, hash);
				Dict[hash] = rff;
				return rff;
			}

			public Dictionary<string, RealFirmwareFile> Dict { get; } = new Dictionary<string, RealFirmwareFile>();
		}

		/// <summary>
		/// Test to determine whether the supplied firmware file matches something in the firmware database
		/// </summary>
		public bool CanFileBeImported(string f)
		{
			try
			{
				var fi = new FileInfo(f);
				if (!fi.Exists)
					return false;

				// weed out filesizes first to reduce the unnecessary overhead of a hashing operation
				if (FirmwareDatabase.FirmwareFiles.All(a => a.Size != fi.Length)) return false;

				// check the hash
				using var reader = new RealFirmwareReader();
				reader.Read(fi);
				var hash = reader.Dict.Values.First().Hash;
				if (FirmwareDatabase.FirmwareFiles.Any(a => a.Hash == hash)) return true;
			}
			catch { }

			return false;
		}

		public void DoScanAndResolve(PathEntryCollection pathEntries, IDictionary<string, string> userSpecifications)
		{
			// build a list of file sizes. Only those will be checked during scanning
			var sizes = new HashSet<long>();
			foreach (var ff in FirmwareDatabase.FirmwareFiles)
			{
				sizes.Add(ff.Size);
			}

			using var reader = new RealFirmwareReader();

			// build a list of files under the global firmwares path, and build a hash for each of them while we're at it
			var todo = new Queue<DirectoryInfo>();
			todo.Enqueue(new DirectoryInfo(pathEntries.AbsolutePathFor(pathEntries.FirmwaresPathFragment, null)));
	
			while (todo.Count != 0)
			{
				var di = todo.Dequeue();

				if (!di.Exists)
				{
					continue;
				}

				// we're going to allow recursing into subdirectories, now. its been verified to work OK
				foreach (var subDir in di.GetDirectories())
				{
					todo.Enqueue(subDir);
				}
				
				foreach (var fi in di.GetFiles())
				{
					if (sizes.Contains(fi.Length))
					{
						reader.Read(fi);
					}
				}
			}

			// now, for each firmware record, try to resolve it
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				// clear previous resolution results
				_resolutionDictionary.Remove(fr);

				// get all options for this firmware (in order)
				var id = fr.ID;
				var options = FirmwareDatabase.FirmwareOptions.Where(fo => fo.ID == id && fo.IsAcceptableOrIdeal);

				// try each option
				foreach (var fo in options)
				{
					var hash = fo.Hash;

					// did we find this firmware?
					if (reader.Dict.ContainsKey(hash))
					{
						// rad! then we can use it
						var ri = new ResolutionInfo
						{
							FilePath = reader.Dict[hash].FileInfo.FullName,
							KnownFirmwareFile = FirmwareDatabase.FirmwareFilesByHash[hash],
							Hash = hash,
							Size = fo.Size
						};
						_resolutionDictionary[fr] = ri;
						break;
					}
				}
			}

			// apply user overrides
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				// do we have a user specification for this firmware record?
				if (userSpecifications.TryGetValue(fr.ID.ConfigKey, out var userSpec))
				{
					// flag it as user specified
					if (!_resolutionDictionary.TryGetValue(fr, out ResolutionInfo ri))
					{
						ri = new ResolutionInfo();
						_resolutionDictionary[fr] = ri;
					}

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
					var rff = reader.Read(fr.ID == NDS_FIRMWARE
						? new FileInfo(Emulation.Cores.Consoles.Nintendo.NDS.MelonDS.CreateModifiedFirmware(userSpec))
						: fi);
					ri.Size = fi.Length;
					ri.Hash = rff.Hash;

					// check whether it was a known file anyway, and go ahead and bind to the known file, as a perk (the firmwares config doesn't really use this information right now)
					if (FirmwareDatabase.FirmwareFilesByHash.TryGetValue(rff.Hash, out var ff))
					{
						ri.KnownFirmwareFile = ff;

						// if the known firmware file is for a different firmware, flag it so we can show a warning
						if (FirmwareDatabase.FirmwareOptions.Any(fo => fo.Hash == rff.Hash && fo.ID != fr.ID))
						{
							ri.KnownMismatching = true;
						}
					}
				}
			} // foreach(firmware record)
		} // DoScanAndResolve()
	} // class FirmwareManager
} // namespace