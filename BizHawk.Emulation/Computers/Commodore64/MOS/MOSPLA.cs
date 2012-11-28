using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// emulates the PLA
	// which handles all bank switching

	public class MOSPLA : IStandardIO
	{
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

		private byte bus;
		private C64Chips chips;
		private bool cia0portRead;
		private PLACpuMap map;
		private bool pinCharen;
		private bool pinExRom;
		private bool pinGame;
		private bool pinHiRam;
		private bool pinLoRam;
		private bool ultimax;
		private Func<byte> vicBankPortRead;

		public MOSPLA(C64Chips newChips, Func<byte>newVicBankPortRead)
		{
			chips = newChips;
			vicBankPortRead = newVicBankPortRead;
			pinExRom = true;
			pinGame = true;
		}

		public void HardReset()
		{
			pinCharen = true;
			pinHiRam = true;
			pinLoRam = true;
			UpdateMap();
		}

		// ------------------------------------

		public bool Charen
		{
			get { return pinCharen; }
			set { pinCharen = value; UpdateMap(); }
		}

		public bool ExRom
		{
			get { return pinExRom; }
			set { pinExRom = value; UpdateMap(); }
		}

		public bool Game
		{
			get { return pinGame; }
			set { pinGame = value; UpdateMap(); }
		}

		public bool HiRam
		{
			get { return pinHiRam; }
			set { pinHiRam = value; UpdateMap(); }
		}

		public bool InputWasRead
		{
			get { return cia0portRead; }
			set { cia0portRead = value; }
		}

		public bool LoRam
		{
			get { return pinLoRam; }
			set { pinLoRam = value; UpdateMap(); }
		}

		public bool UltimaxMode
		{
			get { return ultimax; }
		}

		private void UpdateMap()
		{
			if (ultimax)
				return;

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
				// once in this mode, it is not supposed to change
				map.layout1000 = PLABank.None;
				map.layout8000 = PLABank.CartridgeLo;
				map.layoutA000 = PLABank.None;
				map.layoutC000 = PLABank.None;
				map.layoutD000 = PLABank.IO;
				map.layoutE000 = PLABank.CartridgeHi;
				ultimax = true;
			}
			else
			{
				throw new Exception("Memory configuration missing from PLA, fix this!");
			}
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
					return chips.basicRom.Peek(addr);
				case PLABank.CartridgeHi:
					return chips.cartPort.PeekHiRom(addr);
				case PLABank.CartridgeLo:
					return chips.cartPort.PeekLoRom(addr);
				case PLABank.CharROM:
					return chips.charRom.Peek(addr);
				case PLABank.Cia0:
					return chips.cia0.Peek(addr);
				case PLABank.Cia1:
					return chips.cia1.Peek(addr);
				case PLABank.ColorRam:
					return chips.colorRam.Peek(addr, bus);
				case PLABank.Expansion0:
					return chips.cartPort.PeekLoExp(addr);
				case PLABank.Expansion1:
					return chips.cartPort.PeekHiExp(addr);
				case PLABank.KernalROM:
					return chips.kernalRom.Peek(addr);
				case PLABank.None:
					return 0xFF;
				case PLABank.RAM:
					return chips.ram.Peek(addr);
				case PLABank.Sid:
					return chips.sid.Peek(addr);
				case PLABank.Vic:
					return chips.vic.Peek(addr);
			}
			return 0xFF;
		}

		public void Poke(int addr, byte val)
		{
			switch (Bank((ushort)(addr & 0xFFFF)))
			{
				case PLABank.BasicROM:
					chips.basicRom.Poke(addr, val);
					break;
				case PLABank.CartridgeHi:
					chips.cartPort.PokeHiRom(addr, val);
					break;
				case PLABank.CartridgeLo:
					chips.cartPort.PokeLoRom(addr, val);
					break;
				case PLABank.CharROM:
					chips.charRom.Poke(addr, val);
					break;
				case PLABank.Cia0:
					chips.cia0.Poke(addr, val);
					break;
				case PLABank.Cia1:
					chips.cia1.Poke(addr, val);
					break;
				case PLABank.ColorRam:
					chips.colorRam.Poke(addr, val);
					break;
				case PLABank.Expansion0:
					chips.cartPort.PokeLoExp(addr, val);
					break;
				case PLABank.Expansion1:
					chips.cartPort.PokeHiExp(addr, val);
					break;
				case PLABank.KernalROM:
					chips.kernalRom.Poke(addr, val);
					break;
				case PLABank.None:
					break;
				case PLABank.RAM:
					chips.ram.Poke(addr, val);
					break;
				case PLABank.Sid:
					chips.sid.Poke(addr, val);
					break;
				case PLABank.Vic:
					chips.vic.Poke(addr, val);
					break;
			}
		}

		public byte Read(ushort addr)
		{
			switch (Bank(addr))
			{
				case PLABank.BasicROM:
					bus = chips.basicRom.Read(addr);
					break;
				case PLABank.CartridgeHi:
					bus = chips.cartPort.ReadHiRom(addr);
					break;
				case PLABank.CartridgeLo:
					bus = chips.cartPort.ReadLoRom(addr);
					break;
				case PLABank.CharROM:
					bus = chips.charRom.Read(addr);
					break;
				case PLABank.Cia0:
					if (addr == 0xDC00 || addr == 0xDC01)
						cia0portRead = true;
					bus = chips.cia0.Read(addr);
					break;
				case PLABank.Cia1:
					bus = chips.cia1.Read(addr);
					break;
				case PLABank.ColorRam:
					bus = chips.colorRam.Read(addr, bus);
					break;
				case PLABank.Expansion0:
					bus = chips.cartPort.ReadLoExp(addr);
					break;
				case PLABank.Expansion1:
					bus = chips.cartPort.ReadHiExp(addr);
					break;
				case PLABank.KernalROM:
					bus = chips.kernalRom.Read(addr);
					break;
				case PLABank.None:
					bus = 0xFF;
					break;
				case PLABank.RAM:
					bus = chips.ram.Read(addr);
					break;
				case PLABank.Sid:
					bus = chips.sid.Read(addr);
					break;
				case PLABank.Vic:
					bus = chips.vic.Read(addr);
					break;
			}
			return bus;
		}

		public byte ReadVic(ushort addr)
		{
			addr &= 0x3FFF;

			if (ultimax)
			{
				if (addr >= 0x3000)
					return 0; //todo: change to ROMHI
				else
					return chips.ram.Read(addr);
			}
			else
			{
				if ((addr & 0x7000) == 0x1000)
				{
					return chips.charRom.Read(addr);
				}
				else
				{
					uint bank = (vicBankPortRead() & (uint)0x3);
					switch (bank)
					{
						case 0: addr |= 0xC000; break;
						case 1: addr |= 0x8000; break;
						case 2: addr |= 0x4000; break;
					}
					return chips.ram.Read(addr);
				}
			}
		}

		public void Write(ushort addr, byte val)
		{
			switch (Bank(addr))
			{
				case PLABank.BasicROM:
					chips.basicRom.Write(addr, val);
					break;
				case PLABank.CartridgeHi:
					chips.cartPort.WriteHiRom(addr, val);
					break;
				case PLABank.CartridgeLo:
					chips.cartPort.WriteLoRom(addr, val);
					break;
				case PLABank.CharROM:
					chips.charRom.Write(addr, val);
					break;
				case PLABank.Cia0:
					chips.cia0.Write(addr, val);
					break;
				case PLABank.Cia1:
					chips.cia1.Write(addr, val);
					break;
				case PLABank.ColorRam:
					chips.colorRam.Write(addr, val);
					break;
				case PLABank.Expansion0:
					chips.cartPort.WriteLoExp(addr, val);
					break;
				case PLABank.Expansion1:
					chips.cartPort.WriteHiExp(addr, val);
					break;
				case PLABank.KernalROM:
					chips.kernalRom.Write(addr, val);
					break;
				case PLABank.None:
					break;
				case PLABank.RAM:
					chips.ram.Write(addr, val);
					break;
				case PLABank.Sid:
					chips.sid.Write(addr, val);
					break;
				case PLABank.Vic:
					chips.vic.Write(addr, val);
					break;
			}
		}
	}
}
