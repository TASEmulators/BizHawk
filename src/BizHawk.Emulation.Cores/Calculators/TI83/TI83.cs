using System.IO;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

// http://www.ticalc.org/pub/text/calcinfo/
namespace BizHawk.Emulation.Cores.Calculators.TI83
{
	[Core(CoreNames.TI83Hawk, "zeromus")]
	[ServiceNotApplicable(typeof(IBoardInfo), typeof(IRegionable), typeof(ISaveRam), typeof(ISoundProvider))]
	public partial class TI83 : TI83Common, IEmulator, IVideoProvider, IDebuggable, IInputPollable
	{
		[CoreConstructor(VSystemID.Raw.TI83)]
		public TI83(CoreLoadParameters<TI83CommonSettings, object> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			PutSettings(lp.Settings ?? new TI83CommonSettings());

			_cpu = new Z80A<CpuLink>(new CpuLink(this));

			_rom = lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("TI83", "Rom"));
			LinkPort = new TI83LinkPort(this);

			HardReset();
			SetupMemoryDomains();

			_tracer = new TraceBuffer(_cpu.TraceHeader);

			ser.Register<ITraceable>(_tracer);
			ser.Register<IDisassemblable>(_cpu);
			ser.Register<IStatable>(new StateSerializer(SyncState));
			LinkPort.SendFileToCalc(new MemoryStream(lp.Roms[0].RomData, false), false);
		}

		private readonly TraceBuffer _tracer;

		private readonly Z80A<CpuLink> _cpu;
		private readonly byte[] _rom;

		// configuration
		private IController _controller = NullController.Instance;

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

		public bool ON_key_int, ON_key_int_EN;
		public bool TIM_1_int, TIM_1_int_EN;
		public int TIM_frq, TIM_mult, TIM_count, TIM_hit;

		// Link Cable
		public TI83LinkPort LinkPort { get; }

		private int _linkOutput;

		internal int LinkOutput => _linkOutput;
		internal bool LinkActive { get; set; }
		internal int LinkInput { get; set; }

		internal int LinkState => (_linkOutput | LinkInput) ^ 3;

		private static readonly ControllerDefinition TI83Controller = new ControllerDefinition("TI83 Controller")
		{
			BoolButtons =
			{
				"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "DOT",
				"ON", "ENTER",
				"DOWN", "LEFT", "UP", "RIGHT",
				"PLUS", "MINUS", "MULTIPLY", "DIVIDE",
				"CLEAR", "EXP", "DASH", "PARACLOSE", "TAN", "VARS", "PARAOPEN",
				"COS", "PRGM", "STAT", "COMMA", "SIN", "MATRIX", "X",
				"STO", "LN", "LOG", "SQUARED", "NEG1", "MATH", "ALPHA",
				"GRAPH", "TRACE", "ZOOM", "WINDOW", "Y", "2ND", "MODE", "DEL",
			},
		}.MakeImmutable();

		private byte ReadMemory(ushort addr)
		{
			byte ret;
			int romPage = _romPageLow3Bits | (_romPageHighBit << 3);

			if (addr < 0x4000)
			{
				ret = _rom[addr]; // ROM zero-page
			}
			else if (addr < 0x8000)
			{
				ret = _rom[(romPage * 0x4000) + addr - 0x4000]; // other rom page
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
				// ROM zero-page
			}
			else if (addr < 0x8000)
			{
				// other rom page
			}
			else
			{
				_ram[addr - 0x8000] = value;
			}
		}

