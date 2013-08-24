using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

//IDEA: put filesizes in DB too. then scans can go real quick by only scanning filesizes that match (and then scanning filesizes that dont match, in case of an emergency)
//this would be adviseable if we end up with a very large firmware file

namespace BizHawk.MultiClient
{
	public class FirmwareManager
	{
		//represents a file found on disk in the user's firmware directory matching a file in our database
		class RealFirmwareFile
		{
			public FileInfo fi;
			public string hash;
		}

		public class ResolutionInfo
		{
			public bool UserSpecified;
			public bool Missing;
			public bool KnownMismatching;
			public FirmwareDatabase.FirmwareFile KnownFirmwareFile;
			public string FilePath;
			public string Hash;
		}

		Dictionary<FirmwareDatabase.FirmwareRecord, ResolutionInfo> ResolutionDictionary = new Dictionary<FirmwareDatabase.FirmwareRecord, ResolutionInfo>();


		public ResolutionInfo Resolve(string sysId, string firmwareId)
		{
			return Resolve(FirmwareDatabase.LookupFirmwareRecord(sysId, firmwareId));
		}

		public ResolutionInfo Resolve(FirmwareDatabase.FirmwareRecord record)
		{
			bool first = true;

		RETRY:

			ResolutionInfo resolved = null;
			ResolutionDictionary.TryGetValue(record, out resolved);

			//couldnt find it! do a scan and resolve to try harder
			if (resolved == null && first)
			{
				DoScanAndResolve();
				first = false;
				goto RETRY;
			}

			return resolved;
		}

		//Requests the spcified firmware. tries really hard to scan and resolve as necessary
		public string Request(string sysId, string firmwareId)
		{
			var resolved = Resolve(sysId, firmwareId);
			if (resolved == null) return null;
			return resolved.FilePath;
		}

		class RealFirmwareReader
		{
			byte[] buffer = new byte[0];
			public RealFirmwareFile Read(FileInfo fi)
			{
				RealFirmwareFile rff = new RealFirmwareFile();
				rff.fi = fi;
				long len = fi.Length;
				if (len > buffer.Length) buffer = new byte[len];
				using (var fs = fi.OpenRead()) fs.Read(buffer, 0, (int)len);
				rff.hash = Util.Hash_SHA1(buffer, 0, (int)len);
				dict[rff.hash] = rff;
				files.Add(rff);
				return rff;
			}
			public Dictionary<string, RealFirmwareFile> dict = new Dictionary<string, RealFirmwareFile>();
			public List<RealFirmwareFile> files = new List<RealFirmwareFile>();
		}

		public void DoScanAndResolve()
		{
			RealFirmwareReader reader = new RealFirmwareReader();

			//build a list of files under the global firmwares path, and build a hash for each of them while we're at it
			var todo = new Queue<DirectoryInfo>(new[] { new DirectoryInfo(Global.Config.PathEntries.FirmwaresPath) });
	
			while (todo.Count != 0)
			{
				var di = todo.Dequeue();

				//we're going to allow recursing into subdirectories, now. its been verified to work OK
				if(di.Exists)
				{
					foreach (var disub in di.GetDirectories()) todo.Enqueue(disub);
				
					foreach (var fi in di.GetFiles())
					{
						reader.Read(fi);
					}
				}
			}

			//now, for each firmware record, try to resolve it
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				//clear previous resolution results
				ResolutionDictionary.Remove(fr);

				//get all options for this firmware (in order)
				var options =
					from fo in FirmwareDatabase.FirmwareOptions
					where fo.systemId == fr.systemId && fo.firmwareId == fr.firmwareId
					select fo;

				//try each option
				foreach (var fo in options)
				{
					var hash = fo.hash;
					//did we find this firmware?
					if (reader.dict.ContainsKey(hash))
					{
						//rad! then we can use it
						var ri = new ResolutionInfo();
						ri.FilePath = reader.dict[hash].fi.FullName;
						ri.KnownFirmwareFile = FirmwareDatabase.FirmwareFilesByHash[hash];
						ri.Hash = hash;
						ResolutionDictionary[fr] = ri;
						goto DONE_FIRMWARE;
					}
				}

			DONE_FIRMWARE: ;

			}

			//apply user overrides
			foreach (var fr in FirmwareDatabase.FirmwareRecords)
			{
				string userSpec = null;
				
				//do we have a user specification for this firmware record?
				if (Global.Config.FirmwareUserSpecifications.TryGetValue(fr.ConfigKey, out userSpec))
				{
					//flag it as user specified
					var ri = ResolutionDictionary[fr];
					ri.UserSpecified = true;
					ri.KnownFirmwareFile = null;
					ri.FilePath = userSpec;
					ri.Hash = null;

					//check whether it exists
					var fi = new FileInfo(userSpec);
					if (!fi.Exists)
					{
						ri.Missing = true;
						continue;
					}

					//compute its hash 
					var rff = reader.Read(fi);
					ri.Hash = rff.hash;

					//check whether it was a known file anyway, and go ahead and bind to the known file, as a perk (the firmwares config doesnt really use this information right now)
					FirmwareDatabase.FirmwareFile ff = null;
					if(FirmwareDatabase.FirmwareFilesByHash.TryGetValue(rff.hash,out ff))
					{
						ri.KnownFirmwareFile = ff;

						//if the known firmware file is for a different firmware, flag it so we can show a warning
						var option =
							(from fo in FirmwareDatabase.FirmwareOptions
							where fo.hash == rff.hash && fo.ConfigKey != fr.ConfigKey
							select fr).FirstOrDefault();
						if (option != null)
							ri.KnownMismatching = true;
					}
				}
			}
		}

	}

}