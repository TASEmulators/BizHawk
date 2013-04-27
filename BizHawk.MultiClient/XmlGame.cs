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
					foreach (XmlNode a in n.ChildNodes)
					{
						string name = a.Name;
						string filename = a.Attributes["FileName"].Value;
						if (filename[0] == '|')
						{
							// in same archive
							var ai = f.FindArchiveMember(filename.Substring(1));
							if (ai != null)
							{
								f.BindArchiveMember(ai);
								byte[] data = Util.ReadAllBytes(f.GetStream());
								ret.Assets[name] = data;
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
								byte[] data = File.ReadAllBytes(fullpath);
								ret.Assets[name] = data;
							}
							catch
							{
								throw new Exception("Couldn't load XMLGame LoadAsset \"" + name + "\"");
							}
						}
					}
				}
				return ret;
			}
			catch
			{
				return null;
			}
		}

	}
}
