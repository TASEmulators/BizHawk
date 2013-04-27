using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace BizHawk.MultiClient
{
	public class XmlGame
	{
		public XmlDocument Xml;
		public GameInfo GI = new GameInfo();
		public Dictionary<string, byte[]> Assets = new Dictionary<string, byte[]>();

		public static XmlGame Create(HawkFile f)
		{
			try
			{
				var x = new XmlDocument();
				x.Load(f.GetStream());
				var y = x.SelectSingleNode("./BizHawk-XMLGame");
				if (y == null)
					return null;

				var ret = new XmlGame();
				ret.GI.System = y.Attributes["System"].Value;
				ret.GI.Name = y.Attributes["Name"].Value;
				ret.GI.Status = RomStatus.Unknown;
				ret.Xml = x;

				var n = y.SelectSingleNode("./LoadAssets");
				if (n != null)
				{
					MemoryStream HashStream = new MemoryStream();

					foreach (XmlNode a in n.ChildNodes)
					{
						string name = a.Name;
						string filename = a.Attributes["FileName"].Value;
						byte[] data;
						if (filename[0] == '|')
						{
							// in same archive
							var ai = f.FindArchiveMember(filename.Substring(1));
							if (ai != null)
							{
								f.Unbind();
								f.BindArchiveMember(ai);
								data = Util.ReadAllBytes(f.GetStream());
							}
							else
							{
								throw new Exception("Couldn't load XMLGame LoadAsset \"" + name + "\"");
							}
						}
						else
						{
							// relative path
							string fullpath = Path.GetDirectoryName(f.CanonicalFullPath.Split('|')[0]);
							fullpath = Path.Combine(fullpath, filename);
							try
							{
								data = File.ReadAllBytes(fullpath);
							}
							catch
							{
								throw new Exception("Couldn't load XMLGame LoadAsset \"" + name + "\"");
							}
						}
						ret.Assets[name] = data;

						using (var sha1 = System.Security.Cryptography.SHA1.Create())
						{
							sha1.TransformFinalBlock(data, 0, data.Length);
							HashStream.Write(sha1.Hash, 0, sha1.Hash.Length);
						}
					}
					ret.GI.Hash = Util.Hash_SHA1(HashStream.GetBuffer(), 0, (int)HashStream.Length);
					HashStream.Close();
				}
				else
				{
					ret.GI.Hash = "0000000000000000000000000000000000000000";
				}
				return ret;
			}
			catch (Exception e)
			{
				System.Windows.Forms.MessageBox.Show(e.ToString(), "XMLGame Load Error");
				return null;
			}
		}

	}
}
