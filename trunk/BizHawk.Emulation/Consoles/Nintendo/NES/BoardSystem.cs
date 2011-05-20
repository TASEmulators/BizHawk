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
		public interface INESBoard : IDisposable
		{
			void Create(NES nes);
			bool Configure(NES.EDetectionOrigin origin);
			byte ReadPRG(int addr);
			byte ReadPPU(int addr); byte PeekPPU(int addr);
			byte ReadWRAM(int addr);
			byte ReadEXP(int addr);
			void WritePRG(int addr, byte value);
			void WritePPU(int addr, byte value);
			void WriteWRAM(int addr, byte value);
			void WriteEXP(int addr, byte value);
			byte[] SaveRam { get; }
			byte[] WRAM { get; set; }
			byte[] VRAM { get; set; }
			byte[] ROM { get; set; }
			byte[] VROM { get; set; }
			void SyncState(Serializer ser);
		};


		[INESBoardImpl]
		public abstract class NESBoardBase : INESBoard
		{
			/// <summary>
			/// These are used by SetMirroring() to provide the base class nametable mirroring service.
			/// Apparently, these are not used for internal build configuration logics
			/// </summary>
			public enum EMirrorType
			{
				Vertical, Horizontal,
				OneScreenA, OneScreenB,
			}

			public virtual void Create(NES nes)
			{
				this.NES = nes;
			}
			public abstract bool Configure(NES.EDetectionOrigin origin);

			public CartInfo Cart { get { return NES.cart; } }
			public NES NES { get; set; }

			public virtual void SyncState(Serializer ser)
			{
				ser.Sync("vram", ref vram, true);
				ser.Sync("wram", ref wram, true);
				for (int i = 0; i < 4; i++) ser.Sync("mirroring" + i, ref mirroring[i]);
			}

			public virtual void Dispose() { }

			int[] mirroring = new int[4];
			protected void SetMirroring(int a, int b, int c, int d)
			{
				mirroring[0] = a;
				mirroring[1] = b;
				mirroring[2] = c;
				mirroring[3] = d;
			}

			protected void SetMirrorType(int pad_h, int pad_v)
			{
				if (pad_h == 0)
					if (pad_v == 0)
						SetMirrorType(EMirrorType.OneScreenA);
					else SetMirrorType(EMirrorType.Horizontal);
				else
					if (pad_v == 0)
						SetMirrorType(EMirrorType.Vertical);
					else SetMirrorType(EMirrorType.OneScreenB);
			}

			protected void SetMirrorType(EMirrorType mirrorType)
			{
				switch (mirrorType)
				{
					case EMirrorType.Horizontal: SetMirroring(0, 0, 1, 1); break;
					case EMirrorType.Vertical: SetMirroring(0, 1, 0, 1); break;
					case EMirrorType.OneScreenA: SetMirroring(0, 0, 0, 0); break;
					case EMirrorType.OneScreenB: SetMirroring(1, 1, 1, 1); break;
					default: SetMirroring(-1, -1, -1, -1); break; //crash!
				}
			}

			int ApplyMirroring(int addr)
			{
				int block = (addr >> 10) & 3;
				block = mirroring[block];
				int ofs = addr & 0x3FF;
				return (block << 10) | ofs;
			}

			protected byte HandleNormalPRGConflict(int addr, byte value)
			{
				byte old_value = value;
				value &= ReadPRG(addr);
				Debug.Assert(old_value == value, "Found a test case of bus conflict. please report.");
				return value;
			}

			public virtual byte ReadPRG(int addr) { return ROM[addr]; }
			public virtual void WritePRG(int addr, byte value) { }

			public virtual void WriteWRAM(int addr, byte value)
			{
				if(wram != null)
					wram[addr] = value;
			}
			public virtual byte ReadWRAM(int addr) {
				if (wram != null)
					return wram[addr];
				else return 0xFF;
			}

			public virtual void WriteEXP(int addr, byte value) { }
			public virtual byte ReadEXP(int addr) { return 0xFF; }

			public virtual void WritePPU(int addr, byte value)
			{
				if (addr < 0x2000)
				{
					if (VRAM != null)
						VRAM[addr] = value;
				}
				else
				{
					NES.CIRAM[ApplyMirroring(addr)] = value;
				}
			}

			public virtual byte PeekPPU(int addr) { return ReadPPU(addr); }

			public virtual byte ReadPPU(int addr)
			{
				if (addr < 0x2000)
				{
					if (VROM != null)
						return VROM[addr];
					else return VRAM[addr];
				}
				else
				{
					return NES.CIRAM[ApplyMirroring(addr)];
				}
			}

			public virtual byte[] SaveRam { get { return null; } }
			public byte[] WRAM { get { return wram; } set { wram = value; } }
			public byte[] VRAM { get { return vram; } set { vram = value; } }
			public byte[] ROM { get; set; }
			public byte[] VROM { get; set; }
			byte[] wram, vram;

			protected void Assert(bool test, string comment, params object[] args)
			{
				if (!test) throw new Exception(string.Format(comment, args));
			}
			protected void Assert(bool test)
			{
				if (!test) throw new Exception("assertion failed in board setup!");
			}
			protected void AssertPrg(params int[] prg) { Assert_memtype(Cart.prg_size, "prg", prg); }
			protected void AssertChr(params int[] chr) { Assert_memtype(Cart.chr_size, "chr", chr); }
			protected void AssertWram(params int[] wram) { Assert_memtype(Cart.wram_size, "wram", wram); }
			protected void AssertVram(params int[] vram) { Assert_memtype(Cart.vram_size, "vram", vram); }
			protected void Assert_memtype(int value, string name, int[] valid)
			{
				foreach (int i in valid) if (value == i) return;
				Assert(false, "unhandled {0} size", name);
			}
			protected void AssertBattery(bool has_bat) { Assert(Cart.wram_battery == has_bat); }
		}

		//this will be used to track classes that implement boards
		[AttributeUsage(AttributeTargets.Class)]
		public class INESBoardImplAttribute : Attribute { }
		static List<Type> INESBoardImplementors = new List<Type>();

		static NES()
		{
			//scan types in this assembly to find ones that implement boards to add them to the list
			foreach (Type type in typeof(NES).Assembly.GetTypes())
			{
				var attrs = type.GetCustomAttributes(typeof(INESBoardImplAttribute), true);
				if (attrs.Length == 0) continue;
				if (type.IsAbstract) continue;
				INESBoardImplementors.Add(type);
			}
		}

		/// <summary>
		/// All information necessary for a board to set itself up
		/// </summary>
		public class CartInfo
		{
			public GameInfo game;

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
				return string.Format("pr={0},ch={1},wr={2},vr={3},ba={4},pa={5}|{6},brd={7},map={8},sys={9}", prg_size, chr_size, wram_size, vram_size, wram_battery?1:0, pad_h, pad_v, board_type, mapper, system);
			}
		}

		/// <summary>
		/// Logical game information. May exist in form of several carts (different revisions)
		/// </summary>
		public class GameInfo
		{
			public string name;
			public List<CartInfo> carts = new List<CartInfo>();
			public override string ToString()
			{
				return string.Format("name={0}", name);
			}
		}

		/// <summary>
		/// finds a board class which can handle the provided cart
		/// </summary>
		static Type FindBoard(CartInfo cart, EDetectionOrigin origin)
		{
			NES nes = new NES();
			nes.cart = cart;
			foreach (var type in INESBoardImplementors)
			{
				INESBoard board = (INESBoard)Activator.CreateInstance(type);
				board.Create(nes);
				if (board.Configure(origin))
					return type;
			}
			return null;
		}

		/// <summary>
		/// looks up from the bootgod DB
		/// </summary>
		CartInfo IdentifyFromBootGodDB(string hash_sha1)
		{
			BootGodDB.Initialize();
			List<CartInfo> choices = BootGodDB.Instance.Identify(hash_sha1);
			if (choices.Count == 0) return null;

			//pick the first board for this hash arbitrarily. it probably doesn't make a difference
			return choices[0];
		}

		/// <summary>
		/// looks up from the game DB
		/// </summary>
		CartInfo IdentifyFromGameDB(string hash)
		{
			var gi = Database.CheckDatabase(hash);
			if (gi == null) return null;

			GameInfo game = new GameInfo();
			CartInfo cart = new CartInfo();
			game.carts.Add(cart);

			//try generating a bootgod cart descriptor from the game database
			var dict = gi.ParseOptionsDictionary();
			game.name = gi.Name;
			cart.game = game;
			cart.board_type = dict["board"];
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
				cart.pad_h = byte.Parse(dict["PAD_V"]);
			if (dict.ContainsKey("bad"))
				Console.WriteLine("rom is flagged as BAD!");

			return cart;
		}

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
				GameInfo currGame = null;
				CartInfo currCart = null;
				while (xmlreader.Read())
				{
					switch (state)
					{
						case 0:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "game")
							{
								currGame = new GameInfo();
								currGame.name = xmlreader.GetAttribute("name");
								state = 1;
							}
							break;
						case 1:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "cartridge")
							{
								currCart = new CartInfo();
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
				foreach (GameInfo game in games)
				{
					foreach (CartInfo cart in game.carts)
					{
						sha1_table[cart.sha1].Add(cart);
					}
				}
			}


			List<GameInfo> games = new List<GameInfo>(); //maybe we dont need to track this
			Bag<string, CartInfo> sha1_table = new Bag<string, CartInfo>();

			public List<CartInfo> Identify(string sha1)
			{
				if (!sha1_table.ContainsKey(sha1)) return new List<CartInfo>();
				return sha1_table[sha1];
			}
		}
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