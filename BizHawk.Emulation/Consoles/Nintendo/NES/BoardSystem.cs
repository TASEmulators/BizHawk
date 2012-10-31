using System;
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

//TODO - consider bytebuffer for mirroring
//TODO - could stringpool the bootgod DB for a pedantic optimization

namespace BizHawk.Emulation.Consoles.Nintendo
{
	partial class NES
	{
		public interface INESBoard : IDisposable
		{
			//base class pre-configuration
			void Create(NES nes);
			//one-time inherited classes configuration 
			bool Configure(NES.EDetectionOrigin origin);
			//one-time base class configuration (which can take advantage of any information setup by the more-informed Configure() method)
			void PostConfigure();
			
			//gets called once per PPU clock, for boards with complex behaviour which must be monitoring clock (i.e. mmc3 irq counter)
			void ClockPPU();
			//gets called once per CPU clock; typically for boards with M2 counters
			void ClockCPU();

			byte ReadPRG(int addr);
			byte ReadPPU(int addr); byte PeekPPU(int addr);
			void AddressPPU(int addr);
			byte ReadWRAM(int addr);
			byte ReadEXP(int addr);
			void WritePRG(int addr, byte value);
			void WritePPU(int addr, byte value);
			void WriteWRAM(int addr, byte value);
			void WriteEXP(int addr, byte value);
			void NESSoftReset();
			byte[] SaveRam { get; }
			byte[] WRAM { get; set; }
			byte[] VRAM { get; set; }
			byte[] ROM { get; set; }
			byte[] VROM { get; set; }
			void SyncState(Serializer ser);
			bool IRQSignal { get; }

			//mixes the board's custom audio into the supplied sample buffer
			void ApplyCustomAudio(short[] samples);
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

			public virtual void NESSoftReset()
			{

			}

			public abstract bool Configure(NES.EDetectionOrigin origin);
			public virtual void ClockPPU() { }
			public virtual void ClockCPU() { }

			public CartInfo Cart { get { return NES.cart; } }
			public NES NES { get; set; }

			//this is set to true when SyncState is called, so that we know the base class SyncState was used
			public bool SyncStateFlag = false;

			public virtual void SyncState(Serializer ser)
			{
				ser.Sync("vram", ref vram, true);
				ser.Sync("wram", ref wram, true);
				for (int i = 0; i < 4; i++) ser.Sync("mirroring" + i, ref mirroring[i]);
				ser.Sync("irq_signal", ref irq_signal);
				SyncStateFlag = true;
			}

			public virtual void SyncIRQ(bool flag)
			{
				IRQSignal = flag;
			}

			private bool irq_signal;
			public bool IRQSignal { get { return irq_signal; } set { irq_signal = value; } }

			public virtual void Dispose() { }

			int[] mirroring = new int[4];
			protected void SetMirroring(int a, int b, int c, int d)
			{
				mirroring[0] = a;
				mirroring[1] = b;
				mirroring[2] = c;
				mirroring[3] = d;
			}

			protected void ApplyMemoryMapMask(int mask, ByteBuffer map)
			{
				byte bmask = (byte)mask;
				for (int i = 0; i < map.len; i++)
					map[i] &= bmask;
			}

			//make sure you have bank-masked the map 
			protected int ApplyMemoryMap(int blockSizeBits, ByteBuffer map, int addr)
			{
				int bank = addr >> blockSizeBits;
				int ofs = addr & ((1 << blockSizeBits) - 1);
				bank = map[bank];
				addr = (bank << blockSizeBits) | ofs;
				return addr;
			}

			public static EMirrorType CalculateMirrorType(int pad_h, int pad_v)
			{
				if (pad_h == 0)
					if (pad_v == 0)
						return EMirrorType.OneScreenA;
					else return EMirrorType.Horizontal;
				else
					if (pad_v == 0)
						return EMirrorType.Vertical;
					else return EMirrorType.OneScreenB;
			}

			protected void SetMirrorType(int pad_h, int pad_v)
			{
				SetMirrorType(CalculateMirrorType(pad_h, pad_v));
			}

			public void SetMirrorType(EMirrorType mirrorType)
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

			protected int ApplyMirroring(int addr)
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
				//Debug.Assert(old_value == value, "Found a test case of bus conflict. please report.");
				//report: pinball quest (J). also: double dare
				return value;
			}

			public virtual byte ReadPRG(int addr) { return ROM[addr]; }
			public virtual void WritePRG(int addr, byte value) { }

			public virtual void WriteWRAM(int addr, byte value)
			{
				if(wram != null)
					wram[addr] = value;
			}

			private int wram_mask;
			public virtual void PostConfigure()
			{
				wram_mask = (Cart.wram_size * 1024) - 1;
			}

			public virtual byte ReadWRAM(int addr) {
				if (wram != null)
					return wram[addr & wram_mask];
				else return NES.DB;
			}

			public virtual void WriteEXP(int addr, byte value) { }
			public virtual byte ReadEXP(int addr) { 
				return NES.DB;
			}

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

