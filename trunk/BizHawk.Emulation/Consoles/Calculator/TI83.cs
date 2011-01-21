using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using BizHawk.Emulation.CPUs.Z80;

//http://www.ticalc.org/pub/text/calcinfo/

namespace BizHawk.Emulation.Consoles.Calculator
{
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
			int romPage = romPageLow3Bits | (romPageHighBit << 3);
			//Console.WriteLine("read memory: {0:X4}", addr);
			if (addr < 0x4000)
				return rom[addr]; //ROM zero-page
			else if (addr < 0x8000)
				return rom[romPage*0x4000+addr-0x4000]; //other rom page
			else return ram[addr - 0x8000];
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
					keyboardMask = value;
					Console.WriteLine("write PORT_KEYBOARD {0:X2}",value);
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
					return (byte)((romPageHighBit << 4) | (m_LinkState<<2) | m_LinkOutput);
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
					Console.WriteLine("read PORT_INTCTRL");
					return 0xFF;

				case 16: //PORT_DISPCTRL
					Console.WriteLine("read DISPCTRL");
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
			Console.WriteLine("keyboardMask: {0:X2}",keyboardMask);
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
				//(exponent key)
				if (Controller.IsPressed("CLEAR")) ret ^= 64;
			}
			if ((keyboardMask & 4) == 0)
			{
				if (Controller.IsPressed("3")) ret ^= 2;
				if (Controller.IsPressed("6")) ret ^= 4;
				if (Controller.IsPressed("9")) ret ^= 8;
			}
			if ((keyboardMask & 8) == 0)
			{
				if (Controller.IsPressed("DOT")) ret ^= 1;
				if (Controller.IsPressed("2")) ret ^= 2;
				if (Controller.IsPressed("5")) ret ^= 4;
				if (Controller.IsPressed("8")) ret ^= 8;
			}
			if ((keyboardMask & 16) == 0)
			{
				if (Controller.IsPressed("0")) ret ^= 1;
				if (Controller.IsPressed("1")) ret ^= 2;
				if (Controller.IsPressed("4")) ret ^= 4;
				if (Controller.IsPressed("7")) ret ^= 8;
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

		public TI83()
		{
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.ReadHardware = ReadHardware;
			cpu.WriteHardware = WriteHardware;
			cpu.IRQCallback = IRQCallback;
			cpu.NMICallback = NMICallback;
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

		protected byte[] vram = new byte[0x300];
		class MyVideoProvider : IVideoProvider
		{
			TI83 emu;
			public MyVideoProvider(TI83 emu)
			{
				this.emu = emu;
			}

			public int[] GetVideoBuffer() {
				//unflatten bit buffer
				int[] pixels = new int[96*64];
				int i=0;
				for(int y=0;y<64;y++)
					for (int x = 0; x < 96; x++)
					{
						int offset = y * 96 + x;
						int bufbyte = offset >> 3;
						int bufbit = offset & 7;
						int bit = ((emu.vram[bufbyte] >> (7 - bufbit)) & 1);
						if(bit==0)
							pixels[i++] = 0xFFFFFF;
						else
							pixels[i++] = 0;
					}
				return pixels;
			}
			public int BufferWidth { get { return 96; } }
			public int BufferHeight { get { return 64; } }
			public int BackgroundColor { get { return 0; } }
		}
		public IVideoProvider VideoProvider { get { return new MyVideoProvider(this); } }


		public ISoundProvider SoundProvider { get { return new NullEmulator(); } }

		public static readonly ControllerDefinition TI83Controller =
			new ControllerDefinition
			{
				Name = "TI83 Controls",
				BoolButtons = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9","DOT",
					"ON","ENTER",
					"DOWN","LEFT","UP","RIGHT",
					"PLUS","MINUS","MULTIPLY","DIVIDE",
					"CLEAR"
				}
			};

		public ControllerDefinition ControllerDefinition { get { return TI83Controller; } }

		IController controller;
		public IController Controller
		{
			get { return controller; }
			set { controller = value;  }
		}
		//configuration
		ushort startPC;

		public void LoadGame(IGame game)
		{
			rom = game.GetRomData();
			foreach (string opt in game.GetOptions())
			{
				//different calculators (different revisions?) have different initPC. we track this in the game database by rom hash
				//if( *(unsigned long *)(m_pRom + 0x6ce) == 0x04D3163E ) m_Regs.PC.W = 0x6ce; //KNOWN
				//else if( *(unsigned long *)(m_pRom + 0x6f6) == 0x04D3163E ) m_Regs.PC.W = 0x6f6; //UNKNOWN
	
				if (opt.StartsWith("initPC"))
				startPC = ushort.Parse(opt.Split('=')[1], NumberStyles.HexNumber);
			}

			HardReset();
		}

		public void FrameAdvance(bool render)
		{
			//I eyeballed this speed
			for (int i = 0; i < 5; i++)
			{
				onPressed = Controller.IsPressed("ON");
				//and this was derived from other emus
				cpu.ExecuteCycles(10000);
				cpu.Interrupt = true;
			}
		}

		public void HardReset()
		{
			cpu.Reset();
			ram = new byte[0x8000];
			for(int i=0;i<0x8000;i++)
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

		public int Frame
		{
			get { return 0; }
		}
		public bool DeterministicEmulation { get { return true; } set { } }

		public byte[] SaveRam { get { return null; } }
		public bool SaveRamModified
		{
			get { return false; }
			set { }
		}

		public void SaveStateText(TextWriter writer)
		{
		}

		public void LoadStateText(TextReader reader)
		{
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

        public IList<MemoryDomain> MemoryDomains { get { throw new NotImplementedException(); } }
        public MemoryDomain MainMemory { get { throw new NotImplementedException(); } } 
    }
}