using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.Z80;

//http://www.ticalc.org/pub/text/calcinfo/

namespace BizHawk.Emulation.Consoles.Calculator
{
	[CoreVersion("0.8.1", FriendlyName = "TI-83")]
	public class TI83 : IEmulator
	{
		//hardware
		Z80A cpu = new Z80A();
		byte[] rom;
		byte[] ram;
		int romPageLow3Bits;
		int romPageHighBit;
		bool maskOn;
		bool onPressed = false;
		int keyboardMask;

		int disp_mode;
		int disp_move;
		uint disp_x, disp_y;
		int m_LinkOutput, m_LinkState;
		bool m_CursorMoved;

		//-------

		public byte ReadMemory(ushort addr)
		{
			byte ret;
			int romPage = romPageLow3Bits | (romPageHighBit << 3);
			//Console.WriteLine("read memory: {0:X4}", addr);
			if (addr < 0x4000)
				ret =  rom[addr]; //ROM zero-page
			else if (addr < 0x8000)
				ret = rom[romPage * 0x4000 + addr - 0x4000]; //other rom page
			else ret = ram[addr - 0x8000];

			return ret;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x4000)
				return; //ROM zero-page
			else if (addr < 0x8000)
				return; //other rom page
			else ram[addr - 0x8000] = value;
		}

		public void WriteHardware(ushort addr, byte value)
		{
			switch (addr)
			{
				case 0: //PORT_LINK
					romPageHighBit = (value >> 4) & 1;
					m_LinkOutput = value & 3;
					m_LinkState = m_LinkOutput ^ 3;
					break;
				case 1: //PORT_KEYBOARD:
					lagged = false;
					keyboardMask = value;
					//Console.WriteLine("write PORT_KEYBOARD {0:X2}",value);
					break;
				case 2: //PORT_ROMPAGE
					romPageLow3Bits = value & 0x7;
					break;
				case 3: //PORT_STATUS
					maskOn = ((value & 1) == 1);
					break;
				case 16: //PORT_DISPCTRL
					//Console.WriteLine("write PORT_DISPCTRL {0}",value);
					WriteDispCtrl(value);
					break;
				case 17: //PORT_DISPDATA
					//Console.WriteLine("write PORT_DISPDATA {0}",value);
					WriteDispData(value);
					break;
			}
		}

		public byte ReadHardware(ushort addr)
		{
			switch (addr)
			{
				case 0: //PORT_LINK
					return (byte)((romPageHighBit << 4) | (m_LinkState << 2) | m_LinkOutput);
				case 1: //PORT_KEYBOARD:
					//Console.WriteLine("read PORT_KEYBOARD");
					return ReadKeyboard();
				case 2: //PORT_ROMPAGE
					return (byte)romPageLow3Bits;
				case 3: //PORT_STATUS
					{
						//Console.WriteLine("read PORT_STATUS");
						byte ret = 0;
						// Bits:
						// 0   - Set if ON key is down and ON key is trapped
						// 1   - Update things (keyboard etc)
						// 2   - Unknown, but used
						// 3   - Set if ON key is up
						// 4-7 - Unknown
						//if (onPressed && maskOn) ret |= 1;
						//if (!onPressed) ret |= 0x8;
						ret |= 0x8; //on key is up
						ret |= 0x2; //link isnt emulated
						return ret;
					}

				case 4: //PORT_INTCTRL
					//Console.WriteLine("read PORT_INTCTRL");
					return 0xFF;

				case 16: //PORT_DISPCTRL
					//Console.WriteLine("read DISPCTRL");
					break;

				case 17: //PORT_DISPDATA
					return ReadDispData();
			}
			return 0xFF;
		}

