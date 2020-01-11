using System;

using BizHawk.Common.NumberExtensions;
using BizHawk.Common.BufferExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600
	{
		private readonly GameInfo _game;

		public TIA _tia;
		public M6532 _m6532;
		private DCFilter _dcfilter;
		private MapperBase _mapper;
		private byte[] _ram;

		private IController _controller = NullController.Instance;
		private int _frame;
		private int _lastAddress;

		private bool _leftDifficultySwitchPressed;
		private bool _rightDifficultySwitchPressed;

		private bool _leftDifficultySwitchHeld;
		private bool _rightDifficultySwitchHeld;

		internal MOS6502X<CpuLink> Cpu { get; private set; }
		internal byte[] Ram => _ram;
		internal byte[] Rom { get; }
		internal int DistinctAccessCount { get; private set; }

		public bool SP_FRAME = false;
		public bool SP_RESET = false;
		public bool unselect_reset;

		internal struct CpuLink : IMOS6502XLink
		{
			private readonly Atari2600 _atari2600;

			public CpuLink(Atari2600 atari2600)
			{
				_atari2600 = atari2600;
			}

			public byte DummyReadMemory(ushort address) => _atari2600.ReadMemory(address);

			public void OnExecFetch(ushort address) => _atari2600.ExecFetch(address);

			public byte PeekMemory(ushort address) => _atari2600.ReadMemory(address);

			public byte ReadMemory(ushort address) => _atari2600.ReadMemory(address);

			public void WriteMemory(ushort address, byte value) => _atari2600.WriteMemory(address, value);
		}

		// keeps track of tia cycles, 3 cycles per CPU cycle
		private int cyc_counter;

		private static MapperBase SetMultiCartMapper(int romLength, int gameTotal)
		{
			switch (romLength / gameTotal)
			{
				case 1024 * 2: // 2K
					return new Multicart2K(gameTotal);
				default:
				case 1024 * 4: // 4K
					return new Multicart4K(gameTotal);
				case 1024 * 8: // 8K
					return new Multicart8K(gameTotal);
			}
		}

		internal byte BaseReadMemory(ushort addr)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				return _tia.ReadMemory(addr, false);
			}

			if ((addr & 0x1080) == 0x0080)
			{
				_tia.BusState = _m6532.ReadMemory(addr, false);
				return _m6532.ReadMemory(addr, false);
			}

			_tia.BusState = Rom[addr & 0x0FFF];
			return Rom[addr & 0x0FFF];
		}

		internal byte BasePeekMemory(ushort addr)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				return _tia.ReadMemory(addr, true);
			}

			if ((addr & 0x1080) == 0x0080)
			{
				return _m6532.ReadMemory(addr, true);
			}

			return Rom[addr & 0x0FFF];
		}

		internal void BaseWriteMemory(ushort addr, byte value)
		{
			_tia.BusState = value;
			if (addr != _lastAddress)
			{
				DistinctAccessCount++;
				_lastAddress = addr;
			}

			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				_tia.WriteMemory(addr, value, false);
			}
			else if ((addr & 0x1080) == 0x0080)
			{
				_m6532.WriteMemory(addr, value);
			}
			else
			{
				Console.WriteLine("ROM write(?):  " + addr.ToString("x"));
			}
		}

		internal void BasePokeMemory(ushort addr, byte value)
		{
			addr = (ushort)(addr & 0x1FFF);
			if ((addr & 0x1080) == 0)
			{
				_tia.WriteMemory(addr, value, true);
			}
			else if ((addr & 0x1080) == 0x0080)
			{
				_m6532.WriteMemory(addr, value);
			}
			else
			{
				Console.WriteLine("ROM write(?):  " + addr.ToString("x"));
			}
		}

		private byte ReadMemory(ushort addr)
		{
			if (addr != _lastAddress)
			{
				DistinctAccessCount++;
				_lastAddress = addr;
			}

			_mapper.Bit13 = addr.Bit(13);
			var temp = _mapper.ReadMemory((ushort)(addr & 0x1FFF));
			_tia.BusState = temp;
			var flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");

			return temp;
		}

		private byte PeekMemory(ushort addr)
		{
			var temp = _mapper.PeekMemory((ushort)(addr & 0x1FFF));
			return temp;
		}

		private void WriteMemory(ushort addr, byte value)
		{
			if (addr != _lastAddress)
			{
				DistinctAccessCount++;
				_lastAddress = addr;
			}

			_mapper.WriteMemory((ushort)(addr & 0x1FFF), value);
			var flags = (uint)(MemoryCallbackFlags.AccessWrite);
			MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
		}

		internal void PokeMemory(ushort addr, byte value)
		{
			_mapper.PokeMemory((ushort)(addr & 0x1FFF), value);
		}

		private void ExecFetch(ushort addr)
		{
			var flags = (uint)(MemoryCallbackFlags.AccessExecute);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		}

		private void RebootCore()
		{
			// Regenerate mapper here to make sure its state is entirely clean
			switch (_game.GetOptionsDict()["m"])
			{
				case "2IN1":
					_mapper = SetMultiCartMapper(Rom.Length, 2);
					break;
				case "4IN1":
					_mapper = SetMultiCartMapper(Rom.Length, 4);
					break;
				case "8IN1":
					_mapper = SetMultiCartMapper(Rom.Length, 8);
					break;
				case "16IN1":
					_mapper = SetMultiCartMapper(Rom.Length, 16);
					break;
				case "32IN1":
					_mapper = SetMultiCartMapper(Rom.Length, 32);
					break;
				case "AR":
					_mapper = new mAR(this); // This mapper has to set up configurations in the contructor.
					break;
				case "4K":
					_mapper = new m4K();
					break;
				case "2K":
					_mapper = new m2K();
					break;
				case "CM":
					_mapper = new mCM();
					break;
				case "CV":
					_mapper = new mCV();
					break;
				case "DPC":
					_mapper = new mDPC();
					break;
				case "DPC+":
					_mapper = new mDPCPlus();
					break;
				case "F8":
					_mapper = new mF8();
					break;
				case "F8SC":
					_mapper = new mF8SC();
					break;
				case "F6":
					_mapper = new mF6();
					break;
				case "F6SC":
					_mapper = new mF6SC();
					break;
				case "F4":
					_mapper = new mF4();
					break;
				case "F4SC":
					_mapper = new mF4SC();
					break;
				case "FE":
					_mapper = new mFE();
					break;
				case "E0":
					_mapper = new mE0();
					break;
				case "3F":
					_mapper = new m3F();
					break;
				case "FA":
					_mapper = new mFA();
					break;
				case "FA2":
					_mapper = new mFA2();
					break;
				case "E7":
					_mapper = new mE7();
					break;
				case "F0":
					_mapper = new mF0();
					break;
				case "UA":
					_mapper = new mUA();
					break;

				// Special Sega Mapper which has swapped banks
				case "F8_sega":
					_mapper = new mF8_sega();
					break;

				// Homebrew mappers
				case "3E":
					_mapper = new m3E();
					break;
				case "0840":
					_mapper = new m0840();
					break;
				case "MC":
					_mapper = new mMC();
					break;
				case "EF":
					_mapper = new mEF();
					break;
				case "EFSC":
					_mapper = new mEFSC();
					break;
				case "X07":
					_mapper = new mX07();
					break;
				case "4A50":
					_mapper = new m4A50();
					break;
				case "SB":
					_mapper = new mSB();
					break;

				default:
					throw new InvalidOperationException("mapper not supported: " + _game.GetOptionsDict()["m"]);
			}

			_mapper.Core = this;

			_lagCount = 0;
			Cpu = new MOS6502X<CpuLink>(new CpuLink(this));

			if (_game["PAL"])
			{
				_pal = true;
			}
			else if (_game["NTSC"])
			{
				_pal = false;
			}
			else
			{
				_pal = DetectPal(_game, Rom);
			}

			// dcfilter coefficent is from real observed hardware behavior: a latched "1" will fully decay by ~170 or so tia sound cycles
			_tia = new TIA(this, _pal, Settings.SECAMColors);

			_dcfilter = new DCFilter(_tia, 256);

			_m6532 = new M6532(this);

			HardReset();

			// Show mapper class on romstatusdetails
			CoreComm.RomStatusDetails = $"{this._game.Name}\r\nSHA1:{Rom.HashSHA1()}\r\nMD5:{Rom.HashMD5()}\r\nMapper Impl \"{_mapper.GetType()}\"";

			// Some games (ex. 3D tic tac toe), turn off the screen for extended periods, so we need to allow for this here.
			if (_game.GetOptionsDict().ContainsKey("SP_FRAME"))
			{
				if (_game.GetOptionsDict()["SP_FRAME"] == "true")
				{
					SP_FRAME = true;
				}
			}
			if (_game.GetOptionsDict().ContainsKey("SP_RESET"))
			{
				if (_game.GetOptionsDict()["SP_RESET"] == "true")
				{
					SP_RESET = true;
				}
			}
		}

		private bool _pal;

		private void HardReset()
		{
			_ram = new byte[128];
			_mapper.HardReset();

			Cpu = new MOS6502X<CpuLink>(new CpuLink(this));

			_tia.Reset();
			_m6532 = new M6532(this);
			SetupMemoryDomains();
			cyc_counter = 0;
		}

		private void Cycle()
		{
			_tia.Execute();
			cyc_counter++;
			if (cyc_counter == 3)
			{
				_m6532.Timer.Tick();
				if (Tracer.Enabled && Cpu.AtStart)
				{
					Tracer.Put(Cpu.TraceState());
				}

				Cpu.ExecuteOne();
				_mapper.ClockCpu();

				cyc_counter = 0;
			}
		}

		internal byte ReadControls1(bool peek)
		{
			InputCallbacks.Call();
			
			byte value = _controllerDeck.ReadPort1(_controller);

			if (!peek)
			{
				_islag = false;
			}

			return value;
		}

		internal byte ReadControls2(bool peek)
		{
			InputCallbacks.Call();
			byte value = _controllerDeck.ReadPort2(_controller);

			if (!peek)
			{
				_islag = false;
			}

			return value;
		}

		internal int ReadPot1(int pot)
		{
			int value = _controllerDeck.ReadPot1(_controller, pot);

			return value;
		}

		internal int ReadPot2(int pot)
		{
			int value = _controllerDeck.ReadPot2(_controller, pot);

			return value;
		}

		internal byte ReadConsoleSwitches(bool peek)
		{
			byte value = 0xFF;
			bool select = _controller.IsPressed("Select");
			bool reset = _controller.IsPressed("Reset");

			if (unselect_reset)
			{
				reset = false;
			}

			if (reset) { value &= 0xFE; }
			if (select) { value &= 0xFD; }
			if (SyncSettings.BW) { value &= 0xF7; }
			if (_leftDifficultySwitchPressed)
			{
				value &= 0xBF;
			}

			if (_rightDifficultySwitchPressed)
			{
				value &= 0x7F;
			}

			if (!peek)
			{
				_islag = false;
			}

			return value;
		}
	}
}
