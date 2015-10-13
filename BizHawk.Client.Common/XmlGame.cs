using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class XmlGame
	{
		public XmlGame()
		{
			Assets = new List<KeyValuePair<string, byte[]>>();
			AssetFullPaths = new List<string>();
			GI = new GameInfo();
		}

		public XmlDocument Xml { get; set; }
		public GameInfo GI { get; set; }
		public IList<KeyValuePair<string, byte[]>> Assets { get; set; }
		public IList<string> AssetFullPaths { get; set; } // TODO: Hack work around, to avoid having to refactor Assets into a object array, should be refactored!

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
								System = y.Attributes["System"].Value,
								Name = y.Attributes["Name"].Value,
								Status = RomStatus.Unknown
							},
						Xml = x
					};
				string fullpath = string.Empty;

				var n = y.SelectSingleNode("./LoadAssets");
				if (n != null)
				{
					var HashStream = new MemoryStream();
					int? OriginalIndex = null;

					foreach (XmlNode a in n.ChildNodes)
					{
						string filename = a.Attributes["FileName"].Value;
						byte[] data = new byte[0];
						if (filename[0] == '|')
						{
							// in same archive
							var ai = f.FindArchiveMember(filename.Substring(1));
							if (ai != null)
							{
								if (OriginalIndex == null)
								{
									OriginalIndex = f.GetBoundIndex();
								}

								f.Unbind();
								f.BindArchiveMember(ai);
								data = f.GetStream().ReadAllBytes();
							}
							else
							{
								throw new Exception("Couldn't load XMLGame Asset \"" + filename + "\"");
							}
						}
						else
						{
							// relative path
							fullpath = Path.GetDirectoryName(f.CanonicalFullPath.Split('|').First()) ?? string.Empty;
							fullpath = Path.Combine(fullpath, filename.Split('|').First());
							try
							{
								using (var hf = new HawkFile(fullpath))
								{
									if (hf.IsArchive)
									{
										var archiveItem = hf.ArchiveItems.First(ai => ai.Name == filename.Split('|').Skip(1).First());
										hf.Unbind();
										hf.BindArchiveMember(archiveItem);
										data = hf.GetStream().ReadAllBytes();
									}
									else
									{
										data = File.ReadAllBytes(fullpath.Split('|').First());
									}
								}

								
							}
							catch
							{
								throw new Exception("Couldn't load XMLGame LoadAsset \"" + filename + "\"");
							}
						}

						ret.Assets.Add(new KeyValuePair<string, byte[]>(filename, data));
						ret.AssetFullPaths.Add(fullpath);
						using (var sha1 = System.Security.Cryptography.SHA1.Create())
						{
							sha1.TransformFinalBlock(data, 0, data.Length);
							HashStream.Write(sha1.Hash, 0, sha1.Hash.Length);
						}
					}

					ret.GI.Hash = HashStream.GetBuffer().HashSHA1(0, (int)HashStream.Length);
					HashStream.Close();
					if (OriginalIndex != null)
					{
						f.Unbind();
						f.BindArchiveMember((int)OriginalIndex);
					}
				}
				else
				{
					ret.GI.Hash = "0000000000000000000000000000000000000000";
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
