using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Threading;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public class BootGodDb
	{
		/// <summary>
		/// blocks until the DB is done loading
		/// </summary>
		static EventWaitHandle acquire;

		bool validate = true;

		private readonly Bag<string, CartInfo> _sha1Table = new Bag<string, CartInfo>();

		static BootGodDb instance;

		public static void Initialize(string basePath)
		{
			if (acquire != null) throw new InvalidOperationException("Bootgod DB multiply initialized");
			acquire =  new EventWaitHandle(false, EventResetMode.ManualReset); ;

			var stopwatch = Stopwatch.StartNew();
			ThreadPool.QueueUserWorkItem(_ =>
			{
				instance = new BootGodDb(basePath);
				Console.WriteLine("Bootgod DB load: " + stopwatch.Elapsed + " sec");
				acquire.Set();
			});
		}

		private BootGodDb() { }

		private int ParseSize(string str)
		{
			int temp = 0;
			if(validate) if (!str.EndsWith("k")) throw new Exception();
			int len = str.Length - 1;
			for (int i = 0; i < len; i++)
			{
				temp *= 10;
				temp += (str[i] - '0');
			}
			return temp;
		}

		public BootGodDb(string basePath)
		{
			// notes: there can be multiple each of prg,chr,wram,vram
			// we aren't tracking the individual hashes yet.

			string xmlPath = Path.Combine(basePath, "NesCarts.xml");
			string x7zPath = Path.Combine(basePath, "NesCarts.7z");
			bool loadXml = File.Exists(xmlPath);
			using var nesCartFile = new HawkFile(loadXml ? xmlPath : x7zPath);
			if (!loadXml)
			{
				nesCartFile.BindFirst();
			}

			var stream = nesCartFile.GetStream();

			// in anticipation of any slowness annoying people, and just for shits and giggles, i made a super fast parser
			int state=0;
			var xmlReader = XmlReader.Create(stream);
			CartInfo currCart = null;
			string currName = null;
			while (xmlReader.Read())
			{
				switch (state)
				{
					case 0:
						if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "game")
						{
							currName = xmlReader.GetAttribute("name");
							state = 1;
						}
						break;
					case 2:
						if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "board")
						{
							currCart.BoardType = xmlReader.GetAttribute("type");
							currCart.Pcb = xmlReader.GetAttribute("pcb");
							int mapper = int.Parse(xmlReader.GetAttribute("mapper"));
							if (validate && mapper > 255) throw new Exception("didnt expect mapper>255!");
							// we don't actually use this value at all; only the board name
							state = 3;
						}
						break;
					case 3:
						if (xmlReader.NodeType == XmlNodeType.Element)
						{
							switch(xmlReader.Name)
							{
								case "prg":
									currCart.PrgSize += (short)ParseSize(xmlReader.GetAttribute("size"));
									break;
								case "chr":
									currCart.ChrSize += (short)ParseSize(xmlReader.GetAttribute("size"));
									break;
								case "vram":
									currCart.VramSize += (short)ParseSize(xmlReader.GetAttribute("size"));
									break;
								case "wram":
									currCart.WramSize += (short)ParseSize(xmlReader.GetAttribute("size"));
									if (xmlReader.GetAttribute("battery") != null)
										currCart.WramBattery = true;
									break;
								case "pad":
									currCart.PadH = byte.Parse(xmlReader.GetAttribute("h"));
									currCart.PadV = byte.Parse(xmlReader.GetAttribute("v"));
									break;
								case "chip":
									currCart.Chips.Add(xmlReader.GetAttribute("type"));
									break;
							}
						} else 
						if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "board")
						{
							state = 4;
						}
						break;
					case 4:
						if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "cartridge")
						{
							_sha1Table[currCart.Sha1].Add(currCart);
							currCart = null;
							state = 5;
						}
						break;
					case 5:
					case 1:
						if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "cartridge")
						{
							currCart = new CartInfo();
							currCart.System = xmlReader.GetAttribute("system");
							currCart.Sha1 = "sha1:" + xmlReader.GetAttribute("sha1");
							currCart.Name = currName;
							state = 2;
						}
						if (xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "game")
						{
							currName = null;
							state = 0;
						}
						break;
				}
			} //end xmlreader loop
		}

		public static List<CartInfo> Identify(string sha1)
		{
			if (acquire == null) throw new InvalidOperationException("Bootgod DB not initialized. It's a client responsibility because only a client knows where the database is located.");
			acquire.WaitOne();
			return instance._sha1Table[sha1];
		}
	}
}
