using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80;

// http://www.ticalc.org/pub/text/calcinfo/

namespace BizHawk.Emulation.Cores.Calculators
{
	[CoreAttributes(
		"TI83Hawk",
		"zeromus",
		isPorted: false,
		isReleased: true
		)]
	public partial class TI83 : IEmulator, IMemoryDomains, IStatable, IDebuggable, ISettable<TI83.TI83Settings, object>
	{
		//hardware
		private readonly Z80A cpu = new Z80A();
		private readonly byte[] rom;
		private byte[] ram;
		private int romPageLow3Bits;
		private int romPageHighBit;
		private byte maskOn;
		private bool onPressed;
		private int keyboardMask;

		private int disp_mode;
		private int disp_move;
		private uint disp_x, disp_y;
		internal int m_LinkOutput, m_LinkInput;

		internal int m_LinkState
		{
			get
			{
				return (m_LinkOutput | m_LinkInput) ^ 3;
			}
		}

		internal bool LinkActive;
		private bool m_CursorMoved;

		private int lagCount = 0;
		private bool lagged = true;
		private bool isLag = false;
		private int frame;

		[CoreConstructor("TI83")]
		public TI83(CoreComm comm, GameInfo game, byte[] rom, object Settings)
		{
			PutSettings((TI83Settings)Settings ?? new TI83Settings());

			CoreComm = comm;
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
			cpu.ReadHardware = ReadHardware;
			cpu.WriteHardware = WriteHardware;
			cpu.IRQCallback = IRQCallback;
			cpu.NMICallback = NMICallback;

			this.rom = rom;
			LinkPort = new TI83LinkPort(this);

			//different calculators (different revisions?) have different initPC. we track this in the game database by rom hash
			//if( *(unsigned long *)(m_pRom + 0x6ce) == 0x04D3163E ) m_Regs.PC.W = 0x6ce; //KNOWN
			//else if( *(unsigned long *)(m_pRom + 0x6f6) == 0x04D3163E ) m_Regs.PC.W = 0x6f6; //UNKNOWN

			if (game["initPC"])
				startPC = ushort.Parse(game.OptionValue("initPC"), NumberStyles.HexNumber);

			HardReset();
			SetupMemoryDomains();
		}

		//-------

		public byte ReadMemory(ushort addr)
		{
			byte ret;
			int romPage = romPageLow3Bits | (romPageHighBit << 3);
			//Console.WriteLine("read memory: {0:X4}", addr);
			if (addr < 0x4000)
				ret = rom[addr]; //ROM zero-page
			else if (addr < 0x8000)
				ret = rom[romPage * 0x4000 + addr - 0x4000]; //other rom page
			else ret = ram[addr - 0x8000];

			CoreComm.MemoryCallbackSystem.CallRead(addr);

			return ret;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x4000)
				return; //ROM zero-page
			else if (addr < 0x8000)
				return; //other rom page
			else ram[addr - 0x8000] = value;

			CoreComm.MemoryCallbackSystem.CallWrite(addr);
		}

