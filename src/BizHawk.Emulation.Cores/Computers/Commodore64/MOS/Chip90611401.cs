using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// emulates the PLA
	// which handles all bank switching
	public sealed class Chip90611401
	{
		// ------------------------------------
		public Func<int, int> PeekBasicRom;
		public Func<int, int> PeekCartridgeLo;
		public Func<int, int> PeekCartridgeHi;
		public Func<int, int> PeekCharRom;
		public Func<int, int> PeekCia0;
		public Func<int, int> PeekCia1;
		public Func<int, int> PeekColorRam;
		public Func<int, int> PeekExpansionLo;
		public Func<int, int> PeekExpansionHi;
		public Func<int, int> PeekKernalRom;
		public Func<int, int> PeekMemory;
		public Func<int, int> PeekSid;
		public Func<int, int> PeekVic;
		public Action<int, int> PokeCartridgeLo;
		public Action<int, int> PokeCartridgeHi;
		public Action<int, int> PokeCia0;
		public Action<int, int> PokeCia1;
		public Action<int, int> PokeColorRam;
		public Action<int, int> PokeExpansionLo;
		public Action<int, int> PokeExpansionHi;
		public Action<int, int> PokeMemory;
		public Action<int, int> PokeSid;
		public Action<int, int> PokeVic;
		public Func<int, int> ReadBasicRom;
		public Func<int, int> ReadCartridgeLo;
		public Func<int, int> ReadCartridgeHi;
		public Func<bool> ReadCharen;
		public Func<int, int> ReadCharRom;
		public Func<int, int> ReadCia0;
		public Func<int, int> ReadCia1;
		public Func<int, int> ReadColorRam;
		public Func<int, int> ReadExpansionLo;
		public Func<int, int> ReadExpansionHi;
		public Func<bool> ReadExRom;
		public Func<bool> ReadGame;
		public Func<bool> ReadHiRam;
		public Func<int, int> ReadKernalRom;
		public Func<bool> ReadLoRam;
		public Func<int, int> ReadMemory;
		public Func<int, int> ReadSid;
		public Func<int, int> ReadVic;
		public Action<int, int> WriteCartridgeLo;
		public Action<int, int> WriteCartridgeHi;
		public Action<int, int> WriteCia0;
		public Action<int, int> WriteCia1;
		public Action<int, int> WriteColorRam;
		public Action<int, int> WriteExpansionLo;
		public Action<int, int> WriteExpansionHi;
		public Action<int, int> WriteMemory;
		public Action<int, int> WriteSid;
		public Action<int, int> WriteVic;

		// ------------------------------------
		private enum PlaBank
		{
			None,
			Ram,
			BasicRom,
			KernalRom,
			CharRom,
			CartridgeLo,
			CartridgeHi,
			Vic,
			Sid,
			ColorRam,
			Cia0,
			Cia1,
			Expansion0,
			Expansion1
		}

		// ------------------------------------
		private bool _p24;
		private bool _p25;
		private bool _p26;
		private bool _p27;
		private bool _p28;
		private bool _loram;
		private bool _hiram;
		private bool _game;
		private bool _exrom;
		private bool _charen;
		private bool _a15;
		private bool _a14;
		private bool _a13;
		private bool _a12;

		private PlaBank Bank(int addr, bool read)
		{
			_loram = ReadLoRam();
			_hiram = ReadHiRam();
			_game = ReadGame();

			_a15 = (addr & 0x08000) != 0;
			_a14 = (addr & 0x04000) != 0;
			_a13 = (addr & 0x02000) != 0;
			_a12 = (addr & 0x01000) != 0;

			// upper memory regions 8000-FFFF
			_exrom = ReadExRom();
			if (_a15)
			{
				// io/character access
				if (_a14 && !_a13 && _a12)
				{
					// character rom, banked in at D000-DFFF
					_charen = ReadCharen();
					if (read && !_charen && (((_hiram || _loram) && _game) || (_hiram && !_exrom && !_game)))
					{
						return PlaBank.CharRom;
					}

					// io block, banked in at D000-DFFF
					if ((_charen && (_hiram || _loram)) || (_exrom && !_game))
					{
						if (addr < 0xD400)
						{
							return PlaBank.Vic;
						}

						if (addr < 0xD800)
						{
							return PlaBank.Sid;
						}

						if (addr < 0xDC00)
						{
							return PlaBank.ColorRam;
						}

						if (addr < 0xDD00)
						{
							return PlaBank.Cia0;
						}

						if (addr < 0xDE00)
						{
							return PlaBank.Cia1;
						}

						return addr < 0xDF00
							? PlaBank.Expansion0
							: PlaBank.Expansion1;
					}
				}

				// cartridge high, banked either at A000-BFFF or E000-FFFF depending
				if (_a13 && !_game && ((_hiram && !_a14 && read && !_exrom) || (_a14 && _exrom)))
				{
					return PlaBank.CartridgeHi;
				}

				// cartridge low, banked at 8000-9FFF
				if (!_a14 && !_a13 && ((_loram && _hiram && read && !_exrom) || (_exrom && !_game)))
				{
					return PlaBank.CartridgeLo;
				}

				// kernal rom, banked at E000-FFFF
				if (_hiram && _a14 && _a13 && read && (_game || (!_exrom && !_game)))
				{
					return PlaBank.KernalRom;
				}

				// basic rom, banked at A000-BFFF
				if (_loram && _hiram && !_a14 && _a13 && read && _game)
				{
					return PlaBank.BasicRom;
				}
			}

			// ultimax mode ram exclusion
			if (_exrom && !_game)
			{
				_p24 = !_a15 && !_a14 && _a12;         // 00x1 1000-1FFF, 3000-3FFF
				_p25 = !_a15 && !_a14 && _a13;         // 001x 2000-3FFF
				_p26 = !_a15 && _a14;                  // 01xx 4000-7FFF
				_p27 = _a15 && !_a14 && _a13;          // 101x A000-BFFF
				_p28 = _a15 && _a14 && !_a13 && !_a12; // 1100 C000-CFFF
				if (_p24 || _p25 || _p26 || _p27 || _p28)
				{
					return PlaBank.None;
				}
			}

			return PlaBank.Ram;
		}

		public int Peek(int addr)
		{
			switch (Bank(addr, true))
			{
				case PlaBank.BasicRom:
					return PeekBasicRom(addr);
				case PlaBank.CartridgeHi:
					return PeekCartridgeHi(addr);
				case PlaBank.CartridgeLo:
					return PeekCartridgeLo(addr);
				case PlaBank.CharRom:
					return PeekCharRom(addr);
				case PlaBank.Cia0:
					return PeekCia0(addr);
				case PlaBank.Cia1:
					return PeekCia1(addr);
				case PlaBank.ColorRam:
					return PeekColorRam(addr);
				case PlaBank.Expansion0:
					return PeekExpansionLo(addr);
				case PlaBank.Expansion1:
					return PeekExpansionHi(addr);
				case PlaBank.KernalRom:
					return PeekKernalRom(addr);
				case PlaBank.Ram:
					return PeekMemory(addr);
				case PlaBank.Sid:
					return PeekSid(addr);
				case PlaBank.Vic:
					return PeekVic(addr);
			}

			return 0xFF;
		}

		public void Poke(int addr, int val)
		{
			switch (Bank(addr, false))
			{
				case PlaBank.CartridgeHi:
					PokeCartridgeHi(addr, val);
					break;
				case PlaBank.CartridgeLo:
					PokeCartridgeLo(addr, val);
					break;
				case PlaBank.Cia0:
					PokeCia0(addr, val);
					break;
				case PlaBank.Cia1:
					PokeCia1(addr, val);
					break;
				case PlaBank.ColorRam:
					PokeColorRam(addr, val);
					break;
				case PlaBank.Expansion0:
					PokeExpansionLo(addr, val);
					break;
				case PlaBank.Expansion1:
					PokeExpansionHi(addr, val);
					break;
				case PlaBank.Ram:
					PokeMemory(addr, val);
					break;
				case PlaBank.Sid:
					PokeSid(addr, val);
					break;
				case PlaBank.Vic:
					PokeVic(addr, val);
					break;
			}
		}

		public int Read(int addr)
		{
			switch (Bank(addr, true))
			{
				case PlaBank.BasicRom:
					return ReadBasicRom(addr);
				case PlaBank.CartridgeHi:
					return ReadCartridgeHi(addr);
				case PlaBank.CartridgeLo:
					return ReadCartridgeLo(addr);
				case PlaBank.CharRom:
					return ReadCharRom(addr);
				case PlaBank.Cia0:
					return ReadCia0(addr);
				case PlaBank.Cia1:
					return ReadCia1(addr);
				case PlaBank.ColorRam:
					return ReadColorRam(addr);
				case PlaBank.Expansion0:
					return ReadExpansionLo(addr);
				case PlaBank.Expansion1:
					return ReadExpansionHi(addr);
				case PlaBank.KernalRom:
					return ReadKernalRom(addr);
				case PlaBank.Ram:
					return ReadMemory(addr);
				case PlaBank.Sid:
					return ReadSid(addr);
				case PlaBank.Vic:
					return ReadVic(addr);
			}

			return 0xFF;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_p24), ref _p24);
			ser.Sync(nameof(_p25), ref _p25);
			ser.Sync(nameof(_p26), ref _p26);
			ser.Sync(nameof(_p27), ref _p27);
			ser.Sync(nameof(_p28), ref _p28);
			ser.Sync(nameof(_loram), ref _loram);
			ser.Sync(nameof(_hiram), ref _hiram);
			ser.Sync(nameof(_game), ref _game);
			ser.Sync(nameof(_exrom), ref _exrom);
			ser.Sync(nameof(_charen), ref _charen);
			ser.Sync(nameof(_a15), ref _a15);
			ser.Sync(nameof(_a14), ref _a14);
			ser.Sync(nameof(_a13), ref _a13);
			ser.Sync(nameof(_a12), ref _a12);
		}

		public int VicRead(int addr)
		{
			_game = ReadGame();
			_exrom = ReadExRom();
			_a14 = (addr & 0x04000) == 0;
			_a13 = (addr & 0x02000) != 0;
			_a12 = (addr & 0x01000) != 0;

			// read char rom at 1000-1FFF and 9000-9FFF
			if (_a14 && !_a13 && _a12 && (_game || !_exrom))
			{
				return ReadCharRom(addr);
			}

			// read cartridge rom in ultimax mode
			if (_a13 && _a12 && _exrom && !_game)
			{
				return ReadCartridgeHi(addr);
			}

			return ReadMemory(addr);
		}

		public void Write(int addr, int val)
		{
			switch (Bank(addr, false))
			{
				case PlaBank.CartridgeHi:
					WriteCartridgeHi(addr, val);
					if (ReadGame() || !ReadExRom())
					{
						WriteMemory(addr, val);
					}

					break;
				case PlaBank.CartridgeLo:
					WriteCartridgeLo(addr, val);
					if (ReadGame() || !ReadExRom())
					{
						WriteMemory(addr, val);
					}

					break;
				case PlaBank.Cia0:
					WriteCia0(addr, val);
					break;
				case PlaBank.Cia1:
					WriteCia1(addr, val);
					break;
				case PlaBank.ColorRam:
					WriteColorRam(addr, val);
					break;
				case PlaBank.Expansion0:
					WriteExpansionLo(addr, val);
					return;
				case PlaBank.Expansion1:
					WriteExpansionHi(addr, val);
					return;
				case PlaBank.Ram:
					WriteMemory(addr, val);
					break;
				case PlaBank.Sid:
					WriteSid(addr, val);
					break;
				case PlaBank.Vic:
					WriteVic(addr, val);
					break;
			}
		}
	}
}
