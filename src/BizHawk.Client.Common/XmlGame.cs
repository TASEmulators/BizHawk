using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;

namespace BizHawk.Client.Common
{
	public class XmlGame
	{
		public XmlDocument Xml { get; set; }
		public GameInfo GI { get; } = new();
		public IList<KeyValuePair<string, byte[]>> Assets { get; } = new List<KeyValuePair<string, byte[]>>();
		public IList<string> AssetFullPaths { get; } = new List<string>(); // TODO: Hack work around, to avoid having to refactor Assets into a object array, should be refactored!

		/// <exception cref="InvalidOperationException">internal error</exception>
		public static XmlGame Create(HawkFile f)
		{
			try
			{
				XmlDocument x = new();
				x.Load(f.GetStream());
				var y = x.SelectSingleNode("./BizHawk-XMLGame");
				if (y == null)
				{
					return null;
				}

				XmlGame ret = new()
				{
					GI =
					{
						System = y.Attributes!["System"].Value,
						Name = y.Attributes["Name"].Value,
						Status = RomStatus.Unknown
					},
					Xml = x
				};
				string fullPath = "";

				var n = y.SelectSingleNode("./LoadAssets");
				if (n != null)
				{
					MemoryStream hashStream = new();
					int? originalIndex = null;

					foreach (XmlNode a in n.ChildNodes)
					{
						string filename = a.Attributes!["FileName"].Value;
						byte[] data;
						if (filename[0] == '|')
						{
							// in same archive
							var ai = f.FindArchiveMember(filename.Substring(1));
							if (ai != null)
							{
								originalIndex ??= f.BoundIndex;
								f.Unbind();
								f.BindArchiveMember(ai.Value);
								data = f.GetStream().ReadAllBytes();
							}
							else
							{
								throw new($"Couldn't load XMLGame Asset \"{filename}\"");
							}
						}
						else
						{
							// relative path
							fullPath = Path.GetDirectoryName(f.CanonicalFullPath.SubstringBefore('|')) ?? "";
							fullPath = Path.Combine(fullPath, filename.SubstringBefore('|'));
							try
							{
								using HawkFile hf = new(fullPath, allowArchives: !MAMEMachineDB.IsMAMEMachine(fullPath));
								if (hf.IsArchive)
								{
									var archiveItem = hf.ArchiveItems.First(ai => ai.Name == filename.Split('|').Skip(1).First());
									hf.Unbind();
									hf.BindArchiveMember(archiveItem);
									data = hf.GetStream().ReadAllBytes();

									filename = filename.Split('|').Skip(1).First();
								}
								else
								{
									string path = fullPath.SubstringBefore('|');
									data = RomGame.Is3DSRom(Path.GetExtension(path).ToUpperInvariant())
										? Array.Empty<byte>()
										: File.ReadAllBytes(path);
								}
							}
							catch (Exception e)
							{
								throw new($"Couldn't load XMLGame LoadAsset \"{filename}\"", e);
							}
						}

						ret.Assets.Add(new(filename, data));
						ret.AssetFullPaths.Add(fullPath);
						byte[] sha1 = SHA1Checksum.Compute(data);
						hashStream.Write(sha1, 0, sha1.Length);
					}

					ret.GI.Hash = SHA1Checksum.ComputeDigestHex(hashStream.GetBufferAsSpan());
					hashStream.Close();
					if (originalIndex != null)
					{
						f.Unbind();
						f.BindArchiveMember((int)originalIndex);
					}
				}
				else
				{
					ret.GI.Hash = SHA1Checksum.Zero;
				}

				return ret;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(ex.ToString());
			}
		}
	}
}
