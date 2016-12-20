using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;
using BizHawk.Common;
using System.Reflection;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	/*
	 * This file covers all NES and Famicom controller related stuff.
	 * It supports (or could be easily made to support by adding a new class) every existing
	 * controller device I know about.  It does not support some things that were theoretically
	 * possible with the electronic interface available, but never used.
	 */

	#region interfaces and such

	/// <summary>
	/// stores information about the strobe lines controlled by $4016
	/// </summary>
	public struct StrobeInfo
	{
		/// <summary>
		/// the current value of $4016.0; strobes regular controller ports
		/// </summary>
		public readonly int OUT0;
		/// <summary>
		/// the current value of $4016.1; strobes expansion port
		/// </summary>
		public readonly int OUT1;
		/// <summary>
		/// the current value of $4016.2; strobes expansion port
		/// </summary>
		public readonly int OUT2;
		/// <summary>
		/// the previous value or $4016.0 (for edge sensitive equipment)
		/// </summary>
		public readonly int OUT0old;
		/// <summary>
		/// the previous value or $4016.1 (for edge sensitive equipment)
		/// </summary>
		public readonly int OUT1old;
		/// <summary>
		/// the previous value or $4016.2 (for edge sensitive equipment)
		/// </summary>
		public readonly int OUT2old;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="oldvalue">the old latched $4016 byte</param>
		/// <param name="newvalue">the new latched $4016 byte</param>
		public StrobeInfo(byte oldvalue, byte newvalue)
		{
			OUT0old = oldvalue & 1;
			OUT1old = oldvalue >> 1 & 1;
			OUT2old = oldvalue >> 2 & 1;
			OUT0 = newvalue & 1;
			OUT1 = newvalue >> 1 & 1;
			OUT2 = newvalue >> 2 & 1;
		}
	}

	/// <summary>
	/// the main system deck, handling all $4016 writes and $4016/$4017 reads
	/// </summary>
	public interface IControllerDeck
	{
		/// <summary>
		/// call whenever $4016 is written
		/// </summary>
		/// <param name="s"></param>
		/// <param name="c"></param>
		void Strobe(StrobeInfo s, IController c);
		/// <summary>
		/// call whenever $4016 is read
		/// </summary>
		/// <param name="c"></param>
		/// <returns>bits 0-4 are valid</returns>
		byte ReadA(IController c); // D0:D4
		/// <summary>
		/// call whenever $4017 is read
		/// </summary>
		/// <param name="c"></param>
		/// <returns>bits 0-4 are valid</returns>
		byte ReadB(IController c); // D0:D4
		ControllerDefinition GetDefinition();
		void SyncState(Serializer ser);
	}

	/// <summary>
	/// a peripheral that plugs into the famicom expansion port
	/// </summary>
	public interface IFamicomExpansion
	{
		void Strobe(StrobeInfo s, IController c);
		/// <summary>
		/// read data from $4016
		/// </summary>
		/// <param name="c"></param>
		/// <returns>only bit 1 is valid</returns>
		byte ReadA(IController c);
		/// <summary>
		/// read data from $4017
		/// </summary>
		/// <param name="c"></param>
		/// <returns>bits 1-4 are valid</returns>
		byte ReadB(IController c);
		ControllerDefinition GetDefinition();
		void SyncState(Serializer ser);
	}

	/// <summary>
	/// a peripheral that plugs into either of the two NES controller ports
	/// </summary>
	public interface INesPort
	{
		void Strobe(StrobeInfo s, IController c); // only uses OUT0
		byte Read(IController c); // only uses D0, D3, D4
		ControllerDefinition GetDefinition();
		void SyncState(Serializer ser);
	}

	#endregion

	/// <summary>
	/// a NES or AV famicom, with two attached devices
	/// </summary>
	public class NesDeck : IControllerDeck
	{
		INesPort Left;
		INesPort Right;
		ControlDefUnMerger LeftU;
		ControlDefUnMerger RightU;
		ControllerDefinition Definition;

		public NesDeck(INesPort Left, INesPort Right, Func<int, int, bool> PPUCallback)
		{
			this.Left = Left;
			this.Right = Right;
			List<ControlDefUnMerger> cdum;
			Definition = ControllerDefMerger.GetMerged(new[] { Left.GetDefinition(), Right.GetDefinition() }, out cdum);
			LeftU = cdum[0];
			RightU = cdum[1];

			// apply hacks
			// if this list gets very long, then something should be changed
			// if it stays short, then no problem
			if (Left is FourScore)
				(Left as FourScore).RightPort = false;
			if (Right is FourScore)
				(Right as FourScore).RightPort = true;
			if (Left is IZapper)
				(Left as IZapper).PPUCallback = PPUCallback;
			if (Right is IZapper)
				(Right as IZapper).PPUCallback = PPUCallback;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			Left.Strobe(s, LeftU.UnMerge(c));
			Right.Strobe(s, RightU.UnMerge(c));
		}

		public byte ReadA(IController c)
		{
			return (byte)(Left.Read(LeftU.UnMerge(c)) & 0x19);
		}

		public byte ReadB(IController c)
		{
			return (byte)(Right.Read(RightU.UnMerge(c)) & 0x19);
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Left");
			Left.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Right");
			Right.SyncState(ser);
			ser.EndSection();
		}
	}

	public class UnpluggedNES : INesPort
	{
		public void Strobe(StrobeInfo s, IController c)
		{
		}

		public byte Read(IController c)
		{
			return 0;
		}

		public ControllerDefinition GetDefinition()
		{
			return new ControllerDefinition();
		}

		public void SyncState(Serializer ser)
		{
		}
	}

	/// <summary>
	/// a NES controller; also used internally to represent the two famicom controllers
	/// </summary>
	public class ControllerNES : INesPort
	{
		bool resetting = false;
		int latchedvalue = 0;

		static string[] Buttons =
		{
			"0A", "0B", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right"
		};
		static string[] FamicomP2Buttons =
		{
			"0A", "0B", null, null, "0Up", "0Down", "0Left", "0Right"
		};

		bool FamicomP2Hack;

		ControllerDefinition Definition;

		public ControllerNES()
		{
			Definition = new ControllerDefinition
			{
				BoolButtons = Buttons
					.OrderBy(x => ButtonOrdinals[x])
					.ToList()
			};
		}


		Dictionary<string, int> ButtonOrdinals = new Dictionary<string, int>
		{
			{ "0Up", 1 },
			{ "0Down", 2 },
			{ "0Left", 3 },
			{ "0Right", 4 },
			{ "0Start", 5 },
			{ "0Select", 6 },
			{ "0B", 7 },
			{ "0A", 8 },
		};

		public ControllerNES(bool famicomP2)
		{
			if (famicomP2)
			{
				Definition = new ControllerDefinition
				{
					BoolButtons = FamicomP2Buttons
					.Where((s) => s != null)
					.OrderBy(x => ButtonOrdinals[x])
					.ToList()
				};
			}
			else
			{
				Definition = new ControllerDefinition
				{
					BoolButtons = Buttons
					.OrderBy(x => ButtonOrdinals[x])
					.ToList()
				};
			}

			FamicomP2Hack = famicomP2;
		}

		// reset is not edge triggered; so long as it's high, the latch is continuously reloading
		// so we need to latch in two places:
		// 1. when OUT0 goes low, to get the last set
		// 2. wheneven reading with OUT0 high, since new data for controller is always loading

		void Latch(IController c)
		{
			latchedvalue = SerialUtil.Latch(FamicomP2Hack ? FamicomP2Buttons : Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (resetting)
				Latch(c);
			byte ret = (byte)(latchedvalue & 1);
			if (!resetting)
				latchedvalue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("restting", ref resetting);
			ser.Sync("latchedvalue", ref latchedvalue);
		}
	}

	/// <summary>
	/// a SNES controller plugged into a NES? heresy
	/// </summary>
	public class ControllerSNES : INesPort
	{
		bool resetting = false;
		int latchedvalue = 0;

		static readonly string[] Buttons =
		{
			"0B", "0Y", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right",
			"0A", "0X", "0L", "0R", null, null, null, null // 4 0s at end
		};

		ControllerDefinition Definition;

		public ControllerSNES()
		{
			Definition = new ControllerDefinition
			{
				BoolButtons = Buttons.Where(s => s != null).ToList()
			};
		}

		// reset is not edge triggered; so long as it's high, the latch is continuously reloading
		// so we need to latch in two places:
		// 1. when OUT0 goes low, to get the last set
		// 2. wheneven reading with OUT0 high, since new data for controller is always loading

		void Latch(IController c)
		{
			latchedvalue = SerialUtil.Latch(Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (resetting)
				Latch(c);
			byte ret = (byte)(latchedvalue & 1);
			if (!resetting)
				latchedvalue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("restting", ref resetting);
			ser.Sync("latchedvalue", ref latchedvalue);
		}
	}

	/// <summary>
	/// vaus paddle, the NES (not famicom) version
	/// </summary>
	public class ArkanoidNES : INesPort
	{
		int shiftidx = 0;
		bool resetting = false;
		byte latchedvalue = 0x54 ^ 0xff;

		static ControllerDefinition Definition = new ControllerDefinition
		{
			BoolButtons = { "0Fire" },
			FloatControls = { "0Paddle" },
			FloatRanges = { new[] { 0.0f, 80.0f, 160.0f } }
		};

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (resetting)
				shiftidx = 0;
			if (s.OUT0 > s.OUT0old)
			{
				latchedvalue = (byte)(0x54 + (int)c.GetFloat("0Paddle"));
				latchedvalue ^= 0xff;
			}
		}

		public byte Read(IController c)
		{
			byte ret = c.IsPressed("0Fire") ? (byte)0x08 : (byte)0x00;
			if (resetting)
				return ret;

			byte value = latchedvalue;
			value <<= shiftidx;
			ret |= (byte)(value >> 3 & 0x10);
			shiftidx++;
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("shiftidx", ref shiftidx);
			ser.Sync("restting", ref resetting);
			ser.Sync("latchedvalue", ref latchedvalue);
		}
	}

	public class FourScore : INesPort
	{
		// fourscore is actually one two port thing
		// we emulate it as two separate halves
		// each one behaves slightly differently
		public bool RightPort = false;

		static string[] Buttons =
		{
			"0A", "0B", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right",
			"1A", "1B", "1Select", "1Start", "1Up", "1Down", "1Left", "1Right",
		};
		static ControllerDefinition Definition = new ControllerDefinition { BoolButtons = new List<string>(Buttons) };

		bool resetting = false;
		int latchedvalue = 0;

		void Latch(IController c)
		{
			latchedvalue = SerialUtil.Latch(Buttons, c);
			// set signatures
			latchedvalue &= ~0xff0000;
			if (RightPort) // signatures
				latchedvalue |= 0x040000;
			else
				latchedvalue |= 0x080000;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (resetting)
				Latch(c);
			byte ret = (byte)(latchedvalue & 1);
			if (!resetting)
				latchedvalue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("restting", ref resetting);
			ser.Sync("latchedvalue", ref latchedvalue);
		}
	}

	public class PowerPad : INesPort
	{
		static string[] D3Buttons = { "0PP2", "0PP1", "0PP5", "0PP9", "0PP6", "0PP10", "0PP11", "0PP7" };
		static string[] D4Buttons = { "0PP4", "0PP3", "0PP12", "0PP8" };
		static ControllerDefinition Definition = new ControllerDefinition { BoolButtons = new List<string>(D3Buttons.Concat(D4Buttons)) };

		bool resetting = false;
		int latched3 = 0;
		int latched4 = 0;

		void Latch(IController c)
		{
			latched3 = SerialUtil.Latch(D3Buttons, c);
			latched4 = SerialUtil.Latch(D4Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte Read(IController c)
		{
			if (resetting)
				Latch(c);
			int d3 = latched3 & 1;
			int d4 = latched4 & 1;
			if (!resetting)
			{
				latched3 >>= 1; // ASR not LSR, so endless stream of 1s after data
				latched4 >>= 1;
			}
			return (byte)(d3 << 3 | d4 << 4);
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("restting", ref resetting);
			ser.Sync("latched3", ref latched3);
			ser.Sync("latched4", ref latched4);
		}
	}

	// Dummy interface to indicate zapper behavior, used as a means of type checking for zapper functionality
	public interface IZapper
	{
		Func<int, int, bool> PPUCallback { get; set; }
	}

	public class Zapper : INesPort, IFamicomExpansion, IZapper
	{
		/// <summary>
		/// returns true if light was detected at the ppu coordinates specified
		/// </summary>
		public Func<int, int, bool> PPUCallback { get; set; }

		static ControllerDefinition Definition = new ControllerDefinition
		{
			BoolButtons = { "0Fire" },
			FloatControls = { "0Zapper X", "0Zapper Y" },
			FloatRanges = { new[] { 0.0f, 128.0f, 255.0f }, new[] { 0.0f, 120.0f, 239.0f } }
		};

		public void Strobe(StrobeInfo s, IController c)
		{
		}

		// NES controller port interface
		public byte Read(IController c)
		{
			byte ret = 0;
			if (c.IsPressed("0Fire"))
				ret |= 0x10;
			if (!PPUCallback((int)c.GetFloat("0Zapper X"), (int)c.GetFloat("0Zapper Y")))
				ret |= 0x08;
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
		}

		// famicom expansion hookups
		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			return Read(c);
		}
	}

	public class VSZapper : INesPort, IZapper
	{
		/// <summary>
		/// returns true if light was detected at the ppu coordinates specified
		/// </summary
		public Func<int, int, bool> PPUCallback { get; set; }

		bool resetting = false;
		uint latchedvalue = 0;

		static ControllerDefinition Definition = new ControllerDefinition
		{
			BoolButtons = { "0Fire" },
			FloatControls = { "0Zapper X", "0Zapper Y" },
			FloatRanges = { new[] { 0.0f, 128.0f, 255.0f }, new[] { 0.0f, 120.0f, 239.0f } }
		};

		void Latch(IController c)
		{
			byte ret = 0;
			if (c.IsPressed("0Fire"))
				ret |= 0x80;
			if (PPUCallback((int)c.GetFloat("0Zapper X"), (int)c.GetFloat("0Zapper Y")))
				ret |= 0x40;

			ret |= 0x10; // always 1
			latchedvalue = ret;
			latchedvalue |= 0xFFFFFF00;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		// NES controller port interface
		public byte Read(IController c)
		{
			if (resetting)
				Latch(c);
			byte ret = (byte)(latchedvalue & 1);
			if (!resetting)
				latchedvalue >>= 1; // ASR not LSR, so endless stream of 1s after data
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("restting", ref resetting);
			ser.Sync("latchedvalue", ref latchedvalue);
		}

		// famicom expansion hookups
		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			return Read(c);
		}
	}

	public class FamicomDeck : IControllerDeck
	{
		// two NES controllers are maintained internally
		INesPort Player1 = new ControllerNES(false);
		INesPort Player2 = new ControllerNES(true);
		IFamicomExpansion Player3;

		ControlDefUnMerger Player1U;
		ControlDefUnMerger Player2U;
		ControlDefUnMerger Player3U;

		ControllerDefinition Definition;

		public FamicomDeck(IFamicomExpansion ExpSlot, Func<int, int, bool> PPUCallback)
		{
			Player3 = ExpSlot;
			List<ControlDefUnMerger> cdum;
			Definition = ControllerDefMerger.GetMerged(
				new[] { Player1.GetDefinition(), Player2.GetDefinition(), Player3.GetDefinition() }, out cdum);
			Definition.BoolButtons.Add("P2 Microphone");
			Player1U = cdum[0];
			Player2U = cdum[1];
			Player3U = cdum[2];

			// hack
			if (Player3 is Zapper)
				(Player3 as Zapper).PPUCallback = PPUCallback;
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			Player1.Strobe(s, Player1U.UnMerge(c));
			Player2.Strobe(s, Player2U.UnMerge(c));
			Player3.Strobe(s, Player3U.UnMerge(c));
		}

		public byte ReadA(IController c)
		{
			byte ret = 0;
			ret |= (byte)(Player1.Read(Player1U.UnMerge(c)) & 1);
			ret |= (byte)(Player3.ReadA(Player3U.UnMerge(c)) & 2);
			if (c.IsPressed("P2 Microphone"))
				ret |= 4;
			return ret;
		}

		public byte ReadB(IController c)
		{
			byte ret = 0;
			ret |= (byte)(Player2.Read(Player2U.UnMerge(c)) & 1);
			ret |= (byte)(Player3.ReadB(Player3U.UnMerge(c)) & 30);
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Left");
			Player1.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Right");
			Player2.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Expansion");
			Player3.SyncState(ser);
			ser.EndSection();
		}
	}

	/// <summary>
	/// vaus controller that plugs into a famicom's expansion port
	/// </summary>
	public class ArkanoidFam : IFamicomExpansion
	{
		int shiftidx = 0;
		bool resetting = false;
		byte latchedvalue = 0x54 ^ 0xff;

		static ControllerDefinition Definition = new ControllerDefinition
		{
			BoolButtons = { "0Fire" },
			FloatControls = { "0Paddle" },
			FloatRanges = { new[] { 0.0f, 80.0f, 160.0f } }
		};

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (resetting)
				shiftidx = 0;
			if (s.OUT0 > s.OUT0old)
			{
				latchedvalue = (byte)(0x54 + (int)c.GetFloat("0Paddle"));
				latchedvalue ^= 0xff;
			}
		}

		public byte ReadA(IController c)
		{
			return c.IsPressed("0Fire") ? (byte)0x02 : (byte)0x00;
		}

		public byte ReadB(IController c)
		{
			byte ret = 0;
			if (resetting)
				return ret;

			byte value = latchedvalue;
			value <<= shiftidx;
			ret |= (byte)(value >> 6 & 0x02);
			shiftidx++;
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("shiftidx", ref shiftidx);
			ser.Sync("restting", ref resetting);
			ser.Sync("latchedvalue", ref latchedvalue);
		}
	}

	public class FamilyBasicKeyboard : IFamicomExpansion
	{
		#region buttonlookup
		static string[] Buttons =
		{
			"0]",
			"0[",
			"0RETURN",
			"0F8",
			"0STOP",
			"0¥",
			"0RSHIFT",
			"0カナ",

			"0;",
			"0:",
			"0@",
			"0F7",
			"0^",
			"0-",
			"0/",
			"0_",

			"0K",
			"0L",
			"0O",
			"0F6",
			"00",
			"0P",
			"0,",
			"0.",

			"0J",
			"0U",
			"0I",
			"0F5",
			"08",
			"09",
			"0N",
			"0M",

			"0H",
			"0G",
			"0Y",
			"0F4",
			"06",
			"07",
			"0V",
			"0B",

			"0D",
			"0R",
			"0T",
			"0F3",
			"04",
			"05",
			"0C",
			"0F",

			"0A",
			"0S",
			"0W",
			"0F2",
			"03",
			"0E",
			"0Z",
			"0X",

			"0CTR",
			"0Q",
			"0ESC",
			"0F1",
			"02",
			"01",
			"0GRPH",
			"0LSHIFT",

			"0LEFT",
			"0RIGHT",
			"0UP",
			"0CLR",
			"0INS",
			"0DEL",
			"0SPACE",
			"0DOWN",

		};
		#endregion

		static ControllerDefinition Definition = new ControllerDefinition { BoolButtons = new List<string>(Buttons) };

		bool active;
		int column;
		int row;

		public void Strobe(StrobeInfo s, IController c)
		{
			active = s.OUT2 != 0;
			column = s.OUT1;
			if (s.OUT1 < s.OUT1old)
			{
				row++;
				if (row == 10)
					row = 0;
			}
			if (s.OUT0 != 0)
				row = 0;
		}

		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			if (!active)
				return 0;
			if (row == 9) // empty last row
				return 0;
			int idx = row * 8 + column * 4;

			byte ret = 0;

			if (c.IsPressed(Buttons[idx])) ret |= 16;
			if (c.IsPressed(Buttons[idx + 1])) ret |= 8;
			if (c.IsPressed(Buttons[idx + 2])) ret |= 4;
			if (c.IsPressed(Buttons[idx + 3])) ret |= 2;

			// nothing is clocked here
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("active", ref active);
			ser.Sync("column", ref column);
			ser.Sync("row", ref row);
		}
	}

	public class Famicom4P : IFamicomExpansion
	{
		static string[] P1Buttons =
		{
			"0A", "0B", "0Select", "0Start", "0Up", "0Down", "0Left", "0Right"
		};
		static string[] P2Buttons =
		{
			"1A", "1B", "1Select", "1Start", "1Up", "1Down", "1Left", "1Right",
		};
		static ControllerDefinition Definition = new ControllerDefinition { BoolButtons = new List<string>(P1Buttons.Concat(P2Buttons)) };

		bool resetting = false;
		int latchedp1 = 0;
		int latchedp2 = 0;

		void Latch(IController c)
		{
			latchedp1 = SerialUtil.Latch(P1Buttons, c);
			latchedp2 = SerialUtil.Latch(P2Buttons, c);
		}

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 != 0;
			if (s.OUT0 < s.OUT0old)
				Latch(c);
		}

		public byte ReadA(IController c)
		{
			if (resetting)
				Latch(c);
			byte ret = (byte)(latchedp1 << 1 & 2);
			if (!resetting)
				latchedp1 >>= 1;
			return ret;
		}

		public byte ReadB(IController c)
		{
			if (resetting)
				Latch(c);
			byte ret = (byte)(latchedp2 << 1 & 2);
			if (!resetting)
				latchedp2 >>= 1;
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("resetting", ref resetting);
			ser.Sync("latchedp1", ref latchedp1);
			ser.Sync("latchedp2", ref latchedp2);
		}
	}

	public class OekaKids : IFamicomExpansion
	{
		static ControllerDefinition Definition = new ControllerDefinition
		{
			BoolButtons = { "0Click", "0Touch" },
			FloatControls = { "0Pen X", "0Pen Y" },
			FloatRanges = { new[] { 0.0f, 128.0f, 255.0f }, new[] { 0.0f, 120.0f, 239.0f } }
		};

		bool resetting;
		int shiftidx;
		int latchedvalue = 0;

		public void Strobe(StrobeInfo s, IController c)
		{
			resetting = s.OUT0 == 0;
			if (s.OUT0 < s.OUT0old) // H->L: latch
			{
				int x = (int)c.GetFloat("0Pen X");
				int y = (int)c.GetFloat("0Pen Y");
				// http://forums.nesdev.com/viewtopic.php?p=19454#19454
				// it almost feels like the hardware guys got the request for 
				// a tablet that returned x in [0, 255] and y in [0, 239] and then
				// accidentally flipped the whole thing sideways
				x = (x + 8) * 240 / 256;
				y = (y - 14) * 256 / 240;
				x &= 255;
				y &= 255;
				latchedvalue = x << 10 | y << 2;
				if (c.IsPressed("0Touch"))
					latchedvalue |= 2;
				if (c.IsPressed("0Click"))
					latchedvalue |= 1;
			}
			if (s.OUT0 > s.OUT0old) // L->H: reset shift
				shiftidx = 0;
			if (s.OUT1 > s.OUT1old) // L->H: increment shift
				shiftidx++;
		}

		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			byte ret = (byte)(resetting ? 2 : 0);
			if (resetting)
				return ret;

			// the shiftidx = 0 read is one off the end
			int bit = latchedvalue >> (16 - shiftidx);
			bit &= 4;
			bit ^= 4; // inverted data
			ret |= (byte)(bit);
			return ret;
		}

		public ControllerDefinition GetDefinition()
		{
			return Definition;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("resetting", ref resetting);
			ser.Sync("shiftidx", ref shiftidx);
			ser.Sync("latchedvalue", ref latchedvalue);
		}
	}

	public class UnpluggedFam : IFamicomExpansion
	{
		public void Strobe(StrobeInfo s, IController c)
		{
		}

		public byte ReadA(IController c)
		{
			return 0;
		}

		public byte ReadB(IController c)
		{
			return 0;
		}

		public ControllerDefinition GetDefinition()
		{
			return new ControllerDefinition();
		}

		public void SyncState(Serializer ser)
		{
		}
	}

	public static class SerialUtil
	{
		public static int Latch(string[] values, IController c)
		{
			int ret = 0;
			for (int i = 0; i < 32; i++)
			{
				if (values.Length > i)
				{
					if (values[i] != null && c.IsPressed(values[i]))
						ret |= 1 << i;
				}
				else
				{
					// 1 in all other bits
					ret |= 1 << i;
				}
			}
			return ret;
		}
	}

	#region control definition adapters

	// the idea here is that various connected peripherals have their controls all merged
	// into one definition, including logic to unmerge the data back so each one can work
	// with it without knowing what else is connected

	public class ControlDefUnMerger
	{
		Dictionary<string, string> Remaps;

		public ControlDefUnMerger(Dictionary<string, string> Remaps)
		{
			this.Remaps = Remaps;
		}

		private class DummyController : IController
		{
			IController src;
			Dictionary<string, string> remaps;
			public DummyController(IController src, Dictionary<string, string> remaps)
			{
				this.src = src;
				this.remaps = remaps;
			}

			public ControllerDefinition Definition { get { throw new NotImplementedException(); } }

			public bool this[string button] { get { return IsPressed(button); } }

			public bool IsPressed(string button)
			{
				return src.IsPressed(remaps[button]);
			}

			public float GetFloat(string name)
			{
				return src.GetFloat(remaps[name]);
			}
		}

		public IController UnMerge(IController c)
		{
			return new DummyController(c, Remaps);
		}

	}

	public static class ControllerDefMerger
	{
		private static string Allocate(string input, ref int plr, ref int plrnext)
		{
			int offset = int.Parse(input.Substring(0, 1));
			int currplr = plr + offset;
			if (currplr >= plrnext)
				plrnext = currplr + 1;
			return string.Format("P{0} {1}", currplr, input.Substring(1));
		}

		/// <summary>
		/// handles all player number merging
		/// </summary>
		/// <param name="Controllers"></param>
		/// <returns></returns>
		public static ControllerDefinition GetMerged(IEnumerable<ControllerDefinition> Controllers, out List<ControlDefUnMerger> Unmergers)
		{
			ControllerDefinition ret = new ControllerDefinition();
			Unmergers = new List<ControlDefUnMerger>();
			int plr = 1;
			int plrnext = 1;
			foreach (var def in Controllers)
			{
				Dictionary<string, string> remaps = new Dictionary<string, string>();

				foreach (string s in def.BoolButtons)
				{
					string r = Allocate(s, ref plr, ref plrnext);
					ret.BoolButtons.Add(r);
					remaps[s] = r;
				}
				foreach (string s in def.FloatControls)
				{
					string r = Allocate(s, ref plr, ref plrnext);
					ret.FloatControls.Add(r);
					remaps[s] = r;
				}
				ret.FloatRanges.AddRange(def.FloatRanges);
				plr = plrnext;
				Unmergers.Add(new ControlDefUnMerger(remaps));
			}
			return ret;
		}
	}

	#endregion

	#region settings

	public class NESControlSettings
	{
		static readonly Dictionary<string, Type> FamicomExpansions;
		static readonly Dictionary<string, Type> NesPortDevices;

		static Dictionary<string, Type> Implementors<T>()
		{
			var assy = typeof(NESControlSettings).Assembly;
			var types = assy.GetTypes().Where(c => typeof(T).IsAssignableFrom(c) && !c.IsAbstract && !c.IsInterface);
			var ret = new Dictionary<string, Type>();
			foreach (Type t in types)
				ret[t.Name] = t;
			return ret;
		}

		static NESControlSettings()
		{
			FamicomExpansions = Implementors<IFamicomExpansion>();
			NesPortDevices = Implementors<INesPort>();
		}

		public static IList<string> GetFamicomExpansionValues()
		{
			return new List<string>(FamicomExpansions.Keys).AsReadOnly();
		}
		public static IList<string> GetNesPortValues()
		{
			return new List<string>(NesPortDevices.Keys).AsReadOnly();
		}

		[JsonIgnore]
		private bool _Famicom;
		public bool Famicom { get { return _Famicom; } set { _Famicom = value; } }
		[JsonIgnore]
		private string _NesLeftPort;
		[JsonIgnore]
		private string _NesRightPort;
		public string NesLeftPort
		{
			get { return _NesLeftPort; }
			set
			{
				if (NesPortDevices.ContainsKey(value))
					_NesLeftPort = value;
				else
					throw new InvalidOperationException();
			}
		}
		public string NesRightPort
		{
			get { return _NesRightPort; }
			set
			{
				if (NesPortDevices.ContainsKey(value))
					_NesRightPort = value;
				else
					throw new InvalidOperationException();
			}
		}
		[JsonIgnore]
		private string _FamicomExpPort;
		public string FamicomExpPort
		{
			get { return _FamicomExpPort; }
			set
			{
				if (FamicomExpansions.ContainsKey(value))
					_FamicomExpPort = value;
				else
					throw new InvalidOperationException();
			}
		}

		public NESControlSettings()
		{
			Famicom = false;
			FamicomExpPort = typeof(UnpluggedFam).Name;
			NesLeftPort = typeof(ControllerNES).Name;
			NesRightPort = typeof(ControllerNES).Name;
		}

		public static bool NeedsReboot(NESControlSettings x, NESControlSettings y)
		{
			return !DeepEquality.DeepEquals(x, y);
		}

		public NESControlSettings Clone()
		{
			return (NESControlSettings)MemberwiseClone();
		}

		public IControllerDeck Instantiate(Func<int, int, bool> PPUCallback)
		{
			if (Famicom)
			{
				IFamicomExpansion exp = (IFamicomExpansion)Activator.CreateInstance(FamicomExpansions[FamicomExpPort]);
				IControllerDeck ret = new FamicomDeck(exp, PPUCallback);
				return ret;
			}
			else
			{
				INesPort left = (INesPort)Activator.CreateInstance(NesPortDevices[NesLeftPort]);
				INesPort right = (INesPort)Activator.CreateInstance(NesPortDevices[NesRightPort]);
				IControllerDeck ret = new NesDeck(left, right, PPUCallback);
				return ret;
			}
		}
	}

	#endregion
}
