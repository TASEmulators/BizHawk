using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using BizHawk.API.ApiHawk;
using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class XmlGame
	{
		public XmlDocument Xml { get; set; }
		public GameInfo GI { get; } = new GameInfo();
		public IList<KeyValuePair<string, byte[]>> Assets { get; } = new List<KeyValuePair<string, byte[]>>();
		public IList<string> AssetFullPaths { get; } = new List<string>(); // TODO: Hack work around, to avoid having to refactor Assets into a object array, should be refactored!

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
						System = y.Attributes["System"].Value,
						Name = y.Attributes["Name"].Value,
						Status = RomStatus.Unknown
					},
					Xml = x
				};
				string fullPath = "";

				var n = y.SelectSingleNode("./LoadAssets");
				if (n != null)
				{
					var hashStream = new MemoryStream();
					int? originalIndex = null;

					foreach (XmlNode a in n.ChildNodes)
					{
						string filename = a.Attributes["FileName"].Value;
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
							fullPath = Path.GetDirectoryName(f.CanonicalFullPath.Split('|').First()) ?? "";
							fullPath = Path.Combine(fullPath, filename.Split('|').First());
							try
							{
								using var hf = new HawkFile(fullPath);
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
									data = File.ReadAllBytes(fullPath.Split('|').First());
								}
							}
							catch
							{
								throw new Exception($"Couldn't load XMLGame LoadAsset \"{filename}\"");
							}
						}

						ret.Assets.Add(new KeyValuePair<string, byte[]>(filename, data));
						ret.AssetFullPaths.Add(fullPath);
						using var sha1 = System.Security.Cryptography.SHA1.Create();
						sha1.TransformFinalBlock(data, 0, data.Length);
						hashStream.Write(sha1.Hash, 0, sha1.Hash.Length);
					}

					ret.GI.Hash = hashStream.GetBuffer().HashSHA1(0, (int)hashStream.Length);
					hashStream.Close();
					if (originalIndex != null)
					{
						f.Unbind();
						f.BindArchiveMember((int)originalIndex);
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