			public virtual void AddressPPU(int addr) { }
			public virtual byte PeekPPU(int addr) { return ReadPPU(addr); }

			protected virtual byte ReadPPUChr(int addr)
			{
				if (VROM != null)
					return VROM[addr];
				else return VRAM[addr];
			}

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

			public virtual byte[] SaveRam
			{
				get
				{
					if (!Cart.wram_battery) return null;
					return WRAM;
				}
			}

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
				if (DisableConfigAsserts) return;
				foreach (int i in valid) if (value == i) return;
				Assert(false, "unhandled {0} size of {1}", name,value);
			}
			protected void AssertBattery(bool has_bat) { Assert(Cart.wram_battery == has_bat); }

			public virtual void ApplyCustomAudio(short[] samples) { }

			public bool DisableConfigAsserts = false;
		}

		//this will be used to track classes that implement boards
		[AttributeUsage(AttributeTargets.Class)]
		public class INESBoardImplAttribute : Attribute { }
		//this tracks derived boards that shouldnt be used by the implementation scanner
		[AttributeUsage(AttributeTargets.Class)]
		public class INESBoardImplCancelAttribute : Attribute { }
		static List<Type> INESBoardImplementors = new List<Type>();
		//flags it as being priority, i.e. in the top of the list
		[AttributeUsage(AttributeTargets.Class)]
		public class INESBoardImplPriorityAttribute : Attribute { }

		static INESBoard CreateBoardInstance(Type boardType)
		{
			var board = (INESBoard)Activator.CreateInstance(boardType);
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
			public NESGameInfo game;
			public BizHawk.GameInfo DB_GameInfo;

			public short chr_size;
			public short prg_size;
			public short wram_size, vram_size;
			public byte pad_h, pad_v, mapper;
			public bool wram_battery;
			public bool bad;
			/// <summary>in [0,3]; combination of bits 0 and 3 of flags6.  try not to use; will be null for bootgod-identified roms always</summary>
			public int? inesmirroring;

			public string board_type;
			public string pcb;

			public string sha1;
			public string system;
			public List<string> chips = new List<string>();

			public override string ToString()
			{
				return string.Format("map={0},pr={1},ch={2},wr={3},vr={4},ba={5},pa={6}|{7},brd={8},sys={9}", mapper, prg_size, chr_size, wram_size, vram_size, wram_battery ? 1 : 0, pad_h, pad_v, board_type, system);
			}
		}

		/// <summary>
		/// Logical game information. May exist in form of several carts (different revisions)
		/// </summary>
		public class NESGameInfo
		{
			public string name;
			public List<CartInfo> carts = new List<CartInfo>();
		}

		/// <summary>
		/// finds a board class which can handle the provided cart
		/// </summary>
		static Type FindBoard(CartInfo cart, EDetectionOrigin origin)
		{
			NES nes = new NES();
			nes.cart = cart;
			lock(INESBoardImplementors)
				foreach (var type in INESBoardImplementors)
				{
					using (NESBoardBase board = (NESBoardBase)Activator.CreateInstance(type))
					{
						//unif demands that the boards set themselves up with expected legal values based on the board size
						//except, i guess, for the rom/chr sizes. go figure.
						//so, disable the asserts here
						if (origin == EDetectionOrigin.UNIF)
							board.DisableConfigAsserts = true;

						board.Create(nes);
						if (board.Configure(origin))
						{
							return type;
						}
					}
				}
			return null;
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

			NESGameInfo game = new NESGameInfo();
			CartInfo cart = new CartInfo();
			game.carts.Add(cart);

			//try generating a bootgod cart descriptor from the game database
			var dict = gi.GetOptionsDict();
			game.name = gi.Name;
			cart.DB_GameInfo = gi;
			cart.game = game;
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
				NESGameInfo currGame = null;
				CartInfo currCart = null;
				while (xmlreader.Read())
				{
					switch (state)
					{
						case 0:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "game")
							{
								currGame = new NESGameInfo();
								currGame.name = xmlreader.GetAttribute("name");
								state = 1;
							}
							break;
						case 2:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "board")
							{
								currCart.board_type = xmlreader.GetAttribute("type");
								currCart.pcb = xmlreader.GetAttribute("pcb");
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
						case 1:
							if (xmlreader.NodeType == XmlNodeType.Element && xmlreader.Name == "cartridge")
							{
								currCart = new CartInfo();
								currCart.game = currGame;
								currCart.system = xmlreader.GetAttribute("system");
								currCart.sha1 = "sha1:" + xmlreader.GetAttribute("sha1");
								state = 2;
							}
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
				foreach (NESGameInfo game in games)
				{
					foreach (CartInfo cart in game.carts)
					{
						sha1_table[cart.sha1].Add(cart);
					}
				}
			}


			List<NESGameInfo> games = new List<NESGameInfo>(); //maybe we dont need to track this
			Bag<string, CartInfo> sha1_table = new Bag<string, CartInfo>();

			public List<CartInfo> Identify(string sha1)
			{
				if (!sha1_table.ContainsKey(sha1)) return new List<CartInfo>();
				return sha1_table[sha1];
			}
		}
	}
}
