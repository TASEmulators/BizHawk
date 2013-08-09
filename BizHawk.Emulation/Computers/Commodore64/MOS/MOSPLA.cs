using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// emulates the PLA
	// which handles all bank switching

	public class MOSPLA
	{
		// ------------------------------------

		public Func<int, byte> PeekBasicRom;
		public Func<int, byte> PeekCartridgeLo;
		public Func<int, byte> PeekCartridgeHi;
		public Func<int, byte> PeekCharRom;
		public Func<int, byte> PeekCia0;
		public Func<int, byte> PeekCia1;
		public Func<int, byte> PeekColorRam;
		public Func<int, byte> PeekExpansionLo;
		public Func<int, byte> PeekExpansionHi;
		public Func<int, byte> PeekKernalRom;
		public Func<int, byte> PeekMemory;
		public Func<int, byte> PeekSid;
		public Func<int, byte> PeekVic;
		public Action<int, byte> PokeCartridgeLo;
		public Action<int, byte> PokeCartridgeHi;
		public Action<int, byte> PokeCia0;
		public Action<int, byte> PokeCia1;
		public Action<int, byte> PokeColorRam;
		public Action<int, byte> PokeExpansionLo;
		public Action<int, byte> PokeExpansionHi;
		public Action<int, byte> PokeMemory;
		public Action<int, byte> PokeSid;
		public Action<int, byte> PokeVic;
		public Func<ushort, byte> ReadBasicRom;
		public Func<ushort, byte> ReadCartridgeLo;
		public Func<ushort, byte> ReadCartridgeHi;
		public Func<bool> ReadCharen;
		public Func<ushort, byte> ReadCharRom;
		public Func<ushort, byte> ReadCia0;
		public Func<ushort, byte> ReadCia1;
		public Func<ushort, byte> ReadColorRam;
		public Func<ushort, byte> ReadExpansionLo;
		public Func<ushort, byte> ReadExpansionHi;
		public Func<bool> ReadExRom;
		public Func<bool> ReadGame;
		public Func<bool> ReadHiRam;
		public Func<ushort, byte> ReadKernalRom;
		public Func<bool> ReadLoRam;
		public Func<ushort, byte> ReadMemory;
		public Func<ushort, byte> ReadSid;
		public Func<ushort, byte> ReadVic;
		public Action<ushort, byte> WriteCartridgeLo;
		public Action<ushort, byte> WriteCartridgeHi;
		public Action<ushort, byte> WriteCia0;
		public Action<ushort, byte> WriteCia1;
		public Action<ushort, byte> WriteColorRam;
		public Action<ushort, byte> WriteExpansionLo;
		public Action<ushort, byte> WriteExpansionHi;
		public Action<ushort, byte> WriteMemory;
		public Action<ushort, byte> WriteSid;
		public Action<ushort, byte> WriteVic;
	
		// ------------------------------------

		private enum PLABank
		{
			None,
			RAM,
			BasicROM,
			KernalROM,
			CharROM,
			IO,
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

		private struct PLACpuMap
		{
			public PLABank layout1000;
			public PLABank layout8000;
			public PLABank layoutA000;
			public PLABank layoutC000;
			public PLABank layoutD000;
			public PLABank layoutE000;
		}

		// ------------------------------------

		private PLACpuMap map;
		private bool pinCharenLast;
		private bool pinExRomLast;
		private bool pinGameLast;
		private bool pinHiRamLast;
		private bool pinLoRamLast;

		public MOSPLA()
		{
		}

		public void HardReset()
		{
			UpdateMap();
		}

		// ------------------------------------

		public void ExecutePhase1()
		{
			UpdatePins();
		}

		public void ExecutePhase2()
		{
			UpdatePins();
		}

		public void UpdatePins()
		{
			if ((ReadExRom() != pinExRomLast) || (ReadGame() != pinGameLast) || (ReadLoRam() != pinLoRamLast) || (ReadHiRam() != pinHiRamLast) || (ReadCharen() != pinCharenLast))
			{
				UpdateMap();
			}
		}

		// ------------------------------------

		private void UpdateMap()
		{
			bool pinGame = ReadGame();
			bool pinExRom = ReadExRom();
			bool pinCharen = ReadCharen();
			bool pinHiRam = ReadHiRam();
			bool pinLoRam = ReadLoRam();

			if (pinCharen && pinHiRam && pinLoRam && pinGame && pinExRom)
			{
				// 11111
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.BasicROM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (!pinCharen && pinHiRam && pinLoRam && pinGame && pinExRom)
			{
				// 01111
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.BasicROM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.CharROM;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (pinCharen && !pinHiRam && pinLoRam && pinGame)
			{
				// 1011X
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.RAM;
			}
			else if (pinCharen && !pinHiRam && pinLoRam && !pinGame && !pinExRom)
			{
				// 10100
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.RAM;
			}
			else if (!pinCharen && !pinHiRam && pinLoRam && pinGame)
			{
				// 0011X
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.CharROM;
				map.layoutE000 = PLABank.RAM;
			}
			else if (!pinCharen && !pinHiRam && pinLoRam && !pinGame && !pinExRom)
			{
				// 00100
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.RAM;
				map.layoutE000 = PLABank.RAM;
			}
			else if (!pinHiRam && !pinLoRam && pinGame)
			{
				// X001X
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.RAM;
				map.layoutE000 = PLABank.RAM;
			}
			else if (pinCharen && pinHiRam && !pinLoRam && pinGame)
			{
				// 1101X
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (pinCharen && !pinHiRam && !pinLoRam && !pinExRom)
			{
				// 100X0
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (!pinCharen && pinHiRam && !pinLoRam && pinGame)
			{
				// 0101X
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.CharROM;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (!pinCharen && !pinHiRam && !pinLoRam && !pinExRom)
			{
				// 000X0
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.RAM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.CharROM;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (pinCharen && pinHiRam && pinLoRam && pinGame && !pinExRom)
			{
				// 11110
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.CartridgeLo;
				map.layoutA000 = PLABank.BasicROM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (!pinCharen && pinHiRam && pinLoRam && pinGame && !pinExRom)
			{
				// 01110
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.CartridgeLo;
				map.layoutA000 = PLABank.BasicROM;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.CharROM;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (pinCharen && pinHiRam && !pinLoRam && !pinGame && !pinExRom)
			{
				// 11000
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.CartridgeHi;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (!pinCharen && pinHiRam && !pinLoRam && !pinGame && !pinExRom)
			{
				// 01000
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.RAM;
				map.layoutA000 = PLABank.CartridgeHi;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.CharROM;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (pinCharen && pinHiRam && pinLoRam && !pinGame && !pinExRom)
			{
				// 11100
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.CartridgeLo;
				map.layoutA000 = PLABank.CartridgeHi;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (!pinCharen && pinHiRam && pinLoRam && !pinGame && !pinExRom)
			{
				// 01100
				map.layout1000 = PLABank.RAM;
				map.layout8000 = PLABank.CartridgeLo;
				map.layoutA000 = PLABank.CartridgeHi;
				map.layoutC000 = PLABank.RAM;
				map.layoutD000 = PLABank.CharROM;
				map.layoutE000 = PLABank.KernalROM;
			}
			else if (!pinGame && pinExRom)
			{
				// XXX01 (ultimax)
				map.layout1000 = PLABank.None;
				map.layout8000 = PLABank.CartridgeLo;
				map.layoutA000 = PLABank.None;
				map.layoutC000 = PLABank.None;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.CartridgeHi;
			}
			else
			{
				throw new Exception("Memory configuration missing from PLA, fix this!");
			}

			pinExRomLast = pinExRom;
			pinGameLast = pinGame;
			pinLoRamLast = pinLoRam;
			pinHiRamLast = pinHiRam;
			pinCharenLast = pinCharen;
		}

		// ------------------------------------

		private PLABank Bank(ushort addr)
		{
			if (addr < 0x1000)
				return PLABank.RAM;
			else if (addr >= 0x1000 && addr < 0x8000)
				return map.layout1000;
			else if (addr >= 0x8000 && addr < 0xA000)
				return map.layout8000;
			else if (addr >= 0xA000 && addr < 0xC000)
				return map.layoutA000;
			else if (addr >= 0xC000 && addr < 0xD000)
				return map.layoutC000;
			else if (addr >= 0xD000 && addr < 0xE000)
			{
				if (map.layoutD000 == PLABank.IO)
				{
					if (addr >= 0xD000 && addr < 0xD400)
						return PLABank.Vic;
					else if (addr >= 0xD400 && addr < 0xD800)
						return PLABank.Sid;
					else if (addr >= 0xD800 && addr < 0xDC00)
						return PLABank.ColorRam;
					else if (addr >= 0xDC00 && addr < 0xDD00)
						return PLABank.Cia0;
					else if (addr >= 0xDD00 && addr < 0xDE00)
						return PLABank.Cia1;
					else if (addr >= 0xDE00 && addr < 0xDF00)
						return PLABank.Expansion0;
					else
						return PLABank.Expansion1;
				}
				else
				{
					return map.layoutD000;
				}
			}
			else
			{
				return map.layoutE000;
			}
		}

		public byte Peek(int addr)
		{
			switch (Bank((ushort)(addr & 0xFFFF)))
			{
				case PLABank.BasicROM:
					return PeekBasicRom(addr);
				case PLABank.CartridgeHi:
					return PeekCartridgeHi(addr);
				case PLABank.CartridgeLo:
					return PeekCartridgeLo(addr);
				case PLABank.CharROM:
					return PeekCharRom(addr);
				case PLABank.Cia0:
					return PeekCia0(addr);
				case PLABank.Cia1:
					return PeekCia1(addr);
				case PLABank.ColorRam:
					return PeekColorRam(addr);
				case PLABank.Expansion0:
					return PeekExpansionLo(addr);
				case PLABank.Expansion1:
					return PeekExpansionHi(addr);
				case PLABank.KernalROM:
					return PeekKernalRom(addr);
				case PLABank.None:
					return 0xFF;
				case PLABank.RAM:
					return PeekMemory(addr);
				case PLABank.Sid:
					return PeekSid(addr);
				case PLABank.Vic:
					return PeekVic(addr);
			}
			return 0xFF;
		}

		public void Poke(int addr, byte val)
		{
			switch (Bank((ushort)(addr & 0xFFFF)))
			{
				case PLABank.BasicROM:
					break;
				case PLABank.CartridgeHi:
					PokeCartridgeHi(addr, val);
					break;
				case PLABank.CartridgeLo:
					PokeCartridgeLo(addr, val);
					break;
				case PLABank.CharROM:
					break;
				case PLABank.Cia0:
					PokeCia0(addr, val);
					break;
				case PLABank.Cia1:
					PokeCia1(addr, val);
					break;
				case PLABank.ColorRam:
					PokeColorRam(addr, val);
					break;
				case PLABank.Expansion0:
					PokeExpansionLo(addr, val);
					break;
				case PLABank.Expansion1:
					PokeExpansionHi(addr, val);
					break;
				case PLABank.KernalROM:
					break;
				case PLABank.None:
					break;
				case PLABank.RAM:
					PokeMemory(addr, val);
					break;
				case PLABank.Sid:
					PokeSid(addr, val);
					break;
				case PLABank.Vic:
					PokeVic(addr, val);
					break;
			}
		}

		public byte Read(ushort addr)
		{
			switch (Bank(addr))
			{
				case PLABank.BasicROM:
					return ReadBasicRom(addr);
				case PLABank.CartridgeHi:
					return ReadCartridgeHi(addr);
				case PLABank.CartridgeLo:
					return ReadCartridgeLo(addr);
				case PLABank.CharROM:
					return ReadCharRom(addr);
				case PLABank.Cia0:
					return ReadCia0(addr);
				case PLABank.Cia1:
					return ReadCia1(addr);
				case PLABank.ColorRam:
					return ReadColorRam(addr);
				case PLABank.Expansion0:
					return ReadExpansionLo(addr);
				case PLABank.Expansion1:
					return ReadExpansionHi(addr);
				case PLABank.KernalROM:
					return ReadKernalRom(addr);
				case PLABank.RAM:
					return ReadMemory(addr);
				case PLABank.Sid:
					return ReadSid(addr);
				case PLABank.Vic:
					return ReadVic(addr);
			}
			return 0xFF;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("pinCharenLast", ref pinCharenLast);
			ser.Sync("pinExRomLast", ref pinExRomLast);
			ser.Sync("pinGameLast", ref pinGameLast);
			ser.Sync("pinHiRamLast", ref pinHiRamLast);
			ser.Sync("pinLoRamLast", ref pinLoRamLast);

			if (ser.IsReader) UpdateMap();
		}

		public void Write(ushort addr, byte val)
		{
			switch (Bank(addr))
			{
				case PLABank.BasicROM:
					break;
				case PLABank.CartridgeHi:
					WriteCartridgeHi(addr, val);
					break;
				case PLABank.CartridgeLo:
					WriteCartridgeLo(addr, val);
					break;
				case PLABank.CharROM:
					break;
				case PLABank.Cia0:
					WriteCia0(addr, val);
					break;
				case PLABank.Cia1:
					WriteCia1(addr, val);
					break;
				case PLABank.ColorRam:
					WriteColorRam(addr, val);
					break;
				case PLABank.Expansion0:
					WriteExpansionLo(addr, val);
					return;
				case PLABank.Expansion1:
					WriteExpansionHi(addr, val);
                    return;
				case PLABank.KernalROM:
					break;
				case PLABank.None:
					break;
				case PLABank.RAM:
					// RAM is written through anyway, don't do it here
					break;
				case PLABank.Sid:
					WriteSid(addr, val);
					break;
				case PLABank.Vic:
					WriteVic(addr, val);
					break;
			}
			WriteMemory(addr, val);
		}
	}
}