		byte ReadKeyboard()
		{
			//ref TI-9X

			int ret = 0xFF;
			//Console.WriteLine("keyboardMask: {0:X2}",keyboardMask);
			if ((keyboardMask & 1) == 0)
			{
				if (Controller.IsPressed("DOWN")) ret ^= 1;
				if (Controller.IsPressed("LEFT")) ret ^= 2;
				if (Controller.IsPressed("RIGHT")) ret ^= 4;
				if (Controller.IsPressed("UP")) ret ^= 8;
			}
			if ((keyboardMask & 2) == 0)
			{
				if (Controller.IsPressed("ENTER")) ret ^= 1;
				if (Controller.IsPressed("PLUS")) ret ^= 2;
				if (Controller.IsPressed("MINUS")) ret ^= 4;
				if (Controller.IsPressed("MULTIPLY")) ret ^= 8;
				if (Controller.IsPressed("DIVIDE")) ret ^= 16;
				if (Controller.IsPressed("EXP")) ret ^= 32;
				if (Controller.IsPressed("CLEAR")) ret ^= 64;
			}
			if ((keyboardMask & 4) == 0)
			{
				if (Controller.IsPressed("DASH")) ret ^= 1;
				if (Controller.IsPressed("3")) ret ^= 2;
				if (Controller.IsPressed("6")) ret ^= 4;
				if (Controller.IsPressed("9")) ret ^= 8;
				if (Controller.IsPressed("PARACLOSE")) ret ^= 16;
				if (Controller.IsPressed("TAN")) ret ^= 32;
				if (Controller.IsPressed("VARS")) ret ^= 64;
			}
			if ((keyboardMask & 8) == 0)
			{
				if (Controller.IsPressed("DOT")) ret ^= 1;
				if (Controller.IsPressed("2")) ret ^= 2;
				if (Controller.IsPressed("5")) ret ^= 4;
				if (Controller.IsPressed("8")) ret ^= 8;
				if (Controller.IsPressed("PARAOPEN")) ret ^= 16;
				if (Controller.IsPressed("COS")) ret ^= 32;
				if (Controller.IsPressed("PRGM")) ret ^= 64;
				if (Controller.IsPressed("STAT")) ret ^= 128;
			}
			if ((keyboardMask & 16) == 0)
			{
				if (Controller.IsPressed("0")) ret ^= 1;
				if (Controller.IsPressed("1")) ret ^= 2;
				if (Controller.IsPressed("4")) ret ^= 4;
				if (Controller.IsPressed("7")) ret ^= 8;
				if (Controller.IsPressed("COMMA")) ret ^= 16;
				if (Controller.IsPressed("SIN")) ret ^= 32;
				if (Controller.IsPressed("MATRIX")) ret ^= 64;
				if (Controller.IsPressed("X")) ret ^= 128;
			}

			if ((keyboardMask & 32) == 0)
			{
				if (Controller.IsPressed("STO")) ret ^= 2;
				if (Controller.IsPressed("LN")) ret ^= 4;
				if (Controller.IsPressed("LOG")) ret ^= 8;
				if (Controller.IsPressed("SQUARED")) ret ^= 16;
				if (Controller.IsPressed("NEG1")) ret ^= 32;
				if (Controller.IsPressed("MATH"))
					ret ^= 64;
				if (Controller.IsPressed("ALPHA")) ret ^= 128;
			}

			if ((keyboardMask & 64) == 0)
			{
				if (Controller.IsPressed("GRAPH")) ret ^= 1;
				if (Controller.IsPressed("TRACE")) ret ^= 2;
				if (Controller.IsPressed("ZOOM")) ret ^= 4;
				if (Controller.IsPressed("WINDOW")) ret ^= 8;
				if (Controller.IsPressed("Y")) ret ^= 16;
				if (Controller.IsPressed("2ND")) ret ^= 32;
				if (Controller.IsPressed("MODE")) ret ^= 64;
				if (Controller.IsPressed("DEL")) ret ^= 128;
			}

			return (byte)ret;

		}

		byte ReadDispData()
		{
			if (m_CursorMoved)
			{
				m_CursorMoved = false;
				return 0x00; //not accurate this should be stale data or something
			}

			byte ret;
			if (disp_mode == 1)
			{
				ret = vram[disp_y * 12 + disp_x];
			}
			else
			{
				int column = 6 * (int)disp_x;
				int offset = (int)disp_y * 12 + (column >> 3);
				int shift = 10 - (column & 7);
				ret = (byte)(((vram[offset] << 8) | vram[offset + 1]) >> shift);
			}

			doDispMove();
			return ret;
		}

		void WriteDispData(byte value)
		{
			int offset = -1;
			if (disp_mode == 1)
			{
				offset = (int)disp_y * 12 + (int)disp_x;
				vram[offset] = value;
			}
			else
			{
				int column = 6 * (int)disp_x;
				offset = (int)disp_y * 12 + (column >> 3);
				if (offset < 0x300)
				{
					int shift = column & 7;
					int mask = ~(252 >> shift);
					int Data = value << 2;
					vram[offset] = (byte)(vram[offset] & mask | (Data >> shift));
					if (shift > 2 && offset < 0x2ff)
					{
						offset++;

						shift = 8 - shift;

						mask = ~(252 << shift);
						vram[offset] = (byte)(vram[offset] & mask | (Data << shift));
					}
				}
			}

			doDispMove();
		}

		void doDispMove()
		{
			switch (disp_move)
			{
				case 0: disp_y--; break;
				case 1: disp_y++; break;
				case 2: disp_x--; break;
				case 3: disp_x++; break;
			}

			disp_x &= 0xF; //0xF or 0x1F? dunno
			disp_y &= 0x3F;
		}

