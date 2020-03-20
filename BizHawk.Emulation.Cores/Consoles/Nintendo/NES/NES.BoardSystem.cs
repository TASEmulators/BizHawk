using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using BizHawk.Common;
using BizHawk.Emulation.Common;

//TODO - could stringpool the bootgod DB for a pedantic optimization

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	partial class NES
	{
		//this will be used to track classes that implement boards
		[AttributeUsage(AttributeTargets.Class)]
		public sealed class INESBoardImplAttribute : Attribute { }
		//this tracks derived boards that shouldn't be used by the implementation scanner
		[AttributeUsage(AttributeTargets.Class)]
		public sealed class INESBoardImplCancelAttribute : Attribute { }
		static List<Type> INESBoardImplementors = new List<Type>();
		//flags it as being priority, i.e. in the top of the list
		[AttributeUsage(AttributeTargets.Class)]
		public sealed class INESBoardImplPriorityAttribute : Attribute { }

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
				var attrs = type.GetCustomAttributes(typeof(INESBoardImplAttribute), true);
				if (attrs.Length == 0) continue;
				if (type.IsAbstract) continue;
				var cancelAttrs = type.GetCustomAttributes(typeof(INESBoardImplCancelAttribute), true);
				if (cancelAttrs.Length != 0) continue;
				var priorityAttrs = type.GetCustomAttributes(typeof(INESBoardImplPriorityAttribute), true);
				if (priorityAttrs.Length != 0)
					highPriority.Add(type);
				else normalPriority.Add(type);
			}

			INESBoardImplementors.AddRange(highPriority);
			INESBoardImplementors.AddRange(normalPriority);
		}

		/// <summary>
		/// All information necessary for a board to set itself up
		/// </summary>
		public class CartInfo
		{
			public GameInfo DB_GameInfo;
			public string name;

			public int trainer_size;
			public int chr_size;
			public int prg_size;
			public int wram_size, vram_size;
			public byte pad_h, pad_v;
			public bool wram_battery;
			public bool bad;
			/// <summary>in [0,3]; combination of bits 0 and 3 of flags6.  try not to use; will be null for bootgod-identified roms always</summary>
			public int? inesmirroring;

			public string board_type;
			public string pcb;

			public string sha1;
			public string system;
			public List<string> chips = new List<string>();

			public string palette; // Palette override for VS system
			public byte vs_security; // for VS system games that do a ppu dheck

			public override string ToString() => string.Join(",",
				$"pr={prg_size}",
				$"ch={chr_size}",
				$"wr={wram_size}",
				$"vr={vram_size}",
				$"ba={(wram_battery ? 1 : 0)}",
				$"pa={pad_h}|{pad_v}",
				$"brd={board_type}",
				$"sys={system}");
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
			cart.DB_GameInfo = gi;
			if (!dict.ContainsKey("board"))
				throw new Exception("NES gamedb entries must have a board identifier!");
			cart.board_type = dict["board"];
			if (dict.ContainsKey("system"))
				cart.system = dict["system"];
			cart.prg_size = -1;
			cart.vram_size = -1;
			cart.wram_size = -1;
			cart.chr_size = -1;
			if (dict.ContainsKey("PRG"))
				cart.prg_size = short.Parse(dict["PRG"]);
			if (dict.ContainsKey("CHR"))
				cart.chr_size = short.Parse(dict["CHR"]);
			if(dict.ContainsKey("VRAM"))
				cart.vram_size = short.Parse(dict["VRAM"]);
			if (dict.ContainsKey("WRAM"))
				cart.wram_size = short.Parse(dict["WRAM"]);
			if (dict.ContainsKey("PAD_H"))
				cart.pad_h = byte.Parse(dict["PAD_H"]);
			if (dict.ContainsKey("PAD_V"))
				cart.pad_v = byte.Parse(dict["PAD_V"]);
			if(dict.ContainsKey("MIR"))
				if (dict["MIR"] == "H")
				{
					cart.pad_v = 1; cart.pad_h = 0;
				}
				else if (dict["MIR"] == "V")
				{
					cart.pad_h = 1; cart.pad_v = 0;
				}
			if (dict.ContainsKey("BAD"))
				cart.bad = true;
			if (dict.ContainsKey("MMC3"))
				cart.chips.Add(dict["MMC3"]);
			if (dict.ContainsKey("PCB"))
				cart.pcb = dict["PCB"];
			if (dict.ContainsKey("BATT"))
				cart.wram_battery = bool.Parse(dict["BATT"]);

			if(dict.ContainsKey("palette"))
			{
				cart.palette = dict["palette"];
			}

			if (dict.ContainsKey("vs_security"))
			{
				cart.vs_security = byte.Parse(dict["vs_security"]);
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
								currCart.board_type = xmlreader.GetAttribute("type");
								currCart.pcb = xmlreader.GetAttribute("pcb");
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
								sha1_table[currCart.sha1].Add(currCart);
								currCart = null;
								state = 5;
							}
							break;
						case 5:
						case 1:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "cartridge")
							{
								currCart = new CartInfo();
								currCart.system = xmlreader.GetAttribute("system");
								currCart.sha1 = "sha1:" + xmlreader.GetAttribute("sha1");
								currCart.name = currName;
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

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class MapperPropAttribute : Attribute
	{
		public string Name { get; }

		public MapperPropAttribute(string name)
		{
			Name = name;
		}

		public MapperPropAttribute()
		{
			Name = null;
		}
	}

	public static class AutoMapperProps
	{
		public static void Populate(INesBoard board, NES.NESSyncSettings settings)
		{
			var fields = board.GetType().GetFields();
			foreach (var field in fields)
			{
				var attrib = field.GetCustomAttributes(typeof(MapperPropAttribute), false).OfType<MapperPropAttribute>().SingleOrDefault();
				if (attrib == null)
					continue;
				string Name = attrib.Name ?? field.Name;
				if (!settings.BoardProperties.ContainsKey(Name))
				{
					settings.BoardProperties.Add(Name, (string)Convert.ChangeType(field.GetValue(board), typeof(string)));
				}
			}
		}

		public static void Apply(INesBoard board)
		{
			var fields = board.GetType().GetFields();
			foreach (var field in fields)
			{
				var attribs = field.GetCustomAttributes(false);
				foreach (var attrib in attribs)
				{
					if (attrib is MapperPropAttribute)
					{
						string Name = ((MapperPropAttribute)attrib).Name ?? field.Name;

						if (board.InitialRegisterValues.TryGetValue(Name, out var Value))
						{
							try
							{
								field.SetValue(board, Convert.ChangeType(Value, field.FieldType));
							}
							catch (Exception e) when (e is InvalidCastException || e is FormatException || e is OverflowException)
							{
								throw new InvalidDataException("Auto Mapper Properties were in a bad format!", e);
							}
						}
						break;
					}
				}
			}
		}
	}
}
