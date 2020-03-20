using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using BizHawk.Common;
using BizHawk.Emulation.Common;

//TODO - could stringpool the bootgod DB for a pedantic optimization

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	partial class NES
	{
		static List<Type> INESBoardImplementors = new List<Type>();

		static INesBoard CreateBoardInstance(Type boardType)
		{
			var board = (INesBoard)Activator.CreateInstance(boardType);
			lock (INESBoardImplementors)
			{
				//put the one we chose at the top of the list, for quicker access in the future
				int x = INESBoardImplementors.IndexOf(boardType);
				//(swap)
				var temp = INESBoardImplementors[0];
				INESBoardImplementors[0] = boardType;
				INESBoardImplementors[x] = temp;
			}
			return board;
		}

		public string BoardName => Board.GetType().Name;

		void BoardSystemHardReset()
		{
			INesBoard newboard;
			// FDS and NSF have a unique activation setup
			if (Board is FDS)
			{
				var newfds = new FDS();
				var oldfds = Board as FDS;
				newfds.biosrom = oldfds.biosrom;
				newfds.SetDiskImage(oldfds.GetDiskImage());
				newboard = newfds;
			}
			else if (Board is NSFBoard)
			{
				var newnsf = new NSFBoard();
				var oldnsf = Board as NSFBoard;
				newnsf.InitNSF(oldnsf.nsf);
				newboard = newnsf;
			}
			else
			{
				newboard = CreateBoardInstance(Board.GetType());
			}
			newboard.Create(this);
			// i suppose the old board could have changed its initial register values, although it really shouldn't
			// you can't use SyncSettings.BoardProperties here because they very well might be different than before
			// in case the user actually changed something in the UI
			newboard.InitialRegisterValues = Board.InitialRegisterValues;
			newboard.Configure(origin);
			newboard.Rom = Board.Rom;
			newboard.Vrom = Board.Vrom;
			if (Board.Wram != null)
				newboard.Wram = new byte[Board.Wram.Length];
			if (Board.Vram != null)
				newboard.Vram = new byte[Board.Vram.Length];
			newboard.PostConfigure();
			// the old board's sram must be restored
			if (newboard is FDS)
			{
				var newfds = newboard as FDS;
				var oldfds = Board as FDS;
				newfds.StoreSaveRam(oldfds.ReadSaveRam());
			}
			else if (Board.SaveRam != null)
			{
				Buffer.BlockCopy(Board.SaveRam, 0, newboard.SaveRam, 0, Board.SaveRam.Length);
			}

			Board = newboard;
			ppu.HasClockPPU = Board.GetType().GetMethod(nameof(INesBoard.ClockPpu)).DeclaringType != typeof(NesBoardBase);
		}


		static NES()
		{
			var highPriority = new List<Type>();
			var normalPriority = new List<Type>();

			//scan types in this assembly to find ones that implement boards to add them to the list
			foreach (Type type in typeof(NES).Assembly.GetTypes())
			{
				var attrs = type.GetCustomAttributes(typeof(NesBoardImplAttribute), true);
				if (attrs.Length == 0) continue;
				if (type.IsAbstract) continue;
				var cancelAttrs = type.GetCustomAttributes(typeof(NesBoardImplCancelAttribute), true);
				if (cancelAttrs.Length != 0) continue;
				var priorityAttrs = type.GetCustomAttributes(typeof(NesBoardImplPriorityAttribute), true);
				if (priorityAttrs.Length != 0)
					highPriority.Add(type);
				else normalPriority.Add(type);
			}

			INESBoardImplementors.AddRange(highPriority);
			INESBoardImplementors.AddRange(normalPriority);
		}

		/// <summary>
		/// finds a board class which can handle the provided cart
		/// </summary>
		static Type FindBoard(CartInfo cart, EDetectionOrigin origin, Dictionary<string, string> properties)
		{
			NES nes = new NES { cart = cart };
			Type ret = null;
			lock(INESBoardImplementors)
				foreach (var type in INESBoardImplementors)
				{
					NesBoardBase board = (NesBoardBase)Activator.CreateInstance(type);
					//unif demands that the boards set themselves up with expected legal values based on the board size
					//except, i guess, for the rom/chr sizes. go figure.
					//so, disable the asserts here
					if (origin == EDetectionOrigin.UNIF)
						board.DisableConfigAsserts = true;

					board.Create(nes);
					board.InitialRegisterValues = properties;
					if (board.Configure(origin))
					{
#if DEBUG
						if (ret != null)
							throw new Exception($"Boards {ret} and {type} both responded to {nameof(NESBoardBase.Configure)}!");
						ret = type;
#else
							return type;
#endif
					}
				}
			return ret;
		}

		/// <summary>
		/// looks up from the bootgod DB
		/// </summary>
		CartInfo IdentifyFromBootGodDB(IEnumerable<string> hash_sha1)
		{
			BootGodDB.Initialize();
			foreach (var hash in hash_sha1)
			{
				List<CartInfo> choices = BootGodDB.Instance.Identify(hash);
				//pick the first board for this hash arbitrarily. it probably doesn't make a difference
				if (choices.Count != 0)
					return choices[0];
			}
			return null;
		}

		/// <summary>
		/// looks up from the game DB
		/// </summary>
		CartInfo IdentifyFromGameDB(string hash)
		{
			var gi = Database.CheckDatabase(hash);
			if (gi == null) return null;

			CartInfo cart = new CartInfo();

			//try generating a bootgod cart descriptor from the game database
			var dict = gi.GetOptionsDict();
			cart.GameInfo = gi;
			if (!dict.ContainsKey("board"))
				throw new Exception("NES gamedb entries must have a board identifier!");
			cart.BoardType = dict["board"];
			if (dict.ContainsKey("system"))
				cart.System = dict["system"];
			cart.PrgSize = -1;
			cart.VramSize = -1;
			cart.WramSize = -1;
			cart.ChrSize = -1;
			if (dict.ContainsKey("PRG"))
				cart.PrgSize = short.Parse(dict["PRG"]);
			if (dict.ContainsKey("CHR"))
				cart.ChrSize = short.Parse(dict["CHR"]);
			if(dict.ContainsKey("VRAM"))
				cart.VramSize = short.Parse(dict["VRAM"]);
			if (dict.ContainsKey("WRAM"))
				cart.WramSize = short.Parse(dict["WRAM"]);
			if (dict.ContainsKey("PAD_H"))
				cart.PadH = byte.Parse(dict["PAD_H"]);
			if (dict.ContainsKey("PAD_V"))
				cart.PadV = byte.Parse(dict["PAD_V"]);
			if(dict.ContainsKey("MIR"))
				if (dict["MIR"] == "H")
				{
					cart.PadV = 1; cart.PadH = 0;
				}
				else if (dict["MIR"] == "V")
				{
					cart.PadH = 1; cart.PadV = 0;
				}
			if (dict.ContainsKey("BAD"))
				cart.Bad = true;
			if (dict.ContainsKey("MMC3"))
				cart.Chips.Add(dict["MMC3"]);
			if (dict.ContainsKey("PCB"))
				cart.Pcb = dict["PCB"];
			if (dict.ContainsKey("BATT"))
				cart.WramBattery = bool.Parse(dict["BATT"]);

			if(dict.ContainsKey("palette"))
			{
				cart.Palette = dict["palette"];
			}

			if (dict.ContainsKey("vs_security"))
			{
				cart.VsSecurity = byte.Parse(dict["vs_security"]);
			}

			return cart;
		}

		public class BootGodDB
		{
			static object staticsyncroot = new object();
			object syncroot = new object();

			bool validate = true;

			private static BootGodDB _Instance;
			public static BootGodDB Instance
			{
				get { lock (staticsyncroot) { return _Instance; } }
			}
			private static Func<byte[]> _GetDatabaseBytes;
			public static Func<byte[]> GetDatabaseBytes
			{
				set { lock (staticsyncroot) { _GetDatabaseBytes = value; } }
			}
			public static void Initialize()
			{
				lock (staticsyncroot)
				{
					if (_Instance == null)
						_Instance = new BootGodDB();
				}
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
				var xmlreader = XmlReader.Create(new MemoryStream(_GetDatabaseBytes()));
				CartInfo currCart = null;
				string currName = null;
				while (xmlreader.Read())
				{
					switch (state)
					{
						case 0:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "game")
							{
								currName = xmlreader.GetAttribute("name");
								state = 1;
							}
							break;
						case 2:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "board")
							{
								currCart.BoardType = xmlreader.GetAttribute("type");
								currCart.Pcb = xmlreader.GetAttribute("pcb");
								int mapper = int.Parse(xmlreader.GetAttribute("mapper"));
								if (validate && mapper > 255) throw new Exception("didnt expect mapper>255!");
								// we don't actually use this value at all; only the board name
								state = 3;
							}
							break;
						case 3:
							if (xmlreader.NodeType == XmlNodeType.Element)
							{
								switch(xmlreader.Name)
								{
									case "prg":
										currCart.PrgSize += (short)ParseSize(xmlreader.GetAttribute("size"));
										break;
									case "chr":
										currCart.ChrSize += (short)ParseSize(xmlreader.GetAttribute("size"));
										break;
									case "vram":
										currCart.VramSize += (short)ParseSize(xmlreader.GetAttribute("size"));
										break;
									case "wram":
										currCart.WramSize += (short)ParseSize(xmlreader.GetAttribute("size"));
										if (xmlreader.GetAttribute("battery") != null)
											currCart.WramBattery = true;
										break;
									case "pad":
										currCart.PadH = byte.Parse(xmlreader.GetAttribute("h"));
										currCart.PadV = byte.Parse(xmlreader.GetAttribute("v"));
										break;
									case "chip":
										currCart.Chips.Add(xmlreader.GetAttribute("type"));
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
								sha1_table[currCart.Sha1].Add(currCart);
								currCart = null;
								state = 5;
							}
							break;
						case 5:
						case 1:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "cartridge")
							{
								currCart = new CartInfo();
								currCart.System = xmlreader.GetAttribute("system");
								currCart.Sha1 = "sha1:" + xmlreader.GetAttribute("sha1");
								currCart.Name = currName;
								state = 2;
							}
							if (xmlreader.NodeType == XmlNodeType.EndElement && xmlreader.Name == "game")
							{
								currName = null;
								state = 0;
							}
							break;
					}
				} //end xmlreader loop
			}

			Bag<string, CartInfo> sha1_table = new Bag<string, CartInfo>();

			public List<CartInfo> Identify(string sha1)
			{
				lock (syncroot) return sha1_table[sha1];
			}
		}
	}
}