		void WriteDispCtrl(byte value)
		{
			if (value <= 1)
				disp_mode = value;
			else if (value >= 4 && value <= 7)
				disp_move = value - 4;
			else if ((value & 0xC0) == 0x40)
			{
				//hardware scroll
			}
			else if ((value & 0xE0) == 0x20)
			{
				disp_x = (uint)(value & 0x1F);
				m_CursorMoved = true;
			}
			else if ((value & 0xC0) == 0x80)
			{
				disp_y = (uint)(value & 0x3F);
				m_CursorMoved = true;
			}
			else if ((value & 0xC0) == 0xC0)
			{
				//contrast
			}
			else if (value == 2)
			{
			}
			else if (value == 3)
			{
			}
			else
			{
			}
		}

		public TI83(GameInfo game, byte[] rom)
		{
			CoreOutputComm = new CoreOutputComm();
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.ReadHardware = ReadHardware;
			cpu.WriteHardware = WriteHardware;
			cpu.IRQCallback = IRQCallback;
			cpu.NMICallback = NMICallback;

			this.rom = rom;

			//different calculators (different revisions?) have different initPC. we track this in the game database by rom hash
			//if( *(unsigned long *)(m_pRom + 0x6ce) == 0x04D3163E ) m_Regs.PC.W = 0x6ce; //KNOWN
			//else if( *(unsigned long *)(m_pRom + 0x6f6) == 0x04D3163E ) m_Regs.PC.W = 0x6f6; //UNKNOWN

            if (game["initPC"])
                startPC = ushort.Parse(game.OptionValue("initPC"), NumberStyles.HexNumber);

			HardReset();
			SetupMemoryDomains();
		}

		void IRQCallback()
		{
			//Console.WriteLine("IRQ with vec {0} and cpu.InterruptMode {1}", cpu.RegisterI, cpu.InterruptMode);
			cpu.Interrupt = false;
		}