		private void WriteHardware(ushort addr, byte value)
		{
			addr &= 0xFF;

			switch (addr)
			{
				case 0: // PORT_LINK
					_romPageHighBit = (value >> 4) & 1;
					_linkOutput = value & 3;

					if (LinkActive)
					{
						// Prevent rom calls from disturbing link port activity
						if (LinkActive && _cpu.RegPC < 0x4000)
						{
							return;
						}

						LinkPort.Update();
					}

					break;
				case 1: // PORT_KEYBOARD:
					_lagged = false;
					_keyboardMask = value;
					////Console.WriteLine("write PORT_KEYBOARD {0:X2}",value);
					break;
				case 2: // PORT_ROMPAGE
					_romPageLow3Bits = value & 0x7;
					break;
				case 3: // PORT_STATUS
					// controls ON key interrupts
					if ((value & 0x1) == 0)
					{
						ON_key_int = false;
						ON_key_int_EN = false;
					}
					else
					{
						ON_key_int_EN = true;
					}

					// controls first timer interrupts
					if ((value & 0x2) == 0)
					{
						TIM_1_int = false;
						TIM_1_int_EN = false;
					}
					else
					{
						TIM_1_int_EN = true;
					}

					// controls second timer, not yet implemented and unclear how to differentiate
					if ((value & 0x4) == 0)
					{
					}
					else
					{
					}

					// controls low power mode, not yet implemeneted
					if ((value & 0x8) == 0)
					{
					}
					else
					{
					}
					break;
				case 4: // PORT_INTCTRL
					// controls ON key interrupts
					TIM_frq = value & 6;

					TIM_mult = ((value & 0x10) == 0x10) ? 1800 : 1620;

					TIM_hit = (int) Math.Floor(6000000.0 / Math.Floor((double) TIM_mult / (2 * TIM_frq + 3)));

					// Bit 0 is some form of memory mapping

					// Bit 5 controls reset

					// Bit 6-7 controls battery power compare (not implemented, will always return full power)

					break;
				case 16: // PORT_DISPCTRL
						 ////Console.WriteLine("write PORT_DISPCTRL {0}",value);
					WriteDispCtrl(value);
					break;
				case 17: // PORT_DISPDATA
						 ////Console.WriteLine("write PORT_DISPDATA {0}",value);
					WriteDispData(value);
					break;
			}
		}

		private byte ReadHardware(ushort addr)
		{
			addr &= 0xFF;

			switch (addr)
			{
				case 0: // PORT_LINK
					LinkPort.Update();
					return (byte)((_romPageHighBit << 4) | (LinkState << 2) | LinkOutput);
				case 1: // PORT_KEYBOARD:
						////Console.WriteLine("read PORT_KEYBOARD");
					return ReadKeyboard();
				case 2: // PORT_ROMPAGE
					return (byte)_romPageLow3Bits;
				case 3: // PORT_STATUS
					{
						// Console.WriteLine("read PORT_STATUS");
						// Bits:
						// 0   - Set if ON key Interrupt generated
						// 1   - Update things (keyboard etc)
						// 2   - Unknown, but used
						// 3   - Set if ON key is up
						// 4-7 - Unknown

						return (byte)((_controller.IsPressed("ON") ? 0 : 8) | 
									  (TIM_1_int ? 2 : 0) |
									  (ON_key_int ? 1 : 0));
					}

				case 4: // PORT_INTCTRL
					// returns mirror of link port
					return (byte)((_romPageHighBit << 4) | (LinkState << 2) | LinkOutput);

				case 16: // PORT_DISPCTRL
					// Console.WriteLine("read DISPCTRL");
					break;

				case 17: // PORT_DISPDATA
					return ReadDispData();
			}

			return 0xFF;
		}

