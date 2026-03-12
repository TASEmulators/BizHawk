using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.Common
{
	public class XmlGame
	{
		public XmlDocument Xml { get; set; }
		public GameInfo GI { get; } = new();
		public IList<(string Path, string Filename, byte[] FileData)> Assets { get; } = [ ];

		/// <exception cref="InvalidOperationException">internal error</exception>
		public static XmlGame Create(HawkFile f)
		{
			try
			{
				var x = new XmlDocument();
				x.Load(f.GetStream());
				var y = x.SelectSingleNode("./BizHawk-XMLGame");
				if (y == null)
				{
					return null;
				}

				var ret = new XmlGame
				{
					GI =
					{
						System = y.Attributes!["System"].Value,
						Name = y.Attributes["Name"].Value,
						Status = RomStatus.Unknown,
					},
					Xml = x,
				};
				var fullPath = "";

				var n = y.SelectSingleNode("./LoadAssets");
				if (n != null)
				{
					var hashStream = new MemoryStream();
					int? originalIndex = null;

					foreach (XmlNode a in n.ChildNodes)
					{
						var filename = a.Attributes!["FileName"].Value;
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
								throw new Exception($"Couldn't load XMLGame Asset \"{filename}\"");
							}
						}
						else
						{
							// relative path
							fullPath = Path.GetDirectoryName(f.CanonicalFullPath.SubstringBefore('|')) ?? "";
							fullPath = Path.Combine(fullPath, filename.SubstringBefore('|'));
							try
							{
								using var hf = new HawkFile(fullPath, allowArchives: !MAMEMachineDB.IsMAMEMachine(fullPath));
								if (hf.IsArchive)
								{
									var archiveItem = hf.ArchiveItems.First(ai => ai.Name == filename.SubstringAfter('|'));
									hf.Unbind();
									hf.BindArchiveMember(archiveItem);
									data = hf.GetStream().ReadAllBytes();

									filename = filename.SubstringAfter('|');
									fullPath += $"|{filename}";
								}
								else
								{
									var ext = Path.GetExtension(fullPath).ToUpperInvariant();
									var isArcadeChd = ret.GI.System == VSystemID.Raw.Arcade && ext == ".CHD";
									if (RomGame.Is3DSRom(ext) || (Disc.IsValidExtension(ext) && !isArcadeChd))
									{
										data = [ ];
									}
									else
									{
										data = File.ReadAllBytes(fullPath);
									}
								}
							}
							catch (Exception e)
							{
								throw new Exception($"Couldn't load XMLGame LoadAsset \"{filename}\"", e);
							}
						}

						ret.Assets.Add((fullPath, filename, data));
						var sha1 = SHA1Checksum.Compute(data);
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
