using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.M6502;
using System.Runtime.CompilerServices;

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
		public bool SP_SELECT = false;
		public bool unselect_reset;
		public bool unselect_select;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

			if (MemoryCallbacks.HasReads)
			{
				var flags = (uint)(MemoryCallbackFlags.AccessRead);
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}

			return temp;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void WriteMemory(ushort addr, byte value)
		{
			if (addr != _lastAddress)
			{
				DistinctAccessCount++;
				_lastAddress = addr;
			}

			_mapper.WriteMemory((ushort)(addr & 0x1FFF), value);

			if (MemoryCallbacks.HasWrites)
			{
				var flags = (uint)(MemoryCallbackFlags.AccessWrite);
				MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
			}
		}

		internal void PokeMemory(ushort addr, byte value)
		{
			_mapper.PokeMemory((ushort)(addr & 0x1FFF), value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ExecFetch(ushort addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				var flags = (uint)(MemoryCallbackFlags.AccessExecute);
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			}
		}

		private void RebootCore()
		{
			// Regenerate mapper here to make sure its state is entirely clean
			_mapper = CreateMapper(this, _game.GetOptions()["m"], Rom.Length);
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

			RomDetails = $"{_game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(Rom)}\r\n{MD5Checksum.ComputePrefixedHex(Rom)}\r\nMapper Impl \"{_mapper.GetType()}\"";

			// Some games (ex. 3D tic tac toe), turn off the screen for extended periods, so we need to allow for this here.
			if (_game.GetOptions().TryGetValue("SP_FRAME", out var spFrameStr) && spFrameStr == "true")
			{
				SP_FRAME = true;
			}
			// Some games wait for reset to be unpressed before turning the screen back on, hack unset it if needed
			if (_game.GetOptions().TryGetValue("SP_RESET", out var spResetStr) && spResetStr == "true")
			{
				SP_RESET = true;
			}
			// Ditto select (ex. Karate)
			if (_game.GetOptions().TryGetValue("SP_SELECT", out var spSelectStr) && spSelectStr == "true")
			{
				SP_SELECT = true;
			}
		}

		private static MapperBase CreateMapper(Atari2600 core, string mapperName, int romLength)
		{
			static MapperBase SetMultiCartMapper(Atari2600 core, int romLength, int gameTotal)
			{
				return (romLength / gameTotal) switch
				{
					1024 * 2 => new Multicart2K(core, gameTotal),
					1024 * 4 => new Multicart4K(core, gameTotal),
					1024 * 8 => new Multicart8K(core, gameTotal),
					_ => new Multicart4K(core, gameTotal)
				};
			}

			return mapperName switch
			{
				"2IN1" => SetMultiCartMapper(core, romLength, 2),
				"4IN1" => SetMultiCartMapper(core, romLength, 4),
				"8IN1" => SetMultiCartMapper(core, romLength, 8),
				"16IN1" => SetMultiCartMapper(core, romLength, 16),
				"32IN1" => SetMultiCartMapper(core, romLength, 32),
				"AR" => new mAR(core),
				"4K" => new m4K(core),
				"2K" => new m2K(core),
				"CM" => new mCM(core),
				"CV" => new mCV(core),
				"DPC" => new mDPC(core),
				"DPC+" => new mDPCPlus(core),
				"F8" => new mF8(core),
				"F8SC" => new mF8SC(core),
				"F6" => new mF6(core),
				"F6SC" => new mF6SC(core),
				"F4" => new mF4(core),
				"F4SC" => new mF4SC(core),
				"FE" => new mFE(core),
				"E0" => new mE0(core),
				"3F" => new m3F(core),
				"FA" => new mFA(core),
				"FA2" => new mFA2(core),
				"E7" => new mE7(core),
				"F0" => new mF0(core),
				"UA" => new mUA(core),
				"F8_sega" => new mF8_sega(core),

				// Homebrew mappers
				"3E" => new m3E(core),
				"0840" => new m0840(core),
				"MC" => new mMC(core),
				"EF" => new mEF(core),
				"EFSC" => new mEFSC(core),
				"X07" => new mX07(core),
				"4A50" => new m4A50(core),
				"SB" => new mSB(core),
				_ => throw new InvalidOperationException("mapper not supported: " + mapperName)
			};
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
				if (Tracer.IsEnabled() && Cpu.AtStart)
				{
					Tracer.Put(Cpu.TraceState());
				}

				Cpu.ExecuteOne();
				_mapper.ClockCpu();

				cyc_counter = 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

			if (unselect_select)
			{
				select = false;
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
