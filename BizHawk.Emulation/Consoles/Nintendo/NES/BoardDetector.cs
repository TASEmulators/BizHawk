using System;
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		public class BootGodDB
		{
			bool validate = true;

			public static BootGodDB Instance;
			public static Func<byte[]> GetDatabaseBytes;
			public static void Initialize()
			{
				if(Instance == null) 
					Instance = new BootGodDB();
			}
			int ParseSize(string str)
			{
				int temp = 0;
				if(validate) if (!str.EndsWith("k")) throw new Exception();
				int len=str.Length-1;
				for (int i = 0; i < len; i++)
				{
					temp *= 10;
					temp += (str[i] - '0');
				}
				return temp;
			}
			public BootGodDB()
			{
				//notes: there can be multiple each of prg,chr,wram,vram
				//we arent tracking the individual hashes yet.

				//in anticipation of any slowness annoying people, and just for shits and giggles, i made a super fast parser
				int state=0;
				var xmlreader = XmlReader.Create(new MemoryStream(GetDatabaseBytes()));
				Game currGame = null;
				Cart currCart = null;
				while (xmlreader.Read())
				{
					switch (state)
					{
						case 0:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "game")
							{
								currGame = new Game();
								currGame.name = xmlreader.GetAttribute("name");
								state = 1;
							}
							break;
						case 1:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "cartridge")
							{
								currCart = new Cart();
								currCart.game = currGame;
								currCart.system = xmlreader.GetAttribute("system");
								currCart.sha1 = "sha1:" + xmlreader.GetAttribute("sha1");
								state = 2;
							}
							break;
						case 2:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "board")
							{
								currCart.board_type = xmlreader.GetAttribute("type");
								int mapper = byte.Parse(xmlreader.GetAttribute("mapper"));
								if (validate && mapper > 255) throw new Exception("didnt expect mapper>255!");
								currCart.mapper = (byte)mapper;
								state = 3;
							}
							break;
						case 3:
							if (xmlreader.NodeType == XmlNodeType.Element)
							{
								switch(xmlreader.Name)
								{
									case "prg":
										currCart.prg_size += (short)ParseSize(xmlreader.GetAttribute("size"));
										break;
									case "chr":
										currCart.chr_size += (short)ParseSize(xmlreader.GetAttribute("size"));
										break;
									case "vram":
										currCart.vram_size += (short)ParseSize(xmlreader.GetAttribute("size"));
										break;
									case "wram":
										currCart.wram_size += (short)ParseSize(xmlreader.GetAttribute("size"));
										if (xmlreader.GetAttribute("battery") != null)
											currCart.wram_battery = true;
										break;
									case "pad":
										currCart.pad_h = byte.Parse(xmlreader.GetAttribute("h"));
										currCart.pad_v = byte.Parse(xmlreader.GetAttribute("v"));
										break;
									case "chip":
										currCart.chips.Add(xmlreader.GetAttribute("type"));
										break;
								}
							} else 
							if (xmlreader.NodeType == XmlNodeType.EndElement && xmlreader.Name == "board")
							{
								state = 4;
							}
							break;
						case 4:
							if (xmlreader.NodeType == XmlNodeType.EndElement && xmlreader.Name == "cartridge")
							{
								currGame.carts.Add(currCart);
								currCart = null;
								state = 5;
							}
							break;
						case 5:
							if (xmlreader.NodeType == XmlNodeType.EndElement && xmlreader.Name == "game")
							{
								games.Add(currGame);
								currGame = null;
								state = 0;
							}
							break;
					}
				} //end xmlreader loop

				//analyze
				foreach (Game game in games)
				{
					foreach (Cart cart in game.carts)
					{
						sha1_table[cart.sha1].Add(cart);
					}
				}
			}


			public class Cart
			{
				//we have merged board into cartridge because there is never more than one board per cartridge
				public Game game;

				public short chr_size;
				public short prg_size;
				public short wram_size, vram_size;
				public byte pad_h, pad_v, mapper;
				public bool wram_battery;

				public string board_type;

				public string sha1;
				public string system;
				public List<string> chips = new List<string>();

				public override string ToString()
				{
					return string.Format("r={0},vr={1},pr={2},cr={3},ba={4},pa={5},{6},brd={7},map={8},sys={9}", prg_size, chr_size, wram_size, vram_size, wram_battery, pad_h, pad_v, board_type, mapper, system);
				}
			}

			public class Game
			{
				public string name;
				public List<Cart> carts = new List<Cart>();
				public override string ToString()
				{
					return string.Format("name={0}", name);
				}
			}

			List<Game> games = new List<Game>(); //maybe we dont need to track this
			Bag<string, Cart> sha1_table = new Bag<string, Cart>();

			public List<Cart> Identify(string sha1)
			{
				if (!sha1_table.ContainsKey(sha1)) return new List<Cart>();
				return sha1_table[sha1];
			}
		}

//        static class BoardDetector
//        {
//            public static string Detect(RomInfo romInfo)
//            {
//                string key = string.Format("{0}	{1}	{2}	{3}",romInfo.MapperNumber,romInfo.PRG_Size,romInfo.CHR_Size,romInfo.PRAM_Size);
//                string board;
//                Table.TryGetValue(key, out board);
//                return board;
//            }

//            public static Dictionary<string,string> Table = new Dictionary<string,string>();
//            static BoardDetector()
//            {
//                var sr = new StringReader(ClassifyTable);
//                string line;
//                while ((line = sr.ReadLine()) != null)
//                {
//                    var parts = line.Split('\t');
//                    if (parts.Length < 5) continue;
//                    string key = parts[0] + "\t" + parts[1] + "\t" + parts[2] + "\t" + parts[3];
//                    string board = line.Replace(key, "");
//                    board = board.TrimStart('\t');
//                    if (board.IndexOf(';') != -1)
//                        board = board.Substring(0, board.IndexOf(';'));
//                    Table[key] = board;
//                }
//            }
////MAP	PRG	CHR	PRAM	BOARD
//            static string ClassifyTable = @"
//0	1	1	0	NROM
//0	2	1	0	NROM
//1	8	0	8	SNROM;	this handles zelda,
//2	8	0	0	UNROM
//2	16	0	0	UOROM
//3	2	2	0	CNROM
//3	2	4	0	CNROM
//7	8	0	0	ANROM
//7	16	0	0	AOROM
//11	4	2	0	Discrete_74x377
//11	2	4	0	Discrete_74x377
//13	2	0	0	CPROM
//66	4	2	0	GxROM
//66	8	4	0	GxROM
//";

//        }
	}
}

                        //STD_SAROM                  = MakeId<    1,   64,   64,  8,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SBROM                  = MakeId<    1,   64,   64,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SCROM                  = MakeId<    1,   64,  128,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SEROM                  = MakeId<    1,   32,   64,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SFROM                  = MakeId<    1,  256,   64,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SGROM                  = MakeId<    1,  256,    0,  0,  0, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SHROM                  = MakeId<    1,   32,  128,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SJROM                  = MakeId<    1,  256,   64,  8,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SKROM                  = MakeId<    1,  256,  128,  8,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SLROM                  = MakeId<    1,  256,  128,  0,  0, CRM_0,  NMT_H,  0 >::ID,
                        //STD_SNROM                  = MakeId<    1,  256,    0,  8,  0, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SOROM                  = MakeId<    1,  256,    0,  8,  8, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SUROM                  = MakeId<    1,  512,    0,  8,  0, CRM_8,  NMT_H,  0 >::ID,
                        //STD_SXROM                  = MakeId<    1,  512,    0, 32,  0, CRM_8,  NMT_H,  0 >::ID,