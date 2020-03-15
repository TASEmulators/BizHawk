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
		// represents a file found on disk in the user's firmware directory matching a file in our database
		public class RealFirmwareFile
		{
			public FileInfo FileInfo { get; set; }
			public string Hash { get; set; }
		}

		public List<FirmwareEventArgs> RecentlyServed { get; } = new List<FirmwareEventArgs>();

		public class ResolutionInfo
		{
			public bool UserSpecified { get; set; }
			public bool Missing { get; set; }
			public bool KnownMismatching { get; set; }
			public FirmwareDatabase.FirmwareFile KnownFirmwareFile { get; set; }
			public string FilePath { get; set; }
			public string Hash { get; set; }
			public long Size { get; set; }
		}

		private readonly Dictionary<FirmwareDatabase.FirmwareRecord, ResolutionInfo> _resolutionDictionary = new Dictionary<FirmwareDatabase.FirmwareRecord, ResolutionInfo>();

		public class FirmwareEventArgs
		{
			public string Hash { get; set; }
			public long Size { get; set; }
			public string SystemId { get; set; }
			public string FirmwareId { get; set; }
		}

		public ResolutionInfo Resolve(string firmwaresPath, IDictionary<string, string> userSpecifications, FirmwareDatabase.FirmwareRecord record, bool forbidScan = false)
		{
			// purpose of forbidScan: sometimes this is called from a loop in Scan(). we don't want to repeatedly DoScanAndResolve in that case, its already been done.
			bool first = true;

		RETRY:
			_resolutionDictionary.TryGetValue(record, out var resolved);

			// couldn't find it! do a scan and resolve to try harder
			// NOTE: this could result in bad performance in some cases if the scanning happens repeatedly..
			if (resolved == null && first)
			{
				if (!forbidScan)
				{
					DoScanAndResolve(firmwaresPath, userSpecifications);
				}

				first = false;
				goto RETRY;
			}

			return resolved;
		}

		// Requests the specified firmware. tries really hard to scan and resolve as necessary
		public string Request(string firmwaresPath, IDictionary<string, string> userSpecifications, string sysId, string firmwareId)
		{
			var resolved = Resolve(firmwaresPath, userSpecifications, FirmwareDatabase.LookupFirmwareRecord(sysId, firmwareId));
			if (resolved == null)
			{
				return null;
			}

			RecentlyServed.Add(new FirmwareEventArgs
			{
				SystemId = sysId,
				FirmwareId = firmwareId,
				Hash = resolved.Hash,
				Size = resolved.Size
			});

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
				var rff = new RealFirmwareFile { FileInfo = fi };

				using (var fs = fi.OpenRead())
				{
					_sha1.ComputeHash(fs);
				}

				rff.Hash = _sha1.Hash.BytesToHexString();
				Dict[rff.Hash] = rff;
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
				if (FirmwareDatabase.FirmwareFiles.FirstOrDefault(a => a.Size == fi.Length) == null)
					return false;

				// check the hash
				using var reader = new RealFirmwareReader();
				reader.Read(fi);
				if (FirmwareDatabase.FirmwareFiles.FirstOrDefault(a => a.Hash == reader.Dict.FirstOrDefault().Value.Hash) != null)
					return true;
			}
			catch { }

			return false;
		}

		public void DoScanAndResolve(string firmwaresPath, IDictionary<string, string> userSpecifications)
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
			todo.Enqueue(new DirectoryInfo(Global.Config.PathEntries.AbsolutePathFor(firmwaresPath, null)));
	
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
				var fr1 = fr;
				var options = FirmwareDatabase.FirmwareOptions
						.Where(fo => fo.SystemId == fr1.SystemId
							&& fo.FirmwareId == fr1.FirmwareId
							&& fo.IsAcceptableOrIdeal);

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
						goto DONE_FIRMWARE;
					}
				}

				DONE_FIRMWARE: ;
			}

			// apply user overrides
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				// do we have a user specification for this firmware record?
				if (userSpecifications.TryGetValue(fr.ConfigKey, out var userSpec))
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
					var rff = reader.Read(fi);
					ri.Size = fi.Length;
					ri.Hash = rff.Hash;

					// check whether it was a known file anyway, and go ahead and bind to the known file, as a perk (the firmwares config doesn't really use this information right now)
					if (FirmwareDatabase.FirmwareFilesByHash.TryGetValue(rff.Hash, out var ff))
					{
						ri.KnownFirmwareFile = ff;

						// if the known firmware file is for a different firmware, flag it so we can show a warning
						var option = FirmwareDatabase.FirmwareOptions
							.FirstOrDefault(fo => fo.Hash == rff.Hash && fo.ConfigKey != fr.ConfigKey);

						if (option != null)
						{
							ri.KnownMismatching = true;
						}
					}
				}
			} // foreach(firmware record)
		} // DoScanAndResolve()
	} // class FirmwareManager
} // namespace