		private byte ReadKeyboard()
		{
			InputCallbacks.Call();

			// ref TI-9X
			int ret = 0xFF;
			////Console.WriteLine("keyboardMask: {0:X2}",keyboardMask);
			if ((_keyboardMask & 1) == 0)
			{
				if (_controller.IsPressed("DOWN")) ret ^= 1;
				if (_controller.IsPressed("LEFT")) ret ^= 2;
				if (_controller.IsPressed("RIGHT")) ret ^= 4;
				if (_controller.IsPressed("UP")) ret ^= 8;
			}

			if ((_keyboardMask & 2) == 0)
			{
				if (_controller.IsPressed("ENTER")) ret ^= 1;
				if (_controller.IsPressed("PLUS")) ret ^= 2;
				if (_controller.IsPressed("MINUS")) ret ^= 4;
				if (_controller.IsPressed("MULTIPLY")) ret ^= 8;
				if (_controller.IsPressed("DIVIDE")) ret ^= 16;
				if (_controller.IsPressed("EXP")) ret ^= 32;
				if (_controller.IsPressed("CLEAR")) ret ^= 64;
			}

			if ((_keyboardMask & 4) == 0)
			{
				if (_controller.IsPressed("DASH")) ret ^= 1;
				if (_controller.IsPressed("3")) ret ^= 2;
				if (_controller.IsPressed("6")) ret ^= 4;
				if (_controller.IsPressed("9")) ret ^= 8;
				if (_controller.IsPressed("PARACLOSE")) ret ^= 16;
				if (_controller.IsPressed("TAN")) ret ^= 32;
				if (_controller.IsPressed("VARS")) ret ^= 64;
			}

			if ((_keyboardMask & 8) == 0)
			{
				if (_controller.IsPressed("DOT")) ret ^= 1;
				if (_controller.IsPressed("2")) ret ^= 2;
				if (_controller.IsPressed("5")) ret ^= 4;
				if (_controller.IsPressed("8")) ret ^= 8;
				if (_controller.IsPressed("PARAOPEN")) ret ^= 16;
				if (_controller.IsPressed("COS")) ret ^= 32;
				if (_controller.IsPressed("PRGM")) ret ^= 64;
				if (_controller.IsPressed("STAT")) ret ^= 128;
			}

			if ((_keyboardMask & 16) == 0)
			{
				if (_controller.IsPressed("0")) ret ^= 1;
				if (_controller.IsPressed("1")) ret ^= 2;
				if (_controller.IsPressed("4")) ret ^= 4;
				if (_controller.IsPressed("7")) ret ^= 8;
				if (_controller.IsPressed("COMMA")) ret ^= 16;
				if (_controller.IsPressed("SIN")) ret ^= 32;
				if (_controller.IsPressed("MATRIX")) ret ^= 64;
				if (_controller.IsPressed("X")) ret ^= 128;
			}

			if ((_keyboardMask & 32) == 0)
			{
				if (_controller.IsPressed("STO")) ret ^= 2;
				if (_controller.IsPressed("LN")) ret ^= 4;
				if (_controller.IsPressed("LOG")) ret ^= 8;
				if (_controller.IsPressed("SQUARED")) ret ^= 16;
				if (_controller.IsPressed("NEG1")) ret ^= 32;
				if (_controller.IsPressed("MATH")) ret ^= 64;
				if (_controller.IsPressed("ALPHA")) ret ^= 128;
			}

			if ((_keyboardMask & 64) == 0)
			{
				if (_controller.IsPressed("GRAPH")) ret ^= 1;
				if (_controller.IsPressed("TRACE")) ret ^= 2;
				if (_controller.IsPressed("ZOOM")) ret ^= 4;
				if (_controller.IsPressed("WINDOW")) ret ^= 8;
				if (_controller.IsPressed("Y")) ret ^= 16;
				if (_controller.IsPressed("2ND")) ret ^= 32;
				if (_controller.IsPressed("MODE")) ret ^= 64;
				if (_controller.IsPressed("DEL")) ret ^= 128;
			}

			return (byte)ret;
		}

		private byte ReadDispData()
		{
			if (_cursorMoved)
			{
				_cursorMoved = false;
				return 0x00; // not accurate this should be stale data or something
			}

			byte ret;
			if (_displayMode == 1)
			{
				ret = _vram[(_displayY * 12) + _displayX];
			}
			else
			{
				int column = 6 * (int)_displayX;
				int offset = (int)(_displayY * 12) + (column >> 3);
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
				offset = (int)(_displayY * 12) + (int)_displayX;
				_vram[offset] = value;
			}
			else
			{
				int column = 6 * (int)_displayX;
				offset = (int)(_displayY * 12) + (column >> 3);
				if (offset < 0x300)
				{
					int shift = column & 7;
					int mask = ~(252 >> shift);
					int data = value << 2;
					_vram[offset] = (byte)(_vram[offset] & mask | (data >> shift));
					if (shift > 2 && offset < 0x2ff)
					{
						offset++;

						shift = 8 - shift;

						mask = ~(252 << shift);
						_vram[offset] = (byte)(_vram[offset] & mask | (data << shift));
					}
				}
			}

			DoDispMove();
		}

		private void DoDispMove()
		{
			switch (_displayMove)
			{
				case 0:
					_displayY--;
					break;
				case 1:
					_displayY++;
					break;
				case 2:
					_displayX--;
					break;
				case 3:
					_displayX++;
					break;
			}

			_displayX &= 0xF; // 0xF or 0x1F? dunno
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
				// hardware scroll
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
				// contrast
			}
			else if (value == 2)
			{
			}
			else if (value == 3)
			{
			}
		}

		private void IRQCallback()
		{
			//Console.WriteLine("IRQ with vec {0} and cpu.InterruptMode {1}", _cpu.Regs[_cpu.I], _cpu.InterruptMode);
			_cpu.FlagI = false;
		}

		private void NMICallback()
		{
			//Console.WriteLine("NMI");
			_cpu.NonMaskableInterrupt = false;
		}

		private void HardReset()
		{
			_cpu.Reset();
			_ram = new byte[0x8000];
			for (int i = 0; i < 0x8000; i++)
			{
				_ram[i] = 0xFF;
			}

			_cpu.RegPC = 0;

			_cpu.IFF1 = false;
			_cpu.IFF2 = false;
			_cpu.InterruptMode = 2;

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