		public void WriteHardware(ushort addr, byte value)
		{
			switch (addr)
			{
				case 0: //PORT_LINK
					romPageHighBit = (value >> 4) & 1;
					m_LinkOutput = value & 3;

					if (LinkActive)
					{
						//Prevent rom calls from disturbing link port activity
						if (LinkActive && cpu.RegisterPC < 0x4000)
							return;

						LinkPort.Update();
					}
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
					maskOn = (byte)(value & 1);
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
					LinkPort.Update();
					return (byte)((romPageHighBit << 4) | (m_LinkState << 2) | m_LinkOutput);
				case 1: //PORT_KEYBOARD:
					//Console.WriteLine("read PORT_KEYBOARD");
					return ReadKeyboard();
				case 2: //PORT_ROMPAGE
					return (byte)romPageLow3Bits;
				case 3: //PORT_STATUS
					{
						//Console.WriteLine("read PORT_STATUS");
						// Bits:
						// 0   - Set if ON key is down and ON key is trapped
						// 1   - Update things (keyboard etc)
						// 2   - Unknown, but used
						// 3   - Set if ON key is up
						// 4-7 - Unknown
						//if (onPressed && maskOn) ret |= 1;
						//if (!onPressed) ret |= 0x8;
						return (byte)((Controller.IsPressed("ON") ? maskOn : 8) | (LinkActive ? 0 : 2));
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

		private byte ReadKeyboard()
		{
			CoreComm.InputCallback.Call();
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

		private byte ReadDispData()
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

		private void WriteDispData(byte value)
		{
			int offset;
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

		private void doDispMove()
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

		private void WriteDispCtrl(byte value)
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

		private void IRQCallback()
		{
			//Console.WriteLine("IRQ with vec {0} and cpu.InterruptMode {1}", cpu.RegisterI, cpu.InterruptMode);
			cpu.Interrupt = false;
		}

		private void NMICallback()
		{
			Console.WriteLine("NMI");
			cpu.NonMaskableInterrupt = false;
		}

		public CoreComm CoreComm { get; private set; }

		protected byte[] vram = new byte[0x300];
		private class MyVideoProvider : IVideoProvider
		{
			private readonly TI83 emu;
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
						{
							unchecked { pixels[i++] = (int)emu.Settings.BGColor; }
						}
						else
						{
							pixels[i++] = (int)emu.Settings.ForeColor;
						}

					}
				return pixels;
			}

			public int VirtualWidth { get { return 96; } }
			public int VirtualHeight { get { return 64; } }
			public int BufferWidth { get { return 96; } }
			public int BufferHeight { get { return 64; } }
			public int BackgroundColor { get { return 0; } }
		}
		public IVideoProvider VideoProvider
		{
			get { return new MyVideoProvider(this); }
		}

		public ISoundProvider SoundProvider { get { return NullSound.SilenceProvider; } }
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(NullSound.SilenceProvider, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

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

		public IController Controller { get; set; }

		// configuration
		private ushort startPC;

		public void FrameAdvance(bool render, bool rendersound)
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

			Frame++;
			if (lagged)
			{
				lagCount++;
				isLag = true;
			}
			else
			{
				isLag = false;
			}
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

			maskOn = 1;
			romPageHighBit = 0;
			romPageLow3Bits = 0;
			keyboardMask = 0;

			disp_mode = 0;
			disp_move = 0;
			disp_x = disp_y = 0;
		}

		public int Frame { get { return frame; } set { frame = value; } }
		public int LagCount { get { return lagCount; } set { lagCount = value; } }
		public bool IsLagFrame { get { return isLag; } }

		public void ResetCounters()
		{
			Frame = 0;
			lagCount = 0;
			isLag = false;
		}

		public bool DeterministicEmulation { get { return true; } }

		public bool BinarySaveStatesPreferred { get { return false; } }
		public void SaveStateBinary(BinaryWriter bw) { SyncState(Serializer.CreateBinaryWriter(bw)); }
		public void LoadStateBinary(BinaryReader br) { SyncState(Serializer.CreateBinaryReader(br)); }
		public void SaveStateText(TextWriter tw) { SyncState(Serializer.CreateTextWriter(tw)); }
		public void LoadStateText(TextReader tr) { SyncState(Serializer.CreateTextReader(tr)); }

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("TI83");
			cpu.SyncState(ser);
			ser.Sync("RAM", ref ram, false);
			ser.Sync("romPageLow3Bits", ref romPageLow3Bits);
			ser.Sync("romPageHighBit", ref romPageHighBit);
			ser.Sync("disp_mode", ref disp_mode);
			ser.Sync("disp_move", ref disp_move);
			ser.Sync("disp_x", ref disp_x);
			ser.Sync("disp_y", ref disp_y);
			ser.Sync("m_CursorMoved", ref m_CursorMoved);
			ser.Sync("maskOn", ref maskOn);
			ser.Sync("onPressed", ref onPressed);
			ser.Sync("keyboardMask", ref keyboardMask);
			ser.Sync("m_LinkOutput", ref m_LinkOutput);
			ser.Sync("VRAM", ref vram, false);
			ser.Sync("Frame", ref frame);
			ser.Sync("LagCount", ref lagCount);
			ser.Sync("IsLag", ref isLag);
			ser.EndSection();
		}

		private byte[] stateBuffer;
		public byte[] SaveStateBinary()
		{
			if (stateBuffer == null)
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				stateBuffer = stream.ToArray();
				writer.Close();
				return stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Close();
				return stateBuffer;
			}
		}

		public string SystemId { get { return "TI83"; } }
		public string BoardName { get { return null; } }

		private const ushort RamSizeMask = 0x7FFF;

		public void Dispose() { }

		public TI83LinkPort LinkPort { get; set; }
	}
}