		void NMICallback()
		{
			Console.WriteLine("NMI");
			cpu.NonMaskableInterrupt = false;
		}


		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }

		protected byte[] vram = new byte[0x300];
		class MyVideoProvider : IVideoProvider
		{
			TI83 emu;
			public MyVideoProvider(TI83 emu)
			{
				this.emu = emu;
			}

			public int[] GetVideoBuffer()
			{
				//unflatten bit buffer
				int[] pixels = new int[96 * 64];
				int i = 0;
				for (int y = 0; y < 64; y++)
					for (int x = 0; x < 96; x++)
					{
						int offset = y * 96 + x;
						int bufbyte = offset >> 3;
						int bufbit = offset & 7;
						int bit = ((emu.vram[bufbyte] >> (7 - bufbit)) & 1);
						if (bit == 0)
							unchecked { pixels[i++] = (int)0xFFFFFFFF; }
						else
							pixels[i++] = 0;
							
					}
				return pixels;
			}
            public int VirtualWidth { get { return 96; } }
			public int BufferWidth { get { return 96; } }
			public int BufferHeight { get { return 64; } }
			public int BackgroundColor { get { return 0; } }
		}
		public IVideoProvider VideoProvider { 
			get { return new MyVideoProvider(this); } }

		public ISoundProvider SoundProvider { get { return NullSound.SilenceProvider; } }

		public static readonly ControllerDefinition TI83Controller =
			new ControllerDefinition
			{
				Name = "TI83 Controller",
				BoolButtons = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9","DOT",
					"ON","ENTER",
					"DOWN","LEFT","UP","RIGHT",
					"PLUS","MINUS","MULTIPLY","DIVIDE",
					"CLEAR", "EXP", "DASH", "PARACLOSE", "TAN", "VARS", "PARAOPEN",
					"COS", "PRGM", "STAT", "COMMA", "SIN", "MATRIX", "X",
					"STO", "LN", "LOG", "SQUARED", "NEG1", "MATH", "ALPHA",
					"GRAPH", "TRACE", "ZOOM", "WINDOW", "Y", "2ND", "MODE", "DEL"
				}
			};

		public ControllerDefinition ControllerDefinition { get { return TI83Controller; } }

		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value; }
		}
		//configuration
		ushort startPC;

		public void FrameAdvance(bool render)
		{
			lagged = true;
			//I eyeballed this speed
			for (int i = 0; i < 5; i++)
			{
				onPressed = Controller.IsPressed("ON");
				//and this was derived from other emus
				cpu.ExecuteCycles(10000);
				cpu.Interrupt = true;
			}
			Controller.UpdateControls(Frame++);
			if (lagged)
			{
				_lagcount++;
				islag = true;
			}
			else
				islag = false;
		}

		public void HardReset()
		{
			cpu.Reset();
			ram = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
				ram[i] = 0xFF;
			cpu.RegisterPC = startPC;

			cpu.IFF1 = false;
			cpu.IFF2 = false;
			cpu.InterruptMode = 2;

			maskOn = false;
			romPageHighBit = 0;
			romPageLow3Bits = 0;
			keyboardMask = 0;

			disp_mode = 0;
			disp_move = 0;
			disp_x = disp_y = 0;
		}

		private int _lagcount = 0;
		private bool lagged = true;
		private bool islag = false;
		public int Frame { get; set; }

		public void ResetFrameCounter()
		{
			Frame = 0;
		}

		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }

		public bool DeterministicEmulation { get { return true; } set { } }

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool SaveRamModified
		{
			get { return false; }
			set { }
		}

		public void SaveStateText(TextWriter writer)
		{
			writer.WriteLine("[TI83]\n");
			writer.WriteLine("Frame {0}", Frame);
			cpu.SaveStateText(writer);
			writer.Write("RAM ");
			ram.SaveAsHex(writer);
			writer.WriteLine("romPageLow3Bits {0}", romPageLow3Bits);
			writer.WriteLine("romPageHighBit {0}", romPageHighBit);
			writer.WriteLine("disp_mode {0}", disp_mode);
			writer.WriteLine("disp_move {0}", disp_move);
			writer.WriteLine("disp_x {0}", disp_x);
			writer.WriteLine("disp_y {0}", disp_y);
			writer.WriteLine("m_CursorMoved {0}", m_CursorMoved);
			writer.WriteLine("maskOn {0}", maskOn);
			writer.WriteLine("onPressed {0}", onPressed);
			writer.WriteLine("keyboardMask {0}", keyboardMask);
			writer.WriteLine("m_LinkOutput {0}", m_LinkOutput);
			writer.WriteLine("m_LinkState {0}", m_LinkState);
			writer.WriteLine("lag {0}", _lagcount);
			writer.WriteLine("islag {0}", islag);
			writer.WriteLine("vram {0}", Util.BytesToHexString(vram));
			writer.WriteLine("[/TI83]");
		}

		public void LoadStateText(TextReader reader)
		{
			while (true)
			{
				string[] args = reader.ReadLine().Split(' ');
				if (args[0].Trim() == "") continue;
				if (args[0] == "[TI83]") continue;
				if (args[0] == "[/TI83]") break;
				if (args[0] == "Frame")
					Frame = int.Parse(args[1]);
				else if (args[0] == "[Z80]")
					cpu.LoadStateText(reader);
				else if (args[0] == "RAM")
					ram.ReadFromHex(args[1]);
				else if (args[0] == "romPageLow3Bits")
					romPageLow3Bits = int.Parse(args[1]);
				else if (args[0] == "romPageHighBit")
					romPageHighBit = int.Parse(args[1]);
				else if (args[0] == "disp_mode")
					disp_mode = int.Parse(args[1]);
				else if (args[0] == "disp_move")
					disp_move = int.Parse(args[1]);
				else if (args[0] == "disp_x")
					disp_x = uint.Parse(args[1]);
				else if (args[0] == "disp_y")
					disp_y = uint.Parse(args[1]);
				else if (args[0] == "m_CursorMoved")
					m_CursorMoved = bool.Parse(args[1]);
				else if (args[0] == "maskOn")
					maskOn = bool.Parse(args[1]);
				else if (args[0] == "onPressed")
					onPressed = bool.Parse(args[1]);
				else if (args[0] == "keyboardMask")
					keyboardMask = int.Parse(args[1]);
				else if (args[0] == "m_LinkOutput")
					m_LinkOutput = int.Parse(args[1]);
				else if (args[0] == "m_LinkState")
					m_LinkState = int.Parse(args[1]);
				else if (args[0] == "lag")
					_lagcount = int.Parse(args[1]);
				else if (args[0] == "islag")
					islag = bool.Parse(args[1]);
				else if (args[0] == "vram")
					vram = Util.HexStringToBytes(args[1]);
				else
					Console.WriteLine("Skipping unrecognized identifier " + args[0]);
			}
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
		}

		public void LoadStateBinary(BinaryReader reader)
		{
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public string SystemId { get { return "TI83"; } }

		private IList<MemoryDomain> memoryDomains;
		private const ushort RamSizeMask = 0x7FFF;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>();
			var MainMemoryDomain = new MemoryDomain("Main RAM", ram.Length, Endian.Little,
				addr => ram[addr & RamSizeMask],
				(addr, value) => ram[addr & RamSizeMask] = value);
			domains.Add(MainMemoryDomain);
			memoryDomains = domains.AsReadOnly();
		}

		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public void Dispose() { }
	}
}