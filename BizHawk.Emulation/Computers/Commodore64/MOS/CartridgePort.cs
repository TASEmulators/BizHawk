using System;
using BizHawk.Emulation.Computers.Commodore64.Cartridge;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	sealed public class CartridgePort
	{
        public Func<bool> ReadIRQ;
        public Func<bool> ReadNMI;

		Cart cart;
        bool connected;

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

        public bool ReadExRom() { if (connected) { return cart.ExRom; } else { return true; } }
        public bool ReadGame() { if (connected) { return cart.Game; } else { return true; } }
        public byte ReadHiExp(int addr) { if (connected) { return cart.ReadDF00((addr & 0x00FF)); } else { return 0xFF; } }
		public byte ReadHiRom(int addr) { if (connected) { return cart.ReadA000((addr & 0x1FFF)); } else { return 0xFF; } }
		public byte ReadLoExp(int addr) { if (connected) { return cart.ReadDE00((addr & 0x00FF)); } else { return 0xFF; } }
		public byte ReadLoRom(int addr) { if (connected) { return cart.Read8000((addr & 0x1FFF)); } else { return 0xFF; } }

		public void WriteHiExp(int addr, byte val) { if (connected) { cart.WriteDF00((addr & 0x00FF), val); } }
		public void WriteHiRom(int addr, byte val) { if (connected) { cart.WriteA000((addr & 0x1FFF), val); } }
		public void WriteLoExp(int addr, byte val) { if (connected) { cart.WriteDE00((addr & 0x00FF), val); } }
		public void WriteLoRom(int addr, byte val) { if (connected) { cart.Write8000((addr & 0x1FFF), val); } }

		// ------------------------------------------

		public void Connect(Cart newCart)
		{
			cart = newCart;
			connected = true;
		}

		public void Disconnect()
		{
			cart = null;
			connected = false;
		}

		public void HardReset()
		{
			// note: this will not disconnect any attached media
		}

		public bool IsConnected
		{
			get
			{
				return connected;
			}
		}

        public bool ReadIRQBuffer()
        {
            return true;
        }

        public bool ReadNMIBuffer()
        {
            return true;
        }

        public void SyncState(Serializer ser)
		{
            SaveState.SyncObject(ser, this);
		}
	}
}
