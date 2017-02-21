using System;
using System.Globalization;

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
	[ServiceNotApplicable(typeof(ISoundProvider), typeof(ISaveRam), typeof(IRegionable), typeof(IDriveLight))]
	public partial class TI83 : IEmulator, IVideoProvider, IStatable, IDebuggable, IInputPollable, ISettable<TI83.TI83Settings, object>
	{
		[CoreConstructor("TI83")]
		public TI83(CoreComm comm, GameInfo game, byte[] rom, object Settings)
		{
			ServiceProvider = new BasicServiceProvider(this);
			InputCallbacks = new InputCallbackSystem();
			MemoryCallbacks = new MemoryCallbackSystem();
			PutSettings((TI83Settings)Settings ?? new TI83Settings());

			CoreComm = comm;
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			Cpu.ReadHardware = ReadHardware;
			Cpu.WriteHardware = WriteHardware;
			Cpu.IRQCallback = IRQCallback;
			Cpu.NMICallback = NMICallback;
			Cpu.MemoryCallbacks = MemoryCallbacks;

			Rom = rom;
			LinkPort = new TI83LinkPort(this);

			// different calculators (different revisions?) have different initPC. we track this in the game database by rom hash
			// if( *(unsigned long *)(m_pRom + 0x6ce) == 0x04D3163E ) m_Regs.PC.W = 0x6ce; //KNOWN
			// else if( *(unsigned long *)(m_pRom + 0x6f6) == 0x04D3163E ) m_Regs.PC.W = 0x6f6; //UNKNOWN

			if (game["initPC"])
			{
				_startPC = ushort.Parse(game.OptionValue("initPC"), NumberStyles.HexNumber);
			}

			HardReset();
			SetupMemoryDomains();

			Tracer = new TraceBuffer { Header = Cpu.TraceHeader };

			var serviceProvider = ServiceProvider as BasicServiceProvider;

			serviceProvider.Register<ITraceable>(Tracer);
			serviceProvider.Register<IDisassemblable>(new Disassembler());
		}

		private readonly ITraceable Tracer;

		// hardware
		private const ushort RamSizeMask = 0x7FFF;

		private readonly Z80A Cpu = new Z80A();
		private readonly byte[] Rom;

		private byte[] _ram;
		private byte[] _vram = new byte[0x300];
		private int _romPageLow3Bits;
		private int _romPageHighBit;
		private byte _maskOn;
		private bool _onPressed;
		private int _keyboardMask;

		private int _displayMode;
		private int _displayMove;
		private uint _displayX, _displayY;
		private bool _cursorMoved;
		private int _frame;

		// configuration
		private ushort _startPC;

		// Link Cable
		public TI83LinkPort LinkPort { get; private set; }

		internal bool LinkActive;
		internal int LinkOutput, LinkInput;

		internal int LinkState
		{
			get { return (LinkOutput | LinkInput) ^ 3; }
		}

		private static readonly ControllerDefinition TI83Controller =
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

		private byte ReadMemory(ushort addr)
		{
			byte ret;
			int romPage = _romPageLow3Bits | (_romPageHighBit << 3);
			//Console.WriteLine("read memory: {0:X4}", addr);
			if (addr < 0x4000)
			{
				ret = Rom[addr]; //ROM zero-page
			}
			else if (addr < 0x8000)
			{
				ret = Rom[romPage * 0x4000 + addr - 0x4000]; //other rom page
			}
			else
			{
				ret = _ram[addr - 0x8000];
			}

			return ret;
		}

		private void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x4000)
			{
				return; //ROM zero-page
			}
			else if (addr < 0x8000)
			{
				return; //other rom page
			}
			else
			{
				_ram[addr - 0x8000] = value;
			}
		}

		private void WriteHardware(ushort addr, byte value)
		{
			switch (addr)
			{
				case 0: //PORT_LINK
					_romPageHighBit = (value >> 4) & 1;
					LinkOutput = value & 3;

					if (LinkActive)
					{
						//Prevent rom calls from disturbing link port activity
						if (LinkActive && Cpu.RegisterPC < 0x4000)
							return;

						LinkPort.Update();
					}
					break;
				case 1: //PORT_KEYBOARD:
					_lagged = false;
					_keyboardMask = value;
					//Console.WriteLine("write PORT_KEYBOARD {0:X2}",value);
					break;
				case 2: //PORT_ROMPAGE
					_romPageLow3Bits = value & 0x7;
					break;
				case 3: //PORT_STATUS
					_maskOn = (byte)(value & 1);
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

		private byte ReadHardware(ushort addr)
		{
			switch (addr)
			{
				case 0: //PORT_LINK
					LinkPort.Update();
					return (byte)((_romPageHighBit << 4) | (LinkState << 2) | LinkOutput);
				case 1: //PORT_KEYBOARD:
					//Console.WriteLine("read PORT_KEYBOARD");
					return ReadKeyboard();
				case 2: //PORT_ROMPAGE
					return (byte)_romPageLow3Bits;
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
						return (byte)((Controller.IsPressed("ON") ? _maskOn : 8) | (LinkActive ? 0 : 2));
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
			InputCallbacks.Call();
			//ref TI-9X

			int ret = 0xFF;
			//Console.WriteLine("keyboardMask: {0:X2}",keyboardMask);
			if ((_keyboardMask & 1) == 0)
			{
				if (Controller.IsPressed("DOWN")) ret ^= 1;
				if (Controller.IsPressed("LEFT")) ret ^= 2;
				if (Controller.IsPressed("RIGHT")) ret ^= 4;
				if (Controller.IsPressed("UP")) ret ^= 8;
			}
			if ((_keyboardMask & 2) == 0)
			{
				if (Controller.IsPressed("ENTER")) ret ^= 1;
				if (Controller.IsPressed("PLUS")) ret ^= 2;
				if (Controller.IsPressed("MINUS")) ret ^= 4;
				if (Controller.IsPressed("MULTIPLY")) ret ^= 8;
				if (Controller.IsPressed("DIVIDE")) ret ^= 16;
				if (Controller.IsPressed("EXP")) ret ^= 32;
				if (Controller.IsPressed("CLEAR")) ret ^= 64;
			}
			if ((_keyboardMask & 4) == 0)
			{
				if (Controller.IsPressed("DASH")) ret ^= 1;
				if (Controller.IsPressed("3")) ret ^= 2;
				if (Controller.IsPressed("6")) ret ^= 4;
				if (Controller.IsPressed("9")) ret ^= 8;
				if (Controller.IsPressed("PARACLOSE")) ret ^= 16;
				if (Controller.IsPressed("TAN")) ret ^= 32;
				if (Controller.IsPressed("VARS")) ret ^= 64;
			}
			if ((_keyboardMask & 8) == 0)
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
			if ((_keyboardMask & 16) == 0)
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

			if ((_keyboardMask & 32) == 0)
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

			if ((_keyboardMask & 64) == 0)
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
			if (_cursorMoved)
			{
				_cursorMoved = false;
				return 0x00; //not accurate this should be stale data or something
			}

			byte ret;
			if (_displayMode == 1)
			{
				ret = _vram[_displayY * 12 + _displayX];
			}
			else
			{
				int column = 6 * (int)_displayX;
				int offset = (int)_displayY * 12 + (column >> 3);
				int shift = 10 - (column & 7);
				ret = (byte)(((_vram[offset] << 8) | _vram[offset + 1]) >> shift);
			}

			DoDispMove();
			return ret;
		}

		private void WriteDispData(byte value)
		{
			int offset;
			if (_displayMode == 1)
			{
				offset = (int)_displayY * 12 + (int)_displayX;
				_vram[offset] = value;
			}
			else
			{
				int column = 6 * (int)_displayX;
				offset = (int)_displayY * 12 + (column >> 3);
				if (offset < 0x300)
				{
					int shift = column & 7;
					int mask = ~(252 >> shift);
					int Data = value << 2;
					_vram[offset] = (byte)(_vram[offset] & mask | (Data >> shift));
					if (shift > 2 && offset < 0x2ff)
					{
						offset++;

						shift = 8 - shift;

						mask = ~(252 << shift);
						_vram[offset] = (byte)(_vram[offset] & mask | (Data << shift));
					}
				}
			}

			DoDispMove();
		}

		private void DoDispMove()
		{
			switch (_displayMove)
			{
				case 0: _displayY--; break;
				case 1: _displayY++; break;
				case 2: _displayX--; break;
				case 3: _displayX++; break;
			}

			_displayX &= 0xF; //0xF or 0x1F? dunno
			_displayY &= 0x3F;
		}

		private void WriteDispCtrl(byte value)
		{
			if (value <= 1)
			{
				_displayMode = value;
			}
			else if (value >= 4 && value <= 7)
			{
				_displayMove = value - 4;
			}
			else if ((value & 0xC0) == 0x40)
			{
				//hardware scroll
			}
			else if ((value & 0xE0) == 0x20)
			{
				_displayX = (uint)(value & 0x1F);
				_cursorMoved = true;
			}
			else if ((value & 0xC0) == 0x80)
			{
				_displayY = (uint)(value & 0x3F);
				_cursorMoved = true;
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
			Cpu.Interrupt = false;
		}

		private void NMICallback()
		{
			Console.WriteLine("NMI");
			Cpu.NonMaskableInterrupt = false;
		}

		private void HardReset()
		{
			Cpu.Reset();
			_ram = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
				_ram[i] = 0xFF;
			Cpu.RegisterPC = _startPC;

			Cpu.IFF1 = false;
			Cpu.IFF2 = false;
			Cpu.InterruptMode = 2;

			_maskOn = 1;
			_romPageHighBit = 0;
			_romPageLow3Bits = 0;
			_keyboardMask = 0;

			_displayMode = 0;
			_displayMove = 0;
			_displayX = _displayY = 0;
		}
	}
}