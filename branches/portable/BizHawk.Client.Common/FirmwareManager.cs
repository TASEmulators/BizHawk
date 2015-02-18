using System;
using System.Collections.Generic;
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

		public List<FirmwareEventArgs> RecentlyServed { get; private set; }

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

		public FirmwareManager()
		{
			RecentlyServed = new List<FirmwareEventArgs>();
		}


		public ResolutionInfo Resolve(string sysId, string firmwareId)
		{
			return Resolve(FirmwareDatabase.LookupFirmwareRecord(sysId, firmwareId));
		}

		public ResolutionInfo Resolve(FirmwareDatabase.FirmwareRecord record, bool forbidScan = false)
		{
			//purpose of forbidScan: sometimes this is called from a loop in Scan(). we dont want to repeatedly DoScanAndResolve in that case, its already been done.

			bool first = true;

		RETRY:

			ResolutionInfo resolved;
			_resolutionDictionary.TryGetValue(record, out resolved);

			// couldnt find it! do a scan and resolve to try harder
			// NOTE: this could result in bad performance in some cases if the scanning happens repeatedly..
			if (resolved == null && first)
			{
				if(!forbidScan) DoScanAndResolve();
				first = false;
				goto RETRY;
			}

			return resolved;
		}

		// Requests the spcified firmware. tries really hard to scan and resolve as necessary
		public string Request(string sysId, string firmwareId)
		{
			var resolved = Resolve(sysId, firmwareId);
			if (resolved == null) return null;
			RecentlyServed.Add(new FirmwareEventArgs
					{
						SystemId = sysId,
						FirmwareId = firmwareId,
						Hash = resolved.Hash,
						Size = resolved.Size
					});
			return resolved.FilePath;
		}

		public class RealFirmwareReader : IDisposable
		{
			System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
			public void Dispose()
			{
				sha1.Dispose();
				sha1 = null;
			}
			public RealFirmwareFile Read(FileInfo fi)
			{
				var rff = new RealFirmwareFile { FileInfo = fi };
				long len = fi.Length;

				using (var fs = fi.OpenRead())
				{
					sha1.ComputeHash(fs);
				}

				rff.Hash = sha1.Hash.BytesToHexString();
				dict[rff.Hash] = rff;
				_files.Add(rff);
				return rff;
			}

			public readonly Dictionary<string, RealFirmwareFile> dict = new Dictionary<string, RealFirmwareFile>();
			private readonly List<RealFirmwareFile> _files = new List<RealFirmwareFile>();
		}

		public void DoScanAndResolve()
		{
			//build a list of file sizes. Only those will be checked during scanning
			HashSet<long> sizes = new HashSet<long>();
			foreach (var ff in FirmwareDatabase.FirmwareFiles)
				sizes.Add(ff.size);

			using(var reader = new RealFirmwareReader())
			{
				// build a list of files under the global firmwares path, and build a hash for each of them while we're at it
				var todo = new Queue<DirectoryInfo>();
				todo.Enqueue(new DirectoryInfo(PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPathFragment, null)));
	
				while (todo.Count != 0)
				{
					var di = todo.Dequeue();

					if (!di.Exists)
						continue;

					// we're going to allow recursing into subdirectories, now. its been verified to work OK
					foreach (var disub in di.GetDirectories())
					{
						todo.Enqueue(disub);
					}
				
					foreach (var fi in di.GetFiles())
					{
						if(sizes.Contains(fi.Length))
							reader.Read(fi);
					}
				}

				// now, for each firmware record, try to resolve it
				foreach (var fr in FirmwareDatabase.FirmwareRecords)
				{
					// clear previous resolution results
					_resolutionDictionary.Remove(fr);

					// get all options for this firmware (in order)
					var fr1 = fr;
					var options =
						from fo in FirmwareDatabase.FirmwareOptions
						where fo.systemId == fr1.systemId && fo.firmwareId == fr1.firmwareId && fo.IsAcceptableOrIdeal
						select fo;

					// try each option
					foreach (var fo in options)
					{
						var hash = fo.hash;

						// did we find this firmware?
						if (reader.dict.ContainsKey(hash))
						{
							// rad! then we can use it
							var ri = new ResolutionInfo
								{
									FilePath = reader.dict[hash].FileInfo.FullName,
									KnownFirmwareFile = FirmwareDatabase.FirmwareFilesByHash[hash],
									Hash = hash,
									Size = fo.size
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
					string userSpec;

					// do we have a user specification for this firmware record?
					if (Global.Config.FirmwareUserSpecifications.TryGetValue(fr.ConfigKey, out userSpec))
					{
						// flag it as user specified
						ResolutionInfo ri;
						if (!_resolutionDictionary.TryGetValue(fr, out ri))
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

						// check whether it was a known file anyway, and go ahead and bind to the known file, as a perk (the firmwares config doesnt really use this information right now)
						FirmwareDatabase.FirmwareFile ff;
						if (FirmwareDatabase.FirmwareFilesByHash.TryGetValue(rff.Hash, out ff))
						{
							ri.KnownFirmwareFile = ff;

							// if the known firmware file is for a different firmware, flag it so we can show a warning
							var option =
								(from fo in FirmwareDatabase.FirmwareOptions
								 where fo.hash == rff.Hash && fo.ConfigKey != fr.ConfigKey
								 select fr).FirstOrDefault();

							if (option != null)
							{
								ri.KnownMismatching = true;
							}
						}
					}

				} //foreach(firmware record)
			} //using(new RealFirmwareReader())
		} //DoScanAndResolve()

	} //class FirmwareManager

} //namespace