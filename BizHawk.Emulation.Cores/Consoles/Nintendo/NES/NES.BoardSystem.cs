using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

//TODO - consider bytebuffer for mirroring
//TODO - could stringpool the bootgod DB for a pedantic optimization

namespace BizHawk.Emulation.Cores.Nintendo.NES
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

			byte PeekCart(int addr);

			byte ReadPRG(int addr);
			byte ReadPPU(int addr); byte PeekPPU(int addr);
			void AddressPPU(int addr);
			byte ReadWRAM(int addr);
			byte ReadEXP(int addr);
			byte ReadReg2xxx(int addr);
			byte PeekReg2xxx(int addr);
			void WritePRG(int addr, byte value);
			void WritePPU(int addr, byte value);
			void WriteWRAM(int addr, byte value);
			void WriteEXP(int addr, byte value);
			void WriteReg2xxx(int addr, byte value);
			void NESSoftReset();
			void AtVsyncNMI();
			byte[] SaveRam { get; }
			byte[] WRAM { get; set; }
			byte[] VRAM { get; set; }
			byte[] ROM { get; set; }
			byte[] VROM { get; set; }
			void SyncState(Serializer ser);
			bool IRQSignal { get; }

			//mixes the board's custom audio into the supplied sample buffer
			void ApplyCustomAudio(short[] samples);

			Dictionary<string, string> InitialRegisterValues { get; set; }
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

			Dictionary<string, string> _initialRegisterValues = new Dictionary<string, string>();
			public Dictionary<string, string> InitialRegisterValues { get { return _initialRegisterValues; } set { _initialRegisterValues = value; } }

			public abstract bool Configure(NES.EDetectionOrigin origin);
			public virtual void ClockPPU() { }
			public virtual void ClockCPU() { }
			public virtual void AtVsyncNMI() { }

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
				for (int i = 0; i < map.Len; i++)
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
					wram[addr & wram_mask] = value;
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

			public virtual byte ReadReg2xxx(int addr)
			{
				return NES.ppu.ReadReg(addr & 7);
			}

			public virtual byte PeekReg2xxx(int addr)
			{
				return NES.ppu.PeekReg(addr & 7);
			}

			public virtual void WriteReg2xxx(int addr, byte value)
			{
				NES.ppu.WriteReg(addr & 7, value);
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

			/// <summary>
			/// derived classes should override this if they have peek-unsafe logic
			/// </summary>
			public virtual byte PeekCart(int addr)
			{
				byte ret;
				if (addr >= 0x8000)
				{
					ret = ReadPRG(addr - 0x8000); //easy optimization, since rom reads are so common, move this up (reordering the rest of these elseifs is not easy)
				}
				else if (addr < 0x6000)
				{
					ret = ReadEXP(addr - 0x4000);
				}
				else
				{
					ret = ReadWRAM(addr - 0x6000);
				}

				return ret;
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
				// only disable vram and wram asserts, as UNIF knows its prg and chr sizes
				if (DisableConfigAsserts && (name == "wram" || name == "vram")) return;
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

		public string BoardName { get { return Board.GetType().Name; } }

		void BoardSystemHardReset()
		{
			INESBoard newboard;
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
			newboard.ROM = Board.ROM;
			newboard.VROM = Board.VROM;
			if (Board.WRAM != null)
				newboard.WRAM = new byte[Board.WRAM.Length];
			if (Board.VRAM != null)
				newboard.VRAM = new byte[Board.VRAM.Length];
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
			Board.Dispose();
			Board = newboard;
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

			public override string ToString()
			{
				return string.Format("pr={1},ch={2},wr={3},vr={4},ba={5},pa={6}|{7},brd={8},sys={9}", board_type, prg_size, chr_size, wram_size, vram_size, wram_battery ? 1 : 0, pad_h, pad_v, board_type, system);
			}
		}

		/// <summary>
		/// finds a board class which can handle the provided cart
		/// </summary>
		static Type FindBoard(CartInfo cart, EDetectionOrigin origin, Dictionary<string, string> properties)
		{
			NES nes = new NES();
			nes.cart = cart;
			Type ret = null;
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
						board.InitialRegisterValues = properties;
						if (board.Configure(origin))
						{
#if DEBUG
							if (ret != null)
								throw new Exception(string.Format("Boards {0} and {1} both responded to Configure!", ret, type));
							else
								ret = type;
#else
							return type;
#endif
						}
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
				lock (syncroot)
				{
					if (!sha1_table.ContainsKey(sha1))
						return new List<CartInfo>();
					else
						return sha1_table[sha1];
				}
			}
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class MapperPropAttribute : Attribute
	{
		public string Name { get; private set; }
		public MapperPropAttribute(string Name)
		{
			this.Name = Name;
		}
		public MapperPropAttribute()
		{
			this.Name = null;
		}
	}

	public static class AutoMapperProps
	{
		public static void Populate(NES.INESBoard board, NES.NESSyncSettings settings)
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

		public static void Apply(NES.INESBoard board)
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

						string Value;
						if (board.InitialRegisterValues.TryGetValue(Name, out Value))
						{
							try
							{
								field.SetValue(board, Convert.ChangeType(Value, field.FieldType));
							}
							catch (Exception e)
							{
								if (e is InvalidCastException || e is FormatException || e is OverflowException)
									throw new InvalidDataException("Auto Mapper Properties were in a bad format!", e);
								else
									throw;
							}
						}
						break;
					}
				}
			}
		}
	}
}
