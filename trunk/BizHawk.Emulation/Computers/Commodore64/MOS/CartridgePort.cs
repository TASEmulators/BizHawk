using BizHawk.Emulation.Computers.Commodore64.Cartridges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public class CartridgePort
	{
		private Cartridge cart;
		private bool connected;

		public CartridgePort()
		{
			// start up with no media connected
			Disconnect();
		}

		// ------------------------------------------

		public byte PeekHiExp(int addr) { if (connected) { return cart.PeekDF00(addr & 0x00FF); } else { return 0xFF; } }
		public byte PeekHiRom(int addr) { if (connected) { return cart.PeekA000(addr & 0x1FFF); } else { return 0xFF; } }
		public byte PeekLoExp(int addr) { if (connected) { return cart.PeekDE00(addr & 0x00FF); } else { return 0xFF; } }
		public byte PeekLoRom(int addr) { if (connected) { return cart.Peek8000(addr & 0x1FFF); } else { return 0xFF; } }

		public void PokeHiExp(int addr, byte val) { if (connected) { cart.PokeDF00(addr & 0x00FF, val); } }
		public void PokeHiRom(int addr, byte val) { if (connected) { cart.PokeA000(addr & 0x1FFF, val); } }
		public void PokeLoExp(int addr, byte val) { if (connected) { cart.PokeDE00(addr & 0x00FF, val); } }
		public void PokeLoRom(int addr, byte val) { if (connected) { cart.Poke8000(addr & 0x1FFF, val); } }

		public byte ReadHiExp(ushort addr) { if (connected) { return cart.ReadDF00((ushort)(addr & 0x00FF)); } else { return 0xFF; } }
		public byte ReadHiRom(ushort addr) { if (connected) { return cart.ReadA000((ushort)(addr & 0x1FFF)); } else { return 0xFF; } }
		public byte ReadLoExp(ushort addr) { if (connected) { return cart.ReadDE00((ushort)(addr & 0x00FF)); } else { return 0xFF; } }
		public byte ReadLoRom(ushort addr) { if (connected) { return cart.Read8000((ushort)(addr & 0x1FFF)); } else { return 0xFF; } }

		public void WriteHiExp(ushort addr, byte val) { if (connected) { cart.WriteDF00((ushort)(addr & 0x00FF), val); } }
		public void WriteHiRom(ushort addr, byte val) { if (connected) { cart.WriteA000((ushort)(addr & 0x1FFF), val); } }
		public void WriteLoExp(ushort addr, byte val) { if (connected) { cart.WriteDE00((ushort)(addr & 0x00FF), val); } }
		public void WriteLoRom(ushort addr, byte val) { if (connected) { cart.Write8000((ushort)(addr & 0x1FFF), val); } }

		// ------------------------------------------

		public void Connect(Cartridge newCart)
		{
			cart = newCart;
			connected = true;
		}

		public void Disconnect()
		{
			cart = null;
			connected = false;
		}

		public bool ExRom
		{
			get
			{
				if (connected)
					return cart.ExRom;
				else
					return true;
			}
		}

		public bool Game
		{
			get
			{
				if (connected)
					return cart.Game;
				else
					return true;
			}
		}

		public void HardReset()
		{
			// note: this will not disconnect any attached media
		}

		public bool IRQ
		{
			get
			{
				return true; //todo: hook this up to cartridge
			}
		}

		public bool IsConnected
		{
			get
			{
				return connected;
			}
		}

		public bool NMI
		{
			get
			{
				return true; //todo: hook this up to cartridge
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("connected", ref connected);
			if (connected)
			{
				ser.BeginSection("cartmapper");
				cart.SyncState(ser);
				ser.EndSection();
			}
		}
	}
}
