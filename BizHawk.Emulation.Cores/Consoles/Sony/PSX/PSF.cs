using System;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	public class PSF
	{
		public Dictionary<string, string> TagsDictionary = new Dictionary<string, string>();
		public List<string> LooseTags = new List<string>();

		public byte[] Data;
		public byte[] LibData;

		public bool Load(string fpPSF, Func<Stream,int,byte[]> cbDeflater)
		{
			using(var fs = File.OpenRead(fpPSF))
			{
				//not endian safe
				var br = new BinaryReader(fs);
				var sig = br.ReadStringFixedAscii(4);
				if (sig != "PSF\x1")
					return false;

				int reserved_size = br.ReadInt32();
				int compressed_size = br.ReadInt32();
				int compressed_crc32 = br.ReadInt32();
				
				//load tags
				//tags run until the end of the file
				fs.Position = 16 + reserved_size + compressed_size;
				if (fs.Position + 5 > fs.Length)
				{
					//theres no space for tags, probably just no tags in the file
				}
				else
				{
					if (br.ReadStringFixedAscii(5) == "[TAG]")
					{
						var tagstring = br.ReadStringFixedAscii((int)(fs.Length - fs.Position)).Replace("\r\n", "\n");
						foreach (var tag in tagstring.Split('\n', '\x0'))
						{
							if (tag.Trim() == "")
								continue;
							int eq = tag.IndexOf('=');
							if (eq != -1)
								TagsDictionary[tag.Substring(0, eq)] = tag.Substring(eq + 1);
							else
								LooseTags.Add(tag);
						}
					}
				}

				//load compressed section buffer
				fs.Position = 16 + reserved_size;
				Data = cbDeflater(fs, compressed_size);

				//load lib if needed
				if (TagsDictionary.ContainsKey("_lib"))
				{
					var fpLib = Path.Combine(Path.GetDirectoryName(fpPSF), TagsDictionary["_lib"]);
					if (!File.Exists(fpLib))
						return false;
					PSF lib = new PSF();
					if (!lib.Load(fpLib,cbDeflater))
						return false;
					LibData = lib.Data;
				}
			}

			return true;
		}
	}
